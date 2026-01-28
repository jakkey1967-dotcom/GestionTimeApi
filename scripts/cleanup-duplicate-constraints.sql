-- =====================================================
-- LIMPIAR CONSTRAINTS DUPLICADAS EN parte_tags
-- =====================================================

-- Ver estado actual
SELECT 
    'ANTES DE LIMPIAR' as estado,
    constraint_name,
    constraint_type
FROM information_schema.table_constraints
WHERE table_schema = 'pss_dvnx'
  AND table_name = 'parte_tags'
ORDER BY constraint_type, constraint_name;

-- =====================================================
-- ELIMINAR CONSTRAINTS DUPLICADAS (del script SQL manual)
-- =====================================================

-- 1. Eliminar FK duplicada: fk_parte_tags_parte
ALTER TABLE pss_dvnx.parte_tags 
    DROP CONSTRAINT IF EXISTS fk_parte_tags_parte;

-- 2. Eliminar FK duplicada: fk_parte_tags_tag
ALTER TABLE pss_dvnx.parte_tags 
    DROP CONSTRAINT IF EXISTS fk_parte_tags_tag;

-- =====================================================
-- MANTENER SOLO LAS DE EF CORE:
-- - FK_parte_tags_partesdetrabajo_parte_id
-- - FK_parte_tags_freshdesk_tags_tag_name
-- - PK_parte_tags
-- =====================================================

COMMIT;

-- Ver estado final (limpio)
SELECT 
    '✅ DESPUÉS DE LIMPIAR' as estado,
    constraint_name,
    constraint_type
FROM information_schema.table_constraints
WHERE table_schema = 'pss_dvnx'
  AND table_name = 'parte_tags'
ORDER BY constraint_type, constraint_name;

-- Debería mostrar solo:
-- FK_parte_tags_freshdesk_tags_tag_name   | FOREIGN KEY
-- FK_parte_tags_partesdetrabajo_parte_id  | FOREIGN KEY
-- PK_parte_tags                           | PRIMARY KEY

-- =====================================================
-- VERIFICAR QUE LA TABLA FUNCIONA
-- =====================================================

-- Insertar tag de prueba
INSERT INTO pss_dvnx.freshdesk_tags (name, source) 
VALUES ('test-cleanup', 'local')
ON CONFLICT (name) DO NOTHING;

-- Insertar relación de prueba (usa un parte_id válido)
INSERT INTO pss_dvnx.parte_tags (parte_id, tag_name)
SELECT p.id, 'test-cleanup'
FROM pss_dvnx.partesdetrabajo p
WHERE p.id_usuario = (SELECT id FROM pss_dvnx.users LIMIT 1)
LIMIT 1
ON CONFLICT DO NOTHING;

-- Ver si funcionó
SELECT 
    '✅ TABLA FUNCIONANDO' as resultado,
    COUNT(*) as registros_prueba
FROM pss_dvnx.parte_tags
WHERE tag_name = 'test-cleanup';

-- Limpiar prueba
DELETE FROM pss_dvnx.parte_tags WHERE tag_name = 'test-cleanup';
DELETE FROM pss_dvnx.freshdesk_tags WHERE name = 'test-cleanup';

SELECT '🎉 LIMPIEZA COMPLETADA' as resultado;
