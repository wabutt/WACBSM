using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.IO;
using System.Diagnostics;
using CsvHelper;
using System.Globalization;
using OpenQA.Selenium.Interactions;
using Keys = OpenQA.Selenium.Keys;
using Domain;
using System.Net;
using AutoUpdaterDotNET;
using System.Configuration;
using ICSharpCode.SharpZipLib.Zip;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using CsvHelper.Configuration;
using System.Net.Http;
using Presentation.Models;
using Presentation.Services;
using Presentation.Helpers;
using Presentation.ViewModels;

namespace Presentation
{
    public partial class WAButtfrm : Form
    {
        // ViewModel for application logic
        private MainViewModel _viewModel;

        public WA wa = new WA();

        public static string filenameextracted = string.Empty;
        public string filetype;
        public static StringBuilder strex;

        private CancellationTokenSource cancellationToken;
        private CancellationTokenSource pauseToken;
        private CancellationTokenSource eachmessagetoken;
        private CancellationTokenSource severalpausetoken;

        private CancellationTokenSource cancellationToken2;
        private CancellationTokenSource pauseToken2;
        private CancellationTokenSource eachmessagetoken2;
        private CancellationTokenSource severalpausetoken2;

        private int pausetiming = 0;
        private int pausetiming2 = 0;
        private bool stopbtnclicked;
        private bool stopbtnclicked2;

        private int sendedmessage;
        private int sendedmessage2;
        private int notsendedmessage;
        private int notsendedmessage2;
        private int rowcount;
        private int rowcount2;
        private int eachmessagetiming = 0;
        private int eachmessagetiming2 = 0;

        public string chromedriverversion;
        public string chromedriverdwlink;

        public static string chromewadefaultuserdata = "https://raw.githubusercontent.com/wabutt/itsmevsauce/master/Chrome%20WA%20Profile.zip";
        public static string chromesmsdefaultuserdata = "https://raw.githubusercontent.com/wabutt/itsmevsauce/master/Chrome%20SMS%20Profile.zip";

        // ---- LICENSE STUFF START ----
        private string _licenseKey;

        public WAButtfrm()
        {
            _viewModel = new MainViewModel();

            // ChromeDriver en background
            Task.Run(async () =>
            {
                if (!await _viewModel.ChromeDriverService.EnsureChromeDriverAsync())
                {
                    this.Invoke((MethodInvoker)delegate {
                        Application.Exit();
                    });
                    return;
                }
            });

            CheckUserProfileExist();
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            try
            {
                apptab.Visible = false;
            }
            catch (NullReferenceException ex) when (ex.Source == "XanderUI.dll")
            {
                Console.WriteLine("⚠ XanderUI NullRef (ignorado)");
            }

            ExecuteStart();

            this.Load += WAButtfrm_Load;
        }

        private async void WAButtfrm_Load(object sender, EventArgs e)
        {
            // Cargar licencia y mostrar en título
            string licenseKey = LicenseStorage.LoadLicense();

            if (!string.IsNullOrWhiteSpace(licenseKey))
            {
                try
                {
                    var result = await _viewModel.LicenseService.ValidateAPIKeyAsync(licenseKey);

                    if (result != null && result.IsValid)
                    {
                        string plan = string.IsNullOrWhiteSpace(result.Plan) ? "SIN PLAN" : result.Plan.ToUpper();
                        string expText = "Sin vencimiento";
                        if (result.ExpiresAt.HasValue)
                        {
                            expText = "Vence: " + result.ExpiresAt.Value.ToShortDateString();
                        }
                        this.Text = $"WAButt - Licencia: {plan} - {expText}";
                    }
                }
                catch { }
            }

            // Mostrar formulario
            try
            {
                apptab.Visible = true;
            }
            catch (NullReferenceException ex) when (ex.Source == "XanderUI.dll")
            {
                Console.WriteLine("⚠ XanderUI NullRef (ignorado)");
            }
        }




        private void AutoUpdater_ApplicationExitEvent()
        {
            Console.WriteLine("Cerrando aplicación para actualizar...");

            try
            {
                wa?.CloseWDriver();
                wa?.CloseWDriver2();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        

       
        /// <summary>
        /// Simple modal dialog to ask user for the license key.
        /// </summary>
        

        private Version GetCurrentVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        }



        private async Task SendDocument(string filePath, string message, Actions action, string contactNumber)
        {
            wa.ContactFile(filePath);
            wa.ContactSend(By.XPath(WA.SendIADButton));
            await Task.Delay(1000 + wa.preventblocktiming, cancellationToken.Token);

            if (!CheckAttachMessageStatus())
            {
                // Send message separately
                wa.ClickSearchIcon();
                wa.ContactSearch(contactNumber);
                action.SendKeys(Keys.Space).Build().Perform();
                wa.ContactClick();
                await Task.Delay(1000, cancellationToken.Token);

                wa.ContactMessage(message);
                wa.ContactActionEnter();
            }
        }
        private async Task SendMessageOrFile(string message, string filePath, string contactNumber)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                // Send text only
                await SendTextMessage(message);
            }
            else
            {
                // Send with attachment
                await SendWithAttachment(message, filePath, contactNumber);
            }
        }
        private async Task SendTextMessage(string message)
        {
            await Task.Run(async () =>
            {
                if (pausetiming != 0)
                {
                    await pausetimingaction(pausetiming, pauseToken.Token);
                    pausetiming = 0;
                }

                try
                {
                    Actions action = new Actions(WA.driver);

                    action.SendKeys("a").Build().Perform();
                    action.SendKeys(Keys.Backspace).Build().Perform();
                    await Task.Delay(500, cancellationToken.Token);

                    wa.ContactMessage(message);
                    await Task.Delay(1000 + wa.preventblocktiming, cancellationToken.Token);

                    wa.ContactActionEnter();
                    Console.WriteLine("✓ Text message sent");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending text: {ex.Message}");
                }
            }, cancellationToken.Token);
        }
       
        private async Task SendImageOrVideo(string filePath, string message, Actions action, string contactNumber)
        {
            if (!CheckAttachMessageStatus())
            {
                wa.ImageMessage(filePath);
                await Task.Delay(1000 + wa.preventblocktiming, cancellationToken.Token);
                wa.ContactSend(By.XPath(WA.SendIADButton));
            }
            else
            {
                if (FileHelper.IsImageFile(filePath) || FileHelper.IsVideoFile(filePath))
                {
                    wa.ImageTextMessage(filePath, message);
                    action.SendKeys(".").Build().Perform();
                    action.SendKeys(Keys.Backspace).Build().Perform();
                    await Task.Delay(1000 + wa.preventblocktiming, cancellationToken.Token);
                    wa.ContactSend(By.XPath(WA.SendIADButton));
                }
                else
                {
                    // Send file, then message separately
                    wa.ImageMessage(filePath);
                    await Task.Delay(1000 + wa.preventblocktiming, cancellationToken.Token);
                    wa.ContactSend(By.XPath(WA.SendIADButton));
                    await Task.Delay(2000, cancellationToken.Token);

                    // Re-search and send message
                    wa.ClickSearchIcon();
                    wa.ContactSearch(contactNumber);
                    action.SendKeys(Keys.Space).Build().Perform();
                    wa.ContactClick();
                    await Task.Delay(1000, cancellationToken.Token);

                    wa.ContactMessage(message);
                    wa.ContactActionEnter();
                }
            }
        }
        private async Task SearchAndClickContact(string contactNumber)
        {
            await Task.Run(async () =>
            {
                if (pausetiming != 0)
                {
                    await pausetimingaction(pausetiming, pauseToken.Token);
                    pausetiming = 0;
                }

                try
                {
                    WA.driver.Manage().Window.Size = new Size(850, 650);

                    Actions action = new Actions(WA.driver);

                    wa.ClickSearchIcon();
                    wa.ContactSearch(contactNumber);
                    action.SendKeys(Keys.Space).Build().Perform();
                    wa.ContactClick();

                    await Task.Delay(2000, cancellationToken.Token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error searching contact: {ex.Message}");
                }
            }, cancellationToken.Token);
        }
        private async Task SendWithAttachment(string message, string filePath, string contactNumber)
        {
            await Task.Run(async () =>
            {
                if (pausetiming != 0)
                {
                    await pausetimingaction(pausetiming, pauseToken.Token);
                    pausetiming = 0;
                }

                try
                {
                    Actions action = new Actions(WA.driver);

                    if (filetype == "I") // Image/Video
                    {
                        await SendImageOrVideo(filePath, message, action, contactNumber);
                    }
                    else if (filetype == "A") // Audio
                    {
                        await SendAudio(filePath, message, action, contactNumber);
                    }
                    else if (filetype == "D") // Document
                    {
                        await SendDocument(filePath, message, action, contactNumber);
                    }

                    Console.WriteLine($"✓ Attachment sent: {filetype}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending attachment: {ex.Message}");
                }
            }, cancellationToken.Token);
        }

        private async Task SendAudio(string filePath, string message, Actions action, string contactNumber)
        {
            wa.ContactFileAudio(filePath);
            wa.ContactSend(By.XPath(WA.SendIADButton));
            await Task.Delay(1000 + wa.preventblocktiming, cancellationToken.Token);

            if (!CheckAttachMessageStatus())
            {
                // Send message separately
                wa.ClickSearchIcon();
                wa.ContactSearch(contactNumber);
                action.SendKeys(Keys.Space).Build().Perform();
                wa.ContactClick();
                await Task.Delay(1000, cancellationToken.Token);

                wa.ContactMessage(message);
                wa.ContactActionEnter();
            }
        }                               
        private void OpenSaved()
        {
            Restoremessages();

            string contactsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "tempfilesWAButt", "Contacts.json"
            );

            if (File.Exists(contactsPath) && new FileInfo(contactsPath).Length > 23)
            {
                if (MessageBox.Show("¿Desea cargar los últimos contactos de WhatsApp?",
                    "Observación", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        var contacts = _viewModel.ContactService.LoadContactsFromJson(isSMS: false);
                        _viewModel.ContactService.LoadToDataGridView(contactsdgv, contacts);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error");
                    }
                }
            }
        }
        private void OpenSaved2()
        {
            Restoremessages2();

            string contactsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "tempfilesWAButt", "Contacts2.json"
            );

            if (File.Exists(contactsPath) && new FileInfo(contactsPath).Length > 23)
            {
                if (MessageBox.Show("¿Desea cargar los últimos contactos de Google SMS?",
                    "Observación", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    try
                    {
                        var contacts = _viewModel.ContactService.LoadContactsFromJson(isSMS: true);
                        _viewModel.ContactService.LoadToDataGridView(contacts2dgv, contacts);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error");
                    }
                }
            }
        }

        public void controls(bool state)
        {

            startbtn.Enabled = state;
            uploadbtn.Enabled = state;
            clearfilenamebtn.Enabled = state;
        }
        private void controls2(bool state)
        {

            start2btn.Enabled = state;
        }

        private void WABotfrm_FormClosing(object sender, FormClosingEventArgs e)
        {

            if (apptab.Visible)
            {
                if (MessageBox.Show("¿Estás seguro de salir?", "Confirmación",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Storecontaacts();
                    Storemessages();
                    StoreSettings();
                    wa.CloseWDriver();
                    wa.CloseWDriver2();
                }
                else
                {
                    e.Cancel = true;
                }
            }
            



        }
        private void ResetUIAfterStop()
        {
            pausebtn.Enabled = false;
            stopbtn.Enabled = false;
            startbtn.Enabled = true;
            uploadbtn.Enabled = true;
            clearfilenamebtn.Enabled = true;
            logoutbtn.Enabled = true;
            connectwabtn.Enabled = true;
        }
        private void StoreSettings()
        {
            DataTable dt = new DataTable();

            dt.Columns.Add("waeachmsgpausecant", typeof(string));
            dt.Columns.Add("wafullname", typeof(string));
            dt.Columns.Add("waevitblock", typeof(string));
            dt.Columns.Add("wasenddt", typeof(string));
            dt.Columns.Add("wasendmanymsg", typeof(string));
            dt.Columns.Add("waseveralpausecant", typeof(string));
            dt.Columns.Add("wafileatt", typeof(string));
            dt.Columns.Add("smseachmsgpausecant", typeof(string));
            dt.Columns.Add("smsfullname", typeof(string));
            dt.Columns.Add("smsevitblock", typeof(string));
            dt.Columns.Add("smssenddt", typeof(string));
            dt.Columns.Add("smssendmanymsg", typeof(string));
            dt.Columns.Add("smsseveralpausecant", typeof(string));
            dt.Columns.Add("filetype", typeof(string));
            dt.Columns.Add("sendonlyattach", typeof(string));

            DataRow row = dt.NewRow();
            row[0] = eachmessagetimingtxt.Text;
            row[1] = sendfullnamecb.Checked.ToString();
            row[2] = preventblockcb.Checked.ToString();
            row[3] = senddatetimecb.Checked.ToString();
            row[4] = manymessagescb.Checked.ToString();
            row[5] = severalpausetxt.Text;
            row[6] = filenametxt.Text;
            row[7] = eachmessagetiming2txt.Text;
            row[8] = sendfullname2cb.Checked.ToString();
            row[9] = preventblock2cb.Checked.ToString();
            row[10] = senddatetime2cb.Checked.ToString();
            row[11] = manymessages2cb.Checked.ToString();
            row[12] = severalpause2txt.Text;
            row[13] = filetype;
            row[14] = sendonlyattachcb.Checked.ToString();

            dt.Rows.Add(row);

            string json = JsonConvert.SerializeObject(dt);
            FileHelper.WriteJsonToFile(json, "UserSettings.json");
        }
        private void SetDefaultSettings()
        {
            eachmessagetimingtxt.Text = "30";
            severalpausetxt.Text = "300";
            eachmessagetiming2txt.Text = "30";
            severalpause2txt.Text = "300";
        }
        private void OpenSettings()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "tempfilesWAButt", "UserSettings.json"
            );

            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    DataTable dt = JsonConvert.DeserializeObject<DataTable>(json);

                    eachmessagetimingtxt.Text = Convert.ToString(dt.Rows[0][0]);
                    sendfullnamecb.Checked = Convert.ToBoolean(dt.Rows[0][1]);
                    preventblockcb.Checked = Convert.ToBoolean(dt.Rows[0][2]);
                    senddatetimecb.Checked = Convert.ToBoolean(dt.Rows[0][3]);
                    manymessagescb.Checked = Convert.ToBoolean(dt.Rows[0][4]);
                    severalpausetxt.Text = Convert.ToString(dt.Rows[0][5]);
                    filenametxt.Text = Convert.ToString(dt.Rows[0][6]);

                    eachmessagetiming2txt.Text = Convert.ToString(dt.Rows[0][7]);
                    sendfullname2cb.Checked = Convert.ToBoolean(dt.Rows[0][8]);
                    preventblock2cb.Checked = Convert.ToBoolean(dt.Rows[0][9]);
                    senddatetime2cb.Checked = Convert.ToBoolean(dt.Rows[0][10]);
                    manymessages2cb.Checked = Convert.ToBoolean(dt.Rows[0][11]);
                    severalpause2txt.Text = Convert.ToString(dt.Rows[0][12]);

                    filetype = Convert.ToString(dt.Rows[0][13]);
                    sendonlyattachcb.Checked = Convert.ToBoolean(dt.Rows[0][14]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading settings: {ex.Message}");
                    SetDefaultSettings();
                }
            }
            else
            {
                SetDefaultSettings();
            }
        }
        private async Task DelayBetweenMessages()
        {
            if (eachmessagetiming > 0)
            {
                await _viewModel.TimingService.ApplyDelayAsync(
                    eachmessagetiming,
                    eachmessagetoken.Token,
                    pauseToken.Token);
            }
        }
        private void uploadbtn_Click(object sender, EventArgs e)
        {

            cmsupload.Show(Cursor.Position.X, Cursor.Position.Y);


        }

        private void imagenYVideoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Archivos Personalizados " +


                "(*.tif;*.pjp;*xbm;*jxl;*.svgz;*.jpg;*.jpeg;*.ico;*.tiff;*.gif;*.svg;*.jfif;*.webp;*.png;*.bmp;*.pjpeg;*.avif;*.m4v;*.mp4;*.3gpp;*.mov) | " +
                "*.tif;*.pjp;*xbm;*jxl;*.svgz;*.jpg;*.jpeg;*.ico;*.tiff;*.gif;*.svg;*.jfif;*.webp;*.png;*.bmp;*.pjpeg;*.avif;*.m4v;*.mp4;*.3gpp;*.mov";

            //Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                var size = new FileInfo(ofd.FileName).Length;
                string filename = ofd.FileName;


                if (size < 67108864)
                {
                    filenametxt.Text = filename;
                    filetype = "I";
                }
                else
                {
                    filenametxt.Clear();
                    MessageBox.Show("El tamaño del archivo supera el maximo permitido por WhatsApp de 64 MB", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }


            }
        }

        private void audioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();



            if (ofd.ShowDialog() == DialogResult.OK)
            {
                var size = new FileInfo(ofd.FileName).Length;
                string filename = ofd.FileName;


                if (size < 67108864)
                {
                    filenametxt.Text = filename;

                    filetype = "A";
                }
                else
                {
                    filenametxt.Clear();

                    MessageBox.Show("El tamaño del archivo supera el maximo permitido por WhatsApp de 64 MB", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }




        }

        private void documentoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();



            if (ofd.ShowDialog() == DialogResult.OK)
            {
                var size = new FileInfo(ofd.FileName).Length;
                string filename = ofd.FileName;


                if (size < 67108864)
                {
                    filenametxt.Text = filename;

                    filetype = "D";
                }
                else
                {
                    filenametxt.Clear();

                    MessageBox.Show("El tamaño del archivo supera el maximo permitido por WhatsApp de 64 MB", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }




        private void emojibtn_Click(object sender, EventArgs e)
        {
            Process.Start("https://es.piliapp.com/twitter-symbols/");
        }

        private async void connectwabtn_Click(object sender, EventArgs e)
        {

            if (!InternetHelper.CheckForInternetConnection())
            {
                MessageBox.Show("No cuenta con acceso a internet.", "Error");
                return;
            }

            wa.CloseWDriver();

            try
            {
                loadmessagelbl.Text = "Estado: Conectando...";
                await wa.LaunchBrowser();

                if (wa.driverstate)
                {
                    loadmessagelbl.Text = "Estado: Navegador Abierto, escanee código QR";
                    controls(true);
                    logoutbtn.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }




        }

        private void ExportDgvToGmailCore(DataGridView grid)
        {
            const int PhoneColIndex = 0;      // Columna con el número
            const int FirstNameColIndex = 1;  // Columna con el nombre

            if (grid == null || grid.Rows.Cast<DataGridViewRow>().All(r => r.IsNewRow))
            {
                MessageBox.Show("No hay datos a exportar", "Observación",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            using (var sfd = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = "contactos_google.csv",
                AddExtension = true,
                OverwritePrompt = true
            })
            {
                if (sfd.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    using (var sw = new StreamWriter(sfd.FileName, false, new UTF8Encoding(true)))
                    {
                        // Header reconocido por Google Contacts (como tu ejemplo)
                        sw.WriteLine(
                            "First Name,Middle Name,Last Name,Phonetic First Name,Phonetic Middle Name,Phonetic Last Name," +
                            "Name Prefix,Name Suffix,Nickname,File As,Organization Name,Organization Title,Organization Department," +
                            "Birthday,Notes,Photo,Labels,Phone 1 - Label,Phone 1 - Value"
                        );

                        foreach (DataGridViewRow row in grid.Rows)
                        {
                            if (row.IsNewRow) continue;

                            string name = Convert.ToString(row.Cells[FirstNameColIndex].Value) ?? "";
                            string phone = Convert.ToString(row.Cells[PhoneColIndex].Value) ?? "";

                            name = name.Trim();
                            phone = phone.Trim();

                            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(phone))
                                continue;

                            // NO normalizar: se manda tal cual.
                            // Solo evitamos teléfonos vacíos (porque Google lo ignora)
                            if (string.IsNullOrWhiteSpace(phone))
                                continue;

                            sw.WriteLine(string.Join(",",
                                Csv(name),      // First Name
                                Csv(""), Csv(""), Csv(""), Csv(""), Csv(""), // Middle/Last/Phonetic*
                                Csv(""), Csv(""), Csv(""), Csv(""),         // Prefix/Suffix/Nickname/File As
                                Csv(""), Csv(""), Csv(""),                  // Org Name/Title/Dept
                                Csv(""), Csv(""), Csv(""),                  // Birthday/Notes/Photo
                                Csv("* myContacts"),                         // Labels (igual a tu ejemplo)
                                Csv("Mobile"),                               // Phone 1 - Label
                                Csv(phone)                                   // Phone 1 - Value
                            ));
                        }
                    }

                    MessageBox.Show("CSV listo para importar en Google Contacts.", "OK",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (IOException ex)
                {
                    MessageBox.Show("No fue posible escribir datos en el disco. " + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static string Csv(string value)
        {
            if (value == null) return "\"\"";
            value = value.Replace("\"", "\"\"");
            return $"\"{value}\"";
        }


       
        private void exportDgvToGmail()
        {
            ExportDgvToGmailCore(contactsdgv);
        }

        // ---- Helper ----
        // No alteramos el contenido; solo hacemos el escape CSV cuando hace falta.
       
        private void exportDgvToGmail2()
        {
            ExportDgvToGmailCore(contacts2dgv);
        }
        private void ImportGmailToDgv()
        {
            ImportGmailToDgvCore(contactsdgv, maintab, contactlisttab);
        }




        // ===== Helpers =====

        // Detecta ',' o ';' en el encabezado, ignorando separadores dentro de comillas.
        private static string DetectDelimiter(string path)
        {
            using (var sr = new StreamReader(path, Encoding.UTF8, true))
            {
                var header = sr.ReadLine() ?? string.Empty;
                bool inQuotes = false;
                int commas = 0, semicolons = 0;

                foreach (char c in header)
                {
                    if (c == '"') inQuotes = !inQuotes;
                    else if (!inQuotes)
                    {
                        if (c == ',') commas++;
                        else if (c == ';') semicolons++;
                    }
                }

                // Si ambos son 0 (CSV raro), por defecto coma
                if (commas == 0 && semicolons == 0) return ",";
                return (semicolons > commas) ? ";" : ",";
            }
        }

        // Devuelve el primer nombre de columna existente (case-insensitive, trim)
        private static string FirstExisting(System.Collections.Generic.IList<string> headers, params string[] candidates)
        {
            foreach (var c in candidates)
            {
                var match = headers.FirstOrDefault(h =>
                    string.Equals(Norm(h), Norm(c), StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }
            return null;
        }

        // Normaliza un nombre de encabezado para comparación flexible
        private static string Norm(string s)
        {
            if (s == null) return string.Empty;
            s = s.Trim();
            // Quitamos espacios y guiones para tolerar variantes ("Phone 1 - Value")
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (!char.IsWhiteSpace(ch) && ch != '-') sb.Append(ch);
            }
            return sb.ToString();
        }

        private void ImportGmailToDgvCore(DataGridView grid, TabControl tabControl, TabPage tabPage)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = ""
            };

            if (ofd.ShowDialog() != DialogResult.OK) return;
            if (!File.Exists(ofd.FileName)) return;

            try
            {
                // Muestra la pestaña de la lista
                tabControl.SelectedTab = tabPage;

                // Detectar delimitador en el header (',' o ';')
                var delimiter = DetectDelimiter(ofd.FileName);

                var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    Delimiter = delimiter,
                    IgnoreBlankLines = true,
                    TrimOptions = TrimOptions.Trim,
                    BadDataFound = null,
                    MissingFieldFound = null,
                    HeaderValidated = null
                };

                using (var sr = new StreamReader(ofd.FileName, Encoding.UTF8, true))
                using (var csv = new CsvReader(sr, cfg))
                {
                    // Leer encabezadosw
                    csv.Read();
                    csv.ReadHeader();

                    // Cabeceras según la versión de CsvHelper
                    var headers = csv.Context.Reader.HeaderRecord?.ToList() ?? new List<string>();

                    // Buscar columnas por posibles nombres
                    string phoneCol = FirstExisting(headers,
                        "Phone 1 - Value", "Primary Phone", "Mobile Phone", "Phone",
                        "Teléfono 1 - Valor", "Teléfono principal");

                    string nameCol = FirstExisting(headers,
                        "First Name", "Given Name", "Name", "Nombre");

                    // Validaciones mínimas
                    if (string.IsNullOrEmpty(phoneCol) && string.IsNullOrEmpty(nameCol))
                        throw new InvalidOperationException("No se encontraron columnas de teléfono ni nombre en el CSV.");

                    // Preparar el DataGridView
                    grid.SuspendLayout();
                    grid.Columns.Clear();
                    grid.Rows.Clear();

                    grid.Columns.Add("colPhoneOrGroup", "Numero o Grupo");
                    grid.Columns.Add("colName", "Nombre");
                    var colSent = grid.Columns.Add("colSent", "Enviado (S/N)");
                    grid.Columns[colSent].ReadOnly = true;

                    grid.Columns[0].Width = 200;
                    grid.Columns[1].Width = 350;
                    grid.Columns[2].Width = 100;

                    // Leer filas
                    while (csv.Read())
                    {
                        string phone = phoneCol != null ? (csv.GetField(phoneCol) ?? string.Empty) : string.Empty;
                        string first = nameCol != null ? (csv.GetField(nameCol) ?? string.Empty) : string.Empty;

                        // Si no hay ningún dato útil, saltamos
                        if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(first))
                            continue;

                        // Se agregan tal cual
                        grid.Rows.Add(phone, first, string.Empty);
                    }

                    grid.ResumeLayout();
                    MessageBox.Show("Datos importados!", "Observación",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportGmailToDgv2()
        {
            ImportGmailToDgvCore(contacts2dgv, main2tab, contactlist2tab);
        }


        private async void startbtn_Click(object sender, EventArgs e)
        {
            cancellationToken = new CancellationTokenSource();
            pauseToken = new CancellationTokenSource();
            eachmessagetoken = new CancellationTokenSource();
            severalpausetoken = new CancellationTokenSource();

            await ExecuteSendTask();

        }
        private string PrepareMessage(string contactName)
        {
            // ✅ NO convertir \n a <br/>
            List<string> messages = new List<string>
    {
        m1txt.Text,
        m2txt.Text,
        m3txt.Text,
        m4txt.Text,
        m5txt.Text
    };

            string message;

            if (manymessagescb.Checked)
            {
                List<int> nonEmptyIndices = NotEmptyMessages();
                if (nonEmptyIndices.Count > 0)
                {
                    Random rnd = new Random();
                    int randomIndex = nonEmptyIndices[rnd.Next(nonEmptyIndices.Count)];
                    message = messages[randomIndex];
                }
                else
                {
                    message = messages[0];
                }
            }
            else
            {
                message = messages[0];
            }

            // Normalizar saltos de línea Windows → Unix
            message = message.Replace("\r\n", "\n").Replace("\r", "\n");

            // Replace placeholders
            if (sendfullnamecb.Checked)
            {
                message = Regex.Replace(message, @"\{nombre\}",
                    string.IsNullOrEmpty(contactName) ? "" : contactName);
            }

            if (senddatetimecb.Checked)
            {
                message = Regex.Replace(message, @"\{fecha\}",
                    DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm"));
            }

            return message;
        }
        private void PrepareForSending()
        {
            stopbtnclicked = false;
            rowcount = contactsdgv.RowCount - 1;
            sendpbr.Value = 0;
            sendpbr.Maximum = rowcount;
            totalmessageslbl.Text = rowcount.ToString();
            sendedmessage = 0;
            notsendedmessage = 0;

            notsendedmessagelbl.Text = "0";
            sendedmessagelbl.Text = "0";

            // UI Updates
            startbtn.Enabled = false;
            pausebtn.Enabled = true;
            stopbtn.Enabled = true;
            pausetiming = 0;
            logoutbtn.Enabled = false;
            uploadbtn.Enabled = false;
            clearfilenamebtn.Enabled = false;
            connectwabtn.Enabled = false;
            loadmessagelbl.Text = "Estado: Conectado . . .";
            contactsdgv.AllowUserToAddRows = false;
            contactsdgv.AllowUserToDeleteRows = false;

            // Configure timings
            wa.UseHumanTiming = preventblockcb.Checked;   // ON/OFF humano
            wa.UseProfileCycling = true;                 // usar perfiles aleatorios
            wa.ProfileChangeMinMessages = 10;            // cambia entre 40 y 90 mensajes
            wa.ProfileChangeMaxMessages = 50;

            wa.ResetDistractionSchedule();

            eachmessagetiming = eachmessagetimingcb.Checked
                ? ValidationHelper.SafeInt(eachmessagetimingtxt.Text) * 1000
                : 0;
        }

        private async Task<bool> ValidatePreSendConditions()
        {
            if (!InternetHelper.CheckForInternetConnection())
            {
                MessageBox.Show("No cuenta con acceso a internet, no puedes continuar.",
                    "Observación", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            ClearEmptyRows(contactsdgv);

            if (contactsdgv.Rows.Count < 2)
            {
                MessageBox.Show("No existen contactos a los que enviar!",
                    "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            if (wa.IsBrowserClosed())
            {
                MessageBox.Show("El navegador está cerrado, no se puede enviar mensajes!",
                    "Observación", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            if (!wa.IfConnected(By.XPath("//div[@contenteditable='true']")))
            {
                MessageBox.Show("Debe escanear el código QR para empezar a enviar", "Observación",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }


            return true;
        }
        private string PrepareSMSMessage(string contactName)
        {
            string message = sms1txt.Text;

            if (sendfullname2cb.Checked)
            {
                message = Regex.Replace(message, "{nombre}",
                    string.IsNullOrEmpty(contactName) ? "" : contactName);
            }

            if (senddatetime2cb.Checked)
            {
                message = Regex.Replace(message, "{fecha}",
                    DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
            }

            return message;
        }
        public void InputNumbers(object sender, KeyPressEventArgs e)
        {
            // Get the decimal symbol format defined in your regional settings.
            char decimalSeparator = Convert.ToChar(CultureInfo.CurrentCulture.
                        NumberFormat.NumberDecimalSeparator);
            // Check if pressed key is not a control key, digit key and decimal separator key.
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)
                      )
            {
                // Convert the sender to TextBox.
                TextBox toolTippedControl = sender as TextBox;
                // Create ToolTip parameters.
                string toolTipText = "La casilla solo puede contener los siguientes caracteres:"
                        + "\n\t- Numeros: 0123456789";
                int toolTipPosX = toolTippedControl.Width;
                int toolTipPosY = 0;
                int toolTipDuration = 4000;
                // Create a ToolTip object.
                ToolTip toolTip = new ToolTip
                {
                    // Set ToolTip icon.
                    ToolTipIcon = ToolTipIcon.Warning
                };
                // Pass the created ToolTip parameters and show it.
                toolTip.Show(toolTipText, toolTippedControl, toolTipPosX, toolTipPosY, toolTipDuration);
                // Set Handled method to true to cancel the button press.
                e.Handled = true;
            }
            // Decimal separator symbol must be only one, so:
            // Check if the decimal separator key is pressed.
            // And check if the TextBox already have entered symbol for decimal separator.
            if ((e.KeyChar == decimalSeparator) &&
                    ((sender as TextBox).Text.IndexOf(decimalSeparator) > -1))
            {
                // Set Handled method to true to cancel the button press.
                e.Handled = true;
            }
        }
        private async Task HandlePausePoints(int currentIndex)
        {
            if (string.IsNullOrEmpty(severalpausetxt.Text)) return;

            int pauseEvery = ValidationHelper.SafeInt(severalpausetxt.Text);

            if (currentIndex == pauseEvery && !severalpausetoken.IsCancellationRequested)
            {
                MessageBox.Show(
                    $"Pausa automática después de {pauseEvery} mensajes.\nEsperando 15 minutos...",
                    "Pausa", MessageBoxButtons.OK, MessageBoxIcon.Information
                );

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(15), severalpausetoken.Token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Pause cancelled");
                }
            }
        }
        private void stopbtn_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("¿Desea detener los envíos?", "Confirmación",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                pauseToken?.Cancel();
                cancellationToken?.Cancel();
                eachmessagetoken?.Cancel();
                severalpausetoken?.Cancel();

                pausetiming = 0;
                stopbtnclicked = true;
                pausebtn.Text = "Pausar";

                ResetUIAfterStop();
            }

        }

        private void pausebtn_Click(object sender, EventArgs e)
        {
            if (pausetiming > 0)
            {
                pausebtn.Text = "Pausar";
                pauseToken?.Cancel();
                pausebtn.Enabled = true;
                stopbtn.Enabled = true;
            }
            else
            {
                cmspause.Show(Cursor.Position.X, Cursor.Position.Y);
            }

        }
        
        


        #region  TASK 2 SMS
        private async Task Excecutesendtask2()
        {

            if (InternetHelper.CheckForInternetConnection())
            {

                //condicionales y token de cancellation



                string actualnumber = "";
                string actualname = "";




                //variables


                stopbtnclicked2 = false;
                rowcount2 = contacts2dgv.RowCount - 1;
                send2pbr.Value = 0;
                send2pbr.Maximum = rowcount2;
                totalmessages2lbl.Text = rowcount2.ToString();
                int count = 0;

                sendedmessage2 = 0;
                notsendedmessage2 = 0;




                notsendedmessage2lbl.Text = sendedmessage2.ToString();
                sendedmessage2lbl.Text = notsendedmessage2.ToString();





                if (contacts2dgv.Rows.Count < 2) { MessageBox.Show("No existen contactos a los que enviar SMS!", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                else
                {


                    if (sms1txt.Text == "") { MessageBox.Show("No existe mensaje para enviar SMS!.\nUtilice el espacio de <Mensaje 1>", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                    else
                    {





                        if (wa.IsBrowserClosed2() == false)
                        {
                            if (!wa.IfConnected2(By.XPath("//div[contains(text(),'Iniciar chat')]")))
                            {
                                DialogResult d;
                                d = MessageBox.Show("Debe escanear el codigo QR para empezar a enviar SMS", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);


                            }


                            else
                            {

                                start2btn.Enabled = false;
                                pause2btn.Enabled = true;
                                stop2btn.Enabled = true;
                                pausetiming = 0;
                                logout2btn.Enabled = false;

                                connectgoobtn.Enabled = false;


                                int countmessage = 0;
                                loadmessage2lbl.Text = "Estado: Conectado . . .";
                                bool activatemanymessages = false;
                                contacts2dgv.AllowUserToAddRows = false;
                                contacts2dgv.AllowUserToDeleteRows = false;



                                if (manymessages2cb.Checked)
                                {
                                    if (sms2txt.Text == "" || sms3txt.Text == "" || sms4txt.Text == "" || sms5txt.Text == "")
                                    {
                                        manymessages2cb.Checked = false;
                                        MessageBox.Show("No llenó todos los espacios de mensajes, no se usará la opción <Enviar varios textos en un solo envío>", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);



                                    }
                                }



                                try
                                {
                                    foreach (DataGridViewRow fila in contacts2dgv.Rows)
                                    {
                                        if (fila.IsNewRow) continue;

                                        // Check for cancellation
                                        cancellationToken2.Token.ThrowIfCancellationRequested();

                                        if (InternetHelper.CheckForInternetConnection())
                                    {

                                        wa.preventblocktiming2 = preventblock2cb.Checked ? 4000 : 0;




                                        eachmessagetiming2 = eachmessagetiming2cb.Checked
                                            ? ValidationHelper.SafeInt(eachmessagetiming2txt.Text) * 1000
                                            : 0;







                                        if (Convert.ToString(fila.Cells[0].Value) != string.Empty)
                                        { actualnumber = Convert.ToString(fila.Cells[0].Value); Console.WriteLine(actualnumber); }
                                        else { actualnumber = "numero vacio"; Console.WriteLine(actualnumber); }



                                        if (Convert.ToString(fila.Cells[1].Value) != string.Empty)
                                        { actualname = Convert.ToString(fila.Cells[1].Value); Console.WriteLine(actualname); }
                                        else { actualname = "nombre vacio"; Console.WriteLine(actualname); }




                                        //mensajes

                                        string m1 = Regex.Replace(sms1txt.Text, "\n", "\\n");
                                        string m2 = Regex.Replace(sms1txt.Text, "\n", "\\n");
                                        string m3 = Regex.Replace(sms1txt.Text, "\n", "\\n");
                                        string m4 = Regex.Replace(sms1txt.Text, "\n", "\\n");
                                        string m5 = Regex.Replace(sms1txt.Text, "\n", "\\n");

                                        var messages = new List<string>
                                        {
                                            m1,
                                            m2,
                                            m3,
                                            m4,
                                            m5
                                        };



                                        string actualmessagetosend = "";


                                        //bucle de mensajes 2, 3, 4 , 5 yremplazo con regards y goodbyes



                                        if (manymessages2cb.Checked == true && activatemanymessages == true)
                                        {
                                            countmessage++;




                                            if (countmessage == 5)
                                            {
                                                countmessage = 0;

                                                actualmessagetosend = actualmessagetosend + messages[0];


                                                if (sendfullname2cb.Checked == true || actualmessagetosend.Contains("{nombre}"))
                                                {

                                                    if (actualname == "nombre vacio")
                                                    {
                                                        actualmessagetosend = Regex.Replace(actualmessagetosend, "{nombre}", "");
                                                    }

                                                    else
                                                    {
                                                        actualmessagetosend = Regex.Replace(actualmessagetosend, "{nombre}", actualname);
                                                    }

                                                }
                                                if (senddatetime2cb.Checked || actualmessagetosend.Contains("{fecha}"))
                                                {
                                                    DateTime actualdate = getTimeNow();

                                                    actualmessagetosend = Regex.Replace(actualmessagetosend, "{fecha}", Convert.ToString(actualdate));

                                                }




                                                activatemanymessages = false;



                                            }
                                            else
                                            {


                                                actualmessagetosend = actualmessagetosend + messages[countmessage];

                                                if (sendfullname2cb.Checked || actualmessagetosend.Contains("{nombre}"))
                                                {

                                                    if (actualname == "nombre vacio")
                                                    {
                                                        actualmessagetosend = Regex.Replace(actualmessagetosend, "{nombre}", "");
                                                    }

                                                    else
                                                    {
                                                        actualmessagetosend = Regex.Replace(actualmessagetosend, "{nombre}", actualname);
                                                    }

                                                }
                                                if (senddatetime2cb.Checked || actualmessagetosend.Contains("{fecha}"))
                                                {
                                                    DateTime actualdate = getTimeNow();

                                                    actualmessagetosend = Regex.Replace(actualmessagetosend, "{fecha}", Convert.ToString(actualdate));

                                                }


                                            }

                                        }
                                        else
                                        {



                                            actualmessagetosend = actualmessagetosend + messages[0];


                                            if (sendfullname2cb.Checked || actualmessagetosend.Contains("{nombre}"))
                                            {

                                                if (actualname == "nombre vacio")
                                                {

                                                    actualmessagetosend = Regex.Replace(actualmessagetosend, "{nombre}", "");

                                                }

                                                else
                                                {
                                                    actualmessagetosend = Regex.Replace(actualmessagetosend, "{nombre}", actualname);
                                                }

                                            }

                                            if (senddatetime2cb.Checked || actualmessagetosend.Contains("{fecha}"))
                                            {
                                                DateTime actualdate = getTimeNow();

                                                actualmessagetosend = Regex.Replace(actualmessagetosend, "{fecha}", Convert.ToString(actualdate));

                                            }


                                        }


                                        try
                                        {



                                            loadmessage2lbl.Text = "";
                                            loadmessage2lbl.Text = "Estado: Conectado . . .";




                                            if (actualnumber != "numero vacio")
                                            {
                                                Console.WriteLine("el numero no esta vacio y paso a busca contacto en SMS");



                                                Console.WriteLine("entre a escribir");


                                                await Task.Run(() =>
                                                {


                                                    if (pausetiming2 != 0)
                                                    {
                                                        try
                                                        {
                                                            pausetimingaction(pausetiming2, pauseToken2.Token).Wait();
                                                            pausetiming2 = 0;
                                                        }
                                                        catch (Exception ex)
                                                        {

                                                            Console.WriteLine(ex.Message);
                                                        }

                                                    }


                                                    try
                                                    {



                                                        Actions action = new Actions(WA.driver2);


                                                        wa.ClickSearchIcon2();



                                                        //WA.driver2.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);

                                                        action.SendKeys(Keys.Space).Build().Perform();

                                                        wa.ContactSearch2(actualnumber);





                                                        // WA.driver2.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);

                                                        // Task.Delay(2000).Wait();

                                                        Console.WriteLine("doy click en el contacto");

                                                        wa.ContactClick2();



                                                        //WA.driver2.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);



                                                        Task.Delay(2000).Wait();


                                                        //Console.WriteLine(actualmessagetosend + "MESSAGEEEEEEEE SMS");


                                                        wa.ContactMessage2(actualmessagetosend);



                                                        Console.WriteLine("solo Mensaje escrito");

                                                        Task.Delay(1000 + wa.preventblocktiming2).Wait();

                                                        wa.ContactActionEnter2();

                                                        Console.WriteLine("presione enter para enviar");

                                                        fila.Cells[2].Value = "S";




                                                        Task.Delay(1000 + wa.preventblocktiming2).Wait();

                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Console.WriteLine(ex.Message);

                                                    }


                                                }, cancellationToken2.Token);






                                                sendedmessage2++;
                                                sendedmessage2lbl.Text = Convert.ToString(sendedmessage2);
                                                Console.WriteLine(sendedmessage2);

                                            }
                                            else
                                            {
                                                fila.Cells[2].Value = "N";
                                                notsendedmessage2++;
                                                notsendedmessage2lbl.Text = Convert.ToString(notsendedmessage2);
                                                Console.WriteLine(notsendedmessage2);
                                            }


                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine(ex.Message.ToString());


                                        }
                                    }

                                    else
                                    {


                                        stop2btn.Enabled = false;
                                        pause2btn.Enabled = false;
                                        start2btn.Enabled = true;
                                        MessageBox.Show("Se detuvieron los envios de SMS debido a que no cuenta con acceso a internet.", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Error);

                                        break;
                                    }

                                    count++;
                                    if (count <= rowcount2)
                                    {
                                        send2pbr.Value = count;
                                    }

                                    activatemanymessages = true;


                                    if (severalpause2txt.Text != "")
                                    {


                                        if (fila.Index == ValidationHelper.SafeInt(severalpause2txt.Text) && !severalpausetoken2.IsCancellationRequested)
                                        {

                                            Console.WriteLine("<<<<<<<<<<<<<<<<<<<este es la cuenta ctual de la fila  " + fila.Index);
                                            MessageBox.Show("El envio se pausó debido al <# mensajes para Pausar> designado en esta sección.\nRecomendamos esta pausa para no ser bloqueado en WhatsApp.\nLa pausa suele durar 15 minutos y se empezo el <" + getTimeNow() + ">, actualmente se pausa cada " + severalpausetxt.Text + " mensajes", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                            await Task.Run(() =>
                                            {
                                                try
                                                {
                                                    Task.Delay(TimeSpan.FromSeconds(900), severalpausetoken2.Token).Wait();

                                                }
                                                catch (Exception ex)
                                                {

                                                    Console.WriteLine(ex.Message);
                                                }


                                            });



                                        }



                                    }

                                    await Task.Run(() =>
                                    {
                                        try
                                        {
                                            Task.Delay(eachmessagetiming2, eachmessagetoken2.Token).Wait();


                                        }
                                        catch (Exception ex)
                                        {

                                            Console.WriteLine(ex.Message.ToString());
                                        }




                                    });




                                }



                                if (stopbtnclicked2 != true)
                                {
                                    MessageBox.Show("SMS enviados correctamente! ", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);


                                    stop2btn.Enabled = false;
                                    pause2btn.Enabled = false;
                                    start2btn.Enabled = true;
                                    logout2btn.Enabled = true;
                                    connectgoobtn.Enabled = true;

                                }
                                else
                                {
                                    start2btn.Enabled = true;
                                }

                                notsendedmessage2lbl.Text = Convert.ToString(rowcount2 - sendedmessage2);

                                contacts2dgv.AllowUserToAddRows = true;
                                contacts2dgv.AllowUserToDeleteRows = true;
                                }
                                catch (OperationCanceledException)
                                {
                                    Console.WriteLine("SMS sending cancelled by user");
                                    HandleSendingCancellation2();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error in SMS sending: {ex.Message}");
                                    MessageBox.Show($"Error: {ex.Message}", "Error");
                                    HandleSendingCancellation2();
                                }
                            }
                        }

                        else
                        {
                            DialogResult d;
                            d = MessageBox.Show("El navegador está cerrado, no se puede enviar mensajes!, conecte otra vez presionando <Conectar WhatsApp>", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);



                        }

                    }
                }




            }
            else
            {
                MessageBox.Show("No cuenta con acceso a internet, no puedes continuar.", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }





        }
        #endregion
        private void PasteDataCore(DataGridView grid, TabControl tabControl, TabPage tabPage)
        {
            tabControl.SelectedTab = tabPage;
            try
            {
                grid.Rows.Clear();
                grid.Refresh();

                string s = Clipboard.GetText();
                string[] lines = s.Replace("\n", "").Split('\r');

                grid.Rows.Add(lines.Length - 1);
                string[] fields;
                int row = 0;
                int col = 0;

                foreach (string item in lines)
                {
                    fields = item.Split('\t');
                    foreach (string f in fields)
                    {
                        grid[col, row].Value = f;
                        col++;
                    }
                    row++;
                    col = 0;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Solo se pueden pegar 2 COLUMNAS (NUMERO, NOMBRE DE CONTACTO) de EXCEL",
                    "Observación", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void pastedatabtn_Click(object sender, EventArgs e)
        {
            PasteDataCore(contactsdgv, maintab, contactlisttab);
        }

        private void Storecontaacts()
        {
            // Convert and save WhatsApp contacts
            var contacts = _viewModel.ContactService.ConvertFromDataGridView(contactsdgv);
            _viewModel.ContactService.SaveContactsToJson(contacts, isSMS: false);

            // Convert and save SMS contacts
            var contacts2 = _viewModel.ContactService.ConvertFromDataGridView(contacts2dgv);
            _viewModel.ContactService.SaveContactsToJson(contacts2, isSMS: true);
        }

        private void Storemessages()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "tempfilesWAButt"
            );
            Directory.CreateDirectory(path);

            // Store WhatsApp messages
            var waMessages = new[] { m1txt.Text, m2txt.Text, m3txt.Text, m4txt.Text, m5txt.Text };
            for (int i = 0; i < waMessages.Length; i++)
            {
                File.WriteAllText(Path.Combine(path, $"m{i + 1}.txt"), waMessages[i]);
            }

            // Store SMS messages
            var smsMessages = new[] { sms1txt.Text, sms2txt.Text, sms3txt.Text, sms4txt.Text, sms5txt.Text };
            for (int i = 0; i < smsMessages.Length; i++)
            {
                File.WriteAllText(Path.Combine(path, $"sms{i + 1}.txt"), smsMessages[i]);
            }
        }




        private void Restoremessages()
        {
            string basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "tempfilesWAButt"
            );

            var messageBoxes = new[] { m1txt, m2txt, m3txt, m4txt, m5txt };
            for (int i = 0; i < messageBoxes.Length; i++)
            {
                string filePath = Path.Combine(basePath, $"m{i + 1}.txt");
                if (File.Exists(filePath))
                    messageBoxes[i].Text = File.ReadAllText(filePath);
            }
        }
        private void Restoremessages2()
        {
            string basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "tempfilesWAButt"
            );

            var smsBoxes = new[] { sms1txt, sms2txt, sms3txt, sms4txt, sms5txt };
            for (int i = 0; i < smsBoxes.Length; i++)
            {
                string filePath = Path.Combine(basePath, $"sms{i + 1}.txt");
                if (File.Exists(filePath))
                    smsBoxes[i].Text = File.ReadAllText(filePath);
            }
        }

        private List<int> NotEmptyMessages()
        {
            List<int> result = new List<int>();
            var messageBoxes = new[] { m1txt, m2txt, m3txt, m4txt, m5txt };

            for (int i = 0; i < messageBoxes.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(messageBoxes[i].Text))
                    result.Add(i);
            }

            return result;
        }
        private void gmailbtn_Click(object sender, EventArgs e)
        {
            cmsgmail.Show(Cursor.Position.X, Cursor.Position.Y);
        }

        private void expotarDatosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            exportDgvToGmail();
        }

        private void importarDatosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImportGmailToDgv();


        }

        private void clearfilenamebtn_Click(object sender, EventArgs e)
        {
            filenametxt.Clear();

        }

        private void savebtn_Click(object sender, EventArgs e)
        {




            if (contactsdgv.Rows.Count > 1)
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = "Archivo de Texto (*.txt)|*.txt",
                    FileName = ""
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Convert DataGridView to contact list
                        var contacts = _viewModel.ContactService.ConvertFromDataGridView(contactsdgv);

                        // Export to text file using ContactService
                        _viewModel.ContactService.ExportToTextFile(contacts, sfd.FileName);

                        MessageBox.Show("Datos exportados correctamente!", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("No hay datos a exportar", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }




        }

        private void SetupContactGridColumns(DataGridView grid)
        {
            grid.Columns.Clear();
            grid.Columns.Add("Column", "Numero o Grupo");
            grid.Columns.Add("Column", "Nombre");
            grid.Columns.Add("Column", "Enviado (S/N)");

            grid.Columns[0].Width = 200;
            grid.Columns[1].Width = 350;
            grid.Columns[2].Width = 100;
            grid.Columns[2].ReadOnly = true;
        }

        private void openbtn_Click(object sender, EventArgs e)
        {

            OpenFileDialog sfd = new OpenFileDialog();
            sfd.Filter = "Archivo de Texto (*.txt)|*.txt";
            sfd.FileName = sfd.FileName;


            if (sfd.ShowDialog() == DialogResult.OK)
            {

                if (File.Exists(sfd.FileName))
                {




                    // Import contacts from file using ContactService
                    var contacts = _viewModel.ContactService.ImportFromTextFile(sfd.FileName);

                    // Clear and setup DataGridView using helper
                    SetupContactGridColumns(contactsdgv);   

                    // Load contacts to DataGridView
                    _viewModel.ContactService.LoadToDataGridView(contactsdgv, contacts);

                    MessageBox.Show("Datos importados!", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    maintab.SelectedTab = contactlisttab;

                }

            }





        }

        private void SetPauseTiming(int seconds)
        {
            if (pausetiming == 0)
            {
                // Only create new token if it's null or already canceled
                if (pauseToken == null || pauseToken.IsCancellationRequested)
                {
                    pauseToken = new CancellationTokenSource();
                }

                pausetiming = seconds;
                pausebtn.Text = "Reanudar";
                MessageBox.Show("Los envíos se pausarán en breve.", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                logoutbtn.Enabled = true;
            }
        }

        private void minutosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetPauseTiming(300); // 5 minutes
        }

        private void minutosToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SetPauseTiming(1800); // 30 minutes
        }

        private void horaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetPauseTiming(3600); // 1 hour
        }

        private void horaToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SetPauseTiming(7200); // 2 hours
        }

        private async Task pausetimingaction(int seconds, CancellationToken token)
        {
            if (seconds > 0)
            {
                await _viewModel.TimingService.ApplyPauseAsync(
                    seconds,
                    token,
                    msg => MessageBox.Show(msg, "Pausa"));
            }
        }
        private async Task ExecuteSendTask()
        {
            if (!await ValidatePreSendConditions()) return;

            PrepareForSending();
            int count = 0;

            try
            {
                foreach (DataGridViewRow fila in contactsdgv.Rows)
                {
                    if (fila.IsNewRow) continue;

                    cancellationToken.Token.ThrowIfCancellationRequested();

                    if (!InternetHelper.CheckForInternetConnection())
                    {
                        StopSendingDueToNoInternet();
                        break;
                    }

                    // Process contact
                    await ProcessSingleContact(fila, cancellationToken.Token);

                    // Update progress
                    count++;
                    if (count <= rowcount) sendpbr.Value = count;

                    // Handle pause points
                    await HandlePausePoints(fila.Index);

                    // Delay between messages
                    if (wa.clickstate)
                    {
                        await DelayBetweenMessages();
                    }
                }

                FinalizeSending();
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Sending cancelled by user");
                HandleSendingCancellation();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in sending: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Error");
                FinalizeSending();
            }
        }

        private void HandleSendingCancellation()
        {
            // Complete or reset progress bar
            if (sendpbr.Value > 0 && sendpbr.Value < sendpbr.Maximum)
            {
                sendpbr.Value = sendpbr.Maximum; // Complete it to show it finished
            }

            // Update message labels
            notsendedmessagelbl.Text = (rowcount - sendedmessage).ToString();

            // Re-enable UI controls
            contactsdgv.AllowUserToAddRows = true;
            contactsdgv.AllowUserToDeleteRows = true;
            uploadbtn.Enabled = true;
            clearfilenamebtn.Enabled = true;
            startbtn.Enabled = true;
            logoutbtn.Enabled = true;
            connectwabtn.Enabled = true;
            stopbtn.Enabled = false;
            pausebtn.Enabled = false;

            // Show cancellation message to user
            if (stopbtnclicked)
            {
                MessageBox.Show("Envío cancelado por el usuario.", "Cancelado",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                stopbtnclicked = false; // Reset flag
            }
        }

        private void HandleSendingCancellation2()
        {
            // Complete or reset progress bar for SMS
            if (send2pbr.Value > 0 && send2pbr.Value < send2pbr.Maximum)
            {
                send2pbr.Value = send2pbr.Maximum; // Complete it to show it finished
            }

            // Update message labels
            notsendedmessage2lbl.Text = (rowcount2 - sendedmessage2).ToString();

            // Re-enable UI controls
            contacts2dgv.AllowUserToAddRows = true;
            contacts2dgv.AllowUserToDeleteRows = true;
            start2btn.Enabled = true;
            logout2btn.Enabled = true;
            connectgoobtn.Enabled = true;
            stop2btn.Enabled = false;
            pause2btn.Enabled = false;

            // Show cancellation message to user
            if (stopbtnclicked2)
            {
                MessageBox.Show("Envío SMS cancelado por el usuario.", "Cancelado",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                stopbtnclicked2 = false; // Reset flag
            }
        }
        private void FinalizeSending()
        {
            if (!stopbtnclicked)
            {
                MessageBox.Show("Mensajes enviados correctamente!",
                    "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            uploadbtn.Enabled = true;
            clearfilenamebtn.Enabled = true;
            stopbtn.Enabled = false;
            pausebtn.Enabled = false;
            startbtn.Enabled = true;
            logoutbtn.Enabled = true;
            connectwabtn.Enabled = true;

            notsendedmessagelbl.Text = (rowcount - sendedmessage).ToString();
            contactsdgv.AllowUserToAddRows = true;
            contactsdgv.AllowUserToDeleteRows = true;
        }
        private void StopSendingDueToNoInternet()
        {
            stopbtn.Enabled = false;
            pausebtn.Enabled = false;
            startbtn.Enabled = true;
            MessageBox.Show("Se detuvieron los envios debido a falta de conexión a internet.",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        private void ExecuteStart()
        {
            try
            {
                detailtimer.Start();

                OpenSaved();
                OpenSaved2();
                controls(false);
                controls2(false);

                pausebtn.Enabled = false;
                stopbtn.Enabled = false;
                pause2btn.Enabled = false;
                stop2btn.Enabled = false;
                logoutbtn.Enabled = false;
                logout2btn.Enabled = false;

                sendpbr.Value = 0;
                send2pbr.Value = 0;

                OpenSettings();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ExecuteStart error: {ex.Message}");
            }
        }

        private void CheckUserProfileExist()
        {
            string basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "tempfilesWAButt"
            );

            // Download WA profile if missing
            if (!Directory.Exists(Path.Combine(basePath, "Chrome WA Profile")))
            {
                DownloadAndExtractProfile(chromewadefaultuserdata, "Chrome WA Profile.zip", basePath);
            }

            // Download SMS profile if missing
            if (!Directory.Exists(Path.Combine(basePath, "Chrome SMS Profile")))
            {
                DownloadAndExtractProfile(chromesmsdefaultuserdata, "Chrome SMS Profile.zip", basePath);
            }
        }

        private void DownloadAndExtractProfile(string url, string zipName, string targetDir)
        {
            try
            {
                Directory.CreateDirectory(targetDir);

                using (WebClient client = new WebClient())
                {
                    string zipPath = Path.Combine(targetDir, zipName);
                    client.DownloadFile(url, zipPath);

                    FastZip fastZip = new FastZip();
                    fastZip.ExtractZip(zipPath, targetDir, "");

                    File.Delete(zipPath);
                    Console.WriteLine($"✓ Downloaded and extracted {zipName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading profile: {ex.Message}");
            }
        }
        private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }
        private async void logoutbtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (!InternetHelper.CheckForInternetConnection())
                {
                    MessageBox.Show("No cuenta con acceso a internet.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Disable button to prevent double-clicks
                logoutbtn.Enabled = false;

                await Task.Run(() => wa.LogoutWA());

                if (MessageBox.Show("¿Desea cerrar el programa?", "Confirmación",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Storecontaacts();
                    Storemessages();
                    StoreSettings();
                    wa.CloseWDriver();
                    Application.Exit();
                }
                else
                {
                    logoutbtn.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cerrar sesión: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                logoutbtn.Enabled = true;
            }
        }
        // Migrated to InternetHelper.CheckForInternetConnection()
        private void manymessagescb_Click(object sender, EventArgs e)
        {

        }
        private static DateTime getTimeNow()
        {
       


            DateTime localDate = DateTime.Now;
            String cultureName = "es-PE";


            var culture = new CultureInfo(cultureName);
            string res = localDate.ToString(culture);

            return Convert.ToDateTime(res);


        }
       
        private void severalpausetxt_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputNumbers(sender, e);
        }
        private void eachmessagetimingtxt_KeyPress(object sender, KeyPressEventArgs e)
        {
            InputNumbers(sender, e);
        }
        private void extractgroupnumbersbtn_Click(object sender, EventArgs e)
        {
            ExtractContacts ext = new ExtractContacts(this);
            ext.ShowDialog();

        }


        public async Task GetContactsFromGroup(string tosearch)
        {

            StringBuilder str = new StringBuilder();
            strex = new StringBuilder();
            await Task.Run(() =>
            {

                str = wa.GetContactsFromGroup(tosearch);

                if (Convert.ToString(str) != "")
                {
                    string[] words = WAButtfrm.GetWords(tosearch);
                    string converted = "";

                    foreach (var item in words)
                    {
                        converted = converted + item;
                    }


                    try
                    {
                        filenameextracted = Path.Combine(Environment.GetFolderPath(
                       Environment.SpecialFolder.ApplicationData), "wabutt" + converted + DateTime.Now.ToString("dd-MM-yyyy") + ".csv");


                        StreamWriter swOut = new StreamWriter(filenameextracted);

                        strex.Append(str.ToString().Replace(", ", "\n,").Replace("\n,Tú", "").Replace("\n,You", ""));
                        swOut.Write(str.ToString().Replace(", ", "\n,").Replace("\n,Tú", "").Replace("\n,You", ""));

                        swOut.Close();



                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error :" + ex.Message);
                    }

                }





            });






            //Console.WriteLine(Regex. Replace(converted, @"[^\u0000-\u007F]+", ""));




        }
        
        public static DataTable ReadCSV3(string path)
        {
            DataTable dt = new DataTable();



            if (File.Exists(path))
            {
                Console.WriteLine("READCSV3 OPEN");

                StreamReader sr = new StreamReader(path);
                StringBuilder sb = new StringBuilder();


                string s;


                dt.Columns.Add("Numero", typeof(string));



                int indexname;


                s = sr.ReadLine();

                string[] strs = s.Split(',');


                indexname = strs.ToList().IndexOf("Mobile Phone");




                while (!sr.EndOfStream)
                {
                    s = sr.ReadLine();

                    string[] str = s.Split(',');


                    //because the first line is header
                    string str1 = str[0].ToString();


                    if (!str1.Equals("First Name"))
                    {

                        DataRow row = dt.NewRow();

                        row["Numero"] = str[indexname].ToString();

                        dt.Rows.Add(row);



                    }
                }

                sr.Close();




            }


            return dt;

        }
        public static void StoreGroupContacts(string converted, StringBuilder str)
        {

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv",
                FileName = converted

            };

            bool fileError = false;

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(sfd.FileName))
                {
                    try
                    {
                        File.Delete(sfd.FileName);
                    }
                    catch (IOException ex)
                    {
                        fileError = true;
                        MessageBox.Show("No fue posible escribir datos en el disco." + ex.Message);
                    }
                }
                if (!fileError)
                {
                    try
                    {


                        StreamWriter swOut = new StreamWriter(sfd.FileName);


                        swOut.Write(str.ToString().Replace(", ", "\n,"));

                        swOut.Close();
                        MessageBox.Show("Datos exportados correctamente!", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);


                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error :" + ex.Message);
                    }
                }
            }

        }
        public static string[] GetWords(string input)
        {
            MatchCollection matches = Regex.Matches(input, @"\b[\w']*\b");

            var words = from m in matches.Cast<Match>()
                        where !string.IsNullOrEmpty(m.Value)
                        select TrimSuffix(m.Value);

            return words.ToArray();
        }
        public static string TrimSuffix(string word)
        {
            int apostropheLocation = word.IndexOf('\'');
            if (apostropheLocation != -1)
            {
                word = word.Substring(0, apostropheLocation);
            }

            return word;
        }


        private void Extracttimer_Tick(object sender, EventArgs e)
        {
            if (startbtn.Enabled)
            {
                extractgroupnumbersbtn.Visible = true;

                extractlbl.Visible = true;


                severalpauselbl.Location = new Point(765, 62); 
                severalpausetxt.Location = new Point(803, 20);



            }
            else
            {
                extractgroupnumbersbtn.Visible = false; extractlbl.Visible = false;

                severalpauselbl.Location = new Point(641,62);
                severalpausetxt.Location = new Point(676, 22);
            }

            /*

            if (apptab.SelectedTab == wabottab)
            {
                colorpanel.BackColor = Color.FromArgb(8, 112, 100);
            }

            else
            {
                colorpanel.BackColor = Color.FromArgb(19, 116, 233);
            }

            */

        }
        private async void connectgoobtn_Click(object sender, EventArgs e)
        {
            if (!InternetHelper.CheckForInternetConnection())
            {
                MessageBox.Show("No cuenta con acceso a internet.", "Error");
                return;
            }

            wa.CloseWDriver2();

            try
            {
                loadmessage2lbl.Text = "Estado: Conectando...";
                await wa.LaunchBrowser2();

                if (wa.driverstate2)
                {
                    loadmessage2lbl.Text = "Estado: Navegador Abierto, escanee código QR";
                    controls2(true);
                    logout2btn.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }
        private void selectwabtn_Click(object sender, EventArgs e)
        {
            apptab.SelectedTab = wabottab;


        }
        private void selectsmsbtn_Click(object sender, EventArgs e)
        {
            apptab.SelectedTab = smsbottab;


        }
        private void pastedata2btn_Click(object sender, EventArgs e)
        {
            PasteDataCore(contacts2dgv, main2tab, contactlist2tab);
        }
        private void save2btn_Click(object sender, EventArgs e)
        {
            if (contacts2dgv.Rows.Count > 1)
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = "Archivo de Texto (*.txt)|*.txt",
                    FileName = ""
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Convert DataGridView to contact list
                        var contacts = _viewModel.ContactService.ConvertFromDataGridView(contacts2dgv);

                        // Export to text file using ContactService
                        _viewModel.ContactService.ExportToTextFile(contacts, sfd.FileName);

                        MessageBox.Show("Datos exportados correctamente!", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("No hay datos a exportar", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        private void open2btn_Click(object sender, EventArgs e)
        {
            OpenFileDialog sfd = new OpenFileDialog();
            sfd.Filter = "Archivo de Texto (*.txt)|*.txt";
            sfd.FileName = sfd.FileName;


            if (sfd.ShowDialog() == DialogResult.OK)
            {

                if (File.Exists(sfd.FileName))
                {




                    // Import contacts from file using ContactService
                    var contacts = _viewModel.ContactService.ImportFromTextFile(sfd.FileName);

                    // Clear and setup DataGridView using helper
                    SetupContactGridColumns(contacts2dgv);

                    // Load contacts to DataGridView
                    _viewModel.ContactService.LoadToDataGridView(contacts2dgv, contacts);

                    MessageBox.Show("Datos importados!", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    main2tab.SelectedTab = contactlist2tab;

                }

            }
        }
        private async Task ProcessSingleContact(DataGridViewRow fila, CancellationToken token)
        {
            // Check for cancellation at the start
            token.ThrowIfCancellationRequested();

            string contactNumber = Convert.ToString(fila.Cells[0].Value);
            string contactName = Convert.ToString(fila.Cells[1].Value);

            if (string.IsNullOrEmpty(contactNumber))
            {
                fila.Cells[2].Value = "N";
                notsendedmessage++;
                notsendedmessagelbl.Text = notsendedmessage.ToString();
                return;
            }

            Console.WriteLine($"Processing: {contactNumber}");

            // Check for cancellation before preparing message
            token.ThrowIfCancellationRequested();

            // Prepare message
            string messageToSend = PrepareMessage(contactName);

            // Check for cancellation before searching contact
            token.ThrowIfCancellationRequested();

            // Search and click contact
            await SearchAndClickContact(contactNumber);

            if (!wa.clickstate)
            {
                fila.Cells[2].Value = "N";
                notsendedmessage++;
                notsendedmessagelbl.Text = notsendedmessage.ToString();
                return;
            }

            // Check for cancellation before sending
            token.ThrowIfCancellationRequested();

            // Send message/file
            await SendMessageOrFile(messageToSend, filenametxt.Text, contactNumber);

            fila.Cells[2].Value = "S";
            sendedmessage++;
            sendedmessagelbl.Text = sendedmessage.ToString();
        }

        private async Task ProcessSingleSMS(DataGridViewRow fila)
        {
            string contactNumber = Convert.ToString(fila.Cells[0].Value);
            string contactName = Convert.ToString(fila.Cells[1].Value);

            if (string.IsNullOrEmpty(contactNumber))
            {
                fila.Cells[2].Value = "N";
                notsendedmessage2++;
                return;
            }

            string message = PrepareSMSMessage(contactName);

            await Task.Run(() =>
            {
                try
                {
                    Actions action = new Actions(WA.driver2);

                    wa.ClickSearchIcon2();
                    action.SendKeys(Keys.Space).Build().Perform();
                    wa.ContactSearch2(contactNumber);
                    wa.ContactClick2();
                    Task.Delay(2000).Wait();

                    wa.ContactMessage2(message);
                    Task.Delay(1000 + wa.preventblocktiming2).Wait();
                    wa.ContactActionEnter2();

                    fila.Cells[2].Value = "S";
                    sendedmessage2++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SMS Error: {ex.Message}");
                    fila.Cells[2].Value = "N";
                    notsendedmessage2++;
                }
            }, cancellationToken2.Token);

            sendedmessage2lbl.Text = sendedmessage2.ToString();
        }
        private void emoji2btn_Click(object sender, EventArgs e)
        {
            Process.Start("https://es.piliapp.com/twitter-symbols/");
        }
        private void gmail2btn_Click(object sender, EventArgs e)
        {
            cmsgmail2.Show(Cursor.Position.X, Cursor.Position.Y);
        }
        private void importarDatosToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ImportGmailToDgv2();
        }
        private void exportarDatosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            exportDgvToGmail2();
        }
        private async void start2btn_Click(object sender, EventArgs e)
        {



            cancellationToken2 = new CancellationTokenSource();
            pauseToken2 = new CancellationTokenSource();
            eachmessagetoken2 = new CancellationTokenSource();
            severalpausetoken2 = new CancellationTokenSource();
            try
            {
                await Excecutesendtask2();
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.TargetSite);
            }
            





        }

        private void pause2btn_Click(object sender, EventArgs e)
        {
            if (pausetiming2 > 0)
            {
                pause2btn.Text = "Pausar";
                pauseToken2?.Cancel();
            }
            else
            {
                cmspause2.Show(Cursor.Position.X, Cursor.Position.Y);
            }

        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {


            if (pausetiming2 == 0)
            {
                pauseToken2 = new CancellationTokenSource();

                pausetiming2 = 300;
                pause2btn.Text = "Reanudar";
                MessageBox.Show("Los envíos SMS se pausarán en breve.", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                logout2btn.Enabled = true;
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            pauseToken2 = new CancellationTokenSource();

            if (pausetiming2 == 0)
            {
                pausetiming2 = 1800;
                pause2btn.Text = "Reanudar";
                MessageBox.Show("Los envíos SMS se pausarán en breve.", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                logout2btn.Enabled = true;
            }
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            pauseToken2 = new CancellationTokenSource();
            if (pausetiming2 == 0)
            {
                pausetiming2 = 3600;
                pause2btn.Text = "Reanudar";
                MessageBox.Show("Los envíos SMS se pausarán en breve.", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                logout2btn.Enabled = true;
            }
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            pauseToken2 = new CancellationTokenSource();
            if (pausetiming2 == 0)
            {
                pausetiming2 = 7200;
                pause2btn.Text = "Reanudar";
                MessageBox.Show("Los envíos SMS se pausarán en breve.", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                logout2btn.Enabled = true;
            }
        }

        private void stop2btn_Click(object sender, EventArgs e)
        {if (MessageBox.Show("¿Desea detener los envíos SMS?", "Confirmación", 
                MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                pauseToken2?.Cancel();
                cancellationToken2?.Cancel();
                eachmessagetoken2?.Cancel();
                severalpausetoken2?.Cancel();
                
                stopbtnclicked2 = true;
                pause2btn.Text = "Pausar";
            }
        }

        public void ClearEmptyRows(DataGridView dgv)
        {
            // Convert, remove empty contacts, and reload
            var contacts = _viewModel.ContactService.ConvertFromDataGridView(dgv);
            var validContacts = _viewModel.ContactService.RemoveEmptyContacts(contacts);
            dgv.Rows.Clear();
            _viewModel.ContactService.LoadToDataGridView(dgv, validContacts);
        }

        private void clearemptyrowsbtn_Click(object sender, EventArgs e)
        {
            ClearEmptyRows(contactsdgv);
        }

        private void deleteduplicatedbtn_Click(object sender, EventArgs e)
        {


        }
        private void DeleteDuplicate1()
        {
            // Convert DataGridView to contact list
            var contacts = _viewModel.ContactService.ConvertFromDataGridView(contactsdgv);

            // Remove duplicates using ContactService
            var uniqueContacts = _viewModel.ContactService.RemoveDuplicates(contacts);

            // Clear and reload DataGridView
            contactsdgv.Rows.Clear();
            _viewModel.ContactService.LoadToDataGridView(contactsdgv, uniqueContacts);

            // Set column widths
            DataGridViewColumn column = contactsdgv.Columns[0];
            column.Width = 200;

            DataGridViewColumn column1 = contactsdgv.Columns[1];
            column1.Width = 350;




            DataGridViewColumn column2 = contactsdgv.Columns[2];
            column2.Width = 100;
            column2.ReadOnly = true;
        }
        private void DeleteDuplicate2()
        {
            // Convert DataGridView to contact list
            var contacts = _viewModel.ContactService.ConvertFromDataGridView(contacts2dgv);

            // Remove duplicates using ContactService
            var uniqueContacts = _viewModel.ContactService.RemoveDuplicates(contacts);

            // Clear and reload DataGridView
            contacts2dgv.Rows.Clear();
            _viewModel.ContactService.LoadToDataGridView(contacts2dgv, uniqueContacts);

            // Set column widths
            DataGridViewColumn column = contacts2dgv.Columns[0];
            column.Width = 200;

            DataGridViewColumn column1 = contacts2dgv.Columns[1];
            column1.Width = 350;

            DataGridViewColumn column2 = contacts2dgv.Columns[2];
            column2.Width = 100;
            column2.ReadOnly = true;
        }

        private void CopyGridToClipboard(DataGridView grid)
        {
            // Copiar celdas seleccionadas al portapapeles
            if (grid.GetCellCount(DataGridViewElementStates.Selected) > 0)
            {
                try
                {
                    // Usar el método nativo del DataGridView para copiar
                    grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithAutoHeaderText;
                    Clipboard.SetDataObject(grid.GetClipboardContent());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al copiar: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void copiarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyGridToClipboard(contactsdgv);
        }

        private void contactsdgv_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {

                int currentMouseOverRow = contactsdgv.HitTest(e.X, e.Y).RowIndex;


                copycms.Show(contactsdgv, new Point(e.X, e.Y));

            }
        }

        private string EncodeNonAsciiCharacters(string value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in value)
            {
                if (c > 127)
                {
                    // This character is too big for ASCII
                    string encodedValue = "\\u" + ((int)c).ToString("x4");
                    sb.Append(encodedValue);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
        private bool CheckAttachMessageStatus()
        {
            // Simplified: !(!A || B) = A && !B
            return CheckAttachMessageStatusSub() && !sendonlyattachcb.Checked;
        }

        private bool CheckAttachMessageStatusSub()
        {
            if (string.IsNullOrEmpty(filenametxt.Text))
                return true;

            var messageBoxes = new[] { m1txt, m2txt, m3txt, m4txt, m5txt };
            return messageBoxes.Any(mb => !string.IsNullOrWhiteSpace(mb.Text));
        }

        // Migrated to FileHelper.GetExtension(), IsImageFile(), IsVideoFile()

        private void eliminarDuplicadosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteDuplicate1();
        }

        private void eliminarFilasVaciasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearEmptyRows(contactsdgv);
        }

        private void PasteFromClipboard(DataGridView grid)
        {
            try
            {
                // Validar que haya una celda seleccionada
                if (grid.CurrentCell == null)
                {
                    MessageBox.Show("Selecciona una celda donde pegar los datos.", "Observación",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Obtener texto del portapapeles
                string clipboardText = Clipboard.GetText();
                if (string.IsNullOrWhiteSpace(clipboardText))
                {
                    MessageBox.Show("El portapapeles está vacío.", "Observación",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Normalizar saltos de línea (Windows: \r\n, Unix: \n, Mac: \r)
                clipboardText = clipboardText.Replace("\r\n", "\n").Replace("\r", "\n");
                string[] lines = clipboardText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length == 0)
                {
                    MessageBox.Show("No hay datos válidos para pegar.", "Observación",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                int startRow = grid.CurrentCell.RowIndex;
                int currentRow = startRow;

                // Calcular cuántas filas necesitamos
                int totalRowsNeeded = startRow + lines.Length;
                int currentTotalRows = grid.Rows.Count;

                // Agregar filas faltantes si es necesario
                if (totalRowsNeeded > currentTotalRows)
                {
                    int rowsToAdd = totalRowsNeeded - currentTotalRows;
                    for (int i = 0; i < rowsToAdd; i++)
                    {
                        grid.Rows.Add();
                    }
                }

                // Pegar los datos
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        currentRow++;
                        continue;
                    }

                    // Separar por tabulador
                    string[] fields = line.Split('\t');

                    // Solo pegar en las primeras 2 columnas (Número y Nombre)
                    // La columna 2 (Enviado S/N) se mantiene intacta
                    for (int col = 0; col < Math.Min(fields.Length, 2); col++)
                    {
                        if (currentRow < grid.Rows.Count)
                        {
                            grid[col, currentRow].Value = fields[col].Trim();
                        }
                    }

                    currentRow++;
                }

                MessageBox.Show($"Se pegaron {lines.Length} filas correctamente.", "Éxito",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al pegar datos: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void pegarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PasteFromClipboard(contactsdgv);
        }

        private void limpiarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            contactsdgv.Rows.Clear();
            contactsdgv.Refresh();
        }

        private void contacts2dgv_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {

                int currentMouseOverRow = contacts2dgv.HitTest(e.X, e.Y).RowIndex;


                copy2cms.Show(contacts2dgv, new Point(e.X, e.Y));

            }
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            CopyGridToClipboard(contacts2dgv);
        }

        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            PasteFromClipboard(contacts2dgv);
        }

        private void toolStripMenuItem7_Click(object sender, EventArgs e)
        {
            DeleteDuplicate2();
        }

        private void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            ClearEmptyRows(contacts2dgv);

        }

        private void toolStripMenuItem9_Click(object sender, EventArgs e)
        {
            contacts2dgv.Rows.Clear();
            contacts2dgv.Refresh();
        }

        private void exportdatatlp_Popup(object sender, PopupEventArgs e)
        {

        }

        private void cmsgmail_Opening(object sender, CancelEventArgs e)
        {

        }

        private void apppanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void contactsdgv_KeyDown(object sender, KeyEventArgs e)
        {

        }
    }
    
}