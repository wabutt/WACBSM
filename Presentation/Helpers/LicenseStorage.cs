using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
using Domain;

namespace Presentation.Helpers
{
    public static class LicenseStorage
    {
        private const string REGISTRY_KEY = @"Software\WAButt";
        private const string LICENSE_VALUE = "LicenseKey";
        private const string LICENSE_FILE = "license.dat";

        private static readonly byte[] AES_KEY = Encoding.UTF8.GetBytes("WAButt2024SecureKey123456789012");
        private static readonly byte[] AES_IV = Encoding.UTF8.GetBytes("WAButtIV12345678");

        #region Public Methods

        public static string LoadLicense()
        {
            UserModel user = new UserModel();
            string currentHWID = user.GetMachineGuid();

            // Prioridad 1: Archivo portable (validando HWID)
            string license = LoadFromFile(currentHWID);
            if (!string.IsNullOrWhiteSpace(license))
            {
                Console.WriteLine("✓ Licencia cargada desde archivo");
                SaveToRegistry(license, currentHWID);
                return license;
            }

            // Prioridad 2: Registry (validando HWID)
            license = LoadFromRegistry(currentHWID);
            if (!string.IsNullOrWhiteSpace(license))
            {
                Console.WriteLine("✓ Licencia cargada desde registro");
                SaveToFile(license, currentHWID);
                return license;
            }

            // Prioridad 3: Settings (legacy - migrar)
            license = Properties.Settings.Default.LicenseKey;
            if (!string.IsNullOrWhiteSpace(license))
            {
                Console.WriteLine("✓ Licencia cargada desde Settings (migrando...)");
                SaveToFile(license, currentHWID);
                SaveToRegistry(license, currentHWID);
                return license;
            }

            Console.WriteLine("⚠ No se encontró licencia guardada");
            return null;
        }

        public static void SaveLicense(string license)
        {
            if (string.IsNullOrWhiteSpace(license))
            {
                Console.WriteLine("⚠ Intento de guardar licencia vacía");
                return;
            }

            UserModel user = new UserModel();
            string currentHWID = user.GetMachineGuid();

            bool fileSuccess = SaveToFile(license, currentHWID);
            bool registrySuccess = SaveToRegistry(license, currentHWID);
            bool settingsSuccess = SaveToSettings(license);

            Console.WriteLine($"Licencia guardada: File={fileSuccess}, Registry={registrySuccess}, Settings={settingsSuccess}");
        }

        public static void ClearLicense()
        {
            DeleteFromFile();
            DeleteFromRegistry();
            DeleteFromSettings();
            Console.WriteLine("✓ Licencia eliminada de todas las ubicaciones");
        }

        #endregion

        #region File Operations

        private static string GetLicenseFilePath()
        {
            string exeDir = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
            return Path.Combine(exeDir, LICENSE_FILE);
        }

