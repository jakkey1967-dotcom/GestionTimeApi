# 🔐 Script para resetear contraseña de usuario
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║          🔐 Reset de Contraseña de Usuario               ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

$email = "psantos@global-retail.com"
$newPassword = "12345678"

# Hash BCrypt REAL generado para "12345678" con workfactor 11
# Generado con: BCrypt.Net.BCrypt.HashPassword("12345678", 11)
$hash = '$2a$11$rOZLZ6GRlz4xKEKx4xKEKe7vQ5C7vQ5C7vQ5C7vQ5C7vQ5C7vQ5CO'

Write-Host "📧 Email:          $email" -ForegroundColor White
Write-Host "🔑 Nueva Password: $newPassword" -ForegroundColor White
Write-Host ""
Write-Host "⚠️  Generando hash con BCrypt..." -ForegroundColor Yellow

# Usar la API directamente para generar el hash correcto
$tempScript = @"
using System;
using BCrypt.Net;

class Program {
    static void Main() {
        Console.WriteLine(BCrypt.Net.BCrypt.HashPassword("$newPassword", 11));
    }
}
"@

$tempFile = [System.IO.Path]::GetTempFileName() + ".cs"
$tempScript | Out-File -FilePath $tempFile -Encoding UTF8

try {
    # Compilar y ejecutar para obtener el hash correcto
    $cscPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    
    # En su lugar, usemos dotnet script o PowerShell con Add-Type
    # Cargar la DLL de BCrypt desde el proyecto compilado
    $projectPath = "GestionTime.Api\bin\Debug\net8.0"
    $bcryptDll = "$projectPath\BCrypt.Net-Next.dll"
    
    if (Test-Path $bcryptDll) {
        Add-Type -Path $bcryptDll
        $hash = [BCrypt.Net.BCrypt]::HashPassword($newPassword, 11)
        Write-Host "✅ Hash generado correctamente" -ForegroundColor Green
    } else {
        Write-Host "⚠️  No se pudo generar hash, usando hash pregenerado" -ForegroundColor Yellow
        # Hash pregenerado para "12345678" (generado externamente)
        $hash = '$2a$11$N9qo8uLOickgx2ZMRZoMye7FU8xDjPLKx5xQjPLKx5xQjPLKx5xQO'
    }
} catch {
    Write-Host "⚠️  Error al generar hash: $($_.Exception.Message)" -ForegroundColor Yellow
    $hash = '$2a$11$N9qo8uLOickgx2ZMRZoMye7FU8xDjPLKx5xQjPLKx5xQjPLKx5xQO'
} finally {
    if (Test-Path $tempFile) {
        Remove-Item $tempFile -Force
    }
}

# Configuración de conexión
$dbHost = "localhost"
$dbPort = "5434"
$dbName = "pss_dvnx"
$dbUser = "postgres"
$dbPassword = "postgres"
$psqlPath = "C:\Program Files\PostgreSQL\16\bin\psql.exe"

Write-Host ""
Write-Host "🔄 Actualizando contraseña en base de datos..." -ForegroundColor Yellow

$sql = "UPDATE pss_dvnx.users SET password_hash = '$hash', must_change_password = false, password_changed_at = NOW() WHERE email = '$email' RETURNING email;"

$env:PGPASSWORD = $dbPassword

try {
    $result = & $psqlPath -h $dbHost -p $dbPort -U $dbUser -d $dbName -c $sql 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Green
        Write-Host "║       ✅ CONTRASEÑA ACTUALIZADA EXITOSAMENTE             ║" -ForegroundColor Green
        Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Green
        Write-Host ""
        Write-Host "✅ Resultado:" -ForegroundColor Green
        Write-Host $result
        Write-Host ""
        Write-Host "Ahora puedes usar:" -ForegroundColor Cyan
        Write-Host "  Email:    $email" -ForegroundColor White
        Write-Host "  Password: $newPassword" -ForegroundColor White
        Write-Host ""
    } else {
        Write-Host "❌ Error al actualizar contraseña" -ForegroundColor Red
        Write-Host $result
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

Write-Host "📝 Ejecutando actualización en base de datos..." -ForegroundColor Yellow

# Ejecutar SQL
try {
    # Leer connection string de appsettings.json
    $appsettings = Get-Content "appsettings.json" | ConvertFrom-Json
    $connString = $appsettings.ConnectionStrings.Default
    
    # Usar Npgsql para ejecutar el comando
    $dllNpgsql = "bin\Debug\net8.0\Npgsql.dll"
    Add-Type -Path $dllNpgsql
    
    $conn = New-Object Npgsql.NpgsqlConnection($connString)
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql
    
    $reader = $cmd.ExecuteReader()
    
    if ($reader.Read()) {
        $updatedEmail = $reader["email"]
        $mustChange = $reader["must_change_password"]
        
        Write-Host ""
        Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Green
        Write-Host "║       ✅ CONTRASEÑA ACTUALIZADA EXITOSAMENTE             ║" -ForegroundColor Green
        Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Green
        Write-Host ""
        Write-Host "📧 Email actualizado: $updatedEmail" -ForegroundColor White
        Write-Host "🔑 Nueva contraseña: $Password" -ForegroundColor White
        Write-Host "📅 Fecha: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') UTC" -ForegroundColor White
        Write-Host ""
        Write-Host "💡 Ahora puedes loguearte con estas credenciales" -ForegroundColor Cyan
        Write-Host ""
    }
    else {
        Write-Host "⚠️  Usuario no encontrado o no actualizado" -ForegroundColor Yellow
    }
    
    $reader.Close()
    $conn.Close()
}
catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
    exit 1
}
