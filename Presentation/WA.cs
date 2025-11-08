using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
            // 1) CSS moderno (Chrome 105+): padre <button> que CONTIENE el span del icono
            public static By SearchButton => By.CssSelector("button:has(span[data-icon*='search'])");

            // 2) Fallback (por si falla :has): encuentra el <span> y sube al <button>
            public static By SearchButtonFallback => By.CssSelector("span[data-icon*='search']");

            // Reemplaza tu SearchInput por este combo (3 opciones unidas con coma):
            public static By SearchInput => By.CssSelector(
                // 1) Por aria-label (inglés)
                "div.lexical-rich-text-input div[contenteditable='true'][role='textbox'][data-lexical-editor='true'][aria-label*='Search']," +
                // 2) Por aria-placeholder (inglés)
                "div.lexical-rich-text-input div[contenteditable='true'][role='textbox'][data-lexical-editor='true'][aria-placeholder*='Search']," +
                // 3) Fallback por tabindex que suele usar el buscador
                "div.lexical-rich-text-input div[contenteditable='true'][role='textbox'][data-lexical-editor='true'][tabindex='3']"
            );

            // Messaging
            public static By MessageInput => By.CssSelector("div.lexical-rich-text-input div[contenteditable='true'][role='textbox'][data-lexical-editor='true']");
            public static By SendButton => By.CssSelector("button[aria-label*='Send']");

            // Attachments
            public static By AttachButton => By.CssSelector("div[role='button'][aria-label='Adjuntar'], div[role='button'][aria-label='Attach']");

            public static readonly By AttachImageInput =
            By.CssSelector("li[role='button'] input[type='file'][multiple][accept*='image']");

            public static readonly By AttachImageInputFallback =
                By.CssSelector("input[type='file'][accept*='image']");





            public static By AttachDocumentInput => By.CssSelector("input[accept*='*'][type='file']");



            public static readonly By ImageCaptionInDialog =
                   By.CssSelector("div[role='dialog'] div[contenteditable='true'][role='textbox'][data-lexical-editor='true']");

            // Fallback: composer general (fuera de diálogo)
            public static readonly By ComposerInput =
                By.CssSelector("div[contenteditable='true'][role='textbox'][data-lexical-editor='true']");

            // Botón Enviar dentro del diálogo de media
            public static readonly By SendMediaButton =
                By.CssSelector("div[role='dialog'] [aria-label*='Send'], div[role='dialog'] span[data-icon='send']");
        













        public static By SendAttachmentButton => By.CssSelector("span[data-icon='send']");

            // Navigation
            public static By FirstSearchResult => By.CssSelector("div[role='row']");
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
        private IWebElement WaitForComposer(int timeoutSec = 15)
        {
            try
            {
                var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSec));
                return wait.Until(drv =>
                {
                    var els = drv.FindElements(WASelectors.MessageInput);
                    if (els == null || els.Count == 0) return null;

                    var js = (IJavaScriptExecutor)drv;
                    // Elige el último visible que NO esté dentro de un modal (role=dialog)
                    for (int i = els.Count - 1; i >= 0; i--)
                    {
                        var el = els[i];
                        if (!el.Displayed || !el.Enabled) continue;
                        var insideDialog = (bool)js.ExecuteScript(
                            "return !!(arguments[0].closest && arguments[0].closest('[role=\"dialog\"]'));", el);
                        if (!insideDialog) return el;
                    }
                    return null;
                });
            }
            catch { return null; }
        }



        private void TypeIntoContentEditable(IWebElement el, string text)
        {
            try
            {
                // ✅ Normalizar ANTES de enviar
                var normalized = NormalizeHtmlBreaks(text);

                el.Click();
                el.SendKeys(OpenQA.Selenium.Keys.Control + "a");
                el.SendKeys(OpenQA.Selenium.Keys.Delete);

                // ✅ Enviar línea por línea con Shift+Enter para saltos
                var lines = normalized.Split('\n');
                var actions = new Actions(driver);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (!string.IsNullOrEmpty(lines[i]))
                    {
                        el.SendKeys(lines[i]);
                    }

                    if (i < lines.Length - 1)
                    {
                        // Shift+Enter = salto sin enviar
                        actions.KeyDown(SeleniumKeys.Shift)
                               .SendKeys(SeleniumKeys.Enter)
                               .KeyUp(SeleniumKeys.Shift)
                               .Perform();
                    }
                }

                return;
            }
            catch
            {
                // ✅ Fallback JS con normalización
                var normalized = NormalizeHtmlBreaks(text);

                var js = (IJavaScriptExecutor)driver;
                js.ExecuteScript(@"
            var el = arguments[0], txt = arguments[1];
            el.focus();
            try {
                document.execCommand('selectAll', false, null);
                document.execCommand('insertText', false, txt);
            } catch (e) {
                var sel = window.getSelection();
                sel.removeAllRanges();
                var range = document.createRange();
                range.selectNodeContents(el);
                range.collapse(false);
                sel.addRange(range);
                el.dispatchEvent(new InputEvent('beforeinput', {inputType:'insertText', data:txt, bubbles:true}));
                el.textContent = txt;
                el.dispatchEvent(new InputEvent('input', {inputType:'insertText', data:txt, bubbles:true}));
            }
        ", el, normalized);
            }
        }
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
                IWebElement btn = WaitForElement(WASelectors.SearchButton, 6);
                if (btn == null)
                {
                    // Fallback: tomar el span y subir al <button> ancestro
                    var icon = WaitForElement(WASelectors.SearchButtonFallback, 6);
                    if (icon != null)
                    {
                        btn = icon.FindElement(By.XPath("./ancestor::button[1]"));
                    }
                }

                if (btn == null) throw new NoSuchElementException("Search button not found");

                ((IJavaScriptExecutor)driver)
                    .ExecuteScript("arguments[0].scrollIntoView({block:'center',inline:'center'})", btn);

                btn.Click();
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
                var input = WaitForElement(WASelectors.SearchInput, 20);
                if (input == null) throw new NoSuchElementException("No se encontró el buscador.");

                // Intenta vía SendKeys; si falla, usa JS para contenteditable (Lexical)
                try
                {
                    input.Click();
                    input.SendKeys(OpenQA.Selenium.Keys.Control + "a");
                    input.SendKeys(OpenQA.Selenium.Keys.Delete);
                    if (!string.IsNullOrEmpty(tosearch))
                        input.SendKeys(tosearch);
                }
                catch
                {
                    var js = (IJavaScriptExecutor)driver;
                    js.ExecuteScript(@"
                var el = arguments[0], txt = arguments[1];
                el.focus();
                try {
                    document.execCommand('selectAll', false, null);
                    document.execCommand('insertText', false, txt);
                } catch (e) {
                    var sel = window.getSelection();
                    sel.removeAllRanges();
                    var range = document.createRange();
                    range.selectNodeContents(el);
                    range.collapse(false);
                    sel.addRange(range);
                    el.dispatchEvent(new InputEvent('beforeinput', {inputType:'insertText', data:txt, bubbles:true}));
                    el.textContent = txt;
                    el.dispatchEvent(new InputEvent('input', {inputType:'insertText', data:txt, bubbles:true}));
                    el.dispatchEvent(new Event('change', {bubbles:true}));
                }
            ", input, tosearch ?? string.Empty);
                }

                // Pequeño wait para que refresquen los resultados
                Thread.Sleep(400);
                Console.WriteLine($"✓ Buscando: {tosearch}");
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
        public void ContactMessage(string htmlishMessage)
        {
            try
            {
                var box = WaitForComposer(20);
                if (box == null) throw new NoSuchElementException("Composer no encontrado.");

                var normalized = NormalizeHtmlBreaks(htmlishMessage); // <<< aquí interceptas los <br>
                TypeIntoComposer(box, normalized);

                Console.WriteLine("✓ Mensaje escrito (sin enviar)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error escribiendo mensaje: {ex.Message}");
            }
        }
        private void TypeIntoComposer(IWebElement el, string text)
        {
            if (el == null) return;

            // focus + limpiar
            new Actions(driver)
                .MoveToElement(el).Click()
                .KeyDown(SeleniumKeys.Control).SendKeys("a").KeyUp(SeleniumKeys.Control)
                .SendKeys(SeleniumKeys.Delete)
                .Perform();

            // teclear líneas
            var lines = (text ?? string.Empty).Split('\n');
            var act = new Actions(driver);
            for (int i = 0; i < lines.Length; i++)
            {
                var part = lines[i];
                if (!string.IsNullOrEmpty(part)) act.SendKeys(part);
                if (i < lines.Length - 1)
                    act.KeyDown(SeleniumKeys.Shift).SendKeys(SeleniumKeys.Enter).KeyUp(SeleniumKeys.Shift); // salto sin enviar
            }
            act.Perform();

            // pequeño “nudge” para que Lexical actualice
            new Actions(driver).SendKeys(" ").SendKeys(SeleniumKeys.Backspace).Perform();
        }
        private static string NormalizeHtmlBreaks(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;

            // normaliza finales de línea
            s = s.Replace("\r\n", "\n").Replace("\r", "\n");

            // <br>, <br/>, <br /> → \n
            s = Regex.Replace(s, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);

            // cierra <div> o <p> = salto de línea
            s = Regex.Replace(s, @"</div\s*>", "\n", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"</p\s*>", "\n", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"<div[^>]*>|<p[^>]*>", string.Empty, RegexOptions.IgnoreCase);

            // quita tags HTML
            s = Regex.Replace(s, @"<[^>]+>", string.Empty);

            // ✅ Decode solo entidades comunes (NO Unicode/emojis)
            s = s.Replace("&nbsp;", " ")
                 .Replace("&amp;", "&")
                 .Replace("&lt;", "<")
                 .Replace("&gt;", ">")
                 .Replace("&quot;", "\"")
                 .Replace("&#39;", "'");

            return s;
        }


        /// <summary>
        /// Enviar mensaje (presionar Enter)
        /// </summary>
        public void ContactActionEnter()
        {
            try
            {
                var box = WaitForComposer(5);
                if (box == null) throw new NoSuchElementException("Composer no encontrado para enviar.");
                box.SendKeys(OpenQA.Selenium.Keys.Enter);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error enviando Enter: {ex.Message}");
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
                var abs = Path.GetFullPath(filePath);

                // 1) Abre menú del clip
                var attachBtn = WaitForElement(WASelectors.AttachButton, 10);
                if (attachBtn == null) throw new NoSuchElementException("No encontré el botón Adjuntar (clip).");
                attachBtn.Click();
                Thread.Sleep(300);

                // 2) Localiza el input con varios intentos
                IWebElement fileInput = WaitForElement(WASelectors.AttachImageInput, 5);
                if (fileInput == null)
                    fileInput = WaitForElement(WASelectors.AttachImageInputFallback, 5);
                if (fileInput == null)
                {
                    // Último recurso: XPath amplio
                    try { fileInput = driver.FindElement(By.XPath("//input[@type='file' and contains(@accept,'image')]")); }
                    catch { /* ignore */ }
                }
                if (fileInput == null) throw new NoSuchElementException("No encontré el input de archivo para Fotos/Videos.");

                // 3) Envío del archivo (si está oculto, forzamos visible y reintentamos)
                try
                {
                    fileInput.SendKeys(abs);
                }
                catch (ElementNotInteractableException)
                {
                    var js = (IJavaScriptExecutor)driver;
                    js.ExecuteScript(
                        "arguments[0].style.display='block';" +
                        "arguments[0].style.visibility='visible';" +
                        "arguments[0].removeAttribute('hidden');", fileInput);
                    fileInput.SendKeys(abs);
                }

                Thread.Sleep(1000 + preventblocktiming);
                Console.WriteLine($"✓ Imagen adjuntada: {Path.GetFileName(abs)}");
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
                // 1) Adjunta la imagen (asegúrate de que ImageMessage SOLO adjunta y NO envía)
                ImageMessage(filePath);
                Thread.Sleep(600);

                // 2) Espera el cuadro de caption dentro del diálogo de media
                var captionBox = WaitForElement(WASelectors.ImageCaptionInDialog, 8);
                if (captionBox == null)
                {
                    // Fallback (por si WhatsApp abre el composer general en algunos flujos)
                    captionBox = WaitForElement(WASelectors.ComposerInput, 4);
                }

                if (captionBox == null)
                    throw new NoSuchElementException("No encontré la caja de caption (contenteditable).");

                // 3) Escribe el caption de forma robusta
                TypeIntoContentEditable(captionBox, caption ?? string.Empty);
                Console.WriteLine("✓ Caption agregado");

                Thread.Sleep(800 + preventblocktiming);

                // 4) Enviar (botón del modal de media)
                var sendBtn = WaitForElement(WASelectors.SendMediaButton, 6);
                if (sendBtn != null)
                {
                    sendBtn.Click();
                    Console.WriteLine("✓ Media enviada");
                }
                else
                {
                    // Último recurso, Enter
                    captionBox.SendKeys(OpenQA.Selenium.Keys.Enter);
                    Console.WriteLine("↩️ Enviado con Enter (fallback).");
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