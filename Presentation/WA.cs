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
    public class WA
    {


        public static class WASelectors
        {

            public static By AttachMediaInput =>
    By.CssSelector("input[type='file'][accept*='image'], input[type='file'][accept*='video']");

            // XPath “solo imagen” (si lo quieres estrictamente)
            public static By AttachImageInput =>
                By.XPath("//input[@type='file' and @accept and contains(@accept,'image')]");

            // Fallback amplio (imagen o video, evita documentos)
            public static By AttachMediaInputFallback =>
                By.XPath("//input[@type='file' and @accept and (contains(@accept,'image') or contains(@accept,'video'))]");
            public static By AttachPhotosVideos =>
                By.XPath("//div[@role='application']//li[@role='button' and .//span[contains(normalize-space(.),'Fotos y videos') or contains(normalize-space(.),'Photos and videos')]]");

            public static By AttachPhotosVideosByIcon =>
                By.XPath("//div[@role='application']//li[@role='button' and .//title[normalize-space(.)='ic-filter-filled']]");





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
            public static By AttachButton =>
             By.XPath("//button[@type='button' and (@aria-label='Adjuntar' or @aria-label='Attach') and .//span[@data-icon='plus-rounded']]");


            // Solo imágenes (si realmente quieres SOLO image)
            //public static By AttachImageInput =>By.CssSelector("input[type='file'][accept*='image']");



            // Selector principal: funciona CON y SIN foto de perfil
            public static By FirstSearchResult =>
                By.XPath("//div[@role='gridcell'][@tabindex='0']");

            // Fallback 1: gridcell sin tabindex (en caso de cambios en WhatsApp)
            public static By FirstSearchResultWithAvatar =>
                By.CssSelector("div[role='gridcell']");

            // Fallback 2: búsqueda en grid completo
            public static By FirstSearchResultXPath =>
                By.XPath("//div[@role='grid']//div[@role='gridcell'][1]");

            // Mensaje de "no encontrado"
            public static By NoResultsMessage =>
    By.XPath("//div[@role='grid' and not(.//div[@role='gridcell'])]");


            public static By AttachDocumentInput => By.CssSelector("input[accept*='*'][type='file']");


            public static readonly By ImageCaptionInDialog =
     By.CssSelector(
        "div[contenteditable='true'][role='textbox'][data-lexical-editor='true'][aria-label='Escribe un mensaje']"
    );


            // Fallback: composer general (fuera de diálogo)
            public static readonly By ComposerInput =
    By.CssSelector(
        "div.lexical-rich-text-input div[contenteditable='true'][role='textbox'][data-lexical-editor='true'][aria-label='Escribe un mensaje'], " +
        "div.lexical-rich-text-input div[contenteditable='true'][role='textbox'][data-lexical-editor='true'][aria-placeholder='Escribe un mensaje']"
    );

            // Botón enviar (texto e imágenes)
            public static readonly By SendMediaButton =
                By.CssSelector(
                    "div[role='button'][aria-label='Enviar'], " +
                    "span[data-icon='wds-ic-send-filled']"
                );




            // Navigation
            // old public static By FirstSearchResult => By.CssSelector("div[role='row']");
            public static By ConversationHeader =>
    By.CssSelector("header");

            // Botón menú (3 puntos)
            public static By MenuButton =>
                By.CssSelector("div[role='button'][aria-label='Menú'], div[role='button'][aria-label='Menu']");

            // Opción cerrar sesión
            public static By LogoutOption =>
                By.XPath("//div[@role='button']//span[contains(text(),'Cerrar sesión') or contains(text(),'Log out')]");


        }
        public void ContactMessage(string htmlishMessage)
        {
            try
            {
                var box = WaitForComposer(20);
                if (box == null) throw new NoSuchElementException("Composer no encontrado.");

                var normalized = NormalizeHtmlBreaks(htmlishMessage);

                // 🔍 DEBUG: Ver qué texto se está procesando
                Console.WriteLine($"🔍 htmlishMessage original: [{htmlishMessage}]");
                Console.WriteLine($"🔍 normalized: [{normalized}]");
                Console.WriteLine($"🔍 normalized length: {normalized.Length}");

                TypeIntoComposer(box, normalized);

                // 🔍 DEBUG: Ver qué quedó en el composer
                var js = (IJavaScriptExecutor)driver;
                var composerContent = js.ExecuteScript("return arguments[0].textContent;", box);
                Console.WriteLine($"🔍 Contenido en composer después de escribir: [{composerContent}]");

                Console.WriteLine("✓ Mensaje escrito (sin enviar)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error escribiendo mensaje: {ex.Message}");
            }
        }
        public void ImageMessage(string filePath)
        {
            try
            {
                var abs = Path.GetFullPath(filePath);

                // 1) Click en el clip "+"
                var attachBtn = WaitForElement(WASelectors.AttachButton, 10)
                    ?? throw new NoSuchElementException("No encontré el botón Adjuntar (clip).");
                attachBtn.Click();
                Thread.Sleep(150);

                // 2) Click “Fotos y videos”
                var photos = WaitForElement(WASelectors.AttachPhotosVideos, 4)
                    ?? throw new NoSuchElementException("No encontré 'Fotos y videos' en el menú.");
                photos.Click();
                Thread.Sleep(150);

                // 3) Buscar input (solo 1 selector)
                var fileInput = GetLastFileInput(WASelectors.AttachMediaInput, 5);

                // Retry único: reabrir menú + reclick si no apareció el input
                if (fileInput == null)
                {
                    attachBtn = WaitForElement(WASelectors.AttachButton, 5);
                    attachBtn?.Click();
                    Thread.Sleep(150);

                    photos = WaitForElement(WASelectors.AttachPhotosVideos, 3);
                    photos?.Click();
                    Thread.Sleep(150);

                    fileInput = GetLastFileInput(WASelectors.AttachMediaInput, 5);
                }

                if (fileInput == null)
                    throw new NoSuchElementException("No apareció el input type=file de media.");

                // 4) SendKeys
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
                        "arguments[0].removeAttribute('hidden');",
                        fileInput
                    );
                    fileInput.SendKeys(abs);
                }

                Thread.Sleep(800 + preventblocktiming);
                Console.WriteLine($"✓ Imagen adjuntada: {Path.GetFileName(abs)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error adjuntando imagen: {ex.Message}");
            }
        }


        // Helper: devuelve el último <input type=file> que matchee (WhatsApp suele tener varios)
        private IWebElement GetLastFileInput(By selector, int timeoutSec)
        {
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSec));
                return wait.Until(drv =>
                {
                    var els = drv.FindElements(selector);
                    if (els == null || els.Count == 0) return null;

                    // WhatsApp suele inyectar uno nuevo: el último normalmente es el correcto
                    var last = els[els.Count - 1];
                    return (last.Displayed || last.Enabled) ? last : last; // igual sirve para SendKeys
                });
            }
            catch { return null; }
        }




        /// <summary>
        /// Adjuntar imagen con caption
        /// </summary>
        /// 


        public void ImageTextMessage(string filePath, string caption)
        {
            try
            {
                
                
                ImageMessage(filePath);
                
                Thread.Sleep(300);
              
                // 2. Buscar caja de caption
                var captionBox = WaitForElement(WASelectors.ImageCaptionInDialog, 8);

                if (captionBox == null)
                {
                    captionBox = WaitForElement(WASelectors.ComposerInput, 4);
                }

                if (captionBox == null)
                {
                    Console.WriteLine("✗ No se encontró caja de caption");
                    return;
                }

                // 3. Escribir caption si no está vacío
                if (!string.IsNullOrWhiteSpace(caption))
                {
                    TypeIntoContentEditable(captionBox, caption);
                    Thread.Sleep(500);
                }
           
                //Thread.Sleep(preventblocktiming);

                // 4. Enviar
                var sendBtn = WaitForElement(WASelectors.SendMediaButton, 6);

                if (sendBtn != null)
                {
                    sendBtn.Click();
                    Console.WriteLine("✓ Imagen con caption enviada");
                }
                else
                {
                    // Fallback: Enter
                    new Actions(driver)
                        .SendKeys(SeleniumKeys.Enter)
                        .Perform();
                    Console.WriteLine("✓ Enviado con Enter");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error en ImageTextMessage: {ex.Message}");
            }
        }


        public static string SendIADButton => "//div[@role='button' and @aria-label='Enviar']";
        // --- Campos de instancia ---
        public static IWebDriver driver;
        public static IWebDriver driver2;

        public bool driverstate;
        public bool driverstate2;
        public bool clickstate = false;

        // === Base & RNG ======================
        private volatile int _preventBlockBaseMs = 0;

        // 🔹 FIX: Instancia única de Random con lock para thread-safety
        private static readonly Random _rng = new Random();
        private static readonly object _rngLock = new object();

        // Método helper para obtener valores random de forma thread-safe
        private static int GetRandomNext(int minValue, int maxValue)
        {
            lock (_rngLock)
            {
                return _rng.Next(minValue, maxValue);
            }
        }

        private static double GetRandomDouble()
        {
            lock (_rngLock)
            {
                return _rng.NextDouble();
            }
        }

        // --- Perfiles humanos -------------------------------------------------------
        private enum HumanProfile
        {
            Fast,
            Normal,
            Slow
        }
        public void ContactActionEnter()
        {
            try
            {
                var box = WaitForComposer(5);
                if (box == null) throw new NoSuchElementException("Composer no encontrado para enviar.");
                //FocusComposer(box);

                box.SendKeys(OpenQA.Selenium.Keys.Enter);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error enviando Enter: {ex.Message}");
            }
        }
        private HumanProfile _currentProfile = HumanProfile.Normal;

        // cambio de perfil cada cierto número de mensajes
        private int _msgsSinceProfileChange = 0;
        private int _nextProfileChangeAt = 0;

        // config para cambio aleatorio de perfil
        public bool UseProfileCycling { get; set; } = true;
        public int ProfileChangeMinMessages { get; set; } = 40;
        public int ProfileChangeMaxMessages { get; set; } = 90;

        // 🔹 NUEVO: prende/apaga todo el comportamiento humano
        public bool UseHumanTiming { get; set; } = true;

        // 🔥 NUEVO: Sistema de Page Refresh para prevenir Chrome crash
        private int _totalMessagesProcessed = 0;
        public bool UsePageRefresh { get; set; } = true;
        public int RefreshEveryNMessages { get; set; } = 80;  // Basado en tu experiencia: crash en ~180
        public Action OnPageRefreshNeeded { get; set; }

        // 🔹 FIX: Aplica valores según perfil SIN llamar a ResetDistractionSchedule
        private void ApplyProfile(HumanProfile profile)
        {
            _currentProfile = profile;

            switch (profile)
            {
                case HumanProfile.Fast:
                    PreventBlockBaseMs = 2000;
                    UseDistractions = true;
                    DistractionEveryFixed = false;
                    DistractionEveryMin = 4;
                    DistractionEveryMax = 8;
                    DistractionFactorMin = 2;
                    DistractionFactorMax = 2;
                    break;

                case HumanProfile.Normal:
                    PreventBlockBaseMs = 3200;
                    UseDistractions = true;
                    DistractionEveryFixed = false;
                    DistractionEveryMin = 3;
                    DistractionEveryMax = 6;
                    DistractionFactorMin = 2;
                    DistractionFactorMax = 3;
                    break;

                case HumanProfile.Slow:
                    PreventBlockBaseMs = 5000;
                    UseDistractions = true;
                    DistractionEveryFixed = false;
                    DistractionEveryMin = 2;
                    DistractionEveryMax = 4;
                    DistractionFactorMin = 2;
                    DistractionFactorMax = 3;
                    break;
            }

            // 🔹 FIX: NO llamar a ResetDistractionSchedule aquí
        }

        // programa cuándo tocará cambiar de perfil de nuevo
        private void ScheduleNextProfileChange()
        {
            int min = Math.Max(10, ProfileChangeMinMessages);
            int max = Math.Max(min, ProfileChangeMaxMessages);
            _nextProfileChangeAt = GetRandomNext(min, max + 1);
            _msgsSinceProfileChange = 0;
        }

        // Jitter natural 80–120% con pico al centro (triangular)
        private static int JitterHumanPct(int baseMs, int lowPct = 80, int highPct = 120)
        {
            if (baseMs <= 0) return 0;
            if (highPct < lowPct) { var t = lowPct; lowPct = highPct; highPct = t; }

            var u = (GetRandomDouble() + GetRandomDouble()) / 2.0;

            var min = baseMs * lowPct / 100;
            var max = baseMs * highPct / 100;
            return min + (int)Math.Round(u * (max - min));
        }

        public int PreventBlockBaseMs
        {
            get { return _preventBlockBaseMs; }
            set { _preventBlockBaseMs = Math.Max(0, value); }
        }

        public int preventblocktiming
        {
            get { return JitterHumanPct(_preventBlockBaseMs, 80, 120); }
            set { _preventBlockBaseMs = Math.Max(0, value); }
        }

        private int MicroJitter()
        {
            return GetRandomNext(80, 251);
        }

        // === Distracciones cada K mensajes ==========================================
        private int _processedSinceDistr = 0;
        private int _nextThresholdK = 2;

        public bool UseDistractions { get; set; } = true;
        public bool DistractionEveryFixed { get; set; } = false;
        public int DistractionEveryN { get; set; } = 3;
        public int DistractionEveryMin { get; set; } = 2;
        public int DistractionEveryMax { get; set; } = 3;

        public int DistractionFactorMin { get; set; } = 2;
        public int DistractionFactorMax { get; set; } = 3;

        // 🔹 FIX: Recalcula el umbral de distracción e inicializa perfil si es necesario
        public void ResetDistractionSchedule()
        {
            if (DistractionEveryFixed)
                _nextThresholdK = Math.Max(1, DistractionEveryN);
            else
                _nextThresholdK = Math.Max(1, GetRandomNext(DistractionEveryMin, DistractionEveryMax + 1));

            _processedSinceDistr = 0;

            // 🔹 FIX: Solo inicializa el perfil si nunca se ha hecho
            if (UseHumanTiming && _nextProfileChangeAt <= 0)
            {
                ApplyProfile(_currentProfile); // Aplica el perfil
                ScheduleNextProfileChange();    // Agenda el próximo cambio

                // 🔹 IMPORTANTE: Recalcula el umbral después de aplicar el perfil
                if (DistractionEveryFixed)
                    _nextThresholdK = Math.Max(1, DistractionEveryN);
                else
                    _nextThresholdK = Math.Max(1, GetRandomNext(DistractionEveryMin, DistractionEveryMax + 1));
            }
        }

        // 🔥 NUEVO: Método para resetear el contador de mensajes (útil al iniciar sesión nueva)
        public void ResetMessageCounter()
        {
            _totalMessagesProcessed = 0;
        }

        public int NextPreventDelayMs()
        {
            if (!UseHumanTiming)
                return 0;

            // --- manejo de cambio de perfil aleatorio -------------------------------
            if (UseProfileCycling)
            {
                _msgsSinceProfileChange++;

                if (_nextProfileChangeAt <= 0)
                {
                    ScheduleNextProfileChange();
                }
                else if (_msgsSinceProfileChange >= _nextProfileChangeAt)
                {
                    HumanProfile newProfile = _currentProfile;
                    while (newProfile == _currentProfile)
                    {
                        int v = GetRandomNext(0, 3);
                        newProfile = (HumanProfile)v;
                    }

                    ApplyProfile(newProfile);
                    ScheduleNextProfileChange();

                    // 🔹 Recalcula el umbral después de cambiar de perfil
                    if (DistractionEveryFixed)
                        _nextThresholdK = Math.Max(1, DistractionEveryN);
                    else
                        _nextThresholdK = Math.Max(1, GetRandomNext(DistractionEveryMin, DistractionEveryMax + 1));
                }
            }

            _processedSinceDistr++;

            // 🔥 NUEVO: Incrementar contador total de mensajes procesados
            _totalMessagesProcessed++;

            // 🔥 NUEVO: Verificar si necesita refresh para prevenir Chrome crash
            if (UsePageRefresh &&
                _totalMessagesProcessed % RefreshEveryNMessages == 0 &&
                _totalMessagesProcessed > 0)
            {
                TriggerPageRefresh();
            }

            // Cachea el valor base UNA SOLA VEZ
            int baseDelay = preventblocktiming;

            // ¿toca distracción?
            if (UseDistractions && _processedSinceDistr >= _nextThresholdK)
            {
                if (DistractionEveryFixed)
                    _nextThresholdK = Math.Max(1, DistractionEveryN);
                else
                    _nextThresholdK = Math.Max(1, GetRandomNext(DistractionEveryMin, DistractionEveryMax + 1));

                _processedSinceDistr = 0;

                var factor = GetRandomNext(DistractionFactorMin, DistractionFactorMax + 1);
                return baseDelay * factor + MicroJitter();
            }

            return baseDelay + MicroJitter();
        }

        // 🔥 NUEVO: Método que dispara el callback de refresh
        private void TriggerPageRefresh()
        {
            Console.WriteLine($"\n{'=',70}");
            Console.WriteLine($"🔄 PAGE REFRESH TRIGGERED");
            Console.WriteLine($"   Reason: Prevent Chrome crash (after {_totalMessagesProcessed} messages)");
            Console.WriteLine($"   Current Profile: {_currentProfile}");
            Console.WriteLine($"{'=',70}\n");

            // Ejecuta el callback si está configurado
            OnPageRefreshNeeded?.Invoke();
        }



        public int preventblocktiming2 = 0;

        private WebDriverWait _wait;
        private WebDriverWait _wait2;


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
            Console.WriteLine("=== DIAGNÓSTICO ===");
            Console.WriteLine($"Driver: {(driver == null ? "null" : "existe")}");
            Console.WriteLine($"State: {driverstate}");

            var driverPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "tempfilesWAButt", "webdriver", "chromedriver.exe"
            );
            Console.WriteLine($"ChromeDriver existe: {File.Exists(driverPath)}");

            if (File.Exists(driverPath))
            {
                var size = new FileInfo(driverPath).Length;
                Console.WriteLine($"ChromeDriver tamaño: {size} bytes");
            }

            Console.WriteLine("===================");

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

            ChromeDriverService service = null;

            try
            {
                ct.ThrowIfCancellationRequested();

                Console.WriteLine("Abriendo Chrome para WhatsApp...");

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
                    "--lang=es-PE"
                );

                options.AddArgument("--disable-blink-features=AutomationControlled");
                options.AddExcludedArgument("enable-automation");

                options.AddArguments(
                    "--disable-gpu",
                    "--no-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-extensions",
                    "--disable-plugins",
                    "--disk-cache-size=1",
                    "--media-cache-size=1",
                    "--aggressive-cache-discard",
                    "--disable-application-cache"
                );

                options.PageLoadStrategy = PageLoadStrategy.Normal;
                options.AddUserProfilePreference("credentials_enable_service", false);
                options.AddUserProfilePreference("profile.password_manager_enabled", false);

                // ✅ Crear driver SIN Task.Run (ejecutar en el thread actual)
                driver = new ChromeDriver(service, options);

                driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
                driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(30);

                _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

                driver.Navigate().GoToUrl("https://web.whatsapp.com/");

                driverstate = true;
                Console.WriteLine("✓ WhatsApp Web iniciado correctamente");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("⚠️ Inicio cancelado");
                driverstate = false;

                try { driver?.Quit(); } catch { }
                try { driver?.Dispose(); } catch { }
                driver = null;

                service?.Dispose();
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error iniciando WhatsApp: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                driverstate = false;

                try { driver?.Quit(); } catch { }
                try { driver?.Dispose(); } catch { }
                driver = null;

                service?.Dispose();
                throw;
            }
        }

        #endregion
        private IWebElement WaitForComposer(int timeoutSec = 15)
        {
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSec));
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
            if (el == null || string.IsNullOrEmpty(text))
            {
                Console.WriteLine("⚠ Elemento o texto vacío");
                return;
            }

            try
            {
                text = text.Replace("\r\n", "\n").Replace("\r", "\n");

                Console.WriteLine($"📝 Insertando texto con DataTransfer API");

                var js = (IJavaScriptExecutor)driver;

                js.ExecuteScript(@"
            var el = arguments[0];
            var text = arguments[1];
            
            // Focus y limpiar
            el.focus();
            el.innerHTML = '';
            
            // Usar DataTransfer para simular paste correctamente
            var dataTransfer = new DataTransfer();
            dataTransfer.setData('text/plain', text);
            
            var pasteEvent = new ClipboardEvent('paste', {
                bubbles: true,
                cancelable: true,
                clipboardData: dataTransfer
            });
            
            // Dejar que WhatsApp maneje el paste
            if (!el.dispatchEvent(pasteEvent)) {
                // Si fue cancelado, insertar manualmente
                var lines = text.split('\n');
                var fragment = document.createDocumentFragment();
                
                lines.forEach(function(line, index) {
                    if (index > 0) {
                        fragment.appendChild(document.createElement('br'));
                    }
                    fragment.appendChild(document.createTextNode(line));
                });
                
                el.appendChild(fragment);
            }
            
            // Mover cursor al final
            var range = document.createRange();
            range.selectNodeContents(el);
            range.collapse(false);
            
            var selection = window.getSelection();
            selection.removeAllRanges();
            selection.addRange(range);
            
            // Eventos finales
            el.dispatchEvent(new InputEvent('input', { 
                bubbles: true,
                inputType: 'insertFromPaste'
            }));
            
        ", el, text);

                Thread.Sleep(600);

                Console.WriteLine("✓ Texto insertado con saltos de línea");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
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
                // Esperar a que los resultados de búsqueda carguen
                Thread.Sleep(1500);

                // Verificar si NO hay resultados
                if (CheckNoResults())
                {
                    clickstate = false;
                    Console.WriteLine("✗ Sin resultados");
                    ClickSearchIcon();
                    return;
                }

                // Buscar el primer resultado clickeable
                IWebElement result = FindClickableElement();

                if (result != null)
                {
                    // Intentar hacer click con estrategia de fallback
                    ClickElement(result);
                }
                else
                {
                    clickstate = false;
                    Console.WriteLine("✗ No se encontró contacto");
                    ClickSearchIcon();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error en ContactClick: {ex.Message}");
                clickstate = false;
            }
        }

        // ============= MÉTODOS HELPER =============

        private bool CheckNoResults()
        {
            try
            {
                var elements = driver.FindElements(WASelectors.NoResultsMessage);
                return elements.Count > 0 && elements[0].Displayed;
            }
            catch
            {
                return false;
            }
        }

        private IWebElement FindClickableElement()
        {
            // Intentar múltiples selectores (ahora soportan contactos CON y SIN foto de perfil)
            var selectors = new[]
            {
        new { Name = "GridCell[tabindex=0]", By = WASelectors.FirstSearchResult, Timeout = 4 },
        new { Name = "GridCell genérico", By = WASelectors.FirstSearchResultWithAvatar, Timeout = 3 },
        new { Name = "Primer GridCell del Grid", By = WASelectors.FirstSearchResultXPath, Timeout = 2 }
    };

            foreach (var sel in selectors)
            {
                try
                {
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(sel.Timeout));
                    var element = wait.Until(d => {
                        try
                        {
                            var el = d.FindElement(sel.By);
                            // Verificar que sea visible Y tenga tamaño real
                            if (el.Displayed && el.Size.Width > 0 && el.Size.Height > 0)
                                return el;
                            return null;
                        }
                        catch
                        {
                            return null;
                        }
                    });

                    if (element != null)
                    {
                        Console.WriteLine($"✓ Elemento encontrado: {sel.Name}");
                        return element;
                    }
                }
                catch
                {
                    Console.WriteLine($"⚠ No encontrado: {sel.Name}");
                    continue;
                }
            }

            return null;
        }

        private void ClickElement(IWebElement element)
        {
            // Estrategia 1: Enviar ENTER (más natural)
            try
            {
                var box = WaitForComposer(5);
                if (box == null)
                    throw new NoSuchElementException("Composer no encontrado");
                new Actions(driver).SendKeys(SeleniumKeys.Enter).Build().Perform();
                //FocusComposer(box);
                clickstate = true;
                Thread.Sleep(1000);
                Console.WriteLine("✓ Contacto seleccionado (Enter)");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Enter falló: {ex.Message}");
            }

            // Estrategia 2: Click directo en el elemento
            try
            {
                element.Click();
                clickstate = true;
                Thread.Sleep(1000);
                Console.WriteLine("✓ Contacto seleccionado (Click)");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Click falló: {ex.Message}");
            }

            // Estrategia 3: JavaScript click (última opción)
            try
            {
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", element);
                clickstate = true;
                Thread.Sleep(1000);
                Console.WriteLine("✓ Contacto seleccionado (JS)");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Todas las estrategias fallaron: {ex.Message}");
                clickstate = false;
            }
        }

        #endregion

        #region WhatsApp - Messaging

        /// <summary>
        /// Escribir mensaje en el chat
        /// </summary>
        

        private void TypeIntoComposer(IWebElement el, string text)
        {
            if (el == null || string.IsNullOrEmpty(text))
            {
                Console.WriteLine("⚠ TypeIntoComposer: elemento null o texto vacío");
                return;
            }

            try
            {
                Console.WriteLine($"📝 TypeIntoComposer recibió {text.Length} caracteres");

                var js = (IJavaScriptExecutor)driver;

                // ✅ Método más robusto - usando execCommand
                var result = js.ExecuteScript(@"
            var el = arguments[0];
            var text = arguments[1];
            
            try {
                console.log('[WhatsApp] Insertando texto:', text.substring(0, 50));
                
                // Focus
                el.focus();
                
                // Limpiar usando execCommand
                document.execCommand('selectAll', false, null);
                document.execCommand('delete', false, null);
                
                // Insertar texto línea por línea
                var lines = text.split('\n');
                console.log('[WhatsApp] Total líneas:', lines.length);
                
                for (var i = 0; i < lines.length; i++) {
                    if (lines[i].length > 0) {
                        // Usar execCommand para insertar texto (más compatible)
                        document.execCommand('insertText', false, lines[i]);
                    }
                    
                    // Insertar salto de línea (excepto última línea)
                    if (i < lines.length - 1) {
                        // Shift+Enter programático
                        var enterEvent = new KeyboardEvent('keydown', {
                            bubbles: true,
                            cancelable: true,
                            key: 'Enter',
                            code: 'Enter',
                            keyCode: 13,
                            shiftKey: true
                        });
                        el.dispatchEvent(enterEvent);
                        
                        // Insertar <br> manualmente si el evento no funcionó
                        var br = document.createElement('br');
                        el.appendChild(br);
                    }
                }
                
                console.log('[WhatsApp] Contenido final:', el.textContent);
                console.log('[WhatsApp] HTML final:', el.innerHTML);
                
                // Eventos finales
                el.dispatchEvent(new Event('input', { bubbles: true }));
                el.dispatchEvent(new Event('change', { bubbles: true }));
                
                return el.textContent;
                
            } catch (e) {
                console.error('[WhatsApp] Error:', e);
                return 'ERROR: ' + e.message;
            }
        ", el, text);

                Console.WriteLine($"🔍 JS retornó: [{result}]");

                Thread.Sleep(500);

                // Verificar contenido
                var finalContent = (string)js.ExecuteScript("return arguments[0].textContent;", el);
                Console.WriteLine($"🔍 Contenido final verificado: [{finalContent.Substring(0, Math.Min(50, finalContent.Length))}...]");

                if (finalContent.Length < 5)
                {
                    Console.WriteLine("⚠ WARNING: El contenido parece estar vacío!");
                }
                else
                {
                    Console.WriteLine("✓ Texto escrito correctamente");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error en TypeIntoComposer: {ex.Message}");
            }
        }

        private static string NormalizeHtmlBreaks(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;

            s = s.Replace("\r\n", "\n").Replace("\r", "\n");
            s = Regex.Replace(s, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"</div\s*>", "\n", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"</p\s*>", "\n", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"<div[^>]*>|<p[^>]*>", string.Empty, RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"<[^>]+>", string.Empty);

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
        /// 

        
        #endregion

        #region WhatsApp - Attachments

        /// <summary>
        /// Adjuntar imagen/video
        /// </summary>
        
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
                if (attachBtn == null)
                    throw new NoSuchElementException("No encontré el botón Adjuntar (clip).");

                attachBtn.Click();
                Thread.Sleep(500);

                // Usar GetLastFileInput para mayor robustez
                var fileInput = GetLastFileInput(WASelectors.AttachDocumentInput, 10);

                if (fileInput == null)
                    throw new NoSuchElementException("No apareció el input de documento.");

                fileInput.SendKeys(filePath);

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
                if (attachBtn == null)
                    throw new NoSuchElementException("No encontré el botón Adjuntar (clip).");

                attachBtn.Click();
                Thread.Sleep(500);

                // Los audios se suben como documentos en WhatsApp Web
                var fileInput = GetLastFileInput(WASelectors.AttachDocumentInput, 10);

                if (fileInput == null)
                    throw new NoSuchElementException("No apareció el input de documento/audio.");

                fileInput.SendKeys(audioPath);

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
                if (driver == null) return true;
                var title = driver.Title;
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
            try
            {
                if (driver != null)
                {
                    try
                    {
                        // ✅ Verificar si el proceso está vivo antes de cerrar
                        var title = driver.Title; // Test si responde
                        driver.Quit();
                        Console.WriteLine("✓ WhatsApp cerrado correctamente");
                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine("⚠️ Driver ya estaba cerrado");
                    }
                    catch (WebDriverException)
                    {
                        Console.WriteLine("⚠️ Proceso de Chrome ya no existe");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error cerrando: {ex.Message}");
                    }
                    finally
                    {
                        try { driver?.Dispose(); } catch { }
                        driver = null;
                    }
                }

                driverstate = false;
                Console.WriteLine("✓ Estado reseteado");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en CloseWDriver: {ex.Message}");
                driver = null;
                driverstate = false;
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