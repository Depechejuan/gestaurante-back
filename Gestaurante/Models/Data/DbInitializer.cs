//using Gestaurante.Models.Data;
//using Gestaurante.Models.DTO;
//using Gestaurante.Models.Entities;
//using DotNetEnv;


//namespace Gestaurante.Models.Seed
//{
//    public static class DbInitializer
//    {
//        public static void Seed(AppDbContext context)
//        {
//            // Asegúrate de crear la base de datos si no existe
//            context.Database.EnsureCreated();


//            Env.Load();
//            string email = Environment.GetEnvironmentVariable("ADMIN_EMAIL")
//                ?? throw new Exception("ADMIN_EMAIL no definido");
//            string pass = Environment.GetEnvironmentVariable("ADMIN_PASS")
//                ?? throw new Exception("ADMIN_PASS no definido");
//            string name = Environment.GetEnvironmentVariable("ADMIN_NAME")
//                ?? throw new Exception("ADMIN_NAME no definido");
//            string surename1 = Environment.GetEnvironmentVariable("ADMIN_SURENAME1")
//                ?? throw new Exception("ADMIN_EMAIL no definido");
//            string surename2 = Environment.GetEnvironmentVariable("ADMIN_SURENAME2")
//                ?? throw new Exception("ADMIN_SURENAME2 no definido");
//            string dni = Environment.GetEnvironmentVariable("ADMIN_DNI")
//                ?? throw new Exception("ADMIN_DNI no definido");
//            string nuss = Environment.GetEnvironmentVariable("ADMIN_NUSS")
//                ?? throw new Exception("ADMIN_NUSS no definido");


//            var existing = context.Empleados.FirstOrDefault(e => e.Email == email);
//            if (existing != null)
//            {
//                context.Empleados.Remove(existing);
//                context.SaveChanges();
//                Console.WriteLine($"Empleado con email {email} eliminado.");
//            }

//            string passHashed = BCrypt.Net.BCrypt.HashPassword(pass);
//            Console.WriteLine("Password hasheado para el administrador.");
//            Console.WriteLine($"{passHashed}");
//            // Crear empleados de ejemplo
//            var empleados = new List<Empleado>
//            {
//                new Administrador(email, passHashed, name, surename1, surename2, dni, nuss)
//            };

//            context.Empleados.AddRange(empleados);
//            context.SaveChanges();
//        }
//    }
//}
