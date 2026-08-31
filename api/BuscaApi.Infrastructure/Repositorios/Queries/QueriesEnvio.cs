namespace BuscaApi.Infrastructure.Repositorios.Queries;

public static class QueriesEnvio
{
    // Versao ingenua: carrega TODA a arvore da conta (pastas + envios + destinatarios)
    // e o vinculo de permissao. A filtragem de nivel e a busca textual acontecem
    // em memoria no EnvioRepository — sem WITH RECURSIVE e sem ILIKE no banco.

    public const string QueryPastasDaConta = @"
SELECT
    p.idpasta        AS IdPasta,
    p.idpastapai     AS IdPastaPai,
    p.externalkey    AS ExternalKey,
    p.nomepasta      AS NomePasta
FROM pasta p
WHERE p.idconta = @idConta
  AND p.statusregistro = 0;";

    public const string QueryEnviosDaConta = @"
SELECT
    e.idenvio        AS IdEnvio,
    e.idpasta        AS IdPasta,
    e.externalkey    AS ExternalKey,
    e.assunto        AS Assunto,
    e.statusenvio    AS StatusEnvio,
    e.datacriacao    AS DataCriacao,
    e.dataexpiracao  AS DataExpiracao,
    COALESCE(DEST.nomes_destinatarios, '') AS NomeDestinatarios
FROM envio e
LEFT JOIN LATERAL (
    SELECT STRING_AGG(d.nome, ', ') AS nomes_destinatarios
    FROM destinatario d
    WHERE d.idenvio = e.idenvio
      AND d.email IS NOT NULL
      AND d.email <> ''
) DEST ON true
WHERE e.idconta = @idConta
  AND e.statusregistro = 0;";

    public const string QueryPastasVinculadas = @"
SELECT rup.idpasta AS IdPasta
FROM rlcusuariopasta rup
WHERE rup.idusuario = @idUsuario;";
}
