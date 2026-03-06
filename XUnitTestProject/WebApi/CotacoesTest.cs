using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TesteItau_WebApp.Controllers;
using TesteItau_WebApp.DAO;
using TesteItau_WebApp.Models;
using Xunit;

namespace XUnitTestProject.WebApi
{
    public class CotacoesTest
    {
        private AppDBContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDBContext(options);
        }

        [Fact]
        public async Task PostCotacao_DadosValidos()
        {
            var context = GetDbContext();
            var controller = new CotacoesController(context);

            var cotacao = new Cotacao
            {
                Ticker = "ITUB4",
                DataPregao = DateTime.Now,
                PrecoAbertura = 10,
                PrecoFechamento = 11,
                PrecoMaximo = 12,
                PrecoMinimo = 9
            };

            var result = await controller.PostCotacao(cotacao);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var cotacaoRetornada = Assert.IsType<Cotacao>(createdResult.Value);

            Assert.Equal("ITUB4", cotacaoRetornada.Ticker);
        }

        [Fact]
        public async Task PostCotacao_TickerVazio()
        {
            var context = GetDbContext();
            var controller = new CotacoesController(context);

            var cotacao = new Cotacao
            {
                Ticker = "",
                DataPregao = DateTime.Now,
                PrecoAbertura = 10,
                PrecoFechamento = 11,
                PrecoMaximo = 12,
                PrecoMinimo = 9
            };

            var result = await controller.PostCotacao(cotacao);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PostCotacao_PrecoInvalido()
        {
            var context = GetDbContext();
            var controller = new CotacoesController(context);

            var cotacao = new Cotacao
            {
                Ticker = "ITUB4",
                DataPregao = DateTime.Now,
                PrecoAbertura = -1,
                PrecoFechamento = 11,
                PrecoMaximo = 12,
                PrecoMinimo = 9
            };

            var result = await controller.PostCotacao(cotacao);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetCotacoes_ComCotacoes()
        {
            var context = GetDbContext();

            context.Cotacoes.Add(new Cotacao
            {
                Ticker = "PETR4",
                DataPregao = DateTime.Now,
                PrecoAbertura = 20,
                PrecoFechamento = 21,
                PrecoMaximo = 22,
                PrecoMinimo = 19
            });

            context.SaveChanges();

            var controller = new CotacoesController(context);

            var result = await controller.GetCotacoes();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var cotacoes = Assert.IsAssignableFrom<IEnumerable<Cotacao>>(okResult.Value);

            Assert.Single(cotacoes);
        }

        [Fact]
        public async Task GetCotacao_IdExistente()
        {
            var context = GetDbContext();

            var cotacao = new Cotacao
            {
                Ticker = "VALE3",
                DataPregao = DateTime.Now,
                PrecoAbertura = 30,
                PrecoFechamento = 31,
                PrecoMaximo = 32,
                PrecoMinimo = 29
            };

            context.Cotacoes.Add(cotacao);
            context.SaveChanges();

            var controller = new CotacoesController(context);

            var result = await controller.GetCotacao(cotacao.Id);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var cotacaoRetornada = Assert.IsType<Cotacao>(okResult.Value);

            Assert.Equal(cotacao.Id, cotacaoRetornada.Id);
        }

        [Fact]
        public async Task GetCotacao_IdInexistente()
        {
            var context = GetDbContext();
            var controller = new CotacoesController(context);

            var result = await controller.GetCotacao(999);

            Assert.IsType<NotFoundResult>(result);
        }

    }
}
