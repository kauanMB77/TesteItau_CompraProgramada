using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TesteItau_WebMvc.Models;
using System.Globalization;

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

        [HttpPost]
        public async Task<IActionResult> ImportarCotacao()
        {
            if (HttpContext.Session.GetString("UsuarioLogado") == null)
                return RedirectToAction("Login", "Auth");

            var client = _factory.CreateClient();

            // 1️⃣ Buscar cesta ativa
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

            // 2️⃣ Buscar itens da cesta ativa
            var itensResponse = await client.GetAsync(
                $"https://localhost:7101/api/ItensCesta/itensCesta/{cestaAtiva.Id}");

            var tickersCesta = new List<string>();

            if (itensResponse.IsSuccessStatusCode)
            {
                var json = await itensResponse.Content.ReadAsStringAsync();

                var itens = JsonSerializer.Deserialize<List<CestaTopFiveViewModel>>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new List<CestaTopFiveViewModel>();

                tickersCesta = itens
                    .Select(i => i.Ticker.Trim().ToUpper())
                    .ToList();
            }

            // 3️⃣ Ler arquivo
            var caminhoArquivo = @"C:\Users\kauan\source\repos\TesteItau_WebApp\Cotahist\COTAHIST_D27022026.TXT";

            if (!System.IO.File.Exists(caminhoArquivo))
                return Content("Arquivo não encontrado.");

            var linhas = await System.IO.File.ReadAllLinesAsync(caminhoArquivo);

            int registrosImportados = 0;

            foreach (var linha in linhas)
            {
                // Ignorar header/trailer
                if (!linha.StartsWith("01"))
                    continue;

                if (linha.Length < 121)
                    continue;

                var codneg = linha.Substring(12, 12).Trim().ToUpper();
                var datpre = linha.Substring(2, 8).Trim();
                var preabe = linha.Substring(56, 13).Trim();
                var premax = linha.Substring(69, 13).Trim();
                var premin = linha.Substring(82, 13).Trim();
                var preult = linha.Substring(108, 13).Trim();

                if (!tickersCesta.Contains(codneg))
                    continue;

                // ✅ Parse seguro da data
                if (!DateTime.TryParseExact(
                        datpre,
                        "yyyyMMdd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime dataPregao))
                {
                    continue;
                }

                // ✅ Parse seguro dos preços
                if (!decimal.TryParse(preabe, out decimal abertura))
                    continue;

                if (!decimal.TryParse(premax, out decimal maximo))
                    continue;

                if (!decimal.TryParse(premin, out decimal minimo))
                    continue;

                if (!decimal.TryParse(preult, out decimal fechamento))
                    continue;

                var cotacao = new
                {
                    DataPregao = dataPregao,
                    Ticker = codneg,
                    PrecoAbertura = abertura / 100m,
                    PrecoMaximo = maximo / 100m,
                    PrecoMinimo = minimo / 100m,
                    PrecoFechamento = fechamento / 100m
                };

                var response = await client.PostAsJsonAsync("https://localhost:7101/api/Cotacoes", cotacao);

                if (response.IsSuccessStatusCode)
                    registrosImportados++;
            }

            TempData["Mensagem"] = $"{registrosImportados} cotações importadas com sucesso.";

            return RedirectToAction("Index");
        }
    }
}
