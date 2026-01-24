# ═══════════════════════════════════════════════════════════════════════════════
# BACKUP SIMPLIFICADO - Base de Datos PostgreSQL
# ═══════════════════════════════════════════════════════════════════════════════

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🗄️  BACKUP DE BASE DE DATOS" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════`n" -ForegroundColor Cyan

# Crear carpeta de backups
$backupFolder = "C:\GestionTime\GestionTimeApi\backups"
if (-not (Test-Path $backupFolder)) {
    New-Item -Path $backupFolder -ItemType Directory | Out-Null
    Write-Host "✅ Carpeta de backups creada: $backupFolder`n" -ForegroundColor Green
}

# Solicitar datos de conexión
Write-Host "📋 Información de la base de datos:" -ForegroundColor Yellow
Write-Host ""

$host_input = Read-Host "   Host (presiona Enter para 'localhost')"
$host = if ([string]::IsNullOrWhiteSpace($host_input)) { "localhost" } else { $host_input }

$port_input = Read-Host "   Puerto (presiona Enter para '5432')"
$port = if ([string]::IsNullOrWhiteSpace($port_input)) { "5432" } else { $port_input }

$database = Read-Host "   Nombre de la base de datos"

$username_input = Read-Host "   Usuario (presiona Enter para 'postgres')"
$username = if ([string]::IsNullOrWhiteSpace($username_input)) { "postgres" } else { $username_input }

$securePassword = Read-Host "   Contraseña" -AsSecureString
$password = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword))

$schema_input = Read-Host "   Schema (presiona Enter para 'pss_dvnx')"
$schema = if ([string]::IsNullOrWhiteSpace($schema_input)) { "pss_dvnx" } else { $schema_input }

# Generar nombre del archivo
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $backupFolder "backup_${schema}_${timestamp}.sql"

Write-Host "`n📦 Archivo de backup:" -ForegroundColor Yellow
Write-Host "   $backupFile" -ForegroundColor Cyan

Write-Host "`n🔄 Iniciando backup..." -ForegroundColor Yellow

# Configurar contraseña para pg_dump
$env:PGPASSWORD = $password

try {
    # Ejecutar pg_dump
    $arguments = @(
        "-h", $host,
        "-p", $port,
        "-U", $username,
        "-d", $database,
        "-n", $schema,
        "-F", "p",
        "-f", $backupFile,
        "--no-owner",
        "--no-acl"
    )
    
    $process = Start-Process -FilePath "pg_dump" -ArgumentList $arguments -Wait -NoNewWindow -PassThru
    
    if ($process.ExitCode -eq 0) {
        if (Test-Path $backupFile) {
            $fileSize = (Get-Item $backupFile).Length / 1MB
            
            Write-Host "`n✅ ¡BACKUP COMPLETADO EXITOSAMENTE!" -ForegroundColor Green
            Write-Host "   Archivo: $backupFile" -ForegroundColor Cyan
            Write-Host "   Tamaño: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Cyan
            
            # Crear archivo de información
            $infoFile = "$backupFile.info"
            $info = @"
═══════════════════════════════════════════════════════
BACKUP INFORMATION
═══════════════════════════════════════════════════════
Fecha: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Host: $host
Database: $database
Schema: $schema
Usuario: $username
Tamaño: $([math]::Round($fileSize, 2)) MB

PARA RESTAURAR:
psql -h $host -U $username -d $database -f "$backupFile"
═══════════════════════════════════════════════════════
"@
            Set-Content -Path $infoFile -Value $info
            
            Write-Host "`n📝 Información guardada en: $infoFile" -ForegroundColor Cyan
            
            # Mostrar primeras líneas del backup
            Write-Host "`n📄 Primeras líneas del backup:" -ForegroundColor Yellow
            Get-Content $backupFile -First 10 | ForEach-Object { Write-Host "   $_" -ForegroundColor Gray }
            
            # Listar otros backups
            Write-Host "`n📚 Backups disponibles en esta carpeta:" -ForegroundColor Yellow
            Get-ChildItem -Path $backupFolder -Filter "*.sql" | 
                Sort-Object LastWriteTime -Descending | 
                Select-Object -First 5 | 
                ForEach-Object {
                    $size = $_.Length / 1MB
                    $age = (Get-Date) - $_.LastWriteTime
                    $ageStr = if ($age.TotalDays -ge 1) {
                        "$([math]::Floor($age.TotalDays)) días"
                    } elseif ($age.TotalHours -ge 1) {
                        "$([math]::Floor($age.TotalHours)) horas"
                    } else {
                        "$([math]::Floor($age.TotalMinutes)) minutos"
                    }
                    Write-Host "   $($_.Name) - $([math]::Round($size, 2)) MB (hace $ageStr)" -ForegroundColor Gray
                }
            
            Write-Host "`n✅ Ahora es seguro hacer cambios en el backend" -ForegroundColor Green
            Write-Host "   Si algo sale mal, puedes restaurar con:" -ForegroundColor Yellow
            Write-Host "   psql -h $host -U $username -d $database -f `"$backupFile`"" -ForegroundColor Cyan
        }
        else {
            Write-Host "`n❌ Error: El archivo de backup no se creó" -ForegroundColor Red
        }
    }
    else {
        Write-Host "`n❌ Error al ejecutar pg_dump. Código de salida: $($process.ExitCode)" -ForegroundColor Red
        Write-Host "   Verifica las credenciales y la conexión a la base de datos" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "`n❌ Error durante el backup: $_" -ForegroundColor Red
}
finally {
    # Limpiar contraseña
    $env:PGPASSWORD = $null
}

Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Cyan
