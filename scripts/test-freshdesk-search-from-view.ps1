# Test del endpoint de búsqueda de tickets desde VIEW
# Endpoint: GET /api/v1/freshdesk/tickets/search-from-view

$baseUrl = "http://localhost:2501"
$username = "psantos@global-retail.com"
$password = "12345678"

Write-Host "🚀 Test de búsqueda de tickets desde VIEW (v_freshdesk_ticket_company_min)" -ForegroundColor Cyan
Write-Host ""

# 1. Login Desktop
Write-Host "1️⃣ Autenticando usuario (Desktop)..." -ForegroundColor Yellow
$loginBody = @{
    email = $username
    password = $password
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/login-desktop" `
        -Method POST `
        -Body $loginBody `
        -ContentType "application/json" `
        -ErrorAction Stop

    $token = $loginResponse.accessToken
    
    if ([string]::IsNullOrEmpty($token)) {
        Write-Host "❌ No se recibió token de acceso" -ForegroundColor Red
        exit 1
    }

    Write-Host "✅ Login exitoso" -ForegroundColor Green
    Write-Host "   Usuario: $($loginResponse.user.email)" -ForegroundColor Gray
    Write-Host "   Token: $($token.Substring(0, [Math]::Min(20, $token.Length)))..." -ForegroundColor Gray
    Write-Host ""
    
    # Headers con token JWT
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }
}
catch {
    Write-Host "❌ Error en login: $_" -ForegroundColor Red
    exit 1
}

# 2. Test sin filtros (todos, limit 10)
Write-Host "2️⃣ Test sin filtros (todos, limit 10)..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/freshdesk/tickets/search-from-view?limit=10" `
        -Method GET `
        -Headers $headers `
        -ErrorAction Stop

    Write-Host "✅ Resultados obtenidos: $($response.count)" -ForegroundColor Green
    
    if ($response.tickets -and $response.tickets.Count -gt 0) {
        Write-Host ""
        Write-Host "📋 Primeros tickets:" -ForegroundColor Cyan
        foreach ($ticket in $response.tickets | Select-Object -First 3) {
            Write-Host "   - Ticket #$($ticket.ticketId) | Customer: $($ticket.customer ?? 'N/A') | Subject: $($ticket.subject)" -ForegroundColor Gray
        }
    }
    Write-Host ""
}
catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
    Write-Host ""
}

# 3. Test con filtro por agentId
Write-Host "3️⃣ Test con filtro por agentId (ejemplo: 48023058107)..." -ForegroundColor Yellow
$agentId = 48023058107
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/freshdesk/tickets/search-from-view?agentId=$agentId&limit=5" `
        -Method GET `
        -Headers $headers `
        -ErrorAction Stop

    Write-Host "✅ Resultados para agentId=$agentId : $($response.count)" -ForegroundColor Green
    
    if ($response.tickets -and $response.tickets.Count -gt 0) {
        Write-Host ""
        Write-Host "📋 Tickets del agente:" -ForegroundColor Cyan
        foreach ($ticket in $response.tickets) {
            Write-Host "   - Ticket #$($ticket.ticketId) | Agent: $($ticket.agentName ?? 'N/A') | Status: $($ticket.status)" -ForegroundColor Gray
        }
    }
    Write-Host ""
}
catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
    Write-Host ""
}

# 4. Test con filtro por ticket ID (prefijo)
Write-Host "4️⃣ Test con filtro por ticket ID (prefijo '550')..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/freshdesk/tickets/search-from-view?ticket=550&limit=5" `
        -Method GET `
        -Headers $headers `
        -ErrorAction Stop

    Write-Host "✅ Resultados para ticket='550': $($response.count)" -ForegroundColor Green
    
    if ($response.tickets -and $response.tickets.Count -gt 0) {
        Write-Host ""
        Write-Host "📋 Tickets encontrados:" -ForegroundColor Cyan
        foreach ($ticket in $response.tickets) {
            Write-Host "   - Ticket #$($ticket.ticketId) | Customer: $($ticket.customer ?? 'N/A')" -ForegroundColor Gray
        }
    }
    Write-Host ""
}
catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
    Write-Host ""
}

# 5. Test con filtro por customer (nombre parcial)
Write-Host "5️⃣ Test con filtro por customer ('Kanali')..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/freshdesk/tickets/search-from-view?customer=Kanali&limit=5" `
        -Method GET `
        -Headers $headers `
        -ErrorAction Stop

    Write-Host "✅ Resultados para customer='Kanali': $($response.count)" -ForegroundColor Green
    
    if ($response.tickets -and $response.tickets.Count -gt 0) {
        Write-Host ""
        Write-Host "📋 Tickets encontrados:" -ForegroundColor Cyan
        foreach ($ticket in $response.tickets) {
            Write-Host "   - Ticket #$($ticket.ticketId) | Customer: $($ticket.customer ?? 'N/A') | Subject: $($ticket.subject)" -ForegroundColor Gray
        }
    }
    Write-Host ""
}
catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
    Write-Host ""
}

# 6. Test con múltiples filtros combinados
Write-Host "6️⃣ Test con filtros combinados (agentId + customer)..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/freshdesk/tickets/search-from-view?agentId=$agentId&customer=Kanali&limit=5" `
        -Method GET `
        -Headers $headers `
        -ErrorAction Stop

    Write-Host "✅ Resultados con filtros combinados: $($response.count)" -ForegroundColor Green
    
    if ($response.tickets -and $response.tickets.Count -gt 0) {
        Write-Host ""
        Write-Host "📋 Tickets encontrados:" -ForegroundColor Cyan
        foreach ($ticket in $response.tickets) {
            Write-Host "   - Ticket #$($ticket.ticketId) | Customer: $($ticket.customer ?? 'N/A') | Agent: $($ticket.agentName ?? 'N/A')" -ForegroundColor Gray
        }
    }
    Write-Host ""
}
catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
    Write-Host ""
}

# 7. Test con limit alto (50)
Write-Host "7️⃣ Test con limit máximo (50)..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/freshdesk/tickets/search-from-view?limit=50" `
        -Method GET `
        -Headers $headers `
        -ErrorAction Stop

    Write-Host "✅ Resultados con limit=50: $($response.count)" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
    Write-Host ""
}

Write-Host "🎉 Tests completados" -ForegroundColor Green
Write-Host ""
Write-Host "ℹ️ NOTAS:" -ForegroundColor Cyan
Write-Host "   - Este endpoint usa SOLO la vista PostgreSQL (no llama a Freshdesk API)" -ForegroundColor Gray
Write-Host "   - La vista debe estar sincronizada previamente con: POST /api/v1/integrations/freshdesk/sync/ticket-headers" -ForegroundColor Gray
Write-Host "   - Filtros soportados: agentId, ticket (prefijo), customer (ILIKE)" -ForegroundColor Gray
Write-Host "   - Limit: default 10, max 50" -ForegroundColor Gray
