using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebMvc.Models
{
	public class LoginViewModel
	{
		public string Email { get; set; } = string.Empty;
		public string Senha { get; set; } = string.Empty;
	}
}
