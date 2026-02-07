# ========================================
# 🧪 EJECUTAR TODOS LOS TESTS VERIFICADOS
# ========================================

$ErrorActionPreference = "Continue"

Write-Host "🧪 EJECUTAR TODOS LOS TESTS EN RENDER" -ForegroundColor Cyan
Write-Host "=" * 60

$testFolder = Join-Path $PSScriptRoot "tests-render-verified"
$testFiles = Get-ChildItem -Path $testFolder -Filter "test-*.ps1" | Sort-Object Name

if ($testFiles.Count -eq 0) {
    Write-Host "⚠️  No se encontraron archivos de test en: $testFolder" -ForegroundColor Yellow
    exit 0
}

Write-Host "`n📋 Tests disponibles: $($testFiles.Count)" -ForegroundColor Yellow
$testFiles | ForEach-Object {
    Write-Host "   - $($_.Name)" -ForegroundColor Gray
}

Write-Host "`n🚀 Iniciando ejecución..." -ForegroundColor Cyan
Write-Host ""

$results = @()
$totalTests = $testFiles.Count
$currentTest = 0

foreach ($testFile in $testFiles) {
    $currentTest++
    
    Write-Host "`n" + ("=" * 60) -ForegroundColor Cyan
    Write-Host "📊 Test $currentTest de ${totalTests}: $($testFile.Name)" -ForegroundColor Cyan
    Write-Host ("=" * 60) -ForegroundColor Cyan
    
    $startTime = Get-Date
    
    try {
        & $testFile.FullName
        $success = $?
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        $results += [PSCustomObject]@{
            Test = $testFile.Name
            Status = if ($success) { "✅ PASSED" } else { "❌ FAILED" }
            Duration = "{0:N2}s" -f $duration
        }
    }
    catch {
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        Write-Host "`n❌ ERROR al ejecutar test:" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        
        $results += [PSCustomObject]@{
            Test = $testFile.Name
            Status = "❌ ERROR"
            Duration = "{0:N2}s" -f $duration
        }
    }
    
    # Pausa entre tests
    if ($currentTest -lt $totalTests) {
        Write-Host "`n⏸️  Esperando 2 segundos antes del siguiente test..." -ForegroundColor Gray
        Start-Sleep -Seconds 2
    }
}

# Resumen final
Write-Host "`n" + ("=" * 60) -ForegroundColor Green
Write-Host "📊 RESUMEN DE TESTS" -ForegroundColor Green
Write-Host ("=" * 60) -ForegroundColor Green

$results | Format-Table -AutoSize

$passed = ($results | Where-Object { $_.Status -like "*PASSED*" }).Count
$failed = ($results | Where-Object { $_.Status -like "*FAILED*" -or $_.Status -like "*ERROR*" }).Count

Write-Host "`n✅ Tests exitosos: $passed" -ForegroundColor Green
Write-Host "❌ Tests fallidos: $failed" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })
Write-Host "📊 Total: $totalTests" -ForegroundColor Cyan

if ($failed -eq 0) {
    Write-Host "`n🎉 TODOS LOS TESTS PASARON EXITOSAMENTE" -ForegroundColor Green
}
else {
    Write-Host "`n⚠️  HAY TESTS FALLIDOS - REVISAR LOGS" -ForegroundColor Yellow
}

Write-Host ""
