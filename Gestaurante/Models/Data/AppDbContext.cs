using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using Gestaurante.Models.Entities;

namespace Gestaurante.Models.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Empleado> Empleados => Set<Empleado>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Empleado>()
                .HasDiscriminator<TipoEmpleado>("Tipo")
                .HasValue<Camarero>(TipoEmpleado.Camarero)
                .HasValue<Cocinero>(TipoEmpleado.Cocinero)
                .HasValue<Administrador>(TipoEmpleado.Administrador);
        }
    }
}
