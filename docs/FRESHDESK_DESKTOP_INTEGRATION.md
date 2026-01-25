# 📱 INTEGRACIÓN FRESHDESK - GUÍA PARA GESTIONTIME DESKTOP

## 📋 ÍNDICE
1. [Resumen Ejecutivo](#resumen)
2. [Endpoints Disponibles](#endpoints)
3. [Autenticación](#autenticacion)
4. [Cliente HTTP para Desktop](#cliente-http)
5. [Modelos de Datos (DTOs)](#modelos)
6. [Implementación de Servicios](#servicios)
7. [UI/UX Recomendaciones](#ui-ux)
8. [Flujos de Trabajo](#flujos)
9. [Manejo de Errores](#errores)
10. [Testing](#testing)
11. [Ejemplos Completos](#ejemplos)

---

## 📌 RESUMEN EJECUTIVO {#resumen}

### ✅ Funcionalidades Disponibles desde Desktop

La API de GestionTime ahora expone endpoints de Freshdesk que permiten:

- ✅ **Verificar conexión** con Freshdesk (sin login)
- ✅ **Buscar tickets** del usuario (con autocompletado)
- ✅ **Buscar tags** para categorizar partes de trabajo
- ✅ **Sincronizar tags** manualmente desde Freshdesk

### 🎯 Casos de Uso Principales

1. **Al crear un parte de trabajo**: Buscar ticket de Freshdesk y asociarlo
2. **Autocompletado de tags**: Sugerir tags mientras el usuario escribe
3. **Validación de tickets**: Verificar que un ID de ticket existe
4. **Sincronización manual**: Permitir al usuario actualizar el catálogo de tags

---

## 🌐 ENDPOINTS DISPONIBLES {#endpoints}

### Base URL

```csharp
// Desarrollo local
private const string BaseUrl = "https://localhost:2502";

// Producción (Render)
private const string BaseUrl = "https://gestiontime-api.onrender.com";
```

---

### 1. **GET `/api/v1/freshdesk/ping`** - Verificar Conexión

**Auth**: NO requiere (público)

**Uso**: Verificar que la configuración de Freshdesk en el servidor está correcta.

**Request**:
```http
GET /api/v1/freshdesk/ping
```

**Response** (200 OK):
```json
{
  "ok": true,
  "status": 200,
  "message": "✅ Conexión exitosa con Freshdesk",
  "agent": "support@alterasoftware.com",
  "timestamp": "2026-01-24T21:00:00Z"
}
```

**Cuándo usarlo**: En el startup de la aplicación Desktop o en una pantalla de configuración/diagnóstico.

---

### 2. **GET `/api/v1/freshdesk/tickets/suggest`** - Buscar Tickets

**Auth**: SÍ requiere (JWT en cookie)

**Uso**: Autocompletado de tickets mientras el usuario escribe.

**Parámetros**:
| Parámetro | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `term` | string | null | Término de búsqueda (ID o texto) |
| `limit` | int | 10 | Número máximo de resultados |
| `includeUnassigned` | bool | true | Incluir tickets sin asignar |

**Request**:
```http
GET /api/v1/freshdesk/tickets/suggest?term=55950&limit=10&includeUnassigned=true
Cookie: access_token={jwt_token}
```

**Response** (200 OK):
```json
{
  "success": true,
  "count": 5,
  "tickets": [
    {
      "id": 55950,
      "subject": "20 Dolores caja 1 distinto Menu",
      "status": 2,
      "statusName": "Open",
      "priority": 1,
      "priorityName": "Low",
      "updatedAt": "2026-01-20T16:00:36Z"
    }
  ]
}
```

**Cuándo usarlo**: 
- ComboBox con autocompletado en "Crear Parte de Trabajo"
- TextBox con sugerencias (como Google)
- Validación de ID de ticket

---

### 3. **GET `/api/v1/freshdesk/tags/suggest`** - Buscar Tags

**Auth**: NO requiere (público)

**Uso**: Autocompletado de tags para categorizar partes de trabajo.

**Parámetros**:
| Parámetro | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `term` | string | null | Prefijo para filtrar (case-insensitive) |
| `limit` | int | 20 | Número máximo de resultados |

**Request**:
```http
GET /api/v1/freshdesk/tags/suggest?term=tpv&limit=10
```

**Response** (200 OK):
```json
{
  "success": true,
  "count": 4,
  "tags": [
    "tpv hw",
    "tpv",
    "tpv lenta",
    "tpv software"
  ]
}
```

**Cuándo usarlo**:
- ComboBox editable con autocompletado
- Chips/Tags input con sugerencias
- Filtros de búsqueda

---

### 4. **POST `/api/v1/freshdesk/tags/sync`** - Sincronizar Tags

**Auth**: SÍ requiere (JWT en cookie)

**Uso**: Actualizar manualmente el catálogo de tags desde Freshdesk.

**Parámetros**:
| Parámetro | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `mode` | string | "recent" | "recent" (últimos N días) o "full" |
| `days` | int | 30 | Días hacia atrás (1-365) |
| `limit` | int | 1000 | Máximo de tickets a procesar |

**Request**:
```http
POST /api/v1/freshdesk/tags/sync?mode=recent&days=30&limit=1000
Cookie: access_token={jwt_token}
```

**Response** (200 OK):
```json
{
  "success": true,
  "message": "✅ Sincronización completada en 15234ms",
  "metrics": {
    "ticketsScanned": 300,
    "tagsFound": 87,
    "inserted": 12,
    "updated": 75,
    "durationMs": 15234
  }
}
```

**Cuándo usarlo**:
- Botón "Actualizar Tags" en configuración
- Sincronización al iniciar la app (opcional)
- Cuando el usuario reporta que faltan tags

---

## 🔐 AUTENTICACIÓN {#autenticacion}

### Flujo de Autenticación

La API usa **JWT almacenado en cookies HttpOnly**. El Desktop debe:

1. **Login**: `POST /api/v1/auth/login`
2. **Guardar cookie**: El `HttpClient` debe configurarse para mantener cookies
3. **Usar cookie**: Todos los requests subsecuentes incluyen la cookie automáticamente

### Configuración del HttpClient

```csharp
// En el constructor del servicio
private readonly HttpClient _httpClient;
private readonly CookieContainer _cookies;

public FreshdeskService(HttpClient httpClient)
{
    _cookies = new CookieContainer();
    var handler = new HttpClientHandler
    {
        CookieContainer = _cookies,
        UseCookies = true
    };
    
    _httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri("https://localhost:2502")
    };
}
```

### Verificar Autenticación

```csharp
public bool IsAuthenticated()
{
    var baseUri = _httpClient.BaseAddress;
    var cookies = _cookies.GetCookies(baseUri);
    
    return cookies["access_token"] != null;
}
```

---

## 💻 CLIENTE HTTP PARA DESKTOP {#cliente-http}

### Clase Base: `FreshdeskApiClient.cs`

```csharp
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GestionTime.Desktop.Services
{
    public class FreshdeskApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly CookieContainer _cookies;
        private const string BaseUrl = "https://localhost:2502";

        public FreshdeskApiClient()
        {
            _cookies = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = _cookies,
                UseCookies = true,
                // Ignorar errores SSL en desarrollo
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        #region Freshdesk Endpoints

        /// <summary>
        /// Verifica la conexión con Freshdesk (NO requiere autenticación)
        /// </summary>
        public async Task<FreshdeskPingResponse> PingAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/v1/freshdesk/ping");
                response.EnsureSuccessStatusCode();
                
                return await response.Content.ReadFromJsonAsync<FreshdeskPingResponse>();
            }
            catch (Exception ex)
            {
                throw new FreshdeskApiException("Error al verificar conexión con Freshdesk", ex);
            }
        }

        /// <summary>
        /// Busca tickets del usuario (REQUIERE autenticación)
        /// </summary>
        public async Task<FreshdeskTicketsResponse> SearchTicketsAsync(
            string term = null,
            int limit = 10,
            bool includeUnassigned = true)
        {
            try
            {
                var url = $"/api/v1/freshdesk/tickets/suggest?limit={limit}&includeUnassigned={includeUnassigned}";
                if (!string.IsNullOrEmpty(term))
                {
                    url += $"&term={Uri.EscapeDataString(term)}";
                }

                var response = await _httpClient.GetAsync(url);
                
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("No autenticado. Debe hacer login primero.");
                }
                
                response.EnsureSuccessStatusCode();
                
                return await response.Content.ReadFromJsonAsync<FreshdeskTicketsResponse>();
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FreshdeskApiException("Error al buscar tickets", ex);
            }
        }

        /// <summary>
        /// Busca tags (NO requiere autenticación)
        /// </summary>
        public async Task<FreshdeskTagsResponse> SearchTagsAsync(string term = null, int limit = 20)
        {
            try
            {
                var url = $"/api/v1/freshdesk/tags/suggest?limit={limit}";
                if (!string.IsNullOrEmpty(term))
                {
                    url += $"&term={Uri.EscapeDataString(term)}";
                }

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                return await response.Content.ReadFromJsonAsync<FreshdeskTagsResponse>();
            }
            catch (Exception ex)
            {
                throw new FreshdeskApiException("Error al buscar tags", ex);
            }
        }

        /// <summary>
        /// Sincroniza tags desde Freshdesk (REQUIERE autenticación)
        /// </summary>
        public async Task<FreshdeskSyncResponse> SyncTagsAsync(
            string mode = "recent",
            int days = 30,
            int limit = 1000)
        {
            try
            {
                var url = $"/api/v1/freshdesk/tags/sync?mode={mode}&days={days}&limit={limit}";
                var response = await _httpClient.PostAsync(url, null);
                
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("No autenticado. Debe hacer login primero.");
                }
                
                response.EnsureSuccessStatusCode();
                
                return await response.Content.ReadFromJsonAsync<FreshdeskSyncResponse>();
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FreshdeskApiException("Error al sincronizar tags", ex);
            }
        }

        #endregion

        #region Helper Methods

        public bool IsAuthenticated()
        {
            var baseUri = _httpClient.BaseAddress;
            var cookies = _cookies.GetCookies(baseUri);
            return cookies["access_token"] != null;
        }

        public void ClearAuthentication()
        {
            _cookies = new CookieContainer();
        }

        #endregion
    }

    #region Custom Exceptions

    public class FreshdeskApiException : Exception
    {
        public FreshdeskApiException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }

    #endregion
}
```

---

## 📦 MODELOS DE DATOS (DTOs) {#modelos}

### `FreshdeskModels.cs`

```csharp
using System;
using System.Collections.Generic;

namespace GestionTime.Desktop.Models
{
    #region Ping Response

    public class FreshdeskPingResponse
    {
        public bool Ok { get; set; }
        public int Status { get; set; }
        public string Message { get; set; }
        public string Agent { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion

    #region Tickets

    public class FreshdeskTicketsResponse
    {
        public bool Success { get; set; }
        public int Count { get; set; }
        public List<FreshdeskTicket> Tickets { get; set; } = new();
    }

    public class FreshdeskTicket
    {
        public long Id { get; set; }
        public string Subject { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; }
        public int Priority { get; set; }
        public string PriorityName { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Para mostrar en UI
        public override string ToString() => $"#{Id} - {Subject}";
    }

    #endregion

    #region Tags

    public class FreshdeskTagsResponse
    {
        public bool Success { get; set; }
        public int Count { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    #endregion

    #region Sync Response

    public class FreshdeskSyncResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public FreshdeskSyncMetrics Metrics { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class FreshdeskSyncMetrics
    {
        public int TicketsScanned { get; set; }
        public int TagsFound { get; set; }
        public int Inserted { get; set; }
        public int Updated { get; set; }
        public long DurationMs { get; set; }
    }

    #endregion
}
```

---

## 🏗️ IMPLEMENTACIÓN DE SERVICIOS {#servicios}

### Service Layer: `FreshdeskService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GestionTime.Desktop.Models;

namespace GestionTime.Desktop.Services
{
    public class FreshdeskService
    {
        private readonly FreshdeskApiClient _apiClient;

        public FreshdeskService(FreshdeskApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        #region Public Methods

        /// <summary>
        /// Verifica que Freshdesk esté configurado correctamente
        /// </summary>
        public async Task<bool> VerifyConnectionAsync()
        {
            try
            {
                var result = await _apiClient.PingAsync();
                return result.Ok && result.Status == 200;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Busca tickets por término (para autocompletado)
        /// </summary>
        public async Task<List<FreshdeskTicket>> SearchTicketsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<FreshdeskTicket>();

            try
            {
                var response = await _apiClient.SearchTicketsAsync(
                    term: searchTerm,
                    limit: 10,
                    includeUnassigned: true);

                return response.Success ? response.Tickets : new List<FreshdeskTicket>();
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Propagar para manejar en UI
            }
            catch
            {
                return new List<FreshdeskTicket>();
            }
        }

        /// <summary>
        /// Valida que un ID de ticket existe
        /// </summary>
        public async Task<FreshdeskTicket> GetTicketByIdAsync(long ticketId)
        {
            try
            {
                var response = await _apiClient.SearchTicketsAsync(
                    term: ticketId.ToString(),
                    limit: 1);

                return response.Success && response.Tickets.Any()
                    ? response.Tickets.First()
                    : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Busca tags por prefijo (para autocompletado)
        /// </summary>
        public async Task<List<string>> SearchTagsAsync(string prefix)
        {
            try
            {
                var response = await _apiClient.SearchTagsAsync(
                    term: prefix,
                    limit: 20);

                return response.Success ? response.Tags : new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Sincroniza tags desde Freshdesk (operación larga)
        /// </summary>
        public async Task<FreshdeskSyncResult> SyncTagsAsync(
            string mode = "recent",
            int days = 30,
            IProgress<string> progress = null)
        {
            try
            {
                progress?.Report("Iniciando sincronización...");

                var response = await _apiClient.SyncTagsAsync(mode, days, 1000);

                if (response.Success)
                {
                    progress?.Report($"Completado: {response.Metrics.TagsFound} tags encontrados");
                    
                    return new FreshdeskSyncResult
                    {
                        Success = true,
                        Message = response.Message,
                        TagsInserted = response.Metrics.Inserted,
                        TagsUpdated = response.Metrics.Updated,
                        DurationSeconds = response.Metrics.DurationMs / 1000.0
                    };
                }
                else
                {
                    progress?.Report("Error en sincronización");
                    return new FreshdeskSyncResult { Success = false, Message = response.Message };
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                progress?.Report("Error: No autenticado");
                return new FreshdeskSyncResult { Success = false, Message = ex.Message };
            }
            catch (Exception ex)
            {
                progress?.Report($"Error: {ex.Message}");
                return new FreshdeskSyncResult { Success = false, Message = ex.Message };
            }
        }

        #endregion
    }

    #region Helper Classes

    public class FreshdeskSyncResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int TagsInserted { get; set; }
        public int TagsUpdated { get; set; }
        public double DurationSeconds { get; set; }
    }

    #endregion
}
```

---

## 🎨 UI/UX RECOMENDACIONES {#ui-ux}

### 1. **Selector de Ticket con Autocompletado**

```xml
<!-- XAML para WPF -->
<ComboBox x:Name="TicketComboBox"
          IsEditable="True"
          IsTextSearchEnabled="True"
          TextSearch.TextPath="Subject"
          DisplayMemberPath="Subject"
          SelectedValuePath="Id"
          MinWidth="300">
    <ComboBox.ItemTemplate>
        <DataTemplate>
            <StackPanel>
                <TextBlock Text="{Binding Subject}" FontWeight="Bold"/>
                <TextBlock>
                    <Run Text="#"/><Run Text="{Binding Id}"/>
                    <Run Text=" - "/><Run Text="{Binding StatusName}"/>
                </TextBlock>
            </StackPanel>
        </DataTemplate>
    </ComboBox.ItemTemplate>
</ComboBox>
```

```csharp
// Code-behind
private async void TicketComboBox_TextChanged(object sender, TextChangedEventArgs e)
{
    var searchTerm = TicketComboBox.Text;
    
    if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
        return;

    // Debounce (esperar 300ms antes de buscar)
    await Task.Delay(300);
    
    try
    {
        var tickets = await _freshdeskService.SearchTicketsAsync(searchTerm);
        TicketComboBox.ItemsSource = tickets;
        
        if (tickets.Any())
        {
            TicketComboBox.IsDropDownOpen = true;
        }
    }
    catch (UnauthorizedAccessException)
    {
        MessageBox.Show("Debe iniciar sesión para buscar tickets", "No autenticado", 
            MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
```

---

### 2. **Tags con Autocompletado (Chips Input)**

```xml
<!-- XAML -->
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>
    
    <!-- TextBox para escribir tags -->
    <TextBox x:Name="TagInputBox"
             Grid.Row="0"
             PlaceholderText="Agregar tag..."
             TextChanged="TagInputBox_TextChanged"/>
    
    <!-- ListBox de sugerencias -->
    <ListBox x:Name="TagSuggestionsBox"
             Grid.Row="1"
             Visibility="Collapsed"
             SelectionChanged="TagSuggestionsBox_SelectionChanged"
             MaxHeight="150"/>
</Grid>

<!-- Lista de tags seleccionados (Chips) -->
<ItemsControl x:Name="SelectedTagsPanel">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <WrapPanel/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Border Background="#E3F2FD"
                    CornerRadius="12"
                    Padding="8,4"
                    Margin="4">
                <StackPanel Orientation="Horizontal">
                    <TextBlock Text="{Binding}" 
                               VerticalAlignment="Center"/>
                    <Button Content="×"
                            Click="RemoveTag_Click"
                            Tag="{Binding}"
                            Background="Transparent"
                            BorderThickness="0"
                            FontSize="16"
                            Margin="4,0,0,0"/>
                </StackPanel>
            </Border>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

```csharp
// Code-behind
private List<string> _selectedTags = new List<string>();

private async void TagInputBox_TextChanged(object sender, TextChangedEventArgs e)
{
    var prefix = TagInputBox.Text;
    
    if (string.IsNullOrWhiteSpace(prefix) || prefix.Length < 2)
    {
        TagSuggestionsBox.Visibility = Visibility.Collapsed;
        return;
    }

    await Task.Delay(200); // Debounce
    
    var suggestions = await _freshdeskService.SearchTagsAsync(prefix);
    
    if (suggestions.Any())
    {
        TagSuggestionsBox.ItemsSource = suggestions;
        TagSuggestionsBox.Visibility = Visibility.Visible;
    }
    else
    {
        TagSuggestionsBox.Visibility = Visibility.Collapsed;
    }
}

private void TagSuggestionsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (TagSuggestionsBox.SelectedItem is string tag)
    {
        AddTag(tag);
        TagInputBox.Clear();
        TagSuggestionsBox.Visibility = Visibility.Collapsed;
    }
}

private void AddTag(string tag)
{
    if (!_selectedTags.Contains(tag))
    {
        _selectedTags.Add(tag);
        SelectedTagsPanel.ItemsSource = null;
        SelectedTagsPanel.ItemsSource = _selectedTags;
    }
}

private void RemoveTag_Click(object sender, RoutedEventArgs e)
{
    if (sender is Button button && button.Tag is string tag)
    {
        _selectedTags.Remove(tag);
        SelectedTagsPanel.ItemsSource = null;
        SelectedTagsPanel.ItemsSource = _selectedTags;
    }
}
```

---

### 3. **Botón de Sincronización de Tags**

```xml
<!-- XAML -->
<Button x:Name="SyncTagsButton"
        Content="🔄 Actualizar Tags"
        Click="SyncTagsButton_Click"
        IsEnabled="{Binding IsNotSyncing}"/>

<ProgressBar x:Name="SyncProgressBar"
             Visibility="Collapsed"
             IsIndeterminate="True"
             Height="4"
             Margin="0,8,0,0"/>

<TextBlock x:Name="SyncStatusText"
           Visibility="Collapsed"
           Margin="0,4,0,0"/>
```

```csharp
// Code-behind
private async void SyncTagsButton_Click(object sender, RoutedEventArgs e)
{
    SyncTagsButton.IsEnabled = false;
    SyncProgressBar.Visibility = Visibility.Visible;
    SyncStatusText.Visibility = Visibility.Visible;

    var progress = new Progress<string>(status =>
    {
        SyncStatusText.Text = status;
    });

    try
    {
        var result = await _freshdeskService.SyncTagsAsync("recent", 30, progress);

        if (result.Success)
        {
            MessageBox.Show(
                $"Sincronización completada:\n\n" +
                $"✅ {result.TagsInserted} tags nuevos\n" +
                $"🔄 {result.TagsUpdated} tags actualizados\n" +
                $"⏱️ {result.DurationSeconds:F1} segundos",
                "Sincronización Exitosa",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show(
                $"Error en sincronización:\n{result.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show(
            $"Error inesperado:\n{ex.Message}",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
    finally
    {
        SyncTagsButton.IsEnabled = true;
        SyncProgressBar.Visibility = Visibility.Collapsed;
        SyncStatusText.Visibility = Visibility.Collapsed;
    }
}
```

---

## 📊 FLUJOS DE TRABAJO {#flujos}

### Flujo 1: Crear Parte de Trabajo con Ticket

```
┌─────────────────────────────────────────────────────┐
│  1. Usuario abre ventana "Nuevo Parte de Trabajo"  │
└─────────────────┬───────────────────────────────────┘
                  │
                  v
┌─────────────────────────────────────────────────────┐
│  2. Usuario escribe en campo "Ticket Freshdesk"    │
│     Ejemplo: "55950" o "problema TPV"              │
└─────────────────┬───────────────────────────────────┘
                  │
                  v (después de 300ms debounce)
┌─────────────────────────────────────────────────────┐
│  3. Desktop llama:                                  │
│     GET /api/v1/freshdesk/tickets/suggest?term=...  │
└─────────────────┬───────────────────────────────────┘
                  │
                  v
┌─────────────────────────────────────────────────────┐
│  4. API busca en Freshdesk y devuelve lista        │
│     [ { id: 55950, subject: "..." }, ... ]         │
└─────────────────┬───────────────────────────────────┘
                  │
                  v
┌─────────────────────────────────────────────────────┐
│  5. Desktop muestra sugerencias en ComboBox        │
│     Usuario selecciona ticket                       │
└─────────────────┬───────────────────────────────────┘
                  │
                  v
┌─────────────────────────────────────────────────────┐
│  6. Desktop guarda parte de trabajo con:           │
│     - ticket_id: 55950                             │
│     - ticket_subject: "..." (opcional)             │
└─────────────────────────────────────────────────────┘
```

---

### Flujo 2: Agregar Tags con Autocompletado

```
┌─────────────────────────────────────────────────────┐
│  1. Usuario escribe en campo de tags: "tpv"        │
└─────────────────┬───────────────────────────────────┘
                  │
                  v (después de 200ms debounce)
┌─────────────────────────────────────────────────────┐
│  2. Desktop llama:                                  │
│     GET /api/v1/freshdesk/tags/suggest?term=tpv     │
└─────────────────┬───────────────────────────────────┘
                  │
                  v
┌─────────────────────────────────────────────────────┐
│  3. API devuelve sugerencias desde BD local        │
│     ["tpv", "tpv hw", "tpv lenta", "tpv software"] │
└─────────────────┬───────────────────────────────────┘
                  │
                  v
┌─────────────────────────────────────────────────────┐
│  4. Desktop muestra lista de sugerencias           │
│     Usuario selecciona "tpv hw"                     │
└─────────────────┬───────────────────────────────────┘
                  │
                  v
┌─────────────────────────────────────────────────────┐
│  5. Desktop agrega tag a la lista                  │
│     Tags seleccionados: [tpv hw]                    │
└─────────────────────────────────────────────────────┘
```

---

## ⚠️ MANEJO DE ERRORES {#errores}

### Errores Comunes y Soluciones

#### 1. **401 Unauthorized** (No autenticado)

```csharp
try
{
    var tickets = await _freshdeskService.SearchTicketsAsync("55950");
}
catch (UnauthorizedAccessException)
{
    MessageBox.Show(
        "Su sesión ha expirado. Por favor, inicie sesión nuevamente.",
        "Sesión Expirada",
        MessageBoxButton.OK,
        MessageBoxImage.Warning);
    
    // Redirigir a pantalla de login
    NavigateToLogin();
}
```

#### 2. **500 Internal Server Error** (Error del servidor)

```csharp
catch (FreshdeskApiException ex)
{
    Logger.Error(ex, "Error al buscar tickets");
    
    MessageBox.Show(
        "Ocurrió un error en el servidor. Por favor, intente más tarde.",
        "Error del Servidor",
        MessageBoxButton.OK,
        MessageBoxImage.Error);
}
```

#### 3. **Network Timeout** (Sin conexión)

```csharp
catch (TaskCanceledException)
{
    MessageBox.Show(
        "No se pudo conectar al servidor. Verifique su conexión a internet.",
        "Sin Conexión",
        MessageBoxButton.OK,
        MessageBoxImage.Warning);
}
```

#### 4. **Freshdesk no configurado** (Servidor sin Freshdesk)

```csharp
var isConnected = await _freshdeskService.VerifyConnectionAsync();

if (!isConnected)
{
    MessageBox.Show(
        "Freshdesk no está configurado en el servidor. " +
        "Algunas funcionalidades no estarán disponibles.",
        "Freshdesk No Disponible",
        MessageBoxButton.OK,
        MessageBoxImage.Information);
    
    // Deshabilitar features de Freshdesk en UI
    DisableFreshdeskFeatures();
}
```

---

## 🧪 TESTING {#testing}

### Tests Unitarios para el Cliente

```csharp
using Xunit;
using Moq;
using System.Net.Http;

namespace GestionTime.Desktop.Tests
{
    public class FreshdeskServiceTests
    {
        [Fact]
        public async Task SearchTickets_WithValidTerm_ReturnsResults()
        {
            // Arrange
            var mockApiClient = new Mock<FreshdeskApiClient>();
            mockApiClient
                .Setup(x => x.SearchTicketsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(new FreshdeskTicketsResponse
                {
                    Success = true,
                    Count = 2,
                    Tickets = new List<FreshdeskTicket>
                    {
                        new FreshdeskTicket { Id = 123, Subject = "Test 1" },
                        new FreshdeskTicket { Id = 456, Subject = "Test 2" }
                    }
                });

            var service = new FreshdeskService(mockApiClient.Object);

            // Act
            var results = await service.SearchTicketsAsync("test");

            // Assert
            Assert.NotNull(results);
            Assert.Equal(2, results.Count);
            Assert.Equal(123, results[0].Id);
        }

        [Fact]
        public async Task SearchTags_WithPrefix_ReturnsSuggestions()
        {
            // Arrange
            var mockApiClient = new Mock<FreshdeskApiClient>();
            mockApiClient
                .Setup(x => x.SearchTagsAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new FreshdeskTagsResponse
                {
                    Success = true,
                    Count = 3,
                    Tags = new List<string> { "tpv", "tpv hw", "tpv lenta" }
                });

            var service = new FreshdeskService(mockApiClient.Object);

            // Act
            var results = await service.SearchTagsAsync("tpv");

            // Assert
            Assert.NotNull(results);
            Assert.Equal(3, results.Count);
            Assert.Contains("tpv hw", results);
        }
    }
}
```

---

## 💡 EJEMPLOS COMPLETOS {#ejemplos}

### Ejemplo 1: Ventana "Crear Parte de Trabajo"

```csharp
using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using GestionTime.Desktop.Services;
using GestionTime.Desktop.Models;

namespace GestionTime.Desktop.Views
{
    public partial class NuevoParteTrabajoWindow : Window
    {
        private readonly FreshdeskService _freshdeskService;
        private FreshdeskTicket _selectedTicket;
        private List<string> _selectedTags = new List<string>();

        public NuevoParteTrabajoWindow(FreshdeskService freshdeskService)
        {
            InitializeComponent();
            _freshdeskService = freshdeskService;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Verificar que Freshdesk esté disponible
            var isConnected = await _freshdeskService.VerifyConnectionAsync();
            
            if (!isConnected)
            {
                TicketSearchPanel.IsEnabled = false;
                TicketSearchPanel.ToolTip = "Freshdesk no está disponible";
            }
        }

        private async void TicketSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchTerm = TicketSearchBox.Text;
            
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            {
                TicketSuggestionsBox.ItemsSource = null;
                return;
            }

            await Task.Delay(300); // Debounce
            
            try
            {
                var tickets = await _freshdeskService.SearchTicketsAsync(searchTerm);
                TicketSuggestionsBox.ItemsSource = tickets;
                
                if (tickets.Any())
                {
                    TicketSuggestionsBox.IsDropDownOpen = true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Debe iniciar sesión para buscar tickets.",
                    "No Autenticado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                // Log error silently, no molestar al usuario
                System.Diagnostics.Debug.WriteLine($"Error buscando tickets: {ex.Message}");
            }
        }

        private void TicketSuggestionsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TicketSuggestionsBox.SelectedItem is FreshdeskTicket ticket)
            {
                _selectedTicket = ticket;
                SelectedTicketPanel.Visibility = Visibility.Visible;
                SelectedTicketIdText.Text = $"#{ticket.Id}";
                SelectedTicketSubjectText.Text = ticket.Subject;
                SelectedTicketStatusText.Text = ticket.StatusName;
            }
        }

        private void RemoveTicket_Click(object sender, RoutedEventArgs e)
        {
            _selectedTicket = null;
            SelectedTicketPanel.Visibility = Visibility.Collapsed;
            TicketSearchBox.Clear();
        }

        private async void TagInputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var prefix = TagInputBox.Text;
            
            if (string.IsNullOrWhiteSpace(prefix) || prefix.Length < 2)
            {
                TagSuggestionsBox.Visibility = Visibility.Collapsed;
                return;
            }

            await Task.Delay(200); // Debounce
            
            try
            {
                var suggestions = await _freshdeskService.SearchTagsAsync(prefix);
                
                if (suggestions.Any())
                {
                    TagSuggestionsBox.ItemsSource = suggestions;
                    TagSuggestionsBox.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error buscando tags: {ex.Message}");
            }
        }

        private void TagSuggestionsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TagSuggestionsBox.SelectedItem is string tag)
            {
                AddTag(tag);
                TagInputBox.Clear();
                TagSuggestionsBox.Visibility = Visibility.Collapsed;
            }
        }

        private void AddTag(string tag)
        {
            if (!_selectedTags.Contains(tag))
            {
                _selectedTags.Add(tag);
                RefreshTagsDisplay();
            }
        }

        private void RemoveTag_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                _selectedTags.Remove(tag);
                RefreshTagsDisplay();
            }
        }

        private void RefreshTagsDisplay()
        {
            SelectedTagsPanel.ItemsSource = null;
            SelectedTagsPanel.ItemsSource = _selectedTags;
        }

        private async void GuardarButton_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(DescripcionTextBox.Text))
            {
                MessageBox.Show("Debe ingresar una descripción", "Validación", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var parteDeTrabajo = new ParteDeTrabajo
                {
                    Descripcion = DescripcionTextBox.Text,
                    FechaInicio = FechaInicioDatePicker.SelectedDate ?? DateTime.Now,
                    FechaFin = FechaFinDatePicker.SelectedDate,
                    FreshdeskTicketId = _selectedTicket?.Id,
                    FreshdeskTicketSubject = _selectedTicket?.Subject,
                    Tags = string.Join(",", _selectedTags)
                };

                // Guardar en la API
                await _parteDeTrabajoService.CreateAsync(parteDeTrabajo);

                MessageBox.Show("Parte de trabajo guardado exitosamente", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
```

---

## 📝 CHECKLIST DE IMPLEMENTACIÓN

- [ ] Crear `FreshdeskApiClient.cs` con los 4 endpoints
- [ ] Crear `FreshdeskModels.cs` con todos los DTOs
- [ ] Crear `FreshdeskService.cs` con lógica de negocio
- [ ] Configurar `HttpClient` con cookies en DI/IoC
- [ ] Implementar UI para selector de tickets (ComboBox autocompletado)
- [ ] Implementar UI para selector de tags (Chips input)
- [ ] Agregar botón "Actualizar Tags" en configuración
- [ ] Implementar manejo de errores 401/500
- [ ] Agregar validación de conexión en startup
- [ ] Agregar tests unitarios
- [ ] Documentar en README del Desktop

---

## 🔗 ENLACES ÚTILES

- **API Docs**: Ver `docs/FRESHDESK_INTEGRATION.md` en GestionTimeApi
- **Testing**: `scripts/test-freshdesk-all.ps1` para probar endpoints
- **Swagger**: `https://localhost:2502/swagger` para probar manualmente

---

**Última actualización**: 2026-01-24  
**Autor**: GitHub Copilot  
**Para**: Equipo de desarrollo Desktop (WPF)
