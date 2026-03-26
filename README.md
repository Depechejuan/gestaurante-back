# gestaurante-back
Back-end de la Aplicación de Gestaurante

## Documentación de Operaciones de Sala
- El plan técnico preparado para `mesas -> pedidos -> factura` está detallado en [Gestaurante/docs/operaciones-sala-plan.md](/Users/juanleon/Documents/gestaurante/gestaurante-back/Gestaurante/docs/operaciones-sala-plan.md).
- Este documento describe el alcance implementado sin tocar la lógica de `platos/ingredientes`, que se mantiene fuera de esta rama para no pisar el trabajo paralelo en curso.

## Roadmap futuro: catálogo, operación y pedido online

Este bloque resume cómo debería evolucionar el backend una vez quede cerrada la parte de `platos/ingredientes`.
La idea no es implementarlo todo a la vez, sino avanzar por fases y con contratos públicos separados de la API interna.

### Resumen
- Cerrar primero el catálogo real como fuente única de datos para cliente, staff y admin.
- Mantener la base ya preparada de `mesa -> pedido -> factura`.
- Reforzar la API pública para QR y reutilizar ese patrón en el futuro pedido online.
- Añadir después recogida y, en una fase posterior, `delivery` con pago online opcional.

### Prioridades de backend

#### Catálogo
- CRUD real de `Categoria`, `Plato` e `Ingrediente`.
- Disponibilidad real de platos e ingredientes.
- Soporte para imágenes, alérgenos y orden visual de carta.
- Endpoints públicos de catálogo desacoplados de la API interna.

#### Operación interna
- Mantener las reglas de estados de pedido y línea ya definidas.
- Consolidar cancelaciones de línea y de pedido como flujo operativo real.
- Mantener la factura como snapshot de cierre, no como referencia dinámica al precio del plato.
- Añadir auditoría mínima:
  - quién cancela
  - quién cierra mesa
  - quién genera factura

#### API pública de cliente
- Mantener sesiones públicas temporales para QR.
- Repetir el mismo patrón para checkout público sin registro.
- No reutilizar JWT interno para cliente.
- Añadir rate limiting y validaciones anti abuso en endpoints públicos.

### Plan por fases

#### Fase 1: QR de mesa completo
- Exponer catálogo real para `/public/mesa/{id}`.
- Permitir consultar pedidos de la sesión pública.
- Crear pedidos reales desde QR con precio copiado en `DetallePedido`.
- Invalidar sesiones públicas al cerrar la mesa.

#### Fase 2: pedido online para recogida
- Crear endpoints públicos de checkout sin registro.
- Introducir una sesión pública de cliente fuera de mesa.
- Persistir el pedido solo cuando el cliente lo confirme.
- Permitir consultar el pedido mediante token temporal público.

Endpoints orientativos:
- `GET /public/catalogo`
- `POST /public/checkout/session`
- `POST /public/checkout/pedido`
- `GET /public/checkout/pedido/{publicToken}`

#### Fase 3: delivery y pago online opcional
- Añadir tipo de entrega:
  - `RECOGIDA`
  - `DELIVERY`
  - `MESA`
- Añadir dirección, coste y zona de reparto.
- Añadir estado de pago y proveedor de pago externo.
- Soportar pago online o pago en local según canal.

#### Fase 4: capacidades comerciales
- descuentos
- promociones
- pedidos favoritos
- repetición rápida
- notificaciones
- reserva online

### Cambios de dominio recomendados
- `Pedido`:
  - canal de origen
  - tipo de entrega
  - datos mínimos de cliente
  - observaciones
  - total snapshot
- `DetallePedido`:
  - precio unitario copiado
  - observaciones
  - estado de línea
- `Factura`:
  - método de pago
  - canal
  - total bruto
  - descuento
  - total final
- Sesión pública:
  - mantener `MesaPublicSession`
  - añadir una sesión pública equivalente para checkout futuro

### Decisiones ya fijadas
- El cliente no debe registrarse obligatoriamente para pedir.
- Los cambios de carrito no deben saturar la base de datos.
- El carrito vive en cliente hasta que se confirma el pedido.
- Una vez enviado, el pedido se vuelve inmutable y cualquier corrección posterior se hace por cancelación.
- El histórico de facturas y pedidos debe conservar el precio original aunque el plato cambie después.


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
