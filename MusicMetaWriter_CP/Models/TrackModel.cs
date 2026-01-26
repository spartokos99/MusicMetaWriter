using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MusicMetaWriter_CP.ViewModels
{
    public partial class TrackModel : ObservableObject
    {
        [ObservableProperty] private int _trackNumber;
        [ObservableProperty] private bool _hasCover;
        [ObservableProperty] private Bitmap? _coverImage;
        [ObservableProperty] private string? _trackName;
        [ObservableProperty] private string? _album;
        [ObservableProperty] private string? _artists;
        [ObservableProperty] private string? _genre;
        [ObservableProperty] private double? _bpm;
        [ObservableProperty] private string? _key;
        [ObservableProperty] private int _bits_per_sample;
        [ObservableProperty] private int _sample_rate;
        [ObservableProperty] private string? _path;
        
        public Bitmap? EffectiveCoverImage
        {
            get
            {
                if(MainWindowViewModel.Instance?.newCoverList.TryGetValue(Path, out var newCover) == true)
                {
                    return newCover;
                }

                return CoverImage;
            }
        }

        public bool HasEffectiveCover => EffectiveCoverImage != null;

        public void RefreshCoverDisplay()
        {
            OnPropertyChanged(nameof(EffectiveCoverImage));
            OnPropertyChanged(nameof(HasEffectiveCover));
        }

        public void NotifyAll()
        {
            OnPropertyChanged(string.Empty);
        }
    }
}
