using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebApp.Models
{
	public class Custodia
	{
		public long Id { get; set; }

		public long ContaGraficaId { get; set; }

		public string Ticker { get; set; } = null!;

		public int Quantidade { get; set; }

		public decimal PrecoMedio { get; set; }

		public DateTime DataUltimaAtualizacao { get; set; }

		public ContaGrafica? ContaGrafica { get; set; }
	}
}
