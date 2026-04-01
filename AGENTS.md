# AGENTS.md

## Qué hace este proyecto
`gestaurante-back` es la API ASP.NET Core de Gestaurante. Gestiona:

- autenticación de empleados por JWT
- autenticación de clientes online con JWT separado
- empleados, clientes, mesas, pedidos, líneas de pedido y facturas
- catálogo de categorías, ingredientes y platos
- flujo QR por mesa
- pedido online con recogida o domicilio
- facturación, cobro, descuentos y envío por email

La base de datos es PostgreSQL y el proyecto usa Entity Framework Core con migraciones.

## Arquitectura
La solución está organizada como una API monolítica modular.

- `Controllers`: capa HTTP
- `Models/Entities`: entidades persistidas
- `Models/DTO`: contratos de entrada y salida
- `Models/Services`: lógica de negocio
- `Models/Data`: `DbContext`, factory y seed
- `Models/Enums`: enums compartidos del dominio
- `Utils`: helpers de mapeo, respuesta y lógica de apoyo
- `Migrations`: migraciones EF Core
- `docs`: documentación interna y planes

### Flujo general
- Los controladores reciben DTOs.
- Los servicios hacen validación de negocio y persistencia.
- EF Core trabaja contra [AppDbContext.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Data/AppDbContext.cs).
- En el arranque se aplican migraciones y se ejecuta seed mínimo.

## Ubicación de cada cosa

### Punto de entrada
- [Program.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Program.cs)

### Controladores principales
- `auth empleados`: [UserController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/UserController.cs)
- `admin`: [AdminController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/AdminController.cs)
- `clientes internos`: [ClienteController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/ClienteController.cs)
- `catálogo público`: [PublicCatalogController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/PublicCatalogController.cs)
- `cuenta cliente`: [PublicAccountController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/PublicAccountController.cs)
- `checkout público`: [PublicCheckoutController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/PublicCheckoutController.cs)
- `flujo QR`: [PublicMesaController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/PublicMesaController.cs)
- `mesas`: [MesaController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/MesaController.cs)
- `pedidos`: [PedidoController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/PedidoController.cs)
- `facturas`: [FacturaController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/FacturaController.cs)
- `catálogo interno`: [CategoriaController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/CategoriaController.cs), [IngredienteController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/IngredienteController.cs), [PlatoController.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Controllers/PlatoController.cs)

### Servicios clave
- autenticación empleados: [LoginService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/LoginService.cs), [JwtService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/JWTService.cs)
- autenticación clientes: [CustomerAccountService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/CustomerAccountService.cs), [CustomerJwtService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/CustomerJwtService.cs)
- mesas y QR: [MesaService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/MesaService.cs), [MesaPublicSessionService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/MesaPublicSessionService.cs)
- pedidos: [PedidoService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/PedidoService.cs), [PublicCheckoutService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/PublicCheckoutService.cs)
- facturas: [FacturaService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/FacturaService.cs)
- catálogo: [CategoriaService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/CategoriaService.cs), [IngredienteService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/IngredienteService.cs), [PlatoService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/PlatoService.cs)
- email: [IEmailService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/IEmailService.cs)
- imágenes empleado: [CloudinaryEmployeeImageService.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Services/CloudinaryEmployeeImageService.cs)

### Datos y persistencia
- contexto EF: [AppDbContext.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Data/AppDbContext.cs)
- seed: [DbInitializer.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Data/DbInitializer.cs)
- migraciones: [Migrations](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Migrations)

### Utilidades
- alérgenos: [AllergenResolver.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Utils/AllergenResolver.cs)
- ingredientes visibles para carta pública: [PublicIngredientResolver.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Utils/PublicIngredientResolver.cs)
- helpers de DTO: [ToDTO.cs](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Utils/ToDTO.cs)

## Entidades importantes
- empleados: `Administrador`, `Camarero`, `Cocinero`, `Repartidor`
- clientes: `UsuarioCliente`
- catálogo: `Categoria`, `Ingrediente`, `Plato`, `PlatoIngrediente`
- operación: `Mesa`, `MesaPublicSession`, `Pedido`, `DetallePedido`, `Factura`
- cliente online: `ClienteDireccion`, `ClienteMetodoPago`, `ClienteEmailVerification`

## Instalación y ejecución

### Requisitos
- .NET SDK 9
- PostgreSQL accesible
- variables de entorno configuradas

### Restaurar y compilar
```bash
cd /Users/juanleon/Documents/gestaurante/gestaurante-back
dotnet build Gestaurante/Gestaurante.csproj
```

### Variables de entorno
Crear [`.env`](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/.env) a partir de [`.env.example`](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/.env.example).

Variables mínimas:
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

Variables opcionales/importantes:
- `DEFAULT_*_PASSWORD`: seed de usuarios por defecto
- `SMTP_*`: emails de validación, facturas y notificaciones
- `CLOUDINARY_*`: imágenes de empleados

### Ejecutar en desarrollo
```bash
cd /Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante
dotnet run
```

La API:
- carga `.env`
- aplica migraciones
- ejecuta seed mínimo
- escucha en `http://localhost:${PORT}`

### OpenAPI
Con la API levantada:
- [openapi/v1.json](http://localhost:3003/openapi/v1.json)

## Reglas útiles para trabajar aquí
- Los enums del dominio viven en [Models/Enums](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/Models/Enums).
- Si el cambio toca base de datos, revisa si necesita migración.
- No metas lógica de negocio pesada en controladores: muévela a `Models/Services`.
- No dupliques reglas entre el catálogo público y el interno si pueden centralizarse.
- El precio histórico debe venir de `DetallePedido.PrecioUnitario`, no del precio actual del plato.
- Los clientes no se eliminan físicamente para operación diaria; se activan/desactivan.
- Las facturas anónimas parten de un cliente anónimo controlado por seed.

## Puntos delicados
- El arranque ejecuta migraciones y seed automáticamente.
- Hay dos esquemas de autenticación:
  - empleados
  - clientes
- Algunas pantallas del front dependen de endpoints públicos y privados distintos.
- La búsqueda de clientes para facturas debe ignorar clientes inactivos.
- El flujo QR y el pedido online comparten partes del dominio, pero no la autenticación.

## Comandos rápidos
```bash
# compilar
dotnet build Gestaurante/Gestaurante.csproj

# ejecutar
cd Gestaurante
dotnet run

# crear migración
dotnet ef migrations add NombreMigracion

# aplicar migraciones
dotnet ef database update
```
