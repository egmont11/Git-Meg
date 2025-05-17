using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace GitMegalodon
{
    public class AppSettings
    {
        private static string SettingsFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GitMegalodon",
            "settings.json");

        public List<string> RecentRepositories { get; set; } = new List<string>();

        public static AppSettings Load()
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception)
            {
                // Fallback to default settings if file is corrupted
            }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(this);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception)
            {
                // Log error or show message if needed
            }
        }
    }
}