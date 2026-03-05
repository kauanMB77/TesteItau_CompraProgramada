using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using TesteItau_WebMvc.Models;

public class RebalanceamentoController : Controller
{
    private readonly IHttpClientFactory _factory;

    public RebalanceamentoController(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<IActionResult> Executar()
    {
        var client = _factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost:7101/");

        var resultadoFinal = new List<string>();

        //Buscando a CestaAtiva no momento
        var cestaAtivaResp = await client.GetAsync("api/CestasRecomendacao/ativa");
        var cestaAtivaJson = await cestaAtivaResp.Content.ReadAsStringAsync();
        var cestaAtiva = JsonSerializer.Deserialize<CestaViewModel>(cestaAtivaJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        //Itens da CestaAtiva
        var itensResp = await client.GetAsync($"api/ItensCesta/itensCesta/{cestaAtiva.Id}");
        var itensJson = await itensResp.Content.ReadAsStringAsync();
        var itensCesta = JsonSerializer.Deserialize<List<CestaTopFiveViewModel>>(itensJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        //Cotacoes da CestaAtiva
        var cotacoesResp = await client.GetAsync("api/Cotacoes");
        var cotacoesJson = await cotacoesResp.Content.ReadAsStringAsync();
        var cotacoes = JsonSerializer.Deserialize<List<CotacaoViewModel>>(cotacoesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Dicionario dos tickers por cotacao
        var precosFechamento = cotacoes.GroupBy(c => c.Ticker).ToDictionary(g => g.Key, g => g.First().PrecoFechamento);

        //Lista Usuarios Ativos
        var usuariosResp = await client.GetAsync("api/Clientes");
        var usuariosJson = await usuariosResp.Content.ReadAsStringAsync();
        var usuarios = JsonSerializer.Deserialize<List<ClienteViewModel>>(usuariosJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var usuariosAtivos = usuarios.Where(u => u.Ativo && u.ValorMensal > 0).ToList();

        //Foreach feito para cada usuário, aqui realmente é feito o motor de rebalanceamento
        foreach (var usuario in usuariosAtivos)
        {
            //ContaGrafica de cada Use
            var contaResp = await client.GetAsync($"api/ContasGraficas/cliente/{usuario.Id}");
            if (!contaResp.IsSuccessStatusCode)
                continue;

            var contaJson = await contaResp.Content.ReadAsStringAsync();
            var conta = JsonSerializer.Deserialize<ContaGraficaViewModel>(contaJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (conta == null)
                continue;

            //Custodias da Conta atual do foreach
            var custodiaResp = await client.GetAsync($"api/Custodia/conta/{conta.contaGraficaId}");
            var custodiaJson = await custodiaResp.Content.ReadAsStringAsync();
            var custodias = JsonSerializer.Deserialize<List<CustodiaViewModel>>(custodiaJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!custodias.Any())
                continue;

            //Calculando o valor total da carteira, para utilizar no rebalanceamento
            decimal valorTotal = custodias.Sum(c => c.Quantidade * precosFechamento[c.Ticker]);

            decimal caixa = 0;

            var vendasRealizadas = new List<(string ticker, decimal valorVenda)>();

            //Começando o motor de vendas para Tickers acima da porcentagem
            foreach (var item in itensCesta)
            {
                var custodia = custodias.FirstOrDefault(c => c.Ticker == item.Ticker);
                if (custodia == null)
                    continue;

                decimal precoAtual = precosFechamento[item.Ticker];
                decimal valorAtual = custodia.Quantidade * precoAtual;
                decimal valorAlvo = (item.Percentual / 100m) * valorTotal;
                decimal diferencaValor = valorAtual - valorAlvo;
                decimal percentualAtual = (valorAtual / valorTotal) * 100;
                decimal diferencaPercentual = percentualAtual - item.Percentual;

                //Caso o diferencaPercentual entre a carteira e o ItensCesta seja maior que 5%, vende os itens, este 5% é apenas um buffer para que entre 5% acima e 5% abaixo seja considerado dentro da porcentagem aceitável 
                if (diferencaPercentual > 5)
                {
                    //Calcula quanto será o valor para as compras
                    decimal valorVenda = diferencaValor;
                    int quantidadeVenda = (int)Math.Floor(valorVenda / precoAtual);
                    if (quantidadeVenda <= 0)
                        continue;

                    //caixa = valor utlizado para as compras
                    caixa += quantidadeVenda * precoAtual;
                    custodia.Quantidade -= quantidadeVenda;

                    //Valores para o POST de Rebalanceamentos
                    decimal valorExecutadoVenda = quantidadeVenda * precoAtual;
                    vendasRealizadas.Add((item.Ticker, valorExecutadoVenda));


                    //Log para a tela de resultado
                    resultadoFinal.Add($"Usuário {usuario.Id}: Vendeu {quantidadeVenda} de {item.Ticker}");

                    //Envia apenas diferença negativa
                    var atualizarCustodia = new
                    {
                        contaGraficaId = conta.contaGraficaId,
                        ticker = item.Ticker,
                        quantidade = -quantidadeVenda,
                        precoMedio = custodia.PrecoMedio
                    };

                    var content = new StringContent(JsonSerializer.Serialize(atualizarCustodia), Encoding.UTF8, "application/json");

                    await client.PostAsync("api/Custodia", content);
                }

                //Recalcula o valor total da carteira após todas as possiveis vendas
                //valorTotal = custodias.Sum(c => c.Quantidade * precosFechamento[c.Ticker]);
            }

            //Compras utilizando o valor de venda das ações
            if (caixa > 0)
            {
                // Recalcula valor total após vendas
                valorTotal = custodias.Sum(c => c.Quantidade * precosFechamento[c.Ticker]);

                var ativosParaComprar = new List<(string Ticker, decimal DeficitValor)>();

                //Foreach para calcular quais ativos precisam ser comprados
                //Basicamente busco no foreach o quanto eu tenho do valor na custodia e uso o valorTotal pra ver a porcentagem que eu deveria ter
                foreach (var item in itensCesta)
                {
                    decimal precoAtual = precosFechamento[item.Ticker];
                    var custodia = custodias.FirstOrDefault(c => c.Ticker == item.Ticker);
                    decimal quantidadeAtual = custodia?.Quantidade ?? 0;
                    decimal valorAtual = quantidadeAtual * precoAtual;
                    decimal valorAlvo = (item.Percentual / 100m) * valorTotal;
                    decimal deficit = valorAlvo - valorAtual;

                    if (deficit > 0)
                        ativosParaComprar.Add((item.Ticker, deficit));
                }

                if (ativosParaComprar.Any())
                {
                    decimal totalDeficit = ativosParaComprar.Sum(a => a.DeficitValor);

                    foreach (var ativo in ativosParaComprar)
                    {
                        decimal proporcao = ativo.DeficitValor / totalDeficit;
                        decimal valorDestino = caixa * proporcao;
                        decimal precoAtual = precosFechamento[ativo.Ticker];
                        int quantidadeCompra = (int)Math.Floor(valorDestino / precoAtual);
                        if (quantidadeCompra <= 0)
                            continue;

                        decimal valorExecutado = quantidadeCompra * precoAtual;

                        foreach (var venda in vendasRealizadas)
                        {
                            await EnviarRebalanceamento(client, usuario.Id, venda.ticker, ativo.Ticker,venda.valorVenda);
                        }

                        caixa -= valorExecutado;

                        var custodia = custodias.FirstOrDefault(c => c.Ticker == ativo.Ticker);

                        if (custodia == null)
                        {
                            custodia = new CustodiaViewModel
                            {
                                Ticker = ativo.Ticker,
                                Quantidade = 0,
                                PrecoMedio = 0
                            };

                            custodias.Add(custodia);
                        }

                        //Recalculando o precoMedio para mandar pra API
                        decimal quantidadeAtual = custodia.Quantidade;
                        decimal precoMedioAtual = custodia.PrecoMedio;

                        decimal novoPrecoMedio = precoAtual;

                        if (quantidadeAtual > 0)
                        {
                            novoPrecoMedio = ((quantidadeAtual * precoMedioAtual) + (quantidadeCompra * precoAtual)) / (quantidadeAtual + quantidadeCompra);
                        }

                        custodia.Quantidade += quantidadeCompra;
                        //custodia.PrecoMedio = novoPrecoMedio;

                        resultadoFinal.Add($"Usuário {usuario.Id}: Comprou {quantidadeCompra} de {ativo.Ticker}");

                        var atualizarCustodia = new
                        {
                            contaGraficaId = conta.contaGraficaId,
                            ticker = ativo.Ticker,
                            quantidade = quantidadeCompra,
                            precoMedio = custodia.PrecoMedio
                        };

                        var content = new StringContent(JsonSerializer.Serialize(atualizarCustodia), Encoding.UTF8, "application/json");

                        await client.PostAsync("api/Custodia", content);
                    }
                }
            }
        }

        return View(resultadoFinal);
    }

    private async Task EnviarRebalanceamento(HttpClient client, long clienteId, string tickerVendido, string tickerComprado, decimal valorVenda)
    {
        var rebalanceamento = new
        {
            clienteId = clienteId,
            tipo = "DESVIO",
            tickerVendido = tickerVendido,
            tickerComprado = tickerComprado,
            valorVenda = valorVenda
        };

        await client.PostAsJsonAsync("api/Rebalanceamentos", rebalanceamento);
    }
}