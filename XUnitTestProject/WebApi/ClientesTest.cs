using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TesteItau_WebApp.Controllers;
using TesteItau_WebApp.DAO;
using TesteItau_WebApp.Models;
using Xunit;

namespace XUnitTestProject.WebApi
{
    public class ClientesTest
    {
        private AppDBContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDBContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;

            return new AppDBContext(options);
        }

        [Fact]
        public async Task PostCliente_DadosValidos()
        {
            var context = GetDbContext();
            var controller = new ClientesController(context);
            var cliente = new Client
            {
                Nome = "João",
                CPF = "12345678900",
                Email = "joaoTeste@email.com",
                ValorMensal = 1000,
                Ativo = true
            };

            var result = await controller.PostCliente(cliente);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var clienteRetornado = Assert.IsType<Client>(createdResult.Value);

            Assert.Equal("João", clienteRetornado.Nome);
        }

        [Fact]
        public async Task PostCliente_NomeVazio()
        {
            var context = GetDbContext();
            var controller = new ClientesController(context);
            var cliente = new Client
            {
                Nome = "",
                CPF = "12345678900",
                Email = "teste@email.com",
                ValorMensal = 1000
            };

            var result = await controller.PostCliente(cliente);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetClientes_ComClientes()
        {
            var context = GetDbContext();

            context.Clientes.Add(new Client
            {
                Nome = "Maria",
                CPF = "11112311111",
                Email = "mariaTeste@email.com",
                ValorMensal = 500,
                Ativo = true
            });

            context.SaveChanges();
            var controller = new ClientesController(context);

            var result = await controller.GetClientes();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var clientes = Assert.IsAssignableFrom<IEnumerable<Client>>(okResult.Value);

            Assert.Single(clientes);
        }

        [Fact]
        public async Task GetCliente_IdInexistente()
        {
            var context = GetDbContext();
            var controller = new ClientesController(context);

            var result = await controller.GetCliente(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DesativarConta_ClienteAtivo()
        {
            var context = GetDbContext();
            var cliente = new Client
            {
                Nome = "Carlos",
                CPF = "22222222222",
                Email = "carlos@email.com",
                ValorMensal = 700,
                Ativo = true
            };

            context.Clientes.Add(cliente);
            context.SaveChanges();
            var controller = new ClientesController(context);

            var result = await controller.DesativarConta(cliente.Id);

            var okResult = Assert.IsType<OkObjectResult>(result);

            var clienteAtualizado = await context.Clientes.FindAsync(cliente.Id);
            Assert.False(clienteAtualizado.Ativo);
        }

        [Fact]
        public async Task AtualizarValorInvestimento()
        {
            var context = GetDbContext();

            var cliente = new Client
            {
                Nome = "Pedro",
                CPF = "33312333333",
                Email = "pedro@email.com",
                ValorMensal = 100,
                Ativo = true
            };

            context.Clientes.Add(cliente);
            context.SaveChanges();

            var controller = new ClientesController(context);

            var result = await controller.AtualizarValorInvestimento(cliente.Id, 500);

            var okResult = Assert.IsType<OkObjectResult>(result);

            var clienteAtualizado = await context.Clientes.FindAsync(cliente.Id);
            Assert.Equal(500, clienteAtualizado.ValorMensal);
        }
    }
}
