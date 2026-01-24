# 👤 Script para Crear Usuario Administrador + Datos Iniciales
# Crea admin@admin.com con rol ADMIN y todos los permisos
# Incluye: Roles, Tipos de Trabajo y Grupos

param(
    [string]$Email = "admin@admin.com",
    [string]$Password = "Admin@2025",
    [string]$FullName = "Administrador del Sistema",
    [string]$Schema = "pss_dvnx",
    [switch]$Render,
    [switch]$Force,
    [switch]$SkipSeedData  # Omitir datos iniciales (solo crear usuario)
)

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║    👤 CREAR USUARIO ADMINISTRADOR + DATOS INICIALES 👤   ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Validaciones
if ([string]::IsNullOrWhiteSpace($Email)) {
    Write-Host "❌ Email no puede estar vacío" -ForegroundColor Red
    exit 1
}

if ([string]::IsNullOrWhiteSpace($Password)) {
    Write-Host "❌ Password no puede estar vacío" -ForegroundColor Red
    exit 1
}

if ($Password.Length -lt 8) {
    Write-Host "❌ La contraseña debe tener al menos 8 caracteres" -ForegroundColor Red
    exit 1
}

Write-Host "📋 CONFIGURACIÓN:" -ForegroundColor Cyan
Write-Host "   Email: $Email" -ForegroundColor Yellow
Write-Host "   Nombre: $FullName" -ForegroundColor Yellow
Write-Host "   Schema: $Schema" -ForegroundColor Yellow
Write-Host "   Entorno: $(if ($Render) { 'Render (Production)' } else { 'Local' })" -ForegroundColor Yellow
Write-Host "   Datos Iniciales: $(if ($SkipSeedData) { 'No' } else { 'Sí (Roles, Tipos, Grupos)' })" -ForegroundColor Yellow
Write-Host ""

# Confirmar antes de continuar (solo si no es Force)
if (-not $Force) {
    $confirm = Read-Host "¿Deseas continuar? (s/n)"
    if ($confirm -ne "s" -and $confirm -ne "S") {
        Write-Host "❌ Operación cancelada" -ForegroundColor Yellow
        exit 0
    }
    Write-Host ""
}

# ==================== CONFIGURAR CONNECTION STRING ====================

