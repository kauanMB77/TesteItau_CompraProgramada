using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TesteItau_WebApp.DAO;
using TesteItau_WebApp.Models;

namespace TesteItau_WebApp.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class CotacoesController : Controller
	{
		private readonly AppDBContext _context;

		public CotacoesController(AppDBContext context)
		{
			_context = context;
		}

        /// <summary>
        /// Adiciona uma nova Cotacao na tabela.
        /// </summary>
        /// <param name="cotacao"> Recebe um objeto Cotacao com dataPregao, ticker, precoAbertura, precoFechamento, precoMaximo, precoMinimo</param>
        /// <returns>Informações da cotacao gerada</returns>
        /// <response code="200">cotacao gerada com sucesso.</response>
        /// <response code="400">Falha ao gerar a cotacao, variáveis incorretas</response>
        [HttpPost]
		public async Task<IActionResult> PostCotacao(Cotacao cotacao)
		{
			//Validações
			//Validando o Ticker, ps eu não verifico se o ticker existe, apenas se esta em branco
			if (string.IsNullOrWhiteSpace(cotacao.Ticker))
				return BadRequest("Ticker é obrigatório.");

            //Validando todos os valores da cotacao
            if (cotacao.PrecoAbertura <= 0 || cotacao.PrecoFechamento <= 0 || cotacao.PrecoMaximo <= 0 || cotacao.PrecoMinimo <= 0)
				return BadRequest("Os preços devem ser maiores que zero.");

			cotacao.Id = 0;

			_context.Cotacoes.Add(cotacao);
			await _context.SaveChangesAsync();

			return CreatedAtAction(nameof(GetCotacao), new { id = cotacao.Id }, cotacao);
		}

        /// <summary>
        /// Retorna todas as Cotacoes
        /// </summary>
        /// <returns>Json com todas as Cotacoes</returns>
        /// <response code="200">cotacoes retornadas com sucesso.</response>
        [HttpGet]
		public async Task<IActionResult> GetCotacoes()
		{
			var cotacoes = await _context.Cotacoes
				.ToListAsync();

			return Ok(cotacoes);
		}

        /// <summary>
        /// Retorna a cotacao dado um determinado ID
        /// </summary>
        /// <param name="id"> Id da cotacao</param>
        /// <returns>Informações da cotacao</returns>
        /// <response code="200">Cotacao retornada com sucesso.</response>
        /// <response code="400">Falha ao retornar a cotacao, variáveis incorretas</response>
        [HttpGet("{id}")]
		public async Task<IActionResult> GetCotacao(long id)
		{
            //Validando o Id
            if (string.IsNullOrEmpty(id.ToString()))
                return BadRequest("Id é obrigatório.");

            var cotacao = await _context.Cotacoes.FirstOrDefaultAsync(c => c.Id == id);

			if (cotacao == null)
				return NotFound();

			return Ok(cotacao);
		}
	}
}
