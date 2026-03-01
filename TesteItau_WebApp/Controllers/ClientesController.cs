using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TesteItau_WebApp.DAO;
using TesteItau_WebApp.Models;

namespace TesteItau_WebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly AppDBContext _context;

        public ClientesController(AppDBContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> PostCliente(Client cliente)
        {
            cliente.DataAdesao = DateTime.Now;

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCliente), new { id = cliente.Id }, cliente);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCliente(long id)
        {
            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
                return NotFound();

            return Ok(cliente);
        }

        [HttpGet("{id}/status")]
        public async Task<IActionResult> GetStatusCliente(long id)
        {
            var cliente = await _context.Clientes
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    c.Ativo,
                    c.ValorMensal
                })
                .FirstOrDefaultAsync();

            if (cliente == null)
                return NotFound(new { mensagem = "Cliente não encontrado." });

            return Ok(new
            {
                clienteId = cliente.Id,
                ativo = cliente.Ativo,
                valorInvestido = cliente.ValorMensal
            });
        }

        [HttpPut("{id}/desativar")]
        public async Task<IActionResult> DesativarConta(long id)
        {
            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
                return NotFound(new { mensagem = "Cliente não encontrado." });

            if (!cliente.Ativo)
                return BadRequest(new { mensagem = "Cliente já está desativado." });

            cliente.Ativo = false;

            await _context.SaveChangesAsync();

            return Ok(new { mensagem = "Conta desativada com sucesso." });
        }

        [HttpPut("{id}/ativar")]
        public async Task<IActionResult> AtivarConta(long id)
        {
            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
                return NotFound(new { mensagem = "Cliente não encontrado." });

            if (cliente.Ativo)
                return BadRequest(new { mensagem = "Cliente já está ativo." });

            cliente.Ativo = true;

            await _context.SaveChangesAsync();

            return Ok(new { mensagem = "Conta ativada com sucesso." });
        }

        [HttpPut("{id}/valor")]
        public async Task<IActionResult> AtualizarValorInvestimento(long id, [FromBody] decimal novoValor)
        {
            if (novoValor <= 0)
                return BadRequest(new { mensagem = "O valor deve ser maior que zero." });

            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
                return NotFound(new { mensagem = "Cliente não encontrado." });

            cliente.ValorMensal = novoValor;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensagem = "Valor atualizado com sucesso.",
                novoValor = cliente.ValorMensal
            });
        }

        [HttpGet("{email}/Id")]
        public async Task<IActionResult> GetIdWithEmail(string email)
        {
            var cliente = await _context.Clientes
                .Where(c => c.Email == email)
                .Select(c => new
                {
                    c.Id
                })
                .FirstOrDefaultAsync();

            if (cliente == null)
                return NotFound(new { mensagem = "Cliente não encontrado." });

            return Ok(new
            {
                clienteId = cliente.Id
            });
        }
    }
}
