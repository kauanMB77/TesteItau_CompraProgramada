INSERT INTO Clientes (
    Nome,
    CPF,
    Email,
    ValorMensal,
    Ativo,
    DataAdesao
) VALUES
('Joao Almeida', '12345678901', 'joao@email.com', 40000.00, TRUE, NOW()),
('Maria Eduarda', '98765432100', 'mariaeduarda@email.com', 5000.00, TRUE, NOW()),
('Kauan Bortoloto', '11122233344', 'kauanmb@hotmail.com', 4000.00, TRUE, NOW()),
('Administrador', '41782495401', 'admacc@email.com', 0.00, TRUE, NOW());

INSERT INTO ContasGraficas (
    ClienteId,
    NumeroConta,
    Tipo,
    DataCriacao
) VALUES
(1, 'CG10001', 'Filhote', NOW()),
(2, 'CG10002', 'Filhote', NOW()),
(3, 'CG10003', 'Filhote', NOW()),
(4, 'CG10004', 'Master', NOW());

INSERT INTO Usuarios (
    ClienteId,
    Email,
    Senha,
    Tipo
) VALUES
(1, 'joao@email.com', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 'CLIENTE'),
(2, 'mariaeduarda@email.com', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 'CLIENTE'),
(3, 'kauanmb@hotmail.com', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 'CLIENTE'),
(4, 'admacc@email.com', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 'ADMINISTRADOR');

INSERT INTO CestasRecomendacao (
    Nome,
    Ativa,
    DataCriacao,
    DataDesativacao
) VALUES
('Cesta060326', TRUE, NOW(), NULL);

INSERT INTO ItensCesta (
    CestaId,
    Ticker,
    Percentual
) VALUES
-- Cesta Dividendos
(1, 'ITUB4', 10.00),
(1, 'BBAS3', 15.00),
(1, 'VALE3', 15.00),
(1, 'BPAC11', 30.00),
(1, 'ABEV3', 30.00);