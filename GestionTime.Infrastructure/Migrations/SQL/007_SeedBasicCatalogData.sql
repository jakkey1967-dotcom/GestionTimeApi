-- ============================================================================
-- MIGRATION: Insert basic seed data for Cliente, Grupo, and Tipo
-- Database: gestiontime schema
-- ============================================================================

-- ============================================================================
-- STEP 1: Insert basic TIPOS (Types of work)
-- ============================================================================

INSERT INTO gestiontime.tipo (nombre, descripcion) VALUES
('Instalación', 'Instalación de equipos y sistemas'),
('Mantenimiento', 'Mantenimiento preventivo y correctivo'),
('Reparación', 'Reparación de equipos y averías'),
('Configuración', 'Configuración de sistemas y software'),
('Consultoría', 'Servicios de consultoría técnica'),
('Soporte', 'Soporte técnico remoto'),
('Formación', 'Formación y capacitación'),
('Auditoría', 'Auditorías técnicas y de seguridad'),
('Migración', 'Migración de datos y sistemas'),
('Implementación', 'Implementación de nuevas soluciones')
ON CONFLICT DO NOTHING;

-- ============================================================================
-- STEP 2: Insert basic GRUPOS (Work groups/teams)
-- ============================================================================

INSERT INTO gestiontime.grupo (nombre, descripcion) VALUES
('Soporte Técnico', 'Grupo de soporte técnico general'),
('Infraestructura', 'Infraestructura de TI y redes'),
('Desarrollo', 'Desarrollo de software y aplicaciones'),
('Sistemas', 'Administración de sistemas'),
('Seguridad', 'Seguridad informática y ciberseguridad'),
('Help Desk', 'Mesa de ayuda y atención al cliente'),
('Proyectos', 'Gestión de proyectos especiales'),
('Telecomunicaciones', 'Telecomunicaciones y comunicaciones'),
('Base de Datos', 'Administración de bases de datos'),
('Cloud', 'Servicios en la nube')
ON CONFLICT DO NOTHING;

-- ============================================================================
-- STEP 3: Insert basic CLIENTES (Clients/Companies)
-- ============================================================================

INSERT INTO gestiontime.cliente (nombre, nombre_comercial, provincia, id_puntoop, local_num, data_update, data_html) VALUES
-- Clientes de ejemplo para testing y desarrollo
('Empresa Demo S.L.', 'Demo Corp', 'Madrid', 1001, 1, NOW(), '<p>Cliente de demostración</p>'),
('Tecnología Global S.A.', 'TechGlobal', 'Barcelona', 1002, 1, NOW(), '<p>Empresa tecnológica</p>'),
('Servicios Integrales S.L.', 'ServiIntegrales', 'Valencia', 1003, 1, NOW(), '<p>Servicios múltiples</p>'),
('Retail Systems S.A.', 'RetailSys', 'Sevilla', 1004, 1, NOW(), '<p>Sistemas para retail</p>'),
('Consultoría IT S.L.', 'ConsultIT', 'Bilbao', 1005, 1, NOW(), '<p>Consultoría informática</p>'),

-- Cadenas retail típicas (ejemplos genéricos)
('Supermercados Norte S.A.', 'Super Norte', 'Madrid', 2001, 1, NOW(), '<p>Cadena de supermercados zona norte</p>'),
('Supermercados Norte S.A.', 'Super Norte', 'Madrid', 2001, 2, NOW(), '<p>Sucursal 2 - Centro comercial</p>'),
('Supermercados Norte S.A.', 'Super Norte', 'Madrid', 2001, 3, NOW(), '<p>Sucursal 3 - Zona residencial</p>'),

('Tiendas Express S.L.', 'Express24', 'Barcelona', 2002, 1, NOW(), '<p>Tienda Express principal</p>'),
('Tiendas Express S.L.', 'Express24', 'Barcelona', 2002, 2, NOW(), '<p>Sucursal Express centro</p>'),

('Farmacias Unidas S.A.', 'FarmUnidas', 'Valencia', 2003, 1, NOW(), '<p>Farmacia principal</p>'),
('Farmacias Unidas S.A.', 'FarmUnidas', 'Valencia', 2003, 2, NOW(), '<p>Farmacia sucursal</p>'),

-- Clientes de diferentes sectores
('Banco Regional S.A.', 'BancoRegional', 'Sevilla', 3001, 1, NOW(), '<p>Entidad bancaria regional</p>'),
('Clínica San José S.L.', 'Clínica San José', 'Málaga', 3002, 1, NOW(), '<p>Centro médico privado</p>'),
('Hotel Central S.A.', 'Hotel Central', 'Granada', 3003, 1, NOW(), '<p>Hotel en centro histórico</p>'),
('Escuela Técnica S.L.', 'EscuelaTec', 'Córdoba', 3004, 1, NOW(), '<p>Centro de formación técnica</p>'),
('Transportes Andaluces S.A.', 'TransAndaluz', 'Cádiz', 3005, 1, NOW(), '<p>Empresa de transporte</p>')

ON CONFLICT DO NOTHING;

-- ============================================================================
-- STEP 4: Verification queries
-- ============================================================================

-- Count records inserted
SELECT 
    'tipo' as table_name,
    COUNT(*) as total_records
FROM gestiontime.tipo
UNION ALL
SELECT 
    'grupo' as table_name,
    COUNT(*) as total_records
FROM gestiontime.grupo
UNION ALL
SELECT 
    'cliente' as table_name,
    COUNT(*) as total_records
FROM gestiontime.cliente
ORDER BY table_name;

-- Show sample data
SELECT 'TIPOS INSERTADOS:' as info;
SELECT id_tipo, nombre, descripcion FROM gestiontime.tipo ORDER BY id_tipo;

SELECT 'GRUPOS INSERTADOS:' as info;
SELECT id_grupo, nombre, descripcion FROM gestiontime.grupo ORDER BY id_grupo;

SELECT 'CLIENTES INSERTADOS:' as info;
SELECT id, nombre, nombre_comercial, provincia, id_puntoop, local_num 
FROM gestiontime.cliente 
ORDER BY id 
LIMIT 10;

-- ============================================================================
-- NOTES:
-- ============================================================================
-- Este script inserta:
-- - 10 tipos de trabajo comunes
-- - 10 grupos/equipos de trabajo
-- - 20+ clientes de ejemplo (incluyendo múltiples sucursales)
-- 
-- Todos los datos son seguros para producción y representan casos típicos
-- de uso en sistemas de gestión de tiempo para empresas de servicios técnicos.
-- ============================================================================