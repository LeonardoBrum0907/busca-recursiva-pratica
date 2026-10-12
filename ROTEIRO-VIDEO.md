# Roteiro do vídeo — busca recursiva nas pastas

Fala curta. Um take por parte. O bloco da query alonga o vídeo para cerca de **4 minutos**.

**Apps:** `main` em http://localhost:5173 · solução em http://localhost:5174

| Parte | Tempo | Status |
|---|---|---|
| 1. O problema do cliente | ~40 s | rascunho |
| 2. O problema técnico | ~70 s | rascunho |
| 3. A solução | ~60 s | rascunho |
| 4. A query `WITH RECURSIVE` | ~80 s | rascunho |

---

## 1. O problema do cliente (~40 s)

### Fala

O cliente reportou: a busca só filtra a pasta aberta. Não acha arquivo em subpasta. Para encontrar um contrato, tem que abrir pasta por pasta.

Na raiz, busco **contrato**: nada. Os contratos existem — estão três níveis abaixo.

Desço até ACME, busco de novo: agora aparece. A busca filtra o que já está na tela. O cliente queria que ela procurasse na árvore inteira.

### Tela

- App da `main`, Ana.
- Digitar `contrato` na raiz → vazio.
- Entrar `Clientes` → `2026` → `ACME`, buscar `contrato` → os envios.

---

## 2. O problema técnico (~70 s)

Em cada camada: **como está** e **por que isso impede a busca**. Ordem: banco → rota → front.

### Fala

No banco, a pasta não guarda os filhos nem o caminho. Cada linha só aponta o pai. O contrato da ACME está três ponteiros abaixo de Clientes. Gravar assim é simples, mas achar "contrato" exige descer essa cadeia. Hoje ninguém desce.

A API também não. `GET /itens` recebe a pasta aberta e devolve só os filhos diretos. Não existe parâmetro de termo. A recursão que já existe só responde quem pode ver — não o que casa com o texto. Mesmo que o front pedisse os contratos da subárvore, essa rota não tem como devolvê-los.

No front, o termo fica na tela. A cada tecla filtra o array do nível já carregado: na raiz, só Clientes, Financeiro e RH. Digitar não gera request. O vazio não significa que o arquivo não existe. Significa que ele nunca saiu do banco.

### Tela

- Tabela `pasta`: `ACME` aponta para `2026`, que aponta para `Clientes`. Legenda: sem caminho, só o pai.
- Rota `GET /itens`: só `externalKeyPasta`. Circular a ausência de `termo`.
- Network: clicar numa pasta dispara `/itens`. Digitar **não dispara nada**.
- `App.tsx` da `main`: o `.filter` sobre o nível carregado. Na raiz, três nomes — nenhum é "contrato".

---

## 3. A solução (~60 s)

Mesma ordem do problema: **banco → rota → front**. Em cada camada, o que mudou e por que a busca passa a achar o arquivo.

### Fala

A pasta continua gravada do mesmo jeito: só o ponteiro para o pai. Quem passa a descer essa cadeia é uma query no Postgres. Com `WITH RECURSIVE`, ela parte da pasta atual, visita os filhos até as folhas, monta o caminho e filtra o termo. A rede devolve a página de resultado, não a árvore inteira.

Para isso existe uma rota nova: `GET /buscar`, que recebe o termo. `GET /itens` continua listando um nível — a tela inicial não pode trazer milhares de itens. A busca é que atravessa a subárvore.

No front, digitar deixa de filtrar o array local. A partir de dois caracteres, o termo vai para `/buscar`. Cada resultado traz o caminho, porque o Contrato ACME 2026 pode estar em qualquer profundidade. Na raiz, "contrato" encontra o envio sem abrir pasta nenhuma.

### Tela

- App da solução (porta 5174), Ana, na raiz.
- Digitar `contrato` → aparecem os envios, cada um com o caminho `Clientes > 2026 > ACME`.
- Network: `GET /buscar?termo=contrato`. Contrastar com a `main`, em que digitar não gerava request.
- Corte curto em `QueryBuscaArquivos`: a CTE `Subarvore` concatenando `caminho`. O passo a passo dessa query é a parte 4.

---

## 4. A query `WITH RECURSIVE` (~80 s)

Uma query, duas descidas. Cada uma tem **ponto de partida** e **passo que se repete** até não surgir linha nova. Exemplo: Ana, na raiz, termo `contrato`.

### Fala

`WITH RECURSIVE` não é um loop no .NET. É a mesma query se juntando a si mesma. O primeiro `SELECT` é o ponto de partida. O `SELECT` de baixo junta o filho cujo pai já está no resultado. O Postgres repete esse passo e para quando não aparece pasta nova.

A primeira descida se chama `PastasPermitidas`: quem pode ver. Para a Ana, admin, o ponto de partida já é todas as pastas da conta. Para a Maria, é só Financeiro, o vínculo em `rlcusuariopasta`. O passo seguinte puxa Contratos e Notas Fiscais, porque o pai delas já entrou. No passo de depois não há filho. Para. Clientes nunca entra no conjunto dela.

A segunda descida se chama `Subarvore`: onde procurar. Ela só visita pasta que já está em `PastasPermitidas`. Na busca da raiz, o ponto de partida são as raízes — Clientes, Financeiro e RH — e o caminho, nesse momento, é só o nome. O passo seguinte cola o filho: `Clientes` vira `Clientes > 2026`. O próximo cola de novo: `Clientes > 2026 > ACME`. ACME não tem filho. Aquele ramo acaba.

O termo entra só depois, em cima da subárvore pronta. Um `ILIKE` no nome da pasta, outro no assunto do envio. O Contrato ACME 2026 casa no assunto e herda o caminho que a descida montou: `Clientes > 2026 > ACME`.

### Tela

`QueryBuscaArquivos`, em `QueriesEnvio.cs`. Em cada CTE, mostrar os dois blocos: o `SELECT` de cima (partida) e o `SELECT` que faz `JOIN` na própria CTE (passo).

Quadro da `Subarvore`, busca na raiz:

| Passo | Entram | Caminho |
|---|---|---|
| 0 | Clientes, Financeiro, RH | o próprio nome |
| 1 | 2026, 2025, Contratos, Notas Fiscais, Admissoes | `Clientes > 2026` |
| 2 | ACME, Globex, Initech | `Clientes > 2026 > ACME` |
| 3 | ninguém | para |

Depois do quadro, o `ILIKE` de `Envios` e o campo `caminho` do Contrato ACME 2026.

Corte de um segundo na Maria: o passo 0 de `PastasPermitidas` é só Financeiro. Clientes não aparece no quadro dela.
