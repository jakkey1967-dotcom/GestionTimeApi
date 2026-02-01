# 📁🏷️ Implementación de Grupos y Tipos en GestionTime Desktop

## 📋 Resumen

Implementación completa de los módulos de gestión de **Grupos** y **Tipos** para GestionTime Desktop (WPF).

**Resultado de pruebas:** ✅ TODOS LOS ENDPOINTS FUNCIONAN CORRECTAMENTE

### ✅ Grupos:
```
✅ Total de grupos: 8
✅ Crear: OK (ID: 32)
✅ Obtener por ID: OK
✅ Actualizar: OK
✅ Eliminar: OK (204 No Content)
✅ Validación duplicados: OK (400 Bad Request)
```

### ✅ Tipos:
```
✅ Total de tipos: 11
✅ Crear: OK (ID: 17)
✅ Obtener por ID: OK
✅ Actualizar: OK
✅ Eliminar: OK (204 No Content)
✅ Validación duplicados: OK (409 Conflict)
```

---

## 🎯 Características Comunes

Ambos módulos (Grupos y Tipos) comparten **la misma estructura**:

| Característica | Descripción |
|----------------|-------------|
| **DTOs** | `{Entity}Dto`, `{Entity}CreateRequest`, `{Entity}UpdateRequest` |
| **Campos** | `Id`, `Nombre`, `Descripcion` |
| **Endpoints** | GET (lista), GET (por ID), POST, PUT, DELETE |
| **Validaciones** | Nombre requerido, no duplicados |
| **Sin paginación** | Listas pequeñas, se cargan completas |

---

## 🏗️ Estructura de Archivos a Crear

```
GestionTime.Desktop/
├── Views/
│   └── Catalog/
│       ├── GruposManagementWindow.xaml (+ .xaml.cs)
│       └── TiposManagementWindow.xaml (+ .xaml.cs)
├── ViewModels/
│   └── Catalog/
│       ├── GruposManagementViewModel.cs
│       └── TiposManagementViewModel.cs
├── Services/
│   └── Api/
│       ├── GruposApiService.cs
│       └── TiposApiService.cs
└── Models/
    └── Api/
        ├── GrupoDto.cs
        ├── GrupoCreateRequest.cs
        ├── GrupoUpdateRequest.cs
        ├── TipoDto.cs
        ├── TipoCreateRequest.cs
        └── TipoUpdateRequest.cs
```

---

## 📦 1. Models (DTOs)

### DTOs para GRUPOS

#### `Models/Api/GrupoDto.cs`
```csharp
namespace GestionTime.Desktop.Models.Api;

public class GrupoDto
{
    public int Id { get; set; }
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
}
```

#### `Models/Api/GrupoCreateRequest.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace GestionTime.Desktop.Models.Api;

public class GrupoCreateRequest
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(120, ErrorMessage = "El nombre no puede exceder 120 caracteres")]
    public string? Nombre { get; set; }

    [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
    public string? Descripcion { get; set; }
}
```

#### `Models/Api/GrupoUpdateRequest.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace GestionTime.Desktop.Models.Api;

public class GrupoUpdateRequest
{
    [Required]
    [StringLength(120)]
    public string? Nombre { get; set; }

    [StringLength(500)]
    public string? Descripcion { get; set; }
}
```

### DTOs para TIPOS

#### `Models/Api/TipoDto.cs`
```csharp
namespace GestionTime.Desktop.Models.Api;

public class TipoDto
{
    public int Id { get; set; }
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
}
```

#### `Models/Api/TipoCreateRequest.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace GestionTime.Desktop.Models.Api;

public class TipoCreateRequest
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(120, ErrorMessage = "El nombre no puede exceder 120 caracteres")]
    public string? Nombre { get; set; }

    [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
    public string? Descripcion { get; set; }
}
```

#### `Models/Api/TipoUpdateRequest.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace GestionTime.Desktop.Models.Api;

public class TipoUpdateRequest
{
    [Required]
    [StringLength(120)]
    public string? Nombre { get; set; }

