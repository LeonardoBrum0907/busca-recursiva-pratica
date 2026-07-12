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

- .NET SDK 10
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

## 1. Subir o banco (PostgreSQL via Docker)

```bash
docker compose up -d
```

Isso cria o container `busca_postgres` na porta `15432` e executa automaticamente
`db/01_schema.sql` e `db/02_seed.sql` na primeira subida.

Conferir se subiu:

```bash
docker compose ps
docker compose logs postgres | tail -n 20
```

> Para recriar do zero (reaplicar schema + seed): `docker compose down -v && docker compose up -d`

---

## 2. API (.NET 10) — restore, testes e build

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

No app: troque o usuário no seletor e digite `contrato` para ver a diferença de
permissões em ação.

---

## 4. Checklist de validação

- [ ] Buscar `contrato` como **Ana** retorna itens de ACME, Initech, Contratos e Admissões.
- [ ] Buscar `contrato` como **Maria** retorna apenas os de `Financeiro > Contratos`.
- [ ] Cada resultado mostra o **caminho** (ex.: `Clientes > 2026 > ACME`).
- [ ] Digitar 1 caractere não dispara busca (mínimo de 2).
- [ ] Digitação rápida dispara **uma** chamada (debounce 300 ms — ver aba Network).
- [ ] O spinner aparece durante a busca.
- [ ] `dotnet test BuscaApi.sln` verde.

---

## 5. Publicar no GitHub (repositório público)

```bash
cd "c:/Users/Leonardo.Brum/projetos/busca-recursiva-pratica"

git init
git add .
git commit -m "feat: projeto de pratica de busca recursiva (API .NET 10 + React/Vite)"

# Confirme que o gh esta logado:
gh auth status

# Cria o repo publico e ja faz o push:
gh repo create busca-recursiva-pratica --public --source . --push
```

> Para outro nome, troque `busca-recursiva-pratica`.
> Se preferir privado, use `--private`.

Ao final, o `gh` mostra a URL do repositório criado.

---

## Solução de problemas

- **API não conecta ao banco:** confirme `docker compose ps` (container `healthy`) e
  a connection string em `api/BuscaApi/appsettings.json`
  (`Host=localhost;Port=15432;Database=busca;Username=busca;Password=busca`).
- **Porta 15432 ocupada:** ajuste o mapeamento em `docker-compose.yml` (ex.: `"15433:5432"`)
  e a connection string para `Port=15433`.
- **CORS no frontend:** o dev server usa proxy (`/api`), então não deve ocorrer. Se
  rodar o front em outra porta, ajuste `WithOrigins(...)` em `api/BuscaApi/Program.cs`.
- **`gh` não autenticado:** rode `gh auth login` e repita o passo 5.
```
