using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TesteItau_WebMvc.Models;

namespace TesteItau_WebMvc.Controllers
{
    public class CestaController : Controller
    {
        private readonly IHttpClientFactory _factory;

        public CestaController(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UsuarioLogado") == null)
                return RedirectToAction("Login", "Auth");

            var client = _factory.CreateClient();

            var cestaResponse = await client.GetAsync("https://localhost:7101/api/CestasRecomendacao/ativa");

            if (!cestaResponse.IsSuccessStatusCode)
                return View(new List<CestaTopFiveViewModel>());

            var cestaJson = await cestaResponse.Content.ReadAsStringAsync();

            var cestaAtiva = JsonSerializer.Deserialize<CestaTopFiveViewModel>(
                cestaJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (cestaAtiva == null)
                return View(new List<CestaTopFiveViewModel>());

            var itensResponse = await client.GetAsync("https://localhost:7101/api/ItensCesta");

            var lista = new List<CestaTopFiveViewModel>();

            if (itensResponse.IsSuccessStatusCode)
            {
                var json = await itensResponse.Content.ReadAsStringAsync();

                var todosItens = JsonSerializer.Deserialize<List<CestaTopFiveViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<CestaTopFiveViewModel>();

                lista = todosItens.Where(i => i.CestaId == cestaAtiva.Id).ToList();
            }

            return View(lista);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(List<CestaTopFiveViewModel> model)
        {
            if (HttpContext.Session.GetString("UsuarioLogado") == null)
                return RedirectToAction("Login", "Auth");

            var client = _factory.CreateClient();

            // 1️⃣ Buscar cesta ativa atual
            var cestaResponse = await client.GetAsync("https://localhost:7101/api/CestasRecomendacao/ativa");

            if (!cestaResponse.IsSuccessStatusCode)
                return RedirectToAction("Index");

            var cestaJson = await cestaResponse.Content.ReadAsStringAsync();

            var cestaAtiva = JsonSerializer.Deserialize<CestaViewModel>(
                cestaJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (cestaAtiva == null)
                return RedirectToAction("Index");

            var idCestaAntiga = cestaAtiva.Id;

            // 2️⃣ Criar nova cesta
            var nomeNovaCesta = $"Cesta{DateTime.Now:dd_MM_yy}";

            var novaCesta = new
            {
                Nome = nomeNovaCesta,
                Ativo = true
            };

            var novaCestaResponse = await client.PostAsJsonAsync("https://localhost:7101/api/CestasRecomendacao", novaCesta);

            if (!novaCestaResponse.IsSuccessStatusCode)
                return RedirectToAction("Index");

            var novaCestaJson = await novaCestaResponse.Content.ReadAsStringAsync();

            var cestaCriada = JsonSerializer.Deserialize<CestaViewModel>(
                novaCestaJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (cestaCriada == null)
                return RedirectToAction("Index");

            var novoCestaId = cestaCriada.Id;

            // 3️⃣ Inserir os 5 novos itens
            foreach (var item in model)
            {
                var novoItem = new
                {
                    CestaId = novoCestaId,
                    Ticker = item.Ticker,
                    Percentual = item.Percentual
                };

                await client.PostAsJsonAsync("https://localhost:7101/api/ItensCesta", novoItem);
            }

            // 4️⃣ Desativar cesta antiga
            await client.PutAsync($"https://localhost:7101/api/CestasRecomendacao/desativar/{idCestaAntiga}", null);

            // 5️⃣ Redirecionar
            return RedirectToAction("Index", "Master");
        }

        public IActionResult Historico()
        {
            if (HttpContext.Session.GetString("UsuarioLogado") == null)
                return RedirectToAction("Login", "Auth");

            return View(new List<CestaTopFiveViewModel>());
        }

        [HttpPost]
        public async Task<IActionResult> Historico(long cestaId)
        {
            if (HttpContext.Session.GetString("UsuarioLogado") == null)
                return RedirectToAction("Login", "Auth");

            var client = _factory.CreateClient();

            var response = await client.GetAsync("https://localhost:7101/api/ItensCesta");

            var listaFiltrada = new List<CestaTopFiveViewModel>();

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                var todosItens = JsonSerializer.Deserialize<List<CestaTopFiveViewModel>>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new List<CestaTopFiveViewModel>();

                listaFiltrada = todosItens
                    .Where(i => i.CestaId == cestaId)
                    .ToList();
            }

            ViewBag.CestaId = cestaId;

            return View(listaFiltrada);
        }
    }
}
