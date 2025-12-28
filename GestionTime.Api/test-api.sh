#!/bin/bash

# ?? Script de pruebas automatizadas para GestionTime API
# Verifica que la API funciona correctamente después del arranque

echo "?? Iniciando pruebas de la API GestionTime..."
echo "================================================"

# Variables
API_BASE="https://localhost:2501"
HEALTH_ENDPOINT="$API_BASE/health"
LOGIN_ENDPOINT="$API_BASE/api/v1/auth/login"
PARTES_ENDPOINT="$API_BASE/api/v1/partes"

# Colores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Función para logging
log() {
    echo -e "${BLUE}[$(date '+%H:%M:%S')]${NC} $1"
}

success() {
    echo -e "${GREEN}? $1${NC}"
}

error() {
    echo -e "${RED}? $1${NC}"
}

warning() {
    echo -e "${YELLOW}??  $1${NC}"
}

# Verificar que la API está corriendo
wait_for_api() {
    log "Esperando a que la API esté disponible..."
    local max_attempts=30
    local attempt=1
    
    while [ $attempt -le $max_attempts ]; do
        if curl -s -k "$HEALTH_ENDPOINT" >/dev/null 2>&1; then
            success "API disponible en intento $attempt"
            return 0
        fi
        
        echo -n "."
        sleep 2
        ((attempt++))
    done
    
    error "API no disponible después de $max_attempts intentos"
    return 1
}

