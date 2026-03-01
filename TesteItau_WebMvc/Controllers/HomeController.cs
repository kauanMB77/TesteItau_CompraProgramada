using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using TesteItau_WebMvc.Models;

namespace TesteItau_WebMvc.Controllers
{
	public class HomeController : Controller
	{

        private readonly ILogger<HomeController> _logger;

		public HomeController(ILogger<HomeController> logger)
		{
			_logger = logger;
		}

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UsuarioLogado") == null)
                return RedirectToAction("Login", "Auth");

            HomeViewModel statusCliente = new HomeViewModel();

            using (var httpClient = new HttpClient())
            {
                var clienteId = HttpContext.Session.GetInt32("UsuarioId");
                var response = await httpClient.GetAsync($"https://localhost:7101/api/Clientes/{clienteId}/status");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    statusCliente = JsonSerializer.Deserialize<HomeViewModel>(json, new JsonSerializerOptions {PropertyNameCaseInsensitive = true});
                }
            }

            var viewModel = new HomeViewModel
            {
                ativo = statusCliente?.ativo ?? false,
                valorInvestido = statusCliente?.valorInvestido ?? 0
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
