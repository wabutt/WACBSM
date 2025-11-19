using System;

namespace Presentation.Models
{
    /// <summary>
    /// Represents the result of a license validation check
    /// </summary>
    public class LicenseModel
    {
        /// <summary>
        /// Whether the license is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// License status (active, expired, etc.)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// License plan type
        /// </summary>
        public string Plan { get; set; }

        /// <summary>
        /// License key
        /// </summary>
        public string LicenseKey { get; set; }

        /// <summary>
        /// Expiration date (if applicable)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Number of devices currently using this license
        /// </summary>
        public int DevicesUsed { get; set; }

        /// <summary>
        /// Maximum devices allowed
        /// </summary>
        public int MaxDevices { get; set; }

        /// <summary>
        /// Whether the license has expired
        /// </summary>
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.Now;

        /// <summary>
        /// Whether device limit is reached
        /// </summary>
        public bool IsDeviceLimitReached => DevicesUsed >= MaxDevices;

        public override string ToString()
        {
            return $"License: {Status} - {Message}";
        }
    }
}
