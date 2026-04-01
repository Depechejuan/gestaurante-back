using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Gestaurante.Models.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var envPath = Path.Combine(Directory.GetCurrentDirectory(), "Gestaurante", ".env");
            if (File.Exists(envPath))
                Env.Load(envPath);
            else
            {
                Env.Load();
            }

            string dbHost = Environment.GetEnvironmentVariable("DB_HOST")
                ?? throw new Exception("DB_HOST no definido");
            string dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
            string dbName = Environment.GetEnvironmentVariable("DB_NAME")
                ?? throw new Exception("DB_NAME no definido");
            string dbUser = Environment.GetEnvironmentVariable("DB_USER")
                ?? throw new Exception("DB_USER no definido");
            string dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD")
                ?? throw new Exception("DB_PASSWORD no definido");

            string connectionString =
                $"Server={dbHost};Port={dbPort};Database={dbName};User Id={dbUser};Password={dbPassword};SSL Mode=Require;Trust Server Certificate=true;";

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(connectionString);
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
