using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using TesteItau_WebApp.DAO;
using TesteItau_WebApp.Models;

namespace TesteItau_WebApp.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class CustodiaController : ControllerBase
	{
		private readonly AppDBContext _context;

		public CustodiaController(AppDBContext context)
		{
			_context = context;
		}

        /// <summary>
        /// Adiciona uma nova Custodia a tabela
        /// </summary>
        /// <param name="novaCustodia"> Recebe um objeto Custodia com contaGraficaId, ticker, quantidade e precoMedio</param>
        /// <returns>Informações da custodia Gerada</returns>
        /// <response code="200">Custodia gerada com sucesso.</response>
        /// <response code="400">Falha ao gerar a custodia, variáveis incorretas</response>
        [HttpPost]
        public async Task<IActionResult> PostCustodia(Custodia novaCustodia)
        {
            //Validando se existe a conta informada
            var contaExiste = await _context.ContasGraficas.AnyAsync(c => c.Id == novaCustodia.ContaGraficaId);
            if (!contaExiste)
                return BadRequest("ContaGraficaId informado não existe.");


            var custodiaExistente = await _context.Custodias.FirstOrDefaultAsync(c =>c.ContaGraficaId == novaCustodia.ContaGraficaId && c.Ticker == novaCustodia.Ticker);

            //Aqui eu valido se a custodia com aquele Ticker já existe, caso exista eu apenas atualizo ela com um novo PrecoMedio e quantidade, caso não crio uma nova.
            //Estou fazendo isso para manter cada ContaGrafica sempre com 5 registros, para facilitar as contas, partes gráficas, etc.
            //É mais fácil fazer esse controle agora, do que toda vez ter que listar todas as custodias com aquele ticker e recalcular o precoMedio
            if (custodiaExistente == null)
            {
                //Em praticamente todas as rotas eu defino o id = 0, mesmo ele sendo BIGINT Increment, faço isso apenas para tentar evitar algum problema de BadRequest id = null
                novaCustodia.Id = 0;
                novaCustodia.DataUltimaAtualizacao = DateTime.Now;

                _context.Custodias.Add(novaCustodia);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCustodia), new { id = novaCustodia.Id }, novaCustodia);
            }
            else
            {
                //Calculo da nova Quantidade e PrecoMedio
                int quantidadeAtual = custodiaExistente.Quantidade;
                decimal precoMedioAtual = custodiaExistente.PrecoMedio;

                int novaQuantidade = novaCustodia.Quantidade;
                decimal novoPreco = novaCustodia.PrecoMedio;

                int quantidadeFinal = quantidadeAtual + novaQuantidade;

                decimal novoPrecoMedio = ((quantidadeAtual * precoMedioAtual) + (novaQuantidade * novoPreco))/quantidadeFinal;

                custodiaExistente.Quantidade = quantidadeFinal;
                custodiaExistente.PrecoMedio = Math.Round(novoPrecoMedio, 4);
                custodiaExistente.DataUltimaAtualizacao = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(custodiaExistente);
            }
        }

        /// <summary>
        /// Retorna uma custodia dado seu Id
        /// </summary>
        /// <param name="id"> Id da custodia</param>
        /// <returns>Informacoes da custodia</returns>
        /// <response code="200">Custodia retornada com sucesso.</response>
        [HttpGet("{id}")]
		public async Task<IActionResult> GetCustodia(long id)
		{
			var custodia = await _context.Custodias.Include(c => c.ContaGrafica).FirstOrDefaultAsync(c => c.Id == id);

			if (custodia == null)
				return NotFound();

			return Ok(custodia);
		}

        /// <summary>
        /// Retorna um json com todas as custodias
        /// </summary>
        /// <returns>Retorna um json com todas as custodias</returns>
        /// <response code="200">Custodias retornadas com sucesso.</response>
        [HttpGet]
		public async Task<IActionResult> GetCustodias()
		{
			var custodias = await _context.Custodias.Include(c => c.ContaGrafica).ToListAsync();

			return Ok(custodias);
		}

        /// <summary>
        /// Lista as custodias de determinada contaGrafica
        /// </summary>
        /// <param name="contaGraficaId"> Id da contaGrafica</param>
        /// <returns>Json com as Custodias desta conta</returns>
        /// <response code="200">Custodias retornadas com sucesso.</response>
        /// <response code="400">Falha ao retornar as custodias, variáveis incorretas</response>
        [HttpGet("conta/{contaGraficaId}")]
        public async Task<IActionResult> GetCustodiasByContaGrafica(long contaGraficaId)
        {
            //Validando se existe conta com o id informado
            var contaExiste = await _context.ContasGraficas.AnyAsync(c => c.Id == contaGraficaId);
            if (!contaExiste)
                return NotFound("Conta gráfica não encontrada.");

            var custodias = await _context.Custodias.Where(c => c.ContaGraficaId == contaGraficaId).ToListAsync();

            //Pode ser que retorne null caso a conta não possua custodia, não estou gerando erro aqui, mantenho esse controle na UI ou controller do MVC
            //Para que não tenha nenhum problema com o motor de compra ou Rebalanceamento
            return Ok(custodias);
        }

        /// <summary>
        /// Limpa todas as custodias de certa contaGrafica
        /// </summary>
        /// <param name="contaGraficaId"> Id da ContaGrafica</param>
        /// <param name="ticker"> Ticker da acao</param>
        /// <response code="200">Custodias limpas com sucesso.</response>
        /// <response code="400">Falha ao limpar as custodias, variáveis incorretas</response>
        [HttpDelete("clear/{contaGraficaId}/{ticker}")]
        public async Task<IActionResult> DeleteAllCustodiasById(long  contaGraficaId, string ticker)
        {
            //Validando se existe conta com o id informado
            var custodias = await _context.Custodias.Where(c=> c.ContaGraficaId == contaGraficaId && c.Ticker == ticker).ToListAsync();
            if (!custodias.Any())
                return NotFound("Nenhuma custodia encontrada para o Id informado");

            //Validando o Ticker
            if (string.IsNullOrEmpty(ticker))
                return BadRequest("ticker é obrigatório.");

            _context.Custodias.RemoveRange(custodias);
            await _context.SaveChangesAsync();

            return Ok("Custodias limpas para o Id informado");
        }
    }
}
