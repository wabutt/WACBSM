# 📖 Guía de Migración - Nueva Arquitectura

Esta guía te ayudará a migrar código de WAButt.cs a la nueva arquitectura de Services/Helpers/Models.

---

## 📁 Estructura Disponible

```
Presentation/
├── Models/           - Estructuras de datos
├── Services/         - Lógica de negocio
├── Helpers/          - Utilidades y funciones auxiliares
├── ViewModels/       - Coordinador de servicios
└── WAButt.cs         - UI Form (gradualmente se reduce)
```

---

## 🎯 Acceso a Servicios

En WAButt.cs ya tienes disponible:

```csharp
// En el constructor ya está inicializado:
private MainViewModel _viewModel;

// Puedes acceder a todos los servicios:
_viewModel.LicenseService          // Validación de licencia
_viewModel.ChromeDriverService     // Gestión de ChromeDriver
_viewModel.ContactService          // Gestión de contactos
_viewModel.SettingsService         // Persistencia de settings
_viewModel.TimingService           // Delays y pausas
_viewModel.AttachmentService       // Manejo de archivos
```

---

## 📚 HELPERS - Referencia Completa

### 1️⃣ **InternetHelper**
📄 `Presentation/Helpers/InternetHelper.cs`

#### Métodos Disponibles:

```csharp
// Verificar conexión a internet
bool hasInternet = InternetHelper.CheckForInternetConnection();

// Verificar conexión (async)
bool hasInternet = await InternetHelper.CheckForInternetConnectionAsync();

// Descargar archivo
InternetHelper.DownloadFile(url, destinationPath);

// Descargar archivo (async)
await InternetHelper.DownloadFileAsync(url, destinationPath);

// Descargar contenido como string
string content = InternetHelper.DownloadString(url);

// Descargar contenido como string (async)
string content = await InternetHelper.DownloadStringAsync(url);
```

#### Ejemplo de Migración:

**ANTES:**
```csharp
if (!CheckForInternetConnection())
{
    MessageBox.Show("No hay internet");
    return;
}
```

**DESPUÉS:**
```csharp
if (!InternetHelper.CheckForInternetConnection())
{
    MessageBox.Show("No hay internet");
    return;
}
```

---

### 2️⃣ **ValidationHelper**
📄 `Presentation/Helpers/ValidationHelper.cs`

#### Métodos Disponibles:

```csharp
// Verificar si string contiene solo dígitos
bool isDigits = ValidationHelper.IsDigitsOnly("12345");  // true
bool isDigits = ValidationHelper.IsDigitsOnly("123a5");  // false

// Convertir string a int de forma segura (retorna 0 si falla)
int value = ValidationHelper.SafeInt("123");    // 123
int value = ValidationHelper.SafeInt("abc");    // 0
int value = ValidationHelper.SafeInt(null);     // 0

// Validar número de teléfono
bool isValid = ValidationHelper.IsValidPhoneNumber("+1234567890");     // true
bool isValid = ValidationHelper.IsValidPhoneNumber("123-456-7890");    // true
bool isValid = ValidationHelper.IsValidPhoneNumber("abc");             // false

// Validar email
bool isValid = ValidationHelper.IsValidEmail("user@example.com");      // true
bool isValid = ValidationHelper.IsValidEmail("invalid-email");         // false

// Validación en KeyPress event (para textboxes numéricos)
private void textBox_KeyPress(object sender, KeyPressEventArgs e)
{
    e.Handled = ValidationHelper.InputNumbers(e.KeyChar);
}
```

#### Ejemplo de Migración:

**ANTES:**
```csharp
private int SafeInt(string s) => int.TryParse(s, out var v) ? v : 0;

int threshold = SafeInt(severalpausetxt.Text);

if (str.All(c => char.IsDigit(c)))
{
    // ...
}
```

**DESPUÉS:**
```csharp
// Ya no necesitas el método SafeInt local

int threshold = ValidationHelper.SafeInt(severalpausetxt.Text);

if (ValidationHelper.IsDigitsOnly(str))
{
    // ...
}
```

---

