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

        /// <summary>
        /// Adiciona uma nova Ordem de Compra a tabela
        /// </summary>
        /// <param name="ordem"> Recebe um objeto OrdemCompra com contaMasterId, ticker, quantidade, precoUnitario e TipoMercado</param>
        /// <returns>Informações da compra gerada</returns>
        /// <response code="200">Compra gerada com sucesso.</response>
        /// <response code="400">Falha ao gerar compra, variáveis incorretas</response>
        [HttpPost]
		public async Task<IActionResult> PostOrdemCompra(OrdemCompra ordem)
		{
			//Validando a contaMasterId
			var clienteExiste = await _context.Clientes.AnyAsync(c => c.Id == ordem.ContaMasterId);
			if (!clienteExiste)
				return BadRequest("ContaMasterId informado não existe.");
			
			//Validando o Enum TipoMercado
			if (ordem.TipoMercado != null && ordem.TipoMercado != "LOTE" && ordem.TipoMercado != "FRACIONARIO")
			{
				return BadRequest("TipoMercado deve ser 'LOTE' ou 'FRACIONARIO'.");
			}

			ordem.Id = 0;

			ordem.DataExecucao = ordem.DataExecucao ?? DateTime.Now;

			_context.OrdensCompra.Add(ordem);
			await _context.SaveChangesAsync();

			return CreatedAtAction(nameof(GetOrdemCompra),new { id = ordem.Id }, ordem);
		}

        /// <summary>
        /// Retorna todas as ordens de compra
        /// </summary>
        /// <returns>Json com todas as ordens de compra</returns>
        /// <response code="200">Compras retornadas com sucesso.</response>
        [HttpGet]
		public async Task<IActionResult> GetOrdensCompra()
		{
			var ordens = await _context.OrdensCompra
				.ToListAsync();

			return Ok(ordens);
		}

        /// <summary>
        /// Retorna uma Ordem de compra dado certo Id
        /// </summary>
        /// <param name="id"> Id da ordem de compra</param>
        /// <returns>Informações da Ordem de compra</returns>
        /// <response code="200">Compra retornada com sucesso.</response>
        /// <response code="400">Falha ao retornar compra, variáveis incorretas</response>
        [HttpGet("{id}")]
		public async Task<IActionResult> GetOrdemCompra(long id)
		{
			var ordem = await _context.OrdensCompra.FirstOrDefaultAsync(o => o.Id == id);

			if (ordem == null)
				return NotFound();

			return Ok(ordem);
		}

        /// <summary>
        /// Retorna a última ordem de compra de um determinado ticker
        /// </summary>
        /// <param name="ticker">Ticker a ser retornado</param>
        /// <returns>Última ordem de compra com o ticker</returns>
        /// <response code="200">Compra retornada com sucesso.</response>
        /// <response code="404">Falha ao retornar compra, variáveis incorretas</response>
        [HttpGet("ultimo/{ticker}")]
        public async Task<IActionResult> GetUltimaOrdemPorTicker(string ticker)
        {
            var ordem = await _context.OrdensCompra.Where(o => o.Ticker == ticker).OrderByDescending(o => o.Id).FirstOrDefaultAsync();

            if (ordem == null)
                return NotFound($"Nenhuma ordem encontrada para o ticker {ticker}");

            return Ok(ordem);
        }
    }
}
