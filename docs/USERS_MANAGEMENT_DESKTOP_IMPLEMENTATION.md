# 🖥️ Implementación de Gestión de Usuarios en GestionTime Desktop

## 📋 Resumen

Implementación completa del módulo de gestión de usuarios y roles para GestionTime Desktop (WPF).

**Resultado de pruebas:** ✅ TODOS LOS ENDPOINTS FUNCIONAN CORRECTAMENTE

```
✅ Total de usuarios: 6
✅ Roles disponibles: ADMIN, EDITOR, USER
✅ Actualizar roles: OK
✅ Habilitar/deshabilitar: OK
```

---

## 🏗️ Estructura de Archivos a Crear

```
GestionTime.Desktop/
├── Views/
│   └── Admin/
│       └── UsersManagementWindow.xaml (+ .xaml.cs)
├── ViewModels/
│   └── Admin/
│       └── UsersManagementViewModel.cs
├── Services/
│   └── Api/
│       └── UsersApiService.cs
└── Models/
    └── Api/
        ├── UserDto.cs
        ├── RoleDto.cs
        └── UpdateUserRolesRequest.cs
```

---

## 📦 1. Models (DTOs)

### `Models/Api/UserDto.cs`
```csharp
namespace GestionTime.Desktop.Models.Api;

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public bool Enabled { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool MustChangePassword { get; set; }
    public List<string> Roles { get; set; } = new();
    
    // Propiedad para mostrar en UI
    public string RolesDisplay => string.Join(", ", Roles);
    public string StatusDisplay => Enabled ? "✅ Activo" : "❌ Inactivo";
}
```

### `Models/Api/RoleDto.cs`
```csharp
namespace GestionTime.Desktop.Models.Api;

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
```

### `Models/Api/UpdateUserRolesRequest.cs`
```csharp
namespace GestionTime.Desktop.Models.Api;

public class UpdateUserRolesRequest
{
    public List<string> Roles { get; set; } = new();
}

public class UpdateUserEnabledRequest
{
    public bool Enabled { get; set; }
}
```

---

## 🌐 2. API Service

### `Services/Api/UsersApiService.cs`
```csharp
using System.Net.Http;
using System.Net.Http.Json;
using GestionTime.Desktop.Models.Api;

namespace GestionTime.Desktop.Services.Api;

public class UsersApiService
{
    private readonly HttpClient _httpClient;

    public UsersApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>Obtiene la lista paginada de usuarios.</summary>
    public async Task<UsersPagedResult> GetUsersAsync(int page = 1, int pageSize = 50)
    {
        var response = await _httpClient.GetAsync($"/api/v1/users?page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<UsersPagedResult>();
        return result ?? new UsersPagedResult();
    }

    /// <summary>Obtiene un usuario específico por ID.</summary>
    public async Task<UserDto> GetUserByIdAsync(Guid userId)
    {
        var response = await _httpClient.GetAsync($"/api/v1/users/{userId}");
        response.EnsureSuccessStatusCode();
        
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        return user ?? throw new Exception("Usuario no encontrado");
    }

    /// <summary>Obtiene la lista de roles disponibles.</summary>
    public async Task<List<RoleDto>> GetRolesAsync()
    {
        var response = await _httpClient.GetAsync("/api/v1/roles");
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<RolesResponse>();
        return result?.Roles ?? new List<RoleDto>();
    }

    /// <summary>Actualiza los roles de un usuario.</summary>
    public async Task<bool> UpdateUserRolesAsync(Guid userId, List<string> roles)
    {
        var request = new UpdateUserRolesRequest { Roles = roles };
        var response = await _httpClient.PutAsJsonAsync($"/api/v1/users/{userId}/roles", request);
        
        return response.IsSuccessStatusCode;
    }

    /// <summary>Habilita o deshabilita un usuario.</summary>
    public async Task<bool> UpdateUserEnabledAsync(Guid userId, bool enabled)
    {
        var request = new UpdateUserEnabledRequest { Enabled = enabled };
        var response = await _httpClient.PutAsJsonAsync($"/api/v1/users/{userId}/enabled", request);
        
        return response.IsSuccessStatusCode;
    }
}

// Clases de respuesta auxiliares
public class UsersPagedResult
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public List<UserDto> Users { get; set; } = new();
}

public class RolesResponse
{
    public List<RoleDto> Roles { get; set; } = new();
}
```

---

## 🎨 3. ViewModel

