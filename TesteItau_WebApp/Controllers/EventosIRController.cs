using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TesteItau_WebApp.DAO;
using TesteItau_WebApp.Models;
using Confluent.Kafka;
using System.Text.Json;

namespace TesteItau_WebApp.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class EventosIRController : ControllerBase
	{
		private readonly AppDBContext _context;

		public EventosIRController(AppDBContext context)
		{
			_context = context;
		}


        /// <summary>
        /// Adiciona um EventoIR na tabela
        /// </summary>
        /// <param name="evento">Identificador do evento.</param>
        /// <returns>Evento encontrado.</returns>
        /// <response code="200">Evento publicado.</response>
        /// <response code="404">Evento não publicado.</response>
        [HttpPost]
        public async Task<IActionResult> PostEventoIR(EventoIR evento)
        {
            var clienteExiste = await _context.Clientes.AnyAsync(c => c.Id == evento.ClienteId);

            if (!clienteExiste)
                return BadRequest("ClienteId informado não existe.");

            if (evento.Tipo != "DEDO_DURO" && evento.Tipo != "IR_VENDA")
            {
                return BadRequest("Tipo deve ser 'DEDO_DURO' ou 'IR_VENDA'.");
            }

            evento.Id = 0;

            evento.DataEvento = evento.DataEvento ?? DateTime.Now;

            evento.PublicadoKafka = evento.PublicadoKafka ?? false;

            _context.EventosIR.Add(evento);
            await _context.SaveChangesAsync();

            var config = new ProducerConfig
            {
                BootstrapServers = "localhost:9092"
            };

            using var producer = new ProducerBuilder<Null, string>(config).Build();

            var eventoKafka = new
            {
                ClienteId = evento.ClienteId,
                Tipo = evento.Tipo,
                ValorIR = evento.ValorIR
            };

            var mensagem = JsonSerializer.Serialize(eventoKafka);

            await producer.ProduceAsync("impostos", new Message<Null, string>
            {
                Value = mensagem
            });

            return CreatedAtAction(nameof(GetEventoIR),
                new { id = evento.Id }, evento);
        }


        /// <summary>
        /// Retorna todos os EventosIR
        /// </summary>
        /// <returns>Evento encontrado.</returns>
        /// <response code="200">Eventos retornados.</response>
        [HttpGet]
		public async Task<IActionResult> GetEventosIR()
		{
			var eventos = await _context.EventosIR.ToListAsync();

			return Ok(eventos);
		}


        /// <summary>
        /// Retorna um evento de imposto de renda específico pelo ID.
        /// </summary>
        /// <param name="id">Identificador do evento.</param>
        /// <returns>Evento encontrado.</returns>
        /// <response code="200">Evento encontrado.</response>
        /// <response code="404">Evento não encontrado.</response>
        [HttpGet("{id}")]
		public async Task<IActionResult> GetEventoIR(long id)
		{
			var evento = await _context.EventosIR.FirstOrDefaultAsync(e => e.Id == id);

			if (evento == null)
				return NotFound();

			return Ok(evento);
		}
	}
}
