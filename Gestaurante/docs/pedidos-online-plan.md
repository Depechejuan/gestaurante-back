# Integración de Pedidos Online

Documento de referencia para la rama `codex/pedidos-online`.

## Objetivo
- Añadir cuentas de cliente separadas de empleados.
- Permitir pedido online con recogida o domicilio.
- Validar email por código.
- Soportar pago online simulado con método reutilizable tokenizado.
- Integrar cocina, camarero y repartidor en el mismo flujo operativo.

## Decisiones cerradas
- `UsuariosCliente` y `Empleados` son dominios distintos.
- El pago es simulado, pero el envío de email usa SMTP configurable.
- La tarjeta completa no se guarda; solo se conserva un token de pago y datos enmascarados.
- `Repartidor` se modela como nuevo rol interno.
- Las notificaciones internas son por polling, no por websocket.

## Estado de implementación esperado
- Back:
  - cuentas de cliente
  - verificación email
  - checkout online
  - pedido online integrado con `Pedido`
  - factura inmediata para pago online
  - recogida con pago local facturada al entregar
- Front:
  - login/registro/verificación de cliente
  - pedido online con carrito local
  - cuenta con direcciones, métodos de pago e histórico
  - panel staff/cocina/reparto con badges y colas filtradas

## Flujo operativo actual
- El pedido online nace confirmado y entra en el circuito interno de sala/cocina.
- El camarero envia lineas a cocina, cocina las marca preparadas y el camarero marca cada linea como ok.
- Si todas las lineas activas quedan ok:
  - `DOMICILIO` pasa a `PENDIENTE_ENTREGA`; el repartidor ve cliente, telefono, direccion y elementos.
  - `RECOGIDA` pasa a `EN_ESPERA`; el camarero lo mantiene pendiente hasta que el cliente pasa.
- El repartidor cambia `PENDIENTE_ENTREGA` a `EN_CAMINO` cuando sale y a `ENTREGADO` al finalizar.
- El camarero cambia `EN_ESPERA` a `ENTREGADO` cuando entrega la recogida; si el pago era local, se genera la factura pagada.

## Migraciones y datos existentes
- Los estados nuevos se incorporan con una migracion EF complementaria.
- El backfill solo mueve pedidos online en `LISTO`, no facturados y con todas sus lineas activas ya marcadas como ok.
- No se debe usar reset, borrado, truncate ni recreacion de base de datos para desplegar este cambio.
