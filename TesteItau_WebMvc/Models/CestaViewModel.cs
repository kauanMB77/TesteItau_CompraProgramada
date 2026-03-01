using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebMvc.Models
{
    public class CestaViewModel
    {
        public long Id { get; set; }
        public string Nome { get; set; } = null!;
        public bool Ativo { get; set; }
    }
}
