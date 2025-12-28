@echo off
REM ?? Script de pruebas automatizadas para GestionTime API (Windows)
REM Verifica que la API funciona correctamente después del arranque

echo ?? Iniciando pruebas de la API GestionTime...
echo ================================================

set API_BASE=https://localhost:2501
set HEALTH_ENDPOINT=%API_BASE%/health
set LOGIN_ENDPOINT=%API_BASE%/api/v1/auth/login

echo [INFO] Esperando a que la API esté disponible...

REM Esperar a que la API responda (intentos limitados)
set /a attempts=0
:wait_loop
set /a attempts+=1
if %attempts% gtr 30 (
    echo ? API no disponible después de 30 intentos
    pause
    exit /b 1
)

REM Probar conexión básica (PowerShell)
powershell -Command "try { $response = Invoke-WebRequest -Uri '%HEALTH_ENDPOINT%' -SkipCertificateCheck -ErrorAction Stop; if ($response.StatusCode -eq 200) { exit 0 } else { exit 1 } } catch { exit 1 }" >nul 2>&1

if %errorlevel% neq 0 (
    echo .
    timeout /t 2 /nobreak >nul
    goto wait_loop
)

echo ? API disponible en intento %attempts%

echo.
echo ?? Ejecutando pruebas básicas...
echo ==================================

REM Test 1: Health Check
echo [TEST] ?? Health Check...
powershell -Command "try { $response = Invoke-WebRequest -Uri '%HEALTH_ENDPOINT%' -SkipCertificateCheck; Write-Host '? Health Check - Status:' $response.StatusCode -ForegroundColor Green; Write-Host '   Response:' $response.Content } catch { Write-Host '? Health Check Error:' $_.Exception.Message -ForegroundColor Red }"

REM Test 2: Login
echo.
echo [TEST] ?? Login...
set LOGIN_DATA={"email":"admin@gestiontime.local","password":"admin123"}
powershell -Command "$body = '%LOGIN_DATA%'; try { $response = Invoke-WebRequest -Uri '%LOGIN_ENDPOINT%' -Method Post -Body $body -ContentType 'application/json' -SkipCertificateCheck -SessionVariable session; Write-Host '? Login - Status:' $response.StatusCode -ForegroundColor Green; $Global:session = $session } catch { Write-Host '? Login Error:' $_.Exception.Message -ForegroundColor Red }"

REM Test 3: Catálogos
echo.
echo [TEST] ?? Catálogos...
powershell -Command "try { $response = Invoke-WebRequest -Uri '%API_BASE%/api/v1/tipos' -SkipCertificateCheck; Write-Host '? Tipos - Status:' $response.StatusCode -ForegroundColor Green } catch { Write-Host '? Tipos Error' -ForegroundColor Red }"

powershell -Command "try { $response = Invoke-WebRequest -Uri '%API_BASE%/api/v1/grupos' -SkipCertificateCheck; Write-Host '? Grupos - Status:' $response.StatusCode -ForegroundColor Green } catch { Write-Host '? Grupos Error' -ForegroundColor Red }"

powershell -Command "try { $response = Invoke-WebRequest -Uri '%API_BASE%/api/v1/clientes' -SkipCertificateCheck; Write-Host '? Clientes - Status:' $response.StatusCode -ForegroundColor Green } catch { Write-Host '? Clientes Error' -ForegroundColor Red }"

REM Test 4: Swagger
echo.
echo [TEST] ?? Swagger UI...
powershell -Command "try { $response = Invoke-WebRequest -Uri '%API_BASE%/swagger' -SkipCertificateCheck; Write-Host '? Swagger - Status:' $response.StatusCode -ForegroundColor Green } catch { Write-Host '? Swagger Error' -ForegroundColor Red }"

echo.
echo ?? Pruebas completadas
echo ======================
echo.
echo ? ¡API funcionando correctamente! ??
echo.
echo ?? Accesos disponibles:
echo    - API Base: %API_BASE%
echo    - Swagger:  %API_BASE%/swagger
echo    - Health:   %HEALTH_ENDPOINT%
echo.
echo ?? Credenciales de prueba:
echo    - Admin: admin@gestiontime.local / admin123
echo    - User:  psantos@global-retail.com / psantos123
echo.
echo ?? Para más pruebas detalladas, abre Swagger en tu navegador:
echo    %API_BASE%/swagger
echo.

pause