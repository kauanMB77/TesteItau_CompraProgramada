using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using TesteItau_WebApp.Models;

namespace TesteItau_WebApp.DAO
{
    public class AppDBContext : DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options): base(options) {}

        public DbSet<Client> Clientes { get; set; }
        public DbSet<ContaGrafica> ContasGraficas { get; set; }
		public DbSet<Custodia> Custodias { get; set; }
        public DbSet<OrdemCompra> OrdensCompra { get; set; }
		public DbSet<EventoIR> EventosIR { get; set; }
		public DbSet<Distribuicao> Distribuicoes { get; set; }
	}
}
