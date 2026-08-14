begin;

alter table public.reclamos add column if not exists monto_afectado numeric(12,2);
alter table public.reclamos add column if not exists indisponibilidad_digital boolean not null default false;
alter table public.reclamos add column if not exists desglose_prioridad jsonb not null default '[]'::jsonb;
alter table public.reclamos add column if not exists fecha_alerta_sla timestamptz;
alter table public.reclamos alter column impacto drop not null;
alter table public.reclamos alter column urgencia drop not null;

do $$
begin
  if not exists (select 1 from pg_constraint where conname = 'ck_reclamos_monto_afectado') then
    alter table public.reclamos add constraint ck_reclamos_monto_afectado
      check (monto_afectado is null or monto_afectado >= 0);
  end if;
end $$;

insert into public.subcategorias_reclamo(categoria_id, nombre)
select c.id, x.nombre from public.categorias_reclamo c join (values
  ('Transferencias', 'Transferencia no acreditada'),
  ('Tarjetas', 'Compra no reconocida'),
  ('Cobros', 'Transacción no reconocida'),
  ('Canales digitales', 'Acceso o canal bloqueado')
) x(categoria, nombre) on c.nombre = x.categoria
on conflict (categoria_id, nombre) do update set activo = true;

delete from public.transiciones_estado;
-- El paso por valores negativos evita colisiones con la restricción UNIQUE al repetir la migración.
update public.estados_reclamo set orden = -orden;
update public.estados_reclamo set orden = 100 + abs(orden), activo = false;
update public.estados_reclamo set es_final = false, orden = 1, activo = true where nombre = 'Nuevo';
update public.estados_reclamo set es_final = false, orden = 2, activo = true where nombre = 'En análisis';
update public.estados_reclamo set es_final = true, orden = 3, activo = true where nombre = 'Resuelto';
insert into public.estados_reclamo(nombre, es_final, orden, activo)
values ('Rechazado', true, 4, true)
on conflict (nombre) do update set es_final = excluded.es_final, orden = excluded.orden, activo = excluded.activo;

update public.reclamos r set estado_id = d.id
from public.estados_reclamo o, public.estados_reclamo d
where r.estado_id = o.id and o.nombre in ('Asignado', 'En espera de cliente') and d.nombre = 'En análisis';
update public.reclamos r set estado_id = d.id
from public.estados_reclamo o, public.estados_reclamo d
where r.estado_id = o.id and o.nombre = 'Cerrado' and d.nombre = 'Resuelto';
update public.reclamos r set estado_id = d.id
from public.estados_reclamo o, public.estados_reclamo d
where r.estado_id = o.id and o.nombre = 'Cancelado' and d.nombre = 'Rechazado';

insert into public.transiciones_estado(estado_origen_id, estado_destino_id)
select o.id, d.id from public.estados_reclamo o join (values
  ('Nuevo', 'En análisis'), ('Nuevo', 'Rechazado'),
  ('En análisis', 'Resuelto'), ('En análisis', 'Rechazado')
) x(origen, destino) on o.nombre = x.origen
join public.estados_reclamo d on d.nombre = x.destino;

update public.politicas_sla p set
  horas_resolucion = case n.nombre when 'Crítica' then 2 when 'Alta' then 6 when 'Media' then 12 else 24 end,
  umbral_alerta_minutos = case n.nombre when 'Crítica' then 90 when 'Alta' then 270 when 'Media' then 540 else 1080 end
from public.niveles_prioridad n where n.id = p.prioridad_id;

update public.reclamos r set
  fecha_limite_sla = r.fecha_recepcion + make_interval(hours => case n.nombre
    when 'Crítica' then 2 when 'Alta' then 6 when 'Media' then 12 else 24 end),
  fecha_alerta_sla = r.fecha_recepcion + make_interval(mins => case n.nombre
    when 'Crítica' then 90 when 'Alta' then 270 when 'Media' then 540 else 1080 end)
from public.niveles_prioridad n where n.id = r.prioridad_id;

alter table public.reclamos alter column fecha_alerta_sla set not null;

create index if not exists ix_reclamos_alerta_sla
  on public.reclamos(fecha_alerta_sla, fecha_limite_sla) where fecha_resolucion is null;

commit;
