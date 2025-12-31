# Script para exportar schema gtdefault a CSV
# Uso: .\export_gtdefault.ps1

$ErrorActionPreference = "Stop"

# Configuración
$DATABASE_URL = $env:DATABASE_URL
if (-not $DATABASE_URL) {
    Write-Host "❌ ERROR: Variable DATABASE_URL no encontrada" -ForegroundColor Red
    Write-Host "Configúrala con: `$env:DATABASE_URL = 'tu_connection_string'" -ForegroundColor Yellow
    exit 1
}

$OUTPUT_DIR = ".\gtdefault_export_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
New-Item -ItemType Directory -Path $OUTPUT_DIR -Force | Out-Null

Write-Host "`n╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║       📦 EXPORTANDO SCHEMA GTDEFAULT A CSV 📦           ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

# 1. Obtener lista de tablas en gtdefault
Write-Host "🔍 Buscando tablas en schema 'gtdefault'..." -ForegroundColor Yellow

$queryTablas = @"
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'gtdefault' 
ORDER BY table_name;
"@

$tablas = psql $DATABASE_URL -t -c $queryTablas | Where-Object { $_.Trim() -ne "" }

if (-not $tablas) {
    Write-Host "⚠️  No se encontraron tablas en schema 'gtdefault'" -ForegroundColor Yellow
    exit 0
}

$tablasArray = $tablas | ForEach-Object { $_.Trim() }
Write-Host "✅ Encontradas $($tablasArray.Count) tabla(s):`n" -ForegroundColor Green

foreach ($tabla in $tablasArray) {
    Write-Host "   • $tabla" -ForegroundColor White
}

Write-Host ""

# 2. Exportar cada tabla a CSV
$contador = 0
foreach ($tabla in $tablasArray) {
    $contador++
    $outputFile = Join-Path $OUTPUT_DIR "$tabla.csv"
    
    Write-Host "[$contador/$($tablasArray.Count)] Exportando tabla '$tabla'..." -ForegroundColor Cyan -NoNewline
    
    try {
        # Exportar a CSV con headers
        $query = "\COPY gtdefault.$tabla TO '$outputFile' CSV HEADER"
        psql $DATABASE_URL -c $query | Out-Null
        
        # Obtener número de registros
        $countQuery = "SELECT COUNT(*) FROM gtdefault.$tabla;"
        $registros = psql $DATABASE_URL -t -c $countQuery
        
        Write-Host " ✅ ($($registros.Trim()) registros)" -ForegroundColor Green
    }
    catch {
        Write-Host " ❌ ERROR" -ForegroundColor Red
        Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
    }
}

# 3. Crear archivo de resumen
Write-Host "`n📊 Generando resumen..." -ForegroundColor Yellow

$resumen = @"
EXPORTACIÓN SCHEMA GTDEFAULT
=============================
Fecha: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Total de tablas: $($tablasArray.Count)

TABLAS EXPORTADAS:
"@

foreach ($tabla in $tablasArray) {
    $countQuery = "SELECT COUNT(*) FROM gtdefault.$tabla;"
    $registros = psql $DATABASE_URL -t -c $countQuery
    $resumen += "`n- $tabla : $($registros.Trim()) registros"
}

$resumen | Out-File -FilePath (Join-Path $OUTPUT_DIR "README.txt") -Encoding UTF8

# 4. Crear script SQL para recrear datos
Write-Host "📝 Generando script SQL de inserción..." -ForegroundColor Yellow

$sqlFile = Join-Path $OUTPUT_DIR "gtdefault_inserts.sql"
"-- Script de inserción para schema gtdefault`n-- Generado: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`n" | Out-File -FilePath $sqlFile -Encoding UTF8

foreach ($tabla in $tablasArray) {
    "`n-- Tabla: $tabla" | Out-File -FilePath $sqlFile -Append -Encoding UTF8
    $dumpQuery = "pg_dump $DATABASE_URL --schema=gtdefault --table=gtdefault.$tabla --data-only --inserts"
    Invoke-Expression $dumpQuery | Out-File -FilePath $sqlFile -Append -Encoding UTF8
}

Write-Host "✅ Script SQL generado" -ForegroundColor Green

# 5. Resumen final
Write-Host "`n╔══════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║               ✅ EXPORTACIÓN COMPLETADA ✅                ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════╝`n" -ForegroundColor Green

Write-Host "📁 Archivos generados en: $OUTPUT_DIR" -ForegroundColor Cyan
Write-Host "`nContenido:" -ForegroundColor White
Get-ChildItem $OUTPUT_DIR | ForEach-Object {
    $size = [Math]::Round($_.Length / 1KB, 2)
    Write-Host "   • $($_.Name) ($size KB)" -ForegroundColor Gray
}

Write-Host "`n💡 Para importar los datos:" -ForegroundColor Yellow
Write-Host "   1. Copiar los archivos CSV al servidor destino" -ForegroundColor White
Write-Host "   2. Ejecutar: psql `$DATABASE_URL -f gtdefault_inserts.sql" -ForegroundColor White
Write-Host "   3. O importar CSV individualmente con: \COPY tabla FROM 'archivo.csv' CSV HEADER" -ForegroundColor White