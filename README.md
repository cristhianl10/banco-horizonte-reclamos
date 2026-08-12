# Banco Horizonte — Gestión de Reclamos

Prototipo web académico para centralizar reclamos bancarios, calcular prioridad y SLA de forma objetiva, asignar responsables y entregar trazabilidad e indicadores operativos.

## Solución

```text
Angular → ASP.NET Core Web API → PostgreSQL (Supabase)
                         ├── Supabase Auth
                         └── Supabase Storage
```

La aplicación se construirá en iteraciones. La API .NET será el único acceso a los datos y concentrará reglas de negocio, autorización, cálculo de prioridad, SLA y auditoría.

## Documentación

- [Análisis funcional](docs/01-analisis-funcional.md)
- [Modelo de datos normalizado](docs/02-modelo-datos.md)
- [Arquitectura y diseño técnico](docs/03-arquitectura.md)
- [Plan de implementación](docs/04-hoja-ruta.md)

