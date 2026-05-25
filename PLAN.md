# Plan de migración: ASP.NET → Vercel Serverless

**Estrategia elegida:** Convertir cada endpoint a una Vercel Serverless Function en TypeScript/Node.js, ya que es el runtime con mayor soporte y estabilidad en Vercel. El agente reescribe la lógica; no se usa el runtime de comunidad .NET (demasiado inestable).

---

## Fase 0 — Auditoría del proyecto

El agente debe ejecutar esto **primero**, antes de tocar nada.

**Tareas:**

1. Listar todos los Controllers y sus rutas (`[Route]`, `[HttpGet]`, `[HttpPost]`...).
2. Listar todos los middlewares activos (`Program.cs` / `Startup.cs`).
3. Identificar el ORM usado (Entity Framework, Dapper, ADO.NET...).
4. Identificar el motor de base de datos (SQL Server, PostgreSQL, SQLite...).
5. Listar todos los servicios inyectados (DI container).
6. Listar variables de entorno usadas (`appsettings.json` / environment).
7. Identificar autenticación (JWT, Cookies, OAuth...).
8. Detectar SignalR, background jobs o WebSockets (⚠️ incompatibles con serverless).

**Output esperado:** `/audit/inventory.md` con tabla de endpoints y dependencias.

---

## Fase 1 — Scaffolding del proyecto Vercel

**Tareas:**

1. Crear nuevo repo (o carpeta `/vercel-app` en el mismo repo).
2. Inicializar: `npm init -y`.
3. Instalar dependencias base:
   - `typescript`, `@types/node`
   - `@vercel/node` (tipos para las functions)
   - ORM equivalente según lo detectado en Fase 0
4. Crear `tsconfig.json`.
5. Crear `vercel.json` con la estructura de rutas base:

   ```json
   {
     "rewrites": [
       { "source": "/api/(.*)", "destination": "/api/$1" }
     ]
   }
   ```

6. Crear estructura de carpetas:

   ```
   /api
     /auth
     /[recurso1]
     /[recurso2]
   /lib
     db.ts        ← conexión a base de datos
     auth.ts      ← middleware de autenticación
     errors.ts    ← manejo de errores estándar
   ```

---

## Fase 2 — Capa de base de datos

**Tareas (dependen del ORM detectado):**

**SI Entity Framework + SQL Server:**
- Migrar a Prisma ORM + conexión a PlanetScale / Neon / Supabase.
- Exportar esquema actual: `dotnet ef migrations script`.
- Traducir modelos C# a `schema.prisma`.
- Ejecutar: `prisma migrate deploy` en nueva DB.

**SI Dapper + PostgreSQL:**
- Mantener queries SQL, usar `postgres` (npm) o `pg`.
- Crear `/lib/db.ts` con pool de conexiones.

> ⚠️ **Regla crítica para serverless:**
> - Usar connection pooling externo (PgBouncer, Prisma Accelerate).
> - **NUNCA** abrir una conexión nueva por función sin pool.

---

## Fase 3 — Conversión de endpoints (por cada Controller)

El agente sigue este patrón para cada endpoint.

