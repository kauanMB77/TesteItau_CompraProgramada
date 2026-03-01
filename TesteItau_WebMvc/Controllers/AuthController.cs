using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using TesteItau_WebApp.Models;
using TesteItau_WebMvc.Models;

namespace TesteItau_WebMvc.Controllers
{
	public class AuthController : Controller
	{
		private readonly HttpClient _httpClient;

		public AuthController(IHttpClientFactory factory)
		{
			_httpClient = factory.CreateClient();
			_httpClient.BaseAddress = new Uri("https://localhost:7101/");
		}

		[HttpGet]
		public IActionResult Login()
		{
			return View();
		}

		[HttpPost]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/usuarios/login", content);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Erro = "Email ou senha inválidos.";
                return View(model);
            }

            var result = await response.Content.ReadAsStringAsync();

            var usuario = JsonSerializer.Deserialize<LoginResponseViewModel>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            HttpContext.Session.SetString("UsuarioLogado", usuario.Email);
            HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
            Console.WriteLine($"UsuarioId: {usuario.Id}");

            var emaildWithIdResponse = await _httpClient.GetAsync($"api/Clientes/{usuario.Email}/Id");

            if (!emaildWithIdResponse.IsSuccessStatusCode)
                return RedirectToAction("Index", "Home");

            var emaildWithId = await emaildWithIdResponse.Content.ReadAsStringAsync();

            var contaId = JsonSerializer.Deserialize<ClienteIdWithEmailViewModel>(emaildWithId, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });


            var contaIdResponse = await _httpClient.GetAsync($"api/ContasGraficas/cliente/{contaId.clienteId}");
            Console.WriteLine($"Id Conta Grafica: {contaIdResponse}");

            if (!contaIdResponse.IsSuccessStatusCode)
                return RedirectToAction("Index", "Home");

            var contaIdJson = await contaIdResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"Conta IDJSON: {contaIdJson}");
            var contaIdObj = JsonSerializer.Deserialize<ContaGraficaResponsViewModel>(contaIdJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Console.WriteLine($"ContaIdObj: {contaIdObj.contaGraficaId}");
            if (contaIdObj == null)
                return RedirectToAction("Index", "Home");

            var contaResponse = await _httpClient.GetAsync($"api/ContasGraficas/{contaIdObj.contaGraficaId}");

            Console.WriteLine($"contaResponse: {contaResponse}");
            if (!contaResponse.IsSuccessStatusCode)
                return RedirectToAction("Index", "Home");

            var contaJson = await contaResponse.Content.ReadAsStringAsync();

            var conta = JsonSerializer.Deserialize<ContaGraficaResponsViewModel>(contaJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (conta?.Tipo?.ToUpper() == "MASTER")
            {
                return RedirectToAction("Index", "Master");
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
		{
			HttpContext.Session.Clear();
			return RedirectToAction("Login");
		}
	}
}
