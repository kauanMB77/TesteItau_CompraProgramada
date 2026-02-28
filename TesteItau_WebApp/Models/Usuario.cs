using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebApp.Models
{
	public class Usuario
	{
		public long Id { get; set; }

		public long ClienteId { get; set; }

		public string Email { get; set; } = null!;

		public string Senha { get; set; } = null!;

		public string Tipo { get; set; } = null!;
	}
}
