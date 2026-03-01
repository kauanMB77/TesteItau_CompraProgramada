using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
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

            var usuario = JsonSerializer.Deserialize<LoginResponseViewModel>( result,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            HttpContext.Session.SetString("UsuarioLogado", usuario.Email);
            HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
            HttpContext.Session.SetString("UsuarioTipo", usuario.Tipo);

            return RedirectToAction("Index", "Home");
		}

		public IActionResult Logout()
		{
			HttpContext.Session.Clear();
			return RedirectToAction("Login");
		}
	}
}
