# ═══════════════════════════════════════════════════════════════════════════════
# Script de Backup - Base de Datos PostgreSQL (pss_dvnx)
# ═══════════════════════════════════════════════════════════════════════════════
# Uso: .\Backup-Database.ps1
# ═══════════════════════════════════════════════════════════════════════════════

param(
    [Parameter(Mandatory=$false)]
    [string]$Host = "localhost",
    
    [Parameter(Mandatory=$false)]
    [string]$Port = "5432",
    
    [Parameter(Mandatory=$false)]
    [string]$Database = "gestiontime_db",
    
    [Parameter(Mandatory=$false)]
    [string]$Username = "postgres",
    
    [Parameter(Mandatory=$false)]
    [string]$Schema = "pss_dvnx",
    
    [Parameter(Mandatory=$false)]
    [string]$BackupPath = ".\backups"
)

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🗄️  Backup de Base de Datos PostgreSQL" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════`n" -ForegroundColor Cyan

# Crear carpeta de backups si no existe
if (-not (Test-Path $BackupPath)) {
    New-Item -Path $BackupPath -ItemType Directory | Out-Null
    Write-Host "✅ Carpeta de backups creada: $BackupPath" -ForegroundColor Green
}

# Generar nombre de archivo con timestamp
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $BackupPath "backup_${Schema}_${timestamp}.sql"
$backupCompressed = "${backupFile}.gz"

Write-Host "📂 Información del backup:" -ForegroundColor Yellow
Write-Host "   Host: $Host" -ForegroundColor Gray
Write-Host "   Database: $Database" -ForegroundColor Gray
Write-Host "   Schema: $Schema" -ForegroundColor Gray
Write-Host "   Archivo: $backupFile" -ForegroundColor Gray

# Solicitar contraseña
$securePassword = Read-Host "🔒 Contraseña de PostgreSQL" -AsSecureString
$password = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
)

# Configurar variable de entorno para pg_dump
$env:PGPASSWORD = $password

Write-Host "`n🔄 Iniciando backup..." -ForegroundColor Yellow

try {
    # OPCIÓN A: Backup solo del schema pss_dvnx (más rápido)
    Write-Host "   Modo: Schema específico ($Schema)" -ForegroundColor Cyan
    
    $pgDumpArgs = @(
        "-h", $Host,
        "-p", $Port,
        "-U", $Username,
        "-d", $Database,
        "-n", $Schema,
        "-F", "p",  # Formato plain SQL
        "-f", $backupFile,
        "--verbose",
        "--no-owner",
        "--no-acl"
    )
    
    & pg_dump $pgDumpArgs 2>&1 | ForEach-Object {
        if ($_ -match "error|ERROR") {
            Write-Host "   ❌ $_" -ForegroundColor Red
        }
        else {
            Write-Host "   $_" -ForegroundColor Gray
        }
    }
    
    if ($LASTEXITCODE -eq 0) {
        $fileSize = (Get-Item $backupFile).Length / 1MB
        Write-Host "`n✅ Backup completado exitosamente!" -ForegroundColor Green
        Write-Host "   Archivo: $backupFile" -ForegroundColor Cyan
        Write-Host "   Tamaño: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Cyan
        
        # Comprimir backup (opcional)
        if (Get-Command gzip -ErrorAction SilentlyContinue) {
            Write-Host "`n🗜️  Comprimiendo backup..." -ForegroundColor Yellow
            & gzip -9 $backupFile
            
            if (Test-Path $backupCompressed) {
                $compressedSize = (Get-Item $backupCompressed).Length / 1MB
                Write-Host "✅ Backup comprimido: $backupCompressed" -ForegroundColor Green
                Write-Host "   Tamaño comprimido: $([math]::Round($compressedSize, 2)) MB" -ForegroundColor Cyan
                Write-Host "   Reducción: $([math]::Round(($fileSize - $compressedSize) / $fileSize * 100, 1))%" -ForegroundColor Cyan
            }
        }
        
        # Crear archivo de metadata
        $metadataFile = "${backupFile}.info"
        $metadata = @"
═══════════════════════════════════════════════════════
BACKUP METADATA
═══════════════════════════════════════════════════════
Fecha: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Host: $Host
Database: $Database
Schema: $Schema
Usuario: $Username
Tamaño: $([math]::Round($fileSize, 2)) MB

═══════════════════════════════════════════════════════
RESTAURACIÓN
═══════════════════════════════════════════════════════
Para restaurar este backup:

psql -h $Host -U $Username -d $Database -f "$backupFile"

O si está comprimido:

gunzip -c "$backupCompressed" | psql -h $Host -U $Username -d $Database

═══════════════════════════════════════════════════════
"@
        Set-Content -Path $metadataFile -Value $metadata
        Write-Host "`n📝 Metadata guardada: $metadataFile" -ForegroundColor Cyan
        
        # Listar backups anteriores
        Write-Host "`n📚 Backups disponibles:" -ForegroundColor Yellow
        Get-ChildItem -Path $BackupPath -Filter "backup_${Schema}_*.sql*" | 
            Sort-Object LastWriteTime -Descending | 
            Select-Object -First 5 | 
            ForEach-Object {
                $age = (Get-Date) - $_.LastWriteTime
                $ageStr = if ($age.TotalDays -ge 1) {
                    "$([math]::Floor($age.TotalDays)) días"
                } elseif ($age.TotalHours -ge 1) {
                    "$([math]::Floor($age.TotalHours)) horas"
                } else {
                    "$([math]::Floor($age.TotalMinutes)) minutos"
                }
                
                $size = $_.Length / 1MB
                Write-Host "   $($_.Name) - $([math]::Round($size, 2)) MB (hace $ageStr)" -ForegroundColor Gray
            }
    }
    else {
        Write-Host "`n❌ Error en el backup. Código de salida: $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "`n❌ Error durante el backup: $_" -ForegroundColor Red
    exit 1
}
finally {
    # Limpiar variable de entorno
    $env:PGPASSWORD = $null
}

Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ Proceso completado" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
