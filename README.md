# Banco Horizonte — Claims Management

Banco Horizonte is a full-stack academic application for centralizing bank claims, calculating priority and SLA automatically, assigning analysts, and providing operational traceability and dashboards.

## Stack

- ASP.NET Core Web API on .NET 10
- Angular
- PostgreSQL and Supabase Authentication
- Entity Framework Core and Npgsql
- xUnit and Jasmine/Karma

## Implemented scope

- Validated customer and claim registration with possible-duplicate detection.
- Five cumulative, deterministic priority rules.
- SLA tiers of 24, 12, 6, or 2 hours, with an alert at 75%.
- Searchable operational queue, assignment, reassignment, and analyst self-assignment.
- Workflow `New → In analysis → Resolved/Rejected`, with required observations and history.
- Dashboard for totals, SLA risk, distributions, and analyst workload.
- Supabase authentication, role-based authorization, and ten synthetic demo claims.

See [docs/01-analisis-funcional.md](docs/01-analisis-funcional.md).

## Run locally

```powershell
dotnet run --project .\src\BancoHorizonte.Api
cd .\src\banco-horizonte-web
npm install
npm start
```

Open `http://localhost:4200`; the local API uses `http://localhost:5207`.

For a new database run [database/schema.sql](database/schema.sql). For an existing database run the requirements migration and [database/demo-data.sql](database/demo-data.sql). Both are idempotent and demo data is fictional.

## Verification

```powershell
dotnet test BancoHorizonte.slnx -c Release
cd .\src\banco-horizonte-web
npm test -- --watch=false
npm run build
```

Recorded verification: ASP.NET Release build passed, Angular production build passed, 8 Angular tests passed, and Supabase migration/demo/health checks passed. The xUnit assembly compiled, but Windows Application Control (`0x800711C7`) blocked the runner on the original workstation.

## Deployment

- Frontend: [banco-horizonte-reclamos.vercel.app](https://banco-horizonte-reclamos.vercel.app)
- API: [banco-horizonte-api.onrender.com](https://banco-horizonte-api.onrender.com)
- Auth and PostgreSQL: Supabase

See [docs/07-despliegue.md](docs/07-despliegue.md). The Docker runtime includes the native GSSAPI dependency required by the Supabase PostgreSQL connection.