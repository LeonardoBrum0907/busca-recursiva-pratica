# Instruções de execução

Como subir, rodar e validar o projeto. Todos os comandos partem da raiz do
repositório.

## Pré-requisitos

- .NET SDK 10
- Node.js 18+ (com npm)
- Docker Desktop (para o PostgreSQL)

Verifique rapidamente:

```bash
dotnet --version
node --version
docker --version
```

---

## 1. Subir o ambiente em containers (recomendado)

O Postgres é único e compartilhado pelas duas branches — dois bancos gravando no mesmo
volume corromperiam os dados. Por isso ele vive num compose separado:

```bash
docker compose -f docker-compose.db.yml up -d
docker compose up --build -d
```

Nesta branch (`busca-recursiva`, a solução):

- **Web** — http://localhost:5174
- **API / Swagger** — http://localhost:5081/swagger
- **PostgreSQL (compartilhado)** — `localhost:15432`

Na branch `main`, o mesmo par de comandos sobe a API/frontend em `5173` / `5080`,
contra o mesmo banco. Deixe as duas no ar para comparar o antes e o depois.

Os PDFs gerados em `web/public/documentos/volume/` são montados no container do
frontend.

```bash
docker compose logs -f                          # logs desta branch
docker compose down                             # para só esta branch
docker compose -f docker-compose.db.yml down    # para o Postgres compartilhado
```

> Para recriar o banco do zero (reaplicar schema + seed + índices):
> `docker compose -f docker-compose.db.yml down -v && docker compose -f docker-compose.db.yml up -d`
>
> Isso apaga o volume compartilhado com a outra branch.

### Índices

`db/03_indices.sql` é aplicado automaticamente na primeira subida do banco, junto com
o schema e o seed. Para aplicá-lo num banco que já existe (por exemplo, um criado a
partir da `main`, que não tem esse arquivo):

```bash
docker exec -i busca_postgres psql -U busca -d busca < db/03_indices.sql
```

---

## 2. API — restore, testes e build

```bash
cd api
dotnet restore BuscaApi.sln
dotnet build BuscaApi.sln
dotnet test BuscaApi.sln
```

Esperado: build sem erros e **11 testes verdes**.

Rodar a API fora do container:

```bash
dotnet run --project BuscaApi
```

Teste rápido no Swagger (ou via curl), usando a conta semeada:

```bash
# Ana (admin) — acha contratos em qualquer profundidade
curl -H "X-Auth-Key: a0000000-0000-0000-0000-000000000001" \
  "http://localhost:5081/api/envio/conta/11111111-1111-1111-1111-111111111111/buscar?termo=contrato"

# Maria (restrita ao Financeiro) — só o ramo dela
curl -H "X-Auth-Key: a0000000-0000-0000-0000-000000000002" \
  "http://localhost:5081/api/envio/conta/11111111-1111-1111-1111-111111111111/buscar?termo=contrato"

# Navegação (sem termo)
curl -H "X-Auth-Key: a0000000-0000-0000-0000-000000000001" \
  "http://localhost:5081/api/envio/conta/11111111-1111-1111-1111-111111111111/itens"
```

### Perfilar a query

Com o volume gerado, vale medir o pior caso:

```bash
docker exec -it busca_postgres psql -U busca -d busca
```

E rodar `EXPLAIN ANALYZE` na `QueryBuscaArquivos` (em
`api/BuscaApi.Infrastructure/Repositorios/Queries/QueriesEnvio.cs`) com `termo = 'Vol-'`.
Sem os índices trigram o plano vira sequential scan em `envio`.

---

## 3. Frontend — em outro terminal

```bash
cd web
npm install
npm run dev
```

- App: http://localhost:5174 (faz proxy de `/api` para a API em `:5081`)

---

## 4. Checklist de validação

- [ ] Como **Ana**, campo vazio: a raiz lista `Clientes`, `Financeiro` e `RH`.
- [ ] Como **Ana**, `contrato` na raiz: aparecem envios de `Clientes > 2026 > ACME`,
      `Clientes > 2025 > Initech`, `Financeiro > Contratos` e `RH > Admissoes`.
- [ ] Cada resultado mostra o **caminho** completo.
- [ ] `ACME` na raiz devolve a pasta `ACME` **uma única vez**.
- [ ] `Financeiro` na raiz devolve a pasta-raiz `Financeiro`.
- [ ] Dentro de `Financeiro`, buscar `contrato` não devolve a própria `Financeiro`.
- [ ] Como **Maria**, `contrato` na raiz devolve só o ramo Financeiro.
- [ ] Digitar 1 caractere não dispara busca (mínimo de 2).
- [ ] Digitação rápida dispara **uma** chamada (debounce de 300 ms — ver aba Network).
- [ ] O spinner aparece durante a busca.
- [ ] Clicar num envio abre o preview do PDF.
- [ ] `dotnet test BuscaApi.sln` verde.

---

## 5. Simular volume do cliente (10.000 arquivos)

Para reproduzir a carga que o cliente enfrenta, use o gerador de volume. Ele insere
~10.000 envios no PostgreSQL e replica o `teste.pdf` para cada um em
`web/public/documentos/volume/`.

Pré-requisito: banco rodando com o seed pequeno já aplicado.

```bash
cd scripts
npm install
node gerar-volume.mjs
```

Ou, a partir do frontend: `cd web && npm run gerar-volume`.

Opções:

```bash
node gerar-volume.mjs --quantidade 5000   # gera metade do volume
node gerar-volume.mjs --limpar            # remove volume existente e regera
```

Depois de gerar, os termos interessantes são:

- `Vol-` — pior caso, casa quase todos os ~10.000 envios gerados.
- `contrato` — cerca de 1.250 resultados.

Compare com a `main` na porta 5173: lá, os mesmos termos na raiz não retornam nada.

> Os PDFs gerados ficam em `web/public/documentos/volume/` (ignorados pelo git).
> Estimativa de disco: ~280 MB para 10.000 arquivos.

---

## Solução de problemas

- **API não conecta ao banco:** confirme `docker compose -f docker-compose.db.yml ps`
  (container `healthy`) e a connection string em `api/BuscaApi/appsettings.json`
  (`Host=localhost;Port=15432;Database=busca;Username=busca;Password=busca`).
- **Porta 15432 ocupada:** ajuste o mapeamento em `docker-compose.db.yml`
  (ex.: `"15433:5432"`) e a connection string para `Port=15433`.
- **Busca lenta com volume:** confirme que os índices existem
  (`\di ix_*` no psql). Se não, aplique `db/03_indices.sql`.
- **CORS no frontend:** o dev server usa proxy (`/api`), então não deve ocorrer. Se
  rodar o front em outra porta, ajuste `WithOrigins(...)` em `api/BuscaApi/Program.cs`.
