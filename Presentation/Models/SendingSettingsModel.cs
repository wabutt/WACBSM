using System;

namespace Presentation.Models
{
    /// <summary>
    /// Settings for message sending behavior
    /// </summary>
    public class SendingSettingsModel
    {
        /// <summary>
        /// Whether to send full name in message
        /// </summary>
        public bool SendFullName { get; set; }

        /// <summary>
        /// Whether to send date/time in message
        /// </summary>
        public bool SendDateTime { get; set; }

        /// <summary>
        /// Whether to enable anti-blocking timing
        /// </summary>
        public bool PreventBlock { get; set; }

        /// <summary>
        /// Whether to send multiple messages from list
        /// </summary>
        public bool SendManyMessages { get; set; }

        /// <summary>
        /// Delay between each message in milliseconds
        /// </summary>
        public int EachMessageDelay { get; set; }

        /// <summary>
        /// Number of messages after which to auto-pause
        /// </summary>
        public int AutoPauseAfterMessages { get; set; }

        /// <summary>
        /// Whether to send only attachment without message
        /// </summary>
        public bool SendOnlyAttachment { get; set; }

        /// <summary>
        /// Anti-blocking additional timing in milliseconds
        /// </summary>
        public int PreventBlockTiming { get; set; }

        public SendingSettingsModel()
        {
            // Default values
            SendFullName = false;
            SendDateTime = false;
            PreventBlock = false;
            SendManyMessages = false;
            EachMessageDelay = 0;
            AutoPauseAfterMessages = 0;
            SendOnlyAttachment = false;
            PreventBlockTiming = 0;
        }
    }

    /// <summary>
    /// Application-wide settings
    /// </summary>
    public class ApplicationSettingsModel
    {
        /// <summary>
        /// WhatsApp sending settings
        /// </summary>
        public SendingSettingsModel WhatsAppSettings { get; set; }

        /// <summary>
        /// SMS sending settings
        /// </summary>
        public SendingSettingsModel SMSSettings { get; set; }

        /// <summary>
        /// Last selected attachment file path
        /// </summary>
        public string LastAttachmentPath { get; set; }

        /// <summary>
        /// Last selected attachment type
        /// </summary>
        public string LastAttachmentType { get; set; }

        public ApplicationSettingsModel()
        {
            WhatsAppSettings = new SendingSettingsModel();
            SMSSettings = new SendingSettingsModel();
        }
    }
}
