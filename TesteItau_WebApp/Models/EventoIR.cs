using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebApp.Models
{
	public class EventoIR
	{
		public long Id { get; set; }

		public long ClienteId { get; set; }

		public string Tipo { get; set; } = null!;

		public decimal ValorBase { get; set; }

		public decimal ValorIR { get; set; }

		public bool? PublicadoKafka { get; set; }

		public DateTime? DataEvento { get; set; }
	}
}