if ($Render) {
    Write-Host "🌐 Conectando a Render..." -ForegroundColor Cyan
    
    # Solicitar DATABASE_URL si no está en variable de entorno
    $databaseUrl = $env:DATABASE_URL
    if ([string]::IsNullOrWhiteSpace($databaseUrl)) {
        Write-Host "⚠️  DATABASE_URL no encontrado en variables de entorno" -ForegroundColor Yellow
        $databaseUrl = Read-Host "Ingresa DATABASE_URL de Render"
    }
    
    if ([string]::IsNullOrWhiteSpace($databaseUrl)) {
        Write-Host "❌ DATABASE_URL requerido para Render" -ForegroundColor Red
        exit 1
    }
    
    # Convertir DATABASE_URL de Render a format Npgsql
    try {
        $uri = [System.Uri]$databaseUrl
        $userInfo = $uri.UserInfo.Split(':')
        
        $connectionString = "Host=$($uri.Host);Port=$($uri.Port);Database=$($uri.AbsolutePath.TrimStart('/'));Username=$($userInfo[0]);Password=$($userInfo[1]);SslMode=Require;"
        
        Write-Host "✅ Connection string configurado" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Error convirtiendo DATABASE_URL: $_" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "🏠 Conectando a base de datos local..." -ForegroundColor Cyan
    $connectionString = "Host=localhost;Port=5432;Database=gestiontime;Username=postgres;Password=postgres;"
    Write-Host "✅ Connection string: Host=localhost;Port=5432;Database=gestiontime" -ForegroundColor Green
}

Write-Host ""

# ==================== SQL SCRIPT ====================

$seedDataSection = if (-not $SkipSeedData) {
@"
-- ==================== 2. CREAR DATOS INICIALES (SEED) ====================

-- 2.1 ROLES (si no existen)
INSERT INTO roles (name)
VALUES 
    ('ADMIN'),
    ('EDITOR'),
    ('USER')
ON CONFLICT (name) DO NOTHING;

-- 2.2 TIPOS DE TRABAJO (si no existen)
INSERT INTO tipo (id_tipo, nombre, descripcion)
VALUES
    (1,  'Incidencia',       NULL),
    (2,  'Instalación',      NULL),
    (3,  'Aviso',            NULL),
    (4,  'Petición',         NULL),
    (5,  'Facturable',       NULL),
    (6,  'Duda',             NULL),
    (7,  'Desarrollo',       NULL),
    (8,  'Tarea',            NULL),
    (9,  'Ofertado',         NULL),
    (10, 'Llamada Overlay',  '')
ON CONFLICT (id_tipo) DO NOTHING;

-- 2.3 GRUPOS DE TRABAJO (si no existen)
INSERT INTO grupo (id_grupo, nombre, descripcion)
VALUES
    (1, 'Administración',  NULL),
    (2, 'Comercial',       NULL),
    (3, 'Desarrollo',      NULL),
    (4, 'Gestión Central', NULL),
    (5, 'Logística',       NULL),
    (6, 'Movilidad',       NULL),
    (7, 'Post-Venta',      NULL),
    (8, 'Tiendas',         NULL)
ON CONFLICT (id_grupo) DO NOTHING;

-- Resetear secuencias para que el próximo ID sea correcto
SELECT setval(pg_get_serial_sequence('tipo', 'id_tipo'), (SELECT MAX(id_tipo) FROM tipo));
SELECT setval(pg_get_serial_sequence('grupo', 'id_grupo'), (SELECT MAX(id_grupo) FROM grupo));

"@
} else {
@"
-- ==================== 2. CREAR ROLES (MÍNIMOS) ====================
INSERT INTO roles (name)
VALUES ('ADMIN')
ON CONFLICT (name) DO NOTHING;

INSERT INTO roles (name)
VALUES ('EDITOR')
ON CONFLICT (name) DO NOTHING;

INSERT INTO roles (name)
VALUES ('USER')
ON CONFLICT (name) DO NOTHING;

"@
}

$sqlScript = @"
-- ========================================
-- SCRIPT COMPLETO DE INICIALIZACIÓN
-- ========================================
-- Email: $Email
-- Schema: $Schema
-- Generado: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
-- Incluye: Usuario Admin + $(if ($SkipSeedData) { 'Solo Roles' } else { 'Roles + Tipos + Grupos' })
-- ========================================

-- Establecer schema
SET search_path TO $Schema;

-- ==================== 1. VERIFICAR SI EL USUARIO YA EXISTE ====================
DO `$`$
DECLARE
    v_user_count INT;
BEGIN
    SELECT COUNT(*) INTO v_user_count
    FROM users
    WHERE email = '$Email';

    IF v_user_count > 0 THEN
        RAISE NOTICE '⚠️  El usuario % ya existe. Saltando creación de usuario...', '$Email';
        RAISE EXCEPTION 'Usuario ya existe';
    END IF;
END `$`$;

$seedDataSection

-- ==================== 3. CREAR USUARIO ADMINISTRADOR ====================
INSERT INTO users (
    id,
    email,
    password_hash,
    full_name,
    enabled,
    email_confirmed,
    must_change_password,
    password_changed_at,
    password_expiration_days
)
VALUES (
    gen_random_uuid(),
    '$Email',
    '$(BCrypt.Net.BCrypt.HashPassword($Password))',
    '$FullName',
    true,
    true,  -- Email ya confirmado
    false, -- No requiere cambio de contraseña
    NOW(), -- Contraseña recién cambiada
    999    -- No expira (casi)
);

-- ==================== 4. ASIGNAR ROL ADMIN ====================
INSERT INTO user_roles (user_id, role_id)
SELECT 
    u.id,
    r.id
FROM users u
CROSS JOIN roles r
WHERE u.email = '$Email'
  AND r.name = 'ADMIN';

-- ==================== 5. CREAR PERFIL DE USUARIO ====================
INSERT INTO user_profiles (
    id,
    first_name,
    last_name,
    department,
    position,
    employee_type,
    hire_date,
    created_at,
    updated_at
)
SELECT 
    id,
    'Admin',
    'Sistema',
    'Administración',
    'Administrador del Sistema',
    'Administrador',
    NOW(),
    NOW(),
    NOW()
FROM users
WHERE email = '$Email';

-- ==================== 6. VERIFICAR CREACIÓN ====================
DO `$`$
DECLARE
    v_roles_count INT;
    v_tipos_count INT;
    v_grupos_count INT;
BEGIN
    SELECT COUNT(*) INTO v_roles_count FROM roles;
    SELECT COUNT(*) INTO v_tipos_count FROM tipo;
    SELECT COUNT(*) INTO v_grupos_count FROM grupo;
    
    RAISE NOTICE '';
    RAISE NOTICE '╔══════════════════════════════════════════════════════════╗';
    RAISE NOTICE '║            ✅ CREACIÓN COMPLETADA ✅                     ║';
    RAISE NOTICE '╚══════════════════════════════════════════════════════════╝';
    RAISE NOTICE '';
    RAISE NOTICE '📊 ESTADÍSTICAS:';
    RAISE NOTICE '   Roles creados: %', v_roles_count;
    RAISE NOTICE '   Tipos de trabajo: %', v_tipos_count;
    RAISE NOTICE '   Grupos: %', v_grupos_count;
    RAISE NOTICE '';
END `$`$;

-- Consulta final de verificación
SELECT 
    u.id,
    u.email,
    u.full_name,
    u.enabled,
    u.email_confirmed,
    array_agg(DISTINCT r.name ORDER BY r.name) as roles
FROM users u
LEFT JOIN user_roles ur ON u.id = ur.user_id
LEFT JOIN roles r ON ur.role_id = r.id
WHERE u.email = '$Email'
GROUP BY u.id, u.email, u.full_name, u.enabled, u.email_confirmed;
"@

# ==================== EJECUTAR SQL ====================

Write-Host "🔧 Ejecutando script SQL..." -ForegroundColor Cyan
Write-Host ""

try {
    # Verificar que BCrypt.Net esté disponible
    Add-Type -Path "bin\Debug\net8.0\BCrypt.Net-Next.dll" -ErrorAction Stop
    
    # Generar hash de contraseña con BCrypt
    $passwordHash = [BCrypt.Net.BCrypt]::HashPassword($Password)
    
    # Reemplazar el placeholder con el hash real
    $sqlScript = $sqlScript -replace '\$\(BCrypt\.Net\.BCrypt\.HashPassword\(\$Password\)\)', $passwordHash
    
    # Guardar script temporal
    $tempSqlFile = [System.IO.Path]::GetTempFileName() + ".sql"
    $sqlScript | Out-File -FilePath $tempSqlFile -Encoding UTF8
    
    Write-Host "📄 Script SQL generado: $tempSqlFile" -ForegroundColor Gray
    Write-Host ""
    
    # Ejecutar con psql (necesitas tener psql instalado)
    Write-Host "🚀 Ejecutando en base de datos..." -ForegroundColor Cyan
    
    if ($Render) {
        # Para Render, usa la connection string completa
        $env:PGPASSWORD = $userInfo[1]
        $result = psql "$connectionString" -f $tempSqlFile 2>&1
    }
    else {
        # Para local, usa parámetros individuales
        $env:PGPASSWORD = "postgres"
        $result = psql -h localhost -p 5432 -U postgres -d gestiontime -f $tempSqlFile 2>&1
    }
    
    # Verificar resultado
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Green
        Write-Host "║            ✅ INICIALIZACIÓN EXITOSA ✅                  ║" -ForegroundColor Green
        Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Green
        Write-Host ""
        Write-Host "👤 USUARIO ADMINISTRADOR:" -ForegroundColor Cyan
        Write-Host "   📧 Email: $Email" -ForegroundColor Green
        Write-Host "   🔑 Password: $Password" -ForegroundColor Green
        Write-Host "   👤 Nombre: $FullName" -ForegroundColor Green
        Write-Host "   🎭 Rol: ADMIN" -ForegroundColor Green
        Write-Host "   ✅ Email confirmado: Sí" -ForegroundColor Green
        Write-Host "   🔐 Contraseña expira: No (999 días)" -ForegroundColor Green
        Write-Host ""
        
        if (-not $SkipSeedData) {
            Write-Host "📊 DATOS INICIALES CREADOS:" -ForegroundColor Cyan
            Write-Host "   ✅ Roles: ADMIN, EDITOR, USER" -ForegroundColor Green
            Write-Host "   ✅ Tipos de Trabajo: 10 tipos" -ForegroundColor Green
            Write-Host "   ✅ Grupos: 8 grupos" -ForegroundColor Green
            Write-Host ""
        }
        
        Write-Host "🎉 Puedes iniciar sesión inmediatamente" -ForegroundColor Green
        Write-Host ""
        
        # Mostrar resultado de la consulta
        Write-Host "📊 DETALLES DEL USUARIO:" -ForegroundColor Cyan
        Write-Host $result
        Write-Host ""
    }
    else {
        Write-Host ""
        Write-Host "❌ ERROR AL EJECUTAR SCRIPT" -ForegroundColor Red
        Write-Host ""
        Write-Host "Detalles:" -ForegroundColor Yellow
        Write-Host $result
        Write-Host ""
        
        # Limpiar archivo temporal
        Remove-Item $tempSqlFile -ErrorAction SilentlyContinue
        exit 1
    }
    
    # Limpiar archivo temporal
    Remove-Item $tempSqlFile -ErrorAction SilentlyContinue
    
}
catch {
    Write-Host ""
    Write-Host "❌ ERROR EJECUTANDO SCRIPT" -ForegroundColor Red
    Write-Host ""
    Write-Host "Detalles:" -ForegroundColor Yellow
    Write-Host $_.Exception.Message
    Write-Host ""
    
    Write-Host "🔧 POSIBLES CAUSAS:" -ForegroundColor Cyan
    Write-Host "   • psql no está instalado o no está en PATH" -ForegroundColor Yellow
    Write-Host "   • Credenciales de base de datos incorrectas" -ForegroundColor Yellow
    Write-Host "   • BCrypt.Net.dll no encontrado (ejecuta: dotnet build)" -ForegroundColor Yellow
    Write-Host "   • El usuario ya existe en la base de datos" -ForegroundColor Yellow
    Write-Host "   • El schema no existe (verifica que las migraciones se hayan ejecutado)" -ForegroundColor Yellow
    Write-Host ""
    
    exit 1
}

# ==================== RESUMEN ====================

Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    📋 RESUMEN                            ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Base de datos inicializada correctamente" -ForegroundColor Green
Write-Host ""
Write-Host "🔐 CREDENCIALES DE ACCESO:" -ForegroundColor Yellow
Write-Host "   Email: $Email" -ForegroundColor White
Write-Host "   Password: $Password" -ForegroundColor White
Write-Host ""
Write-Host "📊 DATOS DISPONIBLES:" -ForegroundColor Cyan
Write-Host "   ✅ Roles: ADMIN, EDITOR, USER" -ForegroundColor White
if (-not $SkipSeedData) {
    Write-Host "   ✅ Tipos: Incidencia, Instalación, Aviso, Petición, etc." -ForegroundColor White
    Write-Host "   ✅ Grupos: Administración, Comercial, Desarrollo, etc." -ForegroundColor White
}
Write-Host ""
Write-Host "⚠️  IMPORTANTE: Cambia la contraseña después del primer login" -ForegroundColor Yellow
Write-Host ""
Write-Host "🌐 URL de la API:" -ForegroundColor Cyan
if ($Render) {
    Write-Host "   https://gestiontimeapi.onrender.com" -ForegroundColor White
}
else {
    Write-Host "   http://localhost:5000" -ForegroundColor White
}
Write-Host ""
Write-Host "📚 Endpoints disponibles:" -ForegroundColor Cyan
Write-Host "   POST /api/v1/auth/login" -ForegroundColor Gray
Write-Host "   GET  /api/v1/auth/me" -ForegroundColor Gray
Write-Host "   GET  /api/v1/admin/users" -ForegroundColor Gray
Write-Host "   GET  /api/v1/tipos" -ForegroundColor Gray
Write-Host "   GET  /api/v1/grupos" -ForegroundColor Gray
Write-Host ""
