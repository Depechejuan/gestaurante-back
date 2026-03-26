# Plan de Operaciones de Sala

## Contexto actual
Este documento deja fijado cómo se prepara el backend para el flujo operativo de sala sin invadir la lógica de `platos/ingredientes`, que está siendo desarrollada en paralelo.

La base sobre la que se trabaja es:
- usuarios y JWT ya funcionales
- CRUD básico de `Mesa`, `Pedido`, `DetallePedido` y `Factura`
- `Plato` aún en transición funcional, pero con estructura suficiente para resolver precios al crear pedidos

## Objetivo de esta rama
Convertir el CRUD actual en un flujo de negocio consistente para restaurante:
- una mesa puede acumular varios pedidos
- cada pedido queda cerrado estructuralmente en cuanto se envía
- si el cliente quiere algo más, se crea otro pedido
- los camareros no borran líneas para “editar”, sino que cancelan líneas o pedidos completos
- las líneas o pedidos cancelados no cuentan en la factura
- cerrar una mesa significa recopilar todos los pedidos válidos pendientes de facturar y generar una factura

## Decisiones de negocio fijadas
### 1. Pedido inmutable tras envío
El `POST /Pedido` representa el momento en el que el pedido ya ha sido enviado.

Consecuencias:
- no se permite añadir nuevas líneas a un pedido existente
- no se permite cambiar plato o cantidad de una línea ya enviada
- no se permite borrar líneas para alterar el histórico

En lugar de eso se habilita:
- cancelación completa del pedido
- cancelación individual de línea

### 2. Cancelación en vez de borrado operativo
La cancelación deja rastro en base de datos:
- la línea pasa a `EstadoDetallePedido.CANCELADA`
- se conserva `PrecioUnitario` copiado en el detalle
- la línea cancelada deja de contar para la factura

Si todas las líneas de un pedido quedan canceladas:
- el pedido pasa a `EstadoPedido.CANCELADO`

### 3. Facturación por mesa
La factura deja de ser un simple cálculo manual suelto y pasa a poder construirse desde:
- una mesa completa
- un pedido individual
- un importe manual, si no hay vínculo operativo

Para el cierre de mesa:
- se toman solo pedidos de esa mesa
- solo pedidos no cancelados
- solo pedidos no facturados todavía
- solo líneas activas

El cierre de mesa:
- crea una nueva factura
- vincula los pedidos incluidos a esa factura
- deja la mesa disponible si ya no quedan líneas activas sin facturar

## Cambios de modelo preparados
### Pedido
Se amplía con:
- `IdMesa`
- `IdFactura`

Esto permite:
- saber a qué mesa pertenece cada pedido
- saber si ya fue facturado
- agrupar varios pedidos dentro de una única factura de cierre

### DetallePedido
Se amplía con:
- `Estado`
- `FechaCancelacion`

Con esto el histórico queda protegido y no hace falta borrar líneas para “corregir”.

### Factura
Se amplía con:
- `IdMesa`

Así la factura puede representar el cierre de una mesa completa, no solo de un pedido aislado.

## Contrato HTTP preparado
### Mesas
- `GET /Mesa`
  Devuelve resumen de mesas con pedidos abiertos y total pendiente.
- `GET /Mesa/{id}`
  Devuelve detalle de mesa con sus pedidos y resumen operativo.
- `POST /Mesa`
- `PUT /Mesa/{id}`
- `DELETE /Mesa/{id}`
  Protegido para no borrar mesas con pedidos asociados.
- `POST /Mesa/{id}/cerrar`
  Genera la factura agregando pedidos válidos de la mesa.

### Pedidos
- `GET /Pedido`
- `GET /Pedido/{id}`
- `POST /Pedido`
  Requiere `IdMesa`.
- `PUT /Pedido/{id}`
  Solo para transiciones válidas de estado.
- `DELETE /Pedido/{id}`
  Sigue existiendo como operación técnica, pero no es el flujo operativo recomendado.
- `POST /Pedido/{id}/cancelar`
  Cancela el pedido completo.
- `GET /Pedido/{pedidoId}/linea/{detalleId}`
- `POST /Pedido/{pedidoId}/linea/{detalleId}/cancelar`
  Cancela una línea concreta.

### Facturas
- `GET /Factura`
- `GET /Factura/{id}`
- `POST /Factura`
  Soporta:
  - `IdMesa`
  - `IdPedido`
  - `PrecioTotal` manual
- `PUT /Factura/{id}`
  Restringe reasignaciones y cambios manuales peligrosos sobre facturas ya vinculadas a pedidos.
- `DELETE /Factura/{id}`
  Se bloquea si la factura ya tiene pedidos asociados.

## Reglas de estado del pedido
Las transiciones válidas quedan fijadas así:
- `PENDIENTE -> CONFIRMADO` o `CANCELADO`
- `CONFIRMADO -> PREPARACION` o `CANCELADO`
- `PREPARACION -> LISTO` o `CANCELADO`
- `LISTO -> ENTREGADO` o `CANCELADO`

Estados terminales:
- `ENTREGADO`
- `CANCELADO`

## Permisos
Se endurecen los controladores usando los roles ya emitidos en JWT:
- `MesaController`: `Administrador`, `Camarero`
- `FacturaController`: `Administrador`, `Camarero`
- `PedidoController`: `Administrador`, `Camarero`, `Cocinero`

Restricción adicional:
- las acciones de cancelación quedan limitadas a `Administrador` y `Camarero`

## Qué queda pendiente a propósito
### 1. Flujo público QR de cliente
Todavía no se fija aquí porque falta cerrar el contrato público.

Para soportarlo bien faltará decidir:
- identificador público de mesa apto para QR
- si el cliente usará token temporal, sesión anónima firmada o pedido público con validación por mesa
- caducidad exacta y protección anti-reenvío

Con el estado actual, el dominio interno ya está preparado, pero el endpoint público del cliente aún debe definirse.

### 2. Platos, ingredientes y categorías
No se profundiza aquí porque esa parte está en desarrollo paralelo.

Sí se conserva una premisa importante:
- el precio se copia al `DetallePedido` en el momento de creación

Eso garantiza que una subida de precio futura del plato no altera pedidos ya realizados.

### 3. Factura documental
No se implementa aún:
- PDF
- envío por email
- numeración fiscal final

La factura actual queda preparada como entidad operativa y de cierre, no como documento fiscal final.

## Riesgos y notas
- `Mesa` sigue usando `Guid` interno. Para el QR público probablemente convendrá añadir un identificador visible o corto más adelante.
- `DELETE` en `Pedido` sigue existiendo por compatibilidad técnica, pero el flujo recomendado en producción es cancelar, no borrar.
- Si se decide permitir edición antes de “confirmar” un pedido, habría que introducir un estado de borrador explícito. Con las reglas actuales no existe ese borrador en backend.

## Resultado esperado para el front
Con esta base, el front de staff puede:
- listar mesas con consumo pendiente
- abrir una mesa y ver sus pedidos reales
- cancelar líneas o pedidos
- avanzar estados de cocina/sala
- cerrar la mesa y recibir una factura agregada

Todo esto sin depender aún del módulo completo de `platos/ingredientes`.
