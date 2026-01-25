#!/bin/bash

# ?? Script de verificación rápida - Solo para comprobar que la API arranca
# Uso: ./quick-test.sh

echo "?? Verificación rápida de GestionTime API"
echo "=========================================="

API_URL="https://localhost:2501"

echo "? Esperando 10 segundos para que la API arranque..."
sleep 10

echo ""
echo "?? Probando endpoints básicos..."

# Test 1: Health Check
echo "1?? Health Check:"
if curl -k -s "$API_URL/health" >/dev/null; then
    echo "   ? PASS - API responde"
else
    echo "   ? FAIL - API no responde"
fi

# Test 2: Swagger
echo "2?? Swagger UI:"
if curl -k -s "$API_URL/swagger" >/dev/null; then
    echo "   ? PASS - Swagger disponible"
else
    echo "   ? FAIL - Swagger no disponible"
fi

# Test 3: Login endpoint (estructura)
echo "3?? Login endpoint:"
response=$(curl -k -s -o /dev/null -w "%{http_code}" -X POST "$API_URL/api/v1/auth/login" -H "Content-Type: application/json" -d '{}')
if [ "$response" -eq "400" ] || [ "$response" -eq "401" ]; then
    echo "   ? PASS - Endpoint responde (Status: $response)"
else
    echo "   ? FAIL - Endpoint no funciona (Status: $response)"
fi

echo ""
echo "?? Verificación completada"
echo "=========================="
echo ""
echo "?? Si todo está en verde, puedes acceder a:"
echo "   • API: $API_URL"
echo "   • Swagger: $API_URL/swagger"
echo ""
echo "?? Credenciales de prueba:"
echo "   • admin@gestiontime.local / admin123"
echo "   • psantos@global-retail.com / psantos123"