using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebApp.Models
{
    public class Client
    {
        public long Id { get; set; }

        public string Nome { get; set; } = null!;

        public string CPF { get; set; } = null!;

        public string? Email { get; set; }

        public decimal ValorMensal { get; set; }

        public bool Ativo { get; set; } = true;

        public DateTime DataAdesao { get; set; }
    }
}
