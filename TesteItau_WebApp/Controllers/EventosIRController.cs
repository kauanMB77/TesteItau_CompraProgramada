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

        [HttpGet]
		public async Task<IActionResult> GetEventosIR()
		{
			var eventos = await _context.EventosIR.ToListAsync();

			return Ok(eventos);
		}

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
