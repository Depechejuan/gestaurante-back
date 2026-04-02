# AGENTS.md

## Qué hace este proyecto
`gestaurante-back` es la API ASP.NET Core de Gestaurante. Gestiona:

- autenticación de empleados con JWT
- autenticación de clientes online con JWT separado
- empleados, clientes, mesas, pedidos, líneas de pedido y facturas
- catálogo de categorías, ingredientes y platos
- flujo QR por mesa
- pedido online con recogida o domicilio
- facturación, cobro, descuentos y envío por email

La persistencia está en PostgreSQL con Entity Framework Core.

## Arquitectura
La solución sigue un monolito modular:

- `Controllers`: capa HTTP
- `Models/Entities`: entidades persistidas
- `Models/DTO`: contratos de entrada y salida
- `Models/Services`: lógica de negocio y orquestación
- `Models/Data`: `DbContext`, factoría de diseño y seed
- `Models/Enums`: enums del dominio
- `Configuration`: opciones tipadas y carga de `.env`
- `Infrastructure`: bootstrap explícito de desarrollo
- `Middleware`: manejo global de excepciones
- `Utils`: helpers de mapeo y lógica compartida
- `Migrations`: migraciones EF Core
- `docs`: documentación interna

### Documentación de código
- guía de XML comments y criterio de documentación: [Gestaurante/docs/backend-documentation.md](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/docs/backend-documentation.md)
- los métodos públicos de controladores y servicios deben tener `summary`
- añadir `param`, `returns` y `remarks` cuando el contrato o la regla de negocio lo requieran

### Piezas clave
- Arranque y wiring: [Program.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Program.cs)
- Configuración tipada: [AppConfiguration.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Configuration/AppConfiguration.cs), [AppOptions.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Configuration/AppOptions.cs)
- Bootstrap controlado: [AppBootstrapService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Infrastructure/AppBootstrapService.cs)
- Manejo global de errores: [ApiExceptionMiddleware.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Middleware/ApiExceptionMiddleware.cs)
- Contexto EF: [AppDbContext.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Data/AppDbContext.cs)
- Seed: [DbInitializer.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Data/DbInitializer.cs)

## Ubicación de cada cosa

### Controladores principales
- empleados y auth interna: [UserController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/UserController.cs), [AdminController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/AdminController.cs)
- clientes internos: [ClienteController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/ClienteController.cs)
- catálogo público: [PublicCatalogController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/PublicCatalogController.cs)
- cuenta cliente: [PublicAccountController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/PublicAccountController.cs)
- checkout público: [PublicCheckoutController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/PublicCheckoutController.cs)
- flujo QR: [PublicMesaController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/PublicMesaController.cs)
- operación interna: [MesaController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/MesaController.cs), [PedidoController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/PedidoController.cs), [FacturaController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/FacturaController.cs)
- catálogo interno: [CategoriaController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/CategoriaController.cs), [IngredienteController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/IngredienteController.cs), [PlatoController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/PlatoController.cs)

### Servicios clave
- auth empleados: [LoginService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/LoginService.cs), [JwtService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/JwtService.cs)
- auth clientes: [CustomerAccountService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/CustomerAccountService.cs), [CustomerJwtService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/CustomerJwtService.cs)
- catálogo: [CatalogProjectionService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/CatalogProjectionService.cs), [CategoriaService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/CategoriaService.cs), [IngredienteService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/IngredienteService.cs), [PlatoService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/PlatoService.cs)
- operación: [MesaService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/MesaService.cs), [PedidoService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/PedidoService.cs), [FacturaService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/FacturaService.cs)
- QR y online: [MesaPublicSessionService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/MesaPublicSessionService.cs), [PublicCheckoutService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/PublicCheckoutService.cs)
- email: [IEmailService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/IEmailService.cs), [SmtpEmailService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/SmtpEmailService.cs)
- imágenes: [CloudinaryService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/CloudinaryService.cs), [CloudinaryEmployeeImageService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/CloudinaryEmployeeImageService.cs)

## Instalación y ejecución

### Requisitos
- .NET SDK 9
- PostgreSQL accesible
- `.env` configurado

### Compilar
```bash
cd /Users/juanleon/Documents/gestaurante/gestaurante-back
dotnet build Gestaurante/Gestaurante.csproj
```

### Variables de entorno
Crear [`.env`](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/.env) a partir de [`.env.example`](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/.env.example).

Mínimas:
```env
DB_HOST=
DB_PORT=
DB_USER=
DB_PASSWORD=
DB_NAME=
JWT_KEY=
JWT_ISSUER=
JWT_AUDIENCE=
CUSTOMER_JWT_KEY=
CUSTOMER_JWT_ISSUER=
CUSTOMER_JWT_AUDIENCE=
PORT=3003
```

Importantes:
- `BOOTSTRAP_*`: migraciones, seed y reparación de datos solo bajo modo explícito
- `DEFAULT_*_PASSWORD`: seed por defecto
- `SMTP_*`: emails de validación, facturas y avisos
- `CLOUDINARY_*`: imágenes de empleados
- `CORS_ALLOWED_ORIGINS`: orígenes de producción

### Ejecutar la API
```bash
cd /Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante
dotnet run
```

El arranque normal:
- carga `.env`
- valida configuración
- levanta la API
- no migra ni seedea por defecto

### Bootstrap explícito de desarrollo
```bash
cd /Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante
dotnet run -- --bootstrap
```

Ese modo:
- aplica migraciones
- ejecuta reparaciones controladas
- siembra datos por defecto

### Endpoints útiles
- [openapi/v1.json](http://localhost:3003/openapi/v1.json)
- [health](http://localhost:3003/health)

## Reglas útiles
- Los enums de dominio viven en [Models/Enums](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Enums).
- No metas lógica de negocio en controladores; muévela a `Models/Services`.
- Usa excepciones coherentes y deja que el middleware global traduzca el error HTTP.
- Si el cambio toca persistencia, revisa si necesita migración.
- El precio histórico siempre sale de `DetallePedido.PrecioUnitario`, no del precio actual del plato.
- Los clientes se activan/desactivan; no se borran para operación diaria.

## Puntos delicados
- Hay dos esquemas JWT: empleados y clientes.
- El bootstrap de desarrollo muta datos; el arranque normal no.
- La búsqueda de clientes para facturas debe ignorar clientes inactivos.
- El flujo QR y el pedido online comparten dominio, pero no autenticación.

## Comandos rápidos
```bash
# compilar
dotnet build Gestaurante/Gestaurante.csproj

# ejecutar
cd Gestaurante
dotnet run

# bootstrap explícito
cd Gestaurante
dotnet run -- --bootstrap

# migraciones
dotnet ef migrations add NombreMigracion
dotnet ef database update
```
