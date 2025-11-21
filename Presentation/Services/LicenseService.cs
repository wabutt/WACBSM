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
        private const string API_BASE_URL = "https://licencias.ntsoporte.com/api/license/check";
        private const string API_KEY = "afe3d05f072e9fe4ce3f243f685af78db67136e70605098709f0e7c2527e2449";
        private readonly HttpClient _httpClient;

        public LicenseService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(20)
            };
            _httpClient.DefaultRequestHeaders.Add("X-API-KEY", API_KEY);
        }

        /// <summary>
        /// Validate license key against remote API
        /// Returns LicenseModel with validation result, or null if network/server error occurs
        /// </summary>
        public async Task<LicenseModel> ValidateAPIKeyAsync(string licenseKey)
        {
            try
            {
                UserModel user = new UserModel();
                string machineGuid = user.GetMachineGuid();

                var payload = new
                {
                    licenseKey = licenseKey,
                    hwid = machineGuid,
                    appVersion = Application.ProductVersion,
                    machineName = Environment.MachineName
                };

                string jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                try
                {
                    response = await _httpClient.PostAsync(API_BASE_URL, content);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "No se pudo contactar al servidor de licencias.\n" + ex.Message,
                        "Error de red",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return null;
                }

                string responseJson = await response.Content.ReadAsStringAsync();

                // If server responds with 4xx/5xx, try to show a friendly message
                if (!response.IsSuccessStatusCode)
                {
                    try
                    {
                        var apiError = JsonConvert.DeserializeObject<LicenseCheckResult>(responseJson);
                        if (apiError != null && !string.IsNullOrWhiteSpace(apiError.message))
                        {
                            MessageBox.Show(
                                apiError.message,
                                "Error de licencia",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning
                            );
                        }
                        else
                        {
                            MessageBox.Show(
                                "Error del servidor de licencias: " + (int)response.StatusCode,
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );
                        }
                    }
                    catch
                    {
                        MessageBox.Show(
                            "Error del servidor de licencias: " + (int)response.StatusCode,
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                    return null;
                }

                LicenseCheckResult result;
                try
                {
                    result = JsonConvert.DeserializeObject<LicenseCheckResult>(responseJson);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Respuesta inválida del servidor de licencias.\n" + ex.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return null;
                }

                // Map to LicenseModel
                var licenseModel = new LicenseModel
                {
                    IsValid = result.valid,
                    Message = result.message,
                    Status = result.status,
                    Plan = result.plan,
                    LicenseKey = licenseKey,
                    DevicesUsed = result.devicesUsed,
                    MaxDevices = result.maxDevices
                };

                // Parse expiration date
                if (!string.IsNullOrEmpty(result.expiresAt))
                {
                    if (DateTime.TryParse(result.expiresAt, out DateTime expiresAt))
                    {
                        licenseModel.ExpiresAt = expiresAt;
                    }
                }

                return licenseModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error inesperado al validar la licencia.\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return null;
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
