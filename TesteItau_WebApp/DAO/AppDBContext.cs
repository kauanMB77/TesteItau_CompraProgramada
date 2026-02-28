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
    }
}
