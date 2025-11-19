using System;

namespace Presentation.Models
{
    /// <summary>
    /// Model representing a message with optional attachment
    /// </summary>
    public class MessageModel
    {
        /// <summary>
        /// Text content of the message
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Path to the attachment file (if any)
        /// </summary>
        public string AttachmentPath { get; set; }

        /// <summary>
        /// Type of attachment
        /// </summary>
        public AttachmentType AttachmentType { get; set; }

        /// <summary>
        /// Contact number or recipient identifier
        /// </summary>
        public string ContactNumber { get; set; }

        /// <summary>
        /// Timestamp when message was created
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Indicates if message has an attachment
        /// </summary>
        public bool HasAttachment => !string.IsNullOrWhiteSpace(AttachmentPath);

        /// <summary>
        /// Constructor
        /// </summary>
        public MessageModel()
        {
            Timestamp = DateTime.Now;
            AttachmentType = AttachmentType.None;
        }

        /// <summary>
        /// Constructor with content
        /// </summary>
        public MessageModel(string content) : this()
        {
            Content = content;
        }

        /// <summary>
        /// Constructor with content and attachment
        /// </summary>
        public MessageModel(string content, string attachmentPath) : this(content)
        {
            AttachmentPath = attachmentPath;
        }
    }
}
