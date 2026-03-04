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

        /// <summary>
        /// Adiciona um novo cliente na tabela, após adicionar o cliente é importante adicionar também a ContaGráfica e Usuário referente a este cliente.
        /// </summary>
        /// <param name="cliente"> Recebe um objeto Client com Nome, Cpf, Email, valorMensal e se está ativo ou desativado. A data de adesão é Datetime.Noew</param>
        /// <returns>Informações do cliente gerado</returns>
        /// <response code="200">Cliente criado com sucesso.</response>
        /// <response code="400">Falha ao criar o cliente, variáveis incorretas</response>
        [HttpPost]
        public async Task<IActionResult> PostCliente(Client cliente)
        {
            //Validações
            if (string.IsNullOrEmpty(cliente.Nome))
                return BadRequest("Nome é obrigatório.");

            if (string.IsNullOrEmpty(cliente.CPF))
                return BadRequest("CPF é obrigatório.");

            if (string.IsNullOrEmpty(cliente.Email))
                return BadRequest("Email é obrigatório.");

            if (string.IsNullOrEmpty(cliente.ValorMensal.ToString()))
                return BadRequest("ValorMensal é obrigatório.");

            cliente.DataAdesao = DateTime.Now;

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCliente), new { id = cliente.Id }, cliente);
        }

        /// <summary>
        /// Retorna uma lista Json com todos os clientes.
        /// </summary>
        /// <returns>Lista Json com os clientes</returns>
        /// <response code="200">Lista retornada com sucesso.</response>
        [HttpGet]
        public async Task<IActionResult> GetClientes()
        {
            var clientes = await _context.Clientes.ToListAsync();

            if (clientes == null || !clientes.Any())
                return NotFound();

            return Ok(clientes);
        }

        /// <summary>
        /// Retorna um cliente baseado no id enviado
        /// </summary>
        /// <param name="id"> Id do cliente a ser retornado</param>
        /// <returns>Json com o cliente informado</returns>
        /// <response code="200">Cliente retornado com sucesso.</response>
        /// <response code="400">Falha ao retornar o cliente, variáveis incorretas</response>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCliente(long id)
        {
            //Validações
            if (string.IsNullOrEmpty(id.ToString()))
                return BadRequest("Id é obrigatório.");

            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
                return NotFound();

            return Ok(cliente);
        }

        /// <summary>
        /// Retorna o Status de um cliente informado
        /// </summary>
        /// <param name="id"> Id do cliente a ser retornado</param>
        /// <returns>Retorna Id do cliente, se está ativo ou não e o valorn mensal de investimento/returns>
        /// <response code="200">Cliente retornado com sucesso.</response>
        /// <response code="400">Falha ao retornar o cliente, variáveis incorretas</response>
        [HttpGet("{id}/status")]
        public async Task<IActionResult> GetStatusCliente(long id)
        {
            //Validações
            if (string.IsNullOrEmpty(id.ToString()))
                return BadRequest("Id é obrigatório.");

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

        /// <summary>
        /// Desativa um cliente informado
        /// </summary>
        /// <param name="id"> Id do cliente a ser desativado</param>
        /// <response code="200">Cliente desativado com sucesso.</response>
        /// <response code="400">Falha ao desativar o cliente, variáveis incorretas</response>
        [HttpPut("{id}/desativar")]
        public async Task<IActionResult> DesativarConta(long id)
        {
            //Validações
            if (string.IsNullOrEmpty(id.ToString()))
                return BadRequest("Id é obrigatório.");

            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
                return NotFound(new { mensagem = "Cliente não encontrado." });

            if (!cliente.Ativo)
                return BadRequest(new { mensagem = "Cliente já está desativado." });

            cliente.Ativo = false;

            await _context.SaveChangesAsync();

            return Ok(new { mensagem = "Conta desativada com sucesso." });
        }

        /// <summary>
        /// Ativa um cliente informado.
        /// </summary>
        /// <param name="id"> Id do cliente a ser ativado.</param>
        /// <response code="200">Cliente ativado com sucesso.</response>
        /// <response code="400">Falha ao ativar o cliente, variáveis incorretas</response>
        [HttpPut("{id}/ativar")]
        public async Task<IActionResult> AtivarConta(long id)
        {
            //Validações
            if (string.IsNullOrEmpty(id.ToString()))
                return BadRequest("Id é obrigatório.");

            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
                return NotFound(new { mensagem = "Cliente não encontrado." });

            if (cliente.Ativo)
                return BadRequest(new { mensagem = "Cliente já está ativo." });

            cliente.Ativo = true;

            await _context.SaveChangesAsync();

            return Ok(new { mensagem = "Conta ativada com sucesso." });
        }

        /// <summary>
        /// Atualiza o ValorMensal de um cliente.
        /// </summary>
        /// <param name="id"> Id do Cliente a ser alterado.</param>
        /// <param name="novoValor"> Novo ValorMensal do cliente.</param>
        /// <returns>Novo valor mensal</returns>
        /// <response code="200">Valor alterado com sucesso.</response>
        /// <response code="400">Falha ao alterar o valor, variáveis incorretas</response>
        [HttpPut("{id}/valor")]
        public async Task<IActionResult> AtualizarValorInvestimento(long id, [FromBody] decimal novoValor)
        {
            //Validações
            if (string.IsNullOrEmpty(id.ToString()))
                return BadRequest("Id é obrigatório.");

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

        /// <summary>
        /// Retorna o id de um cliente, passando apenas o Email
        /// </summary>
        /// <param name="email"> Email do cliente.</param>
        /// <returns>ClienteId/returns>
        /// <response code="200">Cliente retornado com sucesso.</response>
        /// <response code="400">Falha ao retornar o cliente, variáveis incorretas</response>
        [HttpGet("{email}/Id")]
        public async Task<IActionResult> GetIdWithEmail(string email)
        {
            //Validações
            if (string.IsNullOrEmpty(email))
                return BadRequest("email é obrigatório.");

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
