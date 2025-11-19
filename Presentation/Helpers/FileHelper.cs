using System;
using System.IO;
using System.Linq;

namespace Presentation.Helpers
{
    /// <summary>
    /// Helper class for file operations
    /// </summary>
    public static class FileHelper
    {
        private static readonly string[] ImageExtensions = { ".tif", ".pjp", ".xbm", ".jxl", ".svgz", ".jpg", ".jpeg", ".ico", ".tiff", ".gif", ".svg", ".jfif", ".webp", ".png", ".bmp", ".pjpeg", ".avif" };
        private static readonly string[] VideoExtensions = { ".m4v", ".mp4", ".3gpp", ".mov" };
        private static readonly string[] AudioExtensions = { ".mp3", ".wav", ".ogg", ".m4a", ".aac", ".opus" };
        private static readonly string[] DocumentExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".zip", ".rar" };

        /// <summary>
        /// Get file extension from path
        /// </summary>
        public static string GetExtension(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return string.Empty;

            return Path.GetExtension(filePath).ToLower();
        }

        /// <summary>
        /// Check if file is an image
        /// </summary>
        public static bool IsImageFile(string filePath)
        {
            string ext = GetExtension(filePath);
            return ImageExtensions.Contains(ext);
        }

        /// <summary>
        /// Check if file is a video
        /// </summary>
        public static bool IsVideoFile(string filePath)
        {
            string ext = GetExtension(filePath);
            return VideoExtensions.Contains(ext);
        }

        /// <summary>
        /// Check if file is audio
        /// </summary>
        public static bool IsAudioFile(string filePath)
        {
            string ext = GetExtension(filePath);
            return AudioExtensions.Contains(ext);
        }

        /// <summary>
        /// Check if file is a document
        /// </summary>
        public static bool IsDocumentFile(string filePath)
        {
            string ext = GetExtension(filePath);
            return DocumentExtensions.Contains(ext);
        }

        /// <summary>
        /// Determine attachment type from file path
        /// </summary>
        public static string DetermineAttachmentType(string filePath)
        {
            if (IsImageFile(filePath) || IsVideoFile(filePath))
                return "I"; // Image/Video
            if (IsAudioFile(filePath))
                return "A"; // Audio
            if (IsDocumentFile(filePath))
                return "D"; // Document

            return "D"; // Default to document
        }

        /// <summary>
        /// Recursively copy directory
        /// </summary>
        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist: {sourceDirName}");
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create destination directory if it doesn't exist
            Directory.CreateDirectory(destDirName);

            // Copy files
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, true);
            }

            // Copy subdirectories
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        /// <summary>
        /// Write JSON string to file
        /// </summary>
        public static void WriteJsonToFile(string jsonData, string filename, string folderName = "tempfilesWAButt")
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                folderName
            );

            Directory.CreateDirectory(path);
            string fullPath = Path.Combine(path, filename);
            File.WriteAllText(fullPath, jsonData);
        }

        /// <summary>
        /// Check if file exists and has content
        /// </summary>
        public static bool FileExistsWithContent(string filePath, long minSize = 0)
        {
            if (!File.Exists(filePath))
                return false;

            FileInfo fileInfo = new FileInfo(filePath);
            return fileInfo.Length > minSize;
        }

        /// <summary>
        /// Get safe file name (remove invalid characters)
        /// </summary>
        public static string GetSafeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "file";

            char[] invalidChars = Path.GetInvalidFileNameChars();
            string safe = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());

            return string.IsNullOrWhiteSpace(safe) ? "file" : safe;
        }
    }
}
