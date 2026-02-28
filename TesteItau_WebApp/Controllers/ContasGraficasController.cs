using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TesteItau_WebApp.DAO;
using TesteItau_WebApp.Models;

namespace TesteItau_WebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContasGraficasController : ControllerBase
    {
        private readonly AppDBContext _context;

        public ContasGraficasController(AppDBContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> PostContaGrafica(ContaGrafica conta)
        {
            var clienteExiste = await _context.Clientes
                .AnyAsync(c => c.Id == conta.ClienteId);

            if (!clienteExiste)
                return BadRequest("ClienteId informado não existe.");

            var jaExisteConta = await _context.ContasGraficas
                .AnyAsync(c => c.ClienteId == conta.ClienteId);
            if (jaExisteConta)
                return BadRequest("Este cliente já possui uma conta gráfica.");

            var numeroContaDuplicado = await _context.ContasGraficas
                .AnyAsync(c => c.NumeroConta == conta.NumeroConta);
            if (numeroContaDuplicado)
                return BadRequest("Número da conta já cadastrado.");

            conta.DataCriacao = DateTime.Now;

            _context.ContasGraficas.Add(conta);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetContaGrafica), new { id = conta.Id }, conta);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetContaGrafica(long id)
        {
            var conta = await _context.ContasGraficas.Include(c => c.Cliente).FirstOrDefaultAsync(c => c.Id == id);

            if (conta == null)
                return NotFound();

            return Ok(conta);
        }

        [HttpGet]
        public async Task<IActionResult> GetContas()
        {
            var contas = await _context.ContasGraficas.Include(c => c.Cliente).ToListAsync();

            return Ok(contas);
        }
    }
}
