-- Ejecutar en Supabase SQL Editor cambiando el correo y el rol.
-- Roles válidos: Operador, Analista, Supervisor, Administrador.
insert into public.usuario_roles(usuario_id, rol_id)
select u.id, r.id
from public.usuarios u
cross join public.roles r
where u.correo = 'CAMBIAR@CORREO.COM'
  and r.nombre = 'Supervisor'
on conflict (usuario_id, rol_id) do nothing;

-- Para dejar un único rol, elimina antes los roles actuales:
-- delete from public.usuario_roles
-- where usuario_id = (select id from public.usuarios where correo = 'CAMBIAR@CORREO.COM');
