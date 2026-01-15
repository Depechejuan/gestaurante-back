using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.EntityFrameworkCore;


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
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Empleado>(entity =>
            {
                entity
                    .HasDiscriminator<TipoEmpleado>("Tipo")
                    .HasValue<Administrador>(TipoEmpleado.Administrador)
                    .HasValue<Camarero>(TipoEmpleado.Camarero)
                    .HasValue<Cocinero>(TipoEmpleado.Cocinero);

                entity.HasKey(e => e.Id)
                    .HasName("PK_Empleados");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(e => e.Email)
                    .IsUnique();

                entity.Property(e => e.DNI)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.HasIndex(e => e.DNI)
                    .IsUnique();

                entity.Property(e => e.DNI)
                    .IsRequired()
                    .HasMaxLength(13);

                entity.HasIndex(e => e.DNI)
                    .IsUnique();

            });
        }
    }
}
