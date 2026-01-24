# ═══════════════════════════════════════════════════════════════════════════════
# BACKUP DESDE RENDER - PostgreSQL
# ═══════════════════════════════════════════════════════════════════════════════

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🗄️  BACKUP DESDE RENDER (PostgreSQL Cloud)" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════`n" -ForegroundColor Cyan

Write-Host "📋 Necesitas la URL de conexión externa de Render" -ForegroundColor Yellow
Write-Host "   1. Ve a tu Dashboard de Render" -ForegroundColor Gray
Write-Host "   2. Click en tu base de datos PostgreSQL" -ForegroundColor Gray
Write-Host "   3. Copia la 'External Database URL' o 'EXTERNAL_URL'`n" -ForegroundColor Gray

Write-Host "Ejemplo de URL de Render:" -ForegroundColor Cyan
Write-Host "postgres://user:password@dpg-xxxxx-a.oregon-postgres.render.com/database_name`n" -ForegroundColor Gray

$renderUrl = Read-Host "🔗 Pega aquí la URL completa de Render"

if ([string]::IsNullOrWhiteSpace($renderUrl)) {
    Write-Host "`n❌ URL vacía. Abortando." -ForegroundColor Red
    exit 1
}

# Parsear URL de Render
if ($renderUrl -match "postgres://([^:]+):([^@]+)@([^/]+)/(.+)") {
    $username = $matches[1]
    $password = $matches[2]
    $host = $matches[3]
    $database = $matches[4]
    
    Write-Host "`n✅ URL parseada correctamente:" -ForegroundColor Green
    Write-Host "   Host: $host" -ForegroundColor Cyan
    Write-Host "   Database: $database" -ForegroundColor Cyan
    Write-Host "   Usuario: $username" -ForegroundColor Cyan
}
else {
    Write-Host "`n❌ Formato de URL incorrecto" -ForegroundColor Red
    Write-Host "   Debe ser: postgres://user:pass@host/database" -ForegroundColor Yellow
    exit 1
}

# Crear carpeta de backups
$backupFolder = "C:\GestionTime\GestionTimeApi\backups"
if (-not (Test-Path $backupFolder)) {
    New-Item -Path $backupFolder -ItemType Directory | Out-Null
}

# Nombre del archivo
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $backupFolder "backup_render_${timestamp}.sql"

Write-Host "`n📦 Archivo de backup: $backupFile" -ForegroundColor Yellow
Write-Host "`n🔄 Conectando a Render y descargando backup..." -ForegroundColor Yellow
Write-Host "   (Esto puede tardar varios minutos dependiendo del tamaño)`n" -ForegroundColor Gray

# Configurar contraseña
$env:PGPASSWORD = $password

try {
    # Ejecutar pg_dump contra Render
    $arguments = @(
        "-h", $host,
        "-U", $username,
        "-d", $database,
        "-n", "pss_dvnx",  # Solo el schema específico
        "-F", "p",
        "-f", $backupFile,
        "--no-owner",
        "--no-acl",
        "--verbose"
    )
    
    Write-Host "Ejecutando: pg_dump -h $host -U $username -d $database -n pss_dvnx" -ForegroundColor Gray
    
    $process = Start-Process -FilePath "pg_dump" -ArgumentList $arguments -Wait -NoNewWindow -PassThru -RedirectStandardError "backup_error.log"
    
    if ($process.ExitCode -eq 0 -and (Test-Path $backupFile)) {
        $fileSize = (Get-Item $backupFile).Length / 1MB
        
        Write-Host "`n✅ ¡BACKUP COMPLETADO EXITOSAMENTE!" -ForegroundColor Green
        Write-Host "   Archivo: $backupFile" -ForegroundColor Cyan
        Write-Host "   Tamaño: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Cyan
        
        # Crear info file
        $infoFile = "$backupFile.info"
        $info = @"
═══════════════════════════════════════════════════════
BACKUP DESDE RENDER
═══════════════════════════════════════════════════════
Fecha: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Host: $host
Database: $database
Schema: pss_dvnx
Tamaño: $([math]::Round($fileSize, 2)) MB

PARA RESTAURAR (LOCAL):
psql -U postgres -d tu_db_local -f "$backupFile"

PARA RESTAURAR (RENDER):
psql "$renderUrl" -f "$backupFile"
═══════════════════════════════════════════════════════
"@
        Set-Content -Path $infoFile -Value $info
        
        Write-Host "`n📝 Información guardada: $infoFile" -ForegroundColor Cyan
        Write-Host "`n✅ Ahora es seguro hacer cambios en el backend" -ForegroundColor Green
    }
    else {
        Write-Host "`n❌ Error al crear backup" -ForegroundColor Red
        if (Test-Path "backup_error.log") {
            Write-Host "`nError log:" -ForegroundColor Yellow
            Get-Content "backup_error.log" | ForEach-Object { Write-Host "   $_" -ForegroundColor Red }
        }
    }
}
catch {
    Write-Host "`n❌ Error: $_" -ForegroundColor Red
}
finally {
    $env:PGPASSWORD = $null
    if (Test-Path "backup_error.log") {
        Remove-Item "backup_error.log" -Force
    }
}

Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Cyan
