# 🏢 Implementación de CRUD de Clientes en GestionTime Desktop

## 📋 Resumen

Implementación completa del módulo de gestión de clientes para GestionTime Desktop (WPF).

**Resultado de pruebas:** ✅ TODOS LOS ENDPOINTS FUNCIONAN CORRECTAMENTE

```
✅ Total de clientes: 59
✅ Listar (paginado): OK
✅ Buscar por término: OK
✅ Crear cliente: OK (ID: 65)
✅ Obtener por ID: OK
✅ Actualizar completo (PUT): OK
✅ Actualizar nota (PATCH): OK
✅ Eliminar cliente: OK
✅ Verificar eliminación (404): OK
```

---

## 🏗️ Estructura de Archivos a Crear

```
GestionTime.Desktop/
├── Views/
│   └── Catalog/
│       └── ClientesManagementWindow.xaml (+ .xaml.cs)
├── ViewModels/
│   └── Catalog/
│       └── ClientesManagementViewModel.cs
├── Services/
│   └── Api/
│       └── ClientesApiService.cs
└── Models/
    └── Api/
        ├── ClienteDto.cs              ⚠️ Reutilizar del backend
        ├── ClienteCreateDto.cs        ⚠️ Reutilizar del backend
        ├── ClienteUpdateDto.cs        ⚠️ Reutilizar del backend
        ├── ClienteUpdateNotaDto.cs    ⚠️ Reutilizar del backend
        └── ClientePagedResult.cs      ⚠️ Reutilizar del backend
```

---

## 📦 1. Models (DTOs) - ⚠️ REUTILIZAR DEL BACKEND

**IMPORTANTE:** Los DTOs ya existen en el backend y **deben ser idénticos** en Desktop.

### Opción A: Shared Library (RECOMENDADO)
Crear un proyecto `GestionTime.Contracts` compartido entre Backend y Desktop:

```xml
<!-- GestionTime.Contracts.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>
```

Mover los DTOs del backend a este proyecto y referenciarlos desde ambos lados.

### Opción B: Copiar y Mantener Sincronizado
Si prefieres copiar, **COPIA EXACTAMENTE** estos archivos del backend:

#### `Models/Api/ClienteDto.cs`
```csharp
namespace GestionTime.Desktop.Models.Api;

public class ClienteDto
{
    public int Id { get; set; }
    public string? Nombre { get; set; }
    public int? IdPuntoop { get; set; }
    public int? LocalNum { get; set; }
    public string? NombreComercial { get; set; }
    public string? Provincia { get; set; }
    public DateTime? DataUpdate { get; set; }
    public string? DataHtml { get; set; }
    public string? Nota { get; set; }
    
    // Propiedades auxiliares para UI
    public string DisplayName => Nombre ?? "Sin nombre";
    public string PuntoOpLocal => IdPuntoop.HasValue && LocalNum.HasValue 
        ? $"{IdPuntoop}/{LocalNum}" 
        : "N/A";
}
```

#### `Models/Api/ClienteCreateDto.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace GestionTime.Desktop.Models.Api;

public class ClienteCreateDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string? Nombre { get; set; }

    public int? IdPuntoop { get; set; }
    public int? LocalNum { get; set; }

    [StringLength(200)]
    public string? NombreComercial { get; set; }

    [StringLength(100)]
    public string? Provincia { get; set; }

    public DateTime? DataUpdate { get; set; }
    public string? DataHtml { get; set; }
    public string? Nota { get; set; }
}
```

#### `Models/Api/ClienteUpdateDto.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace GestionTime.Desktop.Models.Api;

public class ClienteUpdateDto
{
    [Required]
    [StringLength(200)]
    public string? Nombre { get; set; }

    public int? IdPuntoop { get; set; }
    public int? LocalNum { get; set; }

    [StringLength(200)]
    public string? NombreComercial { get; set; }

    [StringLength(100)]
    public string? Provincia { get; set; }

    public DateTime? DataUpdate { get; set; }
    public string? DataHtml { get; set; }
    public string? Nota { get; set; }
}
```

#### `Models/Api/ClienteUpdateNotaDto.cs`
```csharp
namespace GestionTime.Desktop.Models.Api;

