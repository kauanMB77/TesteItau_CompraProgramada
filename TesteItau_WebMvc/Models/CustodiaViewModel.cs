using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebMvc.Models
{
    public class CustodiaViewModel
    {
        public string Ticker { get; set; } = null!;
        public decimal Quantidade { get; set; }
        public decimal PrecoMedio { get; set; }
        public decimal PrecoFechamento { get; set; }
        public decimal ValorAtual => Quantidade * PrecoFechamento;
        public DateTime DataUltimaAtualizacao { get; set; }
    }
}
