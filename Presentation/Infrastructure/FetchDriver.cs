using System;
using System.IO;
using System.IO.Compression;   // ⚠️ Agrega ref a System.IO.Compression y System.IO.Compression.FileSystem
using System.Linq;
using System.Net;
using System.Diagnostics;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;    // via Newtonsoft.Json
using ICSharpCode.SharpZipLib.Zip;

namespace Infrastructure
{
    public static class FetchDriver
    {
        // Punto de entrada: asegura un chromedriver que coincida con el major de Chrome instalado.
        // Devuelve la ruta al chromedriver.exe
        public static string EnsureMatchingChromeDriver(string driverDir)
        {
            if (string.IsNullOrWhiteSpace(driverDir))
                throw new ArgumentException("driverDir no puede ser vacío.");

            Directory.CreateDirectory(driverDir);

            int chromeMajor = GetInstalledChromeMajor();
            var driverExe = Path.Combine(driverDir, "chromedriver.exe");

            // ¿Ya hay driver del mismo major?
            if (File.Exists(driverExe))
            {
                try
                {
                    var info = FileVersionInfo.GetVersionInfo(driverExe);
                    var product = info.ProductVersion ?? info.FileVersion; // "142.0.x.x"
                    if (!string.IsNullOrWhiteSpace(product))
                    {
                        int currentMajor;
                        if (int.TryParse(product.Split('.')[0], out currentMajor) && currentMajor == chromeMajor)
                            return driverExe;
                    }
                }
                catch { /* re-descargar si falla */ }
            }

            // Descarga el que corresponda
            var version = FetchChromedriverVersionForMajor(chromeMajor);
            DownloadAndExtractChromedriver(version, driverDir);

            // A veces queda en subcarpetas (chromedriver-win64\chromedriver.exe)
            var nested = Directory
                .EnumerateFiles(driverDir, "chromedriver.exe", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (nested != null && !string.Equals(nested, driverExe, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // Copia y borra el original para compatibilidad con .NET Framework
                    File.Copy(nested, driverExe, true);
                    try { File.Delete(nested); } catch { }
                }
                catch
                {
                    // Si no pudo copiar, al menos usa el nested
                    driverExe = nested;
                }
            }

            if (!File.Exists(driverExe))
                throw new FileNotFoundException("No se pudo ubicar chromedriver.exe tras la extracción.");

            return driverExe;
        }

        // ---- Helpers ----

        private static int GetInstalledChromeMajor()
        {
            // 1) HKCU\BLBeacon
            try
            {
                var v = Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Google\Chrome\BLBeacon",
                    "version",
                    null
                ) as string;

                int major;
                if (!string.IsNullOrWhiteSpace(v) && int.TryParse(v.Split('.')[0], out major))
                    return major;
            }
            catch { /* ignore */ }

            // 2) FileVersion de chrome.exe
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),     "Google","Chrome","Application","chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google","Chrome","Application","chrome.exe")
            };

            foreach (var exe in candidates)
            {
                if (File.Exists(exe))
                {
                    var fv = FileVersionInfo.GetVersionInfo(exe).FileVersion; // "142.0.7444.61"
                    int major;
                    if (!string.IsNullOrWhiteSpace(fv) && int.TryParse(fv.Split('.')[0], out major))
                        return major;
                }
            }

            throw new InvalidOperationException("No se pudo detectar la versión de Google Chrome instalada.");
        }

        private static string FetchChromedriverVersionForMajor(int major)
        {
            // 1) Endpoint clásico por major
            var url = "https://chromedriver.storage.googleapis.com/LATEST_RELEASE_" + major;
            try
            {
                using (var wc = new WebClient())
                {
                    var v = wc.DownloadString(url);
                    if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
                }
            }
            catch { /* fallback */ }

            // 2) Fallback: Chrome for Testing JSON
            const string cft = "https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions-with-downloads.json";
            try
            {
                using (var wc = new WebClient())
                {
                    var json = wc.DownloadString(cft);
                    var data = JObject.Parse(json);

                    // busca un canal cuyo major coincida
                    var channels = data["channels"];
                    if (channels != null)
                    {
                        foreach (var prop in channels.Children<JProperty>())
                        {
                            var vToken = prop.Value["version"];
                            if (vToken != null)
                            {
                                var vStr = vToken.ToString();
                                int mj;
                                if (int.TryParse(vStr.Split('.')[0], out mj) && mj == major)
                                    return vStr;
                            }
                        }

                        // último recurso: usa stable
                        var stable = channels["stable"]?["version"];
                        if (stable != null) return stable.ToString();
                    }
                }
            }
            catch { /* ignore */ }

            throw new InvalidOperationException("No se pudo resolver la versión de ChromeDriver para el major " + major + ".");
        }

        private static void DownloadAndExtractChromedriver(string version, string driverDir)
        {
            // limpia drivers previos
            try
            {
                foreach (var f in Directory.EnumerateFiles(driverDir, "chromedriver*.exe", SearchOption.AllDirectories))
                    TryDelete(f);
            }
            catch { /* ignore */ }

            var zipPath = Path.Combine(driverDir, "chromedriver.zip");

            // Fuentes ordenadas (CfT público + mirrors + storage clásico)
            var urls = new[]
            {
                "https://storage.googleapis.com/chrome-for-testing-public/" + version + "/win64/chromedriver-win64.zip",
                "https://storage.googleapis.com/chrome-for-testing-public/" + version + "/win32/chromedriver-win32.zip",
                "https://edgedl.me.gvt1.com/edgedl/chrome/chrome-for-testing/" + version + "/win64/chromedriver-win64.zip",
                "https://edgedl.me.gvt1.com/edgedl/chrome/chrome-for-testing/" + version + "/win32/chromedriver-win32.zip",
                "https://chromedriver.storage.googleapis.com/" + version + "/chromedriver_win64.zip",
                "https://chromedriver.storage.googleapis.com/" + version + "/chromedriver_win32.zip"
            };

            Exception last = null;
            foreach (var url in urls)
            {
                try
                {
                    using (var wc = new WebClient())
                    {
                        wc.DownloadFile(url, zipPath);
                    }
                    var fi = new FileInfo(zipPath);
                    if (fi.Length < 100000) throw new Exception("Zip sospechosamente pequeño.");
                    last = null;
                    break;
                }
                catch (Exception ex)
                {
                    last = ex;
                    TryDelete(zipPath);
                }
            }

            if (last != null)
                throw new InvalidOperationException("No se pudo descargar ChromeDriver compatible.", last);

            try
            {
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
