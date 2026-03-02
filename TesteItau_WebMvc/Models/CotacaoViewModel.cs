using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebMvc.Models
{
    public class CotacaoViewModel
    {
        public string Ticker { get; set; }
        public decimal PrecoFechamento { get; set; }
    }
}
