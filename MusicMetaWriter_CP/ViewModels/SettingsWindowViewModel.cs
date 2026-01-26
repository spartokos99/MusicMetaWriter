using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicMetaWriter.Enums;
using MusicMetaWriter.Models;
using System;

namespace MusicMetaWriter_CP.ViewModels
{
    public partial class SettingsWindowViewModel : ObservableObject
    {
        #region Variables
        private readonly MainWindowViewModel _parentVm;
        private readonly AppSettingsModel _tempSettings;

        public event EventHandler? CloseRequested;
        #endregion

        #region ObservableProperties
        [ObservableProperty] public bool search_subdirectories;
        [ObservableProperty] public ThemeEnum use_theme;
        [ObservableProperty] public bool use_better_cover;
        [ObservableProperty] public int reduce_size;
        #endregion

        #region Helpers
        private void LoadAdvancedSettings()
        {
            Use_better_cover = _tempSettings.use_better_cover;
            Use_theme = _tempSettings.use_theme;
            Search_subdirectories = _tempSettings.search_subdirectories;
            Reduce_size = _tempSettings.reduce_size;
        }
        #endregion

        #region Commands
        [RelayCommand]
        private void SaveAdvancedSettings()
        {
            _tempSettings.use_better_cover = Use_better_cover;
            _tempSettings.use_theme = Use_theme;
            _tempSettings.search_subdirectories = Search_subdirectories;
            _tempSettings.reduce_size = Reduce_size;

            _tempSettings.Save(AppSettingsType.Advanced);

            _parentVm.SetTheme();
            _parentVm.ShowNotification("Your adv. settings have been saved.", _parentVm.defaultNotificationTimeSpan, "Success", NotificationType.Success);

            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        public SettingsWindowViewModel(MainWindowViewModel parentVm, AppSettingsModel? tempSettings)
        {
            _parentVm = parentVm;
            _tempSettings = tempSettings ?? AppSettingsModel.Load();
            
            LoadAdvancedSettings();
        }
    }
}
