using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using TesteItau_WebMvc.Models;

namespace TesteItau_WebMvc.Controllers
{
    public class ManutencaoController : Controller
    {
        private readonly HttpClient _httpClient;

        public ManutencaoController(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7101/");
        }

        public async Task<IActionResult> Index()
        {
            //Valida usuario logado
            var clienteId = HttpContext.Session.GetInt32("UsuarioId");
            if (clienteId == null)
                return RedirectToAction("Login", "Auth");

            var response = await _httpClient.GetAsync($"api/Clientes/{clienteId}");
            if (!response.IsSuccessStatusCode)
                return RedirectToAction("Index", "Home");

            var json = await response.Content.ReadAsStringAsync();
            var cliente = JsonSerializer.Deserialize<ManutencaoViewModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            //Retorna cliente para montar 
            return View(cliente);
        }

        [HttpPost]
        public async Task<IActionResult> AlterarValor(string novoValor)
        {
            //Valida usuario logado
            var clienteId = HttpContext.Session.GetInt32("UsuarioId");
            if (clienteId == null)
                return RedirectToAction("Login", "Auth");

            //Valida valor para valorMensal
            if (!decimal.TryParse(novoValor, System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("pt-BR"), out decimal valorConvertido))
                return Content("Valor inválido.");

            var content = new StringContent(JsonSerializer.Serialize(valorConvertido), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/Clientes/{clienteId}/valor", content);

            if (!response.IsSuccessStatusCode)
                return Content("Erro ao alterar valor.");

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Desativar()
        {
            //Valida usuario logado
            var clienteId = HttpContext.Session.GetInt32("UsuarioId");
            if (clienteId == null)
                return RedirectToAction("Login", "Auth");

            var response = await _httpClient.PutAsync($"api/Clientes/{clienteId}/desativar", null);

            if (!response.IsSuccessStatusCode)
                return Content("Erro ao desativar investimento.");

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Ativar()
        {
            //Valida usuario logado
            var clienteId = HttpContext.Session.GetInt32("UsuarioId");
            if (clienteId == null)
                return RedirectToAction("Login", "Auth");

            var response = await _httpClient.PutAsync($"api/Clientes/{clienteId}/ativar", null);

            if (!response.IsSuccessStatusCode)
                return Content("Erro ao ativar investimento.");

            return RedirectToAction("Index", "Home");
        }
    }
}