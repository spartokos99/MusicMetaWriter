using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using Avalonia.Notification;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MusicMetaWriter.Enums;
using MusicMetaWriter.Models;
using MusicMetaWriter_CP.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
        #endregion

        #region ObservableProperties
        [ObservableProperty] private ObservableCollection<TrackModel> _tracks = new();
        [ObservableProperty] private ObservableCollection<TrackModel> _backup = new();

        [ObservableProperty] private double _loadProgress;
        [ObservableProperty] private string _loadStatus = "";
        [ObservableProperty] private bool _isLoading;

        [ObservableProperty] private bool export_mp3;
        [ObservableProperty] private bool export_wav;
        [ObservableProperty] private bool export_flac;
        [ObservableProperty] private bool export_aiff;

        [ObservableProperty] private bool use_ln;
        [ObservableProperty] private double ln_target_i;
        [ObservableProperty] private double ln_target_tpeak;
        [ObservableProperty] private double ln_target_lu;

        [ObservableProperty] private bool cr_subdirectory;
        [ObservableProperty] private bool keep_filename;
        [ObservableProperty] private string? fn_pattern;

        [ObservableProperty] private ObservableCollection<TrackModel> _selectedTracks = new ObservableCollection<TrackModel>();
        [ObservableProperty] private Bitmap? selectedImage;

        [ObservableProperty] private bool btn_replace_enabled = false;
        [ObservableProperty] private bool btn_remove_enabled = false;
        [ObservableProperty] private bool btn_bpm_enabled = false;
        [ObservableProperty] private bool btn_key_enabled = false;
        #endregion

        #region Helper Functions
        public void LoadSettings()
        {
            if (localSettings is not null && MainDataGrid is not null)
            {
                Export_mp3 = localSettings.export_mp3;
                Export_wav = localSettings.export_wav;
                Export_flac = localSettings.export_flac;
                Export_aiff = localSettings.export_aiff;

                Use_ln = localSettings.use_ln;
                Ln_target_i = localSettings.ln_target_i;
                Ln_target_tpeak = localSettings.ln_target_tpeak;
                Ln_target_lu = localSettings.ln_target_lu;

                Cr_subdirectory = localSettings.cr_subdirectory;
                Keep_filename = localSettings.keep_filename;
                Fn_pattern = localSettings.fn_pattern;

                foreach (var col in MainDataGrid.Columns)
                {
                    if (localSettings.hidden_columns is not null && localSettings.hidden_columns.Contains(col.Header?.ToString()?.ToLower().Replace(" ", "_")))
                    {
                        col.IsVisible = false;
                    }
                }
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

        private async Task GenerateTrackModelWithProgress(string[] paths, IProgress<double> progress)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Tracks.Clear();
                Backup.Clear();
            });

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
                        CoverImage = tfile.Tag.Pictures.Length > 0 ? new Bitmap(new MemoryStream(tfile.Tag.Pictures[0].Data.Data)) : null,
                        Bits_per_sample = tfile.Properties.BitsPerSample,
                        Sample_rate = tfile.Properties.AudioSampleRate,
                        Path = storageFile
                    };

                    Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                    {
                        Tracks.Add(tvm);
                        Backup.Add(tvm);
                    });
                } catch (Exception ex)
                {
                    ShowNotification(ex.Message, 10, "Error", NotificationType.Error);
                }
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

        private string[] GetHiddenColumns()
        {
            if (MainDataGrid == null) return [];
            return MainDataGrid.Columns.Where(col => !col.IsVisible).Select(col => col.Header?.ToString()?.ToLower().Replace(" ", "_") ?? "").ToArray();
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
            localSettings.ln_target_i = Ln_target_i;
            localSettings.ln_target_tpeak = Ln_target_tpeak;
            localSettings.ln_target_lu = Ln_target_lu;
            localSettings.cr_subdirectory = Cr_subdirectory;
            localSettings.keep_filename = Keep_filename;
            localSettings.fn_pattern = Fn_pattern;
            localSettings.hidden_columns = GetHiddenColumns();

            localSettings.Save(AppSettingsType.Default);
            ShowNotification("Your settings have been saved.", defaultNotificationTimeSpan, "Success", NotificationType.Success);
        }

        [RelayCommand]
        private void OpenAdvancedSettings()
        {
            var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (mainWindow is null) return;

            var settingsWindow = new SettingsWindow(mainWindow, this, localSettings ?? AppSettingsModel.Load());
            settingsWindow.ShowDialog(mainWindow);
        }

        [RelayCommand]
        private async Task LoadFilesWithProgressAsync(string[] files)
        {
            IsLoading = true;
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
            var files = Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly)
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
                }

                UpdateCoverPreview();
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
            Btn_bpm_enabled = count > 0;
            Btn_key_enabled = count > 0;
        }
        #endregion

        public MainWindowViewModel()
        {
            Instance = this;
        }
    }
}
#pragma warning restore CS0618