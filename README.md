# Busca Recursiva - Projeto de Prática

Versão simplificada (para estudo) da **busca textual recursiva** de pastas e envios,
inspirada no ArecoSign. O objetivo é praticar a estratégia da *Opção A*: um endpoint
dedicado que executa `WITH RECURSIVE` no PostgreSQL, respeitando permissões e
devolvendo o caminho completo de cada item.

## Stack

| Camada | Tecnologia |
|---|---|
| Frontend | React + TypeScript + Vite |
| Backend | .NET 10, Clean Architecture (Domain / Application / Infrastructure / API) |
| Acesso a dados | Dapper + Npgsql |
| Banco | PostgreSQL 16 (`WITH RECURSIVE`, `ILIKE`, `pg_trgm`) |
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

Sobe o PostgreSQL na porta `15432` (mapeada para `5432` no container) e executa automaticamente `db/01_schema.sql`
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
dentro da pasta aberta). Com `termo` preenchido, faz a busca textual recursiva.

O usuário corrente é simulado pelo header **`X-Auth-Key`** (GUID do usuário).
No frontend há um seletor de usuário:

- **Ana (admin)** — vê todas as pastas/envios da conta.
- **Maria (Financeiro)** — só vê o ramo `Financeiro` (graças ao filtro
  `rlcusuariopasta` aplicado dentro da CTE recursiva).

Ao abrir o app (campo de busca vazio):

- Como **Ana (admin)**, aparecem as pastas-raiz da conta.
- Como **Maria**, aparece apenas o ramo `Financeiro` (e subpastas ao navegar).

Experimente buscar por `contrato`:

- Como **Ana**, aparecem contratos de `ACME`, `Initech`, `Contratos` e `Admissoes`.
- Como **Maria**, aparecem apenas os de `Financeiro > Contratos`.

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
    BuscaApi.Infrastructure/# Dapper + query WITH RECURSIVE
    BuscaApi/               # Web API (controller, Swagger, auth simulada)
    BuscaApi.Testes/        # xUnit + Moq
  web/                 # frontend Vite + React + TS
```

## Origem

Baseado nos documentos de análise e plano de implementação da busca recursiva do
ArecoSign (estratégia "Opção A": `WITH RECURSIVE` + `ILIKE` + `pg_trgm`,
com filtro de permissões espelhando `rlcusuariopasta`).
