-- ============================================================================
-- MIGRATION: Complete data migration from localhost to cloud
-- Source: localhost:5432/gestiontime schema gestiontime  
-- Target: cloud BD schema gestiontime
-- ============================================================================

BEGIN;

-- ============================================================================
-- STEP 1: Clear existing data in cloud
-- ============================================================================
TRUNCATE TABLE gestiontime.tipo RESTART IDENTITY CASCADE;
TRUNCATE TABLE gestiontime.grupo RESTART IDENTITY CASCADE; 
TRUNCATE TABLE gestiontime.cliente RESTART IDENTITY CASCADE;

-- ============================================================================
-- STEP 2: Insert TIPOS from local
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
-- STEP 3: Insert GRUPOS from local
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
-- STEP 4: Reset sequences to match max IDs
-- ============================================================================
SELECT setval('gestiontime.tipo_id_tipo_seq', (SELECT MAX(id_tipo) FROM gestiontime.tipo));
SELECT setval('gestiontime.grupo_id_grupo_seq', (SELECT MAX(id_grupo) FROM gestiontime.grupo));

COMMIT;

-- ============================================================================
-- VERIFICATION
-- ============================================================================
SELECT 'DATOS MIGRADOS EXITOSAMENTE' as status;
SELECT 'TIPOS' as tabla, COUNT(*) as total FROM gestiontime.tipo
UNION ALL
SELECT 'GRUPOS', COUNT(*) FROM gestiontime.grupo  
UNION ALL
SELECT 'CLIENTES', COUNT(*) FROM gestiontime.cliente
ORDER BY tabla;