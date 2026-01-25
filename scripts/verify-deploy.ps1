# Script para verificar configuración antes de deploy en Render

Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║        🚀 VERIFICACIÓN PRE-DEPLOY PARA RENDER               ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

$hasErrors = $false

# 1. Verificar Dockerfile
Write-Host "`n1️⃣ Verificando Dockerfile..." -ForegroundColor Yellow
if (!(Test-Path "Dockerfile")) {
    Write-Host "   ❌ Falta Dockerfile en la raíz del proyecto" -ForegroundColor Red
    $hasErrors = $true
} else {
    $dockerContent = Get-Content "Dockerfile" -Raw
    if ($dockerContent -match "EXPOSE 8080") {
        Write-Host "   ✅ Dockerfile encontrado y configurado correctamente" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️ Dockerfile existe pero puede estar mal configurado" -ForegroundColor Yellow
    }
}

# 2. Verificar appsettings.Production.json
Write-Host "`n2️⃣ Verificando appsettings.Production.json..." -ForegroundColor Yellow
if (!(Test-Path "appsettings.Production.json")) {
    Write-Host "   ❌ Falta appsettings.Production.json" -ForegroundColor Red
    $hasErrors = $true
} else {
    $prodConfig = Get-Content "appsettings.Production.json" -Raw | ConvertFrom-Json
    
    # Verificar que usa variables de entorno
    if ($prodConfig.ConnectionStrings.Default -match '\$\{DATABASE_URL\}') {
        Write-Host "   ✅ ConnectionString usa variable de entorno" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️ ConnectionString puede tener valor hardcodeado" -ForegroundColor Yellow
    }
    
    if ($prodConfig.Jwt.Key -match '\$\{JWT_secret_key\}') {
        Write-Host "   ✅ JWT Key usa variable de entorno" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️ JWT Key puede estar hardcodeada" -ForegroundColor Yellow
    }
    
    # Verificar CORS
    if ($prodConfig.Cors.Origins) {
        Write-Host "   ✅ CORS configurado con $($prodConfig.Cors.Origins.Count) orígenes" -ForegroundColor Green
        foreach ($origin in $prodConfig.Cors.Origins) {
            Write-Host "      • $origin" -ForegroundColor Gray
        }
    }
}

# 3. Verificar Program.cs (Health Check)
Write-Host "`n3️⃣ Verificando Health Check en Program.cs..." -ForegroundColor Yellow
if (Test-Path "Program.cs") {
    $programContent = Get-Content "Program.cs" -Raw
    if ($programContent -match 'MapHealthChecks.*"/health"') {
        Write-Host "   ✅ Health check endpoint configurado: /health" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️ No se encontró endpoint /health (recomendado para Render)" -ForegroundColor Yellow
    }
}

# 4. Build local
Write-Host "`n4️⃣ Compilando proyecto (Release)..." -ForegroundColor Yellow
$buildOutput = dotnet build GestionTime.sln -c Release 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Compilación exitosa" -ForegroundColor Green
} else {
    Write-Host "   ❌ Error de compilación:" -ForegroundColor Red
    Write-Host $buildOutput | Select-Object -Last 10
    $hasErrors = $true
}

# 5. Verificar migraciones
Write-Host "`n5️⃣ Verificando migraciones..." -ForegroundColor Yellow
try {
    $migrations = dotnet ef migrations list --project GestionTime.Infrastructure 2>&1
    if ($LASTEXITCODE -eq 0) {
        $migrationsList = $migrations | Select-String -Pattern "^\d{14}_"
        Write-Host "   ✅ $($migrationsList.Count) migraciones encontradas:" -ForegroundColor Green
        $migrationsList | Select-Object -Last 3 | ForEach-Object {
            Write-Host "      • $($_.Line.Trim())" -ForegroundColor Gray
        }
    } else {
        Write-Host "   ⚠️ No se pudieron listar migraciones (puede ser normal si no hay BD local)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ⚠️ Error al verificar migraciones: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 6. Verificar .gitignore
Write-Host "`n6️⃣ Verificando .gitignore..." -ForegroundColor Yellow
if (Test-Path ".gitignore") {
    $gitignoreContent = Get-Content ".gitignore" -Raw
    if ($gitignoreContent -match "appsettings\.Development\.json") {
        Write-Host "   ✅ .gitignore configurado correctamente" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️ .gitignore puede no estar filtrando archivos sensibles" -ForegroundColor Yellow
    }
}

# 7. Verificar Git status
Write-Host "`n7️⃣ Verificando estado de Git..." -ForegroundColor Yellow
$gitStatus = git status --porcelain 2>&1
if ($gitStatus) {
    Write-Host "   ⚠️ Hay cambios sin commitear:" -ForegroundColor Yellow
    $gitStatus | Select-Object -First 5 | ForEach-Object {
        Write-Host "      $_" -ForegroundColor Gray
    }
} else {
    Write-Host "   ✅ Working directory limpio" -ForegroundColor Green
}

# 8. Verificar rama actual
$currentBranch = git branch --show-current 2>&1
if ($currentBranch -eq "main") {
    Write-Host "   ✅ En rama 'main'" -ForegroundColor Green
} else {
    Write-Host "   ⚠️ No estás en rama 'main' (actual: $currentBranch)" -ForegroundColor Yellow
}

# RESUMEN
Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                      📊 RESUMEN                               ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

if ($hasErrors) {
    Write-Host "`n❌ HAY ERRORES QUE DEBEN CORREGIRSE ANTES DE DEPLOY" -ForegroundColor Red
    exit 1
} else {
    Write-Host "`n✅ TODO LISTO PARA DEPLOY EN RENDER!" -ForegroundColor Green
    
    Write-Host "`n📋 PRÓXIMOS PASOS:" -ForegroundColor Cyan
    Write-Host "   1. Configurar variables de entorno en Render Dashboard" -ForegroundColor White
    Write-Host "   2. Commit de cambios: git add . && git commit -m 'Deploy config'" -ForegroundColor White
    Write-Host "   3. Push a main: git push origin main" -ForegroundColor White
    Write-Host "   4. Render detectará el cambio y hará deploy automáticamente" -ForegroundColor White
    Write-Host "   5. Verificar en: https://dashboard.render.com" -ForegroundColor White
    
    Write-Host "`n📚 Ver guía completa en: docs/RENDER_DEPLOY_GUIDE.md" -ForegroundColor Cyan
}