### `ViewModels/Admin/UsersManagementViewModel.cs`
```csharp
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionTime.Desktop.Models.Api;
using GestionTime.Desktop.Services.Api;

namespace GestionTime.Desktop.ViewModels.Admin;

public partial class UsersManagementViewModel : ObservableObject
{
    private readonly UsersApiService _usersApi;

    [ObservableProperty]
    private ObservableCollection<UserDto> users = new();

    [ObservableProperty]
    private UserDto? selectedUser;

    [ObservableProperty]
    private ObservableCollection<RoleCheckboxItem> availableRoles = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private int currentPage = 1;

    [ObservableProperty]
    private int totalPages = 1;

    public UsersManagementViewModel(UsersApiService usersApi)
    {
        _usersApi = usersApi;
    }

    /// <summary>Carga inicial de datos.</summary>
    public async Task LoadDataAsync()
    {
        IsLoading = true;
        StatusMessage = "Cargando usuarios...";

        try
        {
            // Cargar usuarios
            var result = await _usersApi.GetUsersAsync(CurrentPage, pageSize: 50);
            Users = new ObservableCollection<UserDto>(result.Users);
            TotalPages = result.TotalPages;

            // Cargar roles disponibles
            var roles = await _usersApi.GetRolesAsync();
            AvailableRoles = new ObservableCollection<RoleCheckboxItem>(
                roles.Select(r => new RoleCheckboxItem 
                { 
                    RoleName = r.Name, 
                    IsSelected = false 
                })
            );

            StatusMessage = $"✅ Cargados {result.Total} usuarios";
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

    /// <summary>Cuando se selecciona un usuario, actualizar checkboxes de roles.</summary>
    partial void OnSelectedUserChanged(UserDto? value)
    {
        if (value == null) return;

        // Actualizar checkboxes según roles del usuario
        foreach (var roleItem in AvailableRoles)
        {
            roleItem.IsSelected = value.Roles.Contains(roleItem.RoleName);
        }
    }

    /// <summary>Guardar cambios de roles del usuario seleccionado.</summary>
    [RelayCommand]
    private async Task SaveUserRoles()
    {
        if (SelectedUser == null) return;

        IsLoading = true;
        StatusMessage = "Guardando roles...";

        try
        {
            var selectedRoles = AvailableRoles
                .Where(r => r.IsSelected)
                .Select(r => r.RoleName)
                .ToList();

            var success = await _usersApi.UpdateUserRolesAsync(SelectedUser.Id, selectedRoles);

            if (success)
            {
                SelectedUser.Roles = selectedRoles;
                StatusMessage = $"✅ Roles actualizados para {SelectedUser.Email}";
            }
            else
            {
                StatusMessage = "❌ Error al actualizar roles";
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

    /// <summary>Habilitar/deshabilitar usuario.</summary>
    [RelayCommand]
    private async Task ToggleUserEnabled()
    {
        if (SelectedUser == null) return;

        IsLoading = true;
        var newStatus = !SelectedUser.Enabled;
        StatusMessage = newStatus ? "Habilitando usuario..." : "Deshabilitando usuario...";

        try
        {
            var success = await _usersApi.UpdateUserEnabledAsync(SelectedUser.Id, newStatus);

            if (success)
            {
                SelectedUser.Enabled = newStatus;
                StatusMessage = newStatus 
                    ? $"✅ Usuario {SelectedUser.Email} habilitado"
                    : $"✅ Usuario {SelectedUser.Email} deshabilitado";
            }
            else
            {
                StatusMessage = "❌ Error al cambiar estado";
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

    /// <summary>Navegar a página anterior.</summary>
    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private async Task PreviousPage()
    {
        CurrentPage--;
        await LoadDataAsync();
    }

    private bool CanGoPrevious() => CurrentPage > 1;

    /// <summary>Navegar a página siguiente.</summary>
    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private async Task NextPage()
    {
        CurrentPage++;
        await LoadDataAsync();
    }

    private bool CanGoNext() => CurrentPage < TotalPages;
}

/// <summary>Item para checkbox de rol.</summary>
public partial class RoleCheckboxItem : ObservableObject
{
    [ObservableProperty]
    private string roleName = "";

    [ObservableProperty]
    private bool isSelected;
}
```

---

## 🖼️ 4. Vista XAML