    [StringLength(500)]
    public string? Descripcion { get; set; }
}
```

---

## 🌐 2. API Services

### `Services/Api/GruposApiService.cs`
```csharp
using System.Net.Http;
using System.Net.Http.Json;
using GestionTime.Desktop.Models.Api;

namespace GestionTime.Desktop.Services.Api;

/// <summary>Servicio para gestión de grupos (CRUD completo).</summary>
public class GruposApiService
{
    private readonly HttpClient _httpClient;

    public GruposApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>Obtiene la lista completa de grupos.</summary>
    public async Task<List<GrupoDto>> GetGruposAsync()
    {
        var response = await _httpClient.GetAsync("/api/v1/grupos");
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<List<GrupoDto>>();
        return result ?? new List<GrupoDto>();
    }

    /// <summary>Obtiene un grupo específico por ID.</summary>
    public async Task<GrupoDto> GetGrupoByIdAsync(int grupoId)
    {
        var response = await _httpClient.GetAsync($"/api/v1/grupos/{grupoId}");
        response.EnsureSuccessStatusCode();
        
        var grupo = await response.Content.ReadFromJsonAsync<GrupoDto>();
        return grupo ?? throw new Exception("Grupo no encontrado");
    }

    /// <summary>Crea un nuevo grupo.</summary>
    public async Task<GrupoDto> CreateGrupoAsync(GrupoCreateRequest grupo)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/v1/grupos", grupo);
        response.EnsureSuccessStatusCode();
        
        var created = await response.Content.ReadFromJsonAsync<GrupoDto>();
        return created ?? throw new Exception("Error al crear grupo");
    }

    /// <summary>Actualiza un grupo.</summary>
    public async Task<GrupoDto> UpdateGrupoAsync(int grupoId, GrupoUpdateRequest grupo)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/v1/grupos/{grupoId}", grupo);
        response.EnsureSuccessStatusCode();
        
        var updated = await response.Content.ReadFromJsonAsync<GrupoDto>();
        return updated ?? throw new Exception("Error al actualizar grupo");
    }

    /// <summary>Elimina un grupo.</summary>
    public async Task<bool> DeleteGrupoAsync(int grupoId)
    {
        var response = await _httpClient.DeleteAsync($"/api/v1/grupos/{grupoId}");
        return response.IsSuccessStatusCode;
    }
}
```

### `Services/Api/TiposApiService.cs`
```csharp
using System.Net.Http;
using System.Net.Http.Json;
using GestionTime.Desktop.Models.Api;

namespace GestionTime.Desktop.Services.Api;

/// <summary>Servicio para gestión de tipos (CRUD completo).</summary>
public class TiposApiService
{
    private readonly HttpClient _httpClient;

    public TiposApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>Obtiene la lista completa de tipos.</summary>
    public async Task<List<TipoDto>> GetTiposAsync()
    {
        var response = await _httpClient.GetAsync("/api/v1/tipos");
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<List<TipoDto>>();
        return result ?? new List<TipoDto>();
    }

    /// <summary>Obtiene un tipo específico por ID.</summary>
    public async Task<TipoDto> GetTipoByIdAsync(int tipoId)
    {
        var response = await _httpClient.GetAsync($"/api/v1/tipos/{tipoId}");
        response.EnsureSuccessStatusCode();
        
        var tipo = await response.Content.ReadFromJsonAsync<TipoDto>();
        return tipo ?? throw new Exception("Tipo no encontrado");
    }

    /// <summary>Crea un nuevo tipo.</summary>
    public async Task<TipoDto> CreateTipoAsync(TipoCreateRequest tipo)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/v1/tipos", tipo);
        response.EnsureSuccessStatusCode();
        
        var created = await response.Content.ReadFromJsonAsync<TipoDto>();
        return created ?? throw new Exception("Error al crear tipo");
    }

    /// <summary>Actualiza un tipo.</summary>
    public async Task<TipoDto> UpdateTipoAsync(int tipoId, TipoUpdateRequest tipo)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/v1/tipos/{tipoId}", tipo);
        response.EnsureSuccessStatusCode();
        
        var updated = await response.Content.ReadFromJsonAsync<TipoDto>();
        return updated ?? throw new Exception("Error al actualizar tipo");
    }

    /// <summary>Elimina un tipo.</summary>
    public async Task<bool> DeleteTipoAsync(int tipoId)
    {
        var response = await _httpClient.DeleteAsync($"/api/v1/tipos/{tipoId}");
        return response.IsSuccessStatusCode;
    }
}
```

---

## 🎨 3. ViewModels

### `ViewModels/Catalog/GruposManagementViewModel.cs`
```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionTime.Desktop.Models.Api;
using GestionTime.Desktop.Services.Api;

namespace GestionTime.Desktop.ViewModels.Catalog;

public partial class GruposManagementViewModel : ObservableObject
{
    private readonly GruposApiService _gruposApi;

    [ObservableProperty]
    private ObservableCollection<GrupoDto> grupos = new();

    [ObservableProperty]
    private GrupoDto? selectedGrupo;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private string? editNombre;

    [ObservableProperty]
    private string? editDescripcion;

    [ObservableProperty]
    private bool isEditMode;

    [ObservableProperty]
    private bool isNewMode;

    public GruposManagementViewModel(GruposApiService gruposApi)
    {
        _gruposApi = gruposApi;
    }

    /// <summary>Carga inicial de datos.</summary>
    public async Task LoadDataAsync()
    {
        await LoadGruposAsync();
    }

    /// <summary>Carga grupos.</summary>
    [RelayCommand]
    private async Task LoadGrupos()
    {
        IsLoading = true;
        StatusMessage = "Cargando grupos...";

        try
        {
            var result = await _gruposApi.GetGruposAsync();
            Grupos = new ObservableCollection<GrupoDto>(result);

            StatusMessage = $"✅ Cargados {result.Count} grupos";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Cuando se selecciona un grupo.</summary>
    partial void OnSelectedGrupoChanged(GrupoDto? value)
    {
        if (value == null) return;

        EditNombre = value.Nombre;
        EditDescripcion = value.Descripcion;
        
        IsEditMode = false;
        IsNewMode = false;
    }

    /// <summary>Activa modo edición.</summary>
    [RelayCommand]
    private void StartEdit()
    {
        if (SelectedGrupo == null) return;
        IsEditMode = true;
    }

    /// <summary>Cancela edición.</summary>
    [RelayCommand]
    private void CancelEdit()
    {
        IsEditMode = false;
        IsNewMode = false;
        OnSelectedGrupoChanged(SelectedGrupo);
    }

    /// <summary>Guarda cambios.</summary>
    [RelayCommand]
    private async Task SaveChanges()
    {
        if (string.IsNullOrWhiteSpace(EditNombre))
        {
            StatusMessage = "❌ El nombre es requerido";
            return;
        }

        IsLoading = true;
        StatusMessage = IsNewMode ? "Creando grupo..." : "Guardando cambios...";

        try
        {
            if (IsNewMode)
            {
                var create = new GrupoCreateRequest
                {
                    Nombre = EditNombre,
                    Descripcion = EditDescripcion
                };

                var created = await _gruposApi.CreateGrupoAsync(create);
                StatusMessage = $"✅ Grupo creado: {created.Nombre}";
                
                IsNewMode = false;
                await LoadGruposAsync();
            }
            else if (SelectedGrupo != null)
            {
                var update = new GrupoUpdateRequest
                {
                    Nombre = EditNombre,
                    Descripcion = EditDescripcion
                };

                var updated = await _gruposApi.UpdateGrupoAsync(SelectedGrupo.Id, update);
                StatusMessage = $"✅ Grupo actualizado: {updated.Nombre}";
                
                IsEditMode = false;
                await LoadGruposAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Elimina el grupo seleccionado.</summary>
    [RelayCommand]
    private async Task DeleteGrupo()
    {
        if (SelectedGrupo == null) return;

        IsLoading = true;
        StatusMessage = "Eliminando grupo...";

        try
        {
            var deleted = await _gruposApi.DeleteGrupoAsync(SelectedGrupo.Id);
            
            if (deleted)
            {
                StatusMessage = $"✅ Grupo eliminado";
                SelectedGrupo = null;
                await LoadGruposAsync();
            }
            else
            {
                StatusMessage = "❌ Error al eliminar";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Inicia modo nuevo grupo.</summary>
    [RelayCommand]
    private void StartNew()
    {
        SelectedGrupo = null;
        EditNombre = "";
        EditDescripcion = "";
        
        IsNewMode = true;
        IsEditMode = false;
    }
}
```

### `ViewModels/Catalog/TiposManagementViewModel.cs`
```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionTime.Desktop.Models.Api;
using GestionTime.Desktop.Services.Api;

namespace GestionTime.Desktop.ViewModels.Catalog;

public partial class TiposManagementViewModel : ObservableObject
{
    private readonly TiposApiService _tiposApi;

    [ObservableProperty]
    private ObservableCollection<TipoDto> tipos = new();

    [ObservableProperty]
    private TipoDto? selectedTipo;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private string? editNombre;

    [ObservableProperty]
    private string? editDescripcion;

    [ObservableProperty]
    private bool isEditMode;

    [ObservableProperty]
    private bool isNewMode;

    public TiposManagementViewModel(TiposApiService tiposApi)
    {
        _tiposApi = tiposApi;
    }

    /// <summary>Carga inicial de datos.</summary>
    public async Task LoadDataAsync()
    {
        await LoadTiposAsync();
    }

    /// <summary>Carga tipos.</summary>
    [RelayCommand]
    private async Task LoadTipos()
    {
        IsLoading = true;
        StatusMessage = "Cargando tipos...";

        try
        {
            var result = await _tiposApi.GetTiposAsync();
            Tipos = new ObservableCollection<TipoDto>(result);

            StatusMessage = $"✅ Cargados {result.Count} tipos";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Cuando se selecciona un tipo.</summary>
    partial void OnSelectedTipoChanged(TipoDto? value)
    {
        if (value == null) return;

        EditNombre = value.Nombre;
        EditDescripcion = value.Descripcion;
        
        IsEditMode = false;
        IsNewMode = false;
    }

    /// <summary>Activa modo edición.</summary>
    [RelayCommand]
    private void StartEdit()
    {
        if (SelectedTipo == null) return;
        IsEditMode = true;
    }

    /// <summary>Cancela edición.</summary>
    [RelayCommand]
    private void CancelEdit()
    {
        IsEditMode = false;
        IsNewMode = false;
        OnSelectedTipoChanged(SelectedTipo);
    }

    /// <summary>Guarda cambios.</summary>
    [RelayCommand]
    private async Task SaveChanges()
    {
        if (string.IsNullOrWhiteSpace(EditNombre))
        {
            StatusMessage = "❌ El nombre es requerido";
            return;
        }

        IsLoading = true;
        StatusMessage = IsNewMode ? "Creando tipo..." : "Guardando cambios...";

        try
        {
            if (IsNewMode)
            {
                var create = new TipoCreateRequest
                {
                    Nombre = EditNombre,
                    Descripcion = EditDescripcion
                };

                var created = await _tiposApi.CreateTipoAsync(create);
                StatusMessage = $"✅ Tipo creado: {created.Nombre}";
                
                IsNewMode = false;
                await LoadTiposAsync();
            }
            else if (SelectedTipo != null)
            {
                var update = new TipoUpdateRequest
                {
                    Nombre = EditNombre,
                    Descripcion = EditDescripcion
                };

                var updated = await _tiposApi.UpdateTipoAsync(SelectedTipo.Id, update);
                StatusMessage = $"✅ Tipo actualizado: {updated.Nombre}";
                
                IsEditMode = false;
                await LoadTiposAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Elimina el tipo seleccionado.</summary>
    [RelayCommand]
    private async Task DeleteTipo()
    {
        if (SelectedTipo == null) return;

        IsLoading = true;
        StatusMessage = "Eliminando tipo...";

        try
        {
            var deleted = await _tiposApi.DeleteTipoAsync(SelectedTipo.Id);
            
            if (deleted)
            {
                StatusMessage = $"✅ Tipo eliminado";
                SelectedTipo = null;
                await LoadTiposAsync();
            }
            else
            {
                StatusMessage = "❌ Error al eliminar";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Inicia modo nuevo tipo.</summary>
    [RelayCommand]
    private void StartNew()
    {
        SelectedTipo = null;
        EditNombre = "";
        EditDescripcion = "";
        
        IsNewMode = true;
        IsEditMode = false;
    }
}
```

---

## 🖼️ 4. Vista XAML (Template Reutilizable)

**NOTA:** Ambas vistas son **idénticas**, solo cambian:
- Título de la ventana
- Binding del DataContext
- Textos mostrados

### `Views/Catalog/GruposManagementWindow.xaml`
```xml
<Window x:Class="GestionTime.Desktop.Views.Catalog.GruposManagementWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="📁 Gestión de Grupos - GestionTime"
        Height="600" Width="900"
        WindowStartupLocation="CenterScreen"
        Background="#F5F5F5">
    
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Header -->
        <Border Grid.Row="0" Background="White" Padding="15" CornerRadius="5" Margin="0,0,0,15">
            <StackPanel>
                <TextBlock Text="📁 Gestión de Grupos" FontSize="20" FontWeight="Bold" Foreground="#333"/>
                <TextBlock Text="Administra los grupos del sistema" Foreground="#666" Margin="0,5,0,0"/>
            </StackPanel>
        </Border>
        
        <!-- Botón Nuevo -->
        <Button Grid.Row="1"
                Content="➕ Nuevo Grupo"
                Command="{Binding StartNewCommand}"
                HorizontalAlignment="Left"
                Padding="15,5"
                Background="#28A745"
                Foreground="White"
                FontWeight="Bold"
                Cursor="Hand"
                Margin="0,0,0,10"/>
        
        <!-- Contenido principal -->
        <Grid Grid.Row="2">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="1*"/>
                <ColumnDefinition Width="10"/>
                <ColumnDefinition Width="1*"/>
            </Grid.ColumnDefinitions>
            
            <!-- Lista de grupos (izquierda) -->
            <Border Grid.Column="0" Background="White" CornerRadius="5" Padding="10">
                <DataGrid ItemsSource="{Binding Grupos}"
                          SelectedItem="{Binding SelectedGrupo}"
                          AutoGenerateColumns="False"
                          IsReadOnly="True"
                          SelectionMode="Single"
                          GridLinesVisibility="Horizontal"
                          HeadersVisibility="Column"
                          RowHeight="35"
                          AlternatingRowBackground="#F9F9F9">
                    <DataGrid.Columns>
                        <DataGridTextColumn Header="ID" Binding="{Binding Id}" Width="60"/>
                        <DataGridTextColumn Header="Nombre" Binding="{Binding Nombre}" Width="*"/>
                        <DataGridTextColumn Header="Descripción" Binding="{Binding Descripcion}" Width="*"/>
                    </DataGrid.Columns>
                </DataGrid>
            </Border>
            
            <!-- Panel de edición (derecha) -->
            <Border Grid.Column="2" Background="White" CornerRadius="5" Padding="15">
                <StackPanel>
                    <TextBlock Text="✏️ Detalles del Grupo" FontSize="16" FontWeight="Bold" Margin="0,0,0,15"/>
                    
                    <TextBlock Text="Nombre *" FontWeight="Bold" Margin="0,0,0,5"/>
                    <TextBox Text="{Binding EditNombre, UpdateSourceTrigger=PropertyChanged}"
                             IsEnabled="{Binding IsEditMode}" Margin="0,0,0,10"/>
                    
                    <TextBlock Text="Descripción" FontWeight="Bold" Margin="0,0,0,5"/>
                    <TextBox Text="{Binding EditDescripcion, UpdateSourceTrigger=PropertyChanged}"
                             TextWrapping="Wrap"
                             AcceptsReturn="True"
                             MinHeight="80"
                             IsEnabled="{Binding IsEditMode}"
                             Margin="0,0,0,15"/>
                    
                    <!-- Botones de acción (modo visualización) -->
                    <StackPanel Visibility="{Binding IsEditMode, Converter={StaticResource BoolToVisibilityConverter}, ConverterParameter=Inverse}">
                        <Button Content="✏️ Editar"
                                Command="{Binding StartEditCommand}"
                                Background="#0B8C99"
                                Foreground="White"
                                Padding="10"
                                Margin="0,0,0,10"
                                FontWeight="Bold"
                                Cursor="Hand"/>
                        
                        <Button Content="🗑️ Eliminar"
                                Command="{Binding DeleteGrupoCommand}"
                                Background="#DC3545"
                                Foreground="White"
                                Padding="10"
                                FontWeight="Bold"
                                Cursor="Hand"/>
                    </StackPanel>
                    
                    <!-- Botones de acción (modo edición) -->
                    <StackPanel Visibility="{Binding IsEditMode, Converter={StaticResource BoolToVisibilityConverter}}">
                        <Button Content="💾 Guardar"
                                Command="{Binding SaveChangesCommand}"
                                Background="#28A745"
                                Foreground="White"
                                Padding="10"
                                Margin="0,0,0,10"
                                FontWeight="Bold"
                                Cursor="Hand"/>
                        
                        <Button Content="❌ Cancelar"
                                Command="{Binding CancelEditCommand}"
                                Background="#6C757D"
                                Foreground="White"
                                Padding="10"
                                FontWeight="Bold"
                                Cursor="Hand"/>
                    </StackPanel>
                </StackPanel>
            </Border>
        </Grid>
        
        <!-- Status bar -->
        <Border Grid.Row="3" Background="#333" Padding="10" CornerRadius="5" Margin="0,15,0,0">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                
                <TextBlock Grid.Column="0"
                           Text="🔄"
                           Foreground="White"
                           Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibilityConverter}}"
                           Margin="0,0,10,0"/>
                
                <TextBlock Grid.Column="1"
                           Text="{Binding StatusMessage}"
                           Foreground="White"
                           FontWeight="Bold"/>
            </Grid>
        </Border>
    </Grid>
</Window>
```

### `Views/Catalog/GruposManagementWindow.xaml.cs`
```csharp
using System.Windows;
using GestionTime.Desktop.ViewModels.Catalog;

namespace GestionTime.Desktop.Views.Catalog;

public partial class GruposManagementWindow : Window
{
    public GruposManagementWindow(GruposManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        Loaded += async (s, e) => await viewModel.LoadDataAsync();
    }
}
```

### `Views/Catalog/TiposManagementWindow.xaml` 
**(Copiar GruposManagementWindow.xaml y reemplazar "Grupo" → "Tipo")**

### `Views/Catalog/TiposManagementWindow.xaml.cs`
```csharp
using System.Windows;
using GestionTime.Desktop.ViewModels.Catalog;

namespace GestionTime.Desktop.Views.Catalog;

public partial class TiposManagementWindow : Window
{
    public TiposManagementWindow(TiposManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        Loaded += async (s, e) => await viewModel.LoadDataAsync();
    }
}
```

---

## 🔧 5. Registro de Servicios (Dependency Injection)

### En `App.xaml.cs` o `Startup.cs`:
```csharp
// Grupos
services.AddTransient<GruposApiService>();
services.AddTransient<GruposManagementViewModel>();
services.AddTransient<GruposManagementWindow>();

// Tipos
services.AddTransient<TiposApiService>();
services.AddTransient<TiposManagementViewModel>();
services.AddTransient<TiposManagementWindow>();
```

---

## 🎯 6. Abrir Ventanas desde el Menú

### En tu `MainWindow.xaml.cs`:
```csharp
private void MenuGrupos_Click(object sender, RoutedEventArgs e)
{
    var window = _serviceProvider.GetRequiredService<GruposManagementWindow>();
    window.ShowDialog();
}

private void MenuTipos_Click(object sender, RoutedEventArgs e)
{
    var window = _serviceProvider.GetRequiredService<TiposManagementWindow>();
    window.ShowDialog();
}
```

### O si usas MVVM con comandos:
```csharp
[RelayCommand]
private void OpenGruposManagement()
{
    var window = _serviceProvider.GetRequiredService<GruposManagementWindow>();
    window.ShowDialog();
}

[RelayCommand]
private void OpenTiposManagement()
{
    var window = _serviceProvider.GetRequiredService<TiposManagementWindow>();
    window.ShowDialog();
}
```

---

## 📋 7. Checklist de Implementación

### GRUPOS:
- [ ] **1. Crear Models** (`GrupoDto`, `GrupoCreateRequest`, `GrupoUpdateRequest`)
- [ ] **2. Crear API Service** (`GruposApiService.cs`)
- [ ] **3. Crear ViewModel** (`GruposManagementViewModel.cs`)
- [ ] **4. Crear Vista XAML** (`GruposManagementWindow.xaml`)
- [ ] **5. Registrar servicios** en DI container
- [ ] **6. Agregar opción al menú** principal
- [ ] **7. Probar flujo completo**

### TIPOS:
- [ ] **1. Crear Models** (`TipoDto`, `TipoCreateRequest`, `TipoUpdateRequest`)
- [ ] **2. Crear API Service** (`TiposApiService.cs`)
- [ ] **3. Crear ViewModel** (`TiposManagementViewModel.cs`)
- [ ] **4. Crear Vista XAML** (`TiposManagementWindow.xaml`)
- [ ] **5. Registrar servicios** en DI container
- [ ] **6. Agregar opción al menú** principal
- [ ] **7. Probar flujo completo**

---

## 📝 8. Testing

### Test Manual de GRUPOS:
1. Abrir ventana de grupos
2. Verificar que carga la lista (8 grupos)
3. Crear un grupo nuevo
4. Editar el grupo creado
5. Eliminar el grupo
6. Verificar que rechaza nombres duplicados

### Test Manual de TIPOS:
1. Abrir ventana de tipos
2. Verificar que carga la lista (11 tipos)
3. Crear un tipo nuevo
4. Editar el tipo creado
5. Eliminar el tipo
6. Verificar que rechaza nombres duplicados

---

## 🔐 9. Validaciones

**Backend valida:**
- ✅ Nombre requerido
- ✅ Nombre máximo 120 caracteres
- ✅ Descripción máximo 500 caracteres
- ✅ No permite nombres duplicados (400 o 409)
- ✅ No permite eliminar si está en uso (409)

**Frontend debe:**
- ✅ Validar campos requeridos antes de enviar
- ✅ Mostrar mensajes de error claros
- ✅ Confirmar antes de eliminar (MessageBox)
- ✅ Deshabilitar botón "Eliminar" si no hay selección

---

## 🎨 10. Mejoras Opcionales

1. **Búsqueda/Filtrado** - TextBox de filtro en la lista
2. **Ordenamiento** - Click en headers para ordenar
3. **Confirmación de eliminación** - MessageBox con "¿Estás seguro?"
4. **Validación en tiempo real** - Resaltar campos inválidos
5. **Auto-refresh** - Recargar automáticamente después de cambios
6. **Export** - Exportar lista a Excel/CSV
7. **Doble click** - Abrir edición con doble click en la lista

---

## 📚 Referencia de API

### Grupos:
```
GET    /api/v1/grupos           → Lista completa
GET    /api/v1/grupos/{id}      → Por ID
POST   /api/v1/grupos           → Crear
PUT    /api/v1/grupos/{id}      → Actualizar
DELETE /api/v1/grupos/{id}      → Eliminar
```

### Tipos:
```
GET    /api/v1/tipos            → Lista completa
GET    /api/v1/tipos/{id}       → Por ID
POST   /api/v1/tipos            → Crear
PUT    /api/v1/tipos/{id}       → Actualizar
DELETE /api/v1/tipos/{id}       → Eliminar
```

---

## 💡 Tip: Crear un Template Base

Ambos ViewModels y Views son **muy similares**. Puedes:

1. Crear una **clase base genérica** para el ViewModel:
   ```csharp
   public abstract class CatalogManagementViewModel<TDto, TCreate, TUpdate>
   ```

2. Crear un **UserControl base** reutilizable para la vista

3. **Heredar** de ellos en Grupos y Tipos

Esto reduce código duplicado, pero requiere más arquitectura inicial.

---

**Fecha:** 2026-02-01  
**Estado:** ✅ **LISTO PARA IMPLEMENTAR**  
**Tests Backend:** ✅ **TODOS LOS ENDPOINTS FUNCIONAN**
