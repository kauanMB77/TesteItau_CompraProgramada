using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebApp.Models
{
	public class CestaRecomendacao
	{
		public long Id { get; set; }

		public string Nome { get; set; } = null!;

		public bool? Ativa { get; set; }

		public DateTime DataCriacao { get; set; }

		public DateTime? DataDesativacao { get; set; }
	}
}
