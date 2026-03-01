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

		[HttpPost]
		public async Task<IActionResult> PostCustodia(Custodia custodia)
		{
			var contaExiste = await _context.ContasGraficas.AnyAsync(c => c.Id == custodia.ContaGraficaId);

			if (!contaExiste)
				return BadRequest("ContaGraficaId informado não existe.");

			custodia.Id = 0;

			custodia.DataUltimaAtualizacao = DateTime.Now;

			_context.Custodias.Add(custodia);
			await _context.SaveChangesAsync();

			return CreatedAtAction(nameof(GetCustodia), new { id = custodia.Id }, custodia);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetCustodia(long id)
		{
			var custodia = await _context.Custodias.Include(c => c.ContaGrafica).FirstOrDefaultAsync(c => c.Id == id);

			if (custodia == null)
				return NotFound();

			return Ok(custodia);
		}

		[HttpGet]
		public async Task<IActionResult> GetCustodias()
		{
			var custodias = await _context.Custodias.Include(c => c.ContaGrafica).ToListAsync();

			return Ok(custodias);
		}

        [HttpGet("conta/{contaGraficaId}")]
        public async Task<IActionResult> GetCustodiasByContaGrafica(long contaGraficaId)
        {
            var contaExiste = await _context.ContasGraficas
                .AnyAsync(c => c.Id == contaGraficaId);

            if (!contaExiste)
                return NotFound("Conta gráfica não encontrada.");

            var custodias = await _context.Custodias
                .Where(c => c.ContaGraficaId == contaGraficaId)
                .ToListAsync();

            return Ok(custodias);
        }
    }
}