### 3️⃣ **FileHelper**
📄 `Presentation/Helpers/FileHelper.cs`

#### Métodos Disponibles:

```csharp
// Obtener extensión de archivo
string ext = FileHelper.GetExtension("imagen.jpg");  // ".jpg"

// Verificar tipo de archivo
bool isImage = FileHelper.IsImageFile("foto.jpg");       // true
bool isVideo = FileHelper.IsVideoFile("video.mp4");      // true
bool isAudio = FileHelper.IsAudioFile("audio.mp3");      // true
bool isDoc = FileHelper.IsDocumentFile("doc.pdf");       // true

// Determinar tipo de attachment
string type = FileHelper.DetermineAttachmentType("foto.jpg");  // "I" (Image/Video)
string type = FileHelper.DetermineAttachmentType("audio.mp3"); // "A" (Audio)
string type = FileHelper.DetermineAttachmentType("doc.pdf");   // "D" (Document)

// Copiar directorio recursivamente
FileHelper.DirectoryCopy(sourceDir, destDir, copySubDirs: true);

// Escribir JSON a archivo
FileHelper.WriteJsonToFile(jsonString, "settings.json");

// Verificar si archivo existe y tiene contenido
bool exists = FileHelper.FileExistsWithContent("data.json", minSize: 100);

// Obtener nombre de archivo seguro (sin caracteres inválidos)
string safeName = FileHelper.GetSafeFileName("file<name>.txt");  // "filename.txt"
```

#### Ejemplo de Migración:

**ANTES:**
```csharp
private bool GetImageState(string path)
{
    string ext = Path.GetExtension(path).ToLower();
    return ext == ".svg" || ext == ".png" || ext == ".jpg" ||
           ext == ".jpeg" || ext == ".gif" || ext == ".webp";
}

private bool GetVideoState(string path)
{
    string ext = Path.GetExtension(path).ToLower();
    return ext == ".mp4" || ext == ".mov" || ext == ".m4v";
}

if (GetImageState(filePath) || GetVideoState(filePath))
{
    // ...
}
```

**DESPUÉS:**
```csharp
// Ya no necesitas GetImageState ni GetVideoState

if (FileHelper.IsImageFile(filePath) || FileHelper.IsVideoFile(filePath))
{
    // ...
}
```

---

### 4️⃣ **StringHelper**
📄 `Presentation/Helpers/StringHelper.cs`

#### Métodos Disponibles:

```csharp
// Codificar caracteres no-ASCII a Unicode
string encoded = StringHelper.EncodeNonAscii("Niño");  // "Ni\\u00f1o"

// Decodificar Unicode a caracteres
string decoded = StringHelper.DecodeNonAscii("Ni\\u00f1o");  // "Niño"

// Obtener primeras N palabras
string first3 = StringHelper.GetWords("Hola mundo este es un test", 3);  // "Hola mundo este"

// Remover sufijo
string result = StringHelper.TrimSuffix("archivo.txt", ".txt");  // "archivo"

// Normalizar header (trim, lowercase, sin caracteres especiales)
string norm = StringHelper.NormalizeHeader("  Número  ");  // "numero"

// Escapar campo CSV
string escaped = StringHelper.EscapeCsvField("valor, con coma");  // "\"valor, con coma\""

// Truncar string con elipsis
string short = StringHelper.Truncate("Texto muy largo", 10);  // "Texto m..."

// Reemplazar placeholders en template
string msg = StringHelper.ReplacePlaceholders(
    "Hola {name}, fecha: {date}",
    "Juan",
    DateTime.Now
);
// Result: "Hola Juan, fecha: 19/11/2025"
```

#### Ejemplo de Uso:

```csharp
// En lugar de hacer manualmente:
string message = messagetxt.Text.Replace("{name}", contactName);

// Usa:
string message = StringHelper.ReplacePlaceholders(
    messagetxt.Text,
    contactName,
    DateTime.Now
);
```

---

### 5️⃣ **CsvHelper**
📄 `Presentation/Helpers/CsvHelper.cs`

#### Métodos Disponibles:

```csharp
// Detectar delimitador (coma o punto y coma)
string delimiter = CsvHelper.DetectDelimiter("contacts.csv");  // "," o ";"

// Encontrar índice de columna por nombre (busca variaciones)
string[] headers = { "Número", "Nombre", "Estado" };
int phoneIdx = CsvHelper.FirstExisting(headers, "numero", "telefono", "phone");
int nameIdx = CsvHelper.FirstExisting(headers, "nombre", "name");

// Parsear línea CSV respetando comillas
string[] fields = CsvHelper.ParseCsvLine("\"Smith, John\",30,\"New York\"");
// Result: ["Smith, John", "30", "New York"]

// Construir línea CSV
string line = CsvHelper.BuildCsvLine("John", "Doe", "30");
// Result: "John,Doe,30"

// Limpiar campo CSV (remover comillas)
string clean = CsvHelper.CleanCsvField("\"valor\"");  // "valor"
```

---

## 🔧 SERVICES - Referencia Completa

### 1️⃣ **LicenseService**
📄 `Presentation/Services/LicenseService.cs`

#### Métodos Disponibles:

```csharp
// Validar licencia contra API
LicenseModel result = await _viewModel.LicenseService.ValidateAPIKeyAsync(licenseKey);

if (result.IsValid)
{
    MessageBox.Show($"Licencia válida! Plan: {result.Plan}");
}
else
{
    MessageBox.Show($"Licencia inválida: {result.Message}");
}

// Propiedades disponibles en LicenseModel:
result.IsValid          // bool
result.Message          // string
result.Status           // "ACTIVE", "EXPIRED", etc.
result.Plan             // string
result.ExpiresAt        // DateTime?
result.DevicesUsed      // int
result.MaxDevices       // int
result.IsExpired        // bool (computed)
result.IsDeviceLimitReached  // bool (computed)

// Mostrar diálogo para ingresar licencia
string key = _viewModel.LicenseService.PromptForLicenseKey();
if (key != null)  // null si canceló
{
    // Validar la licencia
}

// Guardar licencia
_viewModel.LicenseService.SaveLicenseKey(licenseKey);

// Cargar licencia guardada
string savedKey = _viewModel.LicenseService.LoadLicenseKey();

// Limpiar licencia
_viewModel.LicenseService.ClearLicenseKey();
```

#### Ejemplo de Migración:

**ANTES:**
```csharp
// Método de 100 líneas con toda la lógica de validación API
private async Task<LicenseCheckResult> ValidateAPIKeyAsync(string key)
{
    // ... 100 líneas ...
}

var result = await ValidateAPIKeyAsync(licenseKey);
if (result.valid)
{
    // ...
}
```

**DESPUÉS:**
```csharp
// Ya no necesitas el método local

var result = await _viewModel.LicenseService.ValidateAPIKeyAsync(licenseKey);
if (result.IsValid)  // Nota: IsValid (nueva propiedad)
{
    MessageBox.Show($"Bienvenido! Plan: {result.Plan}");
}
```

---

### 2️⃣ **ChromeDriverService**
📄 `Presentation/Services/ChromeDriverService.cs`

#### Métodos Disponibles:

```csharp
// Asegurar que ChromeDriver está instalado y actualizado
bool success = await _viewModel.ChromeDriverService.EnsureChromeDriverAsync();

// Obtener última versión de ChromeDriver
string version = await _viewModel.ChromeDriverService.FetchLatestVersionAsync();

// Descargar e instalar ChromeDriver
await _viewModel.ChromeDriverService.DownloadAndInstallAsync();

// Matar procesos de ChromeDriver
_viewModel.ChromeDriverService.KillChromeDriverProcesses();

// Propiedades disponibles:
string version = _viewModel.ChromeDriverService.ChromeDriverVersion;
string downloadUrl = _viewModel.ChromeDriverService.DownloadUrl;
```

#### Ejemplo de Migración:

**ANTES:**
```csharp
if (!await ChromeDriverStateAsync())
{
    this.Close();
    return;
}

// Métodos locales:
// - ChromeDriverStateAsync() (20 líneas)
// - FetchChromeDriverVersionAsync() (100 líneas)
// - DwchromedriverAsync() (80 líneas)
// - KillWebDriver() (15 líneas)
```

**DESPUÉS:**
```csharp
if (!await _viewModel.ChromeDriverService.EnsureChromeDriverAsync())
{
    this.Close();
    return;
}

// Ya no necesitas los 4 métodos locales (215 líneas eliminadas)
```

---

### 3️⃣ **ContactService**
📄 `Presentation/Services/ContactService.cs`

#### Métodos Disponibles:

```csharp
// Guardar contactos a JSON
var contacts = new List<ContactModel>
{
    new ContactModel("+1234567890", "Juan"),
    new ContactModel("+9876543210", "María")
};
_viewModel.ContactService.SaveContactsToJson(contacts, isSMS: false);

// Cargar contactos desde JSON
List<ContactModel> contacts = _viewModel.ContactService.LoadContactsFromJson(isSMS: false);

// Remover duplicados
contacts = _viewModel.ContactService.RemoveDuplicates(contacts);

// Remover contactos vacíos
contacts = _viewModel.ContactService.RemoveEmptyContacts(contacts);

// Exportar a archivo de texto
_viewModel.ContactService.ExportToTextFile(contacts, "contacts.txt");

// Importar desde archivo de texto
List<ContactModel> imported = _viewModel.ContactService.ImportFromTextFile("contacts.txt");

// Convertir desde DataGridView
List<ContactModel> contacts = _viewModel.ContactService.ConvertFromDataGridView(contactsdgv);

// Cargar a DataGridView
_viewModel.ContactService.LoadToDataGridView(contactsdgv, contacts);
```

#### ContactModel - Propiedades:

```csharp
var contact = new ContactModel();
contact.PhoneNumber = "+1234567890";
contact.Name = "Juan";
contact.Status = "S";  // "S" = Sent, "N" = Not Sent, null = Pending

// Propiedades computed:
bool isSent = contact.IsSent;      // true si Status == "S"
bool isValid = contact.IsValid;    // true si PhoneNumber no está vacío
```

#### Ejemplo de Migración:

**ANTES:**
```csharp
// Método de 50 líneas para guardar contactos
private void Storecontaacts()
{
    DataTable dt = new DataTable();
    // ... 50 líneas de código ...
    string json = JsonConvert.SerializeObject(dt);
    File.WriteAllText(path, json);
}

Storecontaacts();
```

**DESPUÉS:**
```csharp
// Convierte DataGridView a ContactModel y guarda
var contacts = _viewModel.ContactService.ConvertFromDataGridView(contactsdgv);
_viewModel.ContactService.SaveContactsToJson(contacts);

// 50 líneas → 2 líneas
```

---

### 4️⃣ **SettingsService**
📄 `Presentation/Services/SettingsService.cs`

#### Métodos Disponibles:

```csharp
// Crear modelo de settings
var settings = new ApplicationSettingsModel
{
    WhatsAppSettings = new SendingSettingsModel
    {
        SendFullName = sendfullnamecb.Checked,
        SendDateTime = senddatetimecb.Checked,
        PreventBlock = preventblockcb.Checked,
        EachMessageDelay = int.Parse(eachmessagetimingtxt.Text),
        AutoPauseAfterMessages = int.Parse(severalpausetxt.Text),
        SendOnlyAttachment = sendonlyattachcb.Checked
    },
    LastAttachmentPath = filenametxt.Text,
    LastAttachmentType = filetype
};

// Guardar settings
_viewModel.SettingsService.SaveSettings(settings);

// Cargar settings
ApplicationSettingsModel settings = _viewModel.SettingsService.LoadSettings();

// Aplicar settings a UI
sendfullnamecb.Checked = settings.WhatsAppSettings.SendFullName;
eachmessagetimingtxt.Text = settings.WhatsAppSettings.EachMessageDelay.ToString();

// Guardar mensajes
List<string> messages = new List<string>
{
    m1txt.Text,
    m2txt.Text,
    m3txt.Text
};
_viewModel.SettingsService.SaveMessages(messages, isSMS: false);

// Cargar mensajes
List<string> messages = _viewModel.SettingsService.LoadMessages(isSMS: false);
if (messages.Count > 0) m1txt.Text = messages[0];
if (messages.Count > 1) m2txt.Text = messages[1];
```

#### Ejemplo de Migración:

**ANTES:**
```csharp
// Método de 80 líneas
private void StoreSettings()
{
    DataTable dt = new DataTable();
    dt.Columns.Add("waeachmsgpausecant", typeof(string));
    // ... 80 líneas ...
}
```

**DESPUÉS:**
```csharp
var settings = new ApplicationSettingsModel
{
    WhatsAppSettings = new SendingSettingsModel
    {
        EachMessageDelay = ValidationHelper.SafeInt(eachmessagetimingtxt.Text)
    }
};
_viewModel.SettingsService.SaveSettings(settings);

// 80 líneas → 8 líneas
```

---

### 5️⃣ **TimingService**
📄 `Presentation/Services/TimingService.cs`

#### Métodos Disponibles:

```csharp
// Aplicar delay simple
await _viewModel.TimingService.ApplyDelayAsync(1000, cancellationToken.Token);

// Aplicar delay con múltiples tokens de cancelación
await _viewModel.TimingService.ApplyDelayAsync(
    1000,
    cancellationToken.Token,
    pauseToken.Token
);

// Aplicar pausa con callback
await _viewModel.TimingService.ApplyPauseAsync(
    300,  // 300 segundos = 5 minutos
    pauseToken.Token,
    onPauseStart: (msg) => MessageBox.Show(msg)
);

// Aplicar auto-pausa (15 minutos)
await _viewModel.TimingService.ApplyAutoPauseAsync(
    currentIndex: count,
    pauseAfterCount: 50,  // Pausar cada 50 mensajes
    severalpausetoken.Token,
    onPauseStart: (msg) => MessageBox.Show(msg)
);

// Obtener delay con variación anti-bloqueo
int delay = _viewModel.TimingService.GetAntiBlockDelay(1000);
// Retorna entre 800-1200ms (±20% de variación)
```

#### Ejemplo de Migración:

**ANTES:**
```csharp
await Task.Delay(1000, cancellationToken.Token);

// O peor:
Task.Delay(1000).Wait();  // ← Bloqueante!
```

**DESPUÉS:**
```csharp
await _viewModel.TimingService.ApplyDelayAsync(1000, cancellationToken.Token);

// Con múltiples tokens:
await _viewModel.TimingService.ApplyDelayAsync(
    1000,
    cancellationToken.Token,
    pauseToken.Token,
    eachmessagetoken.Token
);
```

---

### 6️⃣ **AttachmentService**
📄 `Presentation/Services/AttachmentService.cs`

#### Métodos Disponibles:

```csharp
// Validar attachment
if (_viewModel.AttachmentService.ValidateAttachment(filePath, out string error))
{
    MessageBox.Show("Archivo válido");
}
else
{
    MessageBox.Show($"Error: {error}");
}

// Determinar tipo de attachment
AttachmentType type = _viewModel.AttachmentService.DetermineAttachmentType(filePath);
// Retorna: AttachmentType.Image, .Video, .Audio, .Document, .None

// Obtener código de tipo
string code = _viewModel.AttachmentService.GetAttachmentTypeCode(AttachmentType.Image);
// Retorna: "I", "A", "D", o null

// Crear mensaje con attachment
MessageModel message = _viewModel.AttachmentService.CreateMessageWithAttachment(
    "Hola {name}, mira esto",
    @"C:\Users\foto.jpg"
);

// Propiedades del MessageModel:
message.Content              // "Hola {name}, mira esto"
message.AttachmentPath       // "C:\Users\foto.jpg"
message.AttachmentType       // AttachmentType.Image
message.HasAttachment        // true
```

#### Ejemplo de Uso:

