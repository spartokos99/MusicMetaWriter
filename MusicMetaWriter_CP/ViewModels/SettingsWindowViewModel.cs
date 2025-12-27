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
        [ObservableProperty] private bool use_better_cover;
        [ObservableProperty] private ThemeEnum use_theme;
        #endregion

        #region Helper
        private void LoadAdvancedSettings()
        {
            Use_better_cover = _tempSettings.use_better_cover;
            Use_theme = _tempSettings.use_theme;
        }
        #endregion

        #region Commands
        [RelayCommand]
        private void SaveAdvancedSettings()
        {
            _tempSettings.use_better_cover = Use_better_cover;
            _tempSettings.use_theme = Use_theme;

            _tempSettings.Save(AppSettingsType.Advanced);

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
