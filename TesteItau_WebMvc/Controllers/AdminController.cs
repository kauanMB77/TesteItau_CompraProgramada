using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TesteItau_WebMvc.Models;

namespace TesteItau_WebMvc.Controllers
{
    public class AdminController : Controller
    {
        private readonly IHttpClientFactory _factory;

        public AdminController(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        public IActionResult Painel()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AtivarMotorCompra()
        {
            var client = _factory.CreateClient();

            var clientesAtivos = await ObterClientesAtivos(client);
            if (!clientesAtivos.Any())
                return RedirectToAction("Painel");

            var somaAportes = clientesAtivos.Sum(c => c.ValorMensal);
            
            //Faz o valor dividido por 3, conforme indicado no manual to Teste, considerando uma aplicação para dia 5, 15 ou 25 apenas
            var montanteCompra = somaAportes / 3;

            //Valida o retorno da Cesta, caso errada, não prossegue com as compras
            var cesta = await ObterCestaAtiva(client);
            if (cesta == null)
                return RedirectToAction("Painel");

            var cestaItens = await ObterItensCesta(client, cesta.Id);
            var cotacoes = await ObterCotacoes(client);
            var sobrasPorTicker = new Dictionary<string, int>();
            var acoesCompradas = await ProcessarCompras(client, clientesAtivos, cestaItens, cotacoes, montanteCompra, sobrasPorTicker);

            decimal totalInvestido = 0;

            foreach (var acao in acoesCompradas)
            {
                var cotacao = cotacoes.FirstOrDefault(c => c.Ticker == acao.Key);

                if (cotacao != null)
                {
                    totalInvestido += acao.Value * cotacao.PrecoFechamento;
                }
            }

            var dinheiroSobrando = montanteCompra - totalInvestido;

            //Informações para montar a tela de resultado das compras
            ViewBag.DinheiroSobrando = dinheiroSobrando;
            ViewBag.MontanteTotal = montanteCompra;
            ViewBag.TotalContasAtivas = clientesAtivos.Count;
            ViewBag.AcoesCompradas = acoesCompradas;
            ViewBag.AcoesSobraram = sobrasPorTicker;

            return View("ResultadoMotor");
        }

        private async Task<List<ClienteViewModel>> ObterClientesAtivos(HttpClient client)
        {
            var response = await client.GetAsync("https://localhost:7101/api/Clientes");

            if (!response.IsSuccessStatusCode)
                return new List<ClienteViewModel>();

            var json = await response.Content.ReadAsStringAsync();
            var clientes = JsonSerializer.Deserialize<List<ClienteViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ClienteViewModel>();

            return clientes.Where(c => c.Ativo).ToList();
        }

        private async Task<CestaViewModel?> ObterCestaAtiva(HttpClient client)
        {
            var response = await client.GetAsync("https://localhost:7101/api/CestasRecomendacao/ativa");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<CestaViewModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }

        private async Task<List<CestaTopFiveViewModel>> ObterItensCesta(HttpClient client, long cestaId)
        {
            var response = await client.GetAsync($"https://localhost:7101/api/ItensCesta/itensCesta/{cestaId}");

            if (!response.IsSuccessStatusCode)
                return new List<CestaTopFiveViewModel>();

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<List<CestaTopFiveViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<CestaTopFiveViewModel>();
        }

        private async Task<List<CotacaoViewModel>> ObterCotacoes(HttpClient client)
        {
            var response = await client.GetAsync("https://localhost:7101/api/Cotacoes");

            if (!response.IsSuccessStatusCode)
                return new List<CotacaoViewModel>();

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<List<CotacaoViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<CotacaoViewModel>();
        }

        private async Task<Dictionary<string, int>> ProcessarCompras( HttpClient client, List<ClienteViewModel> clientesAtivos, List<CestaTopFiveViewModel> cestaItens, List<CotacaoViewModel> cotacoes, decimal montanteTotal, Dictionary<string, int> sobrasPorTicker)
        {
            var acoesCompradas = new Dictionary<string, int>();

            //Alterar no futuro para pegar o primeido Id de alguma conta Master na lista de clientes
            long contaMasterId = 4;

            foreach (var item in cestaItens)
            {
                var cotacao = cotacoes.FirstOrDefault(c => c.Ticker == item.Ticker);
                if (cotacao == null)
                    continue;

                int quantidadeNovaCompra = CalcularQuantidadeTotal(montanteTotal, item.Percentual, cotacao.PrecoFechamento);

                var response = await client.GetAsync($"https://localhost:7101/api/Custodia/conta/{contaMasterId}");
                var json = await response.Content.ReadAsStringAsync();
               
                var custodiaMaster = JsonSerializer.Deserialize<List<CustodiaViewModel>>(json,new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<CustodiaViewModel>();

                var sobraMaster = (int)Math.Floor(custodiaMaster.Where(c => c.Ticker == item.Ticker).Sum(c => c.Quantidade));
                int quantidadeTotal = quantidadeNovaCompra + sobraMaster;

                //Tira o ticker da conta Master
                await client.DeleteAsync($"https://localhost:7101/api/Custodia/clear/{contaMasterId}/{item.Ticker}");

                if (quantidadeTotal <= 0)
                    continue;

                acoesCompradas[item.Ticker] = quantidadeNovaCompra;

                await EnviarOrdemCompra(client, contaMasterId, item.Ticker, quantidadeTotal, cotacao.PrecoFechamento);
                await DistribuirEntreClientes( client, clientesAtivos, item.Ticker, quantidadeTotal, cotacao.PrecoFechamento, montanteTotal, sobrasPorTicker);
            }

            return acoesCompradas;
        }

        private async Task DistribuirEntreClientes(HttpClient client, List<ClienteViewModel> clientesAtivos, string ticker, int quantidadeTotal, decimal preco, decimal montanteTotal, Dictionary<string, int> sobrasPorTicker)
        {
            int totalDistribuido = 0;

            foreach (var cliente in clientesAtivos)
            {
                var somaAportes = clientesAtivos.Sum(c => c.ValorMensal);
                var proporcao = cliente.ValorMensal / somaAportes;

                //Confirmo o número inteiro
                var quantidadeCliente =(int)Math.Floor(quantidadeTotal * proporcao);

                if (quantidadeCliente <= 0)
                    continue;

                totalDistribuido += quantidadeCliente;

                var conta = await ObterContaGrafica(client, cliente.Id);
                if (conta == null)
                    continue;

                var custodiaCliente = new
                {
                    contaGraficaId = conta.contaGraficaId,
                    ticker = ticker,
                    quantidade = quantidadeCliente,
                    precoMedio = preco
                };

                await client.PostAsJsonAsync("https://localhost:7101/api/Custodia", custodiaCliente);
            }

            var sobra = quantidadeTotal - totalDistribuido;

            if (sobra > 0)
            {
                sobrasPorTicker[ticker] = sobra;

                var custodiaMaster = new
                {
                    contaGraficaId = 4,
                    ticker = ticker,
                    quantidade = sobra,
                    precoMedio = preco
                };

                await client.PostAsJsonAsync("https://localhost:7101/api/Custodia", custodiaMaster);
            }
        }

        private int CalcularQuantidadeTotal(decimal montanteTotal, decimal percentual, decimal preco)
        {
            var valorParaItem = montanteTotal * (percentual / 100m);
            return (int)Math.Floor(valorParaItem / preco);
        }

        private async Task<ContaGraficaViewModel?> ObterContaGrafica(HttpClient client, long clienteId)
        {
            var response = await client.GetAsync($"https://localhost:7101/api/ContasGraficas/cliente/{clienteId}");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<ContaGraficaViewModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        //Preciso alterar o contaMasterId, atualmente está HardCoded
        private async Task EnviarOrdemCompra(HttpClient client, long contaMasterId, string ticker, int quantidade, decimal precoUnitario)
        {
            if (quantidade <= 0)
                return;

            var quantidadeLote = (quantidade / 100) * 100;
            var quantidadeFracionaria = quantidade % 100;

            if (quantidadeLote > 0)
            {
                var ordemLote = new
                {
                    contaMasterId = contaMasterId,
                    ticker = ticker,
                    quantidade = quantidadeLote,
                    precoUnitario = precoUnitario,
                    tipoMercado = "LOTE"
                };

                await client.PostAsJsonAsync("https://localhost:7101/api/OrdensCompra", ordemLote);
            }

            if (quantidadeFracionaria > 0)
            {
                var ordemFracionaria = new
                {
                    contaMasterId = contaMasterId,
                    ticker = ticker,
                    quantidade = quantidadeFracionaria,
                    precoUnitario = precoUnitario,
                    tipoMercado = "FRACIONARIO"
                };

                await client.PostAsJsonAsync("https://localhost:7101/api/OrdensCompra", ordemFracionaria);
            }
        }
    }
}
