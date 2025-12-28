using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MusicMetaWriter_CP.ViewModels
{
    public partial class BatchFillViewModel : ObservableObject
    {
        #region Variables
        public ObservableCollection<string> ColumnHeaders { get; } = new ObservableCollection<string>();
        public ObservableCollection<TrackModel> SelectedTracks { get; } = new ObservableCollection<TrackModel>();
        public ObservableCollection<TrackModel> AllTracks { get; } = new ObservableCollection<TrackModel>();
        public event EventHandler? CloseRequested;
        #endregion

        #region ObservableProperties
        [ObservableProperty] private string? _sel_column;
        [ObservableProperty] private string? _text;
        [ObservableProperty] private bool _onlySelection;
        [ObservableProperty] private bool _disableSelection;
        [ObservableProperty] private string? _selectedCount;

        [ObservableProperty] private bool _canFill = false;
        [ObservableProperty] private bool _isNumeric = false;
        #endregion

        #region Helpers
        public void UpdateFieldStatus()
        {
            CanFill = (Text is not null && Text != "" && Sel_column is not null);
            IsNumeric = (Sel_column is not null && Sel_column == "Key");

            if (IsNumeric && !string.IsNullOrEmpty(Text))
            {
                if (!int.TryParse(Text, out _) && !double.TryParse(Text, out _))
                {
                    Text = "0";
                }
            }
        }

        private static readonly Dictionary<string, Action<TrackModel, string>> FillActions = new()
        {
            ["Track Name"] = (track, value) => track.TrackName = value,
            ["Album"] = (track, value) => track.Album = value,
            ["Artists"] = (track, value) => track.Artists = value,
            ["Genre"] = (track, value) => track.Genre = value,
            ["Key"] = (track, value) => track.Key = value,
            ["BPM"] = (track, value) =>
            {
                if (double.TryParse(value, out var bpm))
                    track.Bpm = bpm;
            }
        };
        #endregion

        #region Commands
        [RelayCommand]
        private void Ok()
        {
            var tracksToEdit = OnlySelection ? SelectedTracks : AllTracks;

            if (string.IsNullOrWhiteSpace(Sel_column) || Text is null)
                return;

            if (FillActions.TryGetValue(Sel_column, out var action))
            {
                foreach (var track in tracksToEdit)
                {
                    action(track, Text);
                    track.RefreshCoverDisplay();
                    track.NotifyAll();
                }
            }

            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Cancel()
            => CloseRequested?.Invoke(this, EventArgs.Empty);
        #endregion

        public BatchFillViewModel(ObservableCollection<DataGridColumn> columns, ObservableCollection<TrackModel> selectedTracks, ObservableCollection<TrackModel> allTracks)
        {
            this.SelectedTracks = selectedTracks;
            this.AllTracks = allTracks;
            SelectedCount = "Only fill selected tracks (" + this.SelectedTracks.Count + ")";
            this.DisableSelection = SelectedTracks.Count <= 0;
            this.OnlySelection = SelectedTracks.Count > 0;

            foreach (var col in columns.Where(i => !i.IsReadOnly && i.Header.ToString() != "#"))
            {
                if (col.Header is string header)
                {
                    ColumnHeaders.Add(header);
                }
            }
        }
    }
}
