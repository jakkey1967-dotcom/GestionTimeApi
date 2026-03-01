using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GestionTime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailOutboxAndDesktopClientHealth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // email_outbox (idempotente)
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS pss_dvnx.email_outbox (
                    id bigserial NOT NULL,
                    user_id uuid NOT NULL,
                    kind character varying(30) NOT NULL,
                    platform character varying(20) NOT NULL DEFAULT 'Desktop',
                    target_version_raw character varying(50),
                    period_key character varying(20) NOT NULL,
                    dedupe_key character varying(300) NOT NULL,
                    subject character varying(300),
                    body_preview character varying(500),
                    status character varying(10) NOT NULL DEFAULT 'PENDING',
                    sent_at timestamp with time zone,
                    error character varying(1000),
                    created_at timestamp with time zone NOT NULL DEFAULT now(),
                    CONSTRAINT ""PK_email_outbox"" PRIMARY KEY (id),
                    CONSTRAINT ""FK_email_outbox_users_user_id"" FOREIGN KEY (user_id)
                        REFERENCES pss_dvnx.users(id) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS uq_email_outbox_dedupe
                    ON pss_dvnx.email_outbox (dedupe_key);
                CREATE INDEX IF NOT EXISTS idx_email_outbox_user_created
                    ON pss_dvnx.email_outbox (user_id, created_at);
                CREATE INDEX IF NOT EXISTS idx_email_outbox_status
                    ON pss_dvnx.email_outbox (status);
            ");

            // Vista: última versión Desktop por usuario (DISTINCT ON)
            // DROP primero para poder cambiar nombres de columna
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS pss_dvnx.v_desktop_client_last_version;");
            migrationBuilder.Sql(@"
                CREATE VIEW pss_dvnx.v_desktop_client_last_version AS
                SELECT DISTINCT ON (cv.user_id)
                    cv.user_id,
                    u.full_name,
                    u.email,
                    cv.app_version_raw,
                    cv.platform,
                    cv.ver_major,
                    cv.ver_minor,
                    cv.ver_patch,
                    cv.ver_prerelease,
                    cv.os_version,
                    cv.machine_name,
                    cv.logged_at
                FROM pss_dvnx.client_versions cv
                JOIN pss_dvnx.users u ON u.id = cv.user_id
                WHERE cv.platform = 'Desktop'
                ORDER BY cv.user_id, cv.logged_at DESC;
            ");

            // Semillas adicionales de app_settings para campañas
            migrationBuilder.Sql(@"
                INSERT INTO pss_dvnx.app_settings (key, value, updated_at) VALUES
                    ('desktop_inactive_weeks', '2', now()),
                    ('desktop_send_dow', 'MON', now()),
                    ('desktop_send_hour', '09', now()),
                    ('desktop_email_cooldown_weeks', '1', now()),
                    ('desktop_release_url', 'https://gestiontimeapi.onrender.com/', now()),
                    ('desktop_release_highlights_md', 'Mejoras de estabilidad, corrección de bugs y nuevas funcionalidades.', now())
                ON CONFLICT (key) DO NOTHING;
                UPDATE pss_dvnx.app_settings SET value = '2.0.2-beta' WHERE key = 'latest_client_version_desktop' AND value = '1.9.5-beta';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS pss_dvnx.v_desktop_client_last_version;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS pss_dvnx.email_outbox;");
            migrationBuilder.Sql(@"
                DELETE FROM pss_dvnx.app_settings WHERE key IN (
                    'desktop_inactive_weeks','desktop_send_dow','desktop_send_hour',
                    'desktop_email_cooldown_weeks','desktop_release_url','desktop_release_highlights_md'
                );
            ");
        }
    }
}
