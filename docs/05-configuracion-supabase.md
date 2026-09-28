# Supabase Configuration

## Project and schema

Create a Supabase project, save a strong database password, and run [database/schema.sql](../database/schema.sql) in **SQL Editor**. For an existing project, run [database/migrations/001_align_finresolve_requirements.sql](../database/migrations/001_align_finresolve_requirements.sql), then [database/demo-data.sql](../database/demo-data.sql).

The schema creates normalized tables, indexes, foreign keys, roles, catalogs, statuses, transitions, priorities, SLA policies, the profile trigger, the initial `Operator` role, and restrictive RLS policies. Business data is accessed by the .NET API; the browser does not query public tables directly.

## Authentication

Enable Email under **Authentication → Providers**. Email confirmation may be disabled only for a local academic prototype; keep it enabled in public environments. Set the production Vercel URL under **URL Configuration** and add its callback paths. Use an active asymmetric JWT signing key (ES256 or RS256); the API validates tokens through the public JWKS endpoint.

## Users and roles

Create test users in **Authentication → Users**:

| Email | Role |
|---|---|
| `operator@bancohorizonte.com` | Operator |
| `analyst@bancohorizonte.com` | Analyst |
| `supervisor@bancohorizonte.com` | Supervisor |
| `admin@bancohorizonte.com` | Administrator |

The trigger creates `public.usuarios` and initially assigns `Operator`. Use [database/assign-role.sql](../database/assign-role.sql) for other roles. Never expose service-role or secret keys.

## Frontend and API configuration

Set the frontend environment:

```ts
export const environment = {
  production: false,
  demoMode: false,
  apiUrl: 'http://localhost:5207/api',
  supabaseUrl: 'https://YOUR-PROJECT.supabase.co',
  supabasePublishableKey: 'sb_publishable_...',
};
```

Use the Supabase **Session pooler** and store this Npgsql string as a user secret or Render secret:

```text
Host=POOLER_HOST;Port=5432;Database=postgres;Username=POOLER_USER;Password=PASSWORD;SSL Mode=Require;Trust Server Certificate=true
```

```powershell
dotnet user-secrets set "ConnectionStrings:Supabase" "YOUR_NPGSQL_CONNECTION" --project .\src\BancoHorizonte.Api
dotnet user-secrets set "Supabase:Url" "https://YOUR-PROJECT.supabase.co" --project .\src\BancoHorizonte.Api
```

Verify by creating a claim as an operator, assigning it as a supervisor, updating it as an analyst, and checking dashboard/history.