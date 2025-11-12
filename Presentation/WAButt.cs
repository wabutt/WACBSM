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

namespace Presentation
{
    public partial class WAButtfrm : Form
    {
     
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

      

        public WAButtfrm()
        {
            AutoUpdater.InstalledVersion = Version.Parse("1.0.0.14");
            UserModel user = new UserModel();

            if (!CheckForInternetConnection())
            {
                MessageBox.Show("No cuenta con acceso a internet, le recomendamos intentar mas tarde.",
                    "Observación", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Load += (sender, e) => { this.Close(); };
                return;
            }

            if (!user.CheckHWID(user.GetMachineGuid()))
            {
                MessageBox.Show("Contact to Creator :) trevorcalfan2@gmail.com",
                    "<3", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                Clipboard.SetText(user.GetMachineGuid());
                this.Load += (sender, e) => { this.Close(); };
                return;
            }

            // Initialize ChromeDriver asynchronously
            Task.Run(async () =>
            {
                if (!await ChromeDriverStateAsync())
                {
                    this.Invoke((MethodInvoker)delegate {
                        this.Load += (sender, e) => { this.Close(); };
                    });
                    return;
                }
            });

            CheckUserProfileExist();
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            updatestart();
            ExecuteStart();
        }

        private async Task DwchromedriverAsync()
        {
            try
            {
                KillWebDriver();

                string driverDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "tempfilesWAButt", "webdriver"
                );
                Directory.CreateDirectory(driverDir);

                string exePath = Path.Combine(driverDir, "chromedriver.exe");
                string versionFile = Path.Combine(driverDir, "chromedriverversion.txt");
                string zipPath = Path.Combine(driverDir, "chromedriver.zip");

                // Check if already up to date
                if (File.Exists(versionFile) && File.Exists(exePath))
                {
                    string currentVersion = File.ReadAllText(versionFile);
                    if (currentVersion == chromedriverversion)
                    {
                        Console.WriteLine("✓ ChromeDriver already up to date");
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Updating from {currentVersion} to {chromedriverversion}");
                    }
                }

                // Delete old files
                if (File.Exists(exePath)) File.Delete(exePath);
                if (File.Exists(zipPath)) File.Delete(zipPath);

                // NEW: Chrome for Testing download URL
                chromedriverdwlink = $"https://storage.googleapis.com/chrome-for-testing-public/{chromedriverversion}/win64/chromedriver-win64.zip";

                Console.WriteLine($"Downloading ChromeDriver from: {chromedriverdwlink}");

                using (WebClient client = new WebClient())
                {
                    await client.DownloadFileTaskAsync(chromedriverdwlink, zipPath);
                    Console.WriteLine("✓ Download complete");
                }

                // Extract
                FastZip fastZip = new FastZip();
                fastZip.ExtractZip(zipPath, driverDir, "");

                // NEW: Chrome for Testing extracts to chromedriver-win64 subfolder
                string extractedFolder = Path.Combine(driverDir, "chromedriver-win64");
                if (Directory.Exists(extractedFolder))
                {
                    string extractedExe = Path.Combine(extractedFolder, "chromedriver.exe");
                    if (File.Exists(extractedExe))
                    {
                        File.Copy(extractedExe, exePath, true);
                        Directory.Delete(extractedFolder, true);
                        Console.WriteLine("✓ Extracted from chromedriver-win64 folder");
                    }
                }

                // Clean up
                File.Delete(zipPath);
                File.WriteAllText(versionFile, chromedriverversion);

                Console.WriteLine("✓ ChromeDriver installed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
                throw;
            }
        }
        private void KillWebDriver()
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("chromedriver"))
                {
                    try { p.Kill(); p.WaitForExit(2000); }
                    catch { }
                }
            }
            catch {  }
        }

        private void updatestart()
        {
            try
            {
                AutoUpdater.Mandatory = true;
                AutoUpdater.UpdateMode = Mode.Forced;
                AutoUpdater.ShowSkipButton = false;
                AutoUpdater.ShowRemindLaterButton = false;
                AutoUpdater.DownloadPath = Environment.CurrentDirectory;
                AutoUpdater.Start("https://raw.githubusercontent.com/wabutt/itsmevsauce/master/AutoUpdater.xml");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update error: {ex.Message}");
            }
        }
        
        private void SendDocument(string filePath, string message, Actions action, string contactNumber)
        {
            wa.ContactFile(filePath);
            wa.ContactSend(By.XPath(WA.SendIADButton));
            Task.Delay(1000 + wa.preventblocktiming).Wait();

            if (!CheckAttachMessageStatus())
            {
                // Send message separately
                wa.ClickSearchIcon();
                wa.ContactSearch(contactNumber);
                action.SendKeys(Keys.Space).Build().Perform();
                wa.ContactClick();
                Task.Delay(1000).Wait();

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
            await Task.Run(() =>
            {
                if (pausetiming != 0)
                {
                    pausetimingaction(pausetiming, pauseToken.Token);
                    pausetiming = 0;
                }

                try
                {
                    Actions action = new Actions(WA.driver);

                    action.SendKeys("a").Build().Perform();
                    Task.Delay(500).Wait();

                    wa.ContactMessage(message);
                    Task.Delay(1000 + wa.preventblocktiming).Wait();

                    wa.ContactActionEnter();
                    Console.WriteLine("✓ Text message sent");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending text: {ex.Message}");
                }
            }, cancellationToken.Token);
        }
        public async Task<bool> ChromeDriverStateAsync()
        {
            if (!Environment.Is64BitOperatingSystem)
            {
                MessageBox.Show("El SO actual es Arquitectura 32 bits, actualizar a 64 bits para continuar",
                    "Observación", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return false;
            }

            try
            {
                await FetchChromeDriverVersionAsync();
                await DwchromedriverAsync();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Observación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }
        private async Task<string> FetchChromeDriverVersionAsync()
        {
            try
            {
                // NEW: Chrome for Testing JSON API (replaces deprecated googleapis)
                using (var client = new WebClient())
                {
                    string json = await client.DownloadStringTaskAsync(
                        "https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions.json"
                    );

                    dynamic versionData = JsonConvert.DeserializeObject(json);
                    chromedriverversion = versionData.channels.Stable.version.ToString();

                    Console.WriteLine($"✓ Latest ChromeDriver: {chromedriverversion}");
                    return chromedriverversion;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error fetching version: {ex.Message}");
                // Fallback to recent stable version
                chromedriverversion = "131.0.6778.87";
                return chromedriverversion;
            }
        }
        private void SendImageOrVideo(string filePath, string message, Actions action, string contactNumber)
        {
            if (!CheckAttachMessageStatus())
            {
                wa.ImageMessage(filePath);
                Task.Delay(1000 + wa.preventblocktiming).Wait();
                wa.ContactSend(By.XPath(WA.SendIADButton));
            }
            else
            {
                if (GetImageState(filePath) || GetVideoState(filePath))
                {
                    wa.ImageTextMessage(filePath, message);
                    action.SendKeys(".").Build().Perform();
                    action.SendKeys(Keys.Backspace).Build().Perform();
                    Task.Delay(1000 + wa.preventblocktiming).Wait();
                    wa.ContactSend(By.XPath(WA.SendIADButton));
                }
                else
                {
                    // Send file, then message separately
                    wa.ImageMessage(filePath);
                    Task.Delay(1000 + wa.preventblocktiming).Wait();
                    wa.ContactSend(By.XPath(WA.SendIADButton));
                    Task.Delay(2000).Wait();

                    // Re-search and send message
                    wa.ClickSearchIcon();
                    wa.ContactSearch(contactNumber);
                    action.SendKeys(Keys.Space).Build().Perform();
                    wa.ContactClick();
                    Task.Delay(1000).Wait();

                    wa.ContactMessage(message);
                    wa.ContactActionEnter();
                }
            }
        }
        private async Task SearchAndClickContact(string contactNumber)
        {
            await Task.Run(() =>
            {
                if (pausetiming != 0)
                {
                    pausetimingaction(pausetiming, pauseToken.Token);
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

                    Task.Delay(2000).Wait();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error searching contact: {ex.Message}");
                }
            }, cancellationToken.Token);
        }
        private async Task SendWithAttachment(string message, string filePath, string contactNumber)
        {
            await Task.Run(() =>
            {
                if (pausetiming != 0)
                {
                    pausetimingaction(pausetiming, pauseToken.Token);
                    pausetiming = 0;
                }

                try
                {
                    Actions action = new Actions(WA.driver);

                    if (filetype == "I") // Image/Video
                    {
                        SendImageOrVideo(filePath, message, action, contactNumber);
                    }
                    else if (filetype == "A") // Audio
                    {
                        SendAudio(filePath, message, action, contactNumber);
                    }
                    else if (filetype == "D") // Document
                    {
                        SendDocument(filePath, message, action, contactNumber);
                    }

                    Console.WriteLine($"✓ Attachment sent: {filetype}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending attachment: {ex.Message}");
                }
            }, cancellationToken.Token);
        }

        private void SendAudio(string filePath, string message, Actions action, string contactNumber)
        {
            wa.ContactFileAudio(filePath);
            wa.ContactSend(By.XPath(WA.SendIADButton));
            Task.Delay(1000 + wa.preventblocktiming).Wait();

            if (!CheckAttachMessageStatus())
            {
                // Send message separately
                wa.ClickSearchIcon();
                wa.ContactSearch(contactNumber);
                action.SendKeys(Keys.Space).Build().Perform();
                wa.ContactClick();
                Task.Delay(1000).Wait();

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
                        ReadJsonContacts("Contacts.json");
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
                        ReadJson2Contacts("Contacts2.json");
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
        private void WriteJSONToFile(string data, string filename)
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "tempfilesWAButt"
            );
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, filename), data);
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
            WriteJSONToFile(json, "UserSettings.json");
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

            if (!CheckForInternetConnection())
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
        private void exportDgvToGmail()
        {
            // Ajusta estos índices si tus columnas están en otro orden:
            const int PhoneColIndex = 0;     // Columna con el teléfono
            const int FirstNameColIndex = 1; // Columna con el nombre

            var grid = contactsdgv;

            if (grid == null || grid.Rows.Cast<DataGridViewRow>().All(r => r.IsNewRow))
            {
                MessageBox.Show("No hay datos a exportar", "Observación",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            using (var sfd = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = "contactos.csv",
                AddExtension = true,
                OverwritePrompt = true
            })
            {
                if (sfd.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    // UTF-8 con BOM para acentos/ñ correctos en Excel y Gmail
                    using (var sw = new StreamWriter(sfd.FileName, false, new UTF8Encoding(true)))
                    {
                        // Encabezados que Gmail reconoce
                        sw.WriteLine("Name,Given Name,Phone 1 - Type,Phone 1 - Value");

                        foreach (DataGridViewRow row in grid.Rows)
                        {
                            if (row.IsNewRow) continue;

                            // NO normalizamos: tomamos el valor tal cual
                            string firstName = Convert.ToString(row.Cells[FirstNameColIndex].Value) ?? string.Empty;
                            string phoneRaw = Convert.ToString(row.Cells[PhoneColIndex].Value) ?? string.Empty;

                            string name = firstName;       // si solo tienes nombre, úsalo como Name
                            string phoneType = "Mobile";   // puedes cambiar a Home/Work si aplica

                            sw.WriteLine(
                                $"{Csv(name)},{Csv(firstName)},{Csv(phoneType)},{Csv(phoneRaw)}"
                            );
                        }
                    }

                    MessageBox.Show("Datos exportados correctamente!", "Observación",
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

        // ---- Helper ----
        // No alteramos el contenido; solo hacemos el escape CSV cuando hace falta.
        private static string Csv(string value)
        {
            if (value == null) return string.Empty;

            bool mustQuote = value.Contains(',') || value.Contains('"') || value.Contains('\r') || value.Contains('\n');
            if (mustQuote)
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }
        private void exportDgvToGmail2()
        {

            // Ajusta estos índices si tus columnas están en otro orden:
            const int PhoneColIndex = 0;     // Teléfono
            const int FirstNameColIndex = 1; // Nombre

            var grid = contacts2dgv;

            if (grid == null || grid.Rows.Cast<DataGridViewRow>().All(r => r.IsNewRow))
            {
                MessageBox.Show("No hay datos a exportar", "Observación",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            using (var sfd = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = "contactos.csv",
                AddExtension = true,
                OverwritePrompt = true
            })
            {
                if (sfd.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    // UTF-8 con BOM para acentos/ñ correctos en Excel y Gmail
                    using (var sw = new StreamWriter(sfd.FileName, false, new UTF8Encoding(true)))
                    {
                        // Encabezados reconocidos por Google Contacts
                        sw.WriteLine("Name,Given Name,Phone 1 - Type,Phone 1 - Value");

                        foreach (DataGridViewRow row in grid.Rows)
                        {
                            if (row.IsNewRow) continue;

                            // NO se normaliza: se toma el teléfono tal cual está en la celda
                            string firstName = Convert.ToString(row.Cells[FirstNameColIndex].Value) ?? string.Empty;
                            string phoneRaw = Convert.ToString(row.Cells[PhoneColIndex].Value) ?? string.Empty;

                            string name = firstName;     // si solo tienes nombre, úsalo como Name
                            string phoneType = "Mobile"; // cambia a Home/Work si corresponde

                            sw.WriteLine($"{Csv(name)},{Csv(firstName)},{Csv(phoneType)},{Csv(phoneRaw)}");
                        }
                    }

                    MessageBox.Show("Datos exportados correctamente!", "Observación",
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
        private void ImportGmailToDgv()
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
                // Muestra la pestaña de la lista (igual que tu código original)
                maintab.SelectedTab = contactlisttab;

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
                    // Leer encabezados
                    if (!csv.Read() || !csv.ReadHeader())
                        throw new InvalidOperationException("El archivo CSV no contiene encabezados.");

                    var headers = csv.HeaderRecord?.ToList() ?? new System.Collections.Generic.List<string>();

                    // Buscar columnas por posibles nombres
                    string phoneCol = FirstExisting(headers,
                        "Phone 1 - Value", "Primary Phone", "Mobile Phone", "Phone", "Teléfono 1 - Valor", "Teléfono principal");

                    string nameCol = FirstExisting(headers,
                        "First Name", "Given Name", "Name", "Nombre");

                    // Validaciones mínimas
                    if (string.IsNullOrEmpty(phoneCol) && string.IsNullOrEmpty(nameCol))
                        throw new InvalidOperationException("No se encontraron columnas de teléfono ni nombre en el CSV.");

                    // Preparar el DataGridView
                    contactsdgv.SuspendLayout();
                    contactsdgv.Columns.Clear();
                    contactsdgv.Rows.Clear();

                    contactsdgv.Columns.Add("colPhoneOrGroup", "Numero o Grupo");
                    contactsdgv.Columns.Add("colName", "Nombre");
                    var colSent = contactsdgv.Columns.Add("colSent", "Enviado (S/N)");
                    contactsdgv.Columns[colSent].ReadOnly = true;

                    // Ancho como en tu versión
                    contactsdgv.Columns[0].Width = 200;
                    contactsdgv.Columns[1].Width = 350;
                    contactsdgv.Columns[2].Width = 100;

                    // Leer filas
                    while (csv.Read())
                    {
                        string phone = phoneCol != null ? (csv.GetField(phoneCol) ?? string.Empty) : string.Empty;
                        string first = nameCol != null ? (csv.GetField(nameCol) ?? string.Empty) : string.Empty;

                        // Si no hay ningún dato útil, saltamos
                        if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(first))
                            continue;

                        // NO normalizamos números: se agregan tal cual
                        contactsdgv.Rows.Add(phone, first, string.Empty);
                    }

                    contactsdgv.ResumeLayout();
                    MessageBox.Show("Datos importados!", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
        private void ImportGmailToDgv2()
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
                // Mostrar la pestaña de la lista 2
                main2tab.SelectedTab = contactlist2tab;

                // Detectar delimitador del archivo
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
                    if (!csv.Read() || !csv.ReadHeader())
                        throw new InvalidOperationException("El archivo CSV no contiene encabezados.");

                    var headers = csv.HeaderRecord?.ToList() ?? new List<string>();

                    // Columnas posibles (en/es)
                    string phoneCol = FirstExisting(headers,
                        "Phone 1 - Value", "Primary Phone", "Mobile Phone", "Phone", "Teléfono 1 - Valor", "Teléfono principal");

                    string nameCol = FirstExisting(headers,
                        "First Name", "Given Name", "Name", "Nombre");

                    if (string.IsNullOrEmpty(phoneCol) && string.IsNullOrEmpty(nameCol))
                        throw new InvalidOperationException("No se encontraron columnas de teléfono ni nombre en el CSV.");

                    // Preparar el DGV destino
                    contacts2dgv.SuspendLayout();
                    contacts2dgv.Columns.Clear();
                    contacts2dgv.Rows.Clear();

                    contacts2dgv.Columns.Add("colPhoneOrGroup", "Numero o Grupo");
                    contacts2dgv.Columns.Add("colName", "Nombre");
                    var sentIdx = contacts2dgv.Columns.Add("colSent", "Enviado (S/N)");
                    contacts2dgv.Columns[sentIdx].ReadOnly = true;

                    contacts2dgv.Columns[0].Width = 200;
                    contacts2dgv.Columns[1].Width = 350;
                    contacts2dgv.Columns[2].Width = 100;

                    // Leer filas (sin normalizar teléfonos)
                    while (csv.Read())
                    {
                        string phone = phoneCol != null ? (csv.GetField(phoneCol) ?? string.Empty) : string.Empty;
                        string name = nameCol != null ? (csv.GetField(nameCol) ?? string.Empty) : string.Empty;

                        if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(name))
                            continue;

                        contacts2dgv.Rows.Add(phone, name, string.Empty);
                    }

                    contacts2dgv.ResumeLayout();
                    MessageBox.Show("Datos importados!", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
            List<string> messages = new List<string>
            {
                m1txt.Text.Replace("\n", "<br/>"),
                m2txt.Text.Replace("\n", "<br/>"),
                m3txt.Text.Replace("\n", "<br/>"),
                m4txt.Text.Replace("\n", "<br/>"),
                m5txt.Text.Replace("\n", "<br/>")
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

            // Replace placeholders
            if (sendfullnamecb.Checked)
            {
                message = Regex.Replace(message, "{nombre}",
                    string.IsNullOrEmpty(contactName) ? "" : contactName);
            }

            if (senddatetimecb.Checked)
            {
                DateTime now = DateTime.Now;
                message = Regex.Replace(message, "{fecha}",
                    now.ToString("dddd, dd MMMM yyyy HH:mm"));
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
            wa.preventblocktiming = preventblockcb.Checked ? 4000 : 0;
            eachmessagetiming = eachmessagetimingcb.Checked
                ? Convert.ToInt32(eachmessagetimingtxt.Text) * 1000
                : 0;
        }
        private async Task<bool> ValidatePreSendConditions()
        {
            if (!CheckForInternetConnection())
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

            if (!wa.IfConnected(By.XPath("/html/body/div[1]/div/div/div[1]/div/div[3]/div/div[4]/header/header/div/div/h1/span")))
            {
                MessageBox.Show("Debe escanear el codigo QR para empezar a enviar",
                    "Observación", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            return true;
        }/*
        private void PrepareForSending2()
        {
            stopbtnclicked2 = false;
            rowcount2 = contacts2dgv.RowCount - 1;
            send2pbr.Value = 0;
            send2pbr.Maximum = rowcount2;
            totalmessages2lbl.Text = rowcount2.ToString();
            sendedmessage2 = 0;
            notsendedmessage2 = 0;

            start2btn.Enabled = false;
            pause2btn.Enabled = true;
            stop2btn.Enabled = true;
            logout2btn.Enabled = false;
            connectgoobtn.Enabled = false;

            loadmessage2lbl.Text = "Estado: Conectado...";
            contacts2dgv.AllowUserToAddRows = false;
            contacts2dgv.AllowUserToDeleteRows = false;

            wa.preventblocktiming2 = preventblock2cb.Checked ? 4000 : 0;
            eachmessagetiming2 = eachmessagetiming2cb.Checked
                ? Convert.ToInt32(eachmessagetiming2txt.Text) * 1000
                : 0;
        }

        private void FinalizeSending2()
        {
            if (!stopbtnclicked2)
            {
                MessageBox.Show("SMS enviados correctamente!", "Éxito");
            }

            stop2btn.Enabled = false;
            pause2btn.Enabled = false;
            start2btn.Enabled = true;
            logout2btn.Enabled = true;
            connectgoobtn.Enabled = true;

            notsendedmessage2lbl.Text = (rowcount2 - sendedmessage2).ToString();
            contacts2dgv.AllowUserToAddRows = true;
            contacts2dgv.AllowUserToDeleteRows = true;
        }*/
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

            int pauseEvery = Convert.ToInt32(severalpausetxt.Text);

            if (currentIndex == pauseEvery && !severalpausetoken.IsCancellationRequested)
            {
                MessageBox.Show(
                    $"Pausa automática después de {pauseEvery} mensajes.\nEsperando 15 minutos...",
                    "Pausa", MessageBoxButtons.OK, MessageBoxIcon.Information
                );

                await Task.Run(() =>
                {
                    try
                    {
                        Task.Delay(TimeSpan.FromMinutes(15), severalpausetoken.Token).Wait();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Pause cancelled: {ex.Message}");
                    }
                });
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

        private async Task Excecutesendtask()
        {
            if (!CheckForInternetConnection())
            {
                MessageBox.Show("No cuenta con acceso a internet, no puedes continuar.", "Observación",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ClearEmptyRows(contactsdgv);

            if (contactsdgv.Rows.Count < 2)
            {
                MessageBox.Show("No existen contactos a los que enviar!", "Observación",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (wa.IsBrowserClosed())
            {
                MessageBox.Show("El navegador está cerrado, no se puede enviar mensajes!, conecte otra vez presionando <Conectar WhatsApp>",
                    "Observación", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            // Verifica sesión WA
            if (!wa.IfConnected(By.XPath("//header/div[2]/div[1]/span[1]/div[2]/div[1]/span[1]")))
            {
                MessageBox.Show("Debe escanear el código QR para empezar a enviar", "Observación",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            // ---- Setup UI ----
            ToggleUiSendingState(isSending: true);

            rowcount = contactsdgv.RowCount - 1;
            sendpbr.Value = 0;
            sendpbr.Maximum = rowcount;
            totalmessageslbl.Text = rowcount.ToString();

            sendedmessage = 0;
            notsendedmessage = 0;
            notsendedmessagelbl.Text = sendedmessage.ToString();
            sendedmessagelbl.Text = notsendedmessage.ToString();

            // Timings
            wa.preventblocktiming = preventblockcb.Checked ? 4000 : 0;
            eachmessagetiming = eachmessagetimingcb.Checked
                ? Math.Max(0, SafeInt(eachmessagetimingtxt.Text)) * 1000
                : 0;

            // Prepara textos base (se respetan saltos con <br/> y sin normalizar teléfonos)
            var messages = new List<string>
            {
                m1txt.Text.Replace("\n", "<br/>"),
                m2txt.Text.Replace("\n", "<br/>"),
                m3txt.Text.Replace("\n", "<br/>"),
                m4txt.Text.Replace("\n", "<br/>"),
                m5txt.Text.Replace("\n", "<br/>")
            };

            int count = 0;
            string filename = filenametxt.Text?.Trim();
            bool hasFile = !string.IsNullOrEmpty(filename);

            try
            {
                foreach (DataGridViewRow fila in contactsdgv.Rows)
                {
                    if (fila.IsNewRow) continue;

                    if (!CheckForInternetConnection())
                    {
                        StopForNoInternet();
                        break;
                    }

                    // Variables por fila
                    string actualnumber = Convert.ToString(fila.Cells[0].Value);
                    string actualname   = Convert.ToString(fila.Cells[1].Value);

                    if (string.IsNullOrWhiteSpace(actualnumber))
                    {
                        MarkNotSent(fila);
                        UpdateProgress(++count);
                        continue;
                    }

                    // Aplicar pausa manual si corresponde (una sola vez)
                    await MaybeApplyManualPauseAsync(fila.Index);

                    // Pausa de usuario (pausetiming) si está activa
                    await MaybeApplyUserPauseAsync();

                    // Componer mensaje (una sola vez)
                    string messageToSend = ComposeMessage(messages, actualname);

                    // Abrir chat
                    bool chatOpened = await OpenChatAsync(actualnumber, cancellationToken.Token);
                    if (!chatOpened)
                    {
                        TryCleanEditor();
                        MarkNotSent(fila);
                        UpdateProgress(++count);
                        continue;
                    }

                    // Enviar según tipo
                    bool sentOk = false;
                    try
                    {
                        if (hasFile)
                        {
                            sentOk = await SendWithFileAsync(filetype, filename, messageToSend, actualnumber, cancellationToken.Token);
                        }
                        else
                        {
                            sentOk = await SendTextOnlyAsync(messageToSend, cancellationToken.Token);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        sentOk = false;
                    }

                    if (sentOk)
                    {
                        fila.Cells[2].Value = "S";
                        sendedmessage++;
                        sendedmessagelbl.Text = sendedmessage.ToString();
                    }
                    else
                    {
                        TryCleanEditor();
                        MarkNotSent(fila);
                    }

                    // Retraso anti-bloqueo / entre mensajes
                    await Task.Delay(1000 + wa.preventblocktiming, cancellationToken.Token);

                    // Pausa configurable entre mensajes
                    if (eachmessagetiming > 0 && wa.clickstate)
                        await Task.Delay(eachmessagetiming, eachmessagetoken.Token);

                    UpdateProgress(++count);
                }

                if (!stopbtnclicked)
                {
                    MessageBox.Show("Mensajes enviados correctamente! ", "Éxito",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                notsendedmessagelbl.Text = Convert.ToString(rowcount - sendedmessage);
            }
            catch (OperationCanceledException)
            {
                // Cancelado por tokens: no hacemos nada extra.
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                ToggleUiSendingState(isSending: false);
                contactsdgv.AllowUserToAddRows = true;
                contactsdgv.AllowUserToDeleteRows = true;
            }
        }

        /* ==================== Helpers ==================== */

        // Evita cast/format exceptions
        private int SafeInt(string s) => int.TryParse(s, out var v) ? v : 0;

        // Habilita/deshabilita controles durante el envío
        private void ToggleUiSendingState(bool isSending)
        {
            startbtn.Enabled = !isSending;
            pausebtn.Enabled = isSending;
            stopbtn.Enabled = isSending;
            logoutbtn.Enabled = !isSending;
            uploadbtn.Enabled = !isSending;
            clearfilenamebtn.Enabled = !isSending;
            connectwabtn.Enabled = !isSending;

            contactsdgv.AllowUserToAddRows = !isSending;
            contactsdgv.AllowUserToDeleteRows = !isSending;
            loadmessagelbl.Text = isSending ? "Estado: Conectado . . ." : "Estado: Inactivo";
        }

        // Pausa manual cada N mensajes (severalpausetxt)
        private async Task MaybeApplyManualPauseAsync(int rowIndex)
        {
            if (string.IsNullOrWhiteSpace(severalpausetxt.Text)) return;

            int threshold = SafeInt(severalpausetxt.Text);
            if (threshold <= 0) return;

            if (rowIndex == threshold && !severalpausetoken.IsCancellationRequested)
            {
                MessageBox.Show(
                    "El envio se pausó debido al <# mensajes para Pausar> designado en esta sección.\n" +
                    "Recomendamos esta pausa para no ser bloqueado en WhatsApp.\n" +
                    $"La pausa suele durar 15 minutos y se empezó el <{getTimeNow()}>, actualmente se pausa cada {severalpausetxt.Text} mensajes",
                    "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);

                await Task.Delay(TimeSpan.FromSeconds(900), severalpausetoken.Token);
            }
        }

        // Pausa de usuario (pausetiming) si aplica
        private async Task MaybeApplyUserPauseAsync()
        {
            if (pausetiming != 0)
            {
                try
                {
                    // Si ya tienes pausetimingaction que bloquea, puedes llamarla con Task.Run
                    await Task.Run(() => pausetimingaction(pausetiming, pauseToken.Token), pauseToken.Token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    pausetiming = 0;
                }
            }
        }

        // Arma el mensaje final (elige aleatorio si corresponde, reemplaza {nombre} y {fecha})
        private string ComposeMessage(IList<string> messages, string actualname)
        {
            string msg = string.Empty;

            if (manymessagescb.Checked)
            {
                var indices = NotEmptyMessages();       // Asumo que devuelve índices válidos
                if (indices.Count > 0)
                {
                    var rnd = new Random();
                    msg = messages[indices[rnd.Next(indices.Count)]];
                }
            }
            else
            {
                msg = messages[0];
            }

            if (sendfullnamecb.Checked)
            {
                msg = Regex.Replace(msg, "{nombre}",
                    string.IsNullOrWhiteSpace(actualname) ? "" : actualname);
            }

            if (senddatetimecb.Checked)
            {
                DateTime actualdate = getTimeNow();
                msg = Regex.Replace(msg, "{fecha}", actualdate.ToString("dddd, dd MMMM yyyy HH:mm"));
            }

            return msg;
        }

        // Abrir chat del contacto
        private async Task<bool> OpenChatAsync(string actualnumber, CancellationToken ct)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var action = new Actions(WA.driver);
                    wa.ClickSearchIcon();
                    wa.ContactSearch(actualnumber);
                    action.SendKeys(Keys.Space).Build().Perform();

                    wa.ContactClick();
                    Task.Delay(2000).Wait();
                    return wa.clickstate;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }, ct);
        }

        // Enviar solo texto
        private async Task<bool> SendTextOnlyAsync(string message, CancellationToken ct)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var action = new Actions(WA.driver);
                    wa.ContactMessage(message);
                    Task.Delay(1000 + wa.preventblocktiming).Wait();
                    wa.ContactActionEnter();
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }, ct);
        }

        // Enviar con archivo (I=imagen/video, A=audio, D=documento)
        // Mantiene tu lógica de adjunto + texto cuando aplica
        private async Task<bool> SendWithFileAsync(string filetype, string filename, string message, string number, CancellationToken ct)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var action = new Actions(WA.driver);

                    if (filetype == "I")
                    {
                        if (!CheckAttachMessageStatus())
                        {
                            wa.ImageMessage(filename);
                            Task.Delay(1000 + wa.preventblocktiming).Wait();
                            wa.ContactSend(By.XPath(WA.SendIADButton));
                        }
                        else
                        {
                            if (GetImageState(filename))
                            {
                                wa.ImageTextMessage(filename, message);
                                Task.Delay(1000 + wa.preventblocktiming).Wait();
                            }
                            else if (GetVideoState(filename))
                            {
                                wa.VideoTextMessage(filename, message);
                                action.SendKeys(".").Build().Perform();
                                action.SendKeys(Keys.Backspace + Keys.Backspace + Keys.Backspace + Keys.Backspace).Build().Perform();
                                Task.Delay(1000 + wa.preventblocktiming).Wait();
                                wa.ContactSend(By.XPath(WA.SendIADButton));
                            }
                            else
                            {
                                // Fallback: envia imagen y luego texto aparte
                                wa.ImageMessage(filename);
                                Task.Delay(1000 + wa.preventblocktiming).Wait();
                                wa.ContactSend(By.XPath(WA.SendIADButton));

                                Task.Delay(1000 + wa.preventblocktiming).Wait();
                                wa.ClickSearchIcon();
                                wa.ContactSearch(number);
                                action.SendKeys(Keys.Space).Build().Perform();
                                wa.ContactClick();
                                Task.Delay(1000 + wa.preventblocktiming).Wait();

                                wa.ContactMessage(message);
                                action.SendKeys("A").Build().Perform();
                                action.SendKeys(Keys.Backspace + Keys.Backspace + Keys.Backspace + Keys.Backspace).Build().Perform();
                                wa.ContactActionEnter();
                            }
                        }

                        return true;
                    }
                    else if (filetype == "A")
                    {
                        wa.ContactFileAudio(filename);
                        wa.ContactSend(By.XPath(WA.SendIADButton));
                        Task.Delay(1000 + wa.preventblocktiming).Wait();

                        // Si no hay campo de texto adjunto, enviar texto por separado
                        if (!CheckAttachMessageStatus())
                        {
                            wa.ClickSearchIcon();
                            wa.ContactSearch(number);
                            action.SendKeys(Keys.Space).Build().Perform();
                            wa.ContactClick();
                            Task.Delay(1000 + wa.preventblocktiming).Wait();

                            wa.ContactMessage(message);
                            action.SendKeys("A").Build().Perform();
                            action.SendKeys(Keys.Backspace + Keys.Backspace + Keys.Backspace + Keys.Backspace).Build().Perform();
                            wa.ContactActionEnter();
                        }

                        return true;
                    }
                    else if (filetype == "D")
                    {
                        wa.ContactFile(filename);
                        wa.ContactSend(By.XPath(WA.SendIADButton));
                        Task.Delay(1000 + wa.preventblocktiming).Wait();

                        if (!CheckAttachMessageStatus())
                        {
                            wa.ClickSearchIcon();
                            wa.ContactSearch(number);
                            action.SendKeys(Keys.Space).Build().Perform();
                            wa.ContactClick();
                            Task.Delay(1000 + wa.preventblocktiming).Wait();

                            wa.ContactMessage(message);
                            action.SendKeys("A").Build().Perform();
                            action.SendKeys(Keys.Backspace + Keys.Backspace + Keys.Backspace + Keys.Backspace).Build().Perform();
                            Task.Delay(1000 + wa.preventblocktiming).Wait();
                            wa.ContactActionEnter();
                        }

                        return true;
                    }

                    // Tipo desconocido => solo texto
                    wa.ContactMessage(message);
                    wa.ContactActionEnter();
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }, ct);
        }

        // Limpieza rápida del editor cuando falla la apertura o envío
        private void TryCleanEditor()
        {
            try
            {
                var action = new Actions(WA.driver);
                action.SendKeys(Keys.Backspace + Keys.Backspace + Keys.Backspace + Keys.Backspace).Build().Perform();
            }
            catch { /* no-op */ }
        }

        private void MarkNotSent(DataGridViewRow fila)
        {
            fila.Cells[2].Value = "N";
            notsendedmessage++;
            notsendedmessagelbl.Text = notsendedmessage.ToString();
        }

        private void UpdateProgress(int count)
        {
            if (count <= rowcount)
                sendpbr.Value = count;
        }

        private void StopForNoInternet()
        {
            stopbtn.Enabled = false;
            pausebtn.Enabled = false;
            startbtn.Enabled = true;
            MessageBox.Show("Se detuvieron los envíos de WA debido a que no cuenta con acceso a internet.",
                "Observación", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private async Task Excecutesendtask2()
        {

            if (CheckForInternetConnection())
            {

                //condicionales y token de cancellation



                string actualnumber = "";
                string actualname = "";




                //variables


                stopbtnclicked2 = false;
                rowcount2 = Convert.ToInt32(contacts2dgv.RowCount) - 1;
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



                                if (manymessages2cb.Checked == true)
                                {
                                    if (sms2txt.Text == "" || sms3txt.Text == "" || sms4txt.Text == "" || sms5txt.Text == "")
                                    {
                                        manymessages2cb.Checked = false;
                                        MessageBox.Show("No llenó todos los espacios de mensajes, no se usará la opción <Enviar varios textos en un solo envío>", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);



                                    }
                                }



                                foreach (DataGridViewRow fila in contacts2dgv.Rows)
                                {


                                    if (fila.IsNewRow) continue;




                                    if (CheckForInternetConnection())
                                    {

                                        if (preventblock2cb.Checked == true)
                                        {
                                            wa.preventblocktiming2 = 4000;
                                        }
                                        else
                                        {
                                            wa.preventblocktiming2 = 0;
                                        }




                                        if (eachmessagetiming2cb.Checked == true)
                                        {
                                            eachmessagetiming2 = Convert.ToInt32(eachmessagetiming2txt.Text) * 1000;
                                        }
                                        else
                                        {
                                            eachmessagetiming2 = 0;
                                        }







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
                                                            pausetimingaction(pausetiming2, pauseToken2.Token);
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


                                        if (fila.Index == Convert.ToInt32(severalpause2txt.Text) && !severalpausetoken2.IsCancellationRequested)
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

        private void pastedatabtn_Click(object sender, EventArgs e)
        {

            maintab.SelectedTab = contactlisttab;
            try
            {
                contactsdgv.Rows.Clear();
                contactsdgv.Refresh();

                string s = Clipboard.GetText();

                string[] lines = s.Replace("\n", "").Split('\r');

                contactsdgv.Rows.Add(lines.Length - 1);
                string[] fields;
                int row = 0;
                int col = 0;

                foreach (string item in lines)
                {
                    fields = item.Split('\t');
                    foreach (string f in fields)
                    {

                        contactsdgv[col, row].Value = f;
                        col++;
                    }
                    row++;
                    col = 0;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("pegar 2 COLUMNAS (NUMERO, NOMBRE DE CONTACTO) de EXCEL", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }




        }

        private void Storecontaacts()
        {
            ToJson(contactsdgv, "Contacts.json");
            ToJson(contacts2dgv, "Contacts2.json");
        }

        private void Storemessages()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "tempfilesWAButt"
            );
            Directory.CreateDirectory(path);

            File.WriteAllText(Path.Combine(path, "m1.txt"), m1txt.Text);
            File.WriteAllText(Path.Combine(path, "m2.txt"), m2txt.Text);
            File.WriteAllText(Path.Combine(path, "m3.txt"), m3txt.Text);
            File.WriteAllText(Path.Combine(path, "m4.txt"), m4txt.Text);
            File.WriteAllText(Path.Combine(path, "m5.txt"), m5txt.Text);

            File.WriteAllText(Path.Combine(path, "sms1.txt"), sms1txt.Text);
            File.WriteAllText(Path.Combine(path, "sms2.txt"), sms2txt.Text);
            File.WriteAllText(Path.Combine(path, "sms3.txt"), sms3txt.Text);
            File.WriteAllText(Path.Combine(path, "sms4.txt"), sms4txt.Text);
            File.WriteAllText(Path.Combine(path, "sms5.txt"), sms5txt.Text);
        }




        private void Restoremessages()
        {
            string basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "tempfilesWAButt"
            );

            if (File.Exists(Path.Combine(basePath, "m1.txt")))
                m1txt.Text = File.ReadAllText(Path.Combine(basePath, "m1.txt"));
            if (File.Exists(Path.Combine(basePath, "m2.txt")))
                m2txt.Text = File.ReadAllText(Path.Combine(basePath, "m2.txt"));
            if (File.Exists(Path.Combine(basePath, "m3.txt")))
                m3txt.Text = File.ReadAllText(Path.Combine(basePath, "m3.txt"));
            if (File.Exists(Path.Combine(basePath, "m4.txt")))
                m4txt.Text = File.ReadAllText(Path.Combine(basePath, "m4.txt"));
            if (File.Exists(Path.Combine(basePath, "m5.txt")))
                m5txt.Text = File.ReadAllText(Path.Combine(basePath, "m5.txt"));
        }
        private void Restoremessages2()
        {
            string basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "tempfilesWAButt"
            );

            if (File.Exists(Path.Combine(basePath, "sms1.txt")))
                sms1txt.Text = File.ReadAllText(Path.Combine(basePath, "sms1.txt"));
            if (File.Exists(Path.Combine(basePath, "sms2.txt")))
                sms2txt.Text = File.ReadAllText(Path.Combine(basePath, "sms2.txt"));
            if (File.Exists(Path.Combine(basePath, "sms3.txt")))
                sms3txt.Text = File.ReadAllText(Path.Combine(basePath, "sms3.txt"));
            if (File.Exists(Path.Combine(basePath, "sms4.txt")))
                sms4txt.Text = File.ReadAllText(Path.Combine(basePath, "sms4.txt"));
            if (File.Exists(Path.Combine(basePath, "sms5.txt")))
                sms5txt.Text = File.ReadAllText(Path.Combine(basePath, "sms5.txt"));
        }

        private void ToJson(DataGridView dgv, string filename)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("number", typeof(string));
            dt.Columns.Add("name", typeof(string));

            foreach (DataGridViewRow item in dgv.Rows)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(item.Cells[0].Value)))
                {
                    DataRow row = dt.NewRow();
                    row["number"] = Convert.ToString(item.Cells[0].Value);
                    row["name"] = Convert.ToString(item.Cells[1].Value);
                    dt.Rows.Add(row);
                }
            }

            string json = JsonConvert.SerializeObject(dt);
            WriteJSONToFile(json, filename);
        }
        public void ReadJsonContacts(string filename)
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "tempfilesWAButt", filename
            );

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                DataTable dt = JsonConvert.DeserializeObject<DataTable>(json);

                foreach (DataRow item in dt.Rows)
                {
                    contactsdgv.Rows.Add(item[0], item[1]);
                }
            }
        }
        public void ReadJson2Contacts(string filename)
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "tempfilesWAButt", filename
            );

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                DataTable dt = JsonConvert.DeserializeObject<DataTable>(json);

                foreach (DataRow item in dt.Rows)
                {
                    contacts2dgv.Rows.Add(item[0], item[1]);
                }
            }
        }
        private string DecodeEncodedNonAsciiCharacters(string value)
        {
            return Regex.Replace(
                value,
                @"\\u(?<Value>[a-zA-Z0-9]{4})",
                m => {
                    return ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString();
                });
        }
        private List<int> NotEmptyMessages()
        {
            List<int> result = new List<int>();

            if (!string.IsNullOrWhiteSpace(m1txt.Text)) result.Add(0);
            if (!string.IsNullOrWhiteSpace(m2txt.Text)) result.Add(1);
            if (!string.IsNullOrWhiteSpace(m3txt.Text)) result.Add(2);
            if (!string.IsNullOrWhiteSpace(m4txt.Text)) result.Add(3);
            if (!string.IsNullOrWhiteSpace(m5txt.Text)) result.Add(4);

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

                            string value = "";


                            DataGridViewRow dr = new DataGridViewRow();
                            StreamWriter swOut = new StreamWriter(sfd.FileName);



                            //write DataGridView rows to csv
                            for (int j = 0; j <= contactsdgv.Rows.Count - 2; j++)
                            {
                                if (j > 0)
                                {
                                    swOut.WriteLine();
                                }

                                dr = contactsdgv.Rows[j];

                                for (int i = 0; i <= contactsdgv.Columns.Count - 2; i++)
                                {
                                    if (i > 0)
                                    {
                                        swOut.Write("\t");
                                    }
                                    if (i < 1)
                                    {
                                        if (Convert.ToString(dr.Cells[i].Value).Replace(" ", "").Length > 9)
                                        {

                                            if (Convert.ToString(dr.Cells[i].Value).StartsWith("+") == false && IsDigitsOnly(Convert.ToString(dr.Cells[i].Value)))
                                            {
                                                swOut.Write("+");
                                            }



                                        }

                                    }

                                    value = Convert.ToString(dr.Cells[i].Value);


                                    //replace comma's with spaces
                                    value = value.Replace('\t', ' ');
                                    //replace embedded newlines with spaces
                                    value = value.Replace(Environment.NewLine, " ");

                                    swOut.Write(value);
                                }
                            }
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
            else
            {
                MessageBox.Show("No hay datos a exportar", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }




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




                    StreamReader sr = new StreamReader(sfd.FileName);
                    StringBuilder sb = new StringBuilder();


                    string s;

                    contactsdgv.Columns.Clear();


                    contactsdgv.Columns.Add("Column", "Numero o Grupo");
                    contactsdgv.Columns.Add("Column", "Nombre");
                    contactsdgv.Columns.Add("Column", "Enviado (S/N)");

                    while (!sr.EndOfStream)
                    {
                        s = sr.ReadLine();

                        string[] str = s.Split('\t');



                        contactsdgv.Rows.Add(str[0].ToString(), str[1].ToString());


                    }
                    sr.Close();

                    DataGridViewColumn column = contactsdgv.Columns[0];
                    column.Width = 200;



                    DataGridViewColumn column1 = contactsdgv.Columns[1];
                    column1.Width = 350;




                    DataGridViewColumn column2 = contactsdgv.Columns[2];
                    column2.Width = 100;
                    column2.ReadOnly = true;

                    MessageBox.Show("Datos importados!", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    maintab.SelectedTab = contactlisttab;

                }

            }





        }

        private void minutosToolStripMenuItem_Click(object sender, EventArgs e)
        {



            if (pausetiming == 0)
            {
                pauseToken = new CancellationTokenSource();

                pausetiming = 300;
                pausebtn.Text = "Reanudar";
                MessageBox.Show("Los envíos se pausarán en breve.", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                logoutbtn.Enabled = true;
            }



        }

        private void minutosToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            pauseToken = new CancellationTokenSource();

            if (pausetiming == 0)
            {
                pausetiming = 1800;
                pausebtn.Text = "Reanudar";
                MessageBox.Show("Los envíos se pausarán en breve.", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                logoutbtn.Enabled = true;
            }


        }

        private void horaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pauseToken = new CancellationTokenSource();
            if (pausetiming == 0)
            {
                pausetiming = 3600;
                pausebtn.Text = "Reanudar";
                MessageBox.Show("Los envíos se pausarán en breve.", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                logoutbtn.Enabled = true;
            }

        }

        private void horaToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            pauseToken = new CancellationTokenSource();
            if (pausetiming == 0)
            {
                pausetiming = 7200;
                pausebtn.Text = "Reanudar";
                MessageBox.Show("Los envíos se pausarán en breve.", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                logoutbtn.Enabled = true;
            }


        }

        private void pausetimingaction(int seconds, CancellationToken token)
        {
            try
            {
                if (seconds > 0)
                {
                    MessageBox.Show($"Pausando por {seconds / 60} minutos", "Pausa");
                    Task.Delay(TimeSpan.FromSeconds(seconds), token).Wait();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Pause cancelled: {ex.Message}");
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

                    if (!CheckForInternetConnection())
                    {
                        StopSendingDueToNoInternet();
                        break;
                    }

                    // Process contact
                    await ProcessSingleContact(fila);

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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in sending: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Error");
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
                if (!CheckForInternetConnection())
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
        public static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("http://google.com/generate_204"))
                    return true;
            }
            catch
            {
                return false;
            }
        }
        private void manymessagescb_Click(object sender, EventArgs e)
        {

        }
        private static DateTime getTimeNow()
        {
            /*
                var client = new TcpClient("time.nist.gov", 13);
                using (var streamReader = new StreamReader(client.GetStream()))
                {



                    var response = streamReader.ReadToEnd();
                    var utcDateTimeString = response.Substring(7, 17);
                    var localDateTime = DateTime.ParseExact(utcDateTimeString, "yy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

                    return localDateTime;



                }
                */



            DateTime localDate = DateTime.Now;
            String cultureName = "es-PE";


            var culture = new CultureInfo(cultureName);
            string res = localDate.ToString(culture);

            return Convert.ToDateTime(res);


        }
        private void getcontactsfromgroupbtn_Click(object sender, EventArgs e)
        {
            // wa.GetContactsFromGroup();
        }
        private bool IsDigitsOnly(string str)
        {
            return str.All(c => char.IsDigit(c));
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



            if (apptab.SelectedTab == wabottab)
            {
                colorpanel.BackColor = Color.FromArgb(8, 112, 100);
            }

            else
            {
                colorpanel.BackColor = Color.FromArgb(19, 116, 233);
            }



        }
        private async void connectgoobtn_Click(object sender, EventArgs e)
        {
            if (!CheckForInternetConnection())
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
            main2tab.SelectedTab = contactlist2tab;
            try
            {
                contacts2dgv.Rows.Clear();
                contacts2dgv.Refresh();

                string s = Clipboard.GetText();

                string[] lines = s.Replace("\n", "").Split('\r');

                contacts2dgv.Rows.Add(lines.Length - 1);
                string[] fields;
                int row = 0;
                int col = 0;

                foreach (string item in lines)
                {
                    fields = item.Split('\t');
                    foreach (string f in fields)
                    {

                        contacts2dgv[col, row].Value = f;
                        col++;
                    }
                    row++;
                    col = 0;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Solo se pueden pegar 2 COLUMNAS (NUMERO, NOMBRE DE CONTACTO) de EXCEL", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
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

                            string value = "";


                            DataGridViewRow dr = new DataGridViewRow();
                            StreamWriter swOut = new StreamWriter(sfd.FileName);



                            //write DataGridView rows to csv
                            for (int j = 0; j <= contacts2dgv.Rows.Count - 2; j++)
                            {
                                if (j > 0)
                                {
                                    swOut.WriteLine();
                                }

                                dr = contacts2dgv.Rows[j];

                                for (int i = 0; i <= contacts2dgv.Columns.Count - 2; i++)
                                {
                                    if (i > 0)
                                    {
                                        swOut.Write("\t");
                                    }
                                    if (i < 1)
                                    {
                                        if (Convert.ToString(dr.Cells[i].Value).Replace(" ", "").Length > 9)
                                        {

                                            if (Convert.ToString(dr.Cells[i].Value).StartsWith("+") == false && IsDigitsOnly(Convert.ToString(dr.Cells[i].Value)))
                                            {
                                                swOut.Write("+");
                                            }



                                        }

                                    }

                                    value = Convert.ToString(dr.Cells[i].Value);


                                    //replace comma's with spaces
                                    value = value.Replace('\t', ' ');
                                    //replace embedded newlines with spaces
                                    value = value.Replace(Environment.NewLine, " ");

                                    swOut.Write(value);
                                }
                            }
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




                    StreamReader sr = new StreamReader(sfd.FileName);
                    StringBuilder sb = new StringBuilder();


                    string s;

                    contacts2dgv.Columns.Clear();


                    contacts2dgv.Columns.Add("Column", "Numero o Grupo");
                    contacts2dgv.Columns.Add("Column", "Nombre");
                    contacts2dgv.Columns.Add("Column", "Enviado (S/N)");

                    while (!sr.EndOfStream)
                    {
                        s = sr.ReadLine();

                        string[] str = s.Split('\t');



                        contacts2dgv.Rows.Add(str[0].ToString(), str[1].ToString());


                    }
                    sr.Close();

                    DataGridViewColumn column = contacts2dgv.Columns[0];
                    column.Width = 200;



                    DataGridViewColumn column1 = contacts2dgv.Columns[1];
                    column1.Width = 350;




                    DataGridViewColumn column2 = contacts2dgv.Columns[2];
                    column2.Width = 100;
                    column2.ReadOnly = true;

                    MessageBox.Show("Datos importados!", "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    main2tab.SelectedTab = contactlist2tab;

                }

            }
        }
        private async Task ProcessSingleContact(DataGridViewRow fila)
        {
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

            // Prepare message
            string messageToSend = PrepareMessage(contactName);

            // Search and click contact
            await SearchAndClickContact(contactNumber);

            if (!wa.clickstate)
            {
                fila.Cells[2].Value = "N";
                notsendedmessage++;
                notsendedmessagelbl.Text = notsendedmessage.ToString();
                return;
            }

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
            for (int i = dgv.Rows.Count - 1; i >= 0; i--)
            {
                DataGridViewRow row = dgv.Rows[i];
                if (!row.IsNewRow && string.IsNullOrEmpty(Convert.ToString(row.Cells[0].Value)))
                {
                    dgv.Rows.RemoveAt(i);
                }
            }
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
            DataTable items = new DataTable();

            items.Columns.Add("Numero o Grupo", typeof(string));
            items.Columns.Add("Nombre", typeof(string));
            items.Columns.Add("Enviado(S/N)", typeof(string));

            for (int i = 0; i < contactsdgv.Rows.Count; i++)
            {
                DataRow rw = items.NewRow();
                rw[0] = Convert.ToString(contactsdgv.Rows[i].Cells[0].Value);
                rw[1] = Convert.ToString(contactsdgv.Rows[i].Cells[1].Value);
                rw[2] = Convert.ToString(contactsdgv.Rows[i].Cells[2].Value);
                if (!items.Rows.Cast<DataRow>().Any(row => row["Numero o Grupo"].Equals(rw["Numero o Grupo"])))
                    items.Rows.Add(rw);
            }



            contactsdgv.Rows.Clear();


            foreach (DataRow item in items.Rows)
            {
                contactsdgv.Rows.Add(Convert.ToString(item[0]), Convert.ToString(item[1]), Convert.ToString(item[2]));
            }




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
            DataTable items = new DataTable();

            items.Columns.Add("Numero o Grupo", typeof(string));
            items.Columns.Add("Nombre", typeof(string));
            items.Columns.Add("Enviado(S/N)", typeof(string));

            for (int i = 0; i < contacts2dgv.Rows.Count; i++)
            {
                DataRow rw = items.NewRow();
                rw[0] = Convert.ToString(contacts2dgv.Rows[i].Cells[0].Value);
                rw[1] = Convert.ToString(contacts2dgv.Rows[i].Cells[1].Value);
                rw[2] = Convert.ToString(contacts2dgv.Rows[i].Cells[2].Value);
                if (!items.Rows.Cast<DataRow>().Any(row => row["Numero o Grupo"].Equals(rw["Numero o Grupo"])))
                    items.Rows.Add(rw);
            }



            contacts2dgv.Rows.Clear();


            foreach (DataRow item in items.Rows)
            {
                contacts2dgv.Rows.Add(Convert.ToString(item[0]), Convert.ToString(item[1]), Convert.ToString(item[2]));
            }




            DataGridViewColumn column = contacts2dgv.Columns[0];
            column.Width = 200;



            DataGridViewColumn column1 = contacts2dgv.Columns[1];
            column1.Width = 350;




            DataGridViewColumn column2 = contacts2dgv.Columns[2];
            column2.Width = 100;
            column2.ReadOnly = true;
        }
        private void dgvwacopymodecms_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void copiarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SendKeys.Send("^C");
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
            return !(!CheckAttachMessageStatusSub() || sendonlyattachcb.Checked);
        }

        private bool CheckAttachMessageStatusSub()
        {
            return string.IsNullOrEmpty(filenametxt.Text) ||
                   !string.IsNullOrWhiteSpace(m1txt.Text) ||
                   !string.IsNullOrWhiteSpace(m2txt.Text) ||
                   !string.IsNullOrWhiteSpace(m3txt.Text) ||
                   !string.IsNullOrWhiteSpace(m4txt.Text) ||
                   !string.IsNullOrWhiteSpace(m5txt.Text);
        }

        private string GetExtension(string path)
        {
            return Path.GetExtension(path);
        }
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

        private void eliminarDuplicadosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteDuplicate1();
        }

        private void eliminarFilasVaciasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearEmptyRows(contactsdgv);
        }

        private void pegarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {

                string s = Clipboard.GetText();

                string[] lines = s.Replace("\n", "").Split('\r');

                string[] fields;
                int row = contactsdgv.CurrentCell.RowIndex;
                int col = 0;
                int sum = row + lines.Length;
                int totalrows = contactsdgv.Rows.Cast<DataGridViewRow>().Where(rown => !(rown.Cells[0].Value == null && rown.Cells[1].Value == null)).Count();

                Console.WriteLine(lines.Length);
                Console.WriteLine(row + 2);
                Console.WriteLine(totalrows);


                for (int i = 0; i < sum - totalrows; i++)
                {
                    contactsdgv.Rows.Add();
                }



                foreach (string item in lines)
                {

                    fields = item.Split('\t');
                    foreach (string f in fields)
                    {



                        contactsdgv[col, row].Value = f;



                        col++;



                    }

                    row++;

                    col = 0;
                }

                foreach (DataGridViewRow item in contactsdgv.Rows)
                {
                    item.Cells[2].Value = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Observación", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
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
            SendKeys.Send("^C");
        }

        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            try
            {

                string s = Clipboard.GetText();
                
                string[] lines = s.Replace("\n", "").Split('\r');

                string[] fields;
                int row = contacts2dgv.CurrentCell.RowIndex;
                int col = 0;
                int sum = row + lines.Length;
                int totalrows = contacts2dgv.Rows.Cast<DataGridViewRow>().Where(rown => !(rown.Cells[0].Value == null && rown.Cells[1].Value == null)).Count();

                Console.WriteLine(lines.Length);
                Console.WriteLine(row + 2);
                Console.WriteLine(totalrows);


                for (int i = 0; i < sum - totalrows; i++)
                {
                    contacts2dgv.Rows.Add();
                }



                foreach (string item in lines)
                {

                    fields = item.Split('\t');
                    foreach (string f in fields)
                    {



                        contacts2dgv[col, row].Value = f;



                        col++;



                    }

                    row++;

                    col = 0;
                }

                foreach (DataGridViewRow item in contacts2dgv.Rows)
                {
                    item.Cells[2].Value = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Observación", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
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
    }
    
}