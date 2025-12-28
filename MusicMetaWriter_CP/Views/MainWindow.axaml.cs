using Avalonia.Controls;
using Avalonia.Interactivity;
using MusicMetaWriter.Models;
using MusicMetaWriter_CP.ViewModels;
using WinRT;

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
            DataContext.As<MainWindowViewModel>().OnDataGridSelChange(sender, e, MyDataGrid);
        }

        private void MyDataGrid_Loaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm && sender is DataGrid dg)
            {
                vm.MainDataGrid = dg;

                // init settings
                vm.localSettings = AppSettingsModel.Load();
                vm.LoadSettings();
                vm.PrepareLogs();

                vm.CheckFFMPEG();
            }
        }
    }
}