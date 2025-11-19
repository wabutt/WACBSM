using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Presentation.Helpers
{
    /// <summary>
    /// Helper class for file operations and validation
    /// </summary>
    public static class FileHelper
    {
        // Supported file extensions by category
        private static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".ico", ".tiff", ".tif", ".svg"
        };

        private static readonly HashSet<string> VideoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm", ".m4v", ".mpg", ".mpeg", ".3gp"
        };

        private static readonly HashSet<string> AudioExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".ogg", ".m4a", ".flac", ".aac", ".wma", ".opus", ".aiff", ".ape"
        };

        private static readonly HashSet<string> DocumentExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".txt", ".rtf", ".odt", ".ods", ".odp", ".csv", ".xml", ".json"
        };

        /// <summary>
        /// Check if file is an image
        /// </summary>
        public static bool IsImageFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            string extension = Path.GetExtension(filePath);
            return ImageExtensions.Contains(extension);
        }

        /// <summary>
        /// Check if file is a video
        /// </summary>
        public static bool IsVideoFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            string extension = Path.GetExtension(filePath);
            return VideoExtensions.Contains(extension);
        }

        /// <summary>
        /// Check if file is an audio file
        /// </summary>
        public static bool IsAudioFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            string extension = Path.GetExtension(filePath);
            return AudioExtensions.Contains(extension);
        }

        /// <summary>
        /// Check if file is a document
        /// </summary>
        public static bool IsDocumentFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            string extension = Path.GetExtension(filePath);
            return DocumentExtensions.Contains(extension);
        }

        /// <summary>
        /// Check if file is supported
        /// </summary>
        public static bool IsSupportedFile(string filePath)
        {
            return IsImageFile(filePath) || IsVideoFile(filePath) ||
                   IsAudioFile(filePath) || IsDocumentFile(filePath);
        }

        /// <summary>
        /// Get file extension without the dot
        /// </summary>
        public static string GetFileExtension(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return string.Empty;

            string extension = Path.GetExtension(filePath);
            return extension.TrimStart('.');
        }

        /// <summary>
        /// Get file size in bytes
        /// </summary>
        public static long GetFileSize(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return 0;

            var fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }

        /// <summary>
        /// Format file size to human readable string
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Get all supported extensions as a comma-separated string
        /// </summary>
        public static string GetAllSupportedExtensions()
        {
            var allExtensions = ImageExtensions
                .Concat(VideoExtensions)
                .Concat(AudioExtensions)
                .Concat(DocumentExtensions)
                .OrderBy(x => x);

            return string.Join(", ", allExtensions);
        }
    }
}
