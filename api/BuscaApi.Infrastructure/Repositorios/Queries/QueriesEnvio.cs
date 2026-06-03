namespace BuscaApi.Infrastructure.Repositorios.Queries;

public static class QueriesEnvio
{
    // Busca recursiva de pastas + envios respeitando permissoes (rlcusuariopasta).
    //
    // - PastasPermitidas: CTE recursiva que resolve a visibilidade do usuario.
    //   Quando @idUsuarioFiltro IS NULL (admin), libera todas as pastas da conta.
    //   Caso contrario, comeca pelas pastas vinculadas via rlcusuariopasta e desce
    //   para todas as subpastas.
    // - Subarvore: expande a subarvore a partir da raiz (@idPastaRaiz) ou da conta,
    //   montando o caminho legivel (ex.: "Clientes > 2026 > ACME").
    // - Pastas / Envios: aplicam o filtro textual (ILIKE) sobre a subarvore.
    public const string QueryBuscaArquivos = @"
WITH RECURSIVE PastasPermitidas AS (
    -- Usuario admin (@idUsuarioFiltro IS NULL): todas as pastas da conta
    SELECT p.idpasta
    FROM pasta p
    WHERE p.idconta = @idConta
      AND @idUsuarioFiltro::int IS NULL

    UNION

    -- Usuario nao-admin: comeca pelas pastas vinculadas a ele
    SELECT p.idpasta
    FROM rlcusuariopasta rup
    INNER JOIN pasta p ON rup.idpasta = p.idpasta
    WHERE rup.idusuario = @idUsuarioFiltro

    UNION

    -- E desce: filhos das pastas permitidas tambem sao permitidos
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
        s.nomepasta AS nome,
        s.caminho,
        (SELECT pp.externalkey FROM pasta pp WHERE pp.idpasta = s.idpastapai) AS externalkeypastapai,
        NULL::TEXT AS statusenvio,
        NULL::TIMESTAMP AS datacriacao,
        NULL::TIMESTAMP AS dataexpiracao,
        NULL::TEXT AS nomedestinatarios
    FROM Subarvore s
    WHERE @incluirPastas = TRUE
      AND s.nomepasta ILIKE '%' || @termo || '%'
      AND (@idPastaRaiz::int IS NULL OR s.idpasta <> @idPastaRaiz)
),
Envios AS (
    SELECT
        e.externalkey,
        'Envio'::TEXT AS tipo,
        e.assunto AS nome,
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
),
Resultado AS (
    SELECT * FROM Pastas
    UNION ALL
    SELECT * FROM Envios
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
FROM Resultado
ORDER BY
    CASE WHEN LOWER(nome) = LOWER(@termo) THEN 0
         WHEN LOWER(nome) LIKE LOWER(@termo) || '%' THEN 1
         ELSE 2 END,
    tipo,
    nome
LIMIT @tamanhoPagina OFFSET @offset;";
}
