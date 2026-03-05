using Microsoft.AspNetCore.Mvc;

namespace TesteItau_WebApp.Models
{
    public class Rebalanceamento
    {
        public long Id { get; set; }

        public long ClienteId { get; set; }

        public string Tipo { get; set; }

        public string TickerVendido { get; set; }

        public string TickerComprado { get; set; }

        public decimal ValorVenda { get; set; }

        public DateTime? DataRebalanceamento { get; set; }

    }
}
