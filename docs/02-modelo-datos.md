# Modelo de datos normalizado

## Decisión de normalización

El modelo se diseña en Tercera Forma Normal (3FN): catálogos separados, relaciones mediante claves foráneas y datos históricos en tablas independientes. `reclamos` contiene solamente el estado operativo actual para lecturas rápidas; cada cambio se conserva además como evento histórico.

## Entidades principales

| Entidad | Responsabilidad |
|---|---|
| usuarios, roles, usuario_roles | Personal interno y autorización de aplicación. |
| clientes | Persona titular o afectada por el reclamo. |
| canales_recepcion | Origen del reclamo. |
| categorias_reclamo, subcategorias_reclamo | Clasificación funcional. |
| niveles_prioridad | Valores ordenados de prioridad. |
| estados_reclamo | Estados permitidos del ciclo de vida. |
| politicas_sla | Tiempo máximo aplicable según prioridad/categoría. |
| reglas_prioridad | Puntuaciones configurables para el cálculo objetivo. |
| reclamos | Caso operativo principal. |
| asignaciones_reclamo | Historial de responsables. |
| historial_reclamo | Auditoría de cambios relevantes. |
| observaciones_reclamo | Comentarios funcionales. |
| adjuntos_reclamo | Metadatos de archivos alojados en Storage. |

## Relaciones

```text
Cliente 1 ── * Reclamo * ── 1 Categoría ── * Subcategoría
Usuario 1 ── * Reclamo (responsable actual)
Reclamo 1 ── * Asignación ── 1 Usuario
Reclamo 1 ── * Historial ── 1 Usuario
Reclamo 1 ── * Observación ── 1 Usuario
Reclamo 1 ── * Adjunto
Reclamo * ── 1 Estado / Prioridad / Canal / Política SLA
Usuario * ── * Rol
```

## Reglas de integridad

- Un código de reclamo es único.
- Una subcategoría pertenece a una sola categoría.
- Una política SLA no se elimina si fue aplicada a un reclamo; se desactiva.
- Un caso cerrado o cancelado no acepta modificaciones operativas sin reapertura autorizada.
- Solo un responsable actual puede estar activo por reclamo.
- Un historial no se actualiza ni elimina desde la aplicación.
- La prioridad y fecha límite son valores calculados al registrar/recalcular; se conservan para auditoría.

## Esquema inicial

El esquema ejecutable estará en [schema.sql](../database/schema.sql). Las columnas de auditoría usan UTC.

