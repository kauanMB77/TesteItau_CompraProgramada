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

        /// <summary>
        /// Adiciona uma nova ContaGrafica na tabela.
        /// </summary>
        /// <param name="conta"> Recebe um objeto ContaGrafica com clienteId, numeroConta e tipo</param>
        /// <returns>Informações da conta gerada</returns>
        /// <response code="200">Conta gerada com sucesso.</response>
        /// <response code="400">Falha ao gerar a conta, variáveis incorretas</response>
        [HttpPost]
        public async Task<IActionResult> PostContaGrafica(ContaGrafica conta)
        {   
            //Validando a existencia do ClienteId
            var clienteExiste = await _context.Clientes.AnyAsync(c => c.Id == conta.ClienteId);
            if (!clienteExiste)
                return BadRequest("ClienteId informado não existe.");

            //Validando se o id já possui conta vinculada
            var jaExisteConta = await _context.ContasGraficas.AnyAsync(c => c.ClienteId == conta.ClienteId);
            if (jaExisteConta)
                return BadRequest("Este cliente já possui uma conta gráfica.");

            //Validando se o numeroConta é UNIQUE para salvar no banco
            var numeroContaDuplicado = await _context.ContasGraficas.AnyAsync(c => c.NumeroConta == conta.NumeroConta);
            if (numeroContaDuplicado)
                return BadRequest("Número da conta já cadastrado.");

            conta.DataCriacao = DateTime.Now;

            _context.ContasGraficas.Add(conta);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetContaGrafica), new { id = conta.Id }, conta);
        }

        /// <summary>
        /// Retorna contaGrafica dado certo Id
        /// </summary>
        /// <param name="id"> Id da conta gráfica</param>
        /// <returns>Informações da conta informada</returns>
        /// <response code="200">Conta retornada com sucesso.</response>
        /// <response code="400">Falha ao retornar a conta, variáveis incorretas</response>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetContaGrafica(long id)
        {
            //Validando o Id
            if (string.IsNullOrEmpty(id.ToString()))
                return BadRequest("Id é obrigatório.");

            var conta = await _context.ContasGraficas.Include(c => c.Cliente).FirstOrDefaultAsync(c => c.Id == id);

            if (conta == null)
                return NotFound();

            return Ok(conta);
        }

        /// <summary>
        /// Retorna Json com todas as Contas
        /// </summary>
        /// <returns>Json com todas as contas</returns>
        /// <response code="200">Contas retornadas com sucesso.</response>
        [HttpGet]
        public async Task<IActionResult> GetContas()
        {
            var contas = await _context.ContasGraficas.Include(c => c.Cliente).ToListAsync();

            return Ok(contas);
        }

        /// <summary>
        /// Retorna uma contaGrafica informando um clienteId
        /// </summary>
        /// <param name="clienteId"> Id do cliente informado</param>
        /// <returns>Informações da contaGrafica</returns>
        /// <response code="200">Conta retornada com sucesso.</response>
        /// <response code="400">Falha ao retornar a conta, variáveis incorretas</response>
        [HttpGet("cliente/{clienteId}")]
        public async Task<IActionResult> GetContaGraficaIdByCliente(long clienteId)
        {
            //Validando o Id
            if (string.IsNullOrEmpty(clienteId.ToString()))
                return BadRequest("Id é obrigatório.");

            var contaId = await _context.ContasGraficas
                .Where(c => c.ClienteId == clienteId)
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            //Valida se existe conta com esse ClienteId
            if (contaId == 0)
                return NotFound("Conta gráfica não encontrada para este cliente.");

            return Ok(new { ContaGraficaId = contaId });
        }
    }
}
