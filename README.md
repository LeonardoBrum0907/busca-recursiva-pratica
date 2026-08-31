# Busca recursiva — projeto de prática

Reprodução reduzida do ticket *"No arcSing a busca do arquivo tem que ser recursiva
nas pastas"* (ArecoSign), para estudar o problema e a solução isolados do resto do
sistema.

O repositório tem duas branches:

| Branch | Papel |
|---|---|
| `main` | **Estado inicial** — a busca filtra em memória, no navegador, só o nível aberto. Ver [PROBLEMA.md](PROBLEMA.md). |
| `busca-recursiva` | **Solução** — endpoint dedicado com `WITH RECURSIVE` no PostgreSQL. Ver `SOLUCAO-BUSCA-RECURSIVA.md` lá. |

A `busca-recursiva` sai da `main`, então o `git diff main..busca-recursiva` mostra
exatamente o que o ticket pedia — nada mais.

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

O Postgres é **único e compartilhado** pelas duas branches (mesmo volume). Suba-o uma
vez, e depois a stack da branch em que você estiver:

```bash
docker compose -f docker-compose.db.yml up -d   # Postgres em :15432
docker compose up --build -d                    # API + web desta branch
```

| Branch | Web | Swagger |
|---|---|---|
| `main` | http://localhost:5173 | http://localhost:5080/swagger |
| `busca-recursiva` | http://localhost:5174 | http://localhost:5081/swagger |

As duas podem ficar no ar ao mesmo tempo, lado a lado, apontando para o mesmo banco.

Detalhes de execução local (sem container), geração de volume e troubleshooting estão
em [INSTRUCOES.md](INSTRUCOES.md).

## O endpoint desta branch

```
GET /api/envio/conta/{externalKeyConta}/itens?externalKeyPasta={guid?}
```

Devolve as pastas filhas diretas (ou as pastas-raiz, se `externalKeyPasta` for nulo) e
os envios daquela pasta. **Não recebe termo de busca** — é justamente esse o problema
que o ticket descreve.

O usuário corrente é simulado pelo header **`X-Auth-Key`** (GUID do usuário), e o
frontend tem um seletor:

- **Ana (admin)** — vê todas as pastas da conta.
- **Maria (Financeiro)** — só vê o ramo `Financeiro` (vínculo em `rlcusuariopasta`,
  expandido pela CTE recursiva de permissões).

## Testes

```bash
cd api
dotnet test BuscaApi.sln
```

## Estrutura

```
busca-recursiva-pratica/
  docker-compose.db.yml  # Postgres compartilhado entre as branches
  docker-compose.yml     # API + web desta branch
  db/                    # schema + seed (executados na 1a subida do container)
  api/                   # solução .NET 10
    BuscaApi.Domain/         # entidades, DTOs, interfaces
    BuscaApi.Application/    # EnvioService (regra de negócio + permissões)
    BuscaApi.Infrastructure/ # Dapper + queries SQL
    BuscaApi/                # Web API (controller, Swagger, auth simulada)
    BuscaApi.Testes/         # xUnit + Moq
  scripts/               # gerador de volume (~10.000 envios)
  web/                   # frontend Vite + React + TS
```
