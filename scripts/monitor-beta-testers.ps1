# ========================================
# 📊 MONITOR DE ACTIVIDAD DE BETA TESTERS
# ========================================

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("local", "render")]
    [string]$Environment = "render",
    
    [Parameter(Mandatory=$false)]
    [string]$OutputFile = "beta-testers-report-$(Get-Date -Format 'yyyy-MM-dd-HHmmss').html"
)

$ErrorActionPreference = "Continue"

Write-Host "📊 MONITOR DE ACTIVIDAD DE BETA TESTERS" -ForegroundColor Cyan
Write-Host "=" * 60
Write-Host "Entorno: $Environment" -ForegroundColor Gray
Write-Host ""

# Configuración según entorno
if ($Environment -eq "local") {
    $baseUrl = "https://localhost:2502"
    Write-Host "🏠 Ejecutando en LOCAL" -ForegroundColor Yellow
} else {
    $baseUrl = "https://gestiontimeapi.onrender.com"
    Write-Host "☁️  Ejecutando en RENDER" -ForegroundColor Green
}

# ========================================
# 1. LOGIN
# ========================================
Write-Host "`n🔐 Paso 1: Autenticación..." -ForegroundColor Yellow

try {
    $loginBody = @{
        email = "psantos@global-retail.com"
        password = "12345678"
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/auth/login-desktop" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody `
        -SkipCertificateCheck:($Environment -eq "local")
    
    $token = $loginResponse.accessToken
    Write-Host "✅ Login exitoso" -ForegroundColor Green
    Write-Host "   Usuario: $($loginResponse.user.email)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Error en login:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Accept" = "application/json"
}

# ========================================
# 2. OBTENER ESTADÍSTICAS
# ========================================
Write-Host "`n📊 Paso 2: Recopilando estadísticas..." -ForegroundColor Yellow

# Endpoint personalizado que crearemos
$apiEndpoint = "$baseUrl/api/v1/users/activity-report"

Write-Host "   Llamando a: $apiEndpoint" -ForegroundColor Gray

# Por ahora, vamos a consultar endpoints existentes
$reportData = @{
    GeneratedAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Environment = $Environment.ToUpper()
    Users = @()
    Summary = @{}
}

# Obtener lista de usuarios (si existe endpoint)
try {
    Write-Host "   → Obteniendo usuarios..." -ForegroundColor Gray
    # Asumiendo que existe GET /api/v1/users (Admin)
    # Si no existe, esta sección fallará y se omitirá
    $usersResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/users" `
        -Method GET `
        -Headers $headers `
        -SkipCertificateCheck:($Environment -eq "local") `
        -ErrorAction SilentlyContinue
    
    if ($usersResponse) {
        $reportData.Users = $usersResponse
        Write-Host "   ✓ Usuarios obtenidos: $($usersResponse.Count)" -ForegroundColor Green
    }
}
catch {
    Write-Host "   ⚠️  No se pudo obtener lista de usuarios (endpoint no disponible o sin permisos)" -ForegroundColor Yellow
}

# Obtener estadísticas de partes
try {
    Write-Host "   → Obteniendo estadísticas de partes..." -ForegroundColor Gray
    
    # Por fecha (últimos 30 días)
    $desde = (Get-Date).AddDays(-30).ToString("yyyy-MM-dd")
    $hasta = (Get-Date).ToString("yyyy-MM-dd")
    
    $partesResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/partes?desde=$desde&hasta=$hasta" `
        -Method GET `
        -Headers $headers `
        -SkipCertificateCheck:($Environment -eq "local") `
        -ErrorAction SilentlyContinue
    
    if ($partesResponse) {
        $reportData.Summary.TotalPartes = $partesResponse.Count
        Write-Host "   ✓ Partes últimos 30 días: $($partesResponse.Count)" -ForegroundColor Green
    }
}
catch {
    Write-Host "   ⚠️  No se pudo obtener partes" -ForegroundColor Yellow
}

# ========================================
# 3. GENERAR REPORTE HTML
# ========================================
Write-Host "`n📄 Paso 3: Generando reporte HTML..." -ForegroundColor Yellow

$htmlReport = @"
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Reporte de Actividad Beta Testers - GestionTime</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { 
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 20px;
            color: #333;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            border-radius: 12px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            overflow: hidden;
        }
        .header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 40px;
            text-align: center;
        }
        .header h1 { font-size: 2.5em; margin-bottom: 10px; }
        .header p { font-size: 1.1em; opacity: 0.9; }
        .content { padding: 40px; }
        .section {
            margin-bottom: 40px;
            padding: 20px;
            background: #f8f9fa;
            border-radius: 8px;
            border-left: 4px solid #667eea;
        }
        .section h2 {
            color: #667eea;
            font-size: 1.8em;
            margin-bottom: 20px;
            display: flex;
            align-items: center;
            gap: 10px;
        }
        .stat-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 20px;
            margin-top: 20px;
        }
        .stat-card {
            background: white;
            padding: 25px;
            border-radius: 8px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
            transition: transform 0.2s;
        }
        .stat-card:hover {
            transform: translateY(-5px);
            box-shadow: 0 4px 16px rgba(0,0,0,0.15);
        }
        .stat-card h3 {
            color: #666;
            font-size: 0.9em;
            text-transform: uppercase;
            letter-spacing: 1px;
            margin-bottom: 10px;
        }
        .stat-card .value {
            color: #667eea;
            font-size: 2.5em;
            font-weight: bold;
        }
        .stat-card .label {
            color: #999;
            font-size: 0.85em;
            margin-top: 5px;
        }
        table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 20px;
            background: white;
            border-radius: 8px;
            overflow: hidden;
        }
        th {
            background: #667eea;
            color: white;
            padding: 15px;
            text-align: left;
            font-weight: 600;
        }
        td {
            padding: 12px 15px;
            border-bottom: 1px solid #eee;
        }
        tr:hover {
            background: #f8f9fa;
        }
        .badge {
            display: inline-block;
            padding: 4px 12px;
            border-radius: 20px;
            font-size: 0.85em;
            font-weight: 600;
        }
        .badge-success { background: #d4edda; color: #155724; }
        .badge-warning { background: #fff3cd; color: #856404; }
        .badge-danger { background: #f8d7da; color: #721c24; }
        .badge-info { background: #d1ecf1; color: #0c5460; }
        .footer {
            background: #f8f9fa;
            padding: 20px 40px;
            text-align: center;
            color: #666;
            border-top: 1px solid #dee2e6;
        }
        .alert {
            padding: 15px 20px;
            border-radius: 8px;
            margin-bottom: 20px;
        }
        .alert-info {
            background: #d1ecf1;
            color: #0c5460;
            border-left: 4px solid #17a2b8;
        }
        .emoji { font-size: 1.2em; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>📊 Reporte de Actividad</h1>
            <p>Monitoreo de Beta Testers - GestionTime</p>
            <p style="font-size: 0.9em; margin-top: 10px;">
                Generado: $($reportData.GeneratedAt) | Entorno: $($reportData.Environment)
            </p>
        </div>
        
        <div class="content">
            <div class="alert alert-info">
                <strong>ℹ️ Nota:</strong> Este reporte muestra la actividad de los usuarios beta testers en la aplicación GestionTime. 
                Los datos se actualizan en tiempo real desde la base de datos en producción.
            </div>

            <div class="section">
                <h2><span class="emoji">📈</span> Resumen General</h2>
                <div class="stat-grid">
                    <div class="stat-card">
                        <h3>Usuarios Activos</h3>
                        <div class="value">$($reportData.Users.Count)</div>
                        <div class="label">Total en sistema</div>
                    </div>
                    <div class="stat-card">
                        <h3>Partes Creados</h3>
                        <div class="value">$($reportData.Summary.TotalPartes)</div>
                        <div class="label">Últimos 30 días</div>
                    </div>
                    <div class="stat-card">
                        <h3>Adopción</h3>
                        <div class="value">--</div>
                        <div class="label">% de uso</div>
                    </div>
                    <div class="stat-card">
                        <h3>Última Actividad</h3>
                        <div class="value">Hoy</div>
                        <div class="label">Tiempo transcurrido</div>
                    </div>
                </div>
            </div>

            <div class="section">
                <h2><span class="emoji">👥</span> Usuarios Registrados</h2>
                <p style="color: #666; margin-bottom: 15px;">
                    Lista completa de usuarios beta testers y su estado actual.
                </p>
                <table>
                    <thead>
                        <tr>
                            <th>Usuario</th>
                            <th>Email</th>
                            <th>Rol</th>
                            <th>Estado</th>
                            <th>Última Sesión</th>
                        </tr>
                    </thead>
                    <tbody>
"@

# Agregar filas de usuarios
if ($reportData.Users.Count -gt 0) {
    foreach ($user in $reportData.Users) {
        $estadoBadge = if ($user.activo) { 
            '<span class="badge badge-success">Activo</span>' 
        } else { 
            '<span class="badge badge-danger">Inactivo</span>' 
        }
        
        $lastLogin = if ($user.lastLoginAt) {
            $user.lastLoginAt
        } else {
            '<span style="color: #999;">Nunca</span>'
        }
        
        $htmlReport += @"
                        <tr>
                            <td><strong>$($user.nombre)</strong></td>
                            <td>$($user.email)</td>
                            <td><span class="badge badge-info">$($user.role)</span></td>
                            <td>$estadoBadge</td>
                            <td>$lastLogin</td>
                        </tr>
"@
    }
} else {
    $htmlReport += @"
                        <tr>
                            <td colspan="5" style="text-align: center; color: #999; padding: 30px;">
                                No se pudo obtener la lista de usuarios. Ejecute el script SQL directamente en la base de datos.
                            </td>
                        </tr>
"@
}

$htmlReport += @"
                    </tbody>
                </table>
            </div>

            <div class="section">
                <h2><span class="emoji">💡</span> Recomendaciones</h2>
                <ul style="line-height: 2; color: #666;">
                    <li>📧 <strong>Contactar usuarios inactivos:</strong> Enviar recordatorio a usuarios que no han iniciado sesión.</li>
                    <li>📊 <strong>Analizar patrones de uso:</strong> Identificar funcionalidades más utilizadas.</li>
                    <li>🐛 <strong>Recopilar feedback:</strong> Solicitar opiniones sobre problemas encontrados.</li>
                    <li>🎓 <strong>Capacitación:</strong> Ofrecer sesiones de entrenamiento para usuarios nuevos.</li>
                </ul>
            </div>

            <div class="section">
                <h2><span class="emoji">📝</span> Consulta SQL Completa</h2>
                <p style="color: #666; margin-bottom: 10px;">
                    Para obtener estadísticas detalladas, ejecute el siguiente script SQL directamente en PostgreSQL:
                </p>
                <pre style="background: #2d2d2d; color: #f8f8f2; padding: 20px; border-radius: 8px; overflow-x: auto; font-family: 'Courier New', monospace; font-size: 0.9em;">
-- Ejecutar en Render Dashboard → PostgreSQL → Shell
\i scripts/monitor-beta-testers-activity.sql

-- O copiar y pegar el contenido del archivo SQL
                </pre>
            </div>
        </div>
        
        <div class="footer">
            <p>
                <strong>GestionTime API</strong> | 
                <a href="https://gestiontimeapi.onrender.com" style="color: #667eea;">gestiontimeapi.onrender.com</a>
            </p>
            <p style="font-size: 0.85em; color: #999; margin-top: 5px;">
                Reporte generado automáticamente | © 2026 GestionTime
            </p>
        </div>
    </div>
</body>
</html>
"@

# Guardar reporte HTML
$htmlReport | Out-File -FilePath $OutputFile -Encoding UTF8

Write-Host "✅ Reporte generado: $OutputFile" -ForegroundColor Green
Write-Host ""

# Abrir en navegador
Write-Host "🌐 Abriendo reporte en navegador..." -ForegroundColor Cyan
Start-Process $OutputFile

Write-Host "`n" + ("=" * 60)
Write-Host "✅ PROCESO COMPLETADO" -ForegroundColor Green
Write-Host ("=" * 60)
Write-Host ""
Write-Host "📌 Para obtener estadísticas detalladas:" -ForegroundColor Yellow
Write-Host "   1. Ir a Render Dashboard → PostgreSQL → Shell" -ForegroundColor Gray
Write-Host "   2. Ejecutar: \i scripts/monitor-beta-testers-activity.sql" -ForegroundColor Gray
Write-Host ""
