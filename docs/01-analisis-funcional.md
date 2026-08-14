# Análisis funcional — Banco Horizonte

## Problema

Banco Horizonte recibe reclamos por correo, llamadas, formularios y hojas de cálculo independientes. El problema no es solo el volumen: la información está fragmentada y no existe un criterio común para decidir qué atender primero.

Esto provoca reclamos duplicados o sin responsable, prioridades subjetivas, incumplimientos de SLA, poca trazabilidad y ausencia de indicadores para supervisión.

## Objetivo

Centralizar el ciclo completo del reclamo en una aplicación web que permita registrar, priorizar automáticamente, asignar, atender, auditar y supervisar casos.

El tablero debe responder en segundos:

- cuáles reclamos consumieron al menos el 75 % de su SLA o ya vencieron;
- quién atiende cada caso y en qué estado está;
- qué reclamos requieren atención inmediata por prioridad crítica o riesgo SLA.

## Usuarios y responsabilidades

| Perfil | Responsabilidad | Acciones del MVP |
|---|---|---|
| Operador | Registrar correctamente el reclamo recibido. | Crear el caso y revisar código, puntaje, prioridad y SLA calculados. |
| Analista | Atender casos y mantener trazabilidad. | Ver detalle, asumir casos sin responsable, cambiar estado y registrar observaciones. |
| Supervisor | Controlar riesgo operativo y avance. | Consultar todos los casos, filtrar, revisar alertas e indicadores y asignar o reasignar responsables. |

La autenticación con Supabase y el rol técnico `Administrador` son extensiones del prototipo. El registro público asigna `Operador`; los demás roles se otorgan mediante el script administrativo existente.

## Flujo transaccional

1. El operador identifica al cliente y registra canal, categoría, subcategoría, descripción, monto opcional, fecha de recepción e indisponibilidad digital.
2. El sistema busca posibles duplicados recientes del mismo cliente y categoría.
3. Al confirmar, genera un código único y calcula puntaje, prioridad, SLA, alerta al 75 % y fecha límite.
4. El supervisor asigna o reasigna un analista; la asignación no cambia el estado.
5. Un analista también puede asumir un caso sin responsable.
6. El analista cambia `Nuevo` a `En análisis` y registra una observación obligatoria.
7. Desde `En análisis`, el caso termina como `Resuelto` o `Rechazado`; no se reabre.
8. Cada creación, asignación, observación, recálculo y cambio de estado queda en el historial.
9. El tablero recalcula el riesgo y muestra casos próximos, vencidos y críticos.

## Priorización automática (RF-02)

Las reglas son acumulativas y parten de cero:

| Condición verificable | Puntos |
|---|---:|
| Transacción o compra no reconocida | +4 |
| Transferencia no acreditada o acceso/canal bloqueado | +3 |
| Monto afectado igual o superior a USD 500 | +3 |
| Canal digital completamente indisponible | +2 |
| Reclamo abierto por más de 24 horas | +2 |

| Puntaje | Prioridad | SLA |
|---:|---|---:|
| 0–2 | Baja | 24 horas |
| 3–4 | Media | 12 horas |
| 5–6 | Alta | 6 horas |
| 7 o más | Crítica | 2 horas |

La API conserva el desglose de reglas aplicado. El operador nunca elige impacto, urgencia ni prioridad.

## SLA

- `fecha_limite_sla = fecha_recepcion + horas_sla`.
- `fecha_alerta_sla = fecha_recepcion + 75 % del SLA`.
- `En tiempo`: todavía no alcanza la alerta.
- `Próximo`: alcanzó el 75 %, sigue abierto y aún no vence.
- `Vencido`: sigue abierto y la fecha límite ya pasó.
- Los casos abiertos por más de 24 horas se repriorizan al consultar bandeja, detalle o tablero.

## Estados permitidos

| Origen | Destinos válidos |
|---|---|
| Nuevo | En análisis, Rechazado |
| En análisis | Resuelto, Rechazado |
| Resuelto | — |
| Rechazado | — |

Todo cambio de estado exige observación, fecha y actor.

## Requisitos funcionales trazados

| ID | Cumplimiento |
|---|---|
| RF-01 | Registro con cliente ficticio, canal, categoría, descripción, monto y fecha; código único y validaciones. |
| RF-02 | Puntaje, prioridad y SLA automáticos mediante las reglas exactas del reto. |
| RF-03 | Bandeja con búsqueda y filtros por estado, prioridad, categoría, canal, responsable y SLA. |
| RF-04 | Expediente con todos los datos, responsable, tiempo SLA, reglas aplicadas y trazabilidad. |
| RF-05 | Asignación y reasignación de analista con historial. |
| RF-06 | Estados mínimos y transiciones controladas con observación obligatoria. |
| RF-07 | Tablero con totales, abiertos, resueltos, próximos, vencidos y distribuciones. |
| RF-08 | Alertas visuales calculadas con fechas reales y umbral del 75 %. |
| RF-09 | Errores de validación y negocio expresados en lenguaje entendible. |
| RF-10 | Script idempotente con diez reclamos y clientes sintéticos. |

## Adaptaciones tecnológicas acordadas

El documento sugería React y MySQL. Este portafolio usa Angular y PostgreSQL/Supabase por decisión del proyecto, conservando el comportamiento funcional. ASP.NET Core es el único acceso de negocio a las tablas; el navegador utiliza Supabase únicamente para autenticación.
