-- ============================================================================
-- MIGRATION: Replace cloud data with local data from localhost gestiontime
-- Source: localhost:5432/gestiontime schema gestiontime
-- Target: cloud BD schema gestiontime
-- ============================================================================

BEGIN;

-- ============================================================================
-- BACKUP existing data (optional - comment out if not needed)
-- ============================================================================
-- CREATE TABLE IF NOT EXISTS gestiontime.backup_tipo_$(date '+%Y%m%d') AS SELECT * FROM gestiontime.tipo;
-- CREATE TABLE IF NOT EXISTS gestiontime.backup_grupo_$(date '+%Y%m%d') AS SELECT * FROM gestiontime.grupo;
-- CREATE TABLE IF NOT EXISTS gestiontime.backup_cliente_$(date '+%Y%m%d') AS SELECT * FROM gestiontime.cliente;

-- ============================================================================
-- CLEAR existing data
-- ============================================================================
TRUNCATE TABLE gestiontime.tipo RESTART IDENTITY CASCADE;
TRUNCATE TABLE gestiontime.grupo RESTART IDENTITY CASCADE;
TRUNCATE TABLE gestiontime.cliente RESTART IDENTITY CASCADE;

-- ============================================================================
-- INSERT TIPOS from local DB
-- ============================================================================
INSERT INTO gestiontime.tipo (id_tipo, nombre, descripcion) VALUES
(1, 'Incidencia', ''),
(2, 'Instalación', ''),
(3, 'Aviso', ''),
(4, 'Petición', ''),
(5, 'Revisión', ''),
(6, 'Mantenimiento', ''),
(7, 'Formación', ''),
(8, 'Cesa', ''),
(9, 'Soporte Telefónico', ''),
(10, 'Auditoria', ''),
(11, 'Presupuesto', ''),
(12, 'Facturación', ''),
(13, 'Gestión', ''),
(14, 'Varios', ''),
(15, 'Nuevo Cliente', ''),
(16, 'Mejoras', ''),
(17, 'TPV', ''),
(18, 'Movilidad', ''),
(19, 'INTEGRA', ''),
(20, 'GENESIS', ''),
(21, 'WEB', ''),
(22, 'COMPRAS', ''),
(23, 'TIENDAS', ''),
(24, 'HANA', ''),
(25, 'RRHH', ''),
(26, 'CONTABILIDAD', ''),
(27, 'CRM', ''),
(28, 'Promociones', ''),
(29, 'B2B', ''),
(30, 'ECOMMERCE', ''),
(31, 'RETAIL', ''),
(32, 'CAR', ''),
(33, 'Venta', '');

-- ============================================================================
-- INSERT GRUPOS from local DB
-- ============================================================================
INSERT INTO gestiontime.grupo (id_grupo, nombre, descripcion) VALUES
(1, 'Administración', ''),
(2, 'Comercial', ''),
(3, 'Desarrollo', ''),
(4, 'Gestión Central', ''),
(5, 'Outsourcing', ''),
(6, 'Soporte Técnico', ''),
(7, 'Sistemas', ''),
(8, 'Técnico', ''),
(9, 'Direccion', ''),
(10, 'Marketing', ''),
(11, 'Telecomunicaciones', ''),
(12, 'Facturación', ''),
(13, 'Impresión', ''),
(14, 'App Movil', ''),
(15, 'Formación', ''),
(16, 'RRHH', ''),
(17, 'E-Commerce', ''),
(18, 'Presupuestos', ''),
(19, 'I+D+i', ''),
(20, 'Integraciones', ''),
(21, 'Proyectos', '');

-- ============================================================================
-- Reset sequences to match the max IDs
-- ============================================================================
SELECT setval('gestiontime.tipo_id_tipo_seq', (SELECT MAX(id_tipo) FROM gestiontime.tipo));
SELECT setval('gestiontime.grupo_id_grupo_seq', (SELECT MAX(id_grupo) FROM gestiontime.grupo));

-- ============================================================================
-- Verification
-- ============================================================================
SELECT 'TIPOS MIGRADOS:' as info, COUNT(*) as total FROM gestiontime.tipo;
SELECT 'GRUPOS MIGRADOS:' as info, COUNT(*) as total FROM gestiontime.grupo;
SELECT 'CLIENTES MIGRADOS:' as info, COUNT(*) as total FROM gestiontime.cliente;

-- Show sample data
SELECT 'TIPOS:' as table_name, id_tipo, nombre FROM gestiontime.tipo ORDER BY id_tipo LIMIT 5;
SELECT 'GRUPOS:' as table_name, id_grupo, nombre FROM gestiontime.grupo ORDER BY id_grupo LIMIT 5;

COMMIT;