-- Diagnostico rapido de tablas
-- Ver que tablas existen realmente

-- 1. Ver todos los schemas
SELECT schema_name 
FROM information_schema.schemata 
ORDER BY schema_name;

-- 2. Ver tablas en pss_dvnx
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'pss_dvnx' 
ORDER BY table_name;

-- 3. Buscar tabla users en cualquier schema
SELECT table_schema, table_name 
FROM information_schema.tables 
WHERE table_name LIKE '%user%' 
ORDER BY table_schema, table_name;

-- 4. Ver columnas de la tabla users (correcta)
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_schema = 'pss_dvnx' 
AND table_name = 'users'
ORDER BY ordinal_position;

-- 5. Ver columnas de user_sessions
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_schema = 'pss_dvnx' 
AND table_name = 'user_sessions'
ORDER BY ordinal_position;

-- 6. Ver columnas de partes_de_trabajo
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_schema = 'pss_dvnx' 
AND table_name = 'partes_de_trabajo'
ORDER BY ordinal_position;
