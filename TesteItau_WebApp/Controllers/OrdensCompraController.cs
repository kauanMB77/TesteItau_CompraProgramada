using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TesteItau_WebApp.DAO;
using TesteItau_WebApp.Models;


namespace TesteItau_WebApp.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class OrdensCompraController : ControllerBase
	{
		private readonly AppDBContext _context;

		public OrdensCompraController(AppDBContext context)
		{
			_context = context;
		}

		[HttpPost]
		public async Task<IActionResult> PostOrdemCompra(OrdemCompra ordem)
		{
			var clienteExiste = await _context.Clientes.AnyAsync(c => c.Id == ordem.ContaMasterId);

			if (!clienteExiste)
				return BadRequest("ContaMasterId informado não existe.");

			if (ordem.TipoMercado != null && ordem.TipoMercado != "LOTE" && ordem.TipoMercado != "FRACIONARIO")
			{
				return BadRequest("TipoMercado deve ser 'LOTE' ou 'FRACIONARIO'.");
			}

			ordem.Id = 0;

			ordem.DataExecucao = ordem.DataExecucao ?? DateTime.Now;

			_context.OrdensCompra.Add(ordem);
			await _context.SaveChangesAsync();

			return CreatedAtAction(nameof(GetOrdemCompra),
				new { id = ordem.Id }, ordem);
		}

		[HttpGet]
		public async Task<IActionResult> GetOrdensCompra()
		{
			var ordens = await _context.OrdensCompra
				.ToListAsync();

			return Ok(ordens);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetOrdemCompra(long id)
		{
			var ordem = await _context.OrdensCompra
				.FirstOrDefaultAsync(o => o.Id == id);

			if (ordem == null)
				return NotFound();

			return Ok(ordem);
		}
	}
}
