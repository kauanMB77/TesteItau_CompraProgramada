using Microsoft.AspNetCore.Mvc;
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
    }
}
