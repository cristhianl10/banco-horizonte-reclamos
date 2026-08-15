-- RF-10: diez reclamos sintéticos e idempotentes. Requiere al menos un usuario registrado.
with actor as (
  select id from public.usuarios where activo order by creado_en limit 1
), demo(documento, nombres, apellidos) as (
  values
  ('0900000001','Sofía','Vega'),('0900000002','Daniel','Ortiz'),('0900000003','Elena','Castro'),
  ('0900000004','Miguel','León'),('0900000005','Paola','Ruiz'),('0900000006','Jorge','Mora'),
  ('0900000007','Lucía','Paz'),('0900000008','Andrés','Solís'),('0900000009','Carla','Mena'),('0900000010','Diego','Reyes')
)
insert into public.clientes(tipo_documento, numero_documento, nombres, apellidos, correo, telefono)
select 'CEDULA', d.documento, d.nombres, d.apellidos, lower(d.nombres || '.' || d.apellidos || '@example.com'),
       '099' || right('0000000' || row_number() over ()::text, 7)
from demo d cross join actor
on conflict (tipo_documento, numero_documento) do nothing;

with actor as (select id from public.usuarios where activo order by creado_en limit 1),
cases(code, documento, category, subcategory, description, amount, unavailable, score, priority, status, age_hours) as (
  values
  ('BH-DEMO-0001','0900000001','Tarjetas','Compra no reconocida','Compra no reconocida por USD 780 reportada desde la aplicación móvil.',780::numeric,false,7,'Crítica','Nuevo',1),
  ('BH-DEMO-0002','0900000002','Transferencias','Transferencia no acreditada','Transferencia debitada que todavía no se acredita al beneficiario.',100,false,3,'Media','En análisis',5),
  ('BH-DEMO-0003','0900000003','Canales digitales','Acceso o canal bloqueado','Acceso bloqueado y canal digital completamente fuera de servicio.',null,true,5,'Alta','En análisis',4),
  ('BH-DEMO-0004','0900000004','Atención al cliente','Demora en atención','Cliente solicita revisión por una afectación económica superior al umbral.',800,false,3,'Media','Nuevo',2),
  ('BH-DEMO-0005','0900000005','Cobros','Transacción no reconocida','La cliente desconoce una transacción registrada en su cuenta.',null,false,4,'Media','En análisis',8),
  ('BH-DEMO-0006','0900000006','Canales digitales','Operación no disponible','La banca móvil se encuentra completamente indisponible.',null,true,2,'Baja','Nuevo',3),
  ('BH-DEMO-0007','0900000007','Atención al cliente','Información incorrecta','Reclamo abierto hace más de veinticuatro horas y aún sin solución.',null,false,2,'Baja','En análisis',30),
  ('BH-DEMO-0008','0900000008','Transferencias','Transferencia no acreditada','Transferencia de USD 600 no acreditada después de más de un día.',600,false,8,'Crítica','Nuevo',26),
  ('BH-DEMO-0009','0900000009','Atención al cliente','Demora en atención','Demora de atención resuelta mediante seguimiento telefónico.',null,false,0,'Baja','Resuelto',30),
  ('BH-DEMO-0010','0900000010','Tarjetas','Tarjeta bloqueada','Solicitud rechazada luego de validar un monto reportado de USD 550.',550,false,3,'Media','Rechazado',10)
), resolved as (
  select c.*, cl.id customer_id, cat.id category_id, sub.id subcategory_id, ch.id channel_id,
         pr.id priority_id, st.id status_id, pol.id policy_id,
         case c.priority when 'Crítica' then 2 when 'Alta' then 6 when 'Media' then 12 else 24 end sla_hours
  from cases c
  join public.clientes cl on cl.tipo_documento='CEDULA' and cl.numero_documento=c.documento
  join public.categorias_reclamo cat on cat.nombre=c.category
  left join public.subcategorias_reclamo sub on sub.categoria_id=cat.id and sub.nombre=c.subcategory
  join public.canales_recepcion ch on ch.nombre='Formulario web'
  join public.niveles_prioridad pr on pr.nombre=c.priority
  join public.estados_reclamo st on st.nombre=c.status
  join public.politicas_sla pol on pol.prioridad_id=pr.id and pol.categoria_id is null and pol.activo
)
insert into public.reclamos(
  codigo, cliente_id, canal_recepcion_id, categoria_id, subcategoria_id, descripcion, monto_afectado,
  indisponibilidad_digital, estado_id, prioridad_id, puntaje_prioridad, desglose_prioridad, politica_sla_id,
  fecha_recepcion, fecha_alerta_sla, fecha_limite_sla, fecha_resolucion, creado_por_usuario_id)
select r.code, r.customer_id, r.channel_id, r.category_id, r.subcategory_id, r.description, r.amount, r.unavailable,
       r.status_id, r.priority_id, r.score,
       case r.code
         when 'BH-DEMO-0001' then jsonb_build_array(jsonb_build_object('rule','Transacción o compra no reconocida','points',4),jsonb_build_object('rule','Monto afectado igual o superior a USD 500','points',3))
         when 'BH-DEMO-0002' then jsonb_build_array(jsonb_build_object('rule','Transferencia no acreditada o acceso/canal bloqueado','points',3))
         when 'BH-DEMO-0003' then jsonb_build_array(jsonb_build_object('rule','Transferencia no acreditada o acceso/canal bloqueado','points',3),jsonb_build_object('rule','Canal digital completamente indisponible','points',2))
         when 'BH-DEMO-0004' then jsonb_build_array(jsonb_build_object('rule','Monto afectado igual o superior a USD 500','points',3))
         when 'BH-DEMO-0005' then jsonb_build_array(jsonb_build_object('rule','Transacción o compra no reconocida','points',4))
         when 'BH-DEMO-0006' then jsonb_build_array(jsonb_build_object('rule','Canal digital completamente indisponible','points',2))
         when 'BH-DEMO-0007' then jsonb_build_array(jsonb_build_object('rule','Reclamo abierto por más de 24 horas','points',2))
         when 'BH-DEMO-0008' then jsonb_build_array(jsonb_build_object('rule','Transferencia no acreditada o acceso/canal bloqueado','points',3),jsonb_build_object('rule','Monto afectado igual o superior a USD 500','points',3),jsonb_build_object('rule','Reclamo abierto por más de 24 horas','points',2))
         when 'BH-DEMO-0010' then jsonb_build_array(jsonb_build_object('rule','Monto afectado igual o superior a USD 500','points',3))
         else '[]'::jsonb
       end,
       r.policy_id,
       now() - make_interval(hours => r.age_hours),
       now() - make_interval(hours => r.age_hours) + make_interval(mins => r.sla_hours * 45),
       now() - make_interval(hours => r.age_hours) + make_interval(hours => r.sla_hours),
       case when r.status='Resuelto' then now() - interval '10 hours' else null end,
       a.id
from resolved r cross join actor a
on conflict (codigo) do update set desglose_prioridad=excluded.desglose_prioridad;

insert into public.historial_reclamo(reclamo_id, actor_usuario_id, tipo_evento, datos_despues)
select r.id, a.id, 'RECLAMO_DEMO_CREADO', jsonb_build_object('codigo', r.codigo)
from public.reclamos r cross join (select id from public.usuarios where activo order by creado_en limit 1) a
where r.codigo like 'BH-DEMO-%'
  and not exists (select 1 from public.historial_reclamo h where h.reclamo_id=r.id and h.tipo_evento='RECLAMO_DEMO_CREADO');