# Test 1: Health Check
test_health() {
    log "?? Probando Health Check..."
    
    response=$(curl -s -k -w "HTTPSTATUS:%{http_code}" "$HEALTH_ENDPOINT" 2>/dev/null)
    http_code=$(echo "$response" | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')
    body=$(echo "$response" | sed -e 's/HTTPSTATUS:.*//g')
    
    if [ "$http_code" = "200" ]; then
        success "Health Check - Status: $http_code"
        echo "   Response: $body"
        return 0
    else
        error "Health Check - Status: $http_code"
        return 1
    fi
}

# Test 2: Login
test_login() {
    log "?? Probando Login..."
    
    local login_data='{"email":"admin@gestiontime.local","password":"admin123"}'
    
    response=$(curl -s -k -w "HTTPSTATUS:%{http_code}" \
        -X POST "$LOGIN_ENDPOINT" \
        -H "Content-Type: application/json" \
        -d "$login_data" \
        -c cookies.txt 2>/dev/null)
    
    http_code=$(echo "$response" | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')
    body=$(echo "$response" | sed -e 's/HTTPSTATUS:.*//g')
    
    if [ "$http_code" = "200" ]; then
        success "Login - Status: $http_code"
        echo "   Response: $body"
        return 0
    else
        error "Login - Status: $http_code"
        echo "   Response: $body"
        return 1
    fi
}

# Test 3: Catálogos (sin autenticación)
test_catalogs() {
    log "?? Probando Catálogos..."
    
    # Test Tipos
    response=$(curl -s -k -w "HTTPSTATUS:%{http_code}" "$API_BASE/api/v1/tipos" 2>/dev/null)
    http_code=$(echo "$response" | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')
    
    if [ "$http_code" = "200" ]; then
        success "Tipos - Status: $http_code"
    else
        error "Tipos - Status: $http_code"
    fi
    
    # Test Grupos
    response=$(curl -s -k -w "HTTPSTATUS:%{http_code}" "$API_BASE/api/v1/grupos" 2>/dev/null)
    http_code=$(echo "$response" | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')
    
    if [ "$http_code" = "200" ]; then
        success "Grupos - Status: $http_code"
    else
        error "Grupos - Status: $http_code"
    fi
    
    # Test Clientes
    response=$(curl -s -k -w "HTTPSTATUS:%{http_code}" "$API_BASE/api/v1/clientes" 2>/dev/null)
    http_code=$(echo "$response" | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')
    
    if [ "$http_code" = "200" ]; then
        success "Clientes - Status: $http_code"
        return 0
    else
        error "Clientes - Status: $http_code"
        return 1
    fi
}

# Test 4: Endpoint protegido (con autenticación)
test_authenticated_endpoint() {
    log "?? Probando endpoint protegido (Partes)..."
    
    if [ ! -f cookies.txt ]; then
        warning "No hay cookies de login. Saltando test autenticado."
        return 1
    fi
    
    response=$(curl -s -k -w "HTTPSTATUS:%{http_code}" \
        "$PARTES_ENDPOINT" \
        -b cookies.txt 2>/dev/null)
    
    http_code=$(echo "$response" | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')
    body=$(echo "$response" | sed -e 's/HTTPSTATUS:.*//g')
    
    if [ "$http_code" = "200" ]; then
        success "Partes (autenticado) - Status: $http_code"
        return 0
    else
        warning "Partes (autenticado) - Status: $http_code"
        return 1
    fi
}

# Test 5: Swagger UI disponible
test_swagger() {
    log "?? Probando Swagger UI..."
    
    response=$(curl -s -k -w "HTTPSTATUS:%{http_code}" "$API_BASE/swagger" 2>/dev/null)
    http_code=$(echo "$response" | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')
    
    if [ "$http_code" = "200" ] || [ "$http_code" = "301" ]; then
        success "Swagger UI - Status: $http_code"
        return 0
    else
        error "Swagger UI - Status: $http_code"
        return 1
    fi
}

# Ejecutar todas las pruebas
run_all_tests() {
    local failed_tests=0
    
    # Esperar a que la API esté disponible
    if ! wait_for_api; then
        error "No se puede continuar. API no disponible."
        exit 1
    fi
    
    echo ""
    echo "?? Ejecutando batería de pruebas..."
    echo "=================================="
    
    # Test 1: Health Check
    test_health || ((failed_tests++))
    
    # Test 2: Login
    test_login || ((failed_tests++))
    
    # Test 3: Catálogos
    test_catalogs || ((failed_tests++))
    
    # Test 4: Endpoint autenticado
    test_authenticated_endpoint || ((failed_tests++))
    
    # Test 5: Swagger
    test_swagger || ((failed_tests++))
    
    # Cleanup
    rm -f cookies.txt 2>/dev/null
    
    echo ""
    echo "?? Resultados de las pruebas:"
    echo "=============================="
    
    if [ $failed_tests -eq 0 ]; then
        success "¡Todas las pruebas pasaron! ??"
        success "La API está funcionando correctamente"
        echo ""
        echo "?? Accesos disponibles:"
        echo "   - API Base: $API_BASE"
        echo "   - Swagger:  $API_BASE/swagger"
        echo "   - Health:   $HEALTH_ENDPOINT"
        echo ""
        echo "?? Credenciales de prueba:"
        echo "   - Admin: admin@gestiontime.local / admin123"
        echo "   - User:  psantos@global-retail.com / psantos123"
        return 0
    else
        error "$failed_tests prueba(s) fallaron"
        warning "Revisa los logs de la API para más detalles"
        return 1
    fi
}

# Función principal
main() {
    if [ "$1" = "--help" ] || [ "$1" = "-h" ]; then
        echo "?? Script de pruebas GestionTime API"
        echo ""
        echo "Uso: $0 [opción]"
        echo ""
        echo "Opciones:"
        echo "  --help, -h     Mostrar esta ayuda"
        echo "  --health       Solo probar health check"
        echo "  --login        Solo probar login"
        echo ""
        echo "Sin parámetros: Ejecutar todas las pruebas"
        exit 0
    elif [ "$1" = "--health" ]; then
        wait_for_api && test_health
    elif [ "$1" = "--login" ]; then
        wait_for_api && test_login
    else
        run_all_tests
    fi
}

# Ejecutar
main "$@"