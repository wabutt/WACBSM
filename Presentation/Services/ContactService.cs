using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using Presentation.Helpers;
using Presentation.Models;

namespace Presentation.Services
{
    /// <summary>
    /// Service for contact management (import/export/storage)
    /// </summary>
    public class ContactService
    {
        private const string CONTACTS_FILENAME = "Contacts.json";
        private const string CONTACTS2_FILENAME = "Contacts2.json";

        /// <summary>
        /// Save contacts to JSON file
        /// </summary>
        public void SaveContactsToJson(List<ContactModel> contacts, bool isSMS = false)
        {
            try
            {
                string filename = isSMS ? CONTACTS2_FILENAME : CONTACTS_FILENAME;
                string json = JsonConvert.SerializeObject(contacts, Formatting.Indented);
                FileHelper.WriteJsonToFile(json, filename);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving contacts: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Load contacts from JSON file
        /// </summary>
        public List<ContactModel> LoadContactsFromJson(bool isSMS = false)
        {
            try
            {
                string filename = isSMS ? CONTACTS2_FILENAME : CONTACTS_FILENAME;
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "tempfilesWAButt",
                    filename
                );

                if (File.Exists(path) && new FileInfo(path).Length > 23)
                {
                    string json = File.ReadAllText(path);
                    return JsonConvert.DeserializeObject<List<ContactModel>>(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading contacts: {ex.Message}");
            }

            return new List<ContactModel>();
        }

        /// <summary>
        /// Remove duplicate contacts
        /// </summary>
        public List<ContactModel> RemoveDuplicates(List<ContactModel> contacts)
        {
            if (contacts == null || contacts.Count == 0)
                return contacts;

            // Remove duplicates by phone number
            var uniqueContacts = contacts
                .Where(c => !string.IsNullOrWhiteSpace(c.PhoneNumber))
                .GroupBy(c => c.PhoneNumber)
                .Select(g => g.First())
                .ToList();

            return uniqueContacts;
        }

        /// <summary>
        /// Remove empty contacts
        /// </summary>
        public List<ContactModel> RemoveEmptyContacts(List<ContactModel> contacts)
        {
            if (contacts == null)
                return new List<ContactModel>();

            return contacts.Where(c => c.IsValid).ToList();
        }

        /// <summary>
        /// Export contacts to text file (TAB-separated format matching WAButt format)
        /// </summary>
        public void ExportToTextFile(List<ContactModel> contacts, string filePath)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(filePath))
                {
                    for (int i = 0; i < contacts.Count; i++)
                    {
                        var contact = contacts[i];

                        if (!contact.IsValid)
                            continue;

                        if (i > 0)
                            sw.WriteLine();

                        string phone = contact.PhoneNumber;

                        // Add + prefix if phone is valid and doesn't have it
                        if (phone.Replace(" ", "").Length > 9 &&
                            !phone.StartsWith("+") &&
                            ValidationHelper.IsDigitsOnly(phone))
                        {
                            phone = "+" + phone;
                        }

                        // TAB-separated: Phone\tName
                        sw.Write(phone);
                        sw.Write("\t");
                        sw.Write(contact.Name ?? string.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting contacts: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Import contacts from text file (TAB-separated format matching WAButt format)
        /// </summary>
        public List<ContactModel> ImportFromTextFile(string filePath)
        {
            var contacts = new List<ContactModel>();

            try
            {
                if (!File.Exists(filePath))
                    return contacts;

                using (StreamReader sr = new StreamReader(filePath))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();

                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        string[] parts = line.Split('\t');

                        if (parts.Length >= 1)
                        {
                            string phone = parts[0].Trim();
                            string name = parts.Length > 1 ? parts[1].Trim() : string.Empty;

                            if (!string.IsNullOrWhiteSpace(phone))
                            {
                                contacts.Add(new ContactModel(phone, name));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing contacts: {ex.Message}");
                throw;
            }

            return contacts;
        }

        /// <summary>
        /// Convert DataGridView to contact list
        /// </summary>
        public List<ContactModel> ConvertFromDataGridView(DataGridView dgv)
        {
            var contacts = new List<ContactModel>();

            if (dgv == null || dgv.Rows.Count == 0)
                return contacts;

            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow)
                    continue;

                string phone = row.Cells[0].Value?.ToString() ?? string.Empty;
                string name = row.Cells[1].Value?.ToString() ?? string.Empty;
                string status = row.Cells[2].Value?.ToString() ?? null;

                if (!string.IsNullOrWhiteSpace(phone))
                {
                    contacts.Add(new ContactModel
                    {
                        PhoneNumber = phone,
                        Name = name,
                        Status = status
                    });
                }
            }

            return contacts;
        }

        /// <summary>
        /// Load contacts to DataGridView
        /// </summary>
        public void LoadToDataGridView(DataGridView dgv, List<ContactModel> contacts)
        {
            if (dgv == null || contacts == null)
                return;

            dgv.Rows.Clear();

            foreach (var contact in contacts)
            {
                dgv.Rows.Add(contact.PhoneNumber, contact.Name, contact.Status);
            }
        }
    }
}
