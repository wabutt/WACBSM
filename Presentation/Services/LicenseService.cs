using System;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Domain;
using Newtonsoft.Json;
using Presentation.Models;

namespace Presentation.Services
{
    /// <summary>
    /// Service for license validation and management
    /// </summary>
    public class LicenseService
    {
        private const string API_BASE_URL = "https://wabutt-api.onrender.com/api/keys";
        private readonly HttpClient _httpClient;

        public LicenseService()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Validate license key against remote API
        /// </summary>
        public async Task<LicenseModel> ValidateAPIKeyAsync(string licenseKey)
        {
            try
            {
                UserModel user = new UserModel();

                var payload = new
                {
                    key = licenseKey,
                    deviceId = user.Deviceid,
                    machineId = user.Machineid
                };

                string jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync($"{API_BASE_URL}/validate", content);
                string jsonResponse = await response.Content.ReadAsStringAsync();

                // Parse response
                var apiResult = JsonConvert.DeserializeObject<LicenseCheckResult>(jsonResponse);

                // Map to LicenseModel
                var licenseModel = new LicenseModel
                {
                    IsValid = apiResult.valid,
                    Message = apiResult.message,
                    Status = apiResult.status,
                    Plan = apiResult.plan,
                    LicenseKey = licenseKey,
                    DevicesUsed = apiResult.devicesUsed,
                    MaxDevices = apiResult.maxDevices
                };

                // Parse expiration date
                if (!string.IsNullOrEmpty(apiResult.expiresAt))
                {
                    if (DateTime.TryParse(apiResult.expiresAt, out DateTime expiresAt))
                    {
                        licenseModel.ExpiresAt = expiresAt;
                    }
                }

                return licenseModel;
            }
            catch (Exception ex)
            {
                return new LicenseModel
                {
                    IsValid = false,
                    Message = $"Error validating license: {ex.Message}",
                    Status = "ERROR"
                };
            }
        }

        /// <summary>
        /// Prompt user to enter license key
        /// </summary>
        public string PromptForLicenseKey()
        {
            using (Form prompt = new Form())
            {
                prompt.Width = 400;
                prompt.Height = 180;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = "Ingresar Licencia";
                prompt.StartPosition = FormStartPosition.CenterScreen;
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;

                Label textLabel = new Label()
                {
                    Left = 20,
                    Top = 20,
                    Width = 360,
                    Text = "Por favor ingrese su clave de licencia:"
                };

                TextBox textBox = new TextBox()
                {
                    Left = 20,
                    Top = 50,
                    Width = 340
                };

                Button confirmation = new Button()
                {
                    Text = "Aceptar",
                    Left = 200,
                    Width = 80,
                    Top = 90,
                    DialogResult = DialogResult.OK
                };

                Button cancel = new Button()
                {
                    Text = "Cancelar",
                    Left = 290,
                    Width = 80,
                    Top = 90,
                    DialogResult = DialogResult.Cancel
                };

                confirmation.Click += (sender, e) => { prompt.Close(); };
                cancel.Click += (sender, e) => { prompt.Close(); };

                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(textBox);
                prompt.Controls.Add(confirmation);
                prompt.Controls.Add(cancel);
                prompt.AcceptButton = confirmation;
                prompt.CancelButton = cancel;

                return prompt.ShowDialog() == DialogResult.OK ? textBox.Text.Trim() : null;
            }
        }

        /// <summary>
        /// Save license key to application settings
        /// </summary>
        public void SaveLicenseKey(string licenseKey)
        {
            Properties.Settings.Default.LicenseKey = licenseKey;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Load license key from application settings
        /// </summary>
        public string LoadLicenseKey()
        {
            return Properties.Settings.Default.LicenseKey;
        }

        /// <summary>
        /// Clear stored license key
        /// </summary>
        public void ClearLicenseKey()
        {
            Properties.Settings.Default.LicenseKey = string.Empty;
            Properties.Settings.Default.Save();
        }

        // Internal class for API response deserialization
        private class LicenseCheckResult
        {
            public bool valid { get; set; }
            public string message { get; set; }
            public string status { get; set; }
            public string plan { get; set; }
            public string expiresAt { get; set; }
            public int devicesUsed { get; set; }
            public int maxDevices { get; set; }
        }
    }
}
