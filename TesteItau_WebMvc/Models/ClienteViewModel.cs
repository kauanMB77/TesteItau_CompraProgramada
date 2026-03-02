using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebMvc.Models
{
    public class ClienteViewModel
    {
    public long Id { get; set; }
        public bool Ativo { get; set; }
        public decimal ValorMensal { get; set; }
        public long ContaGraficaId { get; set; }
    }
}
