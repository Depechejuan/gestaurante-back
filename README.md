# gestaurante-back
Back-end de la Aplicación de Gestaurante

## Documentación de Operaciones de Sala
- El plan técnico preparado para `mesas -> pedidos -> factura` está detallado en [Gestaurante/docs/operaciones-sala-plan.md](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/docs/operaciones-sala-plan.md).
- Este documento describe el alcance implementado sin tocar la lógica de `platos/ingredientes`, que se mantiene fuera de esta rama para no pisar el trabajo paralelo en curso.


# Dependencias NuGet
- Pomelo.EntityFrameworkCore.MySql 

# Instalación dotenv (lectura .env)
dotnet add package DotNetEnv

# Comandos importantes en el Bash/PM para la Base de Datos
Microsoft.EntityFrameworkCore          9.0.1
Microsoft.EntityFrameworkCore.Design   9.0.1
Npgsql.EntityFrameworkCore.PostgreSQL  9.0.4


dotnet restore
dotnet clean
dotnet build

- Si el Build lo hace sin errores (O con algún Warning)
dotnet ef migrations add InitialCreate
dotnet ef database update

# JsonWebToken
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
Si no funciona:
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 9.0.0



# Usuarios de Prueba - Uso Interno
Todas las contraseñas son las genéricas del .env

Administradores:
admin@gestaurante.com

Cocineros:
    lucas.romero@gestaurante.com
    maria.santos@gestaurante.com
    alberto.molina@gestaurante.com
    natalia.ramos@gestaurante.com

Camareros:
paula.garcia@gestaurante.com
    diego.herrera@gestaurante.com
    laura.perez@gestaurante.com
    jorge.ruiz@gestaurante.com
    elena.flores@gestaurante.com
    sergio.ortiz@gestaurante.com