### `Views/Admin/UsersManagementWindow.xaml`
```xml
<Window x:Class="GestionTime.Desktop.Views.Admin.UsersManagementWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        Title="👥 Gestión de Usuarios y Roles - GestionTime"
        Height="600" Width="1000"
        WindowStartupLocation="CenterScreen"
        Background="#F5F5F5">
    
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Header -->
        <Border Grid.Row="0" Background="White" Padding="15" CornerRadius="5" Margin="0,0,0,15">
            <StackPanel>
                <TextBlock Text="👥 Gestión de Usuarios y Roles" FontSize="20" FontWeight="Bold" Foreground="#333"/>
                <TextBlock Text="Administra usuarios y asigna roles desde aquí" Foreground="#666" Margin="0,5,0,0"/>
            </StackPanel>
        </Border>
        
        <!-- Contenido principal -->
        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="2*"/>
                <ColumnDefinition Width="10"/>
                <ColumnDefinition Width="1*"/>
            </Grid.ColumnDefinitions>
            
            <!-- Lista de usuarios (izquierda) -->
            <Border Grid.Column="0" Background="White" CornerRadius="5" Padding="10">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>
                    
                    <TextBlock Grid.Row="0" Text="📋 Usuarios" FontSize="16" FontWeight="Bold" Margin="0,0,0,10"/>
                    
                    <DataGrid Grid.Row="1"
                              ItemsSource="{Binding Users}"
                              SelectedItem="{Binding SelectedUser}"
                              AutoGenerateColumns="False"
                              IsReadOnly="True"
                              SelectionMode="Single"
                              GridLinesVisibility="Horizontal"
                              HeadersVisibility="Column"
                              RowHeight="35"
                              AlternatingRowBackground="#F9F9F9">
                        <DataGrid.Columns>
                            <DataGridTextColumn Header="Estado" Binding="{Binding StatusDisplay}" Width="Auto"/>
                            <DataGridTextColumn Header="Email" Binding="{Binding Email}" Width="2*"/>
                            <DataGridTextColumn Header="Nombre Completo" Binding="{Binding FullName}" Width="2*"/>
                            <DataGridTextColumn Header="Roles" Binding="{Binding RolesDisplay}" Width="*"/>
                        </DataGrid.Columns>
                    </DataGrid>
                </Grid>
            </Border>
            
            <!-- Panel de edición (derecha) -->
            <Border Grid.Column="2" Background="White" CornerRadius="5" Padding="15">
                <StackPanel>
                    <TextBlock Text="✏️ Editar Usuario" FontSize="16" FontWeight="Bold" Margin="0,0,0,15"/>
                    
                    <!-- Información del usuario -->
                    <Border BorderBrush="#E0E0E0" BorderThickness="1" Padding="10" CornerRadius="3" Margin="0,0,0,15">
                        <StackPanel>
                            <TextBlock Text="📧 Email:" FontWeight="Bold" Foreground="#666"/>
                            <TextBlock Text="{Binding SelectedUser.Email}" Margin="0,5,0,10"/>
                            
                            <TextBlock Text="👤 Nombre:" FontWeight="Bold" Foreground="#666"/>
                            <TextBlock Text="{Binding SelectedUser.FullName}" Margin="0,5,0,10"/>
                            
                            <TextBlock Text="📊 Estado:" FontWeight="Bold" Foreground="#666"/>
                            <TextBlock Text="{Binding SelectedUser.StatusDisplay}" Margin="0,5,0,0"/>
                        </StackPanel>
                    </Border>
                    
                    <!-- Checkboxes de roles -->
                    <TextBlock Text="🎭 Roles Asignados:" FontWeight="Bold" Margin="0,0,0,10"/>
                    <ItemsControl ItemsSource="{Binding AvailableRoles}">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <CheckBox Content="{Binding RoleName}"
                                         IsChecked="{Binding IsSelected}"
                                         Margin="0,5,0,5"
                                         FontSize="14"/>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                    
                    <!-- Botones de acción -->
                    <Button Content="💾 Guardar Roles"
                            Command="{Binding SaveUserRolesCommand}"
                            Background="#0B8C99"
                            Foreground="White"
                            Padding="10"
                            Margin="0,15,0,10"
                            FontSize="14"
                            FontWeight="Bold"
                            Cursor="Hand"/>
                    
                    <Button Content="{Binding SelectedUser.Enabled, Converter={StaticResource EnableButtonTextConverter}}"
                            Command="{Binding ToggleUserEnabledCommand}"
                            Background="#FF9800"
                            Foreground="White"
                            Padding="10"
                            FontSize="14"
                            FontWeight="Bold"
                            Cursor="Hand"/>
                </StackPanel>
            </Border>
        </Grid>
        
        <!-- Paginación -->
        <Border Grid.Row="2" Background="White" Padding="10" CornerRadius="5" Margin="0,15,0,10">
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
        <Border Grid.Row="3" Background="#333" Padding="10" CornerRadius="5">
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

### `Views/Admin/UsersManagementWindow.xaml.cs`
```csharp
using System.Windows;
using GestionTime.Desktop.ViewModels.Admin;

