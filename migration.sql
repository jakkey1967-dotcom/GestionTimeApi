CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'gestiontime') THEN
        CREATE SCHEMA gestiontime;
    END IF;
END $EF$;

CREATE TABLE gestiontime.cliente (
    id serial NOT NULL,
    nombre text,
    id_puntoop integer,
    local_num integer,
    nombre_comercial text,
    provincia text,
    data_update timestamp with time zone NOT NULL DEFAULT (now()),
    data_html text,
    CONSTRAINT "PK_cliente" PRIMARY KEY (id)
);

CREATE TABLE gestiontime.grupo (
    id_grupo serial NOT NULL,
    nombre text NOT NULL,
    descripcion text,
    CONSTRAINT "PK_grupo" PRIMARY KEY (id_grupo)
);

CREATE TABLE gestiontime.partesdetrabajo (
    id bigserial NOT NULL,
    fecha_trabajo date NOT NULL,
    hora_inicio time NOT NULL,
    hora_fin time NOT NULL,
    accion text NOT NULL,
    ticket text,
    id_cliente integer NOT NULL,
    tienda text,
    id_grupo integer,
    id_tipo integer,
    id_usuario uuid NOT NULL,
    estado text NOT NULL DEFAULT ('activo'),
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    updated_at timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT "PK_partesdetrabajo" PRIMARY KEY (id),
    CONSTRAINT ck_partes_horas_validas CHECK (hora_fin >= hora_inicio)
);

CREATE TABLE gestiontime.roles (
    id serial NOT NULL,
    name character varying(50) NOT NULL,
    CONSTRAINT "PK_roles" PRIMARY KEY (id)
);

CREATE TABLE gestiontime.tipo (
    id_tipo serial NOT NULL,
    nombre text NOT NULL,
    descripcion text,
    CONSTRAINT "PK_tipo" PRIMARY KEY (id_tipo)
);

CREATE TABLE gestiontime.users (
    id uuid NOT NULL,
    email character varying(200) NOT NULL,
    password_hash text NOT NULL,
    full_name character varying(200) NOT NULL,
    enabled boolean NOT NULL DEFAULT TRUE,
    email_confirmed boolean NOT NULL DEFAULT FALSE,
    password_changed_at timestamp with time zone,
    must_change_password boolean NOT NULL DEFAULT FALSE,
    password_expiration_days integer NOT NULL DEFAULT 90,
    CONSTRAINT "PK_users" PRIMARY KEY (id)
);

CREATE TABLE gestiontime.refresh_tokens (
    id uuid NOT NULL,
    user_id uuid NOT NULL,
    token_hash character varying(128) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    revoked_at timestamp with time zone,
    CONSTRAINT "PK_refresh_tokens" PRIMARY KEY (id),
    CONSTRAINT "FK_refresh_tokens_users_user_id" FOREIGN KEY (user_id) REFERENCES gestiontime.users (id) ON DELETE CASCADE
);

CREATE TABLE gestiontime.user_profiles (
    id uuid NOT NULL,
    first_name character varying(100),
    last_name character varying(100),
    phone character varying(20),
    mobile character varying(20),
    address character varying(200),
    city character varying(100),
    postal_code character varying(10),
    department character varying(100),
    position character varying(100),
    employee_type character varying(50),
    hire_date date,
    avatar_url character varying(500),
    notes text,
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    updated_at timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT "PK_user_profiles" PRIMARY KEY (id),
    CONSTRAINT "FK_user_profiles_users_id" FOREIGN KEY (id) REFERENCES gestiontime.users (id) ON DELETE CASCADE
);

CREATE TABLE gestiontime.user_roles (
    user_id uuid NOT NULL,
    role_id integer NOT NULL,
    CONSTRAINT "PK_user_roles" PRIMARY KEY (user_id, role_id),
    CONSTRAINT "FK_user_roles_roles_role_id" FOREIGN KEY (role_id) REFERENCES gestiontime.roles (id) ON DELETE CASCADE,
    CONSTRAINT "FK_user_roles_users_user_id" FOREIGN KEY (user_id) REFERENCES gestiontime.users (id) ON DELETE CASCADE
);

CREATE INDEX idx_partes_created_at ON gestiontime.partesdetrabajo (created_at);

CREATE INDEX idx_partes_fecha_trabajo ON gestiontime.partesdetrabajo (fecha_trabajo);

CREATE INDEX idx_partes_user_fecha ON gestiontime.partesdetrabajo (id_usuario, fecha_trabajo);

CREATE UNIQUE INDEX "IX_refresh_tokens_token_hash" ON gestiontime.refresh_tokens (token_hash);

CREATE INDEX "IX_refresh_tokens_user_id" ON gestiontime.refresh_tokens (user_id);

CREATE UNIQUE INDEX "IX_roles_name" ON gestiontime.roles (name);

CREATE INDEX "IX_user_roles_role_id" ON gestiontime.user_roles (role_id);

CREATE UNIQUE INDEX "IX_users_email" ON gestiontime.users (email);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251231103227_InitialCreateWithGestionTimeSchema', '8.0.11');

COMMIT;

START TRANSACTION;

CREATE TABLE gestiontime."DataProtectionKeys" (
    "Id" serial NOT NULL,
    "FriendlyName" text,
    "Xml" text,
    CONSTRAINT "PK_DataProtectionKeys" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251231175652_AddDataProtectionKeys', '8.0.11');

COMMIT;

START TRANSACTION;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251231181838_FixDataProtectionKeysSchema', '8.0.11');

COMMIT;

