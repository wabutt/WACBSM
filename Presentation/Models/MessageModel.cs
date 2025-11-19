using System;

namespace Presentation.Models
{
    /// <summary>
    /// Type of attachment for a message
    /// </summary>
    public enum AttachmentType
    {
        None,
        Image,      // I
        Video,      // I
        Audio,      // A
        Document    // D
    }

    /// <summary>
    /// Represents a message to be sent
    /// </summary>
    public class MessageModel
    {
        /// <summary>
        /// Message text content
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Path to attachment file
        /// </summary>
        public string AttachmentPath { get; set; }

        /// <summary>
        /// Type of attachment
        /// </summary>
        public AttachmentType AttachmentType { get; set; }

        /// <summary>
        /// Whether this message has an attachment
        /// </summary>
        public bool HasAttachment => !string.IsNullOrWhiteSpace(AttachmentPath);

        public MessageModel()
        {
            AttachmentType = AttachmentType.None;
        }

        public MessageModel(string content)
        {
            Content = content;
            AttachmentType = AttachmentType.None;
        }

        public MessageModel(string content, string attachmentPath, AttachmentType type)
        {
            Content = content;
            AttachmentPath = attachmentPath;
            AttachmentType = type;
        }

        public override string ToString()
        {
            if (HasAttachment)
                return $"{Content} [Attachment: {AttachmentType}]";
            return Content;
        }
    }
}
