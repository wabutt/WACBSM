using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using ICSharpCode.SharpZipLib.Zip;

namespace Infrastructure
{
    public static class FetchDriver
    {
        private const string CHROME_FOR_TESTING_API = "https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions-with-downloads.json";

        /// <summary>
        /// Asegura que exista la última versión Stable de ChromeDriver.
        /// Si el usuario tiene una versión de Chrome diferente, se recomienda actualizar Chrome.
        /// </summary>
        public static string EnsureMatchingChromeDriver(string driverDir)
        {
            if (string.IsNullOrWhiteSpace(driverDir))
                throw new ArgumentException("driverDir no puede ser vacío.");

            Directory.CreateDirectory(driverDir);

            var driverExe = Path.Combine(driverDir, "chromedriver.exe");

            // Obtener la última versión Stable y su URL de descarga
            string latestVersion;
            string downloadUrl;
            GetLatestStableVersion(out latestVersion, out downloadUrl);

            Console.WriteLine($"Última versión Stable de ChromeDriver: {latestVersion}");

            // Verificar si ya existe el driver con la versión correcta
            if (File.Exists(driverExe))
            {
                try
                {
                    var info = FileVersionInfo.GetVersionInfo(driverExe);
                    var currentVersion = info.ProductVersion ?? info.FileVersion;

                    if (!string.IsNullOrWhiteSpace(currentVersion))
                    {
                        // Comparar major.minor.build (ignorar revision)
                        var currentParts = currentVersion.Split('.');
                        var latestParts = latestVersion.Split('.');

                        if (currentParts.Length >= 3 && latestParts.Length >= 3)
                        {
                            bool sameVersion = currentParts[0] == latestParts[0] &&
                                               currentParts[1] == latestParts[1] &&
                                               currentParts[2] == latestParts[2];

                            if (sameVersion)
                            {
                                Console.WriteLine($"✓ ChromeDriver {currentVersion} ya está actualizado");
                                return driverExe;
                            }
                        }
                    }
                }
                catch { /* re-descargar si falla la verificación */ }
            }

            // Descargar la última versión
            Console.WriteLine($"Descargando ChromeDriver {latestVersion}...");
            DownloadAndExtractChromedriver(downloadUrl, driverDir);

            // A veces queda en subcarpetas (chromedriver-win64\chromedriver.exe)
            var nested = Directory
                .EnumerateFiles(driverDir, "chromedriver.exe", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (nested != null && !string.Equals(nested, driverExe, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    File.Copy(nested, driverExe, true);
                    try { File.Delete(nested); } catch { }

                    // Limpiar carpeta vacía
                    var nestedDir = Path.GetDirectoryName(nested);
                    if (nestedDir != null && Directory.Exists(nestedDir) && !Directory.EnumerateFileSystemEntries(nestedDir).Any())
                    {
                        try { Directory.Delete(nestedDir); } catch { }
                    }
                }
                catch
                {
                    driverExe = nested;
                }
            }

            if (!File.Exists(driverExe))
                throw new FileNotFoundException("No se pudo ubicar chromedriver.exe tras la extracción.");

            Console.WriteLine($"✓ ChromeDriver {latestVersion} instalado correctamente");
            return driverExe;
        }

        /// <summary>
        /// Obtiene la última versión Stable y su URL de descarga desde Chrome for Testing API
        /// </summary>
        private static void GetLatestStableVersion(out string version, out string downloadUrl)
        {
            version = null;
            downloadUrl = null;

            try
            {
                using (var wc = new WebClient())
                {
                    var json = wc.DownloadString(CHROME_FOR_TESTING_API);
                    var data = JObject.Parse(json);

                    var stable = data["channels"]?["Stable"];
                    if (stable != null)
                    {
                        version = stable["version"]?.ToString();

                        // Buscar URL para win64
                        var downloads = stable["downloads"]?["chromedriver"];
                        if (downloads != null)
                        {
                            foreach (var item in downloads)
                            {
                                if (item["platform"]?.ToString() == "win64")
                                {
                                    downloadUrl = item["url"]?.ToString();
                                    break;
                                }
                            }

                            // Fallback a win32 si no hay win64
                            if (string.IsNullOrEmpty(downloadUrl))
                            {
                                foreach (var item in downloads)
                                {
                                    if (item["platform"]?.ToString() == "win32")
                                    {
                                        downloadUrl = item["url"]?.ToString();
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo versión desde API: {ex.Message}");
            }

            // Fallback hardcoded si falla el API
            if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(downloadUrl))
            {
                version = "131.0.6778.87";
                downloadUrl = $"https://storage.googleapis.com/chrome-for-testing-public/{version}/win64/chromedriver-win64.zip";
                Console.WriteLine($"Usando versión fallback: {version}");
            }
        }

        private static void DownloadAndExtractChromedriver(string url, string driverDir)
        {
            // Limpiar drivers previos
            try
            {
                foreach (var f in Directory.EnumerateFiles(driverDir, "chromedriver*.exe", SearchOption.AllDirectories))
                    TryDelete(f);
            }
            catch { }

            var zipPath = Path.Combine(driverDir, "chromedriver.zip");

            try
            {
                using (var wc = new WebClient())
                {
                    wc.DownloadFile(url, zipPath);
                }

                var fi = new FileInfo(zipPath);
                if (fi.Length < 100000)
                    throw new Exception("Archivo descargado sospechosamente pequeño.");

                FastZip fastZip = new FastZip();
                fastZip.ExtractZip(zipPath, driverDir, "");
                Console.WriteLine("✓ Extracción completada");
            }
            finally
            {
                TryDelete(zipPath);
            }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }
}