```csharp
// En lugar de validar manualmente:
var fileInfo = new FileInfo(filePath);
if (fileInfo.Length > 67108864)
{
    MessageBox.Show("Archivo muy grande");
    return;
}

// Usa:
if (!_viewModel.AttachmentService.ValidateAttachment(filePath, out string error))
{
    MessageBox.Show(error);
    return;
}
```

---

## 📊 MODELS - Referencia

### ContactModel
```csharp
var contact = new ContactModel
{
    PhoneNumber = "+1234567890",
    Name = "Juan",
    Status = "S"  // "S", "N", o null
};

// O usar constructor:
var contact = new ContactModel("+1234567890", "Juan");

// Propiedades:
contact.IsSent    // bool: true si Status == "S"
contact.IsValid   // bool: true si PhoneNumber no vacío
```

### MessageModel
```csharp
var message = new MessageModel
{
    Content = "Hola {name}",
    AttachmentPath = @"C:\file.jpg",
    AttachmentType = AttachmentType.Image
};

// O usar constructor:
var message = new MessageModel("Hola", @"C:\file.jpg", AttachmentType.Image);

// Propiedades:
message.HasAttachment  // bool: true si hay attachment
```

### LicenseModel
```csharp
var license = new LicenseModel
{
    IsValid = true,
    Message = "Licencia válida",
    Status = "ACTIVE",
    Plan = "Premium",
    ExpiresAt = DateTime.Now.AddYears(1),
    DevicesUsed = 1,
    MaxDevices = 3
};

// Propiedades computed:
license.IsExpired              // bool
license.IsDeviceLimitReached   // bool
```

### ProgressModel
```csharp
var progress = new ProgressModel(totalMessages: 100);

progress.IncrementSent();      // sendedmessage++
progress.IncrementFailed();    // notsendedmessage++

// Propiedades:
progress.SentMessages          // int
progress.FailedMessages        // int
progress.TotalMessages         // int
progress.CurrentIndex          // int
progress.PercentComplete       // int (0-100)
progress.RemainingMessages     // int
progress.IsComplete            // bool

progress.Reset();              // Reinicia counters
```

---

## 🔄 Ejemplos Completos de Migración

### Ejemplo 1: Botón "Guardar Contactos"

**ANTES (50 líneas):**
```csharp
private void savebtn_Click(object sender, EventArgs e)
{
    SaveFileDialog sfd = new SaveFileDialog();
    sfd.Filter = "Text files (*.txt)|*.txt";

    if (sfd.ShowDialog() == DialogResult.OK)
    {
        using (StreamWriter sw = new StreamWriter(sfd.FileName))
        {
            foreach (DataGridViewRow row in contactsdgv.Rows)
            {
                if (row.IsNewRow) continue;

                string phone = row.Cells[0].Value?.ToString() ?? "";
                string name = row.Cells[1].Value?.ToString() ?? "";

                if (!string.IsNullOrWhiteSpace(phone))
                {
                    sw.WriteLine($"{phone},{name}");
                }
            }
        }
        MessageBox.Show("Contactos guardados!");
    }
}
```

**DESPUÉS (10 líneas):**
```csharp
private void savebtn_Click(object sender, EventArgs e)
{
    SaveFileDialog sfd = new SaveFileDialog();
    sfd.Filter = "Text files (*.txt)|*.txt";

    if (sfd.ShowDialog() == DialogResult.OK)
    {
        var contacts = _viewModel.ContactService.ConvertFromDataGridView(contactsdgv);
        _viewModel.ContactService.ExportToTextFile(contacts, sfd.FileName);
        MessageBox.Show("Contactos guardados!");
    }
}
```

---

### Ejemplo 2: Botón "Conectar WhatsApp"

**ANTES:**
```csharp
private async void connectwabtn_Click(object sender, EventArgs e)
{
    // Validar internet (método local de 12 líneas)
    if (!CheckForInternetConnection())
    {
        MessageBox.Show("No hay internet");
        return;
    }

    // Validar ChromeDriver (método de 80 líneas)
    if (!await ChromeDriverStateAsync())
    {
        MessageBox.Show("Error en ChromeDriver");
        return;
    }

    // ... resto del código (100 líneas)
}
```

