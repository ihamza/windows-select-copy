using System;
using System.IO;
using System.Text.Json;

namespace FileCopier
{
    public class AppSettings
    {
        public string RemoteFolder { get; set; } = @"\\server\share\folder";
        public bool CreateSubfolderByDate { get; set; } = true;

        private static readonly string SettingsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        public static AppSettings Load()
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }

        public void Save()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(SettingsPath, json);
        }
    }
}
