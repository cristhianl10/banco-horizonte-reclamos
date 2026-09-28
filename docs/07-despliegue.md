# Deployment on Vercel, Render, and Supabase

Architecture:

- Angular frontend: [banco-horizonte-reclamos.vercel.app](https://banco-horizonte-reclamos.vercel.app)
- .NET API: [banco-horizonte-api.onrender.com](https://banco-horizonte-api.onrender.com)
- Supabase Authentication and PostgreSQL

## Render API

The existing service is `banco-horizonte-api`, connected to `cristhianl10/banco-horizonte-reclamos` on `main`.

- Runtime: Docker
- Context: repository root
- Dockerfile: `./Dockerfile`
- Plan: Free
- Health check: `/api/health/ready`

Required variables:

| Variable | Value |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Supabase__Url` | Public Supabase URL |
| `ConnectionStrings__Supabase` | Session-pooler Npgsql string, secret |
| `Cors__AllowedOrigins` | Exact Vercel origin(s), separated by semicolons |

The Docker image includes `libgssapi-krb5-2`, required by the Supabase PostgreSQL runtime. After deployment, verify `/api/health` and `/api/health/ready`.

## Vercel frontend

Set Root Directory to `src/banco-horizonte-web`, select Angular, use the existing `vercel.json`, and publish `dist/banco-horizonte-web`.

Configure:

| Variable | Value |
|---|---|
| `BH_API_URL` | `https://banco-horizonte-api.onrender.com/api` |
| `BH_DEMO_MODE` | `false` |
| `BH_SUPABASE_URL` | Public Supabase URL |
| `BH_SUPABASE_PUBLISHABLE_KEY` | Supabase publishable key |

The build generates `public/config.js`; redeploy after changing public variables. Refreshing `/reclamos` and `/dashboard` must not return 404.

## Supabase and production checklist

Use the Session pooler on port 5432, keep passwords and secret/service-role keys out of Git/Vercel, set the exact Vercel origin in CORS and Supabase URL Configuration, and keep email confirmation enabled publicly.

Verify: `/api/health` is healthy, `/api/health/ready` is ready, an operator can create a claim, priority/SLA are automatic, an analyst can take/update a case, a supervisor can assign/view the dashboard, there are no CORS errors, and no secret is in the repository or Angular bundle.