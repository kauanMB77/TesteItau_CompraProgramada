using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TesteItau_WebApp.Controllers;
using TesteItau_WebApp.DAO;
using TesteItau_WebApp.Models;
using Xunit;

namespace XUnitTestProject.WebApi
{
    public class CestaRecomendacaoTest
    {
        private AppDBContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDBContext(options);
        }

        [Fact]
        public async Task PostCesta_DadosValidos()
        {
            var context = GetDbContext();
            var controller = new CestasRecomendacaoController(context);

            var cesta = new CestaRecomendacao
            {
                Nome = "Cesta06_03_26"
            };

            var result = await controller.PostCesta(cesta);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var cestaRetornada = Assert.IsType<CestaRecomendacao>(createdResult.Value);

            Assert.Equal("Cesta06_03_26", cestaRetornada.Nome);
            Assert.True(cestaRetornada.Ativa);
        }

        [Fact]
        public async Task PostCesta_NomeVazio()
        {
            var context = GetDbContext();
            var controller = new CestasRecomendacaoController(context);

            var cesta = new CestaRecomendacao
            {
                Nome = ""
            };

            var result = await controller.PostCesta(cesta);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetCestas_ComCestas()
        {
            var context = GetDbContext();

            context.CestasRecomendacao.Add(new CestaRecomendacao
            {
                Nome = "Cesta Moderada",
                Ativa = true,
                DataCriacao = DateTime.Now
            });

            context.SaveChanges();

            var controller = new CestasRecomendacaoController(context);

            var result = await controller.GetCestas();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var cestas = Assert.IsAssignableFrom<IEnumerable<CestaRecomendacao>>(okResult.Value);

            Assert.Single(cestas);
        }

        [Fact]
        public async Task GetCesta_IdExistente()
        {
            var context = GetDbContext();

            var cesta = new CestaRecomendacao
            {
                Nome = "Cesta",
                Ativa = true,
                DataCriacao = DateTime.Now
            };

            context.CestasRecomendacao.Add(cesta);
            context.SaveChanges();

            var controller = new CestasRecomendacaoController(context);

            var result = await controller.GetCesta(cesta.Id);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var cestaRetornada = Assert.IsType<CestaRecomendacao>(okResult.Value);

            Assert.Equal(cesta.Id, cestaRetornada.Id);
        }

        [Fact]
        public async Task GetCesta_IdInexistente()
        {
            var context = GetDbContext();
            var controller = new CestasRecomendacaoController(context);

            var result = await controller.GetCesta(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetAtiva_ComCestaAtiva()
        {
            var context = GetDbContext();

            context.CestasRecomendacao.Add(new CestaRecomendacao
            {
                Nome = "Cesta Ativa",
                Ativa = true,
                DataCriacao = DateTime.Now
            });

            context.SaveChanges();

            var controller = new CestasRecomendacaoController(context);

            var result = await controller.GetAtiva();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var cesta = Assert.IsType<CestaRecomendacao>(okResult.Value);

            Assert.True(cesta.Ativa);
        }

        [Fact]
        public async Task GetAtiva_SemCestaAtiva()
        {
            var context = GetDbContext();
            var controller = new CestasRecomendacaoController(context);

            var result = await controller.GetAtiva();

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DesativarCesta_CestaExistente()
        {
            var context = GetDbContext();

            var cesta = new CestaRecomendacao
            {
                Nome = "Cesta Teste",
                Ativa = true,
                DataCriacao = DateTime.Now
            };

            context.CestasRecomendacao.Add(cesta);
            context.SaveChanges();

            var controller = new CestasRecomendacaoController(context);

            var result = await controller.DesativarCesta(cesta.Id);

            var okResult = Assert.IsType<OkObjectResult>(result);

            var cestaAtualizada = await context.CestasRecomendacao.FindAsync(cesta.Id);

            Assert.False(cestaAtualizada.Ativa);
            Assert.NotNull(cestaAtualizada.DataDesativacao);
        }

        [Fact]
        public async Task DesativarCesta_IdInexistente()
        {
            var context = GetDbContext();
            var controller = new CestasRecomendacaoController(context);

            var result = await controller.DesativarCesta(999);

            Assert.IsType<NotFoundResult>(result);
        }

    }
}
