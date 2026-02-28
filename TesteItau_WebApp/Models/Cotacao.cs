using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebApp.Models
{
	public class Cotacao
	{
		public long Id { get; set; }

		public DateTime DataPregao { get; set; }

		public string Ticker { get; set; } = null!;

		public decimal PrecoAbertura { get; set; }

		public decimal PrecoFechamento { get; set; }

		public decimal PrecoMaximo { get; set; }

		public decimal PrecoMinimo { get; set; }
	}
}
