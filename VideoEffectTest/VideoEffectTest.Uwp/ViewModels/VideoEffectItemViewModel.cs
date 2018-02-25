using System;

namespace VideoEffectTest.Uwp.ViewModels
{
    public class VideoEffectItemViewModel : ViewModelBase
    {
        private Type videoEffect;
        private string displayName;
        private float minPropertyValue;
        private float maxPropertyValue;
        private object propertyValue;
        private string propertyName;
        private string iconImagePath;
        private bool isSliderEnabled;

        public VideoEffectItemViewModel() {}
        
        public VideoEffectItemViewModel(Type effect, string effectName, string propertyName, float defaultValue, float maxValue, float minValue = 0f)
        {
            videoEffect = effect;
            displayName = effectName;
            iconImagePath = $"ms-appx:///Images/{effectName}.jpg";
            this.propertyName = propertyName;
            propertyValue = defaultValue;
            maxPropertyValue = maxValue;
            minPropertyValue = minValue;
            isSliderEnabled = true;
        }
        
        public Type VideoEffect
        {
            get => videoEffect;
            set => SetProperty(ref videoEffect, value);
        }

        public string DisplayName
        {
            get => displayName;
            set => SetProperty(ref displayName, value);
        }

        public float MinPropertyValue
        {
            get => minPropertyValue;
            set => SetProperty(ref minPropertyValue, value);
        }

        public float MaxPropertyValue
        {
            get => maxPropertyValue;
            set => SetProperty(ref maxPropertyValue, value);
        }

        public object PropertyValue
        {
            get => propertyValue;
            set => SetProperty(ref propertyValue, value);
        }

        public string PropertyName
        {
            get => propertyName;
            set => SetProperty(ref propertyName, value);
        }

        public string IconImagePath
        {
            get => iconImagePath;
            set => SetProperty(ref iconImagePath, value);
        }
        
        public bool IsSliderEnabled
        {
            get => isSliderEnabled;
            set => SetProperty(ref isSliderEnabled, value);
        }
    }
}
