# Versão ingênua da busca (experiência ruim)

Esta branch (`main`) reproduz **de propósito** o jeito não ideal citado em
`SOLUCAO-BUSCA-RECURSIVA.md` na branch `busca-recursiva`:

> várias queries / um nível por vez, ou carregar a árvore toda em memória

Aqui as duas coisas acontecem juntas, para comparar com a **Opção A**
(`WITH RECURSIVE` no PostgreSQL).

---

## O que esta versão faz

1. **Carrega a árvore inteira em memória** a cada chamada  
   Pastas, envios e destinatários da conta vêm do banco **sem filtro de termo**.
   Só depois o .NET monta a árvore, aplica permissão e pagina.

2. **A busca não desce a árvore**  
   Com um termo digitado, o usuário vê apenas pastas e envios do **nível atual**.
   Buscar `contrato` na raiz **não** encontra `Clientes > 2026 > ACME`.
   É preciso abrir pasta por pasta até o nível onde o arquivo está.

3. **Navegação e permissões continuam iguais**  
   - Ana (admin) vê todas as pastas-raiz.  
   - Maria só o ramo Financeiro.  
   - Clique em pasta entra no nível seguinte.  
   - Clique em envio abre o PDF.  
   - Volume de ~10.000 envios e Docker são os mesmos da outra branch.

---

## Por que a experiência é ruim

| Ação | Resultado nesta versão | Na Opção A |
|------|------------------------|------------|
| Buscar `contrato` na raiz (Ana) | 0 resultados (nenhuma pasta-raiz se chama contrato) | ~1.250 envios na subárvore |
| Buscar `Vol-` na raiz | 0 resultados | ~10.000 envios |
| Encontrar um contrato | Navegar `Clientes > 2026 > ACME` e só então buscar | Um campo de busca a partir da raiz |
| Cada tecla / cada pasta | SELECT de **todas** as pastas e envios da conta | Uma query recursiva filtrada no banco |

Com o volume do cliente, cada request puxa ~10 mil envios + ~1 mil pastas para a
API filtrar em memória — mesmo quando o usuário só quer os 3 itens da pasta atual.

---

## Como comparar

1. Esta branch (`main`): http://localhost:5173 — busque `contrato` na raiz.  
2. Branch `busca-recursiva` (Opção A): a mesma busca lista os documentos em qualquer profundidade.

```bash
docker compose up --build -d
```

O banco compartilhado (`pgdata`) já pode ter os 10.000 envios gerados. Se não:

```bash
cd scripts && npm install && node gerar-volume.mjs
```
