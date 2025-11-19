using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace Presentation.Helpers
{
    /// <summary>
    /// Helper class for internet connectivity operations
    /// </summary>
    public static class InternetHelper
    {
        /// <summary>
        /// Check if internet connection is available
        /// </summary>
        public static bool CheckForInternetConnection()
        {
            try
            {
                // Try to ping Google DNS
                using (var ping = new Ping())
                {
                    PingReply reply = ping.Send("8.8.8.8", 3000);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                // If ping fails, try HTTP request as fallback
                try
                {
                    using (var client = new WebClient())
                    {
                        using (client.OpenRead("http://www.google.com"))
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Check if internet connection is available (async version)
        /// </summary>
        public static async Task<bool> CheckForInternetConnectionAsync()
        {
            return await Task.Run(() => CheckForInternetConnection());
        }

        /// <summary>
        /// Download file from URL to destination path
        /// </summary>
        public static void DownloadFile(string url, string destinationPath)
        {
            using (var client = new WebClient())
            {
                client.DownloadFile(url, destinationPath);
            }
        }

        /// <summary>
        /// Download file from URL to destination path (async)
        /// </summary>
        public static async Task DownloadFileAsync(string url, string destinationPath)
        {
            using (var client = new WebClient())
            {
                await client.DownloadFileTaskAsync(url, destinationPath);
            }
        }

        /// <summary>
        /// Download string content from URL
        /// </summary>
        public static string DownloadString(string url)
        {
            using (var client = new WebClient())
            {
                return client.DownloadString(url);
            }
        }

        /// <summary>
        /// Download string content from URL (async)
        /// </summary>
        public static async Task<string> DownloadStringAsync(string url)
        {
            using (var client = new WebClient())
            {
                return await client.DownloadStringTaskAsync(url);
            }
        }
    }
}
