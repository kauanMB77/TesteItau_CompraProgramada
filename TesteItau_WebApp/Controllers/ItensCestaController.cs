using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TesteItau_WebApp.DAO;
using TesteItau_WebApp.Models;

namespace TesteItau_WebApp.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ItensCestaController : Controller
	{
		private readonly AppDBContext _context;

		public ItensCestaController(AppDBContext context)
		{
			_context = context;
		}

		[HttpPost]
		public async Task<IActionResult> PostItemCesta(ItemCesta item)
		{
			var cestaExiste = await _context.CestasRecomendacao
				.AnyAsync(c => c.Id == item.CestaId);

			if (!cestaExiste)
				return BadRequest("CestaId informado não existe.");

			if (string.IsNullOrWhiteSpace(item.Ticker))
				return BadRequest("Ticker é obrigatório.");

			if (item.Percentual == null || item.Percentual <= 0 || item.Percentual > 100)
				return BadRequest("Percentual deve estar entre 0 e 100.");

			item.Id = 0;

			_context.ItensCesta.Add(item);
			await _context.SaveChangesAsync();

			return CreatedAtAction(nameof(GetItemCesta),
				new { id = item.Id }, item);
		}

		[HttpGet]
		public async Task<IActionResult> GetItensCesta()
		{
			var itens = await _context.ItensCesta
				.ToListAsync();

			return Ok(itens);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetItemCesta(long id)
		{
			var item = await _context.ItensCesta
				.FirstOrDefaultAsync(i => i.Id == id);

			if (item == null)
				return NotFound();

			return Ok(item);
		}

        [HttpGet("itensCesta/{id}")]
        public async Task<IActionResult> GetItemCestaPorId(long id)
        {
            var itens = await _context.ItensCesta.Where(i => i.CestaId == id).ToListAsync();

            if (itens == null || !itens.Any())
                return NotFound();

            return Ok(itens);
        }
    }
}
