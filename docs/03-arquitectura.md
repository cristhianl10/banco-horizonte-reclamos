# Arquitectura y diseño técnico

## Componentes

| Capa | Tecnología | Responsabilidad |
|---|---|---|
| Cliente | Angular | UI, rutas, formularios, guards, consumo de API y experiencia de usuario. |
| API | ASP.NET Core Web API | Casos de uso, validación, autorización, cálculo de SLA/prioridad, auditoría y endpoints REST. |
| Persistencia | Entity Framework Core + Npgsql | Mapeo y acceso seguro a PostgreSQL. |
| Infraestructura | Supabase | PostgreSQL, Auth y Storage. |

## Principio de seguridad

Angular nunca se conecta directamente a las tablas PostgreSQL. La API .NET valida el JWT entregado por Supabase Auth y decide qué operación puede realizar cada rol. Las claves de servicio y la cadena de conexión se guardarán como secretos de entorno, nunca en el repositorio.

## Módulos de API

- `Auth`: perfil del usuario autenticado y sincronización inicial.
- `Clientes`: búsqueda y mantenimiento de clientes.
- `Reclamos`: registro, detalle, consulta paginada, actualización y cierre.
- `Asignaciones`: asignar, reasignar y asumir casos.
- `Observaciones`: registrar y listar novedades.
- `Dashboard`: alertas de SLA, métricas y gráficos.
- `Configuracion`: catálogos, políticas SLA y reglas de prioridad.

## Endpoints iniciales

| Método | Ruta | Acción |
|---|---|---|
| POST | `/api/reclamos` | Registrar y calcular prioridad/SLA. |
| GET | `/api/reclamos` | Listar con filtros y paginación. |
| GET | `/api/reclamos/{id}` | Ver detalle e historial. |
| PATCH | `/api/reclamos/{id}/estado` | Cambiar estado validando transición. |
| POST | `/api/reclamos/{id}/asignaciones` | Asignar o reasignar responsable. |
| POST | `/api/reclamos/{id}/asumir` | Analista asume caso disponible. |
| POST | `/api/reclamos/{id}/observaciones` | Agregar observación. |
| GET | `/api/dashboard/resumen` | Indicadores generales. |
| GET | `/api/dashboard/alertas-sla` | Casos próximos a vencer o vencidos. |

## Pantallas Angular

1. Inicio de sesión.
2. Tablero del supervisor.
3. Bandeja de reclamos con filtros.
4. Registro de reclamo.
5. Detalle de reclamo con línea de tiempo.
6. Mis casos del analista.
7. Administración de catálogos y reglas.

