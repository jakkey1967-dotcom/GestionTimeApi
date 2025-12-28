# ?? GUÍA RÁPIDA: Implementar Registro con Verificación de Email

## ? Archivos Ya Creados Automáticamente

### Backend (API) - ? Listos
- ? `RegisterRequest.cs` - DTO solicitud de registro
- ? `RegisterResponse.cs` - DTO respuesta
- ? `VerifyEmailRequest.cs` - DTO verificación de email
- ? `IEmailService.cs` - Actualizado con método de registro
- ? `FakeEmailService.cs` - Actualizado con email de registro
- ? `ResetTokenService.cs` - Actualizado con métodos adicionales

---

## ?? SOLO TE FALTA ESTO (2 minutos):

### Paso 1: Agregar Endpoints al AuthController

1. **Detén la API** (si está corriendo):
```powershell
Get-Process | Where-Object { $_.ProcessName -like "*GestionTime.Api*" } | Stop-Process -Force
```

2. **Abre:**
```
C:\GestionTime\src\GestionTime.Api\Controllers\AuthController.cs
```

3. **Ve al final del archivo** (después del último endpoint, antes del cierre `}`)

4. **Copia TODO el código de:**
```
C:\GestionTime\src\GestionTime.Api\docs\ENDPOINTS_REGISTRO_FINAL.txt
```

5. **Pégalo antes del cierre de la clase**

6. **Guarda** (`Ctrl + S`)

---

### Paso 2: Compilar y Ejecutar

```powershell
cd C:\GestionTime\src\GestionTime.Api
dotnet build
dotnet run
```

---

### Paso 3: Probar en Swagger

Abre: `https://localhost:2501/swagger`

#### Test 1: Solicitar Código de Registro

**POST `/api/v1/auth/register`**

Body:
```json
{
  "email": "nuevo@example.com",
  "fullName": "Usuario Nuevo",
  "password": "password123",
  "empresa": "Mi Empresa"
}
```

**Esperado:**
- Status: 200 OK
- Response:
```json
{
  "success": true,
  "message": "Código enviado a tu correo.",
  "error": null
}
```
- **En la consola de la API verás:**
```
========================
FAKE EMAIL - REGISTRO
Para: nuevo@example.com
Código: 456789
========================
```

#### Test 2: Verificar Email y Crear Usuario

**POST `/api/v1/auth/verify-email`**

Body (usa el código que apareció en la consola):
```json
{
  "email": "nuevo@example.com",
  "token": "456789",
  "fullName": "Usuario Nuevo",
  "password": "password123",
  "empresa": "Mi Empresa"
}
```

**Esperado:**
- Status: 200 OK
- Response:
```json
{
  "success": true,
  "message": "Registro exitoso. Ya puedes iniciar sesión.",
  "error": null
}
```

#### Test 3: Hacer Login con el Nuevo Usuario

**POST `/api/v1/auth/login`**

Body:
```json
{
  "email": "nuevo@example.com",
  "password": "password123"
}
```

**Esperado:**
- Status: 200 OK
- Usuario logueado con rol "User"

---

## ?? Flujo Completo

```
1. Usuario ingresa datos en RegisterPage
   ?
2. POST /api/v1/auth/register
   - Valida email no existe
   - Genera código de 6 dígitos
   - Guarda datos temporales en caché (1 hora)
   - Envía email con código
   ?
3. Usuario recibe email con código
   ?
4. Usuario ingresa código en la app
   ?
5. POST /api/v1/auth/verify-email
   - Valida código
   - Crea usuario en DB
   - Asigna rol "User"
   - Elimina datos temporales
   ?
6. Usuario puede hacer login
```

---

## ?? Características de Seguridad

? Email único (valida que no exista)  
? Código de 6 dígitos aleatorio  
? Expiración de 1 hora  
? Un solo uso (se elimina después de usarlo)  
? Contraseña hasheada con BCrypt  
? Rol "User" asignado automáticamente  
? Validación de email formato correcto  

---

## ?? Validaciones Implementadas

### Backend
| Validación | Endpoint | Respuesta |
|------------|----------|-----------|
| Email ya existe | `register` | 400 Bad Request |
| Código inválido | `verify-email` | 400 Bad Request |
| Código expirado | `verify-email` | 400 Bad Request |
| Email no coincide | `verify-email` | 400 Bad Request |

### Frontend (Desktop) - Próximo Paso
| Validación | Mensaje |
|------------|---------|
| Email vacío | "Ingrese su email" |
| Email inválido | "Email no válido" |
| Contraseña vacía | "Ingrese contraseña" |
| Contraseña < 6 chars | "Mínimo 6 caracteres" |
| Contraseñas no coinciden | "Las contraseñas no coinciden" |
| Código vacío | "Ingrese el código" |
| Código != 6 dígitos | "Código de 6 dígitos" |

---

## ?? Próximo Paso: Frontend (Desktop)

Necesito actualizar `RegisterPage.xaml` y `RegisterPage.xaml.cs` para implementar el flujo de 2 pasos similar al forgot-password.

**¿Quieres que continúe con la implementación del frontend ahora?**

Responde:
- **A** = Sí, implementa el frontend del registro
- **B** = Primero quiero probar el backend
- **C** = Tengo un problema con el backend

---

## ?? Estado Actual

| Componente | Estado |
|-----------|--------|
| DTOs (API) | ? Creados |
| Email Service | ? Actualizado |
| Token Service | ? Actualizado |
| Endpoints | ? Falta copiar al AuthController |
| Compilación | ? Pendiente |
| Swagger Test | ? Pendiente |
| Frontend | ? Pendiente |

---

**Tiempo estimado:** 2 minutos para completar el backend + 10 minutos para el frontend.
