-- ============================================================================
-- MIGRATION: Move existing tables to gestiontime schema
-- ============================================================================

-- Mover tablas existentes al schema gestiontime
ALTER TABLE users SET SCHEMA gestiontime;
ALTER TABLE roles SET SCHEMA gestiontime;
ALTER TABLE user_roles SET SCHEMA gestiontime;
ALTER TABLE refresh_tokens SET SCHEMA gestiontime;
ALTER TABLE user_profiles SET SCHEMA gestiontime;
ALTER TABLE cliente SET SCHEMA gestiontime;
ALTER TABLE grupo SET SCHEMA gestiontime;
ALTER TABLE tipo SET SCHEMA gestiontime;
ALTER TABLE partesdetrabajo SET SCHEMA gestiontime;

-- Verificar que las tablas se movieron
SELECT schemaname, tablename FROM pg_tables 
WHERE schemaname = 'gestiontime' 
ORDER BY tablename;

-- Actualizar la tabla de migraciones también
ALTER TABLE "__EFMigrationsHistory" SET SCHEMA gestiontime;

-- ============================================================================
-- ROLLBACK (en caso de error)
-- ============================================================================
-- ALTER TABLE gestiontime.users SET SCHEMA public;
-- ALTER TABLE gestiontime.roles SET SCHEMA public;
-- ALTER TABLE gestiontime.user_roles SET SCHEMA public;
-- ALTER TABLE gestiontime.refresh_tokens SET SCHEMA public;
-- ALTER TABLE gestiontime.user_profiles SET SCHEMA public;
-- ALTER TABLE gestiontime.cliente SET SCHEMA public;
-- ALTER TABLE gestiontime.grupo SET SCHEMA public;
-- ALTER TABLE gestiontime.tipo SET SCHEMA public;
-- ALTER TABLE gestiontime.partesdetrabajo SET SCHEMA public;
-- ALTER TABLE gestiontime."__EFMigrationsHistory" SET SCHEMA public;
-- ============================================================================