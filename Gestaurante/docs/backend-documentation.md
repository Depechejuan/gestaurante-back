# Documentación del Back-end

## Objetivo
Este documento define cómo documentar el código C# de `gestaurante-back` para que la base de código siga siendo mantenible a medida que crecen los flujos de:

- autenticación interna y de cliente
- catálogo
- mesas, pedidos y cocina
- facturación y cobros
- QR y pedido online

La idea no es añadir comentarios por añadir, sino dejar claro:

- qué hace cada clase
- qué espera cada método
- qué devuelve
- qué reglas de negocio o efectos laterales conviene conocer

## Qué se documenta con XML
Usamos comentarios XML en C# con:

- `<summary>` para describir intención y responsabilidad
- `<param>` para entradas que afectan a la lógica o al contrato
- `<returns>` cuando el resultado no es obvio
- `<remarks>` cuando hay reglas de negocio, efectos laterales o restricciones importantes

## Criterio práctico

### Siempre
- clases públicas de controladores, servicios y validaciones reutilizables
- constructores con dependencias relevantes
- métodos públicos de controladores y servicios
- helpers privados complejos que encapsulan reglas de negocio

### Solo si aporta valor
- métodos privados triviales de una línea
- propiedades autoexplicativas
- mapeos extremadamente obvios

## Reglas por capa

### Controllers
Cada endpoint debe dejar claro:

- qué hace
- qué DTO recibe
- qué devuelve
- cuándo puede responder `404`, `401` o similares si no es evidente

Ejemplo:

```csharp
/// <summary>
/// Crea un pedido online para el cliente autenticado.
/// </summary>
/// <param name="dto">Datos del checkout, incluyendo líneas, entrega y pago.</param>
/// <param name="cancellationToken">Token de cancelación de la petición HTTP.</param>
/// <returns>Respuesta HTTP con el pedido creado o un error de autorización si no hay cliente válido.</returns>
public async Task<IActionResult> CreateOrder([FromBody] CreateOnlineOrderDTO dto, CancellationToken cancellationToken)
```

### Services
Aquí es donde más contexto hace falta. Además de `summary`, normalmente conviene añadir:

- `param` y `returns`
- `remarks` si:
  - se copian snapshots históricos
  - se generan facturas automáticamente
  - se invalidan sesiones
  - se envían emails
  - se recalculan estados operativos

### Validation y Utils
Documentar sobre todo:

- qué valida el componente
- qué formato espera
- qué algoritmo usa si no es obvio

## Convenciones del proyecto
- Escribir la documentación en español, igual que el resto del dominio.
- Describir comportamiento real, no implementación mecánica.
- Evitar comentarios redundantes tipo “asigna el valor”.
- Si una regla de negocio es delicada, usar `<remarks>` en vez de inflar el `<summary>`.
- Si un método tiene efectos laterales importantes, mencionarlos explícitamente.

## Casos donde conviene añadir `<remarks>`

### PedidoService.CreateAsync
- copia `PrecioUnitario` en `DetallePedido`
- puede ocupar una mesa
- puede vincular una sesión pública QR

### PublicCheckoutService.CreateOnlineOrderAsync
- puede generar factura automática
- puede enviar correo
- aplica reglas de envío y pago online

### FacturaService
- diferencia entre snapshot anónimo y cliente real
- no debe romper histórico
- distingue cobro local y cobro online

## Qué revisar antes de dar por cerrada una documentación
- ¿El método público tiene al menos `<summary>`?
- ¿Los parámetros relevantes tienen `<param>`?
- ¿El valor devuelto necesita `<returns>`?
- ¿Hay reglas de negocio no obvias que merecen `<remarks>`?
- ¿El texto sigue siendo cierto tras los últimos cambios?

## Mantenimiento
Cuando se cambie una función pública o una regla de negocio importante:

1. Actualizar el XML del método afectado.
2. Si cambia una regla transversal, actualizar también este documento.
3. Si el cambio afecta al uso del proyecto, revisar [README.md](/Users/juanleon/Documents/gestaurante/gestaurante-back/README.md) y [AGENTS.md](/Users/juanleon/Documents/gestaurante/gestaurante-back/AGENTS.md).
4. Si el cambio requiere corregir datos existentes, hacerlo mediante una migracion incremental; no usar reset, drop, truncate ni recreacion de base de datos.
