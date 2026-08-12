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
    unique (categoria_id, nombre)
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
    subcategoria_id smallint references subcategorias_reclamo(id) on delete restrict,
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

-- La API debe validar que subcategoria_id pertenezca a categoria_id,
-- elegir una política SLA vigente y registrar auditoría en toda mutación.