namespace GestionTime.Desktop.Views.Admin;

public partial class UsersManagementWindow : Window
{
    public UsersManagementWindow(UsersManagementViewModel viewModel)
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
services.AddTransient<UsersApiService>();

// Registrar ViewModel
services.AddTransient<UsersManagementViewModel>();

// Registrar Window
services.AddTransient<UsersManagementWindow>();
```

---

## 🎯 6. Abrir la Ventana desde el Menú Admin

### En tu `MainWindow.xaml.cs` o similar:
```csharp
private void MenuUsersManagement_Click(object sender, RoutedEventArgs e)
{
    var window = _serviceProvider.GetRequiredService<UsersManagementWindow>();
    window.ShowDialog();
}
```

### O si usas MVVM con comando:
```csharp
[RelayCommand]
private void OpenUsersManagement()
{
    var window = _serviceProvider.GetRequiredService<UsersManagementWindow>();
    window.ShowDialog();
}
```

---

## ✅ 7. Checklist de Implementación

- [ ] **1. Crear Models** (`UserDto.cs`, `RoleDto.cs`, etc.)
- [ ] **2. Crear API Service** (`UsersApiService.cs`)
- [ ] **3. Crear ViewModel** (`UsersManagementViewModel.cs`)
- [ ] **4. Crear Vista XAML** (`UsersManagementWindow.xaml`)
- [ ] **5. Registrar servicios** en DI container
- [ ] **6. Agregar opción al menú** (solo visible para Admin)
- [ ] **7. Probar flujo completo**:
  - [ ] Listar usuarios
  - [ ] Seleccionar usuario
  - [ ] Actualizar roles
  - [ ] Habilitar/deshabilitar
  - [ ] Paginación

---

## 🛡️ 8. Protecciones Importantes

### En el ViewModel, agregar validaciones:

```csharp
[RelayCommand]
private async Task SaveUserRoles()
{
    if (SelectedUser == null) return;

    // ⚠️ PROTECCIÓN: No modificar propios roles
    if (SelectedUser.Email == _currentUserEmail)
    {
        StatusMessage = "❌ No puedes modificar tus propios roles";
        return;
    }

    // ... resto del código
}

[RelayCommand]
private async Task ToggleUserEnabled()
{
    if (SelectedUser == null) return;

    // ⚠️ PROTECCIÓN: No deshabilitarse a sí mismo
    if (SelectedUser.Email == _currentUserEmail)
    {
        StatusMessage = "❌ No puedes deshabilitarte a ti mismo";
        return;
    }

    // ... resto del código
}
```

---

## 🎨 9. Converters necesarios

### `BoolToEnableButtonTextConverter.cs`
```csharp
public class BoolToEnableButtonTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool enabled)
        {
            return enabled ? "🔒 Deshabilitar Usuario" : "✅ Habilitar Usuario";
        }
        return "🔒 Deshabilitar Usuario";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

Agregar a `App.xaml`:
```xml
<Application.Resources>
    <local:BoolToEnableButtonTextConverter x:Key="EnableButtonTextConverter"/>
</Application.Resources>
```

---

## 📝 10. Testing Local

**Ejecutar el script de prueba:**
```powershell
.\scripts\test-users-management.ps1
```

**Resultado esperado:**
```
✅ Total de usuarios: 6
✅ Roles disponibles: ADMIN, EDITOR, USER
✅ Actualizar roles: OK
✅ Habilitar/deshabilitar: OK
```

---

## 🚀 11. Próximas Mejoras Opcionales

1. **Búsqueda/Filtrado** de usuarios por email o nombre
2. **Ordenamiento** de columnas en DataGrid
3. **Confirmación** antes de cambios críticos (Modal Dialog)
4. **Auditoría** - Mostrar historial de cambios
5. **Exportar** lista de usuarios a Excel/CSV
6. **Crear usuario** desde la interfaz
7. **Resetear contraseña** de usuario

---

**Fecha:** 2025-01-27  
**Versión:** 1.0  
**Estado:** ✅ Listo para implementar
