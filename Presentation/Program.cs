using System;
using System.Windows.Forms;
using Presentation.Helpers;
using Presentation.Services;
using AutoUpdaterDotNET;
using System.Drawing;
using Domain;

namespace Presentation
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // ✅ VALIDAR LICENCIA ANTES DE CREAR FORMULARIO
            if (!ValidateLicenseBeforeForm())
            {
                return; // Sale sin crear el form
            }

            // ✅ Solo si tiene licencia, crear el formulario
            Application.Run(new WAButtfrm());
        }

        static bool ValidateLicenseBeforeForm()
        {
            // Verificar internet
            if (!InternetHelper.CheckForInternetConnection())
            {
                MessageBox.Show("No cuenta con acceso a internet, le recomendamos intentar mas tarde.",
                    "Observación", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // 🔒 WALL #1: Validar licencia PRIMERO
            string licenseKey = LicenseStorage.LoadLicense();

            int attempts = 0;
            const int maxAttempts = 3;

            while (attempts < maxAttempts)
            {
                attempts++;

                if (string.IsNullOrWhiteSpace(licenseKey))
                {
                    licenseKey = PromptForLicense();
                }

                if (string.IsNullOrWhiteSpace(licenseKey))
                {
                    MessageBox.Show("No se ingresó ninguna licencia. La aplicación se cerrará.",
                        "Licencia requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                // Validar contra servidor
                var licenseService = new LicenseService();
                var task = licenseService.ValidateAPIKeyAsync(licenseKey);
                task.Wait();
                var result = task.Result;

                if (result == null)
                {
                    return false;
                }

                if (result.IsValid)
                {
                    UserModel user = new UserModel();
                    Console.WriteLine("✓ Licencia válida para HWID: " + user.GetMachineGuid());

                    // Guardar licencia válida
                    LicenseStorage.SaveLicense(licenseKey);

                    // 🔒 WALL #2: Verificar actualizaciones (solo si licencia válida)
                    CheckForUpdates();

                    return true;
                }

                // Licencia no válida
                string msg = result.Message ?? "Licencia no válida.";

                var userChoice = MessageBox.Show(
                    "La licencia configurada ya no es válida:\n\n" +
                    msg +
                    "\n\n¿Desea ingresar otra licencia ahora?",
                    "Licencia no válida",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (userChoice == DialogResult.Yes)
                {
                    licenseKey = null;
                    LicenseStorage.ClearLicense();
                    continue;
                }
                else
                {
                    return false;
                }
            }

            MessageBox.Show(
                "Se excedió el número de intentos de activación. La aplicación se cerrará.",
                "Error de licencia",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );

            return false;
        }

        static void CheckForUpdates()
        {
            try
            {
                AutoUpdater.InstalledVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                AutoUpdater.Mandatory = true;
                AutoUpdater.UpdateMode = Mode.Forced;
                AutoUpdater.ShowSkipButton = false;
                AutoUpdater.ShowRemindLaterButton = false;
                AutoUpdater.Synchronous = true;

                string downloadPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "WAButt", "Updates"
                );
                System.IO.Directory.CreateDirectory(downloadPath);
                AutoUpdater.DownloadPath = downloadPath;

                AutoUpdater.AppTitle = "WAButt";
                AutoUpdater.RunUpdateAsAdmin = false;

                AutoUpdater.Start("https://raw.githubusercontent.com/wabutt/itsmevsauce/master/AutoUpdater.xml");

                Console.WriteLine("✓ Update check completado");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Update error: {ex.Message}");
            }
        }

        static string PromptForLicense(string initialValue = "")
        {
            using (var form = new Form())
            using (var lblTitle = new Label())
            using (var lblHint = new Label())
            using (var textBox = new TextBox())
            using (var buttonOk = new Button())
            using (var buttonCancel = new Button())
            {
                form.Text = "Activar licencia";
                form.StartPosition = FormStartPosition.CenterScreen;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ClientSize = new Size(420, 160);
                form.Font = new Font("Segoe UI", 9F);

                // Título
                lblTitle.Text = "Ingrese su código de licencia:";
                lblTitle.AutoSize = true;
                lblTitle.Location = new Point(12, 15);

                // Hint / ayuda
                lblHint.Text = "Ejemplo: XXXX-XXXX-XXXX-XXXX";
                lblHint.AutoSize = true;
                lblHint.ForeColor = Color.DimGray;
                lblHint.Location = new Point(12, 35);

                // TextBox para la licencia
                textBox.SetBounds(12, 55, 396, 23);
                textBox.Text = initialValue ?? string.Empty;
                textBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                textBox.CharacterCasing = CharacterCasing.Upper;

                // Botón OK
                buttonOk.Text = "Aceptar";
                buttonOk.DialogResult = DialogResult.OK;
                buttonOk.SetBounds(242, 105, 80, 27);
                buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

                // Botón Cancelar
                buttonCancel.Text = "Cancelar";
                buttonCancel.DialogResult = DialogResult.Cancel;
                buttonCancel.SetBounds(328, 105, 80, 27);
                buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

                form.AcceptButton = buttonOk;
                form.CancelButton = buttonCancel;

                form.Controls.AddRange(new Control[]
                {
                    lblTitle,
                    lblHint,
                    textBox,
                    buttonOk,
                    buttonCancel
                });

                // Cuando se muestra el form, enfocar y seleccionar todo
                form.Shown += (s, e) =>
                {
                    textBox.Focus();
                    textBox.SelectAll();
                };

                var dialogResult = form.ShowDialog();

                if (dialogResult == DialogResult.OK)
                {
                    var value = (textBox.Text ?? string.Empty).Trim();
                    return string.IsNullOrEmpty(value) ? null : value;
                }

                return null;
            }
        }
    }
}