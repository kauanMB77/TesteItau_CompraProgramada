using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebApp.Models
{
    public class ContaGrafica 
    {
        public long Id { get; set; }

        public long ClienteId { get; set; }

        public string NumeroConta { get; set; } = null!;

        public string Tipo { get; set; } = null!;

        public DateTime DataCriacao { get; set; }

        public Client? Cliente { get; set; }
    }
}
