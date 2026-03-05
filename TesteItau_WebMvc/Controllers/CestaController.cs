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
            //Valida se o usuário esta logado
            if (HttpContext.Session.GetString("UsuarioLogado") == null)
                return RedirectToAction("Login", "Auth");

            var client = _factory.CreateClient();

            //Verificando a cesta ativa
            var cestaResponse = await client.GetAsync("https://localhost:7101/api/CestasRecomendacao/ativa");
            if (!cestaResponse.IsSuccessStatusCode)
                return View(new List<CestaTopFiveViewModel>());

            var cestaJson = await cestaResponse.Content.ReadAsStringAsync();
            var cestaAtiva = JsonSerializer.Deserialize<CestaViewModel>(cestaJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (cestaAtiva == null)
                return View(new List<CestaTopFiveViewModel>());

            //Verificando itens da cesta ativa
            var itensResponse = await client.GetAsync($"https://localhost:7101/api/ItensCesta/itensCesta/{cestaAtiva.Id}");
            var lista = new List<CestaTopFiveViewModel>();

            //Armazena e retorna na View os itens da CestaAtiva
            if (itensResponse.IsSuccessStatusCode)
            {
                var json = await itensResponse.Content.ReadAsStringAsync();
                lista =JsonSerializer.Deserialize<List<CestaTopFiveViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<CestaTopFiveViewModel>();
            }

            return View(lista);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(List<CestaTopFiveViewModel> model)
        {
            //Validando se o usuário esta logado
            if (HttpContext.Session.GetString("UsuarioLogado") == null)
                return RedirectToAction("Login", "Auth");

            var client = _factory.CreateClient();

            //Buscando CestaAtiva
            var cestaResponse = await client.GetAsync("https://localhost:7101/api/CestasRecomendacao/ativa");
            if (!cestaResponse.IsSuccessStatusCode)
                return RedirectToAction("Index");

            var cestaJson = await cestaResponse.Content.ReadAsStringAsync();
            var cestaAtiva = JsonSerializer.Deserialize<CestaViewModel>(cestaJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (cestaAtiva == null)
                return RedirectToAction("Index");

            var idCestaAntiga = cestaAtiva.Id;

            //Armazenando os itens da CestaAntiga (cesta atual antes da mudança que será feita mais pra frente no método)
            var itensAntigosResponse = await client.GetAsync($"https://localhost:7101/api/ItensCesta/itensCesta/{idCestaAntiga}");
            var itensAntigos = new List<CestaTopFiveViewModel>();

            if (itensAntigosResponse.IsSuccessStatusCode)
            {
                var jsonAntigos = await itensAntigosResponse.Content.ReadAsStringAsync();
                itensAntigos = JsonSerializer.Deserialize<List<CestaTopFiveViewModel>>(jsonAntigos, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<CestaTopFiveViewModel>();
            }

            var tickersAntigos = itensAntigos.Select(i => i.Ticker.Trim().ToUpper()).ToList();

            //Criando a nova cesta, com os itens novos
            //O nome por exemplo é Cesta050326, baseado na data, como não precisa ser UNIQUE, não tem problema criar mais de uma por dia
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
            var cestaCriada =JsonSerializer.Deserialize<CestaViewModel>(novaCestaJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            //Valida se não houve nenhum erro na cestaCriada, se já tiver aqui, não continua os próximos passos de desativar a cesta antiga e ativar a nova
            if (cestaCriada == null)
                return RedirectToAction("Index");

            var novoCestaId = cestaCriada.Id;

            //Desativa a cesta antiga
            await client.PutAsync($"https://localhost:7101/api/CestasRecomendacao/desativar/{idCestaAntiga}", null);

            //Começo a lógiga de compras e vendas aqui, coletando os tickers que entraram
            var tickersNovos = model.Select(i => i.Ticker.Trim().ToUpper()).ToList();
            var tickersQueEntraram = tickersNovos.Where(t => !tickersAntigos.Contains(t)).ToList();

            //Importa a cotação dos tickers novos da COTAHIST
            await ImportarCotacoesPorTickers(tickersQueEntraram);

            //Insere novos itens no ItensCesta
            foreach (var item in model)
            {
                var novoItem = new
                {
                    CestaId = novoCestaId,
                    Ticker = item.Ticker.Trim().ToUpper(),
                    Percentual = item.Percentual
                };
                await client.PostAsJsonAsync("https://localhost:7101/api/ItensCesta", novoItem);
            }

            //Verifica Tickers que sairam da Cesta
            var tickersQueSairam = tickersAntigos.Where(t => !tickersNovos.Contains(t)).ToList();

            //Armazeno todos os Clientes
            var clientesResponse = await client.GetAsync("https://localhost:7101/api/Clientes");
            if (clientesResponse.IsSuccessStatusCode)
            {
                var jsonClientes = await clientesResponse.Content.ReadAsStringAsync();
                var clientes =JsonSerializer.Deserialize<List<ClienteViewModel>>(jsonClientes, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ClienteViewModel>();

                //Faço a venda das ações antigas e compra das novas com o valor por Cliente
                foreach (var cliente in clientes.Where(c => c.Ativo))
                {
                    //ContaGráfica de cada Cliente
                    var contaResponse = await client.GetAsync($"https://localhost:7101/api/ContasGraficas/cliente/{cliente.Id}");
                    if (!contaResponse.IsSuccessStatusCode)
                        continue;

                    var jsonConta = await contaResponse.Content.ReadAsStringAsync();
                    var conta =JsonSerializer.Deserialize<ContaGraficaViewModel>(jsonConta, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (conta == null)
                        continue;

                    //Custodias do cliente
                    var custodiaResponse =await client.GetAsync($"https://localhost:7101/api/Custodia/conta/{conta.contaGraficaId}");

                    if (!custodiaResponse.IsSuccessStatusCode)
                        continue;

                    var jsonCustodias = await custodiaResponse.Content.ReadAsStringAsync();
                    var custodias = JsonSerializer.Deserialize<List<CustodiaViewModel>>(jsonCustodias, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<CustodiaViewModel>();


                    decimal valorTotalVendido = 0m;


                    //Venda das ações antigas
                    var cotacoesResponse = await client.GetAsync("https://localhost:7101/api/Cotacoes");
                    if (!cotacoesResponse.IsSuccessStatusCode)
                        continue;

                    var jsonCotacoes = await cotacoesResponse.Content.ReadAsStringAsync();
                    var cotacoes =JsonSerializer.Deserialize<List<CotacaoViewModel>>(jsonCotacoes, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<CotacaoViewModel>();

                    //Passa Ticker a Ticker do cliente
                    foreach (var custodia in custodias)
                    {
                        //Valido se algum desses é o que saiu
                        var tickerCustodia = custodia.Ticker.Trim().ToUpper();
                        if (!tickersQueSairam.Contains(tickerCustodia))
                            continue;

                        var cotacao = cotacoes.Where(c => c.Ticker.Trim().ToUpper() == tickerCustodia).FirstOrDefault();

                        if (cotacao == null)
                            continue;

                        //Adicone a um caixa temporário que será utilizado para a compra
                        decimal valorVenda = custodia.Quantidade * cotacao.PrecoFechamento;
                        valorTotalVendido += valorVenda;

                        //Remove as cotações vendidas
                        await client.DeleteAsync($"https://localhost:7101/api/Custodia/clear/{conta.contaGraficaId}/{tickerCustodia}");
                    }

                    //Valida se houve alguma venda
                    if (valorTotalVendido <= 0)
                        continue;


                    //Compra dos novos Tickers, o valor da venda dos tickers que sairam é distribuida nos tickers novos
                    foreach (var itemNovo in model)
                    {
                        //Verifica os que entraram
                        var tickerNovo = itemNovo.Ticker.Trim().ToUpper();
                        if (!tickersQueEntraram.Contains(tickerNovo))
                            continue;

                        //Cotacoes dos tickers novos
                        var cotacao = cotacoes.Where(c => c.Ticker.Trim().ToUpper() == tickerNovo).FirstOrDefault();
                        if (cotacao == null || cotacao.PrecoFechamento <= 0)
                            continue;

                        decimal percentual = itemNovo.Percentual / 100m;
                        decimal valorParaComprar = valorTotalVendido;
                        if (valorParaComprar <= 0)
                            continue;

                        int quantidade = (int)Math.Floor(valorParaComprar / cotacao.PrecoFechamento);

                        if (quantidade <= 0)
                            continue;

                        var compra = new
                        {
                            contaGraficaId = conta.contaGraficaId,
                            ticker = tickerNovo,
                            quantidade = quantidade,
                            precoMedio = valorParaComprar/quantidade
                        };

                        await client.PostAsJsonAsync("https://localhost:7101/api/Custodia", compra);
                    }
                }
            }

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
            //Valida o UserLogado
            if (HttpContext.Session.GetString("UsuarioLogado") == null)
                return RedirectToAction("Login", "Auth");

            var client = _factory.CreateClient();

            //Lista ItensCesta dada certo ID
            var response = await client.GetAsync("https://localhost:7101/api/ItensCesta");
            var listaFiltrada = new List<CestaTopFiveViewModel>();

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                var todosItens = JsonSerializer.Deserialize<List<CestaTopFiveViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<CestaTopFiveViewModel>();

                listaFiltrada = todosItens.Where(i => i.CestaId == cestaId).ToList();
            }

            //ViewBag para ser mostrado na tela
            ViewBag.CestaId = cestaId;

            return View(listaFiltrada);
        }

        [HttpPost]
        public async Task<IActionResult> ImportarCotacao()
        {
            if (HttpContext.Session.GetString("UsuarioLogado") == null)
                return RedirectToAction("Login", "Auth");

            var client = _factory.CreateClient();

            //Buscando cestaAtiva
            var cestaResponse = await client.GetAsync("https://localhost:7101/api/CestasRecomendacao/ativa");
            if (!cestaResponse.IsSuccessStatusCode)
                return RedirectToAction("Index");

            var cestaJson = await cestaResponse.Content.ReadAsStringAsync();
            var cestaAtiva = JsonSerializer.Deserialize<CestaViewModel>(cestaJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            //Valida se encontrou a cestaAtiva
            if (cestaAtiva == null)
                return RedirectToAction("Index");

            //Armazena itens da cesta, tendo em vista que só busco no COTAHIST as cotacoes destes itens da cesta
            var itensResponse = await client.GetAsync($"https://localhost:7101/api/ItensCesta/itensCesta/{cestaAtiva.Id}");

            var tickersCesta = new List<string>();

            if (itensResponse.IsSuccessStatusCode)
            {
                var json = await itensResponse.Content.ReadAsStringAsync();
                var itens = JsonSerializer.Deserialize<List<CestaTopFiveViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<CestaTopFiveViewModel>();

                tickersCesta = itens.Select(i => i.Ticker.Trim().ToUpper()).ToList();
            }

            //Leitura do arquivo
            var caminhoArquivo = @"C:\Users\kauan\source\repos\TesteItau_WebApp\Cotahist\COTAHIST_D27022026.TXT";

            //Validação se o caminho está certo
            if (!System.IO.File.Exists(caminhoArquivo))
                return Content("Arquivo não encontrado.");

            var linhas = await System.IO.File.ReadAllLinesAsync(caminhoArquivo);
            int registrosImportados = 0;

            foreach (var linha in linhas)
            {
                //Ignorando Header e Trailler
                if (!linha.StartsWith("01"))
                    continue;

                if (linha.Length < 121)
                    continue;

                //Substring das linhas em si
                var codneg = linha.Substring(12, 12).Trim().ToUpper();
                var datpre = linha.Substring(2, 8).Trim();
                var preabe = linha.Substring(56, 13).Trim();
                var premax = linha.Substring(69, 13).Trim();
                var premin = linha.Substring(82, 13).Trim();
                var preult = linha.Substring(108, 13).Trim();

                //Aqui eu valido se o codneg é igual a algum dos tickers da Cesta, se não for não continuo a operação
                if (!tickersCesta.Contains(codneg))
                    continue;

                //Parse da Data
                if (!DateTime.TryParseExact(
                        datpre,
                        "yyyyMMdd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime dataPregao))
                {
                    continue;
                }

                //Parse dos preços
                if (!decimal.TryParse(preabe, out decimal abertura))
                    continue;

                if (!decimal.TryParse(premax, out decimal maximo))
                    continue;

                if (!decimal.TryParse(premin, out decimal minimo))
                    continue;

                if (!decimal.TryParse(preult, out decimal fechamento))
                    continue;

                // /100m para organizar o padrão do COTAHIST
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

            //Informação para a View
            TempData["Mensagem"] = $"{registrosImportados} cotações importadas com sucesso.";

            //No futuro, enviar para a View de ResultadosCotahist
            return RedirectToAction("Index");
        }

        private async Task ImportarCotacoesPorTickers(List<string> tickers)
        {
            //Valida se os tickers estão corretos, para evitar exceções
            if (tickers == null || !tickers.Any())
                return;

            var client = _factory.CreateClient();

            //Tirar o HardCodded
            var caminhoArquivo = @"C:\Users\kauan\source\repos\TesteItau_WebApp\Cotahist\COTAHIST_D27022026.TXT";

            //Valida caminho do arquivo
            if (!System.IO.File.Exists(caminhoArquivo))
                return;

            var linhas = await System.IO.File.ReadAllLinesAsync(caminhoArquivo);

            foreach (var linha in linhas)
            {
                //Ignora Headers e Traillers
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

                //Valida se o codneg está em Tickers, caso não não continua a operação
                if (!tickers.Contains(codneg))
                    continue;

                //Parse da Data
                if (!DateTime.TryParseExact(datpre, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dataPregao))
                    continue;

                //Parse dos preços
                if (!decimal.TryParse(preabe, out decimal abertura))
                    continue;

                if (!decimal.TryParse(premax, out decimal maximo))
                    continue;

                if (!decimal.TryParse(premin, out decimal minimo))
                    continue;

                if (!decimal.TryParse(preult, out decimal fechamento))
                    continue;

                // /100m para organizar o padrão do COTAHIST
                var cotacao = new
                {
                    DataPregao = dataPregao,
                    Ticker = codneg,
                    PrecoAbertura = abertura / 100m,
                    PrecoMaximo = maximo / 100m,
                    PrecoMinimo = minimo / 100m,
                    PrecoFechamento = fechamento / 100m
                };

                await client.PostAsJsonAsync("https://localhost:7101/api/Cotacoes", cotacao);
            }
        }
    }
}