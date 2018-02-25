using System.Collections.ObjectModel;
using VideoEffectTest.Effects.Win2D;

namespace VideoEffectTest.Uwp.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private VideoEffectItemViewModel selectedEffect;

        public MainPageViewModel()
        {
            VideoEffects = new ObservableCollection<VideoEffectItemViewModel>
            {
                new VideoEffectItemViewModel(typeof(EdgeDetectionVideoEffect), "EdgeDetection", "Amount", 0.5f, 1f),
                new VideoEffectItemViewModel(typeof(SaturationVideoEffect), "Saturation", "Intensity", 0.5f, 1f),
                new VideoEffectItemViewModel(typeof(SepiaVideoEffect), "Sepia", "Intensity", 0.5f, 1f),
                new VideoEffectItemViewModel(typeof(VignetteVideoEffect), "Vignette", "Amount", 0.5f, 1f)
            };
        }

        public ObservableCollection<VideoEffectItemViewModel> VideoEffects { get; }

        public VideoEffectItemViewModel SelectedEffect
        {
            get => selectedEffect;
            set => SetProperty(ref selectedEffect, value);
        }
    }
}