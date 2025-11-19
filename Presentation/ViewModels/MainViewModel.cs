using System;
using Presentation.Models;
using Presentation.Services;

namespace Presentation.ViewModels
{
    /// <summary>
    /// ViewModel for main application logic
    /// </summary>
    public class MainViewModel
    {
        // Services
        public LicenseService LicenseService { get; private set; }
        public ChromeDriverService ChromeDriverService { get; private set; }
        public TimingService TimingService { get; private set; }
        public AttachmentService AttachmentService { get; private set; }
        public SettingsService SettingsService { get; private set; }
        public ContactService ContactService { get; private set; }

        // Models
        public ProgressModel WhatsAppProgress { get; private set; }
        public ProgressModel SMSProgress { get; private set; }
        public ApplicationSettingsModel Settings { get; private set; }

        public MainViewModel()
        {
            // Initialize services
            LicenseService = new LicenseService();
            ChromeDriverService = new ChromeDriverService();
            TimingService = new TimingService();
            AttachmentService = new AttachmentService();
            SettingsService = new SettingsService();
            ContactService = new ContactService();

            // Initialize models
            WhatsAppProgress = new ProgressModel();
            SMSProgress = new ProgressModel();
            Settings = new ApplicationSettingsModel();
        }

        /// <summary>
        /// Load application settings
        /// </summary>
        public void LoadSettings()
        {
            Settings = SettingsService.LoadSettings();
        }

        /// <summary>
        /// Save application settings
        /// </summary>
        public void SaveSettings()
        {
            SettingsService.SaveSettings(Settings);
        }

        /// <summary>
        /// Reset WhatsApp progress
        /// </summary>
        public void ResetWhatsAppProgress(int totalContacts)
        {
            WhatsAppProgress = new ProgressModel(totalContacts);
        }

        /// <summary>
        /// Reset SMS progress
        /// </summary>
        public void ResetSMSProgress(int totalContacts)
        {
            SMSProgress = new ProgressModel(totalContacts);
        }
    }
}
