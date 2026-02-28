using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebApp.Models
{
	public class ItemCesta
	{
		public long Id { get; set; }

		public long CestaId { get; set; }

		public string? Ticker { get; set; }

		public decimal? Percentual { get; set; }
	}
}