        /// <summary>
        /// Guarda licencia + HWID encriptados juntos
        /// </summary>
        private static bool SaveToFile(string license, string hwid)
        {
            try
            {
                // ✅ Formato: LICENCIA|HWID (encriptado todo junto)
                string dataToEncrypt = $"{license}|{hwid}";
                string encryptedData = EncryptAES(dataToEncrypt);
                string filePath = GetLicenseFilePath();

                File.WriteAllText(filePath, encryptedData, Encoding.UTF8);

                Console.WriteLine($"✓ Licencia vinculada a HWID guardada en: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error guardando licencia: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Carga licencia solo si el HWID coincide
        /// </summary>
        private static string LoadFromFile(string currentHWID)
        {
            try
            {
                string filePath = GetLicenseFilePath();

                if (!File.Exists(filePath))
                {
                    return null;
                }

                string encryptedContent = File.ReadAllText(filePath, Encoding.UTF8);

                if (string.IsNullOrWhiteSpace(encryptedContent))
                {
                    return null;
                }

                string decryptedData = DecryptAES(encryptedContent);

                // ✅ Validar formato: LICENCIA|HWID
                if (!decryptedData.Contains("|"))
                {
                    Console.WriteLine("⚠ Formato de licencia inválido");
                    return null;
                }

                string[] parts = decryptedData.Split('|');
                if (parts.Length != 2)
                {
                    Console.WriteLine("⚠ Datos de licencia corruptos");
                    return null;
                }

                string storedLicense = parts[0];
                string storedHWID = parts[1];

                // ✅ CRÍTICO: Validar que el HWID coincida
                if (storedHWID != currentHWID)
                {
                    Console.WriteLine("❌ HWID no coincide - Licencia no válida para esta máquina");
                    Console.WriteLine($"   Esperado: {currentHWID}");
                    Console.WriteLine($"   Encontrado: {storedHWID}");

                    // Eliminar archivo corrupto/crackeado
                    DeleteFromFile();

                    return null;
                }

                return storedLicense;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error cargando licencia: {ex.Message}");
                return null;
            }
        }

        private static void DeleteFromFile()
        {
            try
            {
                string filePath = GetLicenseFilePath();
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Console.WriteLine("✓ Archivo de licencia eliminado");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error eliminando archivo: {ex.Message}");
            }
        }

        #endregion

        #region Registry Operations

        /// <summary>
        /// Guarda licencia + HWID en registry
        /// </summary>
        private static bool SaveToRegistry(string license, string hwid)
        {
            try
            {
                string dataToEncrypt = $"{license}|{hwid}";
                string encryptedData = EncryptAES(dataToEncrypt);

                using (var key = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY))
                {
                    if (key != null)
                    {
                        key.SetValue(LICENSE_VALUE, encryptedData, RegistryValueKind.String);
                        Console.WriteLine("✓ Licencia vinculada a HWID guardada en registro");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error guardando en registro: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Carga licencia de registry solo si HWID coincide
        /// </summary>
        private static string LoadFromRegistry(string currentHWID)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY))
                {
                    if (key != null)
                    {
                        var value = key.GetValue(LICENSE_VALUE) as string;

                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            string decryptedData = DecryptAES(value);

                            if (!decryptedData.Contains("|"))
                            {
                                return null;
                            }

                            string[] parts = decryptedData.Split('|');
                            if (parts.Length != 2)
                            {
                                return null;
                            }

                            string storedLicense = parts[0];
                            string storedHWID = parts[1];

                            // ✅ Validar HWID
                            if (storedHWID != currentHWID)
                            {
                                Console.WriteLine("❌ HWID no coincide en registry");
                                DeleteFromRegistry();
                                return null;
                            }

                            return storedLicense;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error cargando desde registro: {ex.Message}");
                return null;
            }
        }

        private static void DeleteFromRegistry()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(LICENSE_VALUE, false);
                        Console.WriteLine("✓ Licencia eliminada del registro");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error eliminando del registro: {ex.Message}");
            }
        }

        #endregion

        #region Settings Operations

        private static bool SaveToSettings(string license)
        {
            try
            {
                Properties.Settings.Default.LicenseKey = license;
                Properties.Settings.Default.Save();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error guardando en Settings: {ex.Message}");
                return false;
            }
        }

        private static void DeleteFromSettings()
        {
            try
            {
                Properties.Settings.Default.LicenseKey = string.Empty;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error limpiando Settings: {ex.Message}");
            }
        }

        #endregion

        #region Encryption (AES-256)

        private static string EncryptAES(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = AES_KEY;
                    aes.IV = AES_IV;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }

                        byte[] encrypted = msEncrypt.ToArray();
                        return Convert.ToBase64String(encrypted);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error encriptando: {ex.Message}");
                return plainText;
            }
        }

        private static string DecryptAES(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                byte[] buffer = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = AES_KEY;
                    aes.IV = AES_IV;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(buffer))
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error desencriptando: {ex.Message}");
                return cipherText;
            }
        }

        #endregion
    }
}