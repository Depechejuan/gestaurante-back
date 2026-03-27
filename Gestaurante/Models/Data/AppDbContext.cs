using Gestaurante.Models.DTO;
using Gestaurante.Models.Entities;
using Microsoft.EntityFrameworkCore;

//TODO   ChatGPT me ha recomendado la inclusión de indices en cada clase para optimizar las consultas a la BD; valorar como hacerlo y añadirlo cuando acabe con los modelBuilder


namespace Gestaurante.Models.Data
{
    public class AppDbContext : DbContext
    {
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
        public DbSet<MesaPublicSession> MesaPublicSessions => Set<MesaPublicSession>();
        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<UsuarioCliente> UsuariosCliente => Set<UsuarioCliente>();
        public DbSet<ClienteEmailVerification> ClienteEmailVerifications => Set<ClienteEmailVerification>();
        public DbSet<ClienteDireccion> ClienteDirecciones => Set<ClienteDireccion>();
        public DbSet<ClienteMetodoPago> ClienteMetodosPago => Set<ClienteMetodoPago>();

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
                    .HasValue<Cocinero>(TipoEmpleado.Cocinero)
                    .HasValue<Repartidor>(TipoEmpleado.Repartidor);

                entity.HasKey(e => e.Id)
                    .HasName("PK_Empleados");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ImageURL)
                    .IsRequired(false)
                    .HasMaxLength(500)
                    .HasDefaultValue(string.Empty);

                entity.Property(e => e.Activo)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.HasIndex(e => e.Email)
                    .IsUnique();

                entity.Property(e => e.DNI)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.HasIndex(e => e.DNI)
                    .IsUnique();

                entity.Property(e => e.NUSS)
                    .IsRequired()
                    .HasMaxLength(13);

