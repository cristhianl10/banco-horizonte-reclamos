# Configuración de Supabase — pasos exactos

El código ya está preparado. Estas acciones requieren tu cuenta de Supabase y son las únicas que debes realizar manualmente.

## 1. Crear el proyecto

1. Ingresa a [Supabase Dashboard](https://supabase.com/dashboard).
2. Selecciona **New project**.
3. Nombre recomendado: `banco-horizonte-reclamos`.
4. Genera y guarda una contraseña fuerte para PostgreSQL.
5. Elige la región más cercana y espera a que el proyecto termine de crearse.

## 2. Crear tablas, catálogos, trigger y seguridad

1. Abre **SQL Editor** → **New query**.
2. Copia todo el contenido de `database/schema.sql`.
3. Presiona **Run** una sola vez.
4. Verifica que no existan errores.

El script crea:

- Tablas normalizadas, índices y claves foráneas.
- Roles de aplicación: Operador, Analista, Supervisor y Administrador.
- Categorías, estados, transiciones, prioridades y políticas SLA.
- Un trigger que crea el perfil público al crear un usuario de Auth.
- El rol inicial `Operador` para todo usuario nuevo.
- RLS sobre todas las tablas públicas sin políticas de acceso desde el navegador. La API .NET accede mediante conexión PostgreSQL y el frontend no puede consultar tablas directamente.

## 3. Configurar Auth

1. Abre **Authentication** → **Providers** → **Email**.
2. Mantén habilitado **Email**.
3. Para el prototipo académico puedes desactivar **Confirm email**. En producción se recomienda activarlo.
4. Abre **Authentication** → **URL Configuration**.
5. En **Site URL** escribe `http://localhost:4200`.
6. Agrega `http://localhost:4200/**` en **Redirect URLs**.
7. En **JWT Signing Keys**, usa una clave asimétrica activa (ES256 o RS256). La API valida los JWT mediante el endpoint JWKS público del proyecto.

## 4. Crear usuarios

En **Authentication** → **Users** → **Add user**, crea al menos:

| Correo sugerido | Rol que asignarás |
|---|---|
| `operador@bancohorizonte.com` | Operador |
| `analista@bancohorizonte.com` | Analista |
| `supervisor@bancohorizonte.com` | Supervisor |
| `admin@bancohorizonte.com` | Administrador |

El trigger crea automáticamente una fila en `public.usuarios` y asigna inicialmente `Operador`.

## 5. Asignar roles empresariales

1. Abre `database/assign-role.sql`.
2. Reemplaza `CAMBIAR@CORREO.COM` por el correo creado.
3. Reemplaza `Supervisor` por el rol deseado.
4. Si quieres un solo rol, ejecuta primero el `delete` comentado.
5. Ejecuta el script una vez por usuario.

También puedes hacerlo directamente:

```sql
delete from public.usuario_roles
where usuario_id = (select id from public.usuarios where correo = 'analista@bancohorizonte.com');

insert into public.usuario_roles(usuario_id, rol_id)
select u.id, r.id
from public.usuarios u cross join public.roles r
where u.correo = 'analista@bancohorizonte.com' and r.nombre = 'Analista';
```

## 6. Obtener valores de conexión

### URL y publishable key

1. Abre **Project Settings** → **API Keys**.
2. Copia **Project URL**.
3. Copia la **Publishable key**. Esta clave sí puede estar en el frontend.
4. No copies ni publiques una **Secret key** o `service_role`.

Edita `src/banco-horizonte-web/src/environments/environment.ts`:

```ts
export const environment = {
  production: false,
  demoMode: false,
  apiUrl: 'http://localhost:5207/api',
  supabaseUrl: 'https://TU-PROYECTO.supabase.co',
  supabasePublishableKey: 'sb_publishable_...',
};
```

### Connection string PostgreSQL

1. En la parte superior del dashboard, presiona **Connect**.
2. Selecciona **Session pooler**. Es compatible con redes IPv4 y adecuada para la API persistente local.
3. Copia la cadena y sustituye `[YOUR-PASSWORD]` con la contraseña del proyecto.
4. Convierte su formato a Npgsql:

```text
Host=aws-0-REGION.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.PROJECT_REF;Password=TU_PASSWORD;SSL Mode=Require;Trust Server Certificate=true
```

No guardes esta cadena en `appsettings.json`. Desde la raíz del proyecto ejecuta:

```powershell
dotnet user-secrets set "ConnectionStrings:Supabase" "TU_CADENA_NPGSQL" --project .\src\BancoHorizonte.Api
dotnet user-secrets set "Supabase:Url" "https://TU-PROYECTO.supabase.co" --project .\src\BancoHorizonte.Api
```

## 7. Ejecutar y verificar

Terminal 1:

```powershell
dotnet run --project .\src\BancoHorizonte.Api
```

Para alinear una base creada con la versión anterior y cargar RF-10, ejecuta desde la raíz:

```powershell
dotnet run --project .\src\BancoHorizonte.Api -- --apply-requirements-database
```

El comando aplica `database/migrations/001_align_finresolve_requirements.sql` y `database/demo-data.sql`. Ambos son idempotentes.

Terminal 2:

```powershell
cd .\src\banco-horizonte-web
npm start
```

1. Abre `http://localhost:4200`.
2. Inicia sesión como supervisor.
3. Registra un reclamo.
4. Comprueba prioridad y SLA calculados.
5. Asígnalo al analista.
6. Inicia sesión como analista y actualiza el estado.
7. Vuelve como supervisor y verifica tablero e historial.

## Solución de problemas

- **401 en la API:** confirma `demoMode: false`, URL de Supabase y que el proyecto usa clave JWT asimétrica.
- **Perfil no configurado:** el usuario se creó antes de ejecutar el trigger. Elimínalo y créalo otra vez, o inserta manualmente su perfil.
- **Sin acceso al dashboard:** asigna `Supervisor` o `Administrador` en `usuario_roles` y vuelve a iniciar sesión.
- **Error de PostgreSQL:** revisa contraseña y usa Session pooler puerto `5432`.
