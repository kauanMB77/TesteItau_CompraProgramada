using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebApp.Models
{
	public class OrdemCompra 
	{
		public long Id { get; set; }

		public long ContaMasterId { get; set; }

		public string Ticker { get; set; } = null!;

		public int Quantidade { get; set; }

		public decimal PrecoUnitario { get; set; }

		public string? TipoMercado { get; set; }

		public DateTime? DataExecucao { get; set; }
	}
}
