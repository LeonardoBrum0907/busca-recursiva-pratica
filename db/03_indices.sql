-- ============================================================
-- Indices da busca recursiva
--
-- Sem eles, o ILIKE '%termo%' vira sequential scan: o % no inicio impede
-- qualquer B-tree comum de ser usada. O pg_trgm resolve isso quebrando o texto
-- em trigramas e indexando-os num GIN.
-- ============================================================

CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- Acelera ILIKE '%termo%' no assunto do envio e no nome da pasta
CREATE INDEX IF NOT EXISTS ix_envio_assunto_trgm
    ON envio USING gin (assunto gin_trgm_ops);

CREATE INDEX IF NOT EXISTS ix_pasta_nomepasta_trgm
    ON pasta USING gin (nomepasta gin_trgm_ops);

-- Acelera a descida pai -> filhos das CTEs PastasPermitidas e Subarvore
CREATE INDEX IF NOT EXISTS ix_pasta_idpastapai
    ON pasta (idpastapai);

-- Acelera a etapa "envios de uma subarvore"
CREATE INDEX IF NOT EXISTS ix_envio_idpasta_status
    ON envio (idpasta, statusregistro);

-- A lista de destinatarios de cada envio (LEFT JOIN LATERAL em /itens e /buscar)
-- filtra destinatario por idenvio. Sem este indice, cada envio varre a tabela inteira.
CREATE INDEX IF NOT EXISTS ix_destinatario_idenvio
    ON destinatario (idenvio);
