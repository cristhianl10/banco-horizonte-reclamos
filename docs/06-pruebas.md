# Estrategia de pruebas

## Comandos

```powershell
dotnet test BancoHorizonte.slnx -c Release
cd .\src\banco-horizonte-web
npm test -- --watch=false
npm run build
```

## Backend

### Prioridad y SLA

- Cada una de las cinco reglas por separado.
- Suma de varias condiciones, incluido el caso crítico de compra no reconocida por USD 780.
- Umbrales Baja, Media, Alta y Crítica con SLA de 24, 12, 6 y 2 horas.
- Regla de antigüedad solo para reclamos abiertos por más de 24 horas.
- Monto negativo y recepción futura rechazados.
- Alerta exactamente al 75 % del plazo.
- Estados `En tiempo`, `Próximo` y `Vencido`.

### Flujo de reclamos

- Normalización de cliente y generación de código.
- Posible duplicado, confirmación y ausencia de penalizaciones no definidas por el reto.
- Categoría/subcategoría coherentes.
- Recálculo por antigüedad sin duplicar eventos.
- Transiciones válidas, saltos inválidos y prohibición de reapertura.
- Observación obligatoria al cambiar estado.
- Asignación y reasignación sin modificar el estado.
- Rechazo de responsables sin rol de Analista o Supervisor.

### Contratos

- Cédula y teléfono de 10 dígitos exactos.
- RUC de 13 dígitos y pasaporte alfanumérico.
- Correo válido, descripción mínima, identificadores de catálogo y monto no negativo.

## Frontend

- Formulario inicialmente inválido.
- Validaciones de cliente, contacto, descripción y monto.
- Formulario válido sin campos subjetivos de impacto o urgencia.
- Registro y acceso con mensajes comprensibles y control para mostrar/ocultar contraseña.

## Resultado de la verificación actual

- Compilación ASP.NET Core Release: correcta.
- Compilación Angular de producción: correcta.
- Angular: 8 pruebas correctas.
- Supabase: migración ejecutada, diez casos demo cargados y health check `ready/connected` correcto.
- El runner xUnit fue bloqueado por la política Windows Application Control del equipo (`0x800711C7`) después de compilar correctamente el proyecto de pruebas. Los casos quedan compilados y listos para ejecutarse en un entorno sin esa restricción.
