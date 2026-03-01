using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebMvc.Models
{
    public class HomeViewModel
    {
        public bool ativo { get; set; }
        public decimal? valorInvestido { get; set; }
    }
}
