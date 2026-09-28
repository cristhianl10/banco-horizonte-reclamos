# Despliegue en Vercel, Render y Supabase

La aplicación utiliza tres servicios:

- Frontend Angular: https://banco-horizonte-reclamos.vercel.app
- API .NET en Render: https://banco-horizonte-api.onrender.com
- Supabase mantiene Auth y PostgreSQL.

## 1. Conexión de Supabase

En Supabase, abre **Connect → Session pooler**, puerto `5432`. Utiliza el host y el usuario que muestra el panel; el usuario del pooler incluye el identificador del proyecto. La conexión directa puede requerir IPv6; el Session pooler permite conectarse desde redes IPv4.

Guarda la conexión solamente en la variable secreta `ConnectionStrings__Supabase` de Render, con formato Npgsql:

```text
Host=HOST-DEL-POOLER;Port=5432;Database=postgres;Username=USUARIO-DEL-POOLER;Password=CONTRASEÑA;SSL Mode=Require
```

No guardes la contraseña en Git, Vercel ni el frontend. Si la cambias en Supabase, actualiza el secreto en Render y vuelve a desplegar.

## 2. API en Render

El servicio existente es `banco-horizonte-api`. El repositorio incluye `render.yaml` para reproducir su configuración sin crear otra base de datos.

- Repositorio: `cristhianl10/banco-horizonte-reclamos`, rama `main`.
- Runtime: Docker; contexto: raíz del repositorio; Dockerfile: `./Dockerfile`.
- Plan configurado: Free.
- Health Check Path: `/api/health/ready`.

| Variable en Render | Valor |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Supabase__Url` | `https://oigcjnymanhasdhrktpq.supabase.co` |
| `ConnectionStrings__Supabase` | Conexión Npgsql del Session pooler, como secreto |
| `Cors__AllowedOrigins` | `https://banco-horizonte-reclamos.vercel.app;https://banco-horizonte-reclamos-cristhianl10s-projects.vercel.app;http://localhost:4200` |

Usa orígenes completos: la política CORS actual no interpreta `https://*.vercel.app` como permiso para todos los subdominios. Después de modificar variables, vuelve a desplegar y comprueba `https://banco-horizonte-api.onrender.com/api/health/ready`.

## 3. Desplegar Angular en Vercel

1. Importa el mismo repositorio en Vercel.
2. Configura **Root Directory** como `src/banco-horizonte-web`.
3. Selecciona **Framework Preset: Angular**. Vercel leerá `vercel.json`, ejecutará `npm run build` y publicará `dist/banco-horizonte-web`. Esta carpeta contiene directamente `index.html`, porque `angular.json` configura `outputPath.browser` como una cadena vacía. No uses la raíz del repositorio ni agregues `/browser` al directorio de salida.
4. Configura estas variables públicas:

| Variable | Valor |
|---|---|
| `BH_API_URL` | `https://banco-horizonte-api.onrender.com/api` |
| `BH_DEMO_MODE` | `false` |
| `BH_SUPABASE_URL` | URL pública de Supabase |
| `BH_SUPABASE_PUBLISHABLE_KEY` | Publishable key de Supabase |

`npm run build` genera `public/config.js` durante el despliegue. El archivo es ignorado por Git y permite separar los valores locales de los productivos.

Si el proyecto ya se importó con otra configuración, corrige **Settings → Build and Deployment → Root Directory** a `src/banco-horizonte-web` y vuelve a desplegar. Un despliegue marcado como listo puede devolver 404 si publicó la raíz del monorepo sin compilar Angular.

## 4. Cerrar la configuración cruzada

Después de obtener la URL definitiva de Vercel:

1. En Render, configura `Cors__AllowedOrigins` con la URL exacta. Para permitir varias, sepáralas con punto y coma:

   ```text
   https://banco-horizonte-reclamos.vercel.app;https://banco-horizonte-reclamos-cristhianl10s-projects.vercel.app;http://localhost:4200
   ```

2. En Supabase abre **Authentication → URL Configuration**.
3. Cambia **Site URL** por la URL productiva de Vercel.
4. Agrega como Redirect URLs:

   ```text
   https://banco-horizonte-reclamos.vercel.app/**
   https://banco-horizonte-reclamos-cristhianl10s-projects.vercel.app/**
   http://localhost:4200/**
   ```

5. Conserva la confirmación de correo activada.
6. Vuelve a desplegar el servicio de Render si cambiaste sus variables.

## 5. Política de registro para el portafolio

El trigger de base de datos asigna `Operador` a cada usuario nuevo. Antes de compartir públicamente la aplicación, elige una política:

- Demostración controlada: desactiva nuevos registros en Supabase y publica credenciales ficticias por rol.
- Registro académico abierto: conserva el registro, utiliza solamente datos ficticios y revisa periódicamente los usuarios.

Para un portafolio público se recomienda la demostración controlada.

## 6. Lista de comprobación

- `GET /api/health` responde `healthy`.
- `GET /api/health/ready` responde `ready`.
- El correo de confirmación dirige al dominio de Vercel.
- Un operador puede registrar un reclamo.
- La prioridad y el SLA se calculan automáticamente.
- Un analista puede asumir y actualizar un caso.
- Un supervisor puede asignar y consultar el tablero.
- Recargar `/reclamos` o `/dashboard` no devuelve 404.
- La consola del navegador no presenta errores de CORS.
- Ningún secreto aparece en el repositorio ni en el bundle de Angular.
