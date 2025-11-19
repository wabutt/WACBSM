using System;
using Presentation.Helpers;
using Presentation.Models;

namespace Presentation.Services
{
    /// <summary>
    /// Service for handling file attachments
    /// </summary>
    public class AttachmentService
    {
        /// <summary>
        /// Validate attachment file
        /// </summary>
        public bool ValidateAttachment(string filePath, out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                errorMessage = "No file selected";
                return false;
            }

            if (!System.IO.File.Exists(filePath))
            {
                errorMessage = "File does not exist";
                return false;
            }

            var fileInfo = new System.IO.FileInfo(filePath);

            // Check file size limits
            long maxSizeBytes = 67108864; // 64 MB

            if (fileInfo.Length > maxSizeBytes)
            {
                errorMessage = $"File size exceeds maximum allowed (64 MB). Current size: {fileInfo.Length / 1024 / 1024} MB";
                return false;
            }

            if (fileInfo.Length == 0)
            {
                errorMessage = "File is empty";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determine attachment type from file path
        /// </summary>
        public AttachmentType DetermineAttachmentType(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return AttachmentType.None;

            if (FileHelper.IsImageFile(filePath))
                return AttachmentType.Image;

            if (FileHelper.IsVideoFile(filePath))
                return AttachmentType.Video;

            if (FileHelper.IsAudioFile(filePath))
                return AttachmentType.Audio;

            if (FileHelper.IsDocumentFile(filePath))
                return AttachmentType.Document;

            return AttachmentType.Document; // Default
        }

        /// <summary>
        /// Get attachment type code for internal use
        /// </summary>
        public string GetAttachmentTypeCode(AttachmentType type)
        {
            switch (type)
            {
                case AttachmentType.Image:
                case AttachmentType.Video:
                    return "I"; // Image/Video

                case AttachmentType.Audio:
                    return "A"; // Audio

                case AttachmentType.Document:
                    return "D"; // Document

                default:
                    return null;
            }
        }

        /// <summary>
        /// Create message model with attachment
        /// </summary>
        public MessageModel CreateMessageWithAttachment(string messageText, string attachmentPath)
        {
            var attachmentType = DetermineAttachmentType(attachmentPath);

            return new MessageModel
            {
                Content = messageText,
                AttachmentPath = attachmentPath,
                AttachmentType = attachmentType
            };
        }
    }
}
