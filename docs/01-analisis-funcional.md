# Análisis funcional

## 1. Contexto y problema

Banco Horizonte recibe diariamente reclamos sobre transferencias, tarjetas, cobros, canales digitales y atención al cliente. Los casos llegan por correo, llamadas, formularios y hojas de cálculo independientes. No existe una fuente única de verdad para conocer el estado, responsable, prioridad o tiempo límite de cada reclamo.

Esto produce:

- Reclamos duplicados o sin responsable.
- Priorización subjetiva.
- Incumplimiento de los tiempos máximos de atención (SLA).
- Falta de trazabilidad de los cambios.
- Falta de indicadores para supervisores y jefaturas.

## 2. Problema central

La ausencia de una plataforma centralizada que clasifique, asigne, controle y audite reclamos impide a Banco Horizonte priorizar objetivamente los casos, cumplir sus SLA y supervisar el desempeño operativo en tiempo real.

## 3. Objetivo general

Desarrollar un prototipo web para centralizar la gestión de reclamos de Banco Horizonte, automatizando la prioridad y el SLA, facilitando la asignación de responsables, preservando la trazabilidad e informando el riesgo operativo mediante un tablero.

## 4. Objetivos específicos

1. Registrar reclamos provenientes de distintos canales bajo un código único.
2. Detectar posibles duplicados antes de crear un caso.
3. Calcular prioridad y fecha límite de SLA con reglas configurables.
4. Asignar y reasignar responsables, preservando el historial de asignaciones.
5. Controlar el ciclo de vida de cada caso y registrar todas sus novedades.
6. Alertar sobre casos próximos a vencer o vencidos.
7. Mostrar indicadores operativos para supervisión.

## 5. Preguntas que debe resolver el producto

| Pregunta operativa | Respuesta del sistema |
|---|---|
| ¿Cuáles reclamos están próximos a incumplir su SLA? | Filtro y tarjeta de alerta que comparan `fecha_limite_sla` contra la hora actual. |
| ¿Quién atiende cada caso y en qué estado se encuentra? | Detalle y listado con responsable actual, estado actual e historial. |
| ¿Qué reclamos requieren atención inmediata? | Vista de prioridad crítica, casos vencidos y casos dentro del umbral de alerta. |

## 6. Alcance del MVP

Incluye:

- Autenticación y autorización por rol.
- Registro y consulta de clientes.
- Registro de reclamos y adjuntos.
- Detección de posibles duplicados.
- Priorización y SLA calculados por reglas.
- Asignación, reasignación y toma de casos.
- Estados, observaciones e historial auditable.
- Tablero para supervisores con filtros e indicadores.
- Catálogos administrativos: categorías, estados, prioridades, canales y políticas SLA.

No incluye inicialmente:

- Integración real con el core bancario, correo o call center.
- Pagos, reversos o resolución automática de transacciones.
- Notificaciones por SMS/WhatsApp o correo productivo.
- Firma digital o expediente regulatorio completo.

## 7. Usuarios y permisos

| Perfil | Necesidad | Acciones permitidas |
|---|---|---|
| Operador de atención | Registrar correctamente un reclamo recibido. | Crear reclamo, buscar cliente, ver código, prioridad y SLA calculados, adjuntar evidencia. |
| Analista | Resolver sus casos y mantener trazabilidad. | Ver casos asignados, asumir casos disponibles, actualizar estado, agregar observaciones y solicitar reasignación. |
| Supervisor | Controlar riesgo y carga operativa. | Consultar todos los casos, filtros, SLA, indicadores, asignar/reasignar y cerrar casos. |
| Administrador | Mantener la configuración de la plataforma. | Administrar usuarios, roles, catálogos, reglas de prioridad y políticas SLA. |

## 8. Flujo principal

1. El operador identifica al cliente y registra el reclamo.
2. El sistema busca coincidencias recientes y muestra posibles duplicados.
3. Al confirmar el registro, se genera el código único y se calcula prioridad, política SLA y fecha límite.
4. El supervisor asigna un analista, o el analista asume un caso disponible.
5. El analista cambia el estado y añade observaciones durante la atención.
6. Toda modificación relevante queda en el historial de auditoría.
7. El supervisor usa el tablero para identificar casos críticos, vencidos y próximos a vencer.
8. Al resolverlo, se registra fecha y solución; el supervisor puede cerrar el caso.

## 9. Estados del reclamo

| Estado | Significado | Transiciones permitidas |
|---|---|---|
| Nuevo | Registrado y pendiente de asignar. | Asignado, Cancelado |
| Asignado | Tiene responsable, aún no se analiza. | En análisis, Reasignado, Cancelado |
| En análisis | El analista está investigando. | En espera de cliente, Resuelto, Reasignado |
| En espera de cliente | Requiere información del cliente. | En análisis, Cancelado |
| Resuelto | Se entregó una solución. | Cerrado, En análisis |
| Cerrado | Caso finalizado y no editable salvo reapertura supervisada. | En análisis (supervisor) |
| Cancelado | Registro inválido o duplicado confirmado. | — |

## 10. Requisitos funcionales

- RF-01: El sistema debe autenticar usuarios y aplicar permisos por rol.
- RF-02: Debe permitir registrar clientes y buscar clientes existentes.
- RF-03: Debe registrar reclamos con canal, categoría, descripción, impacto, urgencia y evidencias.
- RF-04: Debe generar un código único legible por reclamo.
- RF-05: Debe detectar y advertir posibles duplicados por cliente, categoría, descripción y periodo.
- RF-06: Debe calcular prioridad, política SLA y fecha límite automáticamente.
- RF-07: Debe permitir asignar, reasignar y asumir un reclamo.
- RF-08: Debe permitir actualizar estados solo mediante transiciones válidas.
- RF-09: Debe registrar observaciones y adjuntos.
- RF-10: Debe mantener historial inmutable de asignaciones y cambios relevantes.
- RF-11: Debe listar y filtrar reclamos por estado, responsable, categoría, prioridad, fechas y situación SLA.
- RF-12: Debe mostrar métricas y alertas en el tablero del supervisor.
- RF-13: Debe permitir administrar catálogos y reglas a usuarios administradores.

## 11. Requisitos no funcionales

- RNF-01: Interfaz web responsive para escritorio y móvil.
- RNF-02: API REST documentada con OpenAPI/Swagger.
- RNF-03: Las operaciones protegidas deben requerir JWT válido.
- RNF-04: No se expondrán claves privadas ni conexión de base de datos al navegador.
- RNF-05: Los cambios de estado, prioridad y responsable deben ser auditables.
- RNF-06: Listados paginados y filtrables.
- RNF-07: Mensajes de error claros, consistentes y sin filtrar información sensible.
- RNF-08: Fechas almacenadas en UTC y presentadas según zona horaria configurada.

