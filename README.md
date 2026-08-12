# Banco Horizonte — Gestión de Reclamos

Aplicación full stack académica para centralizar reclamos bancarios, calcular prioridad y SLA de forma objetiva, asignar responsables y entregar trazabilidad e indicadores operativos.

## Solución

```text
Angular → ASP.NET Core Web API → PostgreSQL (Supabase)
                         └── Supabase Auth
```

La aplicación se construirá en iteraciones. La API .NET será el único acceso a los datos y concentrará reglas de negocio, autorización, cálculo de prioridad, SLA y auditoría.

El frontend incluye un modo demo completo para recorrer registro, bandeja, expediente y tablero antes de crear la infraestructura. Los adjuntos con Supabase Storage quedan reservados para la iteración posterior a conectar el proyecto real.

## Acceso por perfil

| Perfil | Alcance implementado |
|---|---|
| Operador | Registra reclamos y consulta los casos creados por su usuario. |
| Analista | Consulta casos asignados o disponibles, asume un caso y actualiza su gestión. |
| Supervisor | Consulta el tablero completo, asigna responsables y supervisa todos los reclamos. |
| Administrador | Acceso total y entrada al módulo de configuración. |

## Funcionalidades

- Autenticación Supabase y autorización por roles empresariales.
- Registro con detección de posibles duplicados.
- Prioridad y fecha límite SLA calculadas automáticamente.
- Bandeja filtrable y expediente con línea de tiempo.
- Asignación, reasignación, estados y observaciones.
- Dashboard con riesgos SLA, indicadores y carga por analista.
- Modo demostración sin infraestructura para explorar la interfaz.
- Pruebas automatizadas de reglas y formularios.

## Ejecutar en modo demostración

```powershell
cd .\src\banco-horizonte-web
npm install
npm start
```

Abre `http://localhost:4200`. El modo demo está activo por defecto y no necesita Supabase.

## Verificación

```powershell
dotnet test BancoHorizonte.slnx
cd .\src\banco-horizonte-web
npm test -- --watch=false --browsers=ChromeHeadless
npm run build
```

## Documentación

- [Análisis funcional](docs/01-analisis-funcional.md)
- [Modelo de datos normalizado](docs/02-modelo-datos.md)
- [Arquitectura y diseño técnico](docs/03-arquitectura.md)
- [Plan de implementación](docs/04-hoja-ruta.md)
- [Configuración exacta de Supabase](docs/05-configuracion-supabase.md)
- [Pruebas y cobertura funcional](docs/06-pruebas.md)
