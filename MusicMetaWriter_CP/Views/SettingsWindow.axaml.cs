using Avalonia.Controls;
using MusicMetaWriter.Models;
using MusicMetaWriter_CP.ViewModels;

namespace MusicMetaWriter_CP.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    public SettingsWindow(Window owner, MainWindowViewModel parentVm, AppSettingsModel localSettings)
    {
        InitializeComponent();
        Owner = owner;

        DataContext = new SettingsWindowViewModel(parentVm, localSettings);
        if(DataContext is SettingsWindowViewModel vm)
        {
            vm.CloseRequested += (s, e) => Close();
        }
    }
}