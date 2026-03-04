using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TesteItau_WebApp.DAO;
using TesteItau_WebApp.Models;

namespace TesteItau_WebApp.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ItensCestaController : Controller
	{
		private readonly AppDBContext _context;

		public ItensCestaController(AppDBContext context)
		{
			_context = context;
		}

        /// <summary>
        /// Adiciona um novo Item a Cesta
        /// </summary>
        /// <param name="item"> Recebe um objeto ItemCesta com cestaId, ticker e percentual</param>
        /// <returns>Informações do Item gerado</returns>
        /// <response code="200">Item gerado com sucesso.</response>
        /// <response code="400">Falha ao gerar o item, variáveis incorretas</response>
        [HttpPost]
		public async Task<IActionResult> PostItemCesta(ItemCesta item)
		{
			//Validando se a Cesta existe, lembrando que esse Id é do CestasRecomendacao
			var cestaExiste = await _context.CestasRecomendacao.AnyAsync(c => c.Id == item.CestaId);
			if (!cestaExiste)
				return BadRequest("CestaId informado não existe.");

			//Validando a existencia do ticker
			if (string.IsNullOrEmpty(item.Ticker))
				return BadRequest("Ticker é obrigatório.");

			//Validando o percentual
			if (item.Percentual == null || item.Percentual <= 0 || item.Percentual > 100)
				return BadRequest("Percentual deve estar entre 0 e 100.");

			item.Id = 0;

			_context.ItensCesta.Add(item);
			await _context.SaveChangesAsync();

			return CreatedAtAction(nameof(GetItemCesta),
				new { id = item.Id }, item);
		}

        /// <summary>
        /// Retorna todos os Itens na tabela
        /// </summary>
        /// <returns>Json com todos os Itens </returns>
        /// <response code="200">Itens retornados com sucesso.</response>
        [HttpGet]
		public async Task<IActionResult> GetItensCesta()
		{
			var itens = await _context.ItensCesta.ToListAsync();

			return Ok(itens);
		}

        /// <summary>
        /// Retorna certo Item dado seu Id
        /// </summary>
        /// <param name="id"> Id do Item da Cesta</param>
        /// <returns>Item do id informado</returns>
        /// <response code="200">Item retornado com sucesso.</response>
        /// <response code="400">Falha ao retornar o item, variáveis incorretas</response>
        [HttpGet("{id}")]
		public async Task<IActionResult> GetItemCesta(long id)
		{
            //Validando o Id
            if (string.IsNullOrEmpty(id.ToString()))
                return BadRequest("Id é obrigatório.");

            var item = await _context.ItensCesta.FirstOrDefaultAsync(i => i.Id == id);

			if (item == null)
				return NotFound();

			return Ok(item);
		}

        /// <summary>
        /// Retorna todos os itens de determinada Cesta
        /// </summary>
        /// <param name="id"> Id da Cesta</param>
        /// <returns>Json com todos os Itens desta Cesta </returns>
        /// <response code="200">Itens retornados com sucesso.</response>
        /// <response code="400">Falha ao retornar os itens, variáveis incorretas</response>
        [HttpGet("itensCesta/{id}")]
        public async Task<IActionResult> GetItemCestaPorId(long id)
        {
            //Validando o Id
            if (string.IsNullOrEmpty(id.ToString()))
                return BadRequest("Id é obrigatório.");

            var itens = await _context.ItensCesta.Where(i => i.CestaId == id).ToListAsync();

            if (itens == null || !itens.Any())
                return NotFound();

            return Ok(itens);
        }
    }
}
