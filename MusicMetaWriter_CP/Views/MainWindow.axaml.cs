using Avalonia.Controls;
using MusicMetaWriter;
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
    }
}