# Despliegue en Vercel, Render y Supabase

La solución conserva un solo repositorio y se publica como dos aplicaciones:

- Vercel compila y sirve `src/banco-horizonte-web`.
- Render construye el `Dockerfile` de la raíz y ejecuta la API .NET.
- Supabase mantiene Auth y PostgreSQL.

## 1. Preparar la conexión productiva de Supabase

En Supabase, abre **Connect** y copia la cadena **Session pooler**, puerto `5432`. Esta cadena se guarda únicamente en Render.

No copies la contraseña de PostgreSQL en Angular, Vercel, GitHub, `vercel.json`, `render.yaml` ni el Dockerfile.

## 2. Desplegar la API en Render

1. Crea un Blueprint o Web Service desde este repositorio.
2. Si utilizas Blueprint, Render detectará `render.yaml`.
3. Si lo configuras manualmente, selecciona `Docker` y `./Dockerfile`.
4. Configura estas variables:

| Variable | Valor |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Supabase__Url` | URL pública del proyecto Supabase |
| `ConnectionStrings__Supabase` | Cadena Session pooler completa |
| `Cors__AllowedOrigins` | URL del frontend; temporalmente puede quedar pendiente |

5. Usa `/api/health/ready` como Health Check Path.
6. Espera una URL similar a `https://banco-horizonte-api.onrender.com`.
7. Verifica `https://banco-horizonte-api.onrender.com/api/health/ready`.

La API acepta el puerto dinámico `PORT` de la plataforma. En Docker utiliza `10000` como valor predeterminado.

## 3. Desplegar Angular en Vercel

1. Importa el mismo repositorio en Vercel.
2. Configura **Root Directory** como `src/banco-horizonte-web`.
3. Vercel leerá `vercel.json`.
4. Configura estas variables públicas:

| Variable | Valor |
|---|---|
| `BH_API_URL` | `https://banco-horizonte-api.onrender.com/api` |
| `BH_SUPABASE_URL` | URL pública de Supabase |
| `BH_SUPABASE_PUBLISHABLE_KEY` | Publishable key de Supabase |

`npm run build` genera `public/config.js` durante el despliegue. El archivo es ignorado por Git y permite separar los valores locales de los productivos.

## 4. Cerrar la configuración cruzada

Después de obtener la URL definitiva de Vercel:

1. En Render, configura `Cors__AllowedOrigins` con la URL exacta. Para permitir varias, sepáralas con punto y coma:

   ```text
   https://banco-horizonte-reclamos.vercel.app;http://localhost:4200
   ```

2. En Supabase abre **Authentication → URL Configuration**.
3. Cambia **Site URL** por la URL productiva de Vercel.
4. Agrega como Redirect URLs:

   ```text
   https://banco-horizonte-reclamos.vercel.app/**
   http://localhost:4200/**
   ```

5. Conserva la confirmación de correo activada.
6. Vuelve a desplegar Render si cambiaste sus variables.

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
