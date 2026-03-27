using BCrypt.Net;
using Gestaurante.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gestaurante.Models.Data
{
    public static class DbInitializer
    {
        public static async Task SeedDefaultEmployeesAsync(AppDbContext context, CancellationToken cancellationToken = default)
        {
            if (await context.Empleados.AnyAsync(cancellationToken))
            {
                await SeedDefaultRepartidoresAsync(context, cancellationToken);
                await SeedDefaultMesasAsync(context, cancellationToken);
                return;
            }

            string adminPassword = Environment.GetEnvironmentVariable("DEFAULT_ADMIN_PASSWORD")
                ?? throw new Exception("DEFAULT_ADMIN_PASSWORD no definido");
            string camareroPassword = Environment.GetEnvironmentVariable("DEFAULT_CAMARERO_PASSWORD")
                ?? throw new Exception("DEFAULT_CAMARERO_PASSWORD no definido");
            string cocineroPassword = Environment.GetEnvironmentVariable("DEFAULT_COCINERO_PASSWORD")
                ?? throw new Exception("DEFAULT_COCINERO_PASSWORD no definido");
            string repartidorPassword = Environment.GetEnvironmentVariable("DEFAULT_REPARTIDOR_PASSWORD")
                ?? camareroPassword;

            string adminHash = BCrypt.Net.BCrypt.HashPassword(adminPassword, BCrypt.Net.BCrypt.GenerateSalt(12));
            string camareroHash = BCrypt.Net.BCrypt.HashPassword(camareroPassword, BCrypt.Net.BCrypt.GenerateSalt(12));
            string cocineroHash = BCrypt.Net.BCrypt.HashPassword(cocineroPassword, BCrypt.Net.BCrypt.GenerateSalt(12));
            string repartidorHash = BCrypt.Net.BCrypt.HashPassword(repartidorPassword, BCrypt.Net.BCrypt.GenerateSalt(12));

            var empleados = new List<Empleado>
            {
                new Administrador("admin@gestaurante.com", adminHash, "Admin", "Gestaurante", "Principal", "00000000T", "0111111111111"),

                new Cocinero("lucas.romero@gestaurante.com", cocineroHash, "Lucas", "Romero", "Santos", "00000001R", "0222222222221"),
                new Cocinero("maria.santos@gestaurante.com", cocineroHash, "Maria", "Santos", "Ruiz", "00000002W", "0222222222222"),
                new Cocinero("alberto.molina@gestaurante.com", cocineroHash, "Alberto", "Molina", "Perez", "00000003A", "0222222222223"),
                new Cocinero("natalia.ramos@gestaurante.com", cocineroHash, "Natalia", "Ramos", "Lopez", "00000004G", "0222222222224"),
                new Cocinero("carmen.navarro@gestaurante.com", cocineroHash, "Carmen", "Navarro", "Diaz", "00000005M", "0222222222225"),

                new Camarero("paula.garcia@gestaurante.com", camareroHash, "Paula", "Garcia", "Martin", "00000006Y", "0333333333331"),
                new Camarero("diego.herrera@gestaurante.com", camareroHash, "Diego", "Herrera", "Gil", "00000007F", "0333333333332"),
                new Camarero("laura.perez@gestaurante.com", camareroHash, "Laura", "Perez", "Vega", "00000008P", "0333333333333"),
                new Camarero("jorge.ruiz@gestaurante.com", camareroHash, "Jorge", "Ruiz", "Ortega", "00000009D", "0333333333334"),
                new Camarero("elena.flores@gestaurante.com", camareroHash, "Elena", "Flores", "Cano", "00000010X", "0333333333335"),

                new Repartidor("sergio.reparto@gestaurante.com", repartidorHash, "Sergio", "Morales", "Cruz", "00000011B", "0444444444441"),
                new Repartidor("irene.reparto@gestaurante.com", repartidorHash, "Irene", "Campos", "Sanz", "00000012N", "0444444444442")
            };

            foreach (var empleado in empleados)
            {
                empleado.Activo = true;
                empleado.ImageURL = string.Empty;
            }

            await context.Empleados.AddRangeAsync(empleados, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await SeedDefaultMesasAsync(context, cancellationToken);
        }

        private static async Task SeedDefaultRepartidoresAsync(AppDbContext context, CancellationToken cancellationToken)
        {
            var existingRepartidores = await context.Empleados.OfType<Repartidor>().AnyAsync(cancellationToken);
            if (existingRepartidores)
                return;

            string repartidorPassword = Environment.GetEnvironmentVariable("DEFAULT_REPARTIDOR_PASSWORD")
                ?? Environment.GetEnvironmentVariable("DEFAULT_CAMARERO_PASSWORD")
                ?? throw new Exception("DEFAULT_REPARTIDOR_PASSWORD o DEFAULT_CAMARERO_PASSWORD no definido");

            string repartidorHash = BCrypt.Net.BCrypt.HashPassword(repartidorPassword, BCrypt.Net.BCrypt.GenerateSalt(12));

            var repartidores = new List<Repartidor>
            {
                new Repartidor("sergio.reparto@gestaurante.com", repartidorHash, "Sergio", "Morales", "Cruz", "00000011B", "0444444444441"),
                new Repartidor("irene.reparto@gestaurante.com", repartidorHash, "Irene", "Campos", "Sanz", "00000012N", "0444444444442")
            };

            foreach (var repartidor in repartidores)
            {
                repartidor.Activo = true;
                repartidor.ImageURL = string.Empty;
            }

            await context.Empleados.AddRangeAsync(repartidores, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        private static async Task SeedDefaultMesasAsync(AppDbContext context, CancellationToken cancellationToken)
        {
            if (await context.Mesas.AnyAsync(cancellationToken))
                return;

            var mesas = new List<Mesa>
            {
                new Mesa(Guid.NewGuid(), 2, true, "Terraza A1"),
                new Mesa(Guid.NewGuid(), 2, true, "Terraza A2"),
                new Mesa(Guid.NewGuid(), 2, true, "Terraza A3"),
                new Mesa(Guid.NewGuid(), 4, true, "Interior B1"),
                new Mesa(Guid.NewGuid(), 4, true, "Interior B2"),
                new Mesa(Guid.NewGuid(), 4, true, "Interior B3"),
                new Mesa(Guid.NewGuid(), 4, true, "Interior B4"),
                new Mesa(Guid.NewGuid(), 6, true, "Salon C1"),
                new Mesa(Guid.NewGuid(), 6, true, "Salon C2"),
                new Mesa(Guid.NewGuid(), 6, true, "Salon C3"),
                new Mesa(Guid.NewGuid(), 8, true, "Reservado D1"),
                new Mesa(Guid.NewGuid(), 8, true, "Reservado D2"),
                new Mesa(Guid.NewGuid(), 10, true, "Salon Grande E1"),
                new Mesa(Guid.NewGuid(), 10, true, "Salon Grande E2"),
                new Mesa(Guid.NewGuid(), 12, true, "Evento F1")
            };

            await context.Mesas.AddRangeAsync(mesas, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
