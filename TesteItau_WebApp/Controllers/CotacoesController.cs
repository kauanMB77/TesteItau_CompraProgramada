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

		[HttpPost]
		public async Task<IActionResult> PostCotacao(Cotacao cotacao)
		{
			if (string.IsNullOrWhiteSpace(cotacao.Ticker))
				return BadRequest("Ticker é obrigatório.");

			if (cotacao.PrecoMinimo > cotacao.PrecoMaximo)
				return BadRequest("Preço mínimo não pode ser maior que o preço máximo.");

			if (cotacao.PrecoAbertura <= 0 ||
				cotacao.PrecoFechamento <= 0 ||
				cotacao.PrecoMaximo <= 0 ||
				cotacao.PrecoMinimo <= 0)
				return BadRequest("Os preços devem ser maiores que zero.");

			cotacao.Id = 0;

			_context.Cotacoes.Add(cotacao);
			await _context.SaveChangesAsync();

			return CreatedAtAction(nameof(GetCotacao),
				new { id = cotacao.Id }, cotacao);
		}

		[HttpGet]
		public async Task<IActionResult> GetCotacoes()
		{
			var cotacoes = await _context.Cotacoes
				.ToListAsync();

			return Ok(cotacoes);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetCotacao(long id)
		{
			var cotacao = await _context.Cotacoes
				.FirstOrDefaultAsync(c => c.Id == id);

			if (cotacao == null)
				return NotFound();

			return Ok(cotacao);
		}
	}
}
