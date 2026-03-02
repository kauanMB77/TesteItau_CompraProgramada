using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebMvc.Models
{
    public class ContaGraficaViewModel
    {
        public long contaGraficaId { get; set; }
        public long ClienteId { get; set; }
        public string NumeroConta { get; set; }
    }
}
