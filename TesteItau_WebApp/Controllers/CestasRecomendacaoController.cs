using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TesteItau_WebApp.DAO;
using TesteItau_WebApp.Models;

namespace TesteItau_WebApp.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class CestasRecomendacaoController : ControllerBase
	{
		private readonly AppDBContext _context;

		public CestasRecomendacaoController(AppDBContext context)
		{
			_context = context;
		}

        /// <summary>
        /// Adiciona uma nova cesta a Tabela.
        /// </summary>
        /// <param name="cesta"> Recebe um obJeto CestaRecomendacao, utilizando cesta.Nome para definir o nome da cesta, a cesta recebida nesta API é sempre ativa e a DataCriacao é Datetime.Now</param>
        /// <returns>Id da cesta gerada</returns>
        /// <response code="200">Cesta criada com sucesso.</response>
        /// <response code="400">Falha ao criar a cesta, variáveis incorretas</response>
        [HttpPost]
		public async Task<IActionResult> PostCesta(CestaRecomendacao cesta)
		{
			//Validações
			if (string.IsNullOrWhiteSpace(cesta.Nome))
				return BadRequest("Nome é obrigatório.");

			cesta.Id = 0;

			cesta.DataCriacao = DateTime.Now;

			cesta.Ativa = cesta.Ativa ?? true;

			_context.CestasRecomendacao.Add(cesta);
			await _context.SaveChangesAsync();

			return CreatedAtAction(nameof(GetCesta), new { id = cesta.Id }, cesta);
		}

        /// <summary>
        /// Retorna um Json com todas as cestas
        /// </summary>
        /// <returns>Id da cesta gerada</returns>
        /// <response code="200">Cestas retornadas com sucesso.</response>
        [HttpGet]
		public async Task<IActionResult> GetCestas()
		{
            var cestas = await _context.CestasRecomendacao.ToListAsync();

			return Ok(cestas);
		}

        /// <summary>
        /// Retorna um json com determinada cesta
        /// </summary>
        /// <param name="id"> Id da cesta a ser retornada</param>
        /// <returns>Json com a cesta identificada</returns>
        /// <response code="200">Good Request, cesta retornada.</response>
        /// <response code="400">Falha ao retornar a cesta, variáveis incorretas</response>
        [HttpGet("{id}")]
		public async Task<IActionResult> GetCesta(long id)
		{
            //Validações
            if (string.IsNullOrWhiteSpace(id.ToString()))
                return BadRequest("Id é obrigatório.");

            var cesta = await _context.CestasRecomendacao.FirstOrDefaultAsync(c => c.Id == id);

			if (cesta == null)
				return NotFound();

			return Ok(cesta);
		}

        /// <summary>
        /// Retorna o Id da cesta ativa no momento
        /// </summary>
        /// <returns>Json com a cesta ativa</returns>
        /// <response code="200">Retornando cesta ativa.</response>
        [HttpGet("ativa")]
        public async Task<IActionResult> GetAtiva()
        {
            var cesta = await _context.CestasRecomendacao.FirstOrDefaultAsync(c => c.Ativa == true);

            if (cesta == null)
                return NotFound();

            return Ok(cesta);
        }

        /// <summary>
        /// Desativa determinada cesta
        /// </summary>
        /// <param name="id"> Id da cesta a ser Desativada, é necessário chamar esta rota após criar outra cesta, para desativar a anterior</param>
        /// <returns>Json com a cesta desativada</returns>
        /// <response code="200">Cesta desativada com sucesso.</response>
        /// <response code="400">Falha ao desativar a cesta, variáveis incorretas</response>
        [HttpPut("desativar/{id}")]
		public async Task<IActionResult> DesativarCesta(long id)
		{
            //Validações
            if (string.IsNullOrWhiteSpace(id.ToString()))
                return BadRequest("Id é obrigatório.");


            var cesta = await _context.CestasRecomendacao.FirstOrDefaultAsync(c => c.Id == id);

			if (cesta == null)
				return NotFound();

			cesta.Ativa = false;
			cesta.DataDesativacao = DateTime.Now;

			await _context.SaveChangesAsync();

			return Ok(cesta);
		}
    }
}
