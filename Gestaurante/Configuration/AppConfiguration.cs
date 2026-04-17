using DotNetEnv;
using Microsoft.Extensions.Options;

namespace Gestaurante.Configuration
{
    public static class AppConfiguration
    {
        public static void LoadDotEnv()
        {
            var envCandidates = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), ".env"),
                Path.Combine(Directory.GetCurrentDirectory(), "Gestaurante", ".env"),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.env"))
            };

            foreach (var envPath in envCandidates.Distinct())
            {
                if (!File.Exists(envPath))
                    continue;

                Env.Load(envPath);
            }
        }

        public static string? GetTrimmedValue(this IConfiguration configuration, string key)
        {
            var value = configuration[key]?.Trim();
            if (string.IsNullOrWhiteSpace(value))
                return value;

            return value.Trim('"');
        }

        public static DatabaseOptions BuildDatabaseOptions(this IConfiguration configuration)
        {
            return new DatabaseOptions
            {
                Host = GetRequiredValue(configuration, "DB_HOST"),
                Port = int.TryParse(configuration.GetTrimmedValue("DB_PORT"), out var parsedPort) ? parsedPort : 5432,
                Name = GetRequiredValue(configuration, "DB_NAME"),
                User = GetRequiredValue(configuration, "DB_USER"),
                Password = GetRequiredValue(configuration, "DB_PASSWORD")
            };
        }

        public static EmployeeJwtOptions BuildEmployeeJwtOptions(this IConfiguration configuration)
        {
            return new EmployeeJwtOptions
            {
                Key = GetRequiredValue(configuration, "JWT_KEY"),
                Issuer = GetRequiredValue(configuration, "JWT_ISSUER"),
                Audience = GetRequiredValue(configuration, "JWT_AUDIENCE"),
                ExpireDays = int.TryParse(configuration.GetTrimmedValue("JWT_EXPIRE_DAYS"), out var days) ? days : 30
            };
        }

        public static CustomerJwtOptions BuildCustomerJwtOptions(this IConfiguration configuration)
        {
            return new CustomerJwtOptions
            {
                Key = GetRequiredValue(configuration, "CUSTOMER_JWT_KEY"),
                Issuer = GetRequiredValue(configuration, "CUSTOMER_JWT_ISSUER"),
                Audience = GetRequiredValue(configuration, "CUSTOMER_JWT_AUDIENCE"),
                ExpireDays = int.TryParse(configuration.GetTrimmedValue("CUSTOMER_JWT_EXPIRE_DAYS"), out var days) ? days : 30
            };
        }

        public static SmtpOptions BuildSmtpOptions(this IConfiguration configuration)
        {
            return new SmtpOptions
            {
                Host = configuration.GetTrimmedValue("SMTP_HOST"),
                Port = int.TryParse(configuration.GetTrimmedValue("SMTP_PORT"), out var port) ? port : null,
                User = configuration.GetTrimmedValue("SMTP_USER"),
                Password = configuration.GetTrimmedValue("SMTP_PASSWORD"),
                FromEmail = configuration.GetTrimmedValue("SMTP_FROM_EMAIL") ?? "no-reply@gestaurante.local",
                FromName = configuration.GetTrimmedValue("SMTP_FROM_NAME") ?? "Gestaurante"
            };
        }

        public static CloudinaryOptions BuildCloudinaryOptions(this IConfiguration configuration)
        {
            return new CloudinaryOptions
            {
                CloudName = configuration.GetTrimmedValue("CLOUDINARY_CLOUD_NAME") ?? configuration.GetTrimmedValue("CLOUDINARY_CLOUDNAME"),
                ApiKey = configuration.GetTrimmedValue("CLOUDINARY_API_KEY") ?? configuration.GetTrimmedValue("CLOUDINARY_APIKEY"),
                ApiSecret = configuration.GetTrimmedValue("CLOUDINARY_API_SECRET") ?? configuration.GetTrimmedValue("CLOUDINARY_APISECRET"),
                EmployeeFolder = configuration.GetTrimmedValue("CLOUDINARY_EMPLOYEE_FOLDER") ?? "gestaurante/empleados"
            };
        }

        public static SeedOptions BuildSeedOptions(this IConfiguration configuration)
        {
            return new SeedOptions
            {
                DefaultAdminPassword = configuration.GetTrimmedValue("DEFAULT_ADMIN_PASSWORD") ?? string.Empty,
                DefaultCamareroPassword = configuration.GetTrimmedValue("DEFAULT_CAMARERO_PASSWORD") ?? string.Empty,
                DefaultCocineroPassword = configuration.GetTrimmedValue("DEFAULT_COCINERO_PASSWORD") ?? string.Empty,
                DefaultRepartidorPassword = configuration.GetTrimmedValue("DEFAULT_REPARTIDOR_PASSWORD") ?? string.Empty,
                DefaultClientPassword = configuration.GetTrimmedValue("DEFAULT_CLIENT_PASSWORD") ?? string.Empty
            };
        }

        public static BootstrapOptions BuildBootstrapOptions(this IConfiguration configuration, string[] args)
        {
            var importCatalog = args.Contains("--import-catalog", StringComparer.OrdinalIgnoreCase)
                || bool.TryParse(configuration.GetTrimmedValue("BOOTSTRAP_IMPORT_CATALOG"), out var importCatalogEnabled) && importCatalogEnabled;

            var catalogImportPath = args
                .FirstOrDefault(arg => arg.StartsWith("--catalog-import-path=", StringComparison.OrdinalIgnoreCase))
                ?.Split('=', 2)[1]
                .Trim('"')
                ?? configuration.GetTrimmedValue("BOOTSTRAP_CATALOG_PATH");

            return new BootstrapOptions
            {
                RunOnStart = args.Contains("--bootstrap", StringComparer.OrdinalIgnoreCase)
                    || importCatalog
                    || bool.TryParse(configuration.GetTrimmedValue("BOOTSTRAP_ON_START"), out var runOnStart) && runOnStart,
                ApplyMigrations = !bool.TryParse(configuration.GetTrimmedValue("BOOTSTRAP_APPLY_MIGRATIONS"), out var applyMigrations) || applyMigrations,
                SeedDefaults = !bool.TryParse(configuration.GetTrimmedValue("BOOTSTRAP_SEED_DEFAULTS"), out var seedDefaults) || seedDefaults,
                RepairData = !bool.TryParse(configuration.GetTrimmedValue("BOOTSTRAP_REPAIR_DATA"), out var repairData) || repairData,
                ImportCatalog = importCatalog,
                CatalogImportPath = catalogImportPath
            };
        }

        public static CorsPolicyOptions BuildCorsPolicyOptions(this IConfiguration configuration)
        {
            var rawOrigins = configuration.GetTrimmedValue("CORS_ALLOWED_ORIGINS");
            var origins = string.IsNullOrWhiteSpace(rawOrigins)
                ? new List<string>()
                : rawOrigins
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(origin => origin.TrimEnd('/'))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            return new CorsPolicyOptions
            {
                AllowedOrigins = origins
            };
        }

        public static void RegisterApplicationOptions(this IServiceCollection services, IConfiguration configuration, string[] args)
        {
            services.AddSingleton(Options.Create(configuration.BuildDatabaseOptions()));
            services.AddSingleton(Options.Create(configuration.BuildEmployeeJwtOptions()));
            services.AddSingleton(Options.Create(configuration.BuildCustomerJwtOptions()));
            services.AddSingleton(Options.Create(configuration.BuildSmtpOptions()));
            services.AddSingleton(Options.Create(configuration.BuildCloudinaryOptions()));
            services.AddSingleton(Options.Create(configuration.BuildSeedOptions()));
            services.AddSingleton(Options.Create(configuration.BuildBootstrapOptions(args)));
            services.AddSingleton(Options.Create(configuration.BuildCorsPolicyOptions()));
        }

        private static string GetRequiredValue(IConfiguration configuration, string key)
        {
                return configuration.GetTrimmedValue(key)
                ?? throw new InvalidOperationException($"{key} no definido.");
        }
    }
}
