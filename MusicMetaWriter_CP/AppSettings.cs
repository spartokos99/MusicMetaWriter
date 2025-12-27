using System;
using System.IO;
using System.Text.Json;

namespace MusicMetaWriter
{
    public class AppSettings
    {
        public bool export_mp3 { get; set; } = true;
        public bool export_wav { get; set; } = true;
        public bool export_flac { get; set; } = true;
        public bool export_aiff { get; set; } = true;

        public bool use_ln { get; set; } = false;
        public double ln_target_i { get; set; } = -16;
        public double ln_target_tpeak { get; set; } = -1.5;
        public double ln_target_lu { get; set; } = 11;

        public bool cr_subdirectory { get; set; } = true;
        public bool keep_filename { get; set; } = false;
        public string? fn_pattern { get; set; } = "%number% - %artists% - %album% - %title%";

        public string[]? hidden_columns { get; set; }

        private static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BashCode", "MusicMetaWriter", "settings.json");

        public static AppSettings Load()
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            return new AppSettings();
        }

        public void Save()
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
    }
}