public class ClienteUpdateNotaDto
{
    public string? Nota { get; set; }
}
```

#### `Models/Api/ClientePagedResult.cs`
```csharp
namespace GestionTime.Desktop.Models.Api;

public class ClientePagedResult
{
    public List<ClienteDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
```

---

## 🌐 2. API Service

### `Services/Api/ClientesApiService.cs`
```csharp
using System.Net.Http;
using System.Net.Http.Json;
using GestionTime.Desktop.Models.Api;

namespace GestionTime.Desktop.Services.Api;

/// <summary>Servicio para gestión de clientes (CRUD completo).</summary>
public class ClientesApiService
{
    private readonly HttpClient _httpClient;

    public ClientesApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>Obtiene la lista paginada de clientes.</summary>
    public async Task<ClientePagedResult> GetClientesAsync(
        int page = 1, 
        int pageSize = 50, 
        string? searchTerm = null,
        string? provincia = null,
        bool? hasNota = null)
    {
        var query = $"page={page}&size={pageSize}";
        
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query += $"&q={Uri.EscapeDataString(searchTerm)}";
            
        if (!string.IsNullOrWhiteSpace(provincia))
            query += $"&provincia={Uri.EscapeDataString(provincia)}";
            
        if (hasNota.HasValue)
            query += $"&hasNota={hasNota.Value.ToString().ToLower()}";

        var response = await _httpClient.GetAsync($"/api/v1/clientes?{query}");
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<ClientePagedResult>();
        return result ?? new ClientePagedResult();
    }

    /// <summary>Obtiene un cliente específico por ID.</summary>
    public async Task<ClienteDto> GetClienteByIdAsync(int clienteId)
    {
        var response = await _httpClient.GetAsync($"/api/v1/clientes/{clienteId}");
        response.EnsureSuccessStatusCode();
        
        var cliente = await response.Content.ReadFromJsonAsync<ClienteDto>();
        return cliente ?? throw new Exception("Cliente no encontrado");
    }

    /// <summary>Crea un nuevo cliente.</summary>
    public async Task<ClienteDto> CreateClienteAsync(ClienteCreateDto cliente)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/v1/clientes", cliente);
        response.EnsureSuccessStatusCode();
        
        var created = await response.Content.ReadFromJsonAsync<ClienteDto>();
        return created ?? throw new Exception("Error al crear cliente");
    }

    /// <summary>Actualiza un cliente completo.</summary>
    public async Task<ClienteDto> UpdateClienteAsync(int clienteId, ClienteUpdateDto cliente)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/v1/clientes/{clienteId}", cliente);
        response.EnsureSuccessStatusCode();
        
        var updated = await response.Content.ReadFromJsonAsync<ClienteDto>();
        return updated ?? throw new Exception("Error al actualizar cliente");
    }

    /// <summary>Actualiza solo la nota de un cliente.</summary>
    public async Task<ClienteDto> UpdateNotaAsync(int clienteId, string? nota)
    {
        var request = new ClienteUpdateNotaDto { Nota = nota };
        var response = await _httpClient.PatchAsync(
            $"/api/v1/clientes/{clienteId}/nota", 
            JsonContent.Create(request));
        response.EnsureSuccessStatusCode();
        
        var updated = await response.Content.ReadFromJsonAsync<ClienteDto>();
        return updated ?? throw new Exception("Error al actualizar nota");
    }

    /// <summary>Elimina un cliente.</summary>
    public async Task<bool> DeleteClienteAsync(int clienteId)
    {
        var response = await _httpClient.DeleteAsync($"/api/v1/clientes/{clienteId}");
        return response.IsSuccessStatusCode;
    }
}
```

---

## 🎨 3. ViewModel

### `ViewModels/Catalog/ClientesManagementViewModel.cs`
```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionTime.Desktop.Models.Api;
using GestionTime.Desktop.Services.Api;

namespace GestionTime.Desktop.ViewModels.Catalog;

public partial class ClientesManagementViewModel : ObservableObject
{
    private readonly ClientesApiService _clientesApi;

    [ObservableProperty]
    private ObservableCollection<ClienteDto> clientes = new();

