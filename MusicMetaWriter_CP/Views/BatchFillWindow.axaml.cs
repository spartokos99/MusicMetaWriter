using Avalonia.Controls;
using MusicMetaWriter_CP.ViewModels;
using System.Collections.ObjectModel;
using WinRT;

namespace MusicMetaWriter_CP.Views;

public partial class BatchFillWindow : Window
{
    public BatchFillWindow()
    {
        InitializeComponent();
    }

    public BatchFillWindow(Window owner, ObservableCollection<DataGridColumn> dgcols, ObservableCollection<TrackModel> selectedTracks, ObservableCollection<TrackModel> allTracks)
    {
        InitializeComponent();
        Owner = owner;

        DataContext = new BatchFillViewModel(dgcols, selectedTracks, allTracks);
        if (DataContext is BatchFillViewModel vm)
        {
            vm.CloseRequested += (s, e) => Close();
        }
    }

    #region Events
    private void ValueChanged(object? sender, SelectionChangedEventArgs? e)
    {
        DataContext.As<BatchFillViewModel>().UpdateFieldStatus();
    }

    private void ValueChangedText(object? sender, TextChangedEventArgs e)
    {
        ValueChanged(sender, null);
    }
    #endregion
}