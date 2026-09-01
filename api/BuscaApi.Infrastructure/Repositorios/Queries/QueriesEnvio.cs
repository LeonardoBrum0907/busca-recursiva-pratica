namespace BuscaApi.Infrastructure.Repositorios.Queries;

public static class QueriesEnvio
{
    /// <summary>
    /// Lista o conteudo de UM nivel da arvore: as pastas filhas diretas da pasta aberta
    /// (ou as pastas-raiz da conta) e os envios que estao nessa pasta. Usada pela
    /// navegacao — a tela inicial nao pode despejar milhares de itens.
    ///
    /// A CTE PastasPermitidas resolve visibilidade: um usuario nao-admin enxerga as pastas
    /// vinculadas em rlcusuariopasta e todas as descendentes delas.
    /// </summary>
    public const string QueryItensDoNivel = @"
WITH RECURSIVE PastasPermitidas AS (
    SELECT p.idpasta
    FROM pasta p
    WHERE p.idconta = @idConta
      AND @idUsuarioFiltro::int IS NULL

    UNION

    SELECT p.idpasta
    FROM rlcusuariopasta rup
    INNER JOIN pasta p ON rup.idpasta = p.idpasta
    WHERE rup.idusuario = @idUsuarioFiltro

    UNION

    SELECT filho.idpasta
    FROM pasta filho
    INNER JOIN PastasPermitidas pp ON filho.idpastapai = pp.idpasta
),
Pastas AS (
    SELECT
        p.externalkey,
        'Pasta'::TEXT      AS tipo,
        p.nomepasta        AS nome,
        NULL::TEXT         AS statusenvio,
        NULL::TIMESTAMP    AS datacriacao,
        NULL::TIMESTAMP    AS dataexpiracao,
        NULL::TEXT         AS nomedestinatarios
    FROM pasta p
    INNER JOIN PastasPermitidas pp ON p.idpasta = pp.idpasta
    WHERE p.idconta = @idConta
      AND p.statusregistro = 0
      AND (
          (@idPasta::int IS NULL AND p.idpastapai IS NULL)
          OR p.idpastapai = @idPasta
      )
),
Envios AS (
    SELECT
        e.externalkey,
        'Envio'::TEXT AS tipo,
        e.assunto     AS nome,
        e.statusenvio,
        e.datacriacao,
        e.dataexpiracao,
        COALESCE(DEST.nomes_destinatarios, '') AS nomedestinatarios
    FROM envio e
    INNER JOIN PastasPermitidas pp ON e.idpasta = pp.idpasta
    LEFT JOIN LATERAL (
        SELECT STRING_AGG(d.nome, ', ') AS nomes_destinatarios
        FROM destinatario d
        WHERE d.idenvio = e.idenvio
          AND d.email IS NOT NULL
          AND d.email <> ''
    ) DEST ON true
    WHERE e.idconta = @idConta
      AND e.statusregistro = 0
      AND @idPasta::int IS NOT NULL
      AND e.idpasta = @idPasta
)
SELECT
    externalkey       AS ExternalKey,
    tipo              AS Tipo,
    nome              AS Nome,
    statusenvio       AS StatusEnvio,
    datacriacao       AS DataCriacao,
    dataexpiracao     AS DataExpiracao,
    nomedestinatarios AS NomeDestinatarios
FROM (
    SELECT * FROM Pastas
    UNION ALL
    SELECT * FROM Envios
) Resultado
ORDER BY
    CASE WHEN tipo = 'Pasta' THEN 0 ELSE 1 END,
    nome;";

    /// <summary>
    /// Busca recursiva: dado um termo e uma pasta de partida (ou a raiz da conta),
    /// devolve pastas e envios da subarvore INTEIRA cujo nome contem o termo.
    ///
    /// Tres CTEs, nesta ordem:
    ///
    /// 1. PastasPermitidas — a mesma da navegacao. Entra ANTES da expansao, para que a
    ///    subarvore nunca chegue a enxergar uma pasta sem permissao.
    /// 2. Subarvore — desce da pasta de partida ate as folhas, concatenando o caminho
    ///    legivel ("Clientes > 2026 > ACME") ao longo da descida.
    /// 3. Pastas / Envios — aplicam o ILIKE e se unem num resultado paginado.
    ///
    /// O total vem de COUNT(*) OVER (), evitando uma segunda ida ao banco.
    /// </summary>
    public const string QueryBuscaArquivos = @"
WITH RECURSIVE PastasPermitidas AS (
    SELECT p.idpasta
    FROM pasta p
    WHERE p.idconta = @idConta
      AND @idUsuarioFiltro::int IS NULL

    UNION

    SELECT p.idpasta
    FROM rlcusuariopasta rup
    INNER JOIN pasta p ON rup.idpasta = p.idpasta
    WHERE rup.idusuario = @idUsuarioFiltro

    UNION

    SELECT filho.idpasta
    FROM pasta filho
    INNER JOIN PastasPermitidas pp ON filho.idpastapai = pp.idpasta
),
Subarvore AS (
    SELECT
        p.idpasta,
        p.idpastapai,
        p.externalkey,
        p.nomepasta,
        p.nomepasta::TEXT AS caminho
    FROM pasta p
    INNER JOIN PastasPermitidas pp ON p.idpasta = pp.idpasta
    WHERE p.idconta = @idConta
      AND p.statusregistro = 0
      AND (
          (@idPastaRaiz::int IS NULL AND p.idpastapai IS NULL)
          OR p.idpasta = @idPastaRaiz
      )

    UNION ALL

    SELECT
        filho.idpasta,
        filho.idpastapai,
        filho.externalkey,
        filho.nomepasta,
        (s.caminho || ' > ' || filho.nomepasta)::TEXT
    FROM pasta filho
    INNER JOIN Subarvore s ON filho.idpastapai = s.idpasta
    INNER JOIN PastasPermitidas pp ON filho.idpasta = pp.idpasta
    WHERE filho.statusregistro = 0
),
Pastas AS (
    SELECT
        s.externalkey,
        'Pasta'::TEXT AS tipo,
        s.nomepasta   AS nome,
        s.caminho,
        (SELECT pai.externalkey FROM pasta pai WHERE pai.idpasta = s.idpastapai) AS externalkeypastapai,
        NULL::TEXT      AS statusenvio,
        NULL::TIMESTAMP AS datacriacao,
        NULL::TIMESTAMP AS dataexpiracao,
        NULL::TEXT      AS nomedestinatarios
    FROM Subarvore s
    WHERE @incluirPastas = TRUE
      AND s.nomepasta ILIKE '%' || @termo || '%'
      -- a propria pasta de partida nao e resultado da busca feita dentro dela
      AND (@idPastaRaiz::int IS NULL OR s.idpasta <> @idPastaRaiz)
),
Envios AS (
    SELECT
        e.externalkey,
        'Envio'::TEXT AS tipo,
        e.assunto     AS nome,
        s.caminho,
        s.externalkey AS externalkeypastapai,
        e.statusenvio,
        e.datacriacao,
        e.dataexpiracao,
        COALESCE(DEST.nomes_destinatarios, '') AS nomedestinatarios
    FROM envio e
    INNER JOIN Subarvore s ON e.idpasta = s.idpasta
    LEFT JOIN LATERAL (
        SELECT STRING_AGG(d.nome, ', ') AS nomes_destinatarios
        FROM destinatario d
        WHERE d.idenvio = e.idenvio
          AND d.email IS NOT NULL
          AND d.email <> ''
    ) DEST ON true
    WHERE e.statusregistro = 0
      AND e.assunto ILIKE '%' || @termo || '%'
)
SELECT
    externalkey         AS ExternalKey,
    tipo                AS Tipo,
    nome                AS Nome,
    caminho             AS Caminho,
    externalkeypastapai AS ExternalKeyPastaPai,
    statusenvio         AS StatusEnvio,
    datacriacao         AS DataCriacao,
    dataexpiracao       AS DataExpiracao,
    nomedestinatarios   AS NomeDestinatarios,
    COUNT(*) OVER ()    AS Total
FROM (
    SELECT * FROM Pastas
    UNION ALL
    SELECT * FROM Envios
) Resultado
ORDER BY
    -- relevancia: match exato, depois prefixo, depois o resto
    CASE
        WHEN LOWER(nome) = LOWER(@termo) THEN 0
        WHEN LOWER(nome) LIKE LOWER(@termo) || '%' THEN 1
        ELSE 2
    END,
    caminho,
    tipo,
    nome
LIMIT @tamanhoPagina OFFSET @offset;";
}
