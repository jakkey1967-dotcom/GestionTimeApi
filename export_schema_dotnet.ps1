# Script para exportar cualquier schema de PostgreSQL a CSV usando .NET
# Uso: .\export_schema_dotnet.ps1 -Schema "nombre_schema"

param(
    [Parameter(Mandatory=$false)]
    [string]$Schema,
    
    [Parameter(Mandatory=$false)]
    [string]$ConnectionString = $env:DATABASE_URL
)

$ErrorActionPreference = "Stop"

# Validar connection string
if (-not $ConnectionString) {
    Write-Host "`n? ERROR: Connection string no proporcionado" -ForegroundColor Red
    Write-Host "Uso: .\export_schema_dotnet.ps1 -Schema 'nombre_schema' -ConnectionString 'tu_connection_string'" -ForegroundColor Yellow
    Write-Host "O configura: `$env:DATABASE_URL = 'tu_connection_string'" -ForegroundColor Yellow
    exit 1
}

# Convertir DATABASE_URL de Render si es necesario
if ($ConnectionString -match "^postgres(ql)?://") {
    Write-Host "?? Convirtiendo formato Render a Npgsql..." -ForegroundColor Yellow
    
    $uri = [System.Uri]$ConnectionString
    $userInfo = $uri.UserInfo -split ':'
    
    $ConnectionString = "Host=$($uri.Host);Port=$($uri.Port);Database=$($uri.AbsolutePath.TrimStart('/'));Username=$($userInfo[0]);Password=$($userInfo[1]);SSL Mode=Require;Trust Server Certificate=true"
    Write-Host "? Connection string convertido" -ForegroundColor Green
}

# Agregar el paquete Npgsql si no está
Write-Host "`n?? Verificando Npgsql..." -ForegroundColor Cyan
try {
    Add-Type -Path "$PSScriptRoot\bin\Debug\net8.0\Npgsql.dll" -ErrorAction Stop
} catch {
    Write-Host "??  Npgsql no encontrado localmente, intentando cargar desde NuGet..." -ForegroundColor Yellow
    
    # Intentar cargar desde el proyecto actual
    $npgsqlPath = Get-ChildItem -Path "$PSScriptRoot" -Recurse -Filter "Npgsql.dll" -ErrorAction SilentlyContinue | Select-Object -First 1
    
    if ($npgsqlPath) {
        Add-Type -Path $npgsqlPath.FullName
        Write-Host "? Npgsql cargado: $($npgsqlPath.FullName)" -ForegroundColor Green
    } else {
        Write-Host "? No se pudo cargar Npgsql.dll" -ForegroundColor Red
        Write-Host "Por favor, compila el proyecto primero: dotnet build" -ForegroundColor Yellow
        exit 1
    }
}

function Invoke-PostgresQuery {
    param(
        [string]$Query,
        [string]$ConnString
    )
    
    $conn = New-Object Npgsql.NpgsqlConnection($ConnString)
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $Query
    
    $reader = $cmd.ExecuteReader()
    $results = @()
    
    while ($reader.Read()) {
        $row = @{}
        for ($i = 0; $i -lt $reader.FieldCount; $i++) {
            $row[$reader.GetName($i)] = $reader.GetValue($i)
        }
        $results += [PSCustomObject]$row
    }
    
    $reader.Close()
    $conn.Close()
    
    return $results
}

function Export-TableToCsv {
    param(
        [string]$SchemaName,
        [string]$TableName,
        [string]$OutputPath,
        [string]$ConnString
    )
    
    $conn = New-Object Npgsql.NpgsqlConnection($ConnString)
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT * FROM `"$SchemaName`".`"$TableName`""
    
    $reader = $cmd.ExecuteReader()
    
    # Crear archivo CSV con headers
    $headers = @()
    for ($i = 0; $i -lt $reader.FieldCount; $i++) {
        $headers += $reader.GetName($i)
    }
    
    $csv = New-Object System.Text.StringBuilder
    $csv.AppendLine(($headers -join ',')) | Out-Null
    
    $rowCount = 0
    while ($reader.Read()) {
        $values = @()
        for ($i = 0; $i -lt $reader.FieldCount; $i++) {
            $value = $reader.GetValue($i)
            if ($value -is [DBNull]) {
                $values += ""
            } else {
                $valueStr = $value.ToString() -replace '"', '""'
                if ($valueStr -match '[,"\r\n]') {
                    $values += "`"$valueStr`""
                } else {
                    $values += $valueStr
                }
            }
        }
        $csv.AppendLine(($values -join ',')) | Out-Null
        $rowCount++
    }
    
    $reader.Close()
    $conn.Close()
    
    [System.IO.File]::WriteAllText($OutputPath, $csv.ToString(), [System.Text.Encoding]::UTF8)
    
    return $rowCount
}

