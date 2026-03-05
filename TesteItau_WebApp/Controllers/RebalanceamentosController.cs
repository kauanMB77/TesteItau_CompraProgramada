using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TesteItau_WebApp.DAO;
using TesteItau_WebApp.Models;

namespace TesteItau_WebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RebalanceamentosController : ControllerBase
    {
        private readonly AppDBContext _context;

        public RebalanceamentosController(AppDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Adiciona um novo rebalanceamento
        /// </summary>
        /// <param name="rebalanceamento">
        /// Recebe um objeto Rebalanceamento com ClienteId, Tipo, TickerVendido,
        /// TickerComprado, ValorVenda e DataRebalanceamento
        /// </param>
        /// <returns>Informações do rebalanceamento criado</returns>
        /// <response code="201">Rebalanceamento criado com sucesso.</response>
        /// <response code="400">Falha ao criar rebalanceamento</response>
        [HttpPost]
        public async Task<IActionResult> PostRebalanceamento(Rebalanceamento rebalanceamento)
        {
            // Validando se o cliente existe
            var clienteExiste = await _context.Clientes.AnyAsync(c => c.Id == rebalanceamento.ClienteId);

            if (!clienteExiste)
                return BadRequest("ClienteId informado não existe.");

            // Validando Enum Tipo
            if (rebalanceamento.Tipo != "MUDANCA_CESTA" && rebalanceamento.Tipo != "DESVIO")
            {
                return BadRequest("Tipo deve ser 'MUDANCA_CESTA' ou 'DESVIO'.");
            }

            rebalanceamento.Id = 0;

            rebalanceamento.DataRebalanceamento = rebalanceamento.DataRebalanceamento ?? DateTime.Now;

            _context.Rebalanceamentos.Add(rebalanceamento);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetRebalanceamentos),
                new { id = rebalanceamento.Id },
                rebalanceamento
            );
        }

        /// <summary>
        /// Retorna todos os rebalanceamentos
        /// </summary>
        /// <returns>Lista com todos os rebalanceamentos</returns>
        /// <response code="200">Rebalanceamentos retornados com sucesso.</response>
        [HttpGet]
        public async Task<IActionResult> GetRebalanceamentos()
        {
            var rebalanceamentos = await _context.Rebalanceamentos
                .ToListAsync();

            return Ok(rebalanceamentos);
        }
    }
}
