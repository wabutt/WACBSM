using System;

namespace Presentation.Models
{
    /// <summary>
    /// Represents a contact for messaging
    /// </summary>
    public class ContactModel
    {
        /// <summary>
        /// Contact's phone number
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Contact's name (optional)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Status of the message sent to this contact
        /// S = Sent, N = Not Sent, null = Pending
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Whether the message was successfully sent
        /// </summary>
        public bool IsSent => Status == "S";

        /// <summary>
        /// Whether the contact has valid phone number
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(PhoneNumber);

        public ContactModel()
        {
        }

        public ContactModel(string phoneNumber, string name)
        {
            PhoneNumber = phoneNumber;
            Name = name;
        }

        public override string ToString()
        {
            return $"{PhoneNumber} - {Name} ({Status ?? "Pending"})";
        }
    }
}