**Origen (C#):**

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetItem(int id) {
    var item = await _service.GetById(id);
    if (item == null) return NotFound();
    return Ok(item);
}
```

**Destino (TypeScript `/api/items/[id].ts`):**

```ts
import type { VercelRequest, VercelResponse } from '@vercel/node'
import { db } from '../../lib/db'
import { requireAuth } from '../../lib/auth'

export default async function handler(req: VercelRequest, res: VercelResponse) {
  if (req.method !== 'GET') return res.status(405).end()

  const authError = await requireAuth(req)
  if (authError) return res.status(401).json(authError)

  const { id } = req.query
  const item = await db.item.findUnique({ where: { id: Number(id) } })

  if (!item) return res.status(404).json({ error: 'Not found' })
  return res.status(200).json(item)
}
```

**Checklist por endpoint:**

- □ Ruta traducida correctamente en `/api/...`
- □ Métodos HTTP filtrados (GET/POST/PUT/DELETE)
- □ Autenticación aplicada si el original la tenía
- □ Validación de inputs equivalente
- □ Respuestas de error con mismo código HTTP
- □ Lógica de negocio extraída a `/lib/[servicio].ts` (no inline)
- □ Test manual documentado en `/audit/endpoints-tested.md`

---

## Fase 4 — Middlewares y auth

**Tareas:**

1. **JWT Auth:**
   - Instalar: `jsonwebtoken`, `@types/jsonwebtoken`.
   - Crear `/lib/auth.ts` que replique la lógica de validación.
   - Extraer secret de Vercel Environment Variables.

2. **CORS:**
   - Manejar en cada función o crear `/lib/cors.ts` helper.
   - Replicar la política del `AddCors()` original.

3. **Rate limiting:**
   - Si existía: usar `@vercel/kv` o Upstash Redis.
   - Si no existía: considerar añadirlo (Vercel lo expone fácil).

4. **Logging:**
   - Reemplazar `ILogger` por `console.log` estructurado.
   - Considerar Axiom o Vercel Log Drains si hay logging avanzado.

---

## Fase 5 — Variables de entorno

**Tareas:**

1. Mapear cada entrada de `appsettings.json` / `appsettings.Production.json` a su equivalente en Vercel Environment Variables.
2. Crear `.env.local` para desarrollo local (en `.gitignore`).
3. Configurar en Vercel Dashboard:
   - Production secrets.
   - Preview secrets (pueden apuntar a DB de staging).
4. Actualizar todo `process.env.VARIABLE` en el código (nunca hardcodear, nunca commitear).

---

## Fase 6 — `vercel.json` final

```json
{
  "version": 2,
  "buildCommand": "npm run build",
  "functions": {
    "api/**/*.ts": {
      "memory": 512,
      "maxDuration": 10
    }
  },
  "rewrites": [
    { "source": "/api/(.*)", "destination": "/api/$1" }
  ],
  "headers": [
    {
      "source": "/api/(.*)",
      "headers": [
        { "key": "Access-Control-Allow-Origin", "value": "*" },
        { "key": "Access-Control-Allow-Methods", "value": "GET,POST,PUT,DELETE,OPTIONS" }
      ]
    }
  ]
}
```

---

## Fase 7 — Validación y despliegue

**Tareas:**

1. Ejecutar `vercel dev` localmente.
2. Para cada endpoint del `inventory.md`:
   - □ Test happy path
   - □ Test error path (404, 401, 400)
   - □ Test con datos inválidos
3. Comparar respuestas con el backend original (diff de JSON).
4. Deploy a Preview: `vercel deploy`.
5. Smoke test en Preview URL.
6. Deploy a Production: `vercel deploy --prod`.

---

## ⚠️ Incompatibilidades

> El agente debe **reportar** estas, no resolverlas solo.

| Característica ASP.NET        | Estado en Vercel | Acción requerida                              |
| ----------------------------- | ---------------- | --------------------------------------------- |
| SignalR / WebSockets          | ❌ Incompatible  | Migrar a Pusher / Ably / Supabase Realtime    |
| Background Jobs (Hangfire)    | ❌ Incompatible  | Migrar a Vercel Cron + Queue externa          |
| Streaming HTTP largo          | ⚠️ Limitado      | Max 10s en Hobby, 300s en Pro                 |
| Static files serving          | ✅ Nativo        | Mover a `/public`                             |
| Sesiones en memoria           | ❌ Incompatible  | Migrar a Redis / KV store                     |

---

El agente debe generar `/audit/inventory.md` en la Fase 0 y actualizar `/audit/endpoints-tested.md` al final de cada endpoint convertido, para tener trazabilidad completa del progreso.
