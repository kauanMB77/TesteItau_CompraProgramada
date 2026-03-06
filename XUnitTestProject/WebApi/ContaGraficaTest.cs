using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TesteItau_WebApp.Controllers;
using TesteItau_WebApp.DAO;
using TesteItau_WebApp.Models;
using Xunit;

namespace XUnitTestProject.WebApi
{
    public class ContaGraficaTest
    {
        private AppDBContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDBContext(options);
        }

        [Fact]
        public async Task PostContaGrafica_DadosValidos()
        {
            var context = GetDbContext();

            var cliente = new Client
            {
                Nome = "Joao",
                CPF = "12345678900",
                Email = "joao@email.com",
                ValorMensal = 1000,
                Ativo = true
            };

            context.Clientes.Add(cliente);
            context.SaveChanges();

            var controller = new ContasGraficasController(context);

            var conta = new ContaGrafica
            {
                ClienteId = cliente.Id,
                NumeroConta = "12345",
                Tipo = "INVESTIMENTO"
            };

            var result = await controller.PostContaGrafica(conta);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var contaRetornada = Assert.IsType<ContaGrafica>(createdResult.Value);

            Assert.Equal(cliente.Id, contaRetornada.ClienteId);
        }

        [Fact]
        public async Task PostContaGrafica_ClienteInexistente()
        {
            var context = GetDbContext();
            var controller = new ContasGraficasController(context);

            var conta = new ContaGrafica
            {
                ClienteId = 999,
                NumeroConta = "12345",
                Tipo = "INVESTIMENTO"
            };

            var result = await controller.PostContaGrafica(conta);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PostContaGrafica_ClienteJaPossuiConta()
        {
            var context = GetDbContext();

            var cliente = new Client
            {
                Nome = "Maria",
                CPF = "11111111111",
                Email = "maria@email.com",
                ValorMensal = 500,
                Ativo = true
            };

            context.Clientes.Add(cliente);
            context.SaveChanges();

            context.ContasGraficas.Add(new ContaGrafica
            {
                ClienteId = cliente.Id,
                NumeroConta = "9999",
                Tipo = "INVESTIMENTO",
                DataCriacao = DateTime.Now
            });

            context.SaveChanges();

            var controller = new ContasGraficasController(context);

            var novaConta = new ContaGrafica
            {
                ClienteId = cliente.Id,
                NumeroConta = "8888",
                Tipo = "INVESTIMENTO"
            };

            var result = await controller.PostContaGrafica(novaConta);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PostContaGrafica_NumeroContaDuplicado()
        {
            var context = GetDbContext();

            var cliente1 = new Client
            {
                Nome = "Carlos",
                CPF = "22222222222",
                Email = "carlos@email.com",
                ValorMensal = 700,
                Ativo = true
            };

            var cliente2 = new Client
            {
                Nome = "Pedro",
                CPF = "33333333333",
                Email = "pedro@email.com",
                ValorMensal = 900,
                Ativo = true
            };

            context.Clientes.AddRange(cliente1, cliente2);
            context.SaveChanges();

            context.ContasGraficas.Add(new ContaGrafica
            {
                ClienteId = cliente1.Id,
                NumeroConta = "12345",
                Tipo = "INVESTIMENTO",
                DataCriacao = DateTime.Now
            });

            context.SaveChanges();

            var controller = new ContasGraficasController(context);

            var novaConta = new ContaGrafica
            {
                ClienteId = cliente2.Id,
                NumeroConta = "12345",
                Tipo = "INVESTIMENTO"
            };

            var result = await controller.PostContaGrafica(novaConta);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetContaGrafica_IdExistente()
        {
            var context = GetDbContext();

            var cliente = new Client
            {
                Nome = "Ana",
                CPF = "44444444444",
                Email = "ana@email.com",
                ValorMensal = 400,
                Ativo = true
            };

            context.Clientes.Add(cliente);
            context.SaveChanges();

            var conta = new ContaGrafica
            {
                ClienteId = cliente.Id,
                NumeroConta = "5555",
                Tipo = "INVESTIMENTO",
                DataCriacao = DateTime.Now
            };

            context.ContasGraficas.Add(conta);
            context.SaveChanges();

            var controller = new ContasGraficasController(context);

            var result = await controller.GetContaGrafica(conta.Id);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var contaRetornada = Assert.IsType<ContaGrafica>(okResult.Value);

            Assert.Equal(conta.Id, contaRetornada.Id);
        }

        [Fact]
        public async Task GetContaGrafica_IdInexistente()
        {
            var context = GetDbContext();
            var controller = new ContasGraficasController(context);

            var result = await controller.GetContaGrafica(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetContas_ComContas_DeveRetornarOk()
        {
            var context = GetDbContext();

            var cliente = new Client
            {
                Nome = "Lucas",
                CPF = "55555555555",
                Email = "lucas@email.com",
                ValorMensal = 300,
                Ativo = true
            };

            context.Clientes.Add(cliente);
            context.SaveChanges();

            context.ContasGraficas.Add(new ContaGrafica
            {
                ClienteId = cliente.Id,
                NumeroConta = "7777",
                Tipo = "INVESTIMENTO",
                DataCriacao = DateTime.Now
            });

            context.SaveChanges();

            var controller = new ContasGraficasController(context);

            var result = await controller.GetContas();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var contas = Assert.IsAssignableFrom<IEnumerable<ContaGrafica>>(okResult.Value);

            Assert.Single(contas);
        }

        [Fact]
        public async Task GetContaGraficaIdByCliente_ClienteComConta()
        {
            var context = GetDbContext();

            var cliente = new Client
            {
                Nome = "Marcos",
                CPF = "66666666666",
                Email = "marcos@email.com",
                ValorMensal = 800,
                Ativo = true
            };

            context.Clientes.Add(cliente);
            context.SaveChanges();

            var conta = new ContaGrafica
            {
                ClienteId = cliente.Id,
                NumeroConta = "8888",
                Tipo = "INVESTIMENTO",
                DataCriacao = DateTime.Now
            };

            context.ContasGraficas.Add(conta);
            context.SaveChanges();

            var controller = new ContasGraficasController(context);

            var result = await controller.GetContaGraficaIdByCliente(cliente.Id);

            var okResult = Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetContaGraficaIdByCliente_ClienteSemConta()
        {
            var context = GetDbContext();

            var cliente = new Client
            {
                Nome = "Julia",
                CPF = "77777777777",
                Email = "julia@email.com",
                ValorMensal = 600,
                Ativo = true
            };

            context.Clientes.Add(cliente);
            context.SaveChanges();

            var controller = new ContasGraficasController(context);

            var result = await controller.GetContaGraficaIdByCliente(cliente.Id);

            Assert.IsType<NotFoundObjectResult>(result);
        }

    }
}
