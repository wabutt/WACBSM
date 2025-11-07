using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using SeleniumKeys = OpenQA.Selenium.Keys;

namespace Presentation
{
    /// <summary>
    /// Clase principal para automatización de WhatsApp Web y Google Messages
    /// </summary>
    public class WA
    {
        #region Fields

        public static IWebDriver driver;
        public static IWebDriver driver2;

        public bool driverstate;
        public bool driverstate2;
        public bool clickstate = false;
        public int preventblocktiming = 0;
        public int preventblocktiming2 = 0;

        private WebDriverWait _wait;
        private WebDriverWait _wait2;

        #endregion

        #region Selectors - WhatsApp Web

        /// <summary>
        /// Selectores optimizados para WhatsApp Web
        /// </summary>
        public static class WASelectors
        {
            // Search
            public static By SearchButton => By.CssSelector("button[aria-label*='Search']");
            public static By SearchInput => By.CssSelector("div[contenteditable='true'][data-tab='3']");

            // Messaging
            public static By MessageInput => By.CssSelector("div[contenteditable='true'][data-tab='10']");
            public static By SendButton => By.CssSelector("button[aria-label*='Send']");

            // Attachments
            public static By AttachButton => By.CssSelector("button[aria-label*='Attach']");
            public static By AttachImageInput => By.CssSelector("input[accept*='image'][type='file']");
            public static By AttachDocumentInput => By.CssSelector("input[accept*='*'][type='file']");
            public static By ImageCaptionInput => By.CssSelector("div[contenteditable='true'][data-tab='1']");
            public static By SendAttachmentButton => By.CssSelector("span[data-icon='send']");

            // Navigation
            public static By FirstSearchResult => By.CssSelector("div[role='listitem']");
            public static By ConversationHeader => By.CssSelector("header div._amid");

            // Menu
            public static By MenuButton => By.CssSelector("button[aria-label*='Menu']");
            public static By LogoutOption => By.XPath("//div[@role='button']//span[contains(text(),'Log out')]");
            

        }

        #endregion
        public static string SendIADButton => "//span[@data-icon='send']";

        #region Selectors - Google Messages

        /// <summary>
        /// Selectores para Google Messages (SMS)
        /// </summary>
        public static class SMSSelectors
        {
            public static By NewMessageButton => By.CssSelector("a[aria-label*='Start chat']");
            public static By SearchInput => By.CssSelector("input[placeholder*='Search']");
            public static By MessageInput => By.CssSelector("textarea[placeholder*='Text message']");
            public static By SendButton => By.CssSelector("button[aria-label*='Send']");
            public static By FirstContact => By.CssSelector("mws-conversation-list-item");
        }

        #endregion

        #region Browser Launch - WhatsApp

        /// <summary>
        /// Lanza navegador Chrome para WhatsApp Web
        /// </summary>
        public async Task LaunchBrowser(CancellationToken ct = default)
        {
            try
            {
                // Verificar si ya existe una instancia
                if (driver != null && driverstate && !IsBrowserClosed())
                {
                    Console.WriteLine("✓ Driver WhatsApp ya está activo");
                    return;
                }
            }
            catch { }

            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var driverDir = Path.Combine(docs, "tempfilesWAButt", "webdriver");
            var userDataRoot = Path.Combine(docs, "tempfilesWAButt", "Chrome WA Profile");
            const string profileName = "Default";

            // Crear directorios
            Directory.CreateDirectory(driverDir);
            Directory.CreateDirectory(userDataRoot);

            await Task.Run(() =>
            {
                ChromeDriverService service = null;
                try
                {
                    ct.ThrowIfCancellationRequested();

                    // Configurar servicio
                    service = ChromeDriverService.CreateDefaultService(driverDir);
                    service.HideCommandPromptWindow = true;
                    service.SuppressInitialDiagnosticInformation = true;

                    // Configurar opciones
                    var options = new ChromeOptions();
                    options.AddArguments(
                        $"--user-data-dir={userDataRoot}",
                        $"--profile-directory={profileName}",
                        "--window-size=850,650",
                        "--disable-notifications",
                        "--no-default-browser-check",
                        "--disable-popup-blocking",
                        "--disable-blink-features=AutomationControlled",
                        "--lang=es-PE"
                    );

                    // Reducir detección de automatización
                    options.AddExcludedArgument("enable-automation");
                    options.AddAdditionalOption("useAutomationExtension", false);
                    options.PageLoadStrategy = PageLoadStrategy.Normal;

                    // Crear driver
                    driver = new ChromeDriver(service, options);

                    // Configurar timeouts
                    driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
                    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
                    driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(30);

                    // Inicializar wait
                    _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

                    // Navegar a WhatsApp
                    driver.Navigate().GoToUrl("https://web.whatsapp.com/");

                    driverstate = true;
                    Console.WriteLine("✓ WhatsApp Web iniciado correctamente");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("⚠️ Inicio cancelado");
                    driverstate = false;
                    service?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error iniciando WhatsApp: {ex.Message}");
                    driverstate = false;
                    service?.Dispose();
                    throw;
                }
            }, ct);
        }

        #endregion

        #region Browser Launch - Google Messages

        /// <summary>
        /// Lanza navegador Chrome para Google Messages
        /// </summary>
        public async Task LaunchBrowser2(CancellationToken ct = default)
        {
            try
            {
                if (driver2 != null && driverstate2 && !IsBrowserClosed2())
                {
                    Console.WriteLine("✓ Driver SMS ya está activo");
                    return;
                }
            }
            catch { }

            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var driverDir = Path.Combine(docs, "tempfilesWAButt", "webdriver");
            var userDataRoot = Path.Combine(docs, "tempfilesWAButt", "Chrome SMS Profile");
            const string profileName = "Default";

            Directory.CreateDirectory(driverDir);
            Directory.CreateDirectory(userDataRoot);

            await Task.Run(() =>
            {
                ChromeDriverService service = null;
                try
                {
                    ct.ThrowIfCancellationRequested();

                    service = ChromeDriverService.CreateDefaultService(driverDir);
                    service.HideCommandPromptWindow = true;
                    service.SuppressInitialDiagnosticInformation = true;

                    var options = new ChromeOptions();
                    options.AddArguments(
                        $"--user-data-dir={userDataRoot}",
                        $"--profile-directory={profileName}",
                        "--window-size=850,650",
                        "--disable-notifications",
                        "--no-default-browser-check",
                        "--disable-popup-blocking",
                        "--disable-blink-features=AutomationControlled",
                        "--lang=es-PE"
                    );

                    options.AddExcludedArgument("enable-automation");
                    options.AddAdditionalOption("useAutomationExtension", false);
                    options.PageLoadStrategy = PageLoadStrategy.Eager;

                    driver2 = new ChromeDriver(service, options);

                    driver2.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(8);
                    driver2.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
                    driver2.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(30);

                    _wait2 = new WebDriverWait(driver2, TimeSpan.FromSeconds(20));

                    driver2.Navigate().GoToUrl("https://messages.google.com/web/conversations");

                    driverstate2 = true;
                    Console.WriteLine("✓ Google Messages iniciado correctamente");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("⚠️ Inicio SMS cancelado");
                    driverstate2 = false;
                    service?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error iniciando SMS: {ex.Message}");
                    driverstate2 = false;
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    service?.Dispose();
                }
            }, ct);
        }

        #endregion

        #region WhatsApp - Search & Navigation

        /// <summary>
        /// Click en el botón de búsqueda
        /// </summary>
        public void ClickSearchIcon()
        {
            try
            {
                var searchBtn = WaitForElement(WASelectors.SearchButton, 10);
                searchBtn?.Click();
                Console.WriteLine("✓ Search icon clicked");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error clicking search: {ex.Message}");
            }
        }

