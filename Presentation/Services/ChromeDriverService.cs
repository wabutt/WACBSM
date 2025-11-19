using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json.Linq;
using Presentation.Helpers;

namespace Presentation.Services
{
    /// <summary>
    /// Service for ChromeDriver management
    /// </summary>
    public class ChromeDriverService
    {
        private const string CHROME_FOR_TESTING_API = "https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions-with-downloads.json";
        private readonly HttpClient _httpClient;

        public string ChromeDriverVersion { get; private set; }
        public string DownloadUrl { get; private set; }

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
        /// Fetch latest ChromeDriver version from Chrome for Testing API
        /// </summary>
        public async Task<string> FetchLatestVersionAsync()
        {
            try
            {
                string jsonResponse = await _httpClient.GetStringAsync(CHROME_FOR_TESTING_API);
                JObject json = JObject.Parse(jsonResponse);

                var stableChannel = json["channels"]["Stable"];
                ChromeDriverVersion = stableChannel["version"].ToString();

                // Get download URL for Windows chromedriver
                var downloads = stableChannel["downloads"]["chromedriver"];
                var windowsDownload = downloads.FirstOrDefault(d => d["platform"].ToString() == "win64");

                if (windowsDownload != null)
                {
                    DownloadUrl = windowsDownload["url"].ToString();
                }
                else
                {
                    // Fallback
                    ChromeDriverVersion = "131.0.6778.87";
                    DownloadUrl = $"https://storage.googleapis.com/chrome-for-testing-public/{ChromeDriverVersion}/win64/chromedriver-win64.zip";
                }

                return ChromeDriverVersion;
            }
            catch
            {
                // Fallback version
                ChromeDriverVersion = "131.0.6778.87";
                DownloadUrl = $"https://storage.googleapis.com/chrome-for-testing-public/{ChromeDriverVersion}/win64/chromedriver-win64.zip";
                return ChromeDriverVersion;
            }
        }

        /// <summary>
        /// Download and install ChromeDriver
        /// </summary>
        public async Task DownloadAndInstallAsync()
        {
            string currentDir = Environment.CurrentDirectory;
            string zipPath = Path.Combine(currentDir, "chromedriver.zip");
            string extractPath = currentDir;

            try
            {
                // Download
                await InternetHelper.DownloadFileAsync(DownloadUrl, zipPath);

                // Extract
                FastZip fastZip = new FastZip();
                fastZip.ExtractZip(zipPath, extractPath, null);

                // Clean up
                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                Console.WriteLine($"ChromeDriver {ChromeDriverVersion} installed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ChromeDriver install error: {ex.Message}");
                throw;
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
