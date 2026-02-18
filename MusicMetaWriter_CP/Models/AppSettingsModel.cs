using MusicMetaWriter.Enums;
using MusicMetaWriter_CP.Models;
using System;
using System.IO;
using System.Text.Json;

namespace MusicMetaWriter.Models
{
    public class AppSettingsModel
    {
        #region App Settings
        public bool export_mp4 { get; set; } = true;
        public bool export_mp3 { get; set; } = true;
        public bool export_wav { get; set; } = true;
        public bool export_flac { get; set; } = true;
        public bool export_aiff { get; set; } = true;

        public int video_method { get; set; } = 0;
        public string? video_path { get; set; } = "";

        public bool use_ln { get; set; } = false;
        public string? ln_method { get; set; } = "loudnorm";
        public double ln_target_i { get; set; } = -16;
        public double ln_target_tpeak { get; set; } = -1.5;
        public double ln_target_lu { get; set; } = 11;

        public bool cr_subdirectory { get; set; } = true;
        public bool convertBit { get; set; } = false;
        public int convertToBit { get; set; } = 24;
        public bool dithering { get; set; } = false;
        public bool force44100 { get; set; } = false;

        public bool keep_filename { get; set; } = false;
        public string? fn_pattern { get; set; } = "%number% - %artists% - %album% - %title%";

        public string[]? hidden_columns { get; set; }
        #endregion

        #region Advanced Settings
        public bool use_better_cover { get; set; } = true;
        public ThemeEnum use_theme { get; set; } = ThemeEnum.System;
        public bool search_subdirectories { get; set; } = false;
        public int reduce_size { get; set; } = 3;
        #endregion
        public static string SettingsDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BashCode", "MusicMetaWriter");
        
        public static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

        public static AppSettingsModel Load()
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize(json, AppSettingsModelJsonContext.Default.AppSettingsModel) ?? new AppSettingsModel();
            }
            return new AppSettingsModel();
        }

        public void Save(AppSettingsType type = AppSettingsType.Both)
        {
            AppSettingsModel current;
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                current = JsonSerializer.Deserialize(json, AppSettingsModelJsonContext.Default.AppSettingsModel) ?? new AppSettingsModel();
            } else
            {
                current = new AppSettingsModel();
            }

            if (type == AppSettingsType.Default || type == AppSettingsType.Both)
            {
                current.export_mp4 = this.export_mp4;
                current.export_mp3 = this.export_mp3;
                current.export_wav = this.export_wav;
                current.export_flac = this.export_flac;
                current.export_aiff = this.export_aiff;
                current.video_method = this.video_method;
                current.video_path = this.video_path;
                current.use_ln = this.use_ln;
                current.ln_method = this.ln_method;
                current.ln_target_i = this.ln_target_i;
                current.ln_target_tpeak = this.ln_target_tpeak;
                current.ln_target_lu = this.ln_target_lu;
                current.cr_subdirectory = this.cr_subdirectory;
                current.convertBit = this.convertBit;
                current.convertToBit = this.convertToBit;
                current.dithering = this.dithering;
                current.keep_filename = this.keep_filename;
                current.fn_pattern = this.fn_pattern;
                current.hidden_columns = this.hidden_columns;
            }

            if (type == AppSettingsType.Advanced || type == AppSettingsType.Both)
            {
                current.use_better_cover = this.use_better_cover;
                current.use_theme = this.use_theme;
                current.search_subdirectories = this.search_subdirectories;
                current.reduce_size = this.reduce_size;
            }

            var dir = Path.GetDirectoryName(SettingsPath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var jsonOut = JsonSerializer.Serialize(this, AppSettingsModelJsonContext.Default.AppSettingsModel);
            File.WriteAllText(SettingsPath, jsonOut);
        }
    }
}
