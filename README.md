# Busca por nível — versão ingênua (prática)

Versão simplificada para **comparar** com a Opção A da busca recursiva do ArecoSign.
Nesta branch a API **carrega a árvore inteira em memória** e a busca textual vale
**somente no nível atual** (não percorre subpastas).

A estratégia boa (`WITH RECURSIVE` + `ILIKE` + `pg_trgm`) está na branch
`busca-recursiva`. O detalhe desta versão está em [SOLUCAO-BUSCA-INGENUA.md](SOLUCAO-BUSCA-INGENUA.md).

## Stack

| Camada | Tecnologia |
|---|---|
| Frontend | React + TypeScript + Vite |
| Backend | .NET 10, Clean Architecture (Domain / Application / Infrastructure / API) |
| Acesso a dados | Dapper + Npgsql |
| Banco | PostgreSQL 16 |
| Testes | xUnit + Moq |

## Domínio

```
Conta
 └─ Pasta (hierárquica via idpastapai)
     └─ Envio
         └─ Destinatario
Usuario ──< rlcusuariopasta >── Pasta   (permissões de visibilidade)
```

A árvore semeada:

```
Clientes
  2026
    ACME            -> Contrato ACME 2026, Aditivo Contrato ACME
    Globex          -> Proposta Comercial Globex
  2025
    Initech         -> Contrato Initech 2025
Financeiro
  Contratos         -> Contrato de Locacao, Contrato de Prestacao de Servico
  Notas Fiscais     -> Nota Fiscal 001 - Maio
RH
  Admissoes         -> Contrato de Admissao Joao
```

## Como rodar

### 1. Banco de dados (PostgreSQL via Docker)

```bash
docker compose up -d
```

Sobe a API/frontend ingênuos (`5080` / `5173`) e o Postgres compartilhado em `15432`.
Na branch `busca-recursiva`, o mesmo comando sobe a API/frontend recursivos (`5081` / `5174`)
reusando o mesmo banco — os dois stacks podem ficar no ar juntos.

O PostgreSQL fica na porta `15432` (mapeada para `5432` no container) e executa automaticamente `db/01_schema.sql`
e `db/02_seed.sql`.

> Para recriar do zero (reaplicar schema/seed): `docker compose down -v && docker compose up -d`.

### 2. API (.NET 10)

```bash
cd api
dotnet restore BuscaApi.sln
dotnet build BuscaApi.sln
dotnet run --project BuscaApi
```

- API: `http://localhost:5080`
- Swagger: `http://localhost:5080/swagger`

### 3. Frontend (Vite)

```bash
cd web
npm install
npm run dev
```

- App: `http://localhost:5173` (faz proxy de `/api` para a API).

## Como praticar a busca

O endpoint principal:

```
GET /api/envio/conta/{externalKeyConta}/buscar
    ?termo={texto}
    &externalKeyPasta={guid?}   # null = busca a partir da raiz da conta
    &incluirPastas=true
    &pagina=1
    &tamanhoPagina=50
```

Quando `termo` está **vazio**, o endpoint funciona em modo *navegação*: devolve os
**filhos diretos** da pasta atual (pastas-raiz na conta, ou subpastas + envios
dentro da pasta aberta). Com `termo` preenchido, filtra **apenas esse mesmo nível**
(não desce a árvore). A árvore completa da conta já foi carregada em memória.

O usuário corrente é simulado pelo header **`X-Auth-Key`** (GUID do usuário).
No frontend há um seletor de usuário:

- **Ana (admin)** — vê todas as pastas-raiz da conta.
- **Maria (Financeiro)** — só vê o ramo `Financeiro` (vínculo em `rlcusuariopasta`,
  expandido em memória para as subpastas).

Ao abrir o app (campo de busca vazio):

- Como **Ana (admin)**, aparecem as pastas-raiz da conta.
- Como **Maria**, aparece apenas o ramo `Financeiro` (e subpastas ao navegar).

Experimente buscar por `contrato` **na raiz**:

- Como **Ana** ou **Maria**, o resultado tende a ser **vazio** — nenhuma pasta-raiz
  se chama "contrato". Entre em `Financeiro > Contratos` (ou `Clientes > 2026 > ACME`)
  e busque de novo para ver os envios daquele nível.

## Testes

```bash
cd api
dotnet test BuscaApi.sln
```

## Estrutura

```
busca-recursiva-pratica/
  docker-compose.yml
  db/                  # schema + seed (executados na 1a subida do container)
  api/                 # solução .NET 10
    BuscaApi.Domain/        # entidades, DTOs, interfaces
    BuscaApi.Application/   # EnvioService (regra de negócio + permissões)
    BuscaApi.Infrastructure/# Dapper + carga da arvore em memoria (filtro por nivel)
    BuscaApi/               # Web API (controller, Swagger, auth simulada)
    BuscaApi.Testes/        # xUnit + Moq
  web/                 # frontend Vite + React + TS
```

## Origem

Contraste da Opção A do ArecoSign (`busca-recursiva`): aqui a busca é o caminho
ingênuo (árvore em memória + um nível por vez). Ver [SOLUCAO-BUSCA-INGENUA.md](SOLUCAO-BUSCA-INGENUA.md).
