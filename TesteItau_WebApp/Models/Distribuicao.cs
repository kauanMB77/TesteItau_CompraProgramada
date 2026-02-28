using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebApp.Models
{
	public class Distribuicao
	{
		public long Id { get; set; }

		public long OrdemCompraId { get; set; }

		public long CustodiaFilhoteId { get; set; }

		public string Ticker { get; set; } = null!;

		public int Quantidade { get; set; }

		public decimal PrecoUnitario { get; set; }

		public DateTime? DataDistribuicao { get; set; }
	}
}