# Si no se especifica schema, mostrar lista
if (-not $Schema) {
    Write-Host "`n?? Conectando a la base de datos..." -ForegroundColor Cyan
    
    try {
        $schemas = Invoke-PostgresQuery -Query @"
SELECT schema_name 
FROM information_schema.schemata 
WHERE schema_name NOT IN ('pg_catalog', 'information_schema', 'pg_toast', 'pg_temp_1', 'pg_toast_temp_1')
ORDER BY schema_name
"@ -ConnString $ConnectionString
        
        if ($schemas.Count -eq 0) {
            Write-Host "??  No se encontraron schemas" -ForegroundColor Yellow
            exit 0
        }
        
        Write-Host "`n?? Schemas disponibles:`n" -ForegroundColor Green
        
        $i = 1
        foreach ($s in $schemas) {
            Write-Host "  $i. $($s.schema_name)" -ForegroundColor White
            $i++
        }
        
        Write-Host "`nSeleccione el número del schema a exportar (o Enter para cancelar): " -ForegroundColor Yellow -NoNewline
        $seleccion = Read-Host
        
        if ([string]::IsNullOrWhiteSpace($seleccion)) {
            Write-Host "Operación cancelada" -ForegroundColor Yellow
            exit 0
        }
        
        $index = [int]$seleccion - 1
        if ($index -ge 0 -and $index -lt $schemas.Count) {
            $Schema = $schemas[$index].schema_name
        } else {
            Write-Host "? Selección inválida" -ForegroundColor Red
            exit 1
        }
    } catch {
        Write-Host "? Error al conectar: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

# Crear directorio de salida
$TIMESTAMP = Get-Date -Format "yyyyMMdd_HHmmss"
$OUTPUT_DIR = ".\${Schema}_export_$TIMESTAMP"
New-Item -ItemType Directory -Path $OUTPUT_DIR -Force | Out-Null

Write-Host "`n????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?         ?? EXPORTANDO SCHEMA: $Schema" -ForegroundColor Cyan
Write-Host "????????????????????????????????????????????????????????????`n" -ForegroundColor Cyan

# Obtener lista de tablas
Write-Host "?? Buscando tablas en schema '$Schema'..." -ForegroundColor Yellow

try {
    $tablas = Invoke-PostgresQuery -Query @"
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = '$Schema' 
  AND table_type = 'BASE TABLE'
ORDER BY table_name
"@ -ConnString $ConnectionString

    if ($tablas.Count -eq 0) {
        Write-Host "??  No se encontraron tablas en schema '$Schema'" -ForegroundColor Yellow
        exit 0
    }
    
    Write-Host "? Encontradas $($tablas.Count) tabla(s):`n" -ForegroundColor Green
    
    # Mostrar tablas con conteo
    foreach ($tabla in $tablas) {
        $tableName = $tabla.table_name
        try {
            $count = Invoke-PostgresQuery -Query "SELECT COUNT(*) as count FROM `"$Schema`".`"$tableName`"" -ConnString $ConnectionString
            Write-Host "   • $tableName ($($count[0].count) registros)" -ForegroundColor White
        } catch {
            Write-Host "   • $tableName (error al contar)" -ForegroundColor Yellow
        }
    }
    
    Write-Host ""
    
    # Exportar cada tabla
    $contador = 0
    $errores = @()
    $resumenTablas = @()
    
    foreach ($tabla in $tablas) {
        $contador++
        $tableName = $tabla.table_name
        $outputFile = Join-Path $OUTPUT_DIR "$tableName.csv"
        
        Write-Host "[$contador/$($tablas.Count)] Exportando '$tableName'..." -ForegroundColor Cyan -NoNewline
        
        try {
            $rowCount = Export-TableToCsv -SchemaName $Schema -TableName $tableName -OutputPath $outputFile -ConnString $ConnectionString
            
            $size = [Math]::Round((Get-Item $outputFile).Length / 1KB, 2)
            Write-Host " ? ($rowCount registros, $size KB)" -ForegroundColor Green
            
            $resumenTablas += [PSCustomObject]@{
                Tabla = $tableName
                Registros = $rowCount
                Estado = "OK"
            }
        }
        catch {
            Write-Host " ? ERROR" -ForegroundColor Red
            Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
            $errores += $tableName
            
            $resumenTablas += [PSCustomObject]@{
                Tabla = $tableName
                Registros = 0
                Estado = "ERROR"
            }
        }
    }
    
    # Crear resumen
    Write-Host "`n?? Generando resumen..." -ForegroundColor Yellow
    
    $resumen = @"
EXPORTACIÓN SCHEMA: $Schema
=============================
Fecha: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Connection: $($ConnectionString.Substring(0, [Math]::Min(50, $ConnectionString.Length)))...
Total de tablas: $($tablas.Count)
Exportadas: $($tablas.Count - $errores.Count)
Errores: $($errores.Count)

TABLAS EXPORTADAS:

"@
    
    foreach ($info in $resumenTablas) {
        $resumen += "$($info.Tabla) : $($info.Registros) registros [$($info.Estado)]`n"
    }
    
    if ($errores.Count -gt 0) {
        $resumen += "`nTABLAS CON ERRORES:`n"
        foreach ($err in $errores) {
            $resumen += "- $err`n"
        }
    }
    
    $resumen | Out-File -FilePath (Join-Path $OUTPUT_DIR "README.txt") -Encoding UTF8
    
    # Resumen final
    Write-Host "`n????????????????????????????????????????????????????????????" -ForegroundColor Green
    Write-Host "?               ? EXPORTACIÓN COMPLETADA ?                ?" -ForegroundColor Green
    Write-Host "????????????????????????????????????????????????????????????`n" -ForegroundColor Green
    
    Write-Host "?? Archivos generados en: $OUTPUT_DIR" -ForegroundColor Cyan
    Write-Host "`nContenido:" -ForegroundColor White
    Get-ChildItem $OUTPUT_DIR | ForEach-Object {
        $size = [Math]::Round($_.Length / 1KB, 2)
        Write-Host "   • $($_.Name) ($size KB)" -ForegroundColor Gray
    }
    
    if ($errores.Count -gt 0) {
        Write-Host "`n??  $($errores.Count) tabla(s) con errores" -ForegroundColor Yellow
    }
    
    Write-Host "`n?? Archivos CSV listos para usar" -ForegroundColor Yellow
    
} catch {
    Write-Host "`n? ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.Exception.StackTrace -ForegroundColor DarkGray
    exit 1
}
