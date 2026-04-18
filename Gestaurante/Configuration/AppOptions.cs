namespace Gestaurante.Configuration
{
    public class DatabaseOptions
    {
        public string? ConnectionString { get; set; }
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 5432;
        public string Name { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RequireSsl { get; set; } = true;
        public bool TrustServerCertificate { get; set; } = true;

        public string BuildConnectionString()
        {
            if (!string.IsNullOrWhiteSpace(ConnectionString))
                return ConnectionString;

            var sslMode = RequireSsl ? "Require" : "Disable";
            var trustServerCertificate = TrustServerCertificate ? "true" : "false";
            return $"Server={Host};Port={Port};Database={Name};User Id={User};Password={Password};SSL Mode={sslMode};Trust Server Certificate={trustServerCertificate};";
        }
    }

    public class EmployeeJwtOptions
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpireDays { get; set; } = 30;
    }

    public class CustomerJwtOptions
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpireDays { get; set; } = 30;
    }

    public class SmtpOptions
    {
        public string? Host { get; set; }
        public int? Port { get; set; }
        public string? User { get; set; }
        public string? Password { get; set; }
        public string FromEmail { get; set; } = "no-reply@gestaurante.local";
        public string FromName { get; set; } = "Gestaurante";

        public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && Port.HasValue;
    }

    public class CloudinaryOptions
    {
        public string? CloudName { get; set; }
        public string? ApiKey { get; set; }
        public string? ApiSecret { get; set; }
        public string EmployeeFolder { get; set; } = "gestaurante/empleados";

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(CloudName)
            && !string.IsNullOrWhiteSpace(ApiKey)
            && !string.IsNullOrWhiteSpace(ApiSecret);

        public string ResolveImageUrl(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return string.Empty;

            if (Uri.TryCreate(imagePath, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                return imagePath;

            if (string.IsNullOrWhiteSpace(CloudName))
                return imagePath;

            return $"https://res.cloudinary.com/{CloudName}/image/upload/{imagePath.TrimStart('/')}";
        }
    }

    public class SeedOptions
    {
        public string DefaultAdminPassword { get; set; } = string.Empty;
        public string DefaultCamareroPassword { get; set; } = string.Empty;
        public string DefaultCocineroPassword { get; set; } = string.Empty;
        public string DefaultRepartidorPassword { get; set; } = string.Empty;
        public string DefaultClientPassword { get; set; } = string.Empty;

        public void EnsureReady()
        {
            if (string.IsNullOrWhiteSpace(DefaultAdminPassword))
                throw new InvalidOperationException("DEFAULT_ADMIN_PASSWORD no definido.");

            if (string.IsNullOrWhiteSpace(DefaultCamareroPassword))
                throw new InvalidOperationException("DEFAULT_CAMARERO_PASSWORD no definido.");

            if (string.IsNullOrWhiteSpace(DefaultCocineroPassword))
                throw new InvalidOperationException("DEFAULT_COCINERO_PASSWORD no definido.");

            if (string.IsNullOrWhiteSpace(DefaultClientPassword))
                throw new InvalidOperationException("DEFAULT_CLIENT_PASSWORD no definido.");
        }
    }

    public class BootstrapOptions
    {
        public bool RunOnStart { get; set; }
        public bool ApplyMigrations { get; set; } = true;
        public bool SeedDefaults { get; set; } = true;
        public bool RepairData { get; set; } = true;
        public bool ImportCatalog { get; set; }
        public string? CatalogImportPath { get; set; }
    }

    public class CorsPolicyOptions
    {
        public List<string> AllowedOrigins { get; set; } = new();
    }
}
