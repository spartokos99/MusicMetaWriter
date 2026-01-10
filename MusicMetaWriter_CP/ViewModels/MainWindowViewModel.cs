using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Notification;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using MusicMetaWriter.Enums;
using MusicMetaWriter.Models;
using MusicMetaWriter_CP.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Icon = MsBox.Avalonia.Enums.Icon;

#pragma warning disable CS0618
namespace MusicMetaWriter_CP.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        #region Variables
        public static MainWindowViewModel? Instance { get; private set; }

        public DataGrid? MainDataGrid { get; set; }

        public AppSettingsModel? localSettings { get; set; }

        public INotificationMessageManager NotificationManager { get; } = new NotificationMessageManager();
        public int defaultNotificationTimeSpan = 5;
        public string defaultNotificationBackground = "#333";
        public string defaultNotificationAccent = "#1751C3";

        private static readonly HashSet<string> supportedExtensions = new() { ".mp3", ".wav", ".flac", ".aiff", ".m4a", ".ogg" };

        public Dictionary<string, Bitmap?> newCoverList = new Dictionary<string, Bitmap?>();

        public Window? mainWindow;

        static string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        static string logFilePath = Path.Combine(logDir, "log.txt");
        public static string ffmpegPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg").ToString();

        public bool IsLoudnormSelected => Ln_method == "loudnorm";
        #endregion

        #region ObservableProperties
        [ObservableProperty] private ObservableCollection<TrackModel> _tracks = new();
        [ObservableProperty] private ObservableCollection<TrackModel> _backup = new();

        [ObservableProperty] private ObservableCollection<int> _convertToBitItems = new ObservableCollection<int> { 24, 16 };
        [ObservableProperty] private ObservableCollection<string> _lnMethods = new ObservableCollection<string> { "loudnorm", "replaygain" };

        [ObservableProperty] private bool ffmpegFound = false;

        [ObservableProperty] private double _loadProgress;
        [ObservableProperty] private string _loadStatus = "";
        [ObservableProperty] private bool _showProgress;
        [ObservableProperty] private bool _isLoading;

        [ObservableProperty] private bool export_mp3;
        [ObservableProperty] private bool export_wav;
        [ObservableProperty] private bool export_flac;
        [ObservableProperty] private bool export_aiff;

        [ObservableProperty] private bool use_ln;
        [ObservableProperty] private string? ln_method;
        [ObservableProperty] private double ln_target_i;
        [ObservableProperty] private double ln_target_tpeak;
        [ObservableProperty] private double ln_target_lu;

        [ObservableProperty] private bool cr_subdirectory;
        [ObservableProperty] private bool convertBit;
        [ObservableProperty] private int _convertToBit;

        [ObservableProperty] private bool keep_filename;
        [ObservableProperty] private string? fn_pattern;

        [ObservableProperty] private ObservableCollection<TrackModel> _selectedTracks = new();
        [ObservableProperty] private Bitmap? selectedImage;

        [ObservableProperty] private bool btn_replace_enabled = false;
        [ObservableProperty] private bool btn_remove_enabled = false;
        [ObservableProperty] private bool btn_analyze_enabled = false;
        #endregion

        #region Helper Functions
        public void PrepareLogs()
        {
            try
            {
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                if (!File.Exists(logFilePath))
                {
                    File.Create(logFilePath);
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void WriteLog(string message, LogLevel level = LogLevel.Info)
        {
            try
            {
                if (!File.Exists(logFilePath))
                {
                    PrepareLogs();
                }

                string body = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [" + level.ToString().ToUpper() + "] " + message + Environment.NewLine;
                File.AppendAllText(logFilePath, body);
            } catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void LoadSettings()
        {
            if (localSettings is not null && MainDataGrid is not null)
            {
                Export_mp3 = localSettings.export_mp3;
                Export_wav = localSettings.export_wav;
                Export_flac = localSettings.export_flac;
                Export_aiff = localSettings.export_aiff;

                Use_ln = localSettings.use_ln;
                Ln_method = localSettings.ln_method;
                Ln_target_i = localSettings.ln_target_i;
                Ln_target_tpeak = localSettings.ln_target_tpeak;
                Ln_target_lu = localSettings.ln_target_lu;

                Cr_subdirectory = localSettings.cr_subdirectory;
                ConvertBit = localSettings.convertBit;
                ConvertToBit = localSettings.convertToBit;

                Keep_filename = localSettings.keep_filename;
                Fn_pattern = localSettings.fn_pattern;

                foreach (var col in MainDataGrid.Columns)
                {
                    if (localSettings.hidden_columns is not null && localSettings.hidden_columns.Contains(col.Header?.ToString()?.ToLower().Replace(" ", "_")))
                    {
                        col.IsVisible = false;
                    }
                }

                SetTheme();

                WriteLog("Settings loaded.");
            }
        }

        private string[] GetSelectedFormats()
        {
            var list = new List<string>();
            if (Export_mp3) list.Add("mp3");
            if (Export_wav) list.Add("wav");
            if (Export_flac) list.Add("flac");
            if (Export_aiff) list.Add("aiff");

            return list.ToArray();
        }

        public void SetTheme()
        {
            string theme = localSettings.use_theme == ThemeEnum.Dark ? "dark" : (localSettings.use_theme == ThemeEnum.Light ? "light" : "default");
            switch (theme.ToLower())
            {
                case "light":
                    Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
                    break;
                case "dark":
                    Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;
                    break;
                case "system":
                default:
                    Application.Current!.RequestedThemeVariant = ThemeVariant.Default;
                    break;
            }
        }

        private bool ImagesAreEqual(Bitmap? bmp1, Bitmap? bmp2)
        {
            if (ReferenceEquals(bmp1, bmp2)) return true;
            if (bmp1 is null || bmp2 is null) return false;
            if (bmp1.PixelSize != bmp2.PixelSize || bmp1.Format != bmp2.Format) return false;

            using var ms1 = new MemoryStream();
            using var ms2 = new MemoryStream();

            bmp1.Save(ms1);
            bmp2.Save(ms2);

            return ms1.ToArray().SequenceEqual(ms2.ToArray());
        }

        public void UpdateCoverPreview()
        {
            if (SelectedTracks.Count == 0)
            {
                SelectedImage = null;
                return;
            }

            if (SelectedTracks.Count == 1)
            {
                SelectedImage = SelectedTracks[0].EffectiveCoverImage;
                return;
            }

            if (localSettings is not null && localSettings.use_better_cover)
            {
                var firstCover = SelectedTracks[0].EffectiveCoverImage;
                bool allSame = true;
                for (int i = 1; i < SelectedTracks.Count; i++)
                {
                    if (!ImagesAreEqual(firstCover, SelectedTracks[i].EffectiveCoverImage))
                    {
                        allSame = false;
                        break;
                    }
                }

                SelectedImage = allSame ? firstCover : null;
                return;
            } else
            {
                SelectedImage = null;
            }
        }

        private string[] GetHiddenColumns()
        {
            if (MainDataGrid == null) return [];
            return MainDataGrid.Columns.Where(col => !col.IsVisible).Select(col => col.Header?.ToString()?.ToLower().Replace(" ", "_") ?? "").ToArray();
        }

        public void ShowNotification(string text, int delay, string badge = "Info", NotificationType type = NotificationType.Information, bool animated = true)
        {
            this.NotificationManager.CreateMessage()
                    .Accent(defaultNotificationAccent)
                    .Animates(animated)
                    .Background(defaultNotificationBackground)
                    .HasBadge(badge)
                    .HasType(type)
                    .HasMessage(text)
                    .Dismiss().WithDelay(TimeSpan.FromSeconds(delay))
                    .Queue();
        }

        private static void ExtractTarXz(string archive, string target)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "tar",
                Arguments = $"-xJf \"{archive}\" -C \"{target}\"",
                CreateNoWindow = true
            })!.WaitForExit();
        }

        private static void MakeExecutable(string path)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = $"+x \"{path}\"",
                CreateNoWindow = true
            })!.WaitForExit();
        }

        private static string GetDownloadUrl()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "https://evermeet.cx/ffmpeg/getrelease/zip";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-amd64-static.tar.xz";

            throw new PlatformNotSupportedException();
        }

        public void CheckFFMPEG()
        {
            // system-wide
            if (File.Exists(ffmpegPath))
            {
                IsLoading = false;
                LoadStatus = "";
                FfmpegFound = true;
                WriteLog("Ffmpeg found.");
                return;
            }

            WriteLog("Ffmpeg not found!", LogLevel.Warn);
            IsLoading = true;
            LoadStatus = "ffmpeg not found!";
            FfmpegFound = false;
        }
        #endregion

        #region Tasks
        private async Task GenerateTrackModelWithProgress(string[] paths, IProgress<double> progress)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Tracks.Clear();
                Backup.Clear();
            });

            bool useOgCover = localSettings!.reduce_size == 0;
            int coverSize = localSettings!.reduce_size == 1 ? 300 : (localSettings!.reduce_size == 2 ? 200 : 100);

            for (int i = 0; i < paths.Length; i++)
            {
                var storageFile = paths[i];
                LoadStatus = "Loading files... (" + i + " / " + paths.Length + ")";

                progress.Report((i + 1) / (double)paths.Length * 100);

                try
                {
                    using var tfile = TagLib.File.Create(storageFile);
                    TrackModel tvm = new TrackModel
                    {
                        TrackNumber = (int)tfile.Tag.Track,
                        TrackName = tfile.Tag.Title,
                        Album = tfile.Tag.Album,
                        Artists = tfile.Tag.Artists.Length > 0 ? string.Join(", ", tfile.Tag.Artists) : (tfile.Tag.AlbumArtists.Length > 0 ? string.Join(", ", tfile.Tag.AlbumArtists) : null),
                        Genre = string.Join(", ", tfile.Tag.Genres),
                        Bpm = tfile.Tag.BeatsPerMinute,
                        Key = tfile.Tag.InitialKey,
                        HasCover = tfile.Tag.Pictures.Length > 0,
                        CoverImage = tfile.Tag.Pictures.Length > 0 ? (useOgCover ? new Bitmap(new MemoryStream(tfile.Tag.Pictures[0].Data.Data)) : Bitmap.DecodeToWidth(new MemoryStream(tfile.Tag.Pictures[0].Data.Data), coverSize)) : null,
                        Bits_per_sample = tfile.Properties.BitsPerSample,
                        Sample_rate = tfile.Properties.AudioSampleRate,
                        Path = storageFile
                    };

                    Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                    {
                        Tracks.Add(tvm);
                    });
                }
                catch (Exception ex)
                {
                    ShowNotification(ex.Message, 10, "Error", NotificationType.Error);
                }
            }

            // add all files to backup task
            Backup = new ObservableCollection<TrackModel>(Tracks.Select(item => new TrackModel
            {
                TrackNumber = item.TrackNumber,
                TrackName = item.TrackName,
                Album = item.Album,
                Artists = item.Artists,
                Genre = item.Genre,
                Bpm = item.Bpm,
                Key = item.Key,
                CoverImage = item.CoverImage,
                Path = item.Path
            }));
        }

        private async Task DownloadFfmpegAsync()
        {
            WriteLog("Downloading ffmpeg...");

            IsLoading = true;
            LoadStatus = "Downloading ffmpeg... ";

            var url = GetDownloadUrl();
            var tempDir = Path.Combine(Path.GetTempPath(), "ffmpeg-download-" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            var archivePath = Path.Combine(tempDir, Path.GetFileName(url));

            using var http = new HttpClient();
            await File.WriteAllBytesAsync(archivePath, await http.GetByteArrayAsync(url));

            WriteLog("Extracting ffmpeg...");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ZipFile.ExtractToDirectory(archivePath, tempDir);
                var exe = Directory.GetFiles(tempDir, "ffmpeg.exe", SearchOption.AllDirectories)[0];
                File.Copy(exe, ffmpegPath, overwrite: true);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                ZipFile.ExtractToDirectory(archivePath, tempDir);
                var bin = Directory.GetFiles(tempDir, "ffmpeg", SearchOption.AllDirectories)[0];
                File.Copy(bin, ffmpegPath, overwrite: true);
                MakeExecutable(ffmpegPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ExtractTarXz(archivePath, tempDir);
                var bin = Directory.GetFiles(tempDir, "ffmpeg", SearchOption.AllDirectories)[0];
                File.Copy(bin, ffmpegPath, overwrite: true);
                MakeExecutable(ffmpegPath);
            }

            WriteLog("Ffmpeg downloaded successfully!");

            Directory.Delete(tempDir, recursive: true);
            CheckFFMPEG();
        }

        private async Task<bool> RunFfmpegAsync(string args, string outputFilePath)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = args,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    },
                    EnableRaisingEvents = true
                };

                var tcs = new TaskCompletionSource<bool>();
                process.Exited += (s, e) =>
                {
                    tcs.TrySetResult(process.ExitCode == 0);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        WriteLog($"[FFmpeg ERR] {e.Data}");
                    }
                };
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        WriteLog($"[FFmpeg OUT] {e.Data}");
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                bool success = await tcs.Task;

                if (!success)
                    WriteLog($"FFmpeg failed for {outputFilePath} (exit code: {process.ExitCode})");

                return success;
            }
            catch (Exception ex)
            {
                WriteLog($"Exception while running FFmpeg: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Commands
        [RelayCommand]
        private void SaveSettings()
        {
            if (localSettings is null) localSettings = AppSettingsModel.Load();
            
            localSettings.export_mp3 = Export_mp3;
            localSettings.export_wav = Export_wav;
            localSettings.export_flac = Export_flac;
            localSettings.export_aiff = Export_aiff;
            localSettings.use_ln = Use_ln;
            localSettings.ln_method = Ln_method;
            localSettings.ln_target_i = Ln_target_i;
            localSettings.ln_target_tpeak = Ln_target_tpeak;
            localSettings.ln_target_lu = Ln_target_lu;
            localSettings.cr_subdirectory = Cr_subdirectory;
            localSettings.convertBit = ConvertBit;
            localSettings.convertToBit = ConvertToBit;
            localSettings.keep_filename = Keep_filename;
            localSettings.fn_pattern = Fn_pattern;
            localSettings.hidden_columns = GetHiddenColumns();

            localSettings.Save(AppSettingsType.Default);
            ShowNotification("Your settings have been saved.", defaultNotificationTimeSpan, "Success", NotificationType.Success);

            WriteLog("Settings saved.");
        }

        [RelayCommand]
        private void OpenAdvancedSettings()
        {
            var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (mainWindow is null) return;

            var settingsWindow = new SettingsWindow(mainWindow!, this, localSettings ?? AppSettingsModel.Load());
            settingsWindow.ShowDialog(mainWindow);
        }

        [RelayCommand]
        private void OpenBatchFill()
        {
            var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
               ? desktop.MainWindow
               : null;

            if (mainWindow is null) return;

            var batchWindow = new BatchFillWindow(mainWindow, this.MainDataGrid!.Columns, SelectedTracks, Tracks);
            batchWindow.ShowDialog(mainWindow);
        }

        [RelayCommand]
        private void OpenLogFolder()
        {
            try
            {
                ProcessStartInfo startInfo = new()
                {
                    FileName = logDir,
                    UseShellExecute = true
                };

                Process.Start(startInfo);
            } catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        [RelayCommand]
        private async Task LoadFilesWithProgressAsync(string[] files)
        {
            IsLoading = true;
            ShowProgress = true;
            LoadStatus = "Loading files... (0 / " + files.Length + ")";
            LoadProgress = 0;

            var progress = new Progress<double>(p => LoadProgress = p);
            try
            {
                await Task.Run(() => GenerateTrackModelWithProgress(files, progress));
                ShowNotification($"{files.Length} track{(files.Length > 1 ? "s" : "")} loaded.", defaultNotificationTimeSpan, "Success", NotificationType.Success);
            } catch (Exception ex)
            {
                ShowNotification("Error loading files: " + ex.Message, 10, "Error", NotificationType.Error);
            } finally
            {
                IsLoading = false;
                ShowProgress = false;
                LoadStatus = "";
                LoadProgress = 0;
            }
        }

        [RelayCommand]
        private async Task LoadFilesAsync()
        {
            var window = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (window == null) return;

            var result = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Audio File(s)",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Audio Files")
                    {
                        Patterns = new[] { "*.mp3", "*.wav", "*.flac", "*.aiff", "*.m4a", "*.ogg" },
                        AppleUniformTypeIdentifiers = new[] { "public.audio" },
                        MimeTypes = new[] { "audio/*" }
                    }
                }
            });

            if (result?.Count <= 0) return;
            if (result == null) return;

            var files = result.Select(storageFile => storageFile.Path.LocalPath).ToArray();

            if (files.Length == 0) return;

            await LoadFilesWithProgressAsync(files);
        }

        [RelayCommand]
        private async Task LoadFolderAsync()
        {
            var window = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (window == null) return;

            var result = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Folder with Audio Files",
                AllowMultiple = false
            });

            if (result?.Count <= 0) return;
            if (result == null) return;

            var path = result[0].Path.LocalPath;
            var files = Directory.EnumerateFiles(path, "*.*", localSettings!.search_subdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToArray();

            if (files.Length == 0) return;

            await LoadFilesWithProgressAsync(files);
        }

        [RelayCommand]
        private async Task RemoveCover()
        {
            if(SelectedTracks.Count == 0) return;

            bool isMultiple = SelectedTracks.Count > 1;
            List<string> paths = new List<string>();
            foreach (var track in SelectedTracks)
            {
                paths.Add(track.Path);
            }

            var box = MessageBoxManager
                .GetMessageBoxStandard(
                    title: "Confirm",
                    text: "Are you sure you want to remove the cover of th" + (isMultiple ? "ese" : "is") + " track" + (isMultiple ? "s" : "") + ":\r\n\r\n" + string.Join("\r\n", paths),
                    ButtonEnum.YesNo,
                    Icon.Warning);

            var result = await box.ShowAsync();

            if (result == ButtonResult.Yes)
            {
                foreach(var track in SelectedTracks)
                {
                    if (newCoverList.ContainsKey(track.Path))
                    {
                        newCoverList[track.Path] = null;
                    } else
                    {
                        newCoverList.Add(track.Path, null);
                    }

                    track.RefreshCoverDisplay();
                }

                UpdateCoverPreview();
                ShowNotification(SelectedTracks.Count + " cover" + (SelectedTracks.Count > 1 ? "s" : "") + " removed.", defaultNotificationTimeSpan, "Success", NotificationType.Success);
            }
        }

        [RelayCommand]
        private async Task ReplaceCover()
        {
            if (SelectedTracks.Count == 0) return;

            bool isMultiple = SelectedTracks.Count > 1;
            List<string> paths = new List<string>();
            foreach (var track in SelectedTracks)
            {
                paths.Add(track.Path);
            }

            var window = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (window == null) return;

            var result = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Choose Cover",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Image Files")
                    {
                        Patterns = new[] { "*.jpg", "*.png" },
                        AppleUniformTypeIdentifiers = new[] { "public.image" },
                        MimeTypes = new[] { "image/*" }
                    }
                }
            });

            if (result?.Count <= 0) return;
            if (result == null) return;

            var file = result.Select(sFile => sFile.Path.LocalPath).FirstOrDefault();

            if (file == null) return;

            Bitmap newCover = new Bitmap(file);
            if (newCover.PixelSize.Width != newCover.PixelSize.Height)
            {
                ShowNotification("Cover image has to be squared!", defaultNotificationTimeSpan, "Error", NotificationType.Error);
                return;
            }

            var box = MessageBoxManager
                .GetMessageBoxStandard(
                    title: "Confirm",
                    text: "Are you sure you want to replace the cover of th" + (isMultiple ? "ese" : "is") + " track" + (isMultiple ? "s" : "") + ":\r\n\r\n" + string.Join("\r\n", paths) + "\r\n\r\nWith this image:\r\n" + file,
                    ButtonEnum.YesNo,
                    Icon.Warning);

            var boxResult = await box.ShowAsync();

            if (boxResult == ButtonResult.Yes)
            {
                foreach(var item in SelectedTracks)
                {
                    if (newCoverList.ContainsKey(item.Path))
                    {
                        newCoverList[item.Path] = newCover;
                    } else
                    {
                        newCoverList.Add(item.Path, newCover);
                    }

                    item.RefreshCoverDisplay();
                }
                UpdateCoverPreview();
            }
        }

        [RelayCommand]
        private void ResetTrack(TrackModel trackToReset)
        {
            if (trackToReset is null) return;

            var backupTrack = Backup.FirstOrDefault(t => t.Path == trackToReset.Path);
            if (backupTrack is null) return;

            trackToReset.TrackNumber = backupTrack.TrackNumber;
            trackToReset.TrackName = backupTrack.TrackName;
            trackToReset.Album = backupTrack.Album;
            trackToReset.Artists = backupTrack.Artists;
            trackToReset.Genre = backupTrack.Genre;
            trackToReset.Bpm = backupTrack.Bpm;
            trackToReset.Key = backupTrack.Key;
            trackToReset.CoverImage = backupTrack.CoverImage;

            if (newCoverList.ContainsKey(trackToReset.Path ?? ""))
            {
                newCoverList.Remove(trackToReset.Path ?? "");
            }

            trackToReset.RefreshCoverDisplay();
            trackToReset.NotifyAll();
            UpdateCoverPreview();

            ShowNotification("Track reset to original metadata.", 4, "Info", NotificationType.Information);
        }

        [RelayCommand]
        public void ResetTracks()
        {
            if (Tracks is null) return;
            foreach (TrackModel trackToReset in Tracks)
            {
                var backupTrack = Backup.FirstOrDefault(t => t.Path == trackToReset.Path);
                if(backupTrack is null) continue;

                trackToReset.TrackNumber = backupTrack.TrackNumber;
                trackToReset.TrackName = backupTrack.TrackName;
                trackToReset.Album = backupTrack.Album;
                trackToReset.Artists = backupTrack.Artists;
                trackToReset.Genre = backupTrack.Genre;
                trackToReset.Bpm = backupTrack.Bpm;
                trackToReset.Key = backupTrack.Key;
                trackToReset.CoverImage = backupTrack.CoverImage;

                if (newCoverList.ContainsKey(trackToReset.Path ?? ""))
                {
                    newCoverList.Remove(trackToReset.Path ?? "");
                }

                trackToReset.RefreshCoverDisplay();
                trackToReset.NotifyAll();
            }

            UpdateCoverPreview();

            ShowNotification("Tracks reset to original metadata.", 4, "Info", NotificationType.Information);
        }

        [RelayCommand]
        private async Task ShowAnalyzeWindow()
        {
            if (SelectedTracks is null || SelectedTracks.Count == 0) return;

            var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
            {
                CanResize = false,
                ContentHeader = "Analyze Tracks",
                ContentTitle = "Analyze Tracks",
                ContentMessage = "Are you sure you want to analyze " + SelectedTracks.Count + " track" + (SelectedTracks.Count > 1 ? "s" : "") + "?",
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.WidthAndHeight,
                ButtonDefinitions = new List<ButtonDefinition>
                {
                    new ButtonDefinition { Name = "Key" },
                    new ButtonDefinition { Name = "Cancel" }
                }
            });

            var result = await box.ShowAsync();

            IsLoading = true;
            ShowProgress = true;
            LoadProgress = 0;

            if (result == "Key")
            {
                LoadStatus = "Analyzing Key...";

                var progress = new Progress<double>(percent => LoadProgress = percent);

                try
                {
                    //await Task.Run(() => AnalyzeKeyInBackground(SelectedTracks, progress));

                    LoadStatus = "Key analysis completed!";
                }
                catch (Exception ex)
                {
                    LoadStatus = $"Error: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                    ShowProgress = false;
                    LoadProgress = 100;
                }
            }
        }

        [RelayCommand]
        public async Task EnsureFfmpegAsync()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ffmpegPath)!);
            await DownloadFfmpegAsync();
        }

        [RelayCommand]
        public async Task StartExport()
        {
            #region Open Folder Dialog
            mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (mainWindow?.StorageProvider is null) return;

            var options = new FolderPickerOpenOptions
            {
                Title = "Select output folder",
                AllowMultiple = false
            };

            var result = await mainWindow?.StorageProvider.OpenFolderPickerAsync(options)!;
            var selectedFolder = result?.FirstOrDefault();
            if (selectedFolder is null) return;

            string? outputPath = selectedFolder.TryGetLocalPath();
            if(string.IsNullOrEmpty(outputPath))
            {
                outputPath = selectedFolder.Name;
            }
            #endregion

            #region Error Handling
            if (Tracks.Count == 0)
            {
                var msg = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    CanResize = false,
                    ContentHeader = "No tracks loaded",
                    ContentTitle = "Error",
                    ContentMessage = "You have no track(s) loaded.",
                    ShowInCenter = true,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    ButtonDefinitions = ButtonEnum.Ok,
                    SizeToContent = SizeToContent.WidthAndHeight
                });
                await msg.ShowAsync();
                return;
            }
            if(GetSelectedFormats() is null || GetSelectedFormats().Length == 0)
            {
                var msg = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    CanResize = false,
                    ContentHeader = "No export formats selected.",
                    ContentTitle = "Error",
                    ContentMessage = "Please select at least one output format.",
                    ShowInCenter = true,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    ButtonDefinitions = ButtonEnum.Ok,
                    SizeToContent = SizeToContent.WidthAndHeight
                });
                await msg.ShowAsync();
                return;
            }
            #endregion

            #region Log
            WriteLog("");
            WriteLog(">>> Starting track export ....");
            WriteLog("> Tracks: " + Tracks.Count + " | Subdirectory: " + (Cr_subdirectory ? "Yes" : "No") + " | New files: " + (Tracks.Count * GetSelectedFormats().Length));
            WriteLog("> " + (Keep_filename ? "Keeping original filename" : "Filename Pattern: " + Fn_pattern));

            if (Use_ln)
            {
                if(Ln_method == "replaygain")
                {
                    WriteLog("> Loudness Normalization: Replaygain");
                } else
                {
                    WriteLog("> Loudness Normalization: loudnorm (target: " + Ln_target_i + ", tpeak: " + Ln_target_tpeak + ", lu: " + Ln_target_lu + ")");
                }
            }

            WriteLog("> Export to formats: " + GetSelectedFormats().ToList().ToString());
            WriteLog("");
            #endregion

            IsLoading = true;
            ShowProgress = true;
            LoadProgress = 0;
            LoadStatus = "Preparing export...";

            try
            {
                int totalOperations = Tracks.Count * GetSelectedFormats().Length;
                int completedOperations = 0;

                foreach (TrackModel track in Tracks)
                {
                    string inputPath = track.Path!;
                    string? baseFileName = Path.GetFileNameWithoutExtension(inputPath);
                    string baseStatus = "[" + (track.TrackName ?? baseFileName) + "]";

                    #region Apply pattern on filename
                    if (!Keep_filename)
                    {
                        baseFileName = Fn_pattern;
                        baseFileName = baseFileName?.Replace("%number%", track?.TrackNumber.ToString());
                        baseFileName = baseFileName?.Replace("%title%", track?.TrackName);
                        baseFileName = baseFileName?.Replace("%album%", track?.Album);
                        baseFileName = baseFileName?.Replace("%artists%", track?.Artists);
                        baseFileName = baseFileName?.Replace("%bpm%", track?.Bpm.ToString());
                        baseFileName = baseFileName?.Replace("%key%", track?.Key);

                        string[] replaceables = { "/", ":", ";", "\"", "\'", "^", "!" };
                        foreach (string character in replaceables)
                        {
                            baseFileName = baseFileName?.Replace(character, "_");
                        }
                    }
                    #endregion

                    foreach (string format in GetSelectedFormats())
                    {
                        #region Output Path / Create sub-directory
                        string? finalOutputPath = null;
                        if (Cr_subdirectory)
                        {
                            finalOutputPath = Path.Combine(outputPath, baseFileName ?? "error");
                        }

                        Directory.CreateDirectory(finalOutputPath ?? outputPath);
                        #endregion

                        string outputFilePath = Path.Combine(finalOutputPath ?? outputPath, $"{baseFileName}.{format}");
                        string filter = "", extraFilter = "", formatargs = "", metadata = "", cover = "";

                        #region Loudness Normalization
                        if (Use_ln)
                        {
                            if (Ln_method == "loudnorm")
                            {
                                filter = $"-af loudnorm=I={Ln_target_i.ToString(CultureInfo.InvariantCulture)}" +
                                            $":TP={Ln_target_tpeak.ToString(CultureInfo.InvariantCulture)}" +
                                            $":LRA={Ln_target_lu.ToString(CultureInfo.InvariantCulture)}";
                            }
                            else if (Ln_method == "replaygain")
                            {
                                filter = "-af replaygain";
                            }
                        }
                        #endregion

                        #region Target Bit Depth
                        int targetBitDepth = track?.Bits_per_sample ?? 16;

                        if (ConvertBit)
                        {
                            if (ConvertToBit == 16 || ConvertToBit == 24)
                            {
                                if (track?.Bits_per_sample == null || track.Bits_per_sample >= ConvertToBit)
                                {
                                    targetBitDepth = ConvertToBit;
                                }
                            }
                        }
                        #endregion

                        #region Format specific args
                        switch (format.ToLower())
                        {
                            case "mp3":
                                formatargs = " -c:a libmp3lame -b:a 320k -write_id3v2 1 -id3v2_version 3";
                                break;

                            case "wav":
                                string pcmCodec = targetBitDepth switch
                                {
                                    16 => "pcm_s16le",
                                    24 => "pcm_s24le",
                                    32 => "pcm_s32le",
                                    _ => "pcm_s16le"
                                };
                                formatargs = $" -c:a {pcmCodec}";

                                if (ConvertBit && ConvertToBit == 16 && (track?.Bits_per_sample ?? 0) > 16)
                                {
                                    extraFilter = ",aresample=osf=s16:dither_method=triangular_hp";
                                }
                                formatargs += $" -ar {track?.Sample_rate}";
                                break;

                            case "flac":
                                if (ConvertBit && (ConvertToBit == 16 || ConvertToBit == 24))
                                {
                                    string sampleFmt = ConvertToBit == 16 ? "s16" : "s32";
                                    formatargs = $" -c:a flac -sample_fmt {sampleFmt}";
                                }
                                else
                                {
                                    formatargs = " -c:a flac";
                                }
                                if (ConvertBit && ConvertToBit == 16 && (track?.Bits_per_sample ?? 0) > 16)
                                {
                                    extraFilter = ",aresample=osf=s16:dither_method=triangular_hp";
                                }

                                formatargs += $" -ar {track?.Sample_rate}";
                                break;

                            case "aiff":
                                string aiffCodec = targetBitDepth switch
                                {
                                    16 => "pcm_s16be",
                                    24 => "pcm_s24be",
                                    32 => "pcm_s32be",
                                    _ => "pcm_s16be"
                                };

                                formatargs = $" -c:a {aiffCodec}";

                                if (ConvertBit && ConvertToBit == 16 && (track?.Bits_per_sample ?? 0) > 16)
                                {
                                    extraFilter = ",aresample=osf=s16:dither_method=triangular_hp";
                                }

                                formatargs += $" -ar {track?.Sample_rate} -write_id3v2 1";
                                break;
                        }
                        #endregion

                        #region Metadata
                        var backupItem = Backup.First(b => b.Path == track?.Path);
                        if (track?.TrackNumber != backupItem.TrackNumber)
                        {
                            metadata += $"-metadata track=\"{track?.TrackNumber}\" ";
                        }
                        if (track?.TrackName != backupItem.TrackName)
                        {
                            metadata += $"-metadata title=\"{track?.TrackName}\" ";
                        }
                        if (track?.Album != backupItem.Album)
                        {
                            metadata += $"-metadata album=\"{track?.Album}\" ";
                        }
                        if (track?.Artists != backupItem.Artists)
                        {
                            metadata += $"-metadata artist=\"{track?.Artists}\" ";
                        }
                        if (track?.Bpm != backupItem.Bpm || format == "flac")
                        {
                            metadata += $"-metadata BPM=\"{track?.Bpm}\" ";
                            metadata += $"-metadata TBPM=\"{track?.Bpm}\" ";
                            metadata += format == "aiff" ? $"-metadata com.apple.iTunes:BPM=\"{track?.Bpm}\" " : "";

                        }
                        if (track?.Key != backupItem.Key || format == "flac")
                        {
                            metadata += $"-metadata TKEY=\"{track?.Key}\" ";
                            metadata += $"-metadata INITIALKEY=\"{track?.Key}\" ";
                            metadata += format == "aiff" ? $"-metadata com.apple.iTunes:INITIALKEY=\"{track?.Key}\" " : "";
                        }
                        #endregion

                        #region Cover
                        if (newCoverList.ContainsKey(track?.Path!))
                        {
                            var newCover = newCoverList[track?.Path!];

                            if (backupItem.CoverImage != newCover && format != "wav")
                            {
                                string tempCoverPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".jpg");
                                newCover?.Save(tempCoverPath);

                                cover = $"-i \"{tempCoverPath}\" -map 0:a -map 1:v -c:v mjpeg "
                                    + (format == "flac" ? "-disposition:v attached_pic" : "")
                                    + " -metadata:s:v title=\"Cover\" -metadata:s:v comment=\"Cover\" ";
                            }
                        }
                        #endregion

                        #region Build Args
                        string filterPart = "";
                        if (Use_ln)
                        {
                            filterPart = filter;
                        }

                        if (!string.IsNullOrEmpty(extraFilter))
                        {
                            if (!string.IsNullOrEmpty(filterPart))
                                filterPart += extraFilter;
                            else
                                filterPart = "-af" + extraFilter.TrimStart(',');
                        }

                        string pre_args = $"{cover} {filterPart} {formatargs} {metadata}";
                        string args = $"-y -i \"{track?.Path!}\" " + pre_args + $" \"{outputFilePath}\"";
                        #endregion

                        WriteLog("> Args: " + pre_args);

                        LoadStatus = $"Exporting {track?.TrackName ?? baseFileName} → {format.ToUpper()}";
                        bool success = await RunFfmpegAsync(args, outputFilePath);
                        completedOperations++;

                        LoadProgress = (double)completedOperations / totalOperations * 100;
                        if(!success)
                        {
                            // Optional: collect failed files and show summary at the end
                        }
                    }
                }

                LoadStatus = "Export completed!";
                ShowNotification("Export finished successfully.", 6, "Success", NotificationType.Success);
                WriteLog(">>> Export completed!");
            } catch (Exception ex)
            {
                ShowNotification($"Export failed: {ex.Message}", 10, "Error", NotificationType.Error);
                WriteLog($"Export error: {ex}");
            } finally
            {
                IsLoading = false;
                ShowProgress = false;
                LoadProgress = 0;
                LoadStatus = "";
            }
        }
        #endregion

        #region Events
        public void OnDataGridSelChange(object? sender, SelectionChangedEventArgs e, DataGrid dg)
        {
            SelectedTracks.Clear();
            if (dg.SelectedItems != null)
            {
                foreach (var item in dg.SelectedItems)
                {
                    if (item is TrackModel track2)
                    {
                        SelectedTracks.Add(track2);
                    }
                }
            }

            UpdateCoverPreview();

            var count = SelectedTracks.Count;
            Btn_replace_enabled = count > 0;
            Btn_remove_enabled = count > 0;
            Btn_analyze_enabled = count > 0;
        }
        #endregion

        public MainWindowViewModel()
        {
            Instance = this;

            mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;
        }
    }
}
#pragma warning restore CS0618