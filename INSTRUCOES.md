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

Nesta branch (`main`, estado inicial):

- **Web** — http://localhost:5173
- **API / Swagger** — http://localhost:5080/swagger
- **PostgreSQL (compartilhado)** — `localhost:15432`

Na branch `busca-recursiva`, o mesmo par de comandos sobe a API/frontend em `5174` /
`5081`, contra o mesmo banco. As duas stacks podem ficar no ar juntas para comparar.

Os PDFs gerados em `web/public/documentos/volume/` são montados no container do
frontend.

```bash
docker compose logs -f                          # logs desta branch
docker compose down                             # para só esta branch
docker compose -f docker-compose.db.yml down    # para o Postgres compartilhado
```

> Para recriar o banco do zero (reaplicar schema + seed):
> `docker compose -f docker-compose.db.yml down -v && docker compose -f docker-compose.db.yml up -d`
>
> Isso apaga o volume compartilhado com a outra branch.

---

## 2. API — restore, testes e build

```bash
cd api
dotnet restore BuscaApi.sln
dotnet build BuscaApi.sln
dotnet test BuscaApi.sln
```

Esperado: build sem erros e **5 testes verdes**.

Rodar a API fora do container:

```bash
dotnet run --project BuscaApi
```

Teste rápido no Swagger (ou via curl), usando a conta semeada:

```bash
# Ana (admin) — pastas-raiz da conta
curl -H "X-Auth-Key: a0000000-0000-0000-0000-000000000001" \
  "http://localhost:5080/api/envio/conta/11111111-1111-1111-1111-111111111111/itens"

# Maria (restrita ao Financeiro) — só o ramo Financeiro
curl -H "X-Auth-Key: a0000000-0000-0000-0000-000000000002" \
  "http://localhost:5080/api/envio/conta/11111111-1111-1111-1111-111111111111/itens"
```

Repare que não há parâmetro `termo`: nesta branch a API não sabe o que o usuário
digitou.

---

## 3. Frontend — em outro terminal

```bash
cd web
npm install
npm run dev
```

- App: http://localhost:5173 (faz proxy de `/api` para a API em `:5080`)

---

## 4. Checklist de validação

- [ ] Como **Ana**, a raiz lista `Clientes`, `Financeiro` e `RH`.
- [ ] Como **Maria**, a raiz mostra só `Financeiro`; `Clientes` não aparece.
- [ ] Digitar `contrato` na **raiz** não retorna nada — a busca não desce a árvore.
- [ ] Navegar até `Clientes > 2026 > ACME` e digitar `contrato` lista os envios daquele nível.
- [ ] Na aba Network, digitar **não** dispara requisição: o filtro é client-side.
- [ ] Clicar num envio abre o preview do PDF.
- [ ] `dotnet test BuscaApi.sln` verde.

---

## 5. Simular volume do cliente (10.000 arquivos)

Para reproduzir a lentidão que o cliente enfrenta com milhares de arquivos, use o
gerador de volume. Ele insere ~10.000 envios no PostgreSQL e replica o `teste.pdf`
para cada um em `web/public/documentos/volume/`.

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

Após gerar, recarregue o app e teste:

- Na **raiz**, busque `contrato` ou `Vol-` — nenhum resultado, a busca não desce.
- Navegue em `Clientes > Volume > Setor_001 > Pasta_001` e busque `Vol-` para ver os
  envios **daquela pasta**.
- Abra um envio — o preview carrega o PDF (`/documentos/volume/{uuid}.pdf`, ou
  `teste.pdf` nos envios do seed).

> Os PDFs gerados ficam em `web/public/documentos/volume/` (ignorados pelo git).
> Estimativa de disco: ~280 MB para 10.000 arquivos.

---

## Solução de problemas

- **API não conecta ao banco:** confirme `docker compose -f docker-compose.db.yml ps`
  (container `healthy`) e a connection string em `api/BuscaApi/appsettings.json`
  (`Host=localhost;Port=15432;Database=busca;Username=busca;Password=busca`).
- **Porta 15432 ocupada:** ajuste o mapeamento em `docker-compose.db.yml`
  (ex.: `"15433:5432"`) e a connection string para `Port=15433`.
- **CORS no frontend:** o dev server usa proxy (`/api`), então não deve ocorrer. Se
  rodar o front em outra porta, ajuste `WithOrigins(...)` em `api/BuscaApi/Program.cs`.
