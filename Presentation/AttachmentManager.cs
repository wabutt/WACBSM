using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Presentation.Helpers;
using Presentation.Services;

namespace Presentation
{
    /// <summary>
    /// Form for managing multiple image attachments with preview
    /// </summary>
    public partial class AttachmentManager : Form
    {
        // Service for attachment validation
        private readonly AttachmentService _attachmentService;

        // List of selected attachment paths
        private List<string> _attachmentPaths;

        // Random instance for testing preview
        private static readonly Random _random = new Random();

        /// <summary>
        /// Public property to return selected attachments
        /// </summary>
        public List<string> AttachmentPaths
        {
            get { return _attachmentPaths; }
        }

        /// <summary>
        /// Constructor with optional existing list
        /// </summary>
        public AttachmentManager(List<string> existingPaths = null)
        {
            InitializeComponent();
            _attachmentService = new AttachmentService();
            _attachmentPaths = existingPaths != null ? new List<string>(existingPaths) : new List<string>();

            InitializeDataGridView();
            LoadExistingAttachments();
        }

        /// <summary>
        /// Initialize DataGridView columns
        /// </summary>
        private void InitializeDataGridView()
        {
            attachmentsDgv.AutoGenerateColumns = false;
            attachmentsDgv.AllowUserToAddRows = false;
            attachmentsDgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            attachmentsDgv.MultiSelect = true;

            // Preview column (Image)
            DataGridViewImageColumn previewCol = new DataGridViewImageColumn
            {
                Name = "Preview",
                HeaderText = "Vista Previa",
                Width = 80,
                ImageLayout = DataGridViewImageCellLayout.Zoom
            };
            attachmentsDgv.Columns.Add(previewCol);

            // FileName column
            DataGridViewTextBoxColumn fileNameCol = new DataGridViewTextBoxColumn
            {
                Name = "FileName",
                HeaderText = "Nombre de Archivo",
                Width = 200,
                ReadOnly = true
            };
            attachmentsDgv.Columns.Add(fileNameCol);

            // FilePath column (hidden for full path storage)
            DataGridViewTextBoxColumn filePathCol = new DataGridViewTextBoxColumn
            {
                Name = "FilePath",
                HeaderText = "Ruta Completa",
                Width = 150,
                ReadOnly = true,
                Visible = false
            };
            attachmentsDgv.Columns.Add(filePathCol);

            // Size column
            DataGridViewTextBoxColumn sizeCol = new DataGridViewTextBoxColumn
            {
                Name = "Size",
                HeaderText = "Tamaño",
                Width = 100,
                ReadOnly = true
            };
            attachmentsDgv.Columns.Add(sizeCol);

            // Delete button column
            DataGridViewButtonColumn deleteCol = new DataGridViewButtonColumn
            {
                Name = "Delete",
                HeaderText = "Eliminar",
                Width = 80,
                Text = "Eliminar",
                UseColumnTextForButtonValue = true
            };
            attachmentsDgv.Columns.Add(deleteCol);

            // Set row height for thumbnails
            attachmentsDgv.RowTemplate.Height = 70;

            // Handle button click
            attachmentsDgv.CellContentClick += AttachmentsDgv_CellContentClick;
        }

        /// <summary>
        /// Load existing attachments into grid
        /// </summary>
        private void LoadExistingAttachments()
        {
            if (_attachmentPaths == null || _attachmentPaths.Count == 0)
                return;

            foreach (string path in _attachmentPaths)
            {
                AddAttachmentToGrid(path);
            }

            UpdateCountLabel();
        }

        /// <summary>
        /// Add Attachments button click handler
        /// </summary>
        private void addAttachmentsBtn_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Archivos de Imagen y Video " +
                    "(*.tif;*.pjp;*.xbm;*.jxl;*.svgz;*.jpg;*.jpeg;*.ico;*.tiff;*.gif;*.svg;*.jfif;*.webp;*.png;*.bmp;*.pjpeg;*.avif;*.m4v;*.mp4;*.3gpp;*.mov) | " +
                    "*.tif;*.pjp;*.xbm;*.jxl;*.svgz;*.jpg;*.jpeg;*.ico;*.tiff;*.gif;*.svg;*.jfif;*.webp;*.png;*.bmp;*.pjpeg;*.avif;*.m4v;*.mp4;*.3gpp;*.mov";
                ofd.Multiselect = true;
                ofd.Title = "Seleccionar Imágenes/Videos";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    int addedCount = 0;
                    int rejectedCount = 0;
                    List<string> errors = new List<string>();

                    foreach (string filePath in ofd.FileNames)
                    {
                        // Validate file
                        string errorMessage;
                        if (!_attachmentService.ValidateAttachment(filePath, out errorMessage))
                        {
                            rejectedCount++;
                            errors.Add($"{Path.GetFileName(filePath)}: {errorMessage}");
                            continue;
                        }

                        // Check if already added
                        if (_attachmentPaths.Contains(filePath))
                        {
                            rejectedCount++;
                            errors.Add($"{Path.GetFileName(filePath)}: Ya agregado");
                            continue;
                        }

                        // Validate it's an image or video
                        if (!FileHelper.IsImageFile(filePath) && !FileHelper.IsVideoFile(filePath))
                        {
                            rejectedCount++;
                            errors.Add($"{Path.GetFileName(filePath)}: No es imagen o video");
                            continue;
                        }

                        // Add to list and grid
                        _attachmentPaths.Add(filePath);
                        AddAttachmentToGrid(filePath);
                        addedCount++;
                    }

                    UpdateCountLabel();

                    // Show summary
                    string message = $"Agregados: {addedCount}";
                    if (rejectedCount > 0)
                    {
                        message += $"\nRechazados: {rejectedCount}";
                        if (errors.Count <= 5)
                        {
                            message += "\n\nErrores:\n" + string.Join("\n", errors);
                        }
                    }

                    MessageBox.Show(message, "Resultado", MessageBoxButtons.OK,
                        rejectedCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                }
            }
        }

        /// <summary>
        /// Add attachment to DataGridView
        /// </summary>
        private void AddAttachmentToGrid(string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);

                // Generate thumbnail
                Image thumbnail = GenerateThumbnail(filePath);

                // Format file size
                string sizeText = FormatFileSize(fileInfo.Length);

                // Add row
                int rowIndex = attachmentsDgv.Rows.Add();
                DataGridViewRow row = attachmentsDgv.Rows[rowIndex];

                row.Cells["Preview"].Value = thumbnail;
                row.Cells["FileName"].Value = fileInfo.Name;
                row.Cells["FilePath"].Value = filePath;
                row.Cells["Size"].Value = sizeText;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error agregando {Path.GetFileName(filePath)}: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Generate thumbnail for image/video
        /// </summary>
        private Image GenerateThumbnail(string filePath)
        {
            try
            {
                if (FileHelper.IsImageFile(filePath))
                {
                    // Load image and create thumbnail
                    using (Image originalImage = Image.FromFile(filePath))
                    {
                        return originalImage.GetThumbnailImage(64, 64, null, IntPtr.Zero);
                    }
                }
                else if (FileHelper.IsVideoFile(filePath))
                {
                    // For video, return a placeholder icon
                    Bitmap placeholder = new Bitmap(64, 64);
                    using (Graphics g = Graphics.FromImage(placeholder))
                    {
                        g.Clear(Color.LightGray);
                        using (Font font = new Font("Arial", 10, FontStyle.Bold))
                        {
                            g.DrawString("VIDEO", font, Brushes.Black, new PointF(5, 25));
                        }
                    }
                    return placeholder;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating thumbnail: {ex.Message}");
            }

            // Fallback placeholder
            Bitmap fallback = new Bitmap(64, 64);
            using (Graphics g = Graphics.FromImage(fallback))
            {
                g.Clear(Color.Gray);
                using (Font font = new Font("Arial", 8))
                {
                    g.DrawString("Error", font, Brushes.White, new PointF(10, 25));
                }
            }
            return fallback;
        }

        /// <summary>
        /// Format file size to human-readable format
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
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
        /// Handle delete button click in grid
        /// </summary>
        private void AttachmentsDgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (attachmentsDgv.Columns[e.ColumnIndex].Name == "Delete")
            {
                string filePath = attachmentsDgv.Rows[e.RowIndex].Cells["FilePath"].Value.ToString();

                // Remove from list
                _attachmentPaths.Remove(filePath);

                // Remove from grid
                attachmentsDgv.Rows.RemoveAt(e.RowIndex);

                UpdateCountLabel();
            }
        }

        /// <summary>
        /// Delete Selected button click handler
        /// </summary>
        private void deleteSelectedBtn_Click(object sender, EventArgs e)
        {
            if (attachmentsDgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione al menos una fila para eliminar",
                    "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"¿Eliminar {attachmentsDgv.SelectedRows.Count} archivo(s) seleccionado(s)?",
                "Confirmar eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Remove in reverse order to avoid index issues
                for (int i = attachmentsDgv.SelectedRows.Count - 1; i >= 0; i--)
                {
                    DataGridViewRow row = attachmentsDgv.SelectedRows[i];
                    string filePath = row.Cells["FilePath"].Value.ToString();

                    _attachmentPaths.Remove(filePath);
                    attachmentsDgv.Rows.Remove(row);
                }

                UpdateCountLabel();
            }
        }

        /// <summary>
        /// Clear All button click handler
        /// </summary>
        private void clearAllBtn_Click(object sender, EventArgs e)
        {
            if (_attachmentPaths.Count == 0)
            {
                MessageBox.Show("No hay archivos para limpiar",
                    "Observación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"¿Eliminar todos los {_attachmentPaths.Count} archivo(s)?",
                "Confirmar eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                _attachmentPaths.Clear();
                attachmentsDgv.Rows.Clear();
                UpdateCountLabel();
            }
        }

        /// <summary>
        /// Save button click handler
        /// </summary>
        private void saveBtn_Click(object sender, EventArgs e)
        {
            if (_attachmentPaths.Count == 0)
            {
                MessageBox.Show("Agregue al menos una imagen antes de guardar",
                    "Observación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validate all files still exist
            List<string> missingFiles = new List<string>();
            foreach (string path in _attachmentPaths.ToList())
            {
                if (!File.Exists(path))
                {
                    missingFiles.Add(Path.GetFileName(path));
                    _attachmentPaths.Remove(path);
                }
            }

            if (missingFiles.Count > 0)
            {
                string message = $"Los siguientes archivos ya no existen y fueron removidos:\n" +
                    string.Join("\n", missingFiles);
                MessageBox.Show(message, "Archivos faltantes",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);

                if (_attachmentPaths.Count == 0)
                {
                    return;
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>
        /// Cancel button click handler
        /// </summary>
        private void cancelBtn_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        /// <summary>
        /// Update count label
        /// </summary>
        private void UpdateCountLabel()
        {
            countLabel.Text = $"Total: {_attachmentPaths.Count} archivo(s)";
        }
    }
}
