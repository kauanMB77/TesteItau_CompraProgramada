using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebMvc.Models
{
    public class LoginResponseViewModel
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Tipo { get; set; }
    }
}
