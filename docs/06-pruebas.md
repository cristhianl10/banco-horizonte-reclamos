# Estrategia y resultados de pruebas

## Comandos

```powershell
dotnet test BancoHorizonte.slnx --collect:"XPlat Code Coverage"
cd .\src\banco-horizonte-web
npm test -- --watch=false --browsers=ChromeHeadless
npm run build
```

## Cobertura funcional automatizada

### Motor de prioridad y SLA

- Baja, media, alta y crítica en sus límites.
- Bonificación de categorías sensibles.
- Penalización por reincidencia.
- Impacto y urgencia fuera del rango 1–3.
- Categoría vacía.
- Política específica por categoría y política genérica de respaldo.
- Políticas vencidas, futuras e inactivas.
- Fecha límite calculada y horas inválidas.
- SLA vencido, próximo y en tiempo.
- Umbral de alerta inválido.

### Contratos de entrada

- Solicitud completa válida.
- Tipo y número de documento obligatorios.
- Longitudes mínimas de documento, nombres y apellidos.
- Formato de correo.
- Canal y categoría obligatorios.
- Descripción entre 20 y 4000 caracteres.
- Impacto y urgencia dentro del rango.
- Observación mínima.
- Responsable obligatorio y motivo máximo.

### Casos de uso

- Registro con normalización de documento y correo.
- Código único, prioridad e SLA calculados.
- Advertencia de duplicados recientes sin crear otro registro.
- Confirmación explícita de duplicado y penalización de reincidencia.
- Rechazo de subcategoría ajena a la categoría.
- Rechazo de canal inactivo.
- Cambio de estado válido con observación y auditoría.
- Rechazo de transición no configurada.
- Rechazo de modificación de caso finalizado.
- Asignación y reasignación con un solo responsable activo.
- Rechazo de operador como analista.
- Rechazo de asignación repetida.
- Rechazo de observación sobre reclamo inexistente.

### Angular

- Login rechaza correo incorrecto y contraseña corta.
- Login acepta credenciales con formato válido.
- Registro inicia inválido cuando faltan campos obligatorios.
- Registro rechaza descripción corta, correo inválido y severidad fuera de rango.
- Registro acepta un reclamo completo.

## Validación visual y de interacción

Se verificó con navegador real:

- Dashboard de escritorio.
- Dashboard responsive en 390 × 844.
- Login.
- Registro completo de reclamo.
- Resultado con código generado.
- Apertura de expediente.
- Asignación de responsable.
- Cambio de estado y observación.
- Actualización de la línea de tiempo.
- Ausencia de errores o advertencias en consola.

Resultado actual: **49 pruebas .NET y 5 pruebas Angular aprobadas**.
