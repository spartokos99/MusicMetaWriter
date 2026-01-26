using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MusicMetaWriter.Models;
using MusicMetaWriter_CP.ViewModels;

namespace MusicMetaWriter_CP.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }

        private void MyDataGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            MainWindowViewModel? mwvm = DataContext as MainWindowViewModel;
            mwvm?.OnDataGridSelChange(sender, e, MyDataGrid);
        }

        private void MyDataGrid_Loaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm && sender is DataGrid dg)
            {
                vm.MainDataGrid = dg;

                vm.PrepareLogs();

                vm.localSettings = AppSettingsModel.Load();
                vm.LoadSettings();

                vm.CheckFFMPEG();
            }
        }

        #region NativeMenu
        private void OpenFilesAsync(object? sender, EventArgs args)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.LoadFilesAsyncFunc();
            }
        }

        private void OpenFolderAsync(object? sender, EventArgs args)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.LoadFolderAsyncFunc();
            }
        }

        private void SaveSettings(object? sender, EventArgs args)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.SaveSettingsFunc();
            }
        }

        private void ShowBatchFill(object? sender, System.EventArgs args)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.OpenBatchFillFunc();
            }
        }

        private void ResetTracks(object? sender, EventArgs args)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.ResetTracksFunc();
            }
        }
        #endregion
    }
}