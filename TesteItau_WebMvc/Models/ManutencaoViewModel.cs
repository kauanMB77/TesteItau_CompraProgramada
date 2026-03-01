using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebMvc.Models
{
    public class ManutencaoViewModel
    {
        public bool Ativo { get; set; }
        public decimal ValorMensal { get; set; }
    }
}
