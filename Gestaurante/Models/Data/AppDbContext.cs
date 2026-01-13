using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;

//TODO   ChatGPT me ha recomendado la inclusión de indices en cada clase para optimizar las consultas a la BD; valorar como hacerlo y añadirlo cuando acabe con los modelBuilder


namespace Gestaurante.Models.Data
{
    public class AppDbContext : DbContext
    {
        public enum EstadoFactura
        {
            PENDIENTE,
            PAGADO,
            CANCELADO
        }
        public AppDbContext(DbContextOptions<AppDbContext> options): base(options)
        {
        }
        public DbSet<Empleado> Empleados => Set<Empleado>();
        public DbSet<Plato> Platos => Set<Plato>();
        public DbSet<Ingrediente> Ingredientes => Set<Ingrediente>();
        public DbSet<Mesa> Mesas => Set<Mesa>();
        public DbSet<Pedido> Pedidos => Set<Pedido>();
        public DbSet<Factura> Facturas => Set<Factura>();
        public DbSet<DetallePedido> DetallesPedido => Set<DetallePedido>();
        public DbSet<Categoria> Categorias => Set<Categoria>(); 

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //modelBuilder de Empleados

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

            //modelBuilder de Platos

            modelBuilder.Entity<Plato>(entity =>
            {
                entity.HasKey(p => p.IdPlato)
                    .HasName("PK_Platos");

                entity.Property(p => p.IdPlato)
                    .IsRequired()
                    .ValueGeneratedOnAdd();

                entity.Property(p => p.Nombre)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasComment("Nombre del plato");

                entity.HasIndex(p => p.Nombre)
                    .IsUnique(); // Solo si queremos nombres únicos por cada plato

                entity.Property(p => p.Descripcion)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasComment("Descripción del plato");

                entity.Property(p => p.Imagen)
                    .HasMaxLength(500)
                    .IsRequired(false)
                    .HasComment("URL de la imagen");

                entity.Property(p => p.Disponible)
                    .IsRequired()
                    .HasDefaultValue(false)
                    .HasComment("Disponibilidad del plato");

                entity.Property(p => p.Precio)
                    .IsRequired()
                    .HasColumnType("decimal(10,2)")
                    .HasDefaultValue(0);

                entity.Property(p => p.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()")
                    .ValueGeneratedOnAdd();

                entity.Property(p => p.UpdatedAt)
                    .ValueGeneratedOnAddOrUpdate()
                    .IsRequired(false);
            });

            //modelBuilder de Ingredientes

            modelBuilder.Entity<Ingrediente>(entity =>
            {
                entity.HasKey(i => i.IdIngrediente)
                    .HasName("PK_Ingredientes");

                entity.Property(i => i.IdIngrediente)
                    .IsRequired()
                    .ValueGeneratedOnAdd();

                entity.Property(i => i.Nombre)
                    .IsRequired()
                    .HasMaxLength(100); 

                entity.Property(i => i.Alergenico)
                    .IsRequired()
                    .HasDefaultValue(false)
                    .HasComment("Indica si el ingrediente es alergénico");

                entity.Property(i => i.Disponible)
                    .IsRequired()
                    .HasDefaultValue(true) 
                    .HasComment("Indica si el ingrediente está disponible para usar");
   
                entity.Property(i => i.Imagen)
                    .IsRequired(false) 
                    .HasMaxLength(500) 
                    .HasDefaultValue(string.Empty)
                    .HasComment("URL de la imagen");

                entity.Property(i => i.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()")
                    .ValueGeneratedOnAdd();

                entity.Property(i => i.UpdatedAt)
                    .IsRequired(false)
                    .ValueGeneratedOnAddOrUpdate();
            });

            //modelBuilder Tabla Intermedia PlatoIngrediente
            modelBuilder.Entity<PlatoIngrediente>(entity =>
            {
                entity.HasKey(pi => new { pi.IdPlato, pi.IdIngrediente })
                    .HasName("PK_PlatoIngrediente");

                entity.HasOne(pi => pi.Plato)
                    .WithMany(pi => pi.PlatoIngrediente)
                    .HasForeignKey(pi => pi.IdPlato)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pi => pi.Ingrediente)
                    .WithMany(pi => pi.PlatoIngrediente)
                    .HasForeignKey(pi => pi.IdIngrediente)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            //modelBuilder de Mesa

            modelBuilder.Entity<Mesa>(entity =>
            {
                entity.HasKey(m => m.IdMesa)
                    .HasName("PK_Mesas");

                entity.Property(m => m.IdMesa)
                    .IsRequired()
                    .ValueGeneratedOnAdd();

                entity.Property(m => m.Capacidad)
                    .IsRequired()
                    .HasDefaultValue(4);

                entity.Property(m => m.Estado)
                    .IsRequired()
                    .HasDefaultValue(true) // Por defecto disponible
                    .HasComment("Estado de la mesa: true=Disponible, false=Ocupada");

                entity.Property(m => m.Ubicacion)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasDefaultValue("Interior") // Valor por defecto
                    .HasComment("Ubicación física de la mesa en el restaurante");
            });

            //modelBuilder de Factura   

            modelBuilder.Entity<Factura>(entity =>
            {
                entity.HasKey(f => f.NumeroFactura)
                    .HasName("PK_Facturas");

                entity.Property(f => f.NumeroFactura)
                    .IsRequired()
                    .ValueGeneratedOnAdd();

                entity.Property(f => f.FechaFactura)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()")
                    .ValueGeneratedOnAdd();

                entity.Property(f => f.PrecioTotal)
                    .IsRequired()
                    .HasColumnType("decimal(10,2)")
                    .HasDefaultValue(0);

                entity.Property(f => f.Descuento)
                    .IsRequired()
                    .HasColumnType("decimal(5,2)")
                    .HasDefaultValue(0);

                entity.Property(f => f.Estado)
                    .IsRequired()
                    .HasDefaultValue(EstadoFactura(0)); // Por defecto pendiente;

            });
        }
    }
}
