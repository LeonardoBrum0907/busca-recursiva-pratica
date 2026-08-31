# Instruções para finalizar e publicar o projeto

O código está **100% escrito e revisado**. Faltam apenas os passos que dependem de
terminal (não foi possível executá-los no ambiente atual): instalar dependências,
rodar build/testes, subir o banco e **criar/publicar o repositório no GitHub**.

Execute os comandos abaixo na ordem. Todos partem da raiz do projeto:

```bash
cd "c:/Users/Leonardo.Brum/projetos/busca-recursiva-pratica"
```

---

## Pré-requisitos

- .NET SDK 9
- Node.js 18+ (com npm)
- Docker Desktop (para o PostgreSQL)
- Git e GitHub CLI (`gh`) autenticado (`gh auth login`)

Verifique rapidamente:

```bash
dotnet --version
node --version
docker --version
gh --version
```

---

## 1. Subir o ambiente em containers (recomendado)

Na raiz do projeto:

```bash
docker compose up --build -d
```

Sobe a API e o frontend **ingênuos** desta branch. O PostgreSQL é **único e compartilhado**
com a branch `busca-recursiva` (mesmo volume `busca-recursiva-pratica_pgdata`):

- **web ingênua** — http://localhost:5173
- **API / Swagger ingênua** — http://localhost:5080/swagger
- **PostgreSQL (compartilhado)** — `localhost:15432`

Na branch `busca-recursiva`, o mesmo comando sobe a API/frontend recursivos em
`5174` / `5081`, sem criar outro banco. Os dois frontends podem ficar no ar juntos.

Os PDFs gerados em `web/public/documentos/volume/` são montados no container do frontend.

Para ver logs: `docker compose logs -f`

Para parar só esta branch (mantém o banco e o outro stack): `docker compose stop api-ingenua web-ingenua`

Para parar tudo, inclusive o Postgres compartilhado: `docker compose down`

> Não use `down -v` se quiser preservar os dados: isso apaga o volume compartilhado.

---

## 1b. Subir só o banco (desenvolvimento local)

```bash
docker compose up -d postgres
```

Isso cria o container `busca_postgres` na porta `15432` e executa automaticamente
`db/01_schema.sql` e `db/02_seed.sql` na primeira subida.

Conferir se subiu:

```bash
docker compose ps
docker compose logs postgres | tail -n 20
```

> Para recriar do zero (reaplicar schema + seed): `docker compose down -v && docker compose up -d`
>
> Isso apaga o volume compartilhado com a busca recursiva. Use só se quiser zerar os dados.

---

## 2. API (.NET 9) — restore, testes e build

```bash
cd api
dotnet restore BuscaApi.sln
dotnet build BuscaApi.sln
dotnet test BuscaApi.sln
```

Esperado: build sem erros e **6 testes verdes**.

Rodar a API:

```bash
dotnet run --project BuscaApi
```

- API: http://localhost:5080
- Swagger: http://localhost:5080/swagger

Teste rápido no Swagger (ou via curl), usando a conta semeada:

```bash
# Ana (admin) — ve tudo
curl -H "X-Auth-Key: a0000000-0000-0000-0000-000000000001" \
  "http://localhost:5080/api/envio/conta/11111111-1111-1111-1111-111111111111/buscar?termo=contrato"

# Maria (restrita ao Financeiro) — ve so o ramo Financeiro
curl -H "X-Auth-Key: a0000000-0000-0000-0000-000000000002" \
  "http://localhost:5080/api/envio/conta/11111111-1111-1111-1111-111111111111/buscar?termo=contrato"
```

---

## 3. Frontend (Vite + React) — em outro terminal

```bash
cd "c:/Users/Leonardo.Brum/projetos/busca-recursiva-pratica/web"
npm install
npm run dev
```

- App: http://localhost:5173 (faz proxy de `/api` para a API em :5080)

No app: na raiz, digite `contrato` — a busca **não** encontra documentos nas
subpastas. Navegue até `Financeiro > Contratos` e busque de novo.

---

## 4. Checklist de validação

- [ ] Buscar `contrato` na **raiz** como Ana retorna vazio (ou só pastas-raiz cujo nome case).
- [ ] Navegar até `Financeiro > Contratos` e buscar `contrato` lista os envios daquela pasta.
- [ ] Como **Maria**, a raiz mostra só Financeiro; `Clientes` não aparece.
- [ ] Cada resultado mostra o **caminho** (ex.: `Clientes > 2026 > ACME`).
- [ ] Digitar 1 caractere não dispara busca (mínimo de 2).
- [ ] Digitação rápida dispara **uma** chamada (debounce 300 ms — ver aba Network).
- [ ] O spinner aparece durante a busca (e a API carrega a árvore inteira a cada request).
- [ ] `dotnet test BuscaApi.sln` verde.

---

## 5. Publicar no GitHub (repositório público)

```bash
cd "c:/Users/Leonardo.Brum/projetos/busca-recursiva-pratica"

git init
git add .
git commit -m "feat: projeto de pratica de busca recursiva (API .NET 9 + React/Vite)"

# Confirme que o gh esta logado:
gh auth status

# Cria o repo publico e ja faz o push:
gh repo create busca-recursiva-pratica --public --source . --push
```

> Para outro nome, troque `busca-recursiva-pratica`.
> Se preferir privado, use `--private`.

Ao final, o `gh` mostra a URL do repositório criado.

---

## 6. Simular volume do cliente (10.000 arquivos)

Para reproduzir a lentidão de busca e navegação que o cliente enfrenta com milhares
de arquivos, use o gerador de volume. Ele insere ~10.000 envios no PostgreSQL e
replica o `teste.pdf` para cada um em `web/public/documentos/volume/`.

Pré-requisito: banco rodando (`docker compose up -d`) com o seed pequeno já aplicado.

```bash
cd scripts
npm install
node gerar-volume.mjs
```

Ou, a partir do frontend:

```bash
cd web
npm run gerar-volume
```

Opções:

```bash
node gerar-volume.mjs --quantidade 5000   # gera metade do volume
node gerar-volume.mjs --limpar            # remove volume existente e regera
```

Após gerar, recarregue o app e teste:

- Na **raiz**, busque `contrato` ou `Vol-` — quase nenhum resultado (a busca não desce).
- Navegue em `Clientes > Volume > Setor_001 > Pasta_001` e busque `Vol-` para ver os envios **daquela pasta**.
- Como **Maria**, só o ramo Financeiro aparece na navegação.
- Abra um envio — o preview carrega o PDF (`/documentos/volume/{uuid}.pdf` ou `teste.pdf` no seed).

> Os PDFs gerados ficam em `web/public/documentos/volume/` (ignorados pelo git).
> Estimativa de disco: ~280 MB para 10.000 arquivos.

---

## Solução de problemas

- **API não conecta ao banco:** confirme `docker compose ps` (container `healthy`) e
  a connection string em `api/BuscaApi/appsettings.json`
  (`Host=localhost;Port=5432;Database=busca;Username=busca;Password=busca`).
- **Porta 5432 ocupada:** ajuste o mapeamento em `docker-compose.yml` (ex.: `"5433:5432"`)
  e a connection string para `Port=5433`.
- **CORS no frontend:** o dev server usa proxy (`/api`), então não deve ocorrer. Se
  rodar o front em outra porta, ajuste `WithOrigins(...)` em `api/BuscaApi/Program.cs`.
- **`gh` não autenticado:** rode `gh auth login` e repita o passo 5.
```