**DESPUÉS:**
```csharp
private async void connectwabtn_Click(object sender, EventArgs e)
{
    // Validar internet usando helper
    if (!InternetHelper.CheckForInternetConnection())
    {
        MessageBox.Show("No hay internet");
        return;
    }

    // ChromeDriver ya se inicializó en constructor
    // Ya no necesitas validarlo aquí

    // ... resto del código (100 líneas)
}
```

---

### Ejemplo 3: Validar y Guardar Settings

**ANTES (80 líneas):**
```csharp
private void StoreSettings()
{
    DataTable dt = new DataTable();
    dt.Columns.Add("waeachmsgpausecant", typeof(string));
    dt.Columns.Add("wafullname", typeof(string));
    // ... 20 columnas más

    DataRow row = dt.NewRow();
    row[0] = eachmessagetimingtxt.Text;
    row[1] = sendfullnamecb.Checked.ToString();
    // ... 20 asignaciones más

    dt.Rows.Add(row);

    string json = JsonConvert.SerializeObject(dt);
    string path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "tempfilesWAButt", "Settings.json"
    );
    Directory.CreateDirectory(Path.GetDirectoryName(path));
    File.WriteAllText(path, json);
}
```

**DESPUÉS (15 líneas):**
```csharp
private void StoreSettings()
{
    var settings = new ApplicationSettingsModel
    {
        WhatsAppSettings = new SendingSettingsModel
        {
            EachMessageDelay = ValidationHelper.SafeInt(eachmessagetimingtxt.Text),
            SendFullName = sendfullnamecb.Checked,
            SendDateTime = senddatetimecb.Checked,
            PreventBlock = preventblockcb.Checked,
            // ... otras propiedades
        }
    };

    _viewModel.SettingsService.SaveSettings(settings);
}
```

---

## ✅ Checklist de Migración

Cuando migres un método/botón, sigue estos pasos:

1. ☑️ Identifica qué hace el método
2. ☑️ Busca en esta guía el Helper/Service correspondiente
3. ☑️ Reemplaza llamadas a métodos locales por Helper/Service
4. ☑️ Prueba que funcione correctamente
5. ☑️ Elimina el método local (si ya no se usa)
6. ☑️ Commit del cambio

---

## 🎯 Orden Sugerido de Migración

Migra en este orden (de más fácil a más complejo):

1. ✅ **Helpers** (ya hecho)
   - CheckForInternetConnection → InternetHelper
   - SafeInt, IsDigitsOnly → ValidationHelper
   - GetImageState, GetVideoState → FileHelper

2. 🔄 **Settings**
   - StoreSettings → SettingsService.SaveSettings
   - OpenSettings → SettingsService.LoadSettings
   - Storemessages → SettingsService.SaveMessages

3. 🔄 **Contact Management**
   - Storecontaacts → ContactService.SaveContactsToJson
   - ReadJsonContacts → ContactService.LoadContactsFromJson
   - DeleteDuplicate → ContactService.RemoveDuplicates

4. 🔄 **ChromeDriver**
   - FetchChromeDriverVersionAsync (eliminar, ya está en servicio)
   - DwchromedriverAsync (eliminar, ya está en servicio)
   - KillWebDriver → ChromeDriverService.KillChromeDriverProcesses

5. 🔄 **License**
   - ValidateAPIKeyAsync (eliminar, ya está en servicio)
   - PromptForLicenseKey (eliminar, ya está en servicio)

6. 🔄 **Timing/Delays**
   - Task.Delay → TimingService.ApplyDelayAsync
   - pausetimingaction → TimingService.ApplyPauseAsync

7. ⏳ **Message Sending** (complejo, déjalo para el final)
   - Aquí puedes crear nuevos services si quieres

---

## 📞 Soporte

Si tienes dudas sobre cómo migrar algo específico:
1. Consulta esta guía
2. Revisa los archivos en Models/Services/Helpers para ver implementaciones
3. Busca ejemplos similares en el código

---

**¡Buena suerte con la migración! 🚀**