    [ObservableProperty]
    private ClienteDto? selectedCliente;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private int currentPage = 1;

    [ObservableProperty]
    private int totalPages = 1;

    [ObservableProperty]
    private string? searchTerm;

    // Campos de edición
    [ObservableProperty]
    private string? editNombre;

    [ObservableProperty]
    private int? editIdPuntoop;

    [ObservableProperty]
    private int? editLocalNum;

    [ObservableProperty]
    private string? editNombreComercial;

    [ObservableProperty]
    private string? editProvincia;

    [ObservableProperty]
    private string? editNota;

    [ObservableProperty]
    private bool isEditMode;

    [ObservableProperty]
    private bool isNewMode;

    public ClientesManagementViewModel(ClientesApiService clientesApi)
    {
        _clientesApi = clientesApi;
    }

    /// <summary>Carga inicial de datos.</summary>
    public async Task LoadDataAsync()
    {
        await LoadClientesAsync();
    }

    /// <summary>Carga clientes con filtros.</summary>
    [RelayCommand]
    private async Task LoadClientes()
    {
        IsLoading = true;
        StatusMessage = "Cargando clientes...";

        try
        {
            var result = await _clientesApi.GetClientesAsync(
                CurrentPage, 
                pageSize: 50,
                searchTerm: SearchTerm);
                
            Clientes = new ObservableCollection<ClienteDto>(result.Items);
            TotalPages = (int)Math.Ceiling(result.TotalCount / (double)result.PageSize);

            StatusMessage = $"✅ Cargados {result.TotalCount} clientes";
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

    /// <summary>Cuando se selecciona un cliente, cargar campos de edición.</summary>
    partial void OnSelectedClienteChanged(ClienteDto? value)
    {
        if (value == null) return;

        EditNombre = value.Nombre;
        EditIdPuntoop = value.IdPuntoop;
        EditLocalNum = value.LocalNum;
        EditNombreComercial = value.NombreComercial;
        EditProvincia = value.Provincia;
        EditNota = value.Nota;
        
        IsEditMode = false;
        IsNewMode = false;
    }

    /// <summary>Activa modo edición.</summary>
    [RelayCommand]
    private void StartEdit()
    {
        if (SelectedCliente == null) return;
        IsEditMode = true;
    }

    /// <summary>Cancela edición.</summary>
    [RelayCommand]
    private void CancelEdit()
    {
        IsEditMode = false;
        IsNewMode = false;
        OnSelectedClienteChanged(SelectedCliente);
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
        StatusMessage = IsNewMode ? "Creando cliente..." : "Guardando cambios...";

        try
        {
            if (IsNewMode)
            {
                var create = new ClienteCreateDto
                {
                    Nombre = EditNombre,
                    IdPuntoop = EditIdPuntoop,
                    LocalNum = EditLocalNum,
                    NombreComercial = EditNombreComercial,
                    Provincia = EditProvincia,
                    Nota = EditNota
                };

                var created = await _clientesApi.CreateClienteAsync(create);
                StatusMessage = $"✅ Cliente creado: {created.Nombre}";
                
                IsNewMode = false;
                await LoadClientesAsync();
            }
            else if (SelectedCliente != null)
            {
                var update = new ClienteUpdateDto
                {
                    Nombre = EditNombre,
                    IdPuntoop = EditIdPuntoop,
                    LocalNum = EditLocalNum,
                    NombreComercial = EditNombreComercial,
                    Provincia = EditProvincia,
                    Nota = EditNota
                };

                var updated = await _clientesApi.UpdateClienteAsync(SelectedCliente.Id, update);
                StatusMessage = $"✅ Cliente actualizado: {updated.Nombre}";
                
                IsEditMode = false;
                await LoadClientesAsync();
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

    /// <summary>Actualiza solo la nota.</summary>
    [RelayCommand]
    private async Task UpdateNota()
    {
        if (SelectedCliente == null) return;

        IsLoading = true;
        StatusMessage = "Actualizando nota...";

        try
        {
            await _clientesApi.UpdateNotaAsync(SelectedCliente.Id, EditNota);
            StatusMessage = "✅ Nota actualizada";
            await LoadClientesAsync();
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

    /// <summary>Elimina el cliente seleccionado.</summary>
    [RelayCommand]
    private async Task DeleteCliente()
    {
        if (SelectedCliente == null) return;

        // Aquí deberías mostrar un MessageBox de confirmación
        IsLoading = true;
        StatusMessage = "Eliminando cliente...";

        try
        {
            var deleted = await _clientesApi.DeleteClienteAsync(SelectedCliente.Id);
            
            if (deleted)
            {
                StatusMessage = $"✅ Cliente eliminado";
                SelectedCliente = null;
                await LoadClientesAsync();
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

    /// <summary>Inicia modo nuevo cliente.</summary>
    [RelayCommand]
    private void StartNew()
    {
        SelectedCliente = null;
        EditNombre = "";
        EditIdPuntoop = null;
        EditLocalNum = null;
        EditNombreComercial = "";
        EditProvincia = "";
        EditNota = "";
        
        IsNewMode = true;
        IsEditMode = false;
    }

    /// <summary>Navegar a página anterior.</summary>
    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private async Task PreviousPage()
    {
        CurrentPage--;
        await LoadClientesAsync();
    }

    private bool CanGoPrevious() => CurrentPage > 1;

    /// <summary>Navegar a página siguiente.</summary>
    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private async Task NextPage()
    {
        CurrentPage++;
        await LoadClientesAsync();
    }

    private bool CanGoNext() => CurrentPage < TotalPages;
}
```

---

## 🖼️ 4. Vista XAML

### `Views/Catalog/ClientesManagementWindow.xaml`
```xml
<Window x:Class="GestionTime.Desktop.Views.Catalog.ClientesManagementWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        Title="🏢 Gestión de Clientes - GestionTime"
        Height="700" Width="1200"
        WindowStartupLocation="CenterScreen"
        Background="#F5F5F5">
    
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Header -->
        <Border Grid.Row="0" Background="White" Padding="15" CornerRadius="5" Margin="0,0,0,15">
            <StackPanel>
                <TextBlock Text="🏢 Gestión de Clientes" FontSize="20" FontWeight="Bold" Foreground="#333"/>
                <TextBlock Text="Administra los clientes del sistema" Foreground="#666" Margin="0,5,0,0"/>
            </StackPanel>
        </Border>
        
        <!-- Barra de búsqueda y acciones -->
        <Border Grid.Row="1" Background="White" Padding="10" CornerRadius="5" Margin="0,0,0,10">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                
                <TextBox Grid.Column="0"
                         Text="{Binding SearchTerm, UpdateSourceTrigger=PropertyChanged}"
                         Padding="10"
                         FontSize="14"
                         VerticalContentAlignment="Center">
                    <TextBox.InputBindings>
                        <KeyBinding Key="Enter" Command="{Binding LoadClientesCommand}"/>
                    </TextBox.InputBindings>
                </TextBox>
                
                <Button Grid.Column="1"
                        Content="🔍 Buscar"
                        Command="{Binding LoadClientesCommand}"
                        Margin="10,0,0,0"
                        Padding="15,5"
                        Background="#0B8C99"
                        Foreground="White"
                        FontWeight="Bold"
                        Cursor="Hand"/>
                
                <Button Grid.Column="2"
                        Content="➕ Nuevo Cliente"
                        Command="{Binding StartNewCommand}"
                        Margin="10,0,0,0"
                        Padding="15,5"
                        Background="#28A745"
                        Foreground="White"
                        FontWeight="Bold"
                        Cursor="Hand"/>
            </Grid>
        </Border>
        
        <!-- Contenido principal -->
        <Grid Grid.Row="2">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="2*"/>
                <ColumnDefinition Width="10"/>
                <ColumnDefinition Width="1*"/>
            </Grid.ColumnDefinitions>
            
            <!-- Lista de clientes (izquierda) -->
            <Border Grid.Column="0" Background="White" CornerRadius="5" Padding="10">
                <DataGrid ItemsSource="{Binding Clientes}"
                          SelectedItem="{Binding SelectedCliente}"
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
                        <DataGridTextColumn Header="Punto OP/Local" Binding="{Binding PuntoOpLocal}" Width="120"/>
                        <DataGridTextColumn Header="Provincia" Binding="{Binding Provincia}" Width="120"/>
                    </DataGrid.Columns>
                </DataGrid>
            </Border>
            
            <!-- Panel de edición (derecha) -->
            <Border Grid.Column="2" Background="White" CornerRadius="5" Padding="15">
                <ScrollViewer VerticalScrollBarVisibility="Auto">
                    <StackPanel>
                        <TextBlock Text="✏️ Detalles del Cliente" FontSize="16" FontWeight="Bold" Margin="0,0,0,15"/>
                        
                        <!-- Campos de edición -->
                        <TextBlock Text="Nombre *" FontWeight="Bold" Margin="0,0,0,5"/>
                        <TextBox Text="{Binding EditNombre, UpdateSourceTrigger=PropertyChanged}"
                                 IsEnabled="{Binding IsEditMode}" Margin="0,0,0,10"/>
                        
                        <TextBlock Text="ID Punto OP" FontWeight="Bold" Margin="0,0,0,5"/>
                        <TextBox Text="{Binding EditIdPuntoop, UpdateSourceTrigger=PropertyChanged}"
                                 IsEnabled="{Binding IsEditMode}" Margin="0,0,0,10"/>
                        
                        <TextBlock Text="Local Num" FontWeight="Bold" Margin="0,0,0,5"/>
                        <TextBox Text="{Binding EditLocalNum, UpdateSourceTrigger=PropertyChanged}"
                                 IsEnabled="{Binding IsEditMode}" Margin="0,0,0,10"/>
                        
                        <TextBlock Text="Nombre Comercial" FontWeight="Bold" Margin="0,0,0,5"/>
                        <TextBox Text="{Binding EditNombreComercial, UpdateSourceTrigger=PropertyChanged}"
                                 IsEnabled="{Binding IsEditMode}" Margin="0,0,0,10"/>
                        
                        <TextBlock Text="Provincia" FontWeight="Bold" Margin="0,0,0,5"/>
                        <TextBox Text="{Binding EditProvincia, UpdateSourceTrigger=PropertyChanged}"
                                 IsEnabled="{Binding IsEditMode}" Margin="0,0,0,10"/>
                        
                        <TextBlock Text="Nota" FontWeight="Bold" Margin="0,0,0,5"/>
                        <TextBox Text="{Binding EditNota, UpdateSourceTrigger=PropertyChanged}"
                                 TextWrapping="Wrap"
                                 AcceptsReturn="True"
                                 MinHeight="80"
                                 IsEnabled="{Binding IsEditMode}"
                                 Margin="0,0,0,15"/>
                        
                        <!-- Botones de acción -->
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
                                    Command="{Binding DeleteClienteCommand}"
                                    Background="#DC3545"
                                    Foreground="White"
                                    Padding="10"
                                    FontWeight="Bold"
                                    Cursor="Hand"/>
                        </StackPanel>
                        
                        <StackPanel Visibility="{Binding IsEditMode, Converter={StaticResource BoolToVisibilityConverter}}">
                            <Button Content="💾 Guardar Cambios"
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
                </ScrollViewer>
            </Border>
        </Grid>
        
        <!-- Paginación -->
        <Border Grid.Row="3" Background="White" Padding="10" CornerRadius="5" Margin="0,15,0,10">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                
                <Button Grid.Column="0"
                        Content="◀ Anterior"
                        Command="{Binding PreviousPageCommand}"
                        Padding="15,5"
                        Margin="0,0,10,0"/>
                
                <TextBlock Grid.Column="1"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center">
                    <Run Text="Página "/>
                    <Run Text="{Binding CurrentPage}" FontWeight="Bold"/>
                    <Run Text=" de "/>
                    <Run Text="{Binding TotalPages}" FontWeight="Bold"/>
                </TextBlock>
                
                <Button Grid.Column="2"
                        Content="Siguiente ▶"
                        Command="{Binding NextPageCommand}"
                        Padding="15,5"
                        Margin="10,0,0,0"/>
            </Grid>
        </Border>
        
        <!-- Status bar -->
        <Border Grid.Row="4" Background="#333" Padding="10" CornerRadius="5">
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

### `Views/Catalog/ClientesManagementWindow.xaml.cs`
```csharp
using System.Windows;
using GestionTime.Desktop.ViewModels.Catalog;

namespace GestionTime.Desktop.Views.Catalog;

public partial class ClientesManagementWindow : Window
{
    public ClientesManagementWindow(ClientesManagementViewModel viewModel)
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
// Registrar servicio API
services.AddTransient<ClientesApiService>();

// Registrar ViewModel
services.AddTransient<ClientesManagementViewModel>();

// Registrar Window
services.AddTransient<ClientesManagementWindow>();
```

---

## 🎯 6. Abrir la Ventana desde el Menú

### En tu `MainWindow.xaml.cs`:
```csharp
private void MenuClientes_Click(object sender, RoutedEventArgs e)
{
    var window = _serviceProvider.GetRequiredService<ClientesManagementWindow>();
    window.ShowDialog();
}
```

### O si usas MVVM con comando:
```csharp
[RelayCommand]
private void OpenClientesManagement()
{
    var window = _serviceProvider.GetRequiredService<ClientesManagementWindow>();
    window.ShowDialog();
}
```

---

## 📋 7. Checklist de Implementación

- [ ] **1. Decidir estrategia de DTOs** (Shared Library vs Copiar)
- [ ] **2. Crear/Copiar Models** (DTOs del backend)
- [ ] **3. Crear API Service** (`ClientesApiService.cs`)
- [ ] **4. Crear ViewModel** (`ClientesManagementViewModel.cs`)
- [ ] **5. Crear Vista XAML** (`ClientesManagementWindow.xaml`)
- [ ] **6. Registrar servicios** en DI container
- [ ] **7. Agregar opción al menú** principal
- [ ] **8. Probar flujo completo**:
  - [ ] Listar clientes (paginación)
  - [ ] Buscar por término
  - [ ] Crear nuevo cliente
  - [ ] Editar cliente existente
  - [ ] Actualizar solo nota
  - [ ] Eliminar cliente

---

## 📝 8. Testing

### Test Manual de Integración:
1. Abrir ventana de clientes
2. Verificar que carga la lista
3. Probar búsqueda por término
4. Crear un cliente nuevo
5. Editar el cliente creado
6. Actualizar solo la nota
7. Eliminar el cliente
8. Verificar paginación

---

## 🔐 9. Consideraciones de Seguridad

- ✅ **Solo usuarios autenticados** pueden acceder (Bearer token)
- ✅ **Validación en backend** - Frontend solo muestra errores
- ✅ **Confirmación antes de eliminar** - Agregar MessageBox
- ✅ **Manejo de errores** - Mostrar mensajes claros al usuario

---

## 🚀 10. Mejoras Opcionales

1. **Validación en tiempo real** - Resaltar campos inválidos
2. **Auto-complete** - Para provincia (combo box)
3. **Exportar a Excel** - Lista de clientes
4. **Filtros avanzados** - Panel de filtros desplegable
5. **Vista detalle** - Doble clic para ver más información
6. **Historial de cambios** - Auditoría de modificaciones
7. **Búsqueda por código de barras** - Integración con scanner

---

## 📚 Referencia de API

**Endpoints disponibles:**
```
GET    /api/v1/clientes?page=1&size=50&q=term
GET    /api/v1/clientes/{id}
POST   /api/v1/clientes
PUT    /api/v1/clientes/{id}
PATCH  /api/v1/clientes/{id}/nota
DELETE /api/v1/clientes/{id}
```

**Parámetros de búsqueda:**
- `page` - Número de página (default: 1)
- `size` - Tamaño de página (default: 50, max: 100)
- `q` - Término de búsqueda (nombre, comercial, provincia)
- `provincia` - Filtrar por provincia exacta
- `hasNota` - Filtrar por presencia de nota (true/false)

---

**Fecha:** 2026-02-01  
**Estado:** ✅ **LISTO PARA IMPLEMENTAR**  
**Test Backend:** ✅ **TODOS LOS ENDPOINTS FUNCIONAN**

