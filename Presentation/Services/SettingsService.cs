using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Newtonsoft.Json;
using Presentation.Helpers;
using Presentation.Models;

namespace Presentation.Services
{
    /// <summary>
    /// Service for application settings persistence
    /// </summary>
    public class SettingsService
    {
        private const string SETTINGS_FILENAME = "Settings.json";
        private const string MESSAGES_FILENAME = "Messages.json";
        private const string MESSAGES2_FILENAME = "Messages2.json";

        /// <summary>
        /// Save application settings to JSON
        /// </summary>
        public void SaveSettings(ApplicationSettingsModel settings)
        {
            try
            {
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                FileHelper.WriteJsonToFile(json, SETTINGS_FILENAME);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Load application settings from JSON
        /// </summary>
        public ApplicationSettingsModel LoadSettings()
        {
            try
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "tempfilesWAButt",
                    SETTINGS_FILENAME
                );

                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonConvert.DeserializeObject<ApplicationSettingsModel>(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }

            // Return default settings
            return new ApplicationSettingsModel();
        }

        /// <summary>
        /// Save messages to JSON
        /// </summary>
        public void SaveMessages(List<string> messages, bool isSMS = false)
        {
            try
            {
                string filename = isSMS ? MESSAGES2_FILENAME : MESSAGES_FILENAME;
                string json = JsonConvert.SerializeObject(messages, Formatting.Indented);
                FileHelper.WriteJsonToFile(json, filename);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving messages: {ex.Message}");
            }
        }

        /// <summary>
        /// Load messages from JSON
        /// </summary>
        public List<string> LoadMessages(bool isSMS = false)
        {
            try
            {
                string filename = isSMS ? MESSAGES2_FILENAME : MESSAGES_FILENAME;
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "tempfilesWAButt",
                    filename
                );

                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonConvert.DeserializeObject<List<string>>(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading messages: {ex.Message}");
            }

            return new List<string>();
        }
    }
}
