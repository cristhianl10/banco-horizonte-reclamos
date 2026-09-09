# Despliegue en Vercel, Aiven Runtime y Supabase

La solución conserva un solo repositorio y se publica como dos aplicaciones:

- Vercel compila y sirve `src/banco-horizonte-web`.
- Aiven Runtime construye el `Dockerfile` de la raíz y ejecuta la API .NET.
- Supabase mantiene Auth y PostgreSQL.

> Aiven Runtime se encuentra en disponibilidad limitada. La cuenta debe tener el acceso aprobado y se debe revisar el precio mostrado por la consola antes de confirmar el despliegue. El plan gratuito de PostgreSQL de Aiven no incluye automáticamente el alojamiento de aplicaciones.

## 1. Preparar la conexión productiva de Supabase

En Supabase, abre **Connect** y copia la cadena **Session pooler**, puerto `5432`. Esta cadena se guarda únicamente como secreto de Aiven Runtime.

No copies la contraseña de PostgreSQL en Angular, Vercel, GitHub, archivos de configuración ni el Dockerfile. Tampoco reutilices esta contraseña para cuentas personales o servicios distintos.

## 2. Desplegar la API en Aiven Runtime

1. Crea o abre el proyecto `banco-horizonte-reclamos` en Aiven.
2. Entra en **Runtime**. Si aparece **Request access**, solicita el acceso y espera su aprobación.
3. Selecciona **Deploy app** y conecta la cuenta personal de GitHub.
4. Elige el repositorio `cristhianl10/banco-horizonte-reclamos` y la rama `main`.
5. Selecciona el `Dockerfile` de la raíz como manifiesto. La imagen expone el puerto `10000` y acepta `PORT` cuando la plataforma lo proporciona.
6. Configura estas variables exclusivamente como secretos o variables del Runtime:

| Variable | Valor |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Supabase__Url` | URL pública del proyecto Supabase |
| `ConnectionStrings__Supabase` | Cadena Session pooler completa |
| `Cors__AllowedOrigins` | `https://banco-horizonte-reclamos-cristhianl10s-projects.vercel.app` |

7. No crees otro PostgreSQL en Aiven: la aplicación continúa usando el PostgreSQL de Supabase.
8. Antes de pulsar **Deploy**, confirma en la consola que el coste sea el esperado y que no dependa solo de créditos temporales.
9. Usa `/api/health/ready` como Health Check Path si la plataforma lo solicita.
10. Cuando Aiven entregue el dominio, verifica `https://DOMINIO-AIVEN/api/health/ready`.

El `Dockerfile` ya usa una compilación multi-stage de .NET, ejecuta la aplicación como usuario sin privilegios y fue validado por GitHub Actions.

## 3. Desplegar Angular en Vercel

1. Importa el mismo repositorio en Vercel.
2. Configura **Root Directory** como `src/banco-horizonte-web`.
3. Selecciona **Framework Preset: Angular**. Vercel leerá `vercel.json`, ejecutará `npm run build` y publicará `dist/banco-horizonte-web`. Esta carpeta contiene directamente `index.html`, porque `angular.json` configura `outputPath.browser` como una cadena vacía. No uses la raíz del repositorio ni agregues `/browser` al directorio de salida.
4. Configura estas variables públicas:

| Variable | Valor |
|---|---|
| `BH_API_URL` | `https://DOMINIO-AIVEN/api` |
| `BH_DEMO_MODE` | `false` |
| `BH_SUPABASE_URL` | URL pública de Supabase |
| `BH_SUPABASE_PUBLISHABLE_KEY` | Publishable key de Supabase |

`npm run build` genera `public/config.js` durante el despliegue. El archivo es ignorado por Git y permite separar los valores locales de los productivos.

Si el proyecto ya se importó con otra configuración, corrige **Settings → Build and Deployment → Root Directory** a `src/banco-horizonte-web` y vuelve a desplegar. Un despliegue marcado como listo puede devolver 404 si publicó la raíz del monorepo sin compilar Angular.

## 4. Cerrar la configuración cruzada

Después de obtener la URL definitiva de Vercel:

1. En Aiven Runtime, configura `Cors__AllowedOrigins` con la URL exacta. Para permitir varias, sepáralas con punto y coma:

   ```text
   https://banco-horizonte-reclamos-cristhianl10s-projects.vercel.app;http://localhost:4200
   ```

2. En Supabase abre **Authentication → URL Configuration**.
3. Cambia **Site URL** por la URL productiva de Vercel.
4. Agrega como Redirect URLs:

   ```text
   https://banco-horizonte-reclamos-cristhianl10s-projects.vercel.app/**
   http://localhost:4200/**
   ```

5. Conserva la confirmación de correo activada.
6. Vuelve a desplegar Aiven Runtime si cambiaste sus variables.

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
