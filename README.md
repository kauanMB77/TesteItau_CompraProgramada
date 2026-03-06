# TesteItau_WebApp

Bom dia, segue README com passo a passo para utilização do projeto TesteItau_CompraProgramada feito por Kauan Moreira Bortoloto.

===DOCKER===

Busquen dentro da pasta YMLS ambos os arquivos .yml para recriar os containers do MySql e Kafka e KafkaUI, ambos são necessários para o funcionamento da aplicação.

--Validações---

Para verificar o funcionamento de ambos, entre com o kafkaUI em: http://localhost:8080/ui/clusters/local/all-topics/impostos e verifique que o tópico está criado. Caso não crie um tópico com o nome impostos dentro do Kafka.

Para validar o MySql, tente conectar com o MySQLWorkBench na instancia criada do MySQL.

===MySQL===

Após validar a instancia, busque os arquivos .sql dentro da pasta root Sql, nela rode inicialmente o arquivo CreateDatabase.sql, caso não haja erros, rode o arquivo InitialInsert.sql, para popular com as contas iniciais necessarias para 
o funcionamento do projeto.

--Validações---

Para validar, de um select na tabela de Clientes e valide que 4 clientes foram criados.

===VsStudio===

Por fim clone o repositório e o abra com o VsStudio, selecione como startUp Projects tanto TesteItau_WebApp quanto TesteItau_WebMvc e clique em start. Devem ser abertas duas novas abas em seu navegador, sendo uma delas o Swagger e uma 
delas a tela de Auth do projeto.

--Validações---

Chame a rota GETClientes dentro do swagger, caso ele retorne os 4 usuários corretamente todas as ligações estão funcionando. Caso algum erro ocorra neste ponto, o mais provavel será que a instancia do SQL não está funcionando, ou que a 
senha do Database configurada em: appsettings.json dentro do TesteItau_WebApp esteja incorreta.

==Funcionamento=

Já é possível utilizar o sistema assim como demonstrado no video de showcase, a área do usuário já está em pleno funcionamento. Porém caso queira simular ações como o MotorDeCompra, lembre-se que é necessário importar as cotações dentro 
da área de Cesta. Caso nenhuma Cotação seja importada nenhuma compra será realizada no MotorDeCompra.



===---Demais Dúvidas---===

Para demais dúvidas ou feedbacks, estou disponível para contatos, desde já agradeço a atenção:

Cel: (11) 99408-9841
Email: kauanmb@hotmail.com