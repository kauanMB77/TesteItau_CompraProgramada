using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebMvc.Models
{
    public class CadastroViewModel
    {
        public string Nome { get; set; }
        public string Cpf { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; }
        public decimal ValorMensal { get; set; }
    }
}
