# 🔍 Guía Detallada de Migración Paso a Paso

Esta guía te muestra **EXACTAMENTE** cómo migrar cada tipo de código de WAButt.cs a la nueva arquitectura.

---

## 📖 ÍNDICE

1. [Cómo Identificar Qué Migrar](#como-identificar)
2. [Migración de Validaciones](#migracion-validaciones)
3. [Migración de Archivos](#migracion-archivos)
4. [Migración de Contactos](#migracion-contactos)
5. [Migración de Settings](#migracion-settings)
6. [Migración de Licencia](#migracion-licencia)
7. [Migración de Delays/Timing](#migracion-timing)
8. [Ejemplos Completos por Botón](#ejemplos-botones)
9. [Troubleshooting](#troubleshooting)

---

## 🔎 Cómo Identificar Qué Migrar {#como-identificar}

### **Paso 1: Abre un método en WAButt.cs**

Ejemplo: `connectwabtn_Click`

### **Paso 2: Busca estos patrones:**

| Si ves esto... | Usa esto... |
|----------------|-------------|
| `CheckForInternetConnection()` | `InternetHelper.CheckForInternetConnection()` |
| `SafeInt(...)` | `ValidationHelper.SafeInt(...)` |
| `.All(c => char.IsDigit(c))` | `ValidationHelper.IsDigitsOnly(...)` |
| `Path.GetExtension(...).ToLower()` seguido de comparaciones | `FileHelper.IsImageFile(...)` o similar |
| `JsonConvert.SerializeObject(...)` + `File.WriteAllText(...)` para contactos | `_viewModel.ContactService.SaveContactsToJson(...)` |
| `JsonConvert.DeserializeObject(...)` + `File.ReadAllText(...)` para contactos | `_viewModel.ContactService.LoadContactsFromJson()` |
| Guardar settings a JSON | `_viewModel.SettingsService.SaveSettings(...)` |
| Validar licencia con API | `_viewModel.LicenseService.ValidateAPIKeyAsync(...)` |
| `Task.Delay(...).Wait()` | `await _viewModel.TimingService.ApplyDelayAsync(...)` |
| Descargar ChromeDriver | `_viewModel.ChromeDriverService.EnsureChromeDriverAsync()` |

---

## ✅ Migración de Validaciones {#migracion-validaciones}

### **Caso 1: Validar si string tiene solo números**

#### 📍 Código Original en WAButt.cs:

```csharp
// Línea 3154 (ejemplo)
if (Convert.ToString(dr.Cells[i].Value).StartsWith("+") == false &&
    IsDigitsOnly(Convert.ToString(dr.Cells[i].Value)))
{
    // ...
}

// El método está en línea 3618:
private bool IsDigitsOnly(string str)
{
    return str.All(c => char.IsDigit(c));
}
```

#### ✨ Código Migrado:

```csharp
// En tu método:
if (Convert.ToString(dr.Cells[i].Value).StartsWith("+") == false &&
    ValidationHelper.IsDigitsOnly(Convert.ToString(dr.Cells[i].Value)))
{
    // ...
}

// ELIMINA el método IsDigitsOnly de WAButt.cs (ya no lo necesitas)
```

#### 📝 Pasos:

1. ✅ Busca todas las llamadas a `IsDigitsOnly(`
2. ✅ Reemplaza con `ValidationHelper.IsDigitsOnly(`
3. ✅ Compila y prueba
4. ✅ Elimina el método `IsDigitsOnly` de WAButt.cs
5. ✅ Commit: "Replace IsDigitsOnly with ValidationHelper"

---

### **Caso 2: Convertir texto a número de forma segura**

#### 📍 Código Original:

```csharp
// Línea 2035 (ya eliminado en tu caso, pero para referencia)
private int SafeInt(string s) => int.TryParse(s, out var v) ? v : 0;

// Uso en línea 1899:
int delay = SafeInt(eachmessagetimingtxt.Text) * 1000;
```

#### ✨ Código Migrado:

```csharp
// Ya no necesitas el método SafeInt local

// Uso migrado:
int delay = ValidationHelper.SafeInt(eachmessagetimingtxt.Text) * 1000;
```

#### 📝 Pasos:

1. ✅ Busca `SafeInt(`
2. ✅ Reemplaza con `ValidationHelper.SafeInt(`
3. ✅ Elimina el método local `SafeInt`
4. ✅ Prueba que los números se parseen correctamente

---

### **Caso 3: Validar números en KeyPress**

#### 📍 Código Original:

```csharp
// Línea 1754
public void InputNumbers(object sender, KeyPressEventArgs e)
{
    char decimalSeparator = Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

    if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
    {
        // ... mostrar tooltip ...
        e.Handled = true;
    }
    // ... más código
}

// Usado en líneas 3621, 3625:
private void severalpausetxt_KeyPress(object sender, KeyPressEventArgs e)
{
    InputNumbers(sender, e);
}
```

#### ✨ Código Migrado (Simple):

```csharp
// Reemplazar InputNumbers con ValidationHelper.InputNumbers
private void severalpausetxt_KeyPress(object sender, KeyPressEventArgs e)
{
    e.Handled = ValidationHelper.InputNumbers(e.KeyChar);
}

// ELIMINA el método InputNumbers completo de WAButt.cs
```

#### 📝 Pasos:

1. ✅ Busca `InputNumbers(sender, e)`
2. ✅ Reemplaza con `e.Handled = ValidationHelper.InputNumbers(e.KeyChar);`
3. ✅ Elimina el método `InputNumbers` de WAButt.cs
4. ✅ Prueba que los textboxes solo acepten números

---

## 📁 Migración de Archivos {#migracion-archivos}

### **Caso 1: Verificar si es imagen o video**

#### 📍 Código Original:

```csharp
// Línea 4506-4516 (ejemplo, ya eliminado)
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

// Uso en línea 713:
if (GetImageState(filePath) || GetVideoState(filePath))
{
    wa.ImageTextMessage(filePath, message);
    // ...
}
```

#### ✨ Código Migrado:

```csharp
// Ya no necesitas GetImageState ni GetVideoState

// Uso migrado:
if (FileHelper.IsImageFile(filePath) || FileHelper.IsVideoFile(filePath))
{
    wa.ImageTextMessage(filePath, message);
    // ...
}
```

#### 📝 Pasos:

1. ✅ Busca `GetImageState(`
2. ✅ Reemplaza con `FileHelper.IsImageFile(`
3. ✅ Busca `GetVideoState(`
4. ✅ Reemplaza con `FileHelper.IsVideoFile(`
5. ✅ Elimina ambos métodos de WAButt.cs
6. ✅ Prueba subir imagen y video

---

### **Caso 2: Determinar tipo de archivo para attachment**

#### 📍 Código Original:

```csharp
// En imagenYVideoToolStripMenuItem_Click (línea 1040):
if (ofd.ShowDialog() == DialogResult.OK)
{
    var size = new FileInfo(ofd.FileName).Length;
    string filename = ofd.FileName;

    if (size < 67108864)  // 64 MB
    {
        filenametxt.Text = filename;
        filetype = "I";  // Asumes que es imagen/video
    }
}
```

#### ✨ Código Migrado:

```csharp
if (ofd.ShowDialog() == DialogResult.OK)
{
    string filename = ofd.FileName;

    // Valida el archivo
    if (!_viewModel.AttachmentService.ValidateAttachment(filename, out string error))
    {
        MessageBox.Show(error, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
    }

    // Determina el tipo automáticamente
    filetype = FileHelper.DetermineAttachmentType(filename);
    filenametxt.Text = filename;
}
```

#### 📝 Pasos:

1. ✅ Reemplaza validación manual de tamaño con `ValidateAttachment`
2. ✅ Usa `DetermineAttachmentType` en lugar de asignar "I", "A", "D" manualmente
3. ✅ Prueba subiendo diferentes tipos de archivos
4. ✅ Verifica que muestre error si archivo es muy grande

---

## 📇 Migración de Contactos {#migracion-contactos}

### **Caso 1: Guardar contactos a JSON**

#### 📍 Código Original en WAButt.cs:

```csharp
// Método Storecontaacts() - aproximadamente 50 líneas
private void Storecontaacts()
{
    DataTable dt = new DataTable();
    dt.Columns.Add("Number", typeof(string));
    dt.Columns.Add("Name", typeof(string));
    dt.Columns.Add("Estado", typeof(string));

    foreach (DataGridViewRow row in contactsdgv.Rows)
    {
        if (row.IsNewRow) continue;

        DataRow dr = dt.NewRow();
        dr[0] = row.Cells[0].Value?.ToString() ?? "";
        dr[1] = row.Cells[1].Value?.ToString() ?? "";
        dr[2] = row.Cells[2].Value?.ToString() ?? "";
        dt.Rows.Add(dr);
    }

    string json = JsonConvert.SerializeObject(dt, Formatting.Indented);

    string path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "tempfilesWAButt", "Contacts.json"
    );

    Directory.CreateDirectory(Path.GetDirectoryName(path));
    File.WriteAllText(path, json);
}
```

#### ✨ Código Migrado (3 líneas):

```csharp
private void Storecontaacts()
{
    var contacts = _viewModel.ContactService.ConvertFromDataGridView(contactsdgv);
    _viewModel.ContactService.SaveContactsToJson(contacts, isSMS: false);
}
```

#### 📝 Pasos Detallados:

1. ✅ **Identifica** dónde se llama `Storecontaacts()`
   - Busca en WAButt.cs: `Storecontaacts()`
   - Probablemente en: `WABotfrm_FormClosing`, botones de guardar

2. ✅ **Copia el método antiguo** (por seguridad)
   - Comenta el método antiguo en lugar de eliminarlo
   - Crea el nuevo método migrado arriba

3. ✅ **Prueba el nuevo método:**
   ```
   - Agrega contactos en el DataGridView
   - Cierra la aplicación
   - Verifica que se creó Contacts.json en MyDocuments\tempfilesWAButt\
   - Abre el JSON y verifica que tiene los contactos
   ```

4. ✅ **Si funciona:**
   - Elimina el método comentado
   - Commit: "Migrate Storecontaacts to ContactService"

---

### **Caso 2: Cargar contactos desde JSON**

#### 📍 Código Original:

```csharp
// Método ReadJsonContacts - aproximadamente 40 líneas
private void ReadJsonContacts(string filename)
{
    string path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "tempfilesWAButt", filename
    );

    if (!File.Exists(path)) return;

    string json = File.ReadAllText(path);
    DataTable dt = JsonConvert.DeserializeObject<DataTable>(json);

    contactsdgv.Rows.Clear();

    foreach (DataRow row in dt.Rows)
    {
        contactsdgv.Rows.Add(
            row["Number"]?.ToString() ?? "",
            row["Name"]?.ToString() ?? "",
            row["Estado"]?.ToString() ?? ""
        );
    }
}
```

#### ✨ Código Migrado:

```csharp
private void ReadJsonContacts(string filename)
{
    // Cargar contactos desde JSON
    var contacts = _viewModel.ContactService.LoadContactsFromJson(isSMS: false);

    // Cargar a DataGridView
    _viewModel.ContactService.LoadToDataGridView(contactsdgv, contacts);
}
```

#### 📝 Pasos:

1. ✅ Guarda contactos usando el método migrado anterior
2. ✅ Cierra la aplicación
3. ✅ Abre la aplicación
4. ✅ Verifica que el diálogo pregunte cargar contactos
5. ✅ Acepta y verifica que se carguen correctamente
6. ✅ Si funciona, elimina el método antiguo

---

### **Caso 3: Eliminar duplicados**

#### 📍 Código Original:

```csharp
// Método DeleteDuplicate1 - aproximadamente 30 líneas
private void DeleteDuplicate1()
{
    // Código complejo con loops y validaciones
    HashSet<string> seen = new HashSet<string>();
    List<DataGridViewRow> toRemove = new List<DataGridViewRow>();

    foreach (DataGridViewRow row in contactsdgv.Rows)
    {
        if (row.IsNewRow) continue;

        string phone = row.Cells[0].Value?.ToString() ?? "";

        if (seen.Contains(phone))
        {
            toRemove.Add(row);
        }
        else
        {
            seen.Add(phone);
        }
    }

    foreach (var row in toRemove)
    {
        contactsdgv.Rows.Remove(row);
    }

    MessageBox.Show($"Eliminados {toRemove.Count} duplicados");
}
```

#### ✨ Código Migrado:

```csharp
private void DeleteDuplicate1()
{
    // Obtener contactos del grid
    var contacts = _viewModel.ContactService.ConvertFromDataGridView(contactsdgv);

    // Contar duplicados ANTES
    int before = contacts.Count;

    // Remover duplicados
    contacts = _viewModel.ContactService.RemoveDuplicates(contacts);

    // Contar duplicados DESPUÉS
    int removed = before - contacts.Count;

    // Actualizar grid
    _viewModel.ContactService.LoadToDataGridView(contactsdgv, contacts);

    MessageBox.Show($"Eliminados {removed} duplicados");
}
```

#### 📝 Pasos:

1. ✅ Agrega contactos duplicados manualmente
2. ✅ Ejecuta "Eliminar Duplicados"
3. ✅ Verifica que solo quede 1 de cada número
4. ✅ Verifica que el mensaje muestre cuántos se eliminaron

---

### **Caso 4: Importar desde archivo de texto**

#### 📍 Código Original (método openbtn_Click):

```csharp
private void openbtn_Click(object sender, EventArgs e)
{
    OpenFileDialog ofd = new OpenFileDialog();
    ofd.Filter = "Text files (*.txt)|*.txt";

    if (ofd.ShowDialog() == DialogResult.OK)
    {
        string[] lines = File.ReadAllLines(ofd.FileName);
        contactsdgv.Rows.Clear();

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] parts = line.Split(',');

            if (parts.Length >= 2)
            {
                string phone = parts[0].Trim();
                string name = parts[1].Trim();
                contactsdgv.Rows.Add(phone, name, "");
            }
        }

        MessageBox.Show("Contactos importados!");
    }
}
```

#### ✨ Código Migrado:

```csharp
private void openbtn_Click(object sender, EventArgs e)
{
    OpenFileDialog ofd = new OpenFileDialog();
    ofd.Filter = "Text files (*.txt)|*.txt";

    if (ofd.ShowDialog() == DialogResult.OK)
    {
        try
        {
            // Importar desde archivo
            var contacts = _viewModel.ContactService.ImportFromTextFile(ofd.FileName);

            // Cargar a grid
            _viewModel.ContactService.LoadToDataGridView(contactsdgv, contacts);

            MessageBox.Show($"Importados {contacts.Count} contactos!");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al importar: {ex.Message}", "Error");
        }
    }
}
```

#### 📝 Pasos:

1. ✅ Crea un archivo de prueba contacts.txt:
   ```
   +1234567890,Juan
   +9876543210,María
   +5555555555,Pedro
   ```

2. ✅ Usa "Abrir" → selecciona el archivo
3. ✅ Verifica que se carguen los 3 contactos
4. ✅ Verifica que el mensaje diga "Importados 3 contactos!"

---

## ⚙️ Migración de Settings {#migracion-settings}

### **Caso 1: Guardar Settings**

#### 📍 Código Original (StoreSettings - ~80 líneas):

```csharp
private void StoreSettings()
{
    DataTable dt = new DataTable();

    // Crear 14 columnas
    dt.Columns.Add("waeachmsgpausecant", typeof(string));
    dt.Columns.Add("wafullname", typeof(string));
    dt.Columns.Add("waevitblock", typeof(string));
    dt.Columns.Add("wasenddt", typeof(string));
    dt.Columns.Add("wasendmanymsg", typeof(string));
    dt.Columns.Add("waseveralpausecant", typeof(string));
    dt.Columns.Add("wafileatt", typeof(string));
    dt.Columns.Add("smseachmsgpausecant", typeof(string));
    // ... 7 columnas más

    DataRow row = dt.NewRow();
    row[0] = eachmessagetimingtxt.Text;
    row[1] = sendfullnamecb.Checked.ToString();
    row[2] = preventblockcb.Checked.ToString();
    row[3] = senddatetimecb.Checked.ToString();
    row[4] = manymessagescb.Checked.ToString();
    row[5] = severalpausetxt.Text;
    row[6] = filenametxt.Text;
    // ... 7 asignaciones más

    dt.Rows.Add(row);

    string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
    WriteJSONToFile(json, "Settings.json");
}
```

#### ✨ Código Migrado:

```csharp
private void StoreSettings()
{
    var settings = new ApplicationSettingsModel
    {
        WhatsAppSettings = new SendingSettingsModel
        {
            EachMessageDelay = ValidationHelper.SafeInt(eachmessagetimingtxt.Text),
            SendFullName = sendfullnamecb.Checked,
            PreventBlock = preventblockcb.Checked,
            SendDateTime = senddatetimecb.Checked,
            SendManyMessages = manymessagescb.Checked,
            AutoPauseAfterMessages = ValidationHelper.SafeInt(severalpausetxt.Text),
            PreventBlockTiming = wa.preventblocktiming
        },
        SMSSettings = new SendingSettingsModel
        {
            EachMessageDelay = ValidationHelper.SafeInt(eachmessagetiming2txt.Text),
            SendFullName = sendfullname2cb.Checked,
            PreventBlock = preventblock2cb.Checked,
            SendDateTime = senddatetime2cb.Checked,
            SendManyMessages = manymessages2cb.Checked,
            AutoPauseAfterMessages = ValidationHelper.SafeInt(severalpause2txt.Text),
            PreventBlockTiming = wa.preventblocktiming2
        },
        LastAttachmentPath = filenametxt.Text,
        LastAttachmentType = filetype
    };

    _viewModel.SettingsService.SaveSettings(settings);
}
```

#### 📝 Pasos:

1. ✅ **Antes de cambiar**: Cierra app y verifica que Settings.json existe
2. ✅ **Implementa el código migrado**
3. ✅ **Prueba**:
   - Cambia varias settings (checkboxes, delays, etc.)
   - Cierra la aplicación
   - Abre Settings.json y verifica que tenga las nuevas settings
4. ✅ **Verifica la estructura del JSON** (debe ser más legible que antes)

---

### **Caso 2: Cargar Settings**

#### 📍 Código Original (OpenSettings):

```csharp
private void OpenSettings()
{
    string path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "tempfilesWAButt", "Settings.json"
    );

    if (!File.Exists(path)) return;

    string json = File.ReadAllText(path);
    DataTable dt = JsonConvert.DeserializeObject<DataTable>(json);

    if (dt.Rows.Count == 0) return;

    DataRow row = dt.Rows[0];

    eachmessagetimingtxt.Text = row[0]?.ToString() ?? "";
    sendfullnamecb.Checked = row[1]?.ToString() == "True";
    preventblockcb.Checked = row[2]?.ToString() == "True";
    senddatetimecb.Checked = row[3]?.ToString() == "True";
    // ... más asignaciones
}
```

#### ✨ Código Migrado:

```csharp
private void OpenSettings()
{
    try
    {
        var settings = _viewModel.SettingsService.LoadSettings();

        // WhatsApp Settings
        eachmessagetimingtxt.Text = settings.WhatsAppSettings.EachMessageDelay.ToString();
        sendfullnamecb.Checked = settings.WhatsAppSettings.SendFullName;
        preventblockcb.Checked = settings.WhatsAppSettings.PreventBlock;
        senddatetimecb.Checked = settings.WhatsAppSettings.SendDateTime;
        manymessagescb.Checked = settings.WhatsAppSettings.SendManyMessages;
        severalpausetxt.Text = settings.WhatsAppSettings.AutoPauseAfterMessages.ToString();

        // SMS Settings
        eachmessagetiming2txt.Text = settings.SMSSettings.EachMessageDelay.ToString();
        sendfullname2cb.Checked = settings.SMSSettings.SendFullName;
        preventblock2cb.Checked = settings.SMSSettings.PreventBlock;
        senddatetime2cb.Checked = settings.SMSSettings.SendDateTime;
        manymessages2cb.Checked = settings.SMSSettings.SendManyMessages;
        severalpause2txt.Text = settings.SMSSettings.AutoPauseAfterMessages.ToString();

        // Attachment
        filenametxt.Text = settings.LastAttachmentPath ?? "";
        filetype = settings.LastAttachmentType ?? "";
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error loading settings: {ex.Message}");
        SetDefaultSettings(); // Fallback a defaults
    }
}
```

#### 📝 Pasos:

1. ✅ Guarda settings con el método migrado anterior
2. ✅ Cierra la aplicación
3. ✅ Abre la aplicación
4. ✅ Verifica que todos los checkboxes estén como los dejaste
5. ✅ Verifica que los valores numéricos se carguen correctamente

---

## 🔐 Migración de Licencia {#migracion-licencia}

### **Caso 1: Validar Licencia**

#### 📍 Código Original en constructor WAButtfrm():

```csharp
// Dentro del evento Load (líneas 131-167)
this.Load += async (sender, e) =>
{
    apptab.Visible = false;

    if (string.IsNullOrWhiteSpace(_licenseKey))
    {
        // Pedir licencia
        _licenseKey = PromptForLicenseKey();

        if (string.IsNullOrWhiteSpace(_licenseKey))
        {
            MessageBox.Show("Licencia requerida");
            this.Close();
            return;
        }
    }

    // Validar
    var result = await ValidateAPIKeyAsync(_licenseKey);

    if (!result.valid)
    {
        MessageBox.Show(result.message);
        _licenseKey = null;
        Properties.Settings.Default.LicenseKey = null;
        Properties.Settings.Default.Save();
        this.Close();
        return;
    }

    // Success
    Properties.Settings.Default.LicenseKey = _licenseKey;
    Properties.Settings.Default.Save();
    apptab.Visible = true;
};
```

#### ✨ Código Migrado:

```csharp
this.Load += async (sender, e) =>
{
    apptab.Visible = false;

    // Cargar licencia guardada
    _licenseKey = _viewModel.LicenseService.LoadLicenseKey();

    // Si no hay licencia, pedirla
    if (string.IsNullOrWhiteSpace(_licenseKey))
    {
        _licenseKey = _viewModel.LicenseService.PromptForLicenseKey();

        if (string.IsNullOrWhiteSpace(_licenseKey))
        {
            MessageBox.Show("Licencia requerida para usar la aplicación.",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            this.Close();
            return;
        }
    }

    // Validar licencia contra API
    var licenseModel = await _viewModel.LicenseService.ValidateAPIKeyAsync(_licenseKey);

    if (!licenseModel.IsValid)
    {
        MessageBox.Show(
            $"Licencia inválida: {licenseModel.Message}\n\nEstado: {licenseModel.Status}",
            "Error de Licencia",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error
        );

        // Limpiar licencia inválida
        _viewModel.LicenseService.ClearLicenseKey();
        this.Close();
        return;
    }

    // Licencia válida - guardar y mostrar info
    _viewModel.LicenseService.SaveLicenseKey(_licenseKey);

    MessageBox.Show(
        $"Bienvenido!\n\nPlan: {licenseModel.Plan}\n" +
        $"Dispositivos: {licenseModel.DevicesUsed}/{licenseModel.MaxDevices}\n" +
        $"Expira: {licenseModel.ExpiresAt?.ToString("dd/MM/yyyy") ?? "Nunca"}",
        "Licencia Válida",
        MessageBoxButtons.OK,
        MessageBoxIcon.Information
    );

    apptab.Visible = true;
};
```

#### 📝 Pasos:

1. ✅ **Elimina** los métodos locales:
   - `ValidateAPIKeyAsync()` (ya está en LicenseService)
   - `PromptForLicenseKey()` (ya está en LicenseService)

2. ✅ **Prueba el flujo completo**:
   - Elimina tu licencia: borrar `Properties.Settings.Default.LicenseKey`
   - Inicia la app
   - Debe pedir licencia
   - Ingresa licencia válida
   - Debe validar y mostrar info
   - Cierra y abre app
   - Debe recordar la licencia

3. ✅ **Prueba licencia inválida**:
   - Ingresa "INVALID-KEY"
   - Debe mostrar error y cerrar
   - Debe limpiar la licencia guardada

---

## ⏱️ Migración de Delays/Timing {#migracion-timing}

### **Caso 1: Reemplazar Task.Delay bloqueantes**

#### 📍 Código Original (INCORRECTO):

```csharp
// En SendTextMessage (líneas 633, 636)
action.SendKeys("a").Build().Perform();
action.SendKeys(Keys.Backspace).Build().Perform();
Task.Delay(500).Wait();  // ❌ BLOQUEANTE!

wa.ContactMessage(message);
Task.Delay(1000 + wa.preventblocktiming).Wait();  // ❌ BLOQUEANTE!
```

#### ✨ Código Migrado:

```csharp
// Ya está migrado en tu código, pero para referencia:
action.SendKeys("a").Build().Perform();
action.SendKeys(Keys.Backspace).Build().Perform();
await Task.Delay(500, cancellationToken.Token);  // ✅ Async con token

wa.ContactMessage(message);
await Task.Delay(1000 + wa.preventblocktiming, cancellationToken.Token);  // ✅ Async con token
```

#### 🎯 MEJOR AÚN - Usando TimingService:

```csharp
action.SendKeys("a").Build().Perform();
action.SendKeys(Keys.Backspace).Build().Perform();
await _viewModel.TimingService.ApplyDelayAsync(500, cancellationToken.Token);

wa.ContactMessage(message);
// Con anti-bloqueo (variación aleatoria)
int delay = _viewModel.TimingService.GetAntiBlockDelay(1000 + wa.preventblocktiming);
await _viewModel.TimingService.ApplyDelayAsync(delay, cancellationToken.Token);
```

#### 📝 Pasos:

1. ✅ Busca `.Wait()` en todo el archivo
2. ✅ Reemplaza con `await` + token de cancelación
3. ✅ Si quieres anti-bloqueo, usa `GetAntiBlockDelay()`
4. ✅ Asegúrate que el método sea `async`

---

### **Caso 2: Pausas con múltiples tokens**

#### 📍 Código Original:

```csharp
// DelayBetweenMessages (línea 1023)
await Task.Run(() =>
{
    try
    {
        Task.Delay(eachmessagetiming, eachmessagetoken.Token).Wait();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Delay cancelled: {ex.Message}");
    }
});
```

#### ✨ Código Migrado:

```csharp
private async Task DelayBetweenMessages()
{
    if (eachmessagetiming > 0)
    {
        // Usar linked token para pausar con múltiples tokens
        await _viewModel.TimingService.ApplyDelayAsync(
            eachmessagetiming,
            eachmessagetoken.Token,
            pauseToken.Token
        );
    }
}
```

#### 📝 Explicación:

- `ApplyDelayAsync` con múltiples tokens = pausa si CUALQUIERA se cancela
- Útil cuando tienes botón "Pausar" Y botón "Detener"
- Más limpio que `CreateLinkedTokenSource` manual

---

## 🔘 Ejemplos Completos por Botón {#ejemplos-botones}

### **Botón: Guardar Contactos (savebtn)**

#### 📋 Funcionalidad:
- Pedir ubicación de archivo
- Guardar contactos del DataGridView a archivo de texto
- Mostrar confirmación

#### 📍 Código Original Completo:

```csharp
private void savebtn_Click(object sender, EventArgs e)
{
    SaveFileDialog sfd = new SaveFileDialog();
    sfd.Filter = "Text files (*.txt)|*.txt";
    sfd.Title = "Guardar Contactos";

    if (sfd.ShowDialog() == DialogResult.OK)
    {
        try
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

            MessageBox.Show(
                "Contactos guardados exitosamente!",
                "Éxito",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error al guardar: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }
}
```

#### ✨ Código Migrado Completo:

```csharp
private void savebtn_Click(object sender, EventArgs e)
{
    SaveFileDialog sfd = new SaveFileDialog();
    sfd.Filter = "Text files (*.txt)|*.txt";
    sfd.Title = "Guardar Contactos";

    if (sfd.ShowDialog() == DialogResult.OK)
    {
        try
        {
            // Convertir DataGridView a lista de ContactModel
            var contacts = _viewModel.ContactService.ConvertFromDataGridView(contactsdgv);

            // Guardar a archivo
            _viewModel.ContactService.ExportToTextFile(contacts, sfd.FileName);

            MessageBox.Show(
                $"Guardados {contacts.Count} contactos exitosamente!",
                "Éxito",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error al guardar: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }
}
```

#### 📊 Comparación:
- **ANTES**: 40 líneas, manejo manual de StreamWriter y loops
- **DESPUÉS**: 25 líneas, uso de servicio + muestra cantidad guardada
- **Reducción**: 37.5%

---

### **Botón: Abrir Contactos (openbtn)**

#### ✨ Código Migrado Completo:

```csharp
private void openbtn_Click(object sender, EventArgs e)
{
    OpenFileDialog ofd = new OpenFileDialog();
    ofd.Filter = "Text files (*.txt)|*.txt";
    ofd.Title = "Abrir Contactos";

    if (ofd.ShowDialog() == DialogResult.OK)
    {
        try
        {
            // Importar desde archivo
            var contacts = _viewModel.ContactService.ImportFromTextFile(ofd.FileName);

            // Cargar a DataGridView
            _viewModel.ContactService.LoadToDataGridView(contactsdgv, contacts);

            MessageBox.Show(
                $"Importados {contacts.Count} contactos!",
                "Éxito",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error al abrir: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }
}
```

---

### **Botón: Eliminar Duplicados**

#### ✨ Código Migrado Completo:

```csharp
private void eliminarDuplicadosToolStripMenuItem_Click(object sender, EventArgs e)
{
    try
    {
        // Obtener contactos actuales
        var contacts = _viewModel.ContactService.ConvertFromDataGridView(contactsdgv);

        int before = contacts.Count;

        // Remover duplicados
        contacts = _viewModel.ContactService.RemoveDuplicates(contacts);

        int removed = before - contacts.Count;

        // Actualizar grid
        _viewModel.ContactService.LoadToDataGridView(contactsdgv, contacts);

        MessageBox.Show(
            $"Eliminados {removed} contactos duplicados.\n\nRestantes: {contacts.Count}",
            "Duplicados Eliminados",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error: {ex.Message}", "Error");
    }
}
```

---

### **Botón: Limpiar Filas Vacías**

#### ✨ Código Migrado Completo:

```csharp
private void eliminarFilasVaciasToolStripMenuItem_Click(object sender, EventArgs e)
{
    try
    {
        // Obtener contactos actuales
        var contacts = _viewModel.ContactService.ConvertFromDataGridView(contactsdgv);

        int before = contacts.Count;

        // Remover vacíos
        contacts = _viewModel.ContactService.RemoveEmptyContacts(contacts);

        int removed = before - contacts.Count;

        // Actualizar grid
        _viewModel.ContactService.LoadToDataGridView(contactsdgv, contacts);

        MessageBox.Show(
            $"Eliminadas {removed} filas vacías.\n\nRestantes: {contacts.Count}",
            "Filas Vacías Eliminadas",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error: {ex.Message}", "Error");
    }
}
```

---

## 🔧 Troubleshooting {#troubleshooting}

### **Problema 1: "No se puede usar _viewModel"**

**Síntoma:**
```
Error CS0103: El nombre '_viewModel' no existe en el contexto actual
```

**Solución:**
Verifica que en el constructor de WAButtfrm esté:

```csharp
public WAButtfrm()
{
    // DEBE ESTAR ESTO:
    _viewModel = new MainViewModel();

    // ... resto del código
}
```

---

### **Problema 2: "ContactService no existe"**

**Síntoma:**
```
Error CS0117: 'MainViewModel' no contiene una definición para 'ContactService'
```

**Solución:**
1. Verifica que `ContactService.cs` esté en `Presentation/Services/`
2. Verifica que `Presentation.csproj` incluya:
   ```xml
   <Compile Include="Services\ContactService.cs" />
   ```
3. Rebuild del proyecto

---

### **Problema 3: "using Presentation.Services' falta"**

**Síntoma:**
```
Error: El tipo o el nombre del espacio de nombres 'Services' no existe
```

**Solución:**
Agregar al inicio de WAButt.cs:

```csharp
using Presentation.Models;
using Presentation.Services;
using Presentation.Helpers;
using Presentation.ViewModels;
```

---

### **Problema 4: Contactos no se guardan**

**Síntoma:**
- No da error
- Pero no se crea el archivo JSON

**Solución:**
1. Verifica que exista la carpeta:
   ```
   C:\Users\[TuUsuario]\Documents\tempfilesWAButt\
   ```

2. Verifica permisos de escritura

3. Agrega debugging:
   ```csharp
   var contacts = _viewModel.ContactService.ConvertFromDataGridView(contactsdgv);
   Console.WriteLine($"Guardando {contacts.Count} contactos");
   _viewModel.ContactService.SaveContactsToJson(contacts);
   Console.WriteLine("Guardado exitoso");
   ```

---

### **Problema 5: Settings no se cargan al iniciar**

**Síntoma:**
- Settings guardados correctamente
- Pero al abrir app, están en default

**Solución:**
Verifica que en `ExecuteStart()` se llame:

```csharp
private void ExecuteStart()
{
    // ...código...

    OpenSettings();  // ← ESTO DEBE ESTAR
}
```

---

## ✅ Checklist de Verificación Post-Migración

Después de migrar cada funcionalidad, verifica:

- [ ] ✅ Compila sin errores
- [ ] ✅ No hay warnings nuevos
- [ ] ✅ Funcionalidad probada manualmente
- [ ] ✅ Métodos antiguos eliminados (o comentados)
- [ ] ✅ Commit hecho con mensaje descriptivo
- [ ] ✅ Reducción de líneas documentada

---

## 📈 Tracking de Progreso

Usa esta tabla para trackear tu progreso:

| Funcionalidad | Original | Migrado | Líneas Reducidas | Status |
|---------------|----------|---------|------------------|--------|
| CheckForInternetConnection | 12 | 0 | 12 | ✅ |
| SafeInt | 1 | 0 | 1 | ✅ |
| IsDigitsOnly | 3 | 0 | 3 | ✅ |
| GetImageState/GetVideoState | 15 | 0 | 15 | ✅ |
| Storecontaacts | 50 | 3 | 47 | ⏳ Pendiente |
| ReadJsonContacts | 40 | 2 | 38 | ⏳ Pendiente |
| DeleteDuplicate1 | 30 | 10 | 20 | ⏳ Pendiente |
| StoreSettings | 80 | 20 | 60 | ⏳ Pendiente |
| OpenSettings | 60 | 15 | 45 | ⏳ Pendiente |
| ValidateAPIKeyAsync | 100 | 2 | 98 | ⏳ Pendiente |
| ... | ... | ... | ... | ... |

**Total reducido hasta ahora: ~31 líneas**
**Objetivo: ~3,900 líneas**

---

¡Buena suerte con la migración! 🚀
