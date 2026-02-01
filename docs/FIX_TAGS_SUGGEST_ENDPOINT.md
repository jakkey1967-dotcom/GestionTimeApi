# FIX: Tags suggest no muestra sugerencias

**Fecha**: 2026-01-31  
**Problema**: El AutoSuggestBox del Desktop no muestra sugerencias de tags  
**Causa**: El Desktop esperaba `List<string>` pero el backend devuelve `{ success, count, tags: [] }`  
**Solución**: Agregar DTO `TagSuggestResponse` en el Desktop

---

## 🔴 PROBLEMA

Al escribir en el AutoSuggestBox de tags en `ParteItemEdit`, **NO aparecen sugerencias** aunque el endpoint `/api/v1/freshdesk/tags/suggest` funciona correctamente en Swagger.

**Causa raíz**:
- El backend devuelve: `{ "success": true, "count": 3, "tags": ["tag1", "tag2", "tag3"] }`
- El Desktop intentaba deserializar como: `List<string>` directamente
- Esto causaba error de parsing y las sugerencias quedaban vacías

---

## ✅ SOLUCIÓN (SOLO DESKTOP)

### ⚠️ NO TOCAR EL BACKEND - Ya funciona correctamente

El endpoint en Swagger devuelve el formato correcto. El problema está en el Desktop.

### Archivo: `Views/ParteItemEdit.xaml.cs`

#### 1. Agregar DTO para la respuesta (antes de `ParteRequest`):

```csharp
/// <summary>
/// Response DTO para /api/v1/freshdesk/tags/suggest
/// </summary>
private sealed class TagSuggestResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
}
```

#### 2. Modificar `SearchTagSuggestionsAsync` (línea ~2080):

**ANTES** ❌:
```csharp
// Llamar endpoint de sugerencias
var endpoint = $"/api/v1/freshdesk/tags/suggest?term={Uri.EscapeDataString(query)}&limit=10";
var suggestions = await App.Api.GetAsync<List<string>>(endpoint, ct);

if (suggestions != null && suggestions.Any())
{
    _tagSuggestions.Clear();
    _tagSuggestions.AddRange(suggestions);
    TxtTagInput.ItemsSource = _tagSuggestions;
    
    App.Log?.LogDebug("✅ {count} sugerencias de tags encontradas", suggestions.Count);
}
```

**DESPUÉS** ✅:
```csharp
// Llamar endpoint de sugerencias
var endpoint = $"/api/v1/freshdesk/tags/suggest?term={Uri.EscapeDataString(query)}&limit=10";

// 🔧 FIX: El endpoint devuelve { success, count, tags: [] }
// NO es un List<string> directo, es un objeto con propiedad "tags"
var response = await App.Api.GetAsync<TagSuggestResponse>(endpoint, ct);

if (response?.Tags != null && response.Tags.Any())
{
    _tagSuggestions.Clear();
    _tagSuggestions.AddRange(response.Tags);
    TxtTagInput.ItemsSource = _tagSuggestions;
    
    App.Log?.LogDebug("✅ {count} sugerencias de tags encontradas", response.Tags.Count);
}
```

---

## 📊 COMPORTAMIENTO ESPERADO

### Swagger (ya funciona):
```
GET /api/v1/freshdesk/tags/suggest?term=t&limit=10
```
**Respuesta**:
```json
{
  "success": true,
  "count": 3,
  "tags": [
    "Testing",
    "Troubleshooting",
    "Training"
  ]
}
```

### Desktop (ahora debe funcionar):
1. Escribir "te" en AutoSuggestBox de tags
2. Esperar 300ms (debounce)
3. Llamada al endpoint
4. **Muestra sugerencias**: "Testing", "Troubleshooting"
5. Seleccionar o presionar Enter → Tag agregado como chip

---

## ✅ CHECKLIST

- [x] Archivo modificado: `ParteItemEdit.xaml.cs`
- [x] DTO `TagSuggestResponse` agregado
- [x] Método `SearchTagSuggestionsAsync` actualizado
- [x] Compilación exitosa ✅
- [ ] **Pendiente**: Ejecutar Desktop y probar

---

## 🧪 PRUEBA

1. **Ejecutar backend** (si no está corriendo):
   ```powershell
   cd C:\GestionTime\GestionTimeApi
   dotnet run --project GestionTime.Api
   ```

2. **Ejecutar Desktop**

3. **Abrir ParteItemEdit** (crear o editar parte)

4. **En el campo "TAGS / ETIQUETAS"**:
   - Escribir "te"
   - Esperar 300ms
   - Debe aparecer dropdown con sugerencias

5. **Verificar logs del Desktop** (`logs/app.log`):
   ```
   🔍 Buscando tags: 'te'...
   ✅ 2 sugerencias de tags encontradas
   ```

---

**Fin del documento**
