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

        // ================================
        // 1 - Buscar Cesta ativa
        // ================================
        var cestaAtivaResp = await client.GetAsync("api/CestasRecomendacao/ativa");
        var cestaAtivaJson = await cestaAtivaResp.Content.ReadAsStringAsync();
        var cestaAtiva = JsonSerializer.Deserialize<CestaViewModel>(
            cestaAtivaJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // ================================
        // 2 - Buscar itens da cesta
        // ================================
        var itensResp = await client.GetAsync($"api/ItensCesta/itensCesta/{cestaAtiva.Id}");
        var itensJson = await itensResp.Content.ReadAsStringAsync();
        var itensCesta = JsonSerializer.Deserialize<List<CestaTopFiveViewModel>>(
            itensJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // ================================
        // 3 - Buscar cotações
        // ================================
        var cotacoesResp = await client.GetAsync("api/Cotacoes");
        var cotacoesJson = await cotacoesResp.Content.ReadAsStringAsync();
        var cotacoes = JsonSerializer.Deserialize<List<CotacaoViewModel>>(
            cotacoesJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Armazena preço de fechamento por ticker
        var precosFechamento = cotacoes.GroupBy(c => c.Ticker).ToDictionary(g => g.Key,g => g.First().PrecoFechamento);

        // ================================
        // 4 - Buscar usuários
        // ================================
        var usuariosResp = await client.GetAsync("api/Clientes");
        var usuariosJson = await usuariosResp.Content.ReadAsStringAsync();
        var usuarios = JsonSerializer.Deserialize<List<ClienteViewModel>>(
            usuariosJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var usuariosAtivos = usuarios
            .Where(u => u.Ativo && u.ValorMensal > 0)
            .ToList();

        foreach (var usuario in usuariosAtivos)
        {
            // ================================
            // Buscar Conta Gráfica
            // ================================
            var contaResp = await client.GetAsync($"api/ContasGraficas/cliente/{usuario.Id}");

            if (!contaResp.IsSuccessStatusCode)
                continue;

            var contaJson = await contaResp.Content.ReadAsStringAsync();

            var conta = JsonSerializer.Deserialize<ContaGraficaViewModel>(
                contaJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (conta == null)
                continue;

            // ================================
            // Buscar Custodias
            // ================================
            var custodiaResp = await client.GetAsync($"api/Custodia/conta/{conta.contaGraficaId}");
            var custodiaJson = await custodiaResp.Content.ReadAsStringAsync();
            var custodias = JsonSerializer.Deserialize<List<CustodiaViewModel>>(custodiaJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!custodias.Any())
                continue;

            // ================================
            // 5 - Calcular valor total da carteira
            // ================================
            decimal valorTotal = custodias.Sum(c =>
                c.Quantidade * precosFechamento[c.Ticker]);

            decimal caixa = 0;

            // ================================
            // 5.1 - PRIMEIRO PASSO: VENDAS
            // ================================
            foreach (var item in itensCesta)
            {
                var custodia = custodias
                    .FirstOrDefault(c => c.Ticker == item.Ticker);

                if (custodia == null)
                    continue;

                decimal precoAtual = precosFechamento[item.Ticker];

                decimal valorAtual = custodia.Quantidade * precoAtual;
                decimal valorAlvo = (item.Percentual / 100m) * valorTotal;

                decimal diferencaValor = valorAtual - valorAlvo;
                decimal percentualAtual = (valorAtual / valorTotal) * 100;
                decimal diferencaPercentual = percentualAtual - item.Percentual;

                if (diferencaPercentual > 5) // ACIMA → VENDER
                {
                    decimal valorVenda = diferencaValor;

                    int quantidadeVenda = (int)Math.Floor(valorVenda / precoAtual);

                    if (quantidadeVenda <= 0)
                        continue;

                    // Atualiza caixa
                    caixa += quantidadeVenda * precoAtual;

                    // Atualiza custodia local
                    custodia.Quantidade -= quantidadeVenda;

                    resultadoFinal.Add(
                        $"Usuário {usuario.Id}: Vendeu {quantidadeVenda} de {item.Ticker}");

                    // Envia apenas diferença negativa
                    var atualizarCustodia = new
                    {
                        contaGraficaId = conta.contaGraficaId,
                        ticker = item.Ticker,
                        quantidade = -quantidadeVenda
                    };

                    var content = new StringContent(
                        JsonSerializer.Serialize(atualizarCustodia),
                        Encoding.UTF8,
                        "application/json");

                    await client.PostAsync("api/Custodia", content);
                }

                //Recalcula o valor total
                valorTotal = custodias.Sum(c => c.Quantidade * precosFechamento[c.Ticker]);
            }
            // ================================
            // 5.2 - SEGUNDO PASSO: COMPRAS PROPORCIONAIS
            // ================================

            if (caixa > 0)
            {
                // Recalcula valor total após vendas
                valorTotal = custodias.Sum(c =>
                    c.Quantidade * precosFechamento[c.Ticker]);

                var ativosParaComprar = new List<(string Ticker, decimal DeficitValor)>();

                foreach (var item in itensCesta)
                {
                    decimal precoAtual = precosFechamento[item.Ticker];

                    var custodia = custodias
                        .FirstOrDefault(c => c.Ticker == item.Ticker);

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

                        caixa -= valorExecutado;

                        var custodia = custodias
                            .FirstOrDefault(c => c.Ticker == ativo.Ticker);

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

                        // ================================
                        // 🔥 RECALCULAR PREÇO MÉDIO (SOMENTE NA COMPRA)
                        // ================================

                        decimal quantidadeAtual = custodia.Quantidade;
                        decimal precoMedioAtual = custodia.PrecoMedio;

                        decimal novoPrecoMedio = precoAtual;

                        if (quantidadeAtual > 0)
                        {
                            novoPrecoMedio =
                                ((quantidadeAtual * precoMedioAtual) +
                                 (quantidadeCompra * precoAtual))
                                / (quantidadeAtual + quantidadeCompra);
                        }

                        custodia.Quantidade += quantidadeCompra;
                        custodia.PrecoMedio = novoPrecoMedio;

                        resultadoFinal.Add(
                            $"Usuário {usuario.Id}: Comprou {quantidadeCompra} de {ativo.Ticker}");

                        var atualizarCustodia = new
                        {
                            contaGraficaId = conta.contaGraficaId,
                            ticker = ativo.Ticker,
                            quantidade = quantidadeCompra,
                            precoMedio = novoPrecoMedio
                        };

                        var content = new StringContent(
                            JsonSerializer.Serialize(atualizarCustodia),
                            Encoding.UTF8,
                            "application/json");

                        await client.PostAsync("api/Custodia", content);
                    }
                }
            }
        }

        return View(resultadoFinal);
    }
}