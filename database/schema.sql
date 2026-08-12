-- Banco Horizonte: esquema inicial PostgreSQL / Supabase.
-- La API ASP.NET Core gestiona las operaciones de negocio.

create extension if not exists pgcrypto;

create table roles (
    id smallint generated always as identity primary key,
    nombre varchar(50) not null unique,
    descripcion varchar(200) not null,
    activo boolean not null default true
);

create table usuarios (
    id uuid primary key references auth.users(id) on delete restrict,
    nombres varchar(100) not null,
    apellidos varchar(100) not null,
    correo varchar(254) not null unique,
    activo boolean not null default true,
    creado_en timestamptz not null default now(),
    actualizado_en timestamptz not null default now()
);

create table usuario_roles (
    usuario_id uuid not null references usuarios(id) on delete cascade,
    rol_id smallint not null references roles(id) on delete restrict,
    primary key (usuario_id, rol_id)
);

create table clientes (
    id uuid primary key default gen_random_uuid(),
    tipo_documento varchar(20) not null,
    numero_documento varchar(30) not null,
    nombres varchar(100) not null,
    apellidos varchar(100) not null,
    correo varchar(254),
    telefono varchar(30),
    creado_en timestamptz not null default now(),
    actualizado_en timestamptz not null default now(),
    unique (tipo_documento, numero_documento)
);

create table canales_recepcion (
    id smallint generated always as identity primary key,
    nombre varchar(50) not null unique,
    activo boolean not null default true
);

create table categorias_reclamo (
    id smallint generated always as identity primary key,
    nombre varchar(100) not null unique,
    descripcion varchar(300),
    activo boolean not null default true
);

create table subcategorias_reclamo (
    id smallint generated always as identity primary key,
    categoria_id smallint not null references categorias_reclamo(id) on delete restrict,
    nombre varchar(100) not null,
    activo boolean not null default true,
    unique (categoria_id, nombre),
    unique (id, categoria_id)
);

create table estados_reclamo (
    id smallint generated always as identity primary key,
    nombre varchar(50) not null unique,
    es_final boolean not null default false,
    orden smallint not null unique,
    activo boolean not null default true
);

create table transiciones_estado (
    estado_origen_id smallint not null references estados_reclamo(id) on delete cascade,
    estado_destino_id smallint not null references estados_reclamo(id) on delete restrict,
    primary key (estado_origen_id, estado_destino_id),
    check (estado_origen_id <> estado_destino_id)
);

create table niveles_prioridad (
    id smallint generated always as identity primary key,
    nombre varchar(30) not null unique,
    puntaje_minimo smallint not null unique check (puntaje_minimo >= 0),
    orden smallint not null unique,
    color_hex char(7) not null check (color_hex ~ '^#[0-9A-Fa-f]{6}$'),
    activo boolean not null default true
);

create table politicas_sla (
    id uuid primary key default gen_random_uuid(),
    nombre varchar(100) not null unique,
    categoria_id smallint references categorias_reclamo(id) on delete restrict,
    prioridad_id smallint not null references niveles_prioridad(id) on delete restrict,
    horas_resolucion integer not null check (horas_resolucion > 0),
    umbral_alerta_minutos integer not null check (umbral_alerta_minutos > 0),
    vigente_desde timestamptz not null default now(),
    vigente_hasta timestamptz,
    activo boolean not null default true,
    check (vigente_hasta is null or vigente_hasta > vigente_desde)
);

create table reglas_prioridad (
    id uuid primary key default gen_random_uuid(),
    nombre varchar(100) not null unique,
    categoria_id smallint references categorias_reclamo(id) on delete restrict,
    impacto smallint check (impacto between 1 and 3),
    urgencia smallint check (urgencia between 1 and 3),
    puntos smallint not null check (puntos > 0),
    activo boolean not null default true,
    check (categoria_id is not null or impacto is not null or urgencia is not null)
);

create table reclamos (
    id uuid primary key default gen_random_uuid(),
    codigo varchar(30) not null unique,
    cliente_id uuid not null references clientes(id) on delete restrict,
    canal_recepcion_id smallint not null references canales_recepcion(id) on delete restrict,
    categoria_id smallint not null references categorias_reclamo(id) on delete restrict,
    subcategoria_id smallint,
    descripcion text not null check (char_length(trim(descripcion)) >= 20),
    impacto smallint not null check (impacto between 1 and 3),
    urgencia smallint not null check (urgencia between 1 and 3),
    estado_id smallint not null references estados_reclamo(id) on delete restrict,
    prioridad_id smallint not null references niveles_prioridad(id) on delete restrict,
    puntaje_prioridad smallint not null check (puntaje_prioridad >= 0),
    politica_sla_id uuid not null references politicas_sla(id) on delete restrict,
    fecha_recepcion timestamptz not null default now(),
    fecha_limite_sla timestamptz not null,
    fecha_resolucion timestamptz,
    responsable_actual_id uuid references usuarios(id) on delete restrict,
    creado_por_usuario_id uuid not null references usuarios(id) on delete restrict,
    creado_en timestamptz not null default now(),
    actualizado_en timestamptz not null default now(),
    foreign key (subcategoria_id, categoria_id) references subcategorias_reclamo(id, categoria_id) on delete restrict,
    check (fecha_limite_sla > fecha_recepcion),
    check (fecha_resolucion is null or fecha_resolucion >= fecha_recepcion)
);

create table asignaciones_reclamo (
    id uuid primary key default gen_random_uuid(),
    reclamo_id uuid not null references reclamos(id) on delete restrict,
    analista_id uuid not null references usuarios(id) on delete restrict,
    asignado_por_usuario_id uuid not null references usuarios(id) on delete restrict,
    asignado_en timestamptz not null default now(),
    finalizado_en timestamptz,
    motivo varchar(500),
    check (finalizado_en is null or finalizado_en >= asignado_en)
);

create unique index ux_asignacion_activa_por_reclamo
    on asignaciones_reclamo(reclamo_id) where finalizado_en is null;

create table observaciones_reclamo (
    id uuid primary key default gen_random_uuid(),
    reclamo_id uuid not null references reclamos(id) on delete restrict,
    autor_usuario_id uuid not null references usuarios(id) on delete restrict,
    contenido text not null check (char_length(trim(contenido)) > 0),
    es_interna boolean not null default true,
    creado_en timestamptz not null default now()
);

create table historial_reclamo (
    id bigint generated always as identity primary key,
    reclamo_id uuid not null references reclamos(id) on delete restrict,
    actor_usuario_id uuid not null references usuarios(id) on delete restrict,
    tipo_evento varchar(50) not null,
    datos_antes jsonb,
    datos_despues jsonb,
    ocurrido_en timestamptz not null default now()
);

create table adjuntos_reclamo (
    id uuid primary key default gen_random_uuid(),
    reclamo_id uuid not null references reclamos(id) on delete restrict,
    cargado_por_usuario_id uuid not null references usuarios(id) on delete restrict,
    nombre_original varchar(255) not null,
    ruta_storage varchar(500) not null unique,
    tipo_mime varchar(100) not null,
    tamanio_bytes bigint not null check (tamanio_bytes > 0),
    creado_en timestamptz not null default now()
);

create index ix_reclamos_bandeja on reclamos(estado_id, prioridad_id, fecha_limite_sla);
create index ix_reclamos_responsable on reclamos(responsable_actual_id, estado_id);
create index ix_reclamos_cliente on reclamos(cliente_id, fecha_recepcion desc);
create index ix_historial_reclamo on historial_reclamo(reclamo_id, ocurrido_en desc);

-- Catálogos iniciales idempotentes.
insert into roles(nombre, descripcion) values
('Operador', 'Registra reclamos recibidos'), ('Analista', 'Atiende reclamos asignados'),
('Supervisor', 'Supervisa, asigna y consulta indicadores'), ('Administrador', 'Configura la plataforma')
on conflict (nombre) do nothing;

insert into canales_recepcion(nombre) values ('Correo electrónico'), ('Llamada'), ('Formulario web'), ('Sucursal'), ('Aplicación móvil')
on conflict (nombre) do nothing;

insert into categorias_reclamo(nombre, descripcion) values
('Transferencias', 'Transferencias nacionales e interbancarias'), ('Tarjetas', 'Tarjetas de crédito y débito'),
('Cobros', 'Débitos y cobros no reconocidos'), ('Canales digitales', 'Web y aplicación móvil'),
('Atención al cliente', 'Experiencia y atención recibida')
on conflict (nombre) do nothing;

insert into subcategorias_reclamo(categoria_id, nombre)
select c.id, x.nombre from categorias_reclamo c join (values
('Transferencias', 'Transferencia no recibida'), ('Transferencias', 'Transferencia duplicada'),
('Tarjetas', 'Tarjeta bloqueada'), ('Tarjetas', 'Consumo no reconocido'),
('Cobros', 'Cobro duplicado'), ('Cobros', 'Débito no autorizado'),
('Canales digitales', 'No puede iniciar sesión'), ('Canales digitales', 'Operación no disponible'),
('Atención al cliente', 'Demora en atención'), ('Atención al cliente', 'Información incorrecta')) x(categoria, nombre)
on c.nombre = x.categoria on conflict (categoria_id, nombre) do nothing;

insert into estados_reclamo(nombre, es_final, orden) values
('Nuevo', false, 1), ('Asignado', false, 2), ('En análisis', false, 3),
('En espera de cliente', false, 4), ('Resuelto', false, 5), ('Cerrado', true, 6), ('Cancelado', true, 7)
on conflict (nombre) do nothing;

insert into transiciones_estado(estado_origen_id, estado_destino_id)
select o.id, d.id from estados_reclamo o join (values
('Nuevo','Asignado'), ('Nuevo','Cancelado'), ('Asignado','En análisis'), ('Asignado','Cancelado'),
('En análisis','En espera de cliente'), ('En análisis','Resuelto'), ('En espera de cliente','En análisis'),
('En espera de cliente','Cancelado'), ('Resuelto','Cerrado'), ('Resuelto','En análisis')) x(origen,destino)
on o.nombre=x.origen join estados_reclamo d on d.nombre=x.destino
on conflict do nothing;

insert into niveles_prioridad(nombre, puntaje_minimo, orden, color_hex) values
('Baja', 0, 1, '#3A7D65'), ('Media', 3, 2, '#2E6F95'), ('Alta', 5, 3, '#D97706'), ('Crítica', 7, 4, '#C53B3B')
on conflict (nombre) do nothing;

insert into politicas_sla(nombre, prioridad_id, horas_resolucion, umbral_alerta_minutos)
select 'SLA ' || p.nombre, p.id,
case p.nombre when 'Crítica' then 4 when 'Alta' then 8 when 'Media' then 24 else 72 end,
case p.nombre when 'Crítica' then 60 when 'Alta' then 120 when 'Media' then 240 else 720 end
from niveles_prioridad p on conflict (nombre) do nothing;

create or replace function public.handle_new_auth_user()
returns trigger language plpgsql security definer set search_path = ''
as $$
declare default_role_id smallint;
begin
  insert into public.usuarios(id, nombres, apellidos, correo)
  values (new.id,
    coalesce(nullif(trim(new.raw_user_meta_data ->> 'first_names'), ''), 'Usuario'),
    coalesce(nullif(trim(new.raw_user_meta_data ->> 'last_names'), ''), 'Banco Horizonte'), new.email)
  on conflict (id) do nothing;
  select id into default_role_id from public.roles where nombre = 'Operador';
  insert into public.usuario_roles(usuario_id, rol_id) values (new.id, default_role_id)
  on conflict do nothing;
  return new;
end;
$$;

drop trigger if exists on_auth_user_created on auth.users;
create trigger on_auth_user_created after insert on auth.users
for each row execute procedure public.handle_new_auth_user();

-- El navegador solo usa Auth; las tablas quedan cerradas al API automático de Supabase.
alter table roles enable row level security;
alter table usuarios enable row level security;
alter table usuario_roles enable row level security;
alter table clientes enable row level security;
alter table canales_recepcion enable row level security;
alter table categorias_reclamo enable row level security;
alter table subcategorias_reclamo enable row level security;
alter table estados_reclamo enable row level security;
alter table transiciones_estado enable row level security;
alter table niveles_prioridad enable row level security;
alter table politicas_sla enable row level security;
alter table reglas_prioridad enable row level security;
alter table reclamos enable row level security;
alter table asignaciones_reclamo enable row level security;
alter table observaciones_reclamo enable row level security;
alter table historial_reclamo enable row level security;
alter table adjuntos_reclamo enable row level security;

-- La API debe validar que subcategoria_id pertenezca a categoria_id,
-- elegir una política SLA vigente y registrar auditoría en toda mutación.
