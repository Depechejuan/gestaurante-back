# Integración de Pedidos Online

Documento de referencia para la rama `codex/pedidos-online`.

## Objetivo
- Añadir cuentas de cliente separadas de empleados.
- Permitir pedido online con recogida o domicilio.
- Validar email por código.
- Soportar pago online mock con método reutilizable tokenizado.
- Integrar cocina, camarero y repartidor en el mismo flujo operativo.

## Decisiones cerradas
- `UsuariosCliente` y `Empleados` son dominios distintos.
- El pago es mock, pero el envío de email usa SMTP configurable.
- La tarjeta completa no se guarda; solo se conserva un token mock y datos enmascarados.
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
