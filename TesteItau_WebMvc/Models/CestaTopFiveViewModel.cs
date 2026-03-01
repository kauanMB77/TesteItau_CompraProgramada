using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebMvc.Models
{
    public class CestaTopFiveViewModel
    {
        public long Id { get; set; }
        public long CestaId { get; set; }
        public string Ticker { get; set; } = null!;
        public decimal Percentual { get; set; }
    }
}
