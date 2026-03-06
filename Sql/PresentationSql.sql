SET FOREIGN_KEY_CHECKS = 0;

TRUNCATE TABLE Distribuicoes;
TRUNCATE TABLE OrdensCompra;
TRUNCATE TABLE Custodias;
TRUNCATE TABLE EventosIR;
TRUNCATE TABLE Rebalanceamentos;
truncate table Cotacoes;

SET FOREIGN_KEY_CHECKS = 1;

select * from Usuarios;
select * from ContasGraficas;
select * from Clientes;

select * from Cotacoes;
select * from OrdensCompra;

UPDATE Cotacoes
SET PrecoFechamento = 46.50
WHERE Id = 4;

select * from CestasRecomendacao;
select * from ItensCesta;

select * from Distribuicoes;
select * from EventosIR;

select * from Rebalanceamentos;