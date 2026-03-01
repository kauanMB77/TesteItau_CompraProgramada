using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebMvc.Models
{
    public class CarteiraViewModel
    {
        public long ContaGraficaId { get; set; }
        public List<CustodiaViewModel> Custodias { get; set; } = new();
    }
}
