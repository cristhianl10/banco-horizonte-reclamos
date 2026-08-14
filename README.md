# Banco Horizonte — Gestión de reclamos

Proyecto académico full stack para centralizar reclamos bancarios, calcular automáticamente su prioridad y SLA, asignar responsables y ofrecer trazabilidad e indicadores operativos.

## Stack

- ASP.NET Core Web API (.NET 10)
- Angular
- PostgreSQL y autenticación de Supabase
- Entity Framework Core + Npgsql
- xUnit y Jasmine/Karma

## Alcance implementado

- Registro validado de clientes y reclamos con detección de posibles duplicados.
- Cinco reglas acumulativas de prioridad, sin selección subjetiva del operador.
- SLA de 24, 12, 6 o 2 horas y alerta cuando se consume el 75 %.
- Bandeja con búsqueda y filtros operativos.
- Asignación, reasignación y toma de casos.
- Flujo `Nuevo → En análisis → Resuelto/Rechazado` con observación e historial.
- Tablero de totales, riesgo SLA, distribuciones y carga por analista.
- Autenticación Supabase, autorización por roles y mensajes de error orientados al usuario.
- Diez reclamos sintéticos para demostración.

El análisis completo está en [docs/01-analisis-funcional.md](docs/01-analisis-funcional.md).

## Ejecutar localmente

Desde la raíz, inicia la API:

```powershell
dotnet run --project .\src\BancoHorizonte.Api
```

En otra terminal, inicia Angular:

```powershell
cd .\src\banco-horizonte-web
npm install
npm start
```

Abre `http://localhost:4200`. La API utiliza `http://localhost:5207` en el perfil local.

## Base de datos

- Instalación nueva: ejecuta [database/schema.sql](database/schema.sql) en Supabase SQL Editor.
- Proyecto existente: ejecuta [database/migrations/001_align_finresolve_requirements.sql](database/migrations/001_align_finresolve_requirements.sql) y luego [database/demo-data.sql](database/demo-data.sql).
- Alternativa desde la raíz, con los secretos ya configurados:

```powershell
dotnet run --project .\src\BancoHorizonte.Api -- --apply-requirements-database
```

Los scripts son idempotentes y los datos demo son ficticios.

## Verificación

```powershell
dotnet test BancoHorizonte.slnx -c Release
cd .\src\banco-horizonte-web
npm test -- --watch=false
npm run build
```

Consulta [docs/05-configuracion-supabase.md](docs/05-configuracion-supabase.md) para la configuración y [docs/06-pruebas.md](docs/06-pruebas.md) para la estrategia de pruebas.
