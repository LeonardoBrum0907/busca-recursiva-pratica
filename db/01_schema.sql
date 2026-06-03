-- ============================================================
-- Schema simplificado para praticar busca recursiva (PostgreSQL)
-- Espelha o dominio do ArecoSign: Conta > Pasta (hierarquica) > Envio
-- ============================================================

-- Habilita similaridade textual (necessario para ILIKE performante)
CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE TABLE conta (
    idconta     SERIAL PRIMARY KEY,
    externalkey UUID NOT NULL UNIQUE,
    nome        TEXT NOT NULL
);

CREATE TABLE usuario (
    idusuario SERIAL PRIMARY KEY,
    authkey   UUID NOT NULL UNIQUE,
    nome      TEXT NOT NULL,
    idconta   INT  NOT NULL REFERENCES conta (idconta),
    admin     BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE pasta (
    idpasta        SERIAL PRIMARY KEY,
    idpastapai     INT  NULL REFERENCES pasta (idpasta),
    externalkey    UUID NOT NULL UNIQUE,
    nomepasta      TEXT NOT NULL,
    idconta        INT  NOT NULL REFERENCES conta (idconta),
    statusregistro INT  NOT NULL DEFAULT 0
);

CREATE TABLE envio (
    idenvio        SERIAL PRIMARY KEY,
    idpasta        INT  NOT NULL REFERENCES pasta (idpasta),
    idconta        INT  NOT NULL REFERENCES conta (idconta),
    externalkey    UUID NOT NULL UNIQUE,
    assunto        TEXT NOT NULL,
    statusenvio    TEXT NULL,
    datacriacao    TIMESTAMP NULL,
    dataexpiracao  TIMESTAMP NULL,
    statusregistro INT  NOT NULL DEFAULT 0
);

CREATE TABLE destinatario (
    iddestinatario SERIAL PRIMARY KEY,
    idenvio        INT  NOT NULL REFERENCES envio (idenvio),
    nome           TEXT NOT NULL,
    email          TEXT NULL
);

-- Vinculo usuario <-> pasta (visibilidade para usuarios nao-admin)
CREATE TABLE rlcusuariopasta (
    idusuario INT NOT NULL REFERENCES usuario (idusuario),
    idpasta   INT NOT NULL REFERENCES pasta (idpasta),
    PRIMARY KEY (idusuario, idpasta)
);

-- ============================================================
-- Indices (Task 10 do plano)
-- ============================================================

-- Indices trigram para acelerar ILIKE '%termo%'
CREATE INDEX IF NOT EXISTS ix_envio_assunto_trgm
    ON envio USING gin (assunto gin_trgm_ops);

CREATE INDEX IF NOT EXISTS ix_pasta_nomepasta_trgm
    ON pasta USING gin (nomepasta gin_trgm_ops);

-- Acelera a recursao pai -> filhos
CREATE INDEX IF NOT EXISTS ix_pasta_idpastapai
    ON pasta (idpastapai);

-- Acelera a etapa "envios de uma subarvore"
CREATE INDEX IF NOT EXISTS ix_envio_idpasta_status
    ON envio (idpasta, statusregistro);
