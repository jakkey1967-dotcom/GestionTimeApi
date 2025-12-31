# 🌐 Multi-Tenancy: Guía de Integración Cliente

## 🎯 **Arquitectura**

Cada cliente tiene su **propia URL de API** con su propio deploy:

```
Cliente 1: https://gestiontime-pss-dvnx.onrender.com
Cliente 2: https://gestiontime-abc.onrender.com
Cliente 3: https://gestiontime-xyz.onrender.com
```

---

## 📱 **Integración en App Desktop (C#/.NET)**

### **1. Configuración de Clientes**

```csharp
public class ClientConfig
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string ApiUrl { get; set; }
    public string Logo { get; set; }
}

public class ClientsManager
{
    private static readonly Dictionary<string, ClientConfig> Clients = new()
    {
        ["pss_dvnx"] = new ClientConfig
        {
            Id = "pss_dvnx",
            Name = "PSS DVNX",
            ApiUrl = "https://gestiontime-pss-dvnx.onrender.com",
            Logo = "pss_dvnx_logo.png"
        },
        ["cliente_abc"] = new ClientConfig
        {
            Id = "cliente_abc",
            Name = "Cliente ABC",
            ApiUrl = "https://gestiontime-abc.onrender.com",
            Logo = "cliente_abc_logo.png"
        }
    };
    
    public static ClientConfig GetClient(string clientId)
    {
        if (Clients.TryGetValue(clientId, out var client))
            return client;
        
        throw new Exception($"Cliente '{clientId}' no configurado");
    }
    
    public static IEnumerable<ClientConfig> GetAllClients() => Clients.Values;
}
```

### **2. Pantalla de Selección de Cliente**

```csharp
// LoginWindow.xaml.cs (WPF) o Form (WinForms)
public partial class LoginWindow : Window
{
    private string _selectedClientId;
    private string _apiBaseUrl;
    
    public LoginWindow()
    {
        InitializeComponent();
        LoadClients();
    }
    
    private void LoadClients()
    {
        var clients = ClientsManager.GetAllClients();
        ClientComboBox.ItemsSource = clients;
        ClientComboBox.DisplayMemberPath = "Name";
        ClientComboBox.SelectedValuePath = "Id";
    }
    
    private void ClientComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedClientId = ClientComboBox.SelectedValue as string;
        var client = ClientsManager.GetClient(_selectedClientId);
        _apiBaseUrl = client.ApiUrl;
        
        // Cambiar logo si existe
        LogoImage.Source = new BitmapImage(new Uri(client.Logo, UriKind.Relative));
    }
    
    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_apiBaseUrl))
        {
            MessageBox.Show("Selecciona un cliente primero");
            return;
        }
        
        var httpClient = new HttpClient { BaseAddress = new Uri(_apiBaseUrl) };
        
        var loginRequest = new
        {
            email = EmailTextBox.Text,
            password = PasswordBox.Password
        };
        
        var response = await httpClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        
        if (response.IsSuccessStatusCode)
        {
            // Guardar API URL para uso posterior
            Settings.Default.ApiBaseUrl = _apiBaseUrl;
            Settings.Default.ClientId = _selectedClientId;
            Settings.Default.Save();
            
            // Abrir ventana principal
            var mainWindow = new MainWindow(_apiBaseUrl);
            mainWindow.Show();
            this.Close();
        }
        else
        {
            MessageBox.Show("Login fallido");
        }
    }
}
```

### **3. Service Layer con API URL Dinámica**

```csharp
public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    
    public ApiService()
    {
        // Recuperar URL guardada en settings
        _apiBaseUrl = Settings.Default.ApiBaseUrl 
                      ?? throw new Exception("No hay cliente configurado");
        
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_apiBaseUrl)
        };
    }
    
    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var request = new { email, password };
        var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/login", request);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<LoginResponse>();
    }
    
    public async Task<List<ParteDeTrabajo>> GetPartesAsync(DateTime fecha)
    {
        var response = await _httpClient.GetAsync($"/api/v1/partes?fecha={fecha:yyyy-MM-dd}");
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<List<ParteDeTrabajo>>();
    }
}
```

---

## 🌐 **Integración en App Web (JavaScript/TypeScript)**

### **1. Configuración de Clientes**

```typescript
// config/clients.ts
export interface ClientConfig {
  id: string;
  name: string;
  apiUrl: string;
  logo: string;
  primaryColor: string;
}

export const CLIENTS: Record<string, ClientConfig> = {
  pss_dvnx: {
    id: 'pss_dvnx',
    name: 'PSS DVNX',
    apiUrl: 'https://gestiontime-pss-dvnx.onrender.com',
    logo: '/logos/pss_dvnx.png',
    primaryColor: '#0B8C99'
  },
  cliente_abc: {
    id: 'cliente_abc',
    name: 'Cliente ABC',
    apiUrl: 'https://gestiontime-abc.onrender.com',
    logo: '/logos/cliente_abc.png',
    primaryColor: '#FF6B35'
  }
};

export function getClient(clientId: string): ClientConfig {
  const client = CLIENTS[clientId];
  if (!client) {
    throw new Error(`Cliente '${clientId}' no configurado`);
  }
  return client;
}

export function getAllClients(): ClientConfig[] {
  return Object.values(CLIENTS);
}
```

### **2. Context de Cliente (React)**

```typescript
// contexts/ClientContext.tsx
import React, { createContext, useContext, useState, useEffect } from 'react';
import { ClientConfig, getClient } from '../config/clients';

interface ClientContextType {
  client: ClientConfig | null;
  selectClient: (clientId: string) => void;
  apiUrl: string | null;
}

const ClientContext = createContext<ClientContextType | undefined>(undefined);

export function ClientProvider({ children }: { children: React.ReactNode }) {
  const [client, setClient] = useState<ClientConfig | null>(null);
  
  useEffect(() => {
    // Recuperar cliente guardado
    const savedClientId = localStorage.getItem('selectedClient');
    if (savedClientId) {
      try {
        setClient(getClient(savedClientId));
      } catch (error) {
        console.error('Cliente guardado no válido');
      }
    }
  }, []);
  
  const selectClient = (clientId: string) => {
    const selectedClient = getClient(clientId);
    setClient(selectedClient);
    localStorage.setItem('selectedClient', clientId);
  };
  
  return (
    <ClientContext.Provider value={{ 
      client, 
      selectClient, 
      apiUrl: client?.apiUrl || null 
    }}>
      {children}
    </ClientContext.Provider>
  );
}

export function useClient() {
  const context = useContext(ClientContext);
  if (!context) {
    throw new Error('useClient debe usarse dentro de ClientProvider');
  }
  return context;
}
```

### **3. Pantalla de Selección de Cliente**

```typescript
// pages/ClientSelection.tsx
import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useClient } from '../contexts/ClientContext';
import { getAllClients } from '../config/clients';

export function ClientSelection() {
  const { selectClient } = useClient();
  const navigate = useNavigate();
  const clients = getAllClients();
  
  const handleClientSelect = (clientId: string) => {
    selectClient(clientId);
    navigate('/login');
  };
  
  return (
    <div className="client-selection">
      <h1>Selecciona tu Empresa</h1>
      <div className="clients-grid">
        {clients.map(client => (
          <button
            key={client.id}
            className="client-card"
            onClick={() => handleClientSelect(client.id)}
            style={{ borderColor: client.primaryColor }}
          >
            <img src={client.logo} alt={client.name} />
            <h3>{client.name}</h3>
          </button>
        ))}
      </div>
    </div>
  );
}
```

### **4. API Service con URL Dinámica**

```typescript
// services/api.ts
import { useClient } from '../contexts/ClientContext';

export function useApi() {
  const { apiUrl } = useClient();
  
  if (!apiUrl) {
    throw new Error('No hay cliente seleccionado');
  }
  
  const fetchApi = async (endpoint: string, options?: RequestInit) => {
    const response = await fetch(`${apiUrl}${endpoint}`, {
      ...options,
      credentials: 'include', // Para cookies
      headers: {
        'Content-Type': 'application/json',
        ...options?.headers
      }
    });
    
    if (!response.ok) {
      throw new Error(`API Error: ${response.status}`);
    }
    
    return response.json();
  };
  
  return {
    login: (email: string, password: string) =>
      fetchApi('/api/v1/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password })
      }),
    
    getPartes: (fecha: string) =>
      fetchApi(`/api/v1/partes?fecha=${fecha}`),
    
    createParte: (parte: any) =>
      fetchApi('/api/v1/partes', {
        method: 'POST',
        body: JSON.stringify(parte)
      })
  };
}
```

---

## 🔄 **Flujo Completo**

### **Desktop App:**
```
1. Usuario abre app
2. Ve pantalla "Seleccionar Cliente"
3. Elige "PSS DVNX"
4. App guarda: ApiUrl = "https://gestiontime-pss-dvnx.onrender.com"
5. Usuario hace login → POST https://gestiontime-pss-dvnx.onrender.com/api/v1/auth/login
6. Todos los requests usan esa URL base
7. Al cerrar sesión, puede cambiar de cliente
```

### **Web App:**
```
1. Usuario accede a app.gestiontime.com
2. Ve pantalla "Seleccionar Cliente"
3. Elige "Cliente ABC"
4. App guarda en localStorage: clientId = "cliente_abc"
5. Todos los requests van a: https://gestiontime-abc.onrender.com
6. Token JWT solo funciona en esa API (diferentes JWT keys)
```

---

## ✅ **Ventajas de Este Enfoque**

1. ✅ **Simple** - No necesitas middleware ni lógica compleja
2. ✅ **Seguro** - Tokens de un cliente no sirven en otro
3. ✅ **Escalable** - Cada cliente puede tener su propio servidor
4. ✅ **Flexible** - Fácil agregar nuevos clientes
5. ✅ **Ya funciona** - Tu API actual soporta esto sin cambios

---

## 🚀 **Próximos Pasos**

1. ✅ Crear servicios en Render para cada cliente
2. ✅ Configurar variables de entorno (`DB_SCHEMA`, `JWT_SECRET_KEY`)
3. ✅ Implementar selector de cliente en tu app
4. ✅ Guardar URL seleccionada en settings/localStorage
5. ✅ Usar esa URL para todos los requests

---

## 📝 **Ejemplo Real**

**Usuario Juan trabaja para PSS DVNX:**
- Abre app → Selecciona "PSS DVNX"
- Login → `POST https://gestiontime-pss-dvnx.onrender.com/api/v1/auth/login`
- Ver partes → `GET https://gestiontime-pss-dvnx.onrender.com/api/v1/partes`
- Sus datos están en schema `pss_dvnx`

**Usuario María trabaja para Cliente ABC:**
- Abre app → Selecciona "Cliente ABC"
- Login → `POST https://gestiontime-abc.onrender.com/api/v1/auth/login`
- Ver partes → `GET https://gestiontime-abc.onrender.com/api/v1/partes`
- Sus datos están en schema `cliente_abc`

**Completamente aislados, sin código adicional en la API!** 🎉
