# Modelo de datos normalizado

## Criterio

El modelo está en Tercera Forma Normal: catálogos separados, relaciones por claves foráneas y datos históricos en tablas independientes. `reclamos` conserva el estado operativo actual para consultas rápidas; los cambios viven además en tablas de auditoría.

## Entidades

| Entidad | Responsabilidad |
|---|---|
| `usuarios`, `roles`, `usuario_roles` | Identidad interna y autorización. |
| `clientes` | Persona ficticia afectada por el reclamo. |
| `canales_recepcion` | Origen del reclamo. |
| `categorias_reclamo`, `subcategorias_reclamo` | Clasificación funcional. |
| `niveles_prioridad` | Baja, Media, Alta y Crítica. |
| `estados_reclamo`, `transiciones_estado` | Ciclo de vida permitido. |
| `politicas_sla` | Plazo vigente asociado a una prioridad. |
| `reclamos` | Caso operativo, hechos, resultado de prioridad y marcas SLA. |
| `asignaciones_reclamo` | Historial de responsables. |
| `historial_reclamo` | Auditoría de eventos. |
| `observaciones_reclamo` | Comentarios funcionales. |

## Datos calculados del reclamo

`reclamos` persiste `monto_afectado`, `indisponibilidad_digital`, `puntaje_prioridad`, `desglose_prioridad`, `fecha_alerta_sla` y `fecha_limite_sla`. Impacto y urgencia no forman parte del modelo actual: la prioridad se deriva exclusivamente de hechos verificables definidos en RF-02.

## Integridad

- Código único por reclamo.
- Documento único por tipo de documento.
- Subcategoría perteneciente a la categoría seleccionada.
- Un único responsable activo por reclamo.
- Monto nulo o no negativo.
- Alerta posterior a la recepción y anterior al vencimiento.
- Estados finales sin transiciones de salida.
- Políticas aplicadas no se eliminan; se desactivan.
- Historial generado por la API en cada mutación relevante.

El esquema para instalaciones nuevas está en [schema.sql](../database/schema.sql) y la actualización del esquema existente en [001_align_finresolve_requirements.sql](../database/migrations/001_align_finresolve_requirements.sql).
