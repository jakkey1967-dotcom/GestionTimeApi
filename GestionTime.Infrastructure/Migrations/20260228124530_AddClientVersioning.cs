using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GestionTime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Columna 'nota' puede existir de migración manual anterior
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'pss_dvnx' AND table_name = 'cliente' AND column_name = 'nota'
                    ) THEN
                        ALTER TABLE pss_dvnx.cliente ADD COLUMN nota text;
                    END IF;
                END $$;");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS pss_dvnx.app_settings (
                    key character varying(100) NOT NULL,
                    value character varying(500) NOT NULL,
                    updated_at timestamp with time zone NOT NULL DEFAULT now(),
                    CONSTRAINT ""PK_app_settings"" PRIMARY KEY (key)
                );");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS pss_dvnx.client_versions (
                    id bigserial NOT NULL,
                    user_id uuid NOT NULL,
                    platform character varying(20) NOT NULL DEFAULT 'Desktop',
                    app_version_raw character varying(50) NOT NULL,
                    ver_major integer NOT NULL,
                    ver_minor integer NOT NULL,
                    ver_patch integer NOT NULL,
                    ver_prerelease character varying(50),
                    os_version character varying(100),
                    machine_name character varying(100),
                    logged_at timestamp with time zone NOT NULL DEFAULT now(),
                    CONSTRAINT ""PK_client_versions"" PRIMARY KEY (id),
                    CONSTRAINT ""FK_client_versions_users_user_id"" FOREIGN KEY (user_id)
                        REFERENCES pss_dvnx.users(id) ON DELETE CASCADE
                );");

            // Tabla 'cliente_notas' puede existir de migración manual anterior
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS pss_dvnx.cliente_notas (
                    id uuid NOT NULL DEFAULT gen_random_uuid(),
                    cliente_id integer NOT NULL,
                    owner_user_id uuid,
                    nota text NOT NULL,
                    created_at timestamp with time zone NOT NULL DEFAULT now(),
                    updated_at timestamp with time zone NOT NULL DEFAULT now(),
                    created_by uuid,
                    updated_by uuid,
                    CONSTRAINT ""PK_cliente_notas"" PRIMARY KEY (id),
                    CONSTRAINT ""FK_cliente_notas_cliente_cliente_id"" FOREIGN KEY (cliente_id)
                        REFERENCES pss_dvnx.cliente(id) ON DELETE CASCADE
                );");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS idx_client_versions_user_logged
                    ON pss_dvnx.client_versions (user_id, logged_at);
                CREATE INDEX IF NOT EXISTS idx_client_versions_ver
                    ON pss_dvnx.client_versions (ver_major, ver_minor, ver_patch);");

            migrationBuilder.Sql(@"
                INSERT INTO pss_dvnx.app_settings (key, value, updated_at) VALUES
                    ('min_client_version_desktop', '2.0.0', now()),
                    ('latest_client_version_desktop', '1.9.5-beta', now()),
                    ('update_url_desktop', 'https://github.com/jakkey1967-dotcom/Repositorio_GestionTimeDesktop/releases/latest', now())
                ON CONFLICT (key) DO NOTHING;");

            // Índices de cliente_notas pueden existir de migración manual anterior
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS idx_cliente_notas_cliente_id
                    ON pss_dvnx.cliente_notas (cliente_id) WHERE owner_user_id IS NULL;
                CREATE UNIQUE INDEX IF NOT EXISTS uq_cliente_notas_personal
                    ON pss_dvnx.cliente_notas (cliente_id, owner_user_id) WHERE owner_user_id IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "pss_dvnx",
                table: "app_settings",
                keyColumn: "key",
                keyValues: new object[] { "min_client_version_desktop", "latest_client_version_desktop", "update_url_desktop" });

            migrationBuilder.Sql(@"DROP TABLE IF EXISTS pss_dvnx.app_settings;");

            migrationBuilder.Sql(@"DROP TABLE IF EXISTS pss_dvnx.client_versions;");

            migrationBuilder.Sql(@"DROP TABLE IF EXISTS pss_dvnx.cliente_notas;");

            migrationBuilder.Sql(@"
                ALTER TABLE pss_dvnx.cliente DROP COLUMN IF EXISTS nota;");
        }
    }
}