        /// <summary>
        /// Buscar contacto en WhatsApp
        /// </summary>
        public void ContactSearch(string tosearch)
        {
            try
            {
                var searchInput = WaitForElement(WASelectors.SearchInput, 20);

                if (searchInput != null)
                {
                    searchInput.Clear();
                    searchInput.SendKeys(tosearch);
                    Thread.Sleep(500); // Esperar resultados
                    Console.WriteLine($"✓ Buscando: {tosearch}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error en búsqueda: {ex.Message}");
            }
        }

        /// <summary>
        /// Click en el primer resultado de búsqueda
        /// </summary>
        public void ContactClick()
        {
            try
            {
                var firstResult = WaitForElement(WASelectors.FirstSearchResult, 6);

                if (firstResult != null)
                {
                    new Actions(driver).SendKeys(SeleniumKeys.Enter).Build().Perform();
                    clickstate = true;
                    Thread.Sleep(1000);
                    Console.WriteLine("✓ Contacto seleccionado");
                }
                else
                {
                    clickstate = false;
                    Console.WriteLine("✗ Contacto no encontrado");
                    ClickSearchIcon();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error al hacer clic: {ex.Message}");
                clickstate = false;
            }
        }

        #endregion

        #region WhatsApp - Messaging

        /// <summary>
        /// Escribir mensaje en el chat
        /// </summary>
        public void ContactMessage(string message)
        {
            try
            {
                var messageBox = WaitForElement(WASelectors.MessageInput, 20);

                if (messageBox != null)
                {
                    // Usar JavaScript para evitar problemas con caracteres especiales
                    var js = (IJavaScriptExecutor)driver;

                    // Limpiar y establecer contenido
                    js.ExecuteScript("arguments[0].textContent = arguments[1];", messageBox, message);

                    // Disparar eventos para que WhatsApp detecte el cambio
                    js.ExecuteScript(@"
                        arguments[0].dispatchEvent(new Event('input', { bubbles: true }));
                        arguments[0].dispatchEvent(new Event('change', { bubbles: true }));
                    ", messageBox);

                    Console.WriteLine("✓ Mensaje escrito");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error escribiendo mensaje: {ex.Message}");
            }
        }

        /// <summary>
        /// Enviar mensaje (presionar Enter)
        /// </summary>
        public void ContactActionEnter()
        {
            try
            {
                new Actions(driver).SendKeys(SeleniumKeys.Enter).Build().Perform();
                Thread.Sleep(500);
                Console.WriteLine("✓ Mensaje enviado");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error enviando: {ex.Message}");
            }
        }

        #endregion

        #region WhatsApp - Attachments

        /// <summary>
        /// Adjuntar imagen/video
        /// </summary>
        public void ImageMessage(string filePath)
        {
            try
            {
                // Click en botón adjuntar
                var attachBtn = WaitForElement(WASelectors.AttachButton, 10);
                attachBtn?.Click();
                Thread.Sleep(500);

                // Enviar archivo
                var fileInput = WaitForElement(WASelectors.AttachImageInput, 10);
                fileInput?.SendKeys(filePath);

                Thread.Sleep(1000 + preventblocktiming);
                Console.WriteLine($"✓ Imagen adjuntada: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error adjuntando imagen: {ex.Message}");
            }
        }

        /// <summary>
        /// Adjuntar imagen con caption
        /// </summary>
        public void ImageTextMessage(string filePath, string caption)
        {
            try
            {
                ImageMessage(filePath);
                Thread.Sleep(1000);

                // Escribir caption
                var captionBox = WaitForElement(WASelectors.ImageCaptionInput, 10);

                if (captionBox != null)
                {
                    var js = (IJavaScriptExecutor)driver;
                    js.ExecuteScript("arguments[0].textContent = arguments[1];", captionBox, caption);
                    js.ExecuteScript(@"
                        arguments[0].dispatchEvent(new Event('input', { bubbles: true }));
                    ", captionBox);

                    Console.WriteLine("✓ Caption agregado");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error con caption: {ex.Message}");
            }
        }

        /// <summary>
        /// Adjuntar imagen con caption (video)
        /// </summary>
        public void VideoTextMessage(string filePath, string caption)
        {
            // Mismo proceso que imagen
            ImageTextMessage(filePath, caption);
        }

        /// <summary>
        /// Adjuntar documento
        /// </summary>
        public void ContactFile(string filePath)
        {
            try
            {
                var attachBtn = WaitForElement(WASelectors.AttachButton, 10);
                attachBtn?.Click();
                Thread.Sleep(500);

                var fileInput = WaitForElement(WASelectors.AttachDocumentInput, 10);
                fileInput?.SendKeys(filePath);

                Thread.Sleep(2000 + preventblocktiming);
                Console.WriteLine($"✓ Documento adjuntado: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error adjuntando documento: {ex.Message}");
            }
        }

        /// <summary>
        /// Adjuntar audio
        /// </summary>
        public void ContactFileAudio(string audioPath)
        {
            try
            {
                var attachBtn = WaitForElement(WASelectors.AttachButton, 10);
                attachBtn?.Click();
                Thread.Sleep(500);

                // Los audios usan el mismo input que imágenes
                var fileInput = WaitForElement(WASelectors.AttachImageInput, 10);
                fileInput?.SendKeys(audioPath);

                Thread.Sleep(1000 + preventblocktiming);
                Console.WriteLine($"✓ Audio adjuntado: {Path.GetFileName(audioPath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error adjuntando audio: {ex.Message}");
            }
        }

        /// <summary>
        /// Enviar attachment
        /// </summary>
        public void ContactSend(By selector)
        {
            try
            {
                new Actions(driver).SendKeys(SeleniumKeys.Enter).Build().Perform();
                Thread.Sleep(500);
                Console.WriteLine("✓ Attachment enviado");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error enviando attachment: {ex.Message}");
            }
        }

        #endregion

        #region Google Messages (SMS)

        /// <summary>
        /// Click en botón de nueva conversación (SMS)
        /// </summary>
        public void ClickSearchIcon2()
        {
            try
            {
                var newMessageBtn = WaitForElement(SMSSelectors.NewMessageButton, 10, driver2);
                newMessageBtn?.Click();
                Console.WriteLine("✓ New message clicked");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Buscar contacto en SMS
        /// </summary>
        public void ContactSearch2(string number)
        {
            try
            {
                var searchInput = WaitForElement(SMSSelectors.SearchInput, 20, driver2);

                if (searchInput != null)
                {
                    searchInput.Clear();
                    searchInput.SendKeys(number);
                    Thread.Sleep(500);
                    Console.WriteLine($"✓ Buscando SMS: {number}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error SMS búsqueda: {ex.Message}");
            }
        }

        /// <summary>
        /// Click en contacto SMS
        /// </summary>
        public void ContactClick2()
        {
            try
            {
                new Actions(driver2).SendKeys(SeleniumKeys.Enter).Build().Perform();
                Thread.Sleep(1000);
                Console.WriteLine("✓ Contacto SMS seleccionado");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error SMS click: {ex.Message}");
            }
        }

        /// <summary>
        /// Escribir mensaje SMS
        /// </summary>
        public void ContactMessage2(string message)
        {
            try
            {
                var messageBox = WaitForElement(SMSSelectors.MessageInput, 20, driver2);

                if (messageBox != null)
                {
                    messageBox.Clear();
                    messageBox.SendKeys(message);
                    Console.WriteLine("✓ Mensaje SMS escrito");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error SMS mensaje: {ex.Message}");
            }
        }

        /// <summary>
        /// Enviar mensaje SMS
        /// </summary>
        public void ContactActionEnter2()
        {
            try
            {
                new Actions(driver2).SendKeys(SeleniumKeys.Enter).Build().Perform();
                Thread.Sleep(500);
                Console.WriteLine("✓ SMS enviado");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error enviando SMS: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Esperar por un elemento con timeout
        /// </summary>
        private IWebElement WaitForElement(By locator, int timeoutSeconds, IWebDriver driverInstance = null)
        {
            try
            {
                var targetDriver = driverInstance ?? driver;
                var wait = new WebDriverWait(targetDriver, TimeSpan.FromSeconds(timeoutSeconds));

                return wait.Until(d =>
                {
                    try
                    {
                        var element = d.FindElement(locator);
                        return element.Displayed && element.Enabled ? element : null;
                    }
                    catch
                    {
                        return null;
                    }
                });
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine($"⚠️ Timeout esperando elemento: {locator}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error esperando elemento: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Verificar si elemento existe
        /// </summary>
        public static bool FindElement(IWebDriver driver, By by, int timeoutInSeconds)
        {
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutInSeconds));
                wait.Until(d => d.FindElement(by));
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Connection Status

        /// <summary>
        /// Verificar si WhatsApp está conectado
        /// </summary>
        public bool IfConnected(By by)
        {
            try
            {
                driver.FindElement(by);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        /// <summary>
        /// Verificar si SMS está conectado
        /// </summary>
        public bool IfConnected2(By by)
        {
            try
            {
                driver2.FindElement(by);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        /// <summary>
        /// Verificar si navegador WhatsApp está cerrado
        /// </summary>
        public bool IsBrowserClosed()
        {
            try
            {
                var title = driver?.Title;
                return false;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Verificar si navegador SMS está cerrado
        /// </summary>
        public bool IsBrowserClosed2()
        {
            try
            {
                var title = driver2?.Title;
                return false;
            }
            catch
            {
                return true;
            }
        }

        #endregion

        #region Logout & Cleanup

        /// <summary>
        /// Cerrar sesión en WhatsApp
        /// </summary>
        public void LogoutWA()
        {
            try
            {
                // Click en menú
                var menuBtn = WaitForElement(WASelectors.MenuButton, 5);
                menuBtn?.Click();
                Thread.Sleep(500);

                // Click en logout
                var logoutBtn = WaitForElement(WASelectors.LogoutOption, 5);
                logoutBtn?.Click();

                Console.WriteLine("✓ Sesión cerrada");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error logout: {ex.Message}");
            }
        }

        /// <summary>
        /// Cerrar navegador WhatsApp
        /// </summary>
        public void CloseWDriver()
        {
            if (driverstate)
            {
                try
                {
                    driver?.Quit();
                    driver = null;
                    driverstate = false;
                    Console.WriteLine("✓ WhatsApp cerrado");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error cerrando WhatsApp: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Cerrar navegador SMS
        /// </summary>
        public void CloseWDriver2()
        {
            if (driverstate2)
            {
                try
                {
                    driver2?.Quit();
                    driver2 = null;
                    driverstate2 = false;
                    Console.WriteLine("✓ SMS cerrado");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error cerrando SMS: {ex.Message}");
                }
            }
        }

        #endregion

        #region Group Contacts Extraction

        /// <summary>
        /// Obtener contactos de un grupo (legacy)
        /// </summary>
        public StringBuilder GetContactsFromGroup(string groupName)
        {
            StringBuilder result = new StringBuilder();

            try
            {
                ClickSearchIcon();
                ContactSearch(groupName);
                new Actions(driver).SendKeys(SeleniumKeys.Space).Build().Perform();
                ContactClick();

                if (clickstate)
                {
                    Thread.Sleep(5000);

                    var headerElements = driver.FindElements(WASelectors.ConversationHeader);

                    result.AppendLine("First Name,Mobile Phone");
                    result.Append(",");

                    foreach (var element in headerElements)
                    {
                        result.Append(element.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error obteniendo contactos: {ex.Message}");
            }

            return result;
        }

        #endregion
    }
}