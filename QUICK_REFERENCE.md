# 🚀 Referencia Rápida - Migración

## 📖 Acceso Básico

```csharp
// En WAButt.cs ya tienes disponible:
_viewModel.LicenseService
_viewModel.ChromeDriverService
_viewModel.ContactService
_viewModel.SettingsService
_viewModel.TimingService
_viewModel.AttachmentService
```

---

## ⚡ Helpers - Reemplazos Comunes

| ANTES | DESPUÉS |
|-------|---------|
| `CheckForInternetConnection()` | `InternetHelper.CheckForInternetConnection()` |
| `SafeInt(text)` | `ValidationHelper.SafeInt(text)` |
| `IsDigitsOnly(str)` | `ValidationHelper.IsDigitsOnly(str)` |
| `GetImageState(path)` | `FileHelper.IsImageFile(path)` |
| `GetVideoState(path)` | `FileHelper.IsVideoFile(path)` |

---

## 🔧 Services - Uso Común

### Licencia
```csharp
var result = await _viewModel.LicenseService.ValidateAPIKeyAsync(key);
if (result.IsValid) { ... }
```

### Contactos
```csharp
// Guardar
var contacts = _viewModel.ContactService.ConvertFromDataGridView(dgv);
_viewModel.ContactService.SaveContactsToJson(contacts);

// Cargar
var contacts = _viewModel.ContactService.LoadContactsFromJson();
```

### Settings
```csharp
_viewModel.SettingsService.SaveSettings(settings);
var settings = _viewModel.SettingsService.LoadSettings();
```

### Timing
```csharp
await _viewModel.TimingService.ApplyDelayAsync(1000, token);
```

Ver MIGRATION_GUIDE.md para documentación completa.