                entity.HasIndex(e => e.NUSS)
                    .IsUnique();
            });

            modelBuilder.Entity<UsuarioCliente>(entity =>
            {
                entity.HasKey(u => u.IdUsuarioCliente)
                    .HasName("PK_UsuariosCliente");

                entity.Property(u => u.IdUsuarioCliente)
                    .IsRequired()
                    .ValueGeneratedOnAdd();

                entity.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(u => u.Email)
                    .IsUnique();

                entity.Property(u => u.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(u => u.FirstName)
                    .IsRequired()
                    .HasMaxLength(120);

                entity.Property(u => u.LastName)
                    .IsRequired()
                    .HasMaxLength(160);

                entity.Property(u => u.Phone)
                    .IsRequired()
                    .HasMaxLength(25);

                entity.Property(u => u.Activo)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.Property(u => u.EmailVerificado)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(u => u.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

                entity.Property(u => u.UpdatedAt)
                    .IsRequired(false);
            });

            modelBuilder.Entity<ClienteEmailVerification>(entity =>
            {
                entity.HasKey(v => v.IdClienteEmailVerification)
                    .HasName("PK_ClienteEmailVerifications");

                entity.Property(v => v.IdClienteEmailVerification)
                    .IsRequired()
                    .ValueGeneratedOnAdd();

                entity.Property(v => v.CodeHash)
                    .IsRequired()
                    .HasMaxLength(128);

                entity.Property(v => v.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

                entity.HasIndex(v => new { v.IdUsuarioCliente, v.ExpiresAt });

                entity.HasOne<UsuarioCliente>()
                    .WithMany()
                    .HasForeignKey(v => v.IdUsuarioCliente)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ClienteDireccion>(entity =>
            {
                entity.HasKey(d => d.IdClienteDireccion)
                    .HasName("PK_ClienteDirecciones");

                entity.Property(d => d.IdClienteDireccion)
                    .IsRequired()
                    .ValueGeneratedOnAdd();

                entity.Property(d => d.Alias)
                    .IsRequired()
                    .HasMaxLength(80);

                entity.Property(d => d.Street)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(d => d.City)
                    .IsRequired()
                    .HasMaxLength(120);

                entity.Property(d => d.Province)
                    .IsRequired()
                    .HasMaxLength(120);

                entity.Property(d => d.PostalCode)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(d => d.Notes)
                    .IsRequired(false)
                    .HasMaxLength(200)
                    .HasDefaultValue(string.Empty);

                entity.Property(d => d.IsDefault)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(d => d.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

                entity.Property(d => d.UpdatedAt)
                    .IsRequired(false);

                entity.HasOne<UsuarioCliente>()
                    .WithMany(u => u.Direcciones)
                    .HasForeignKey(d => d.IdUsuarioCliente)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ClienteMetodoPago>(entity =>
            {
                entity.HasKey(m => m.IdClienteMetodoPago)
                    .HasName("PK_ClienteMetodosPago");

                entity.Property(m => m.IdClienteMetodoPago)
                    .IsRequired()
                    .ValueGeneratedOnAdd();

                entity.Property(m => m.MockPaymentToken)
                    .IsRequired()
                    .HasMaxLength(120);

                entity.Property(m => m.Brand)
                    .IsRequired()
                    .HasMaxLength(40);

                entity.Property(m => m.Last4)
                    .IsRequired()
                    .HasMaxLength(4);

                entity.Property(m => m.HolderName)
                    .IsRequired()
                    .HasMaxLength(120);

                entity.Property(m => m.IsDefault)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(m => m.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

                entity.HasOne<UsuarioCliente>()
                    .WithMany(u => u.MetodosPago)
                    .HasForeignKey(m => m.IdUsuarioCliente)
                    .OnDelete(DeleteBehavior.Cascade);
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
                    .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'")
                    .ValueGeneratedOnAdd();

                entity.Property(p => p.UpdatedAt)
                    .ValueGeneratedOnAddOrUpdate()
                    .IsRequired(false);

                // Relación con Categoria
                entity.HasOne(p => p.Categoria)
                    .WithMany()
                    .HasForeignKey(p => p.IdCategoria)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Platos_Categorias");
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
                    .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'")
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
                    .WithMany(pi => pi.PlatoIngredientes)
                    .HasForeignKey(pi => pi.IdPlato)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pi => pi.Ingrediente)
                    .WithMany(pi => pi.PlatoIngredientes)
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

            modelBuilder.Entity<MesaPublicSession>(entity =>
            {
                entity.HasKey(s => s.IdMesaPublicSession)
                    .HasName("PK_MesaPublicSessions");

                entity.Property(s => s.IdMesaPublicSession)
                    .IsRequired()
                    .ValueGeneratedOnAdd();

                entity.Property(s => s.TokenHash)
                    .IsRequired()
                    .HasMaxLength(128);

                entity.Property(s => s.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.Property(s => s.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

                entity.Property(s => s.ExpiresAt)
                    .IsRequired();

                entity.Property(s => s.LastSeenAt)
                    .IsRequired(false);

                entity.Property(s => s.ClosedAt)
                    .IsRequired(false);

                entity.HasIndex(s => new { s.IdMesa, s.IsActive, s.ExpiresAt });

                entity.HasOne<Mesa>()
                    .WithMany()
                    .HasForeignKey(s => s.IdMesa)
                    .OnDelete(DeleteBehavior.Restrict);
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
                    .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'")
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
                    .HasDefaultValue(EstadoFactura.PENDIENTE);

                entity.Property(f => f.CanalPedido)
                    .IsRequired(false);

                entity.HasOne<Mesa>()
                    .WithMany()
                    .HasForeignKey(f => f.IdMesa)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Pedido>()
                    .WithMany()
                    .HasForeignKey(f => f.IdPedido)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            //modelBuilder de Pedido

            modelBuilder.Entity<Pedido>(entity =>
            {
                entity.HasKey(p => p.IdPedido)
                    .HasName("PK_Pedidos");
                entity.Property(p => p.IdPedido)
                    .IsRequired()
                    .ValueGeneratedOnAdd();
                entity.Property(p => p.FechaPedido)
                    .IsRequired()
                    .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'")
                    .ValueGeneratedOnAdd();
                entity.Property(p => p.FechaModificacion)
                    .IsRequired(false)
                    .ValueGeneratedOnAddOrUpdate();
                entity.Property(p => p.Estado)
                    .IsRequired()
                    .HasDefaultValue(EstadoPedido.PENDIENTE);
                entity.Property(p => p.CanalPedido)
                    .IsRequired()
                    .HasDefaultValue(CanalPedido.SALA);
                entity.Property(p => p.TipoEntrega)
                    .IsRequired()
                    .HasDefaultValue(TipoEntrega.MESA);
                entity.Property(p => p.EstadoPago)
                    .IsRequired()
                    .HasDefaultValue(EstadoPago.NO_APLICA);
                entity.Property(p => p.ClienteNombre)
                    .IsRequired(false)
                    .HasMaxLength(160)
                    .HasDefaultValue(string.Empty);
                entity.Property(p => p.ClienteEmail)
                    .IsRequired(false)
                    .HasMaxLength(100)
                    .HasDefaultValue(string.Empty);
                entity.Property(p => p.ClienteTelefono)
                    .IsRequired(false)
                    .HasMaxLength(25)
                    .HasDefaultValue(string.Empty);
                entity.Property(p => p.ClienteDireccionSnapshot)
                    .IsRequired(false)
                    .HasMaxLength(400)
                    .HasDefaultValue(string.Empty);
                entity.Property(p => p.Notas)
                    .IsRequired(false)
                    .HasMaxLength(500)
                    .HasDefaultValue(string.Empty);

                entity.HasOne<Mesa>()
                    .WithMany()
                    .HasForeignKey(p => p.IdMesa)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Factura>()
                    .WithMany()
                    .HasForeignKey(p => p.IdFactura)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<MesaPublicSession>()
                    .WithMany()
                    .HasForeignKey(p => p.IdMesaPublicSession)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<UsuarioCliente>()
                    .WithMany()
                    .HasForeignKey(p => p.IdUsuarioCliente)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            //modelBuilder de DetallePedido
            modelBuilder.Entity<DetallePedido>(entity => 
            {
                entity.HasKey(dp => dp.IdDetallePedido)
                    .HasName("PK_DetallePedido");
                entity.Property(dp => dp.IdDetallePedido)
                    .IsRequired()
                    .ValueGeneratedOnAdd();
                entity.Property(dp => dp.Cantidad)
                    .IsRequired()
                    .HasDefaultValue(1);
                entity.Property(dp => dp.PrecioUnitario)
                    .IsRequired()
                    .HasColumnType("decimal(10,2)");
                entity.Property(dp => dp.Estado)
                    .IsRequired()
                    .HasDefaultValue(EstadoDetallePedido.ACTIVA);
                entity.Property(dp => dp.FechaCancelacion)
                    .IsRequired(false);
                entity.HasOne<Plato>()
                    .WithMany()
                    .HasForeignKey(dp => dp.IdPlato)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Pedido>()
                    .WithMany(p => p.DetallesPedido)
                    .HasForeignKey(dp => dp.IdPedido)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            //modelBuilder de Categoria
            modelBuilder.Entity<Categoria>(entity =>
            {
                entity.HasKey(c => c.IdCategoria)
                    .HasName("PK_Categorias");

                entity.Property(c => c.IdCategoria)
                    .IsRequired()
                    .ValueGeneratedOnAdd();

                entity.Property(c => c.Descripcion)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("Descripción de la categoría");

                entity.HasMany(c => c.Platos)
                    .WithOne(p => p.Categoria)
                    .HasForeignKey(p => p.IdCategoria)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

    }
}
/*
 * entidades 
 *  empleado
 *  categoria
 *  pedido X
 *  detallePedido X
 *  factura X
 *  mesa X
 *  plato X
 *  ingrediente X
 *  platoIngrediente (tabla intermedia) X
 *  
 */
