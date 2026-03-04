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
        public async Task<IActionResult> PostCustodia(Custodia novaCustodia)
        {
            var contaExiste = await _context.ContasGraficas
                .AnyAsync(c => c.Id == novaCustodia.ContaGraficaId);

            if (!contaExiste)
                return BadRequest("ContaGraficaId informado não existe.");
            var custodiaExistente = await _context.Custodias.FirstOrDefaultAsync(c =>c.ContaGraficaId == novaCustodia.ContaGraficaId && c.Ticker == novaCustodia.Ticker);

            if (custodiaExistente == null)
            {

                novaCustodia.Id = 0;
                novaCustodia.DataUltimaAtualizacao = DateTime.Now;

                _context.Custodias.Add(novaCustodia);
                await _context.SaveChangesAsync();

                return CreatedAtAction(
                    nameof(GetCustodia),
                    new { id = novaCustodia.Id },
                    novaCustodia);
            }
            else
            {

                var quantidadeAtual = custodiaExistente.Quantidade;
                var precoMedioAtual = custodiaExistente.PrecoMedio;

                var novaQuantidade = novaCustodia.Quantidade;
                var novoPreco = novaCustodia.PrecoMedio;

                var quantidadeFinal = quantidadeAtual + novaQuantidade;

                var novoPrecoMedio =
                    ((quantidadeAtual * precoMedioAtual) +
                     (novaQuantidade * novoPreco))
                    / quantidadeFinal;

                custodiaExistente.Quantidade = quantidadeFinal;
                custodiaExistente.PrecoMedio = Math.Round(novoPrecoMedio, 4);
                custodiaExistente.DataUltimaAtualizacao = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(custodiaExistente);
            }
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

        [HttpDelete("clear/{contaGraficaId}/{ticker}")]
        public async Task<IActionResult> DeleteAllCustodiasById(long  contaGraficaId, string ticker)
        {
            var custodias = await _context.Custodias.Where(c=> c.ContaGraficaId == contaGraficaId && c.Ticker == ticker).ToListAsync();

            if (!custodias.Any())
                return NotFound("Nenhuma custodia encontrada para o Id informado");

            _context.Custodias.RemoveRange(custodias);
            await _context.SaveChangesAsync();

            return Ok("Custodias limpas para o Id informado");
        }
    }
}
