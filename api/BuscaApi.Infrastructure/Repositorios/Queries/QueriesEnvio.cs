namespace BuscaApi.Infrastructure.Repositorios.Queries;

public static class QueriesEnvio
{
    /// <summary>
    /// Lista o conteudo de UM nivel da arvore: as pastas filhas diretas da pasta aberta
    /// (ou as pastas-raiz da conta) e os envios que estao nessa pasta.
    ///
    /// Nao existe parametro de termo: a API nao sabe o que o usuario digitou. Quem filtra
    /// e o frontend, em memoria, sobre o que esta query devolveu.
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

    public const string QueryContarArquivos = @"
SELECT COUNT(*)::int
FROM envio
WHERE idconta = @idConta
  AND statusregistro = 0;";
}
