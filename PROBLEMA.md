# O problema: a busca não é recursiva

Esta branch (`main`) é o **estado inicial** do ticket *"No arcSing a busca do arquivo
tem que ser recursiva nas pastas"*. É o ponto de partida do exercício: o código como
estava **antes** de qualquer solução.

A solução está na branch `busca-recursiva`, documentada em `SOLUCAO-BUSCA-RECURSIVA.md`
lá.

---

## Relato do cliente

> A barra de busca filtra apenas itens da pasta atualmente aberta. Não localiza
> envios/pastas que estejam em subpastas. O usuário precisa entrar manualmente em
> cada nível para encontrar um item.

Com milhares de arquivos, achar um documento vira uma caçada pasta por pasta.

## Causa raiz

A busca é **100% client-side**. O frontend carrega o conteúdo do nível aberto e
filtra esse array em memória a cada tecla:

```ts
const itensFiltrados =
  termoLimpo.length === 0
    ? itensUnificados
    : itensUnificados.filter((item) => item.nome.toLowerCase().includes(termoLimpo))
```

`itensUnificados` (o `ItensUnificadosAux` do ArecoSign) é populado exclusivamente com
os **filhos diretos** da pasta atual. **A API nunca recebe o termo de busca**, então
não há como ela devolver itens de subníveis — nem que quisesse.

O endpoint disponível só sabe listar um nível:

```
GET /api/envio/conta/{externalKeyConta}/itens?externalKeyPasta={guid?}
```

Sem `termo`, sem paginação, sem caminho. Ele devolve as pastas filhas diretas (ou as
pastas-raiz, quando `externalKeyPasta` é nulo) e os envios daquela pasta.

## O que já existe e funciona

Nem tudo é problema — a parte de permissão já está resolvida no banco, e é dela que a
solução vai reaproveitar o padrão:

- A CTE `PastasPermitidas` em `QueriesEnvio.cs` já usa `WITH RECURSIVE` para expandir
  as pastas de `rlcusuariopasta` até as descendentes. Quem tem acesso a `Financeiro`
  enxerga `Contratos` e `Notas Fiscais`.
- Ana (admin) vê a conta inteira; Maria só o ramo Financeiro.
- Navegação por breadcrumb e preview de PDF por envio.

Ou seja, `WITH RECURSIVE` **já é um padrão presente no projeto**. O que falta é usá-lo
para a busca.

## O que falta (e vira o escopo do ticket)

| Faltando aqui | Vira, na solução |
|---|---|
| A API não recebe o termo | Endpoint dedicado `GET .../buscar?termo=` |
| A busca não desce a árvore | CTE recursiva de subárvore com `ILIKE` |
| O resultado não diz onde o item está | Campo `Caminho` (`Clientes > 2026 > ACME`) |
| Sem paginação | `LIMIT`/`OFFSET` + `COUNT(*) OVER ()` |
| Filtra a cada tecla, sem mínimo de caracteres | Debounce de 300 ms e mínimo de 2 caracteres |
| Sem índice para `ILIKE '%termo%'` | `pg_trgm` + B-trees (`db/03_indices.sql`) |

## Como reproduzir a dor

```bash
docker compose -f docker-compose.db.yml up -d
docker compose up --build -d
```

Abra http://localhost:5173 como **Ana (admin)** e:

1. Na raiz, digite `contrato`. **Nenhum resultado** — nenhuma pasta-raiz se chama
   assim, e os contratos estão três níveis abaixo.
2. Navegue até `Clientes > 2026 > ACME` e digite `contrato` de novo. Agora aparecem —
   mas só porque você já estava na pasta certa.
3. Gere o volume do cliente (`cd scripts && npm install && node gerar-volume.mjs`) e
   busque `Vol-` na raiz. Continua vazio, com ~10.000 envios no banco.

Como **Maria (Financeiro)**, o comportamento é o mesmo, restrito ao ramo dela.
