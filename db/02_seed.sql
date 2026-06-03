-- ============================================================
-- Dados de exemplo para praticar a busca recursiva
--
-- Conta:    Empresa Demo (externalkey fixo, usado pelo frontend)
-- Usuarios: Ana (admin)  -> ve tudo
--           Maria        -> nao-admin, so enxerga o ramo "Financeiro"
--
-- Arvore de pastas:
--   Clientes
--     2026
--       ACME
--       Globex
--     2025
--       Initech
--   Financeiro
--     Contratos
--     Notas Fiscais
--   RH
--     Admissoes
-- ============================================================

-- ---------- Conta ----------
INSERT INTO conta (idconta, externalkey, nome) VALUES
    (1, '11111111-1111-1111-1111-111111111111', 'Empresa Demo');

-- ---------- Usuarios ----------
INSERT INTO usuario (idusuario, authkey, nome, idconta, admin) VALUES
    (1, 'a0000000-0000-0000-0000-000000000001', 'Ana (admin)',   1, TRUE),
    (2, 'a0000000-0000-0000-0000-000000000002', 'Maria (Financeiro)', 1, FALSE);

-- ---------- Pastas ----------
INSERT INTO pasta (idpasta, idpastapai, externalkey, nomepasta, idconta, statusregistro) VALUES
    (1,  NULL, gen_random_uuid(), 'Clientes',      1, 0),
    (2,  1,    gen_random_uuid(), '2026',          1, 0),
    (3,  2,    gen_random_uuid(), 'ACME',          1, 0),
    (4,  2,    gen_random_uuid(), 'Globex',        1, 0),
    (5,  1,    gen_random_uuid(), '2025',          1, 0),
    (6,  5,    gen_random_uuid(), 'Initech',       1, 0),
    (7,  NULL, gen_random_uuid(), 'Financeiro',    1, 0),
    (8,  7,    gen_random_uuid(), 'Contratos',     1, 0),
    (9,  7,    gen_random_uuid(), 'Notas Fiscais', 1, 0),
    (10, NULL, gen_random_uuid(), 'RH',            1, 0),
    (11, 10,   gen_random_uuid(), 'Admissoes',     1, 0);

-- ---------- Envios (em subpastas profundas) ----------
INSERT INTO envio (idenvio, idpasta, idconta, externalkey, assunto, statusenvio, datacriacao, dataexpiracao, statusregistro) VALUES
    (1, 3,  1, gen_random_uuid(), 'Contrato ACME 2026',          'Pendente',  '2026-05-20', '2026-06-20', 0),
    (2, 3,  1, gen_random_uuid(), 'Aditivo Contrato ACME',       'Assinado',  '2026-04-10', NULL,         0),
    (3, 4,  1, gen_random_uuid(), 'Proposta Comercial Globex',   'Pendente',  '2026-05-22', '2026-06-22', 0),
    (4, 6,  1, gen_random_uuid(), 'Contrato Initech 2025',       'Assinado',  '2025-11-15', NULL,         0),
    (5, 8,  1, gen_random_uuid(), 'Contrato de Locacao',         'Pendente',  '2026-05-01', '2026-06-01', 0),
    (6, 8,  1, gen_random_uuid(), 'Contrato de Prestacao de Servico', 'Pendente', '2026-05-05', '2026-06-05', 0),
    (7, 9,  1, gen_random_uuid(), 'Nota Fiscal 001 - Maio',      'Assinado',  '2026-05-31', NULL,         0),
    (8, 11, 1, gen_random_uuid(), 'Contrato de Admissao Joao',   'Pendente',  '2026-05-18', '2026-06-18', 0);

-- ---------- Destinatarios ----------
INSERT INTO destinatario (idenvio, nome, email) VALUES
    (1, 'Joao Silva',     'joao@acme.com'),
    (1, 'Maria Souza',    'maria@acme.com'),
    (2, 'Joao Silva',     'joao@acme.com'),
    (3, 'Carlos Globex',  'carlos@globex.com'),
    (4, 'Peter Initech',  'peter@initech.com'),
    (5, 'Locatario Teste','locatario@demo.com'),
    (6, 'Fornecedor X',   'contato@fornecedorx.com'),
    (7, 'Financeiro',     'fiscal@empresa-demo.com'),
    (8, 'Joao Admissao',  'joao.rh@empresa-demo.com');

-- ---------- Permissoes (rlcusuariopasta) ----------
-- Maria (idusuario=2) so tem acesso ao ramo Financeiro (idpasta=7).
-- A CTE recursiva da busca expande para as subpastas (Contratos, Notas Fiscais).
INSERT INTO rlcusuariopasta (idusuario, idpasta) VALUES
    (2, 7);

-- ---------- Reset das sequences (ids foram inseridos explicitamente) ----------
SELECT setval('conta_idconta_seq',        (SELECT MAX(idconta)        FROM conta));
SELECT setval('usuario_idusuario_seq',    (SELECT MAX(idusuario)      FROM usuario));
SELECT setval('pasta_idpasta_seq',        (SELECT MAX(idpasta)        FROM pasta));
SELECT setval('envio_idenvio_seq',        (SELECT MAX(idenvio)        FROM envio));
SELECT setval('destinatario_iddestinatario_seq', (SELECT MAX(iddestinatario) FROM destinatario));
