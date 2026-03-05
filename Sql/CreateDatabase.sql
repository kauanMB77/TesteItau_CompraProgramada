CREATE TABLE Clientes (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(200) NOT NULL,
    CPF VARCHAR(11) NOT NULL UNIQUE,
    Email VARCHAR(200),
    ValorMensal DECIMAL(18,2),
    Ativo BOOLEAN DEFAULT TRUE,
    DataAdesao DATETIME
);

CREATE TABLE ContasGraficas(
	Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    ClienteId BIGINT NOT NULL UNIQUE,
    NumeroConta VARCHAR(20) NOT NULL UNIQUE,
    Tipo Enum('Master','Filhote') NOT NULL,
    DataCriacao DATETIME,
    
    CONSTRAINT FK_ContasGraficas_Clientes
    FOREIGN Key (ClienteID)
    References Clientes(Id)
);

CREATE TABLE Custodias(
	Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    ContaGraficaId BIGINT NOT NULL,
    Ticker VARCHAR (10) NOT NULL,
    Quantidade INT,
    PrecoMedio DECIMAL (18,4),
    DataUltimaAtualizacao DATETIME,
    
    CONSTRAINT FK_Custodias_ContasGraficas
    Foreign Key (ContaGraficaID)
    References ContasGraficas(Id)
);

CREATE TABLE OrdensCompra(
	Id BIGINT AUTO_INCREMENT PRIMARY KEY,
	ContaMasterId BIGINT NOT NULL,
    Ticker VARCHAR (10) NOT NULL,
    Quantidade INT NOT NULL,
    PrecoUnitario DECIMAL (18,4) NOT NULL,
    TipoMercado Enum('LOTE', 'FRACIONARIO'),
    DataExecucao DATETIME,
    
    CONSTRAINT FK_OrdensCompra_Clientes
    Foreign Key (ContaMasterId)
    References Clientes(Id)
);

CREATE TABLE Distribuicoes(
	Id BIGINT AUTO_INCREMENT PRIMARY KEY,
	OrdemCompraId BIGINT NOT NULL,
    CustodiaFilhoteId BIGINT NOT NULL,
    Ticker VARCHAR (10) NOT NULL,
    Quantidade Int NOT NULL,
    PrecoUnitario Decimal (18,4) NOT NULL,
    DataDistribuicao DATETIME,
    
    CONSTRAINT FK_Distribuicoes_OrdensCompra
    Foreign Key (OrdemCompraId)
	References OrdensCompra(Id),
    
    CONSTRAINT FL_Distribuicoes_Custodia
    Foreign Key (CustodiaFilhoteId)
    References Custodias(Id)
);

CREATE TABLE CestasRecomendacao(
	Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR (100) NOT NULL,
    Ativa BOOLEAN,
    DataCriacao DATETIME NOT NULL,
    DataDesativacao DATETIME
);

CREATE TABLE ItensCesta(
	Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    CestaId BIGINT NOT NULL,
    Ticker VARCHAR (10),
    Percentual DECIMAL(5,2),

	CONSTRAINT FK_ItensCesta_CestasRecomendacao
    Foreign Key (CestaId)
    References CestasRecomendacao(Id)
);

CREATE TABLE EventosIR(
	Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    ClienteId BIGINT NOT NULL,
    Tipo Enum('DEDO_DURO','IR_VENDA'),
    ValorBase decimal(18,2) NOT NULL,
    ValorIR decimal (18,2) NOT NULL,
    PublicadoKafka BOOLEAN,
    DataEvento DATETIME,

	CONSTRAINT FK_EventosIR_Clientes
    foreign key (ClienteId)
    references Clientes(Id)
);

CREATE TABLE Cotacoes(
	Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    DataPregao DATE NOT NULL,
    Ticker VARCHAR (10) NOT NULL,
    PrecoAbertura DECIMAL (18,4) NOT NULL,
    PrecoFechamento DECIMAL (18,4) NOT NULL,
    PrecoMaximo DECIMAL (18,4) NOT NULL,
	PrecoMinimo DECIMAL (18,4) NOT NULL
);

CREATE TABLE Rebalanceamentos(
	Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    ClienteId BIGINT NOT NULL,
    Tipo Enum('MUDANCA_CESTA','DESVIO'),
    TickerVendido VARCHAR (10) NOT NULL,
    TickerComprado VARCHAR (10) NOT NULL,
    ValorVenda DECIMAL (18,2) NOT NULL,
    DataRebalanceamento DATETIME,
    
    CONSTRAINT FK_Rebalanceamentos_Clientes
    foreign key (ClienteId)
    references Clientes(Id)
);

CREATE TABLE Usuarios(
	Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    ClienteId BIGINT NOT NULL UNIQUE,
    Email VARCHAR(200) NOT NULL,
    Senha VARCHAR(200) NOT NULL,
    Tipo Enum('CLIENTE','ADMINISTRADOR'),
    
    CONSTRAINT FK_Usuarios_Clientes
	FOREIGN Key (ClienteID)
    References Clientes(Id)
);