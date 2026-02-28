using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TesteItau_WebApp.DAO;
using TesteItau_WebApp.Models;

namespace TesteItau_WebApp.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class DistribuicoesController : Controller
	{
		private readonly AppDBContext _context;

		public DistribuicoesController(AppDBContext context)
		{
			_context = context;
		}

		[HttpPost]
		public async Task<IActionResult> PostDistribuicao(Distribuicao distribuicao)
		{
			var ordemExiste = await _context.OrdensCompra.AnyAsync(o => o.Id == distribuicao.OrdemCompraId);

			if (!ordemExiste)
				return BadRequest("OrdemCompraId informado não existe.");

			var custodiaExiste = await _context.Custodias.AnyAsync(c => c.Id == distribuicao.CustodiaFilhoteId);

			if (!custodiaExiste)
				return BadRequest("CustodiaFilhoteId informado não existe.");

			distribuicao.Id = 0;

			distribuicao.DataDistribuicao = distribuicao.DataDistribuicao ?? DateTime.Now;

			_context.Distribuicoes.Add(distribuicao);
			await _context.SaveChangesAsync();

			return CreatedAtAction(nameof(GetDistribuicao),
				new { id = distribuicao.Id }, distribuicao);
		}

		[HttpGet]
		public async Task<IActionResult> GetDistribuicoes()
		{
			var distribuicoes = await _context.Distribuicoes.ToListAsync();

			return Ok(distribuicoes);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetDistribuicao(long id)
		{
			var distribuicao = await _context.Distribuicoes.FirstOrDefaultAsync(d => d.Id == id);

			if (distribuicao == null)
				return NotFound();

			return Ok(distribuicao);
		}
	}
}
