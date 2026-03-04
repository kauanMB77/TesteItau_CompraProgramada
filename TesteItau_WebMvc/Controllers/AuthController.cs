using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
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

        /// <summary>
        /// Rota de Login para o site, necessária para adicionar o HttpContext
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            //Sanitizando a model
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            //Valida o Email e Senha
            var response = await _httpClient.PostAsync("api/usuarios/login", content);
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Erro = "Email ou senha inválidos.";
                return View(model);
            }

            var result = await response.Content.ReadAsStringAsync();
            var usuario = JsonSerializer.Deserialize<LoginResponseViewModel>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            //Colocando o HttpContex que é utilizado em outras Controllers
            HttpContext.Session.SetString("UsuarioLogado", usuario.Email);
            HttpContext.Session.SetInt32("UsuarioId", usuario.Id) ;

            var emaildWithIdResponse = await _httpClient.GetAsync($"api/Clientes/{usuario.Email}/Id");
            if (!emaildWithIdResponse.IsSuccessStatusCode)
                return RedirectToAction("Index", "Home");
            var emaildWithId = await emaildWithIdResponse.Content.ReadAsStringAsync();

            var contaId = JsonSerializer.Deserialize<ClienteIdWithEmailViewModel>(emaildWithId, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var contaIdResponse = await _httpClient.GetAsync($"api/ContasGraficas/cliente/{contaId.clienteId}");
            //if (!contaIdResponse.IsSuccessStatusCode)
            //    return RedirectToAction("Index", "Home");

            var contaIdJson = await contaIdResponse.Content.ReadAsStringAsync();

            var contaIdObj = JsonSerializer.Deserialize<ContaGraficaResponsViewModel>(contaIdJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            //if (contaIdObj == null)
            //    return RedirectToAction("Index", "Home");

            var contaResponse = await _httpClient.GetAsync($"api/ContasGraficas/{contaIdObj.contaGraficaId}");
            //if (!contaResponse.IsSuccessStatusCode)
            //    return RedirectToAction("Index", "Home");

            var contaJson = await contaResponse.Content.ReadAsStringAsync();
            var conta = JsonSerializer.Deserialize<ContaGraficaResponsViewModel>(contaJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (conta?.Tipo?.ToUpper() == "MASTER")
            {
                return RedirectToAction("Index", "Master");
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Cadastro()
        {
            return View();
        }

        /// <summary>
        /// Esta rota não só cadastra o Usuário, ela faz POST para Cliente, ContaGrafica e por fim para Usuario
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Cadastro(CadastroViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                //Fazendo POST na tabela de Clientes
                var cadastroResponse = await _httpClient.PostAsJsonAsync("api/Clientes", new
                {
                    nome = model.Nome,
                    cpf = model.Cpf,
                    email = model.Email,
                    valorMensal = model.ValorMensal
                });

                //Validando o retorno de create Clientes
                if (!cadastroResponse.IsSuccessStatusCode)
                {
                    var erro = await cadastroResponse.Content.ReadAsStringAsync();
                    ViewBag.Erro = $"Erro ao criar cliente: {erro}";
                    return View(model);
                }

                //Busca o ClienteID pelo Email
                var clienteResponse = await _httpClient.GetAsync($"api/Clientes/{model.Email}/Id");
                if (!clienteResponse.IsSuccessStatusCode)
                {
                    var erro = await clienteResponse.Content.ReadAsStringAsync();
                    ViewBag.Erro = $"Erro ao buscar ClienteId: {erro}";
                    return View(model);
                }

                //Valida se o cliente existe e foi criado corretamente
                var cliente = await clienteResponse.Content.ReadFromJsonAsync<ClienteIdWithEmailViewModel>();
                if (cliente == null)
                {
                    ViewBag.Erro = "Cliente não encontrado após cadastro.";
                    return View(model);
                }

                long clienteId = cliente.clienteId;

                //Fazendo POST para criar ContaGrafica
                var contaResponse = await _httpClient.PostAsJsonAsync("api/ContasGraficas", new
                {
                    clienteId = clienteId,
                    numeroConta = clienteId.ToString(),
                    tipo = "FILHOTE"
                });

                //Valida retorno da criação de contaGrafica
                if (!contaResponse.IsSuccessStatusCode)
                {
                    var erro = await contaResponse.Content.ReadAsStringAsync();
                    ViewBag.Erro = $"Erro ao criar Conta Gráfica: {erro}";
                    return View(model);
                }

                //Fazendo POST para rota de Criacao de Usuario
                var usuarioResponse = await _httpClient.PostAsJsonAsync("api/Usuarios", new
                {
                    clienteId = clienteId,
                    email = model.Email,
                    senha = model.Senha,
                    tipo = "CLIENTE"
                });

                //Valida Criacao de Usuario
                if (!usuarioResponse.IsSuccessStatusCode)
                {
                    var erro = await usuarioResponse.Content.ReadAsStringAsync();
                    ViewBag.Erro = $"Erro ao criar Usuário: {erro}";
                    return View(model);
                }

                //Se tudo der certo, para o Login com as informacoes adicionadas nas 3 tabelas
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.Erro = "Erro inesperado: " + ex.Message;
                return View(model);
            }
        }

        public IActionResult Logout()
		{
			HttpContext.Session.Clear();
			return RedirectToAction("Login");
		}
	}
}
