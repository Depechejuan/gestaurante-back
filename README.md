# gestaurante-back
Back-end de la Aplicación de Gestaurante

Dependencias NuGet
- Pomelo.EntityFrameworkCore.MySql 

Instalación dotenv (lectura .env)
dotnet add package DotNetEnv

Comandos importantes en el Bash/PM
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add package Pomelo.EntityFrameworkCore.MySql --version 8.0.0

dotnet restore

dotnet clean
dotnet build

Si el Build lo hace sin errores (O con algún Warning)
dotnet ef migrations add InitialCreate
dotnet ef database update
