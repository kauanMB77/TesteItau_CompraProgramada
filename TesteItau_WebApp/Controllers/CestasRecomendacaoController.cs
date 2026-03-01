using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TesteItau_WebApp.DAO;
using TesteItau_WebApp.Models;

namespace TesteItau_WebApp.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class CestasRecomendacaoController : Controller
	{
		private readonly AppDBContext _context;

		public CestasRecomendacaoController(AppDBContext context)
		{
			_context = context;
		}

		[HttpPost]
		public async Task<IActionResult> PostCesta(CestaRecomendacao cesta)
		{
			if (string.IsNullOrWhiteSpace(cesta.Nome))
				return BadRequest("Nome é obrigatório.");

			cesta.Id = 0;

			cesta.DataCriacao = DateTime.Now;

			cesta.Ativa = cesta.Ativa ?? true;

			_context.CestasRecomendacao.Add(cesta);
			await _context.SaveChangesAsync();

			return CreatedAtAction(nameof(GetCesta),
				new { id = cesta.Id }, cesta);
		}

		[HttpGet]
		public async Task<IActionResult> GetCestas()
		{
			var cestas = await _context.CestasRecomendacao
				.ToListAsync();

			return Ok(cestas);
		}

        [HttpGet("{id}")]
		public async Task<IActionResult> GetCesta(long id)
		{
			var cesta = await _context.CestasRecomendacao.FirstOrDefaultAsync(c => c.Id == id);

			if (cesta == null)
				return NotFound();

			return Ok(cesta);
		}

        [HttpGet("ativa")]
        public async Task<IActionResult> GetAtiva()
        {
            var cesta = await _context.CestasRecomendacao.FirstOrDefaultAsync(c => c.Ativa == true);

            if (cesta == null)
                return NotFound();

            return Ok(cesta);
        }

        [HttpPut("desativar/{id}")]
		public async Task<IActionResult> DesativarCesta(long id)
		{
			var cesta = await _context.CestasRecomendacao
				.FirstOrDefaultAsync(c => c.Id == id);

			if (cesta == null)
				return NotFound();

			cesta.Ativa = false;
			cesta.DataDesativacao = DateTime.Now;

			await _context.SaveChangesAsync();

			return Ok(cesta);
		}
    }
}
