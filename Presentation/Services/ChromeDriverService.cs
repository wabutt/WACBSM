using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json.Linq;
using Presentation.Helpers;
using Microsoft.Win32;
using System.Collections.Generic;

namespace Presentation.Services
{
    /// <summary>
    /// Service for ChromeDriver management
    /// </summary>
    public class ChromeDriverService
    {
        private const string CHROME_FOR_TESTING_API = "https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions-with-downloads.json";
        private const string LEGACY_API_BASE = "https://chromedriver.storage.googleapis.com/LATEST_RELEASE_";
        private readonly HttpClient _httpClient;

        public string ChromeDriverVersion { get; private set; }
        public string DownloadUrl { get; private set; }
        public int InstalledChromeMajorVersion { get; private set; }

        public ChromeDriverService()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Ensure ChromeDriver is installed and up to date
        /// </summary>
        public async Task<bool> EnsureChromeDriverAsync()
        {
            try
            {
                // Detectar versión de Chrome instalada
                InstalledChromeMajorVersion = GetInstalledChromeMajorVersion();
                Console.WriteLine($"Chrome instalado: versión major {InstalledChromeMajorVersion}");

                // Verificar si el driver actual es compatible
                string driverPath = Path.Combine(Environment.CurrentDirectory, "chromedriver.exe");
                if (IsCurrentDriverCompatible(driverPath))
                {
                    Console.WriteLine("ChromeDriver actual es compatible, no requiere actualización");
                    return true;
                }

                // Buscar versión de ChromeDriver compatible
                await FetchLatestVersionAsync();
                await DownloadAndInstallAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ChromeDriver setup error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get installed Chrome major version
        /// </summary>
        private int GetInstalledChromeMajorVersion()
        {
            // 1) Intentar desde registro de Windows (BLBeacon)
            try
            {
                var version = Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Google\Chrome\BLBeacon",
                    "version",
                    null
                ) as string;

                if (!string.IsNullOrWhiteSpace(version))
                {
                    int major;
                    if (int.TryParse(version.Split('.')[0], out major))
                    {
                        return major;
                    }
                }
            }
            catch { }

            // 2) Intentar desde FileVersion de chrome.exe
            var chromePaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe")
            };

            foreach (var chromePath in chromePaths)
            {
                if (File.Exists(chromePath))
                {
                    try
                    {
                        var fileVersion = FileVersionInfo.GetVersionInfo(chromePath).FileVersion;
                        if (!string.IsNullOrWhiteSpace(fileVersion))
                        {
                            int major;
                            if (int.TryParse(fileVersion.Split('.')[0], out major))
                            {
                                return major;
                            }
                        }
                    }
                    catch { }
                }
            }

            // Fallback: retornar versión estable reciente
            Console.WriteLine("No se pudo detectar Chrome, usando versión estable actual");
            return 145; // Actualizar periódicamente
        }

        /// <summary>
        /// Check if current ChromeDriver is compatible with installed Chrome
        /// </summary>
        private bool IsCurrentDriverCompatible(string driverPath)
        {
            if (!File.Exists(driverPath))
                return false;

            try
            {
                var driverInfo = FileVersionInfo.GetVersionInfo(driverPath);
                var productVersion = driverInfo.ProductVersion ?? driverInfo.FileVersion;

                if (!string.IsNullOrWhiteSpace(productVersion))
                {
                    int driverMajor;
                    if (int.TryParse(productVersion.Split('.')[0], out driverMajor))
                    {
                        return driverMajor == InstalledChromeMajorVersion;
                    }
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Fetch latest ChromeDriver version from Chrome for Testing API
        /// </summary>
        public async Task<string> FetchLatestVersionAsync()
        {
            // 1. Intentar buscar versión exacta del major en Chrome for Testing API
            try
            {
                string jsonResponse = await _httpClient.GetStringAsync(CHROME_FOR_TESTING_API);
                JObject json = JObject.Parse(jsonResponse);

                var channels = json["channels"];
                if (channels != null)
                {
                    // Buscar en todos los canales una versión que coincida con el major instalado
                    foreach (var channelProperty in channels.Children<JProperty>())
                    {
                        var channelData = channelProperty.Value;
                        var versionToken = channelData["version"];

                        if (versionToken != null)
                        {
                            string version = versionToken.ToString();
                            int versionMajor;

                            if (int.TryParse(version.Split('.')[0], out versionMajor) &&
                                versionMajor == InstalledChromeMajorVersion)
                            {
                                ChromeDriverVersion = version;

                                // Obtener URL de descarga para Windows
                                var downloads = channelData["downloads"]?["chromedriver"];
                                if (downloads != null)
                                {
                                    var windowsDownload = downloads.FirstOrDefault(d => d["platform"]?.ToString() == "win64");
                                    if (windowsDownload != null)
                                    {
                                        DownloadUrl = windowsDownload["url"].ToString();
                                        Console.WriteLine($"Encontrada versión compatible: {ChromeDriverVersion} (canal {channelProperty.Name})");
                                        return ChromeDriverVersion;
                                    }
                                }
                            }
                        }
                    }

                    // Si no se encuentra versión exacta, usar Stable
                    var stableChannel = channels["Stable"];
                    if (stableChannel != null)
                    {
                        ChromeDriverVersion = stableChannel["version"].ToString();
                        var downloads = stableChannel["downloads"]?["chromedriver"];
                        if (downloads != null)
                        {
                            var windowsDownload = downloads.FirstOrDefault(d => d["platform"]?.ToString() == "win64");
                            if (windowsDownload != null)
                            {
                                DownloadUrl = windowsDownload["url"].ToString();
                                Console.WriteLine($"Usando versión Stable: {ChromeDriverVersion}");
                                return ChromeDriverVersion;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo versión de Chrome for Testing API: {ex.Message}");
            }

            // 2. Intentar API legacy de ChromeDriver Storage
            try
            {
                string legacyUrl = LEGACY_API_BASE + InstalledChromeMajorVersion;
                ChromeDriverVersion = await _httpClient.GetStringAsync(legacyUrl);
                ChromeDriverVersion = ChromeDriverVersion.Trim();

                // Generar URL de descarga
                DownloadUrl = $"https://chromedriver.storage.googleapis.com/{ChromeDriverVersion}/chromedriver_win32.zip";
                Console.WriteLine($"Versión obtenida de API legacy: {ChromeDriverVersion}");
                return ChromeDriverVersion;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo versión de API legacy: {ex.Message}");
            }

            // 3. Fallback a versión estable conocida
            ChromeDriverVersion = "145.0.7632.46"; // Actualizado a versión actual
            DownloadUrl = $"https://storage.googleapis.com/chrome-for-testing-public/{ChromeDriverVersion}/win64/chromedriver-win64.zip";
            Console.WriteLine($"Usando versión fallback: {ChromeDriverVersion}");
            return ChromeDriverVersion;
        }

        /// <summary>
        /// Download and install ChromeDriver
        /// </summary>
        public async Task DownloadAndInstallAsync()
        {
            string currentDir = Environment.CurrentDirectory;
            string zipPath = Path.Combine(currentDir, "chromedriver.zip");
            string extractPath = currentDir;
            string driverExe = Path.Combine(currentDir, "chromedriver.exe");

            // Limpiar drivers previos
            try
            {
                if (File.Exists(driverExe))
                {
                    File.Delete(driverExe);
                    Console.WriteLine("ChromeDriver anterior eliminado");
                }

                // Limpiar posibles subdirectorios de extracción anterior
                var oldDriverDirs = Directory.GetDirectories(currentDir, "chromedriver-win*");
                foreach (var dir in oldDriverDirs)
                {
                    try { Directory.Delete(dir, true); } catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Advertencia al limpiar archivos: {ex.Message}");
            }

            // Lista de URLs de fallback
            var downloadUrls = new List<string>
            {
                DownloadUrl, // URL primaria obtenida de la API
                $"https://storage.googleapis.com/chrome-for-testing-public/{ChromeDriverVersion}/win64/chromedriver-win64.zip",
                $"https://storage.googleapis.com/chrome-for-testing-public/{ChromeDriverVersion}/win32/chromedriver-win32.zip",
                $"https://edgedl.me.gvt1.com/edgedl/chrome/chrome-for-testing/{ChromeDriverVersion}/win64/chromedriver-win64.zip",
                $"https://edgedl.me.gvt1.com/edgedl/chrome/chrome-for-testing/{ChromeDriverVersion}/win32/chromedriver-win32.zip",
                $"https://chromedriver.storage.googleapis.com/{ChromeDriverVersion}/chromedriver_win32.zip"
            };

            Exception lastException = null;
            bool downloaded = false;

            // Intentar descargar de cada URL
            foreach (var url in downloadUrls.Distinct())
            {
                try
                {
                    Console.WriteLine($"Intentando descargar de: {url}");
                    await InternetHelper.DownloadFileAsync(url, zipPath);

                    // Verificar que el archivo descargado es válido
                    var fileInfo = new FileInfo(zipPath);
                    if (fileInfo.Length < 100000)
                    {
                        throw new Exception("Archivo descargado sospechosamente pequeño");
                    }

                    downloaded = true;
                    Console.WriteLine($"Descarga exitosa ({fileInfo.Length / 1024} KB)");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error descargando de {url}: {ex.Message}");
                    lastException = ex;

                    // Limpiar archivo parcial
                    try
                    {
                        if (File.Exists(zipPath))
                            File.Delete(zipPath);
                    }
                    catch { }
                }
            }

            if (!downloaded)
            {
                throw new InvalidOperationException(
                    $"No se pudo descargar ChromeDriver {ChromeDriverVersion} de ninguna fuente disponible",
                    lastException
                );
            }

            try
            {
                // Extraer archivo
                Console.WriteLine("Extrayendo ChromeDriver...");
                FastZip fastZip = new FastZip();
                fastZip.ExtractZip(zipPath, extractPath, null);

                // Buscar chromedriver.exe (puede estar en subcarpetas)
                var extractedDriver = Directory
                    .EnumerateFiles(extractPath, "chromedriver.exe", SearchOption.AllDirectories)
                    .FirstOrDefault();

                if (extractedDriver != null && !string.Equals(extractedDriver, driverExe, StringComparison.OrdinalIgnoreCase))
                {
                    // Mover a la raíz si está en subcarpeta
                    File.Copy(extractedDriver, driverExe, true);
                    Console.WriteLine($"ChromeDriver movido a: {driverExe}");

                    // Limpiar subcarpeta
                    try
                    {
                        string subDir = Path.GetDirectoryName(extractedDriver);
                        if (subDir != extractPath)
                        {
                            Directory.Delete(subDir, true);
                        }
                    }
                    catch { }
                }

                if (!File.Exists(driverExe))
                {
                    throw new FileNotFoundException("No se encontró chromedriver.exe después de la extracción");
                }

                Console.WriteLine($"✓ ChromeDriver {ChromeDriverVersion} instalado exitosamente");

                // Verificar versión instalada
                try
                {
                    var installedVersion = FileVersionInfo.GetVersionInfo(driverExe);
                    Console.WriteLine($"Versión verificada: {installedVersion.ProductVersion ?? installedVersion.FileVersion}");
                }
                catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error durante extracción: {ex.Message}");
                throw;
            }
            finally
            {
                // Limpiar archivo zip
                try
                {
                    if (File.Exists(zipPath))
                        File.Delete(zipPath);
                }
                catch { }
            }
        }

        /// <summary>
        /// Kill all running ChromeDriver processes
        /// </summary>
        public void KillChromeDriverProcesses()
        {
            try
            {
                Process[] chromeDriverProcesses = Process.GetProcessesByName("chromedriver");

                foreach (Process process in chromeDriverProcesses)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(5000);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error killing ChromeDriver: {ex.Message}");
            }
        }
    }
}
