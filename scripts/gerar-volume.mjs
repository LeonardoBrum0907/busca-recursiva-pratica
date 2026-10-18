#!/usr/bin/env node
/**
 * Gera volume de dados para simular o ambiente do cliente:
 * - Pastas e envios no PostgreSQL
 * - Cópia de teste.pdf para cada envio em web/public/documentos/volume/
 *
 * Uso:
 *   node gerar-volume.mjs
 *   node gerar-volume.mjs --quantidade 5000
 *   node gerar-volume.mjs --limpar
 *   node gerar-volume.mjs --pasta-grande 5000
 *
 * --pasta-grande N cria, além da árvore, uma pasta Clientes > Volume > Arquivo_Geral com N
 * envios diretos. É o caso da pasta que o cliente abre e o front precisa listar inteira.
 */

import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { randomUUID } from 'node:crypto'
import pg from 'pg'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const RAIZ = path.resolve(__dirname, '..')

const PDF_ORIGEM = path.join(RAIZ, 'web/public/documentos/teste.pdf')
const PDF_DESTINO_DIR = path.join(RAIZ, 'web/public/documentos/volume')

const ID_CONTA = 1
const LOTE = 500

const TIPOS_ASSUNTO = [
  'Contrato',
  'Nota Fiscal',
  'Proposta',
  'Aditivo',
  'Termo',
  'Relatorio',
  'Orcamento',
  'Recibo',
]

const STATUS = ['Pendente', 'Assinado', 'Expirado', 'Cancelado']

const RAMOS_PADRAO = [
  { idPastaPai: 1, proporcao: 0.8, setores: 40, pastasPorSetor: 20, enviosPorPasta: 10 },
  { idPastaPai: 7, proporcao: 0.2, setores: 10, pastasPorSetor: 20, enviosPorPasta: 10 },
]

function parseArgs() {
  const args = process.argv.slice(2)
  let quantidade = 10_000
  let limpar = false
  let pastaGrande = 0

  for (let i = 0; i < args.length; i++) {
    if (args[i] === '--limpar') limpar = true
    if (args[i] === '--pasta-grande' && args[i + 1]) {
      pastaGrande = parseInt(args[++i], 10)
      if (Number.isNaN(pastaGrande) || pastaGrande < 1) {
        console.error('Tamanho da pasta grande inválido. Use um número inteiro positivo.')
        process.exit(1)
      }
    }
    if (args[i] === '--quantidade' && args[i + 1]) {
      quantidade = parseInt(args[++i], 10)
      if (Number.isNaN(quantidade) || quantidade < 1) {
        console.error('Quantidade inválida. Use um número inteiro positivo.')
        process.exit(1)
      }
    }
  }

  return { quantidade, limpar, pastaGrande }
}

function escalarRamos(quantidadeAlvo) {
  const totalPadrao = RAMOS_PADRAO.reduce(
    (s, r) => s + r.setores * r.pastasPorSetor * r.enviosPorPasta,
    0,
  )
  const fator = quantidadeAlvo / totalPadrao

  return RAMOS_PADRAO.map((ramo) => {
    const enviosRamo = Math.round(
      ramo.setores * ramo.pastasPorSetor * ramo.enviosPorPasta * fator,
    )
    const enviosPorPasta = Math.max(1, Math.round(ramo.enviosPorPasta * fator))
    const pastasPorSetor = Math.max(1, Math.round(ramo.pastasPorSetor * Math.sqrt(fator)))
    const setores = Math.max(1, Math.ceil(enviosRamo / (pastasPorSetor * enviosPorPasta)))

    return {
      idPastaPai: ramo.idPastaPai,
      setores,
      pastasPorSetor,
      enviosPorPasta,
    }
  })
}

function criarPdf(destino) {
  try {
    fs.linkSync(PDF_ORIGEM, destino)
  } catch {
    fs.copyFileSync(PDF_ORIGEM, destino)
  }
}

async function limparVolume(client) {
  console.log('Limpando dados de volume existentes...')

  const { rows: pastasVolume } = await client.query(
    `SELECT idpasta FROM pasta WHERE nomepasta = 'Volume' AND idconta = $1`,
    [ID_CONTA],
  )

  if (pastasVolume.length === 0) {
    console.log('Nenhum dado de volume encontrado no banco.')
  } else {
    for (const { idpasta } of pastasVolume) {
      await client.query(
        `WITH RECURSIVE subarvore AS (
           SELECT idpasta FROM pasta WHERE idpasta = $1
           UNION ALL
           SELECT filho.idpasta FROM pasta filho
           INNER JOIN subarvore s ON filho.idpastapai = s.idpasta
         )
         DELETE FROM destinatario WHERE idenvio IN (
           SELECT idenvio FROM envio WHERE idpasta IN (SELECT idpasta FROM subarvore)
         )`,
        [idpasta],
      )

      await client.query(
        `WITH RECURSIVE subarvore AS (
           SELECT idpasta FROM pasta WHERE idpasta = $1
           UNION ALL
           SELECT filho.idpasta FROM pasta filho
           INNER JOIN subarvore s ON filho.idpastapai = s.idpasta
         )
         DELETE FROM envio WHERE idpasta IN (SELECT idpasta FROM subarvore)`,
        [idpasta],
      )

      await client.query(
        `WITH RECURSIVE subarvore AS (
           SELECT idpasta FROM pasta WHERE idpasta = $1
           UNION ALL
           SELECT filho.idpasta FROM pasta filho
           INNER JOIN subarvore s ON filho.idpastapai = s.idpasta
         )
         DELETE FROM pasta WHERE idpasta IN (
           SELECT idpasta FROM subarvore WHERE idpasta <> $1
         )`,
        [idpasta],
      )

      await client.query(`DELETE FROM pasta WHERE idpasta = $1`, [idpasta])
    }

    console.log(`Removidas ${pastasVolume.length} pasta(s) Volume e subárvores.`)
  }

  if (fs.existsSync(PDF_DESTINO_DIR)) {
    const arquivos = fs.readdirSync(PDF_DESTINO_DIR)
    for (const arq of arquivos) {
      fs.unlinkSync(path.join(PDF_DESTINO_DIR, arq))
    }
    console.log(`Removidos ${arquivos.length} PDF(s) de ${PDF_DESTINO_DIR}`)
  }
}

function gerarAssunto(indice) {
  const tipo = TIPOS_ASSUNTO[indice % TIPOS_ASSUNTO.length]
  return `${tipo} Vol-${String(indice).padStart(5, '0')}`
}

function gerarDataCriacao(indice) {
  const base = new Date('2024-01-01')
  base.setDate(base.getDate() + (indice % 730))
  return base.toISOString().slice(0, 10)
}

function novoEnvio(idpasta, indice) {
  const status = STATUS[indice % STATUS.length]
  const datacriacao = gerarDataCriacao(indice)
  const dataexpiracao =
    status === 'Pendente'
      ? new Date(datacriacao).toISOString().slice(0, 10).replace(/-01$/, '-02')
      : null

  return {
    idpasta,
    externalkey: randomUUID(),
    assunto: gerarAssunto(indice),
    statusenvio: status,
    datacriacao,
    dataexpiracao,
    nomeDest: `Destinatario Vol-${String(indice).padStart(5, '0')}`,
    emailDest: `vol${indice}@demo.com`,
  }
}

async function inserirLoteEnvios(client, envios) {
  if (envios.length === 0) return

  const valores = []
  const params = []
  let p = 1

  for (const envio of envios) {
    valores.push(
      `($${p}, $${p + 1}, $${p + 2}, $${p + 3}, $${p + 4}, $${p + 5}, $${p + 6}, 0)`,
    )
    params.push(
      envio.idpasta,
      ID_CONTA,
      envio.externalkey,
      envio.assunto,
      envio.statusenvio,
      envio.datacriacao,
      envio.dataexpiracao,
    )
    p += 7
  }

  const sql = `
    INSERT INTO envio (idpasta, idconta, externalkey, assunto, statusenvio, datacriacao, dataexpiracao, statusregistro)
    VALUES ${valores.join(', ')}
    RETURNING idenvio, externalkey
  `

  const { rows } = await client.query(sql, params)
  return rows
}

async function inserirLoteDestinatarios(client, destinatarios) {
  if (destinatarios.length === 0) return

  const valores = []
  const params = []
  let p = 1

  for (const dest of destinatarios) {
    valores.push(`($${p}, $${p + 1}, $${p + 2})`)
    params.push(dest.idenvio, dest.nome, dest.email)
    p += 3
  }

  await client.query(
    `INSERT INTO destinatario (idenvio, nome, email) VALUES ${valores.join(', ')}`,
    params,
  )
}

async function gerarVolume(client, ramos, pastaGrande) {
  if (!fs.existsSync(PDF_ORIGEM)) {
    throw new Error(`PDF de origem não encontrado: ${PDF_ORIGEM}`)
  }

  fs.mkdirSync(PDF_DESTINO_DIR, { recursive: true })

  const inicio = Date.now()
  let totalEnvios = 0
  let totalPastas = 0
  let indiceGlobal = 1

  const loteEnvios = []
  let idPrimeiroVolume = null

  for (const ramo of ramos) {
    const { rows: [pastaPai] } = await client.query(
      `SELECT idpasta, nomepasta FROM pasta WHERE idpasta = $1`,
      [ramo.idPastaPai],
    )

    if (!pastaPai) {
      throw new Error(`Pasta pai id=${ramo.idPastaPai} não encontrada. Rode o seed primeiro.`)
    }

    const { rows: [pastaVolume] } = await client.query(
      `INSERT INTO pasta (idpastapai, externalkey, nomepasta, idconta, statusregistro)
       VALUES ($1, $2, 'Volume', $3, 0)
       RETURNING idpasta`,
      [ramo.idPastaPai, randomUUID(), ID_CONTA],
    )
    totalPastas++
    idPrimeiroVolume ??= pastaVolume.idpasta

    console.log(`\nRamo: ${pastaPai.nomepasta} > Volume (idpasta=${pastaVolume.idpasta})`)

    for (let s = 1; s <= ramo.setores; s++) {
      const nomeSetor = `Setor_${String(s).padStart(3, '0')}`
      const { rows: [setor] } = await client.query(
        `INSERT INTO pasta (idpastapai, externalkey, nomepasta, idconta, statusregistro)
         VALUES ($1, $2, $3, $4, 0)
         RETURNING idpasta`,
        [pastaVolume.idpasta, randomUUID(), nomeSetor, ID_CONTA],
      )
      totalPastas++

      for (let p = 1; p <= ramo.pastasPorSetor; p++) {
        const nomePasta = `Pasta_${String(p).padStart(3, '0')}`
        const { rows: [pasta] } = await client.query(
          `INSERT INTO pasta (idpastapai, externalkey, nomepasta, idconta, statusregistro)
           VALUES ($1, $2, $3, $4, 0)
           RETURNING idpasta`,
          [setor.idpasta, randomUUID(), nomePasta, ID_CONTA],
        )
        totalPastas++

        for (let e = 1; e <= ramo.enviosPorPasta; e++) {
          loteEnvios.push(novoEnvio(pasta.idpasta, indiceGlobal))

          indiceGlobal++
          totalEnvios++

          if (loteEnvios.length >= LOTE) {
            await flushLote(client, loteEnvios, totalEnvios)
          }
        }
      }
    }
  }

  if (pastaGrande > 0) {
    const { rows: [arquivo] } = await client.query(
      `INSERT INTO pasta (idpastapai, externalkey, nomepasta, idconta, statusregistro)
       VALUES ($1, $2, 'Arquivo_Geral', $3, 0)
       RETURNING idpasta`,
      [idPrimeiroVolume, randomUUID(), ID_CONTA],
    )
    totalPastas++
    console.log(`\nPasta grande: Volume > Arquivo_Geral com ${pastaGrande} envios`)

    for (let e = 1; e <= pastaGrande; e++) {
      loteEnvios.push(novoEnvio(arquivo.idpasta, indiceGlobal))
      indiceGlobal++
      totalEnvios++

      if (loteEnvios.length >= LOTE) {
        await flushLote(client, loteEnvios, totalEnvios)
      }
    }
  }

  if (loteEnvios.length > 0) {
    await flushLote(client, loteEnvios, totalEnvios)
  }

  const duracao = ((Date.now() - inicio) / 1000).toFixed(1)
  const tamanhoDisco = calcularTamanhoDir(PDF_DESTINO_DIR)

  console.log('\n--- Resumo ---')
  console.log(`Pastas criadas:  ${totalPastas}`)
  console.log(`Envios criados:  ${totalEnvios}`)
  console.log(`PDFs gerados:    ${totalEnvios}`)
  console.log(`Tempo:           ${duracao}s`)
  console.log(`Disco (volume/): ${tamanhoDisco}`)
}

async function flushLote(client, loteEnvios, totalAcumulado) {
  const inseridos = await inserirLoteEnvios(client, loteEnvios)
  const destinatarios = inseridos.map((row, i) => ({
    idenvio: row.idenvio,
    nome: loteEnvios[i].nomeDest,
    email: loteEnvios[i].emailDest,
  }))
  await inserirLoteDestinatarios(client, destinatarios)

  for (const envio of loteEnvios) {
    const destino = path.join(PDF_DESTINO_DIR, `${envio.externalkey}.pdf`)
    criarPdf(destino)
  }

  console.log(`  ... ${totalAcumulado} envios processados`)
  loteEnvios.length = 0
}

function calcularTamanhoDir(dir) {
  if (!fs.existsSync(dir)) return '0 B'
  let bytes = 0
  for (const arq of fs.readdirSync(dir)) {
    bytes += fs.statSync(path.join(dir, arq)).size
  }
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

async function volumeJaExiste(client) {
  const { rows } = await client.query(
    `SELECT COUNT(*)::int AS total FROM pasta WHERE nomepasta = 'Volume' AND idconta = $1`,
    [ID_CONTA],
  )
  return rows[0].total > 0
}

async function main() {
  const { quantidade, limpar, pastaGrande } = parseArgs()
  const ramos = escalarRamos(quantidade)

  const enviosPrevistos = ramos.reduce(
    (s, r) => s + r.setores * r.pastasPorSetor * r.enviosPorPasta,
    0,
  )

  console.log(`Gerando ~${enviosPrevistos} envios (alvo: ${quantidade})...`)

  const client = new pg.Client({
    host: 'localhost',
    port: 15432,
    database: 'busca',
    user: 'busca',
    password: 'busca',
  })

  await client.connect()

  try {
    if (limpar) {
      await client.query('BEGIN')
      await limparVolume(client)
      await client.query('COMMIT')
    }

    if (!limpar && (await volumeJaExiste(client))) {
      console.error(
        'Já existem pastas "Volume" no banco. Use --limpar para remover e regerar.',
      )
      process.exit(1)
    }

    await client.query('BEGIN')
    await gerarVolume(client, ramos, pastaGrande)
    await client.query('COMMIT')
    console.log('\nVolume gerado com sucesso.')
  } catch (err) {
    await client.query('ROLLBACK')
    console.error('Erro:', err.message)
    process.exit(1)
  } finally {
    await client.end()
  }
}

main()
