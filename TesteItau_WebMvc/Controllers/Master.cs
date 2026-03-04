using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TesteItau_WebMvc.Models;

namespace TesteItau_WebMvc.Controllers
{
    public class Master : Controller
    {
        private readonly IHttpClientFactory _factory;

        public Master(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UsuarioLogado") == null)
                return RedirectToAction("Login", "Auth");

            return View();
        }

        public async Task<IActionResult> Carteira()
        {
            if (HttpContext.Session.GetString("UsuarioLogado") == null)
                return RedirectToAction("Login", "Auth");

            var contaId = HttpContext.Session.GetInt32("UsuarioId");

            if (contaId == null)
                return RedirectToAction("Login", "Auth");

            var client = _factory.CreateClient();

            var responseContaGrafica = await client.GetAsync($"https://localhost:7101/api/ContasGraficas/Cliente/{contaId}");

            if (!responseContaGrafica.IsSuccessStatusCode)
                return View(new CarteiraViewModel());

            var jsonContaGrafica = await responseContaGrafica.Content.ReadAsStringAsync();

            Console.WriteLine($"ContaGrafica JSON: {jsonContaGrafica}");

            var contaGrafica =
                JsonSerializer.Deserialize<ContaGraficaResponseViewModel>(
                    jsonContaGrafica,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (contaGrafica == null || contaGrafica.contaGraficaId == 0)
                return View(new CarteiraViewModel());

            var responseCustodias =
                await client.GetAsync($"https://localhost:7101/api/Custodia/conta/{contaGrafica.contaGraficaId}");

            Console.WriteLine($"Custodias JSON: {responseCustodias}");

            var listaCustodias = new List<CustodiaViewModel>();

            if (responseCustodias.IsSuccessStatusCode)
            {
                var jsonCustodias = await responseCustodias.Content.ReadAsStringAsync();

                Console.WriteLine($"Custodias JSON: {jsonCustodias}");

                listaCustodias =
                    JsonSerializer.Deserialize<List<CustodiaViewModel>>(
                        jsonCustodias,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new List<CustodiaViewModel>();
            }

            return View(new CarteiraViewModel
            {
                ContaGraficaId = contaGrafica.contaGraficaId,
                Custodias = listaCustodias
            });
        }
    }
}
