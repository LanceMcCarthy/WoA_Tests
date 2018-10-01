using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.Effects;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;
using VideoEffectTest.Uwp.Common;

namespace VideoEffectTest.Uwp
{
    public sealed partial class MainPage : Page
    {
        private RecordingState currentState = RecordingState.NotInitialized;
        private MediaCapture mediaCapture;
        private DeviceInformation selectedCamera;
        private IVideoEffectDefinition previewEffect;
        private IPropertySet effectPropertySet;

        public MainPage()
        {
            InitializeComponent();
        }
        
        #region Event Handlers

        private async void EffectsListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PageViewModel.SelectedEffect == null)
                return;
            
            await ClearVideoEffectAsync();
            await ApplyVideoEffectAsync();
        }
        
        private void SelectedEffectSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e) => UpdateVideoEffectPropertyValue(e.NewValue);

        private async void ClearEffectButton_OnClick(object sender, RoutedEventArgs e)
        {
            await ClearVideoEffectAsync();

            PageViewModel.SelectedEffect = null;
        }
        
        private async void ReloadVideoStreamButton_OnClick(object sender, RoutedEventArgs e) => await InitializeVideoAsync();
        

        #endregion

        #region Video effect management

        private async Task ApplyVideoEffectAsync()
        {
            if (currentState == RecordingState.Previewing)
            {
                previewEffect = ConstructVideoEffectDefinition();

                await mediaCapture.AddVideoEffectAsync(previewEffect, MediaStreamType.VideoPreview);
            }
            else if (currentState == RecordingState.NotInitialized || currentState == RecordingState.Stopped)
            {
                await new MessageDialog("The preview or recording stream is not available.", "Effect not applied").ShowAsync();
            }
        }

        private IVideoEffectDefinition ConstructVideoEffectDefinition()
        {
            if (string.IsNullOrEmpty(PageViewModel.SelectedEffect.VideoEffect.FullName))
                return null;

            if (string.IsNullOrEmpty(PageViewModel.SelectedEffect.PropertyName))
                return new VideoEffectDefinition(PageViewModel.SelectedEffect.VideoEffect.FullName);

            effectPropertySet = new PropertySet
            {
                [PageViewModel.SelectedEffect.PropertyName] = PageViewModel.SelectedEffect.PropertyValue
            };

            return new VideoEffectDefinition(PageViewModel.SelectedEffect.VideoEffect.FullName, effectPropertySet);
        }

        private void UpdateVideoEffectPropertyValue(double value)
        {
            
            if (PageViewModel.SelectedEffect == null || effectPropertySet == null)
                return;

            // Update the value of the selected effect
            PageViewModel.SelectedEffect.PropertyValue = (float)value;

            effectPropertySet[PageViewModel.SelectedEffect.PropertyName] = (float)PageViewModel.SelectedEffect.PropertyValue;
        }

        private async Task ClearVideoEffectAsync()
        {
            await mediaCapture.ClearEffectsAsync(MediaStreamType.VideoPreview);
            previewEffect = null;
        }

        #endregion

        #region MediaCapture initialization and disposal

        private async Task InitializeVideoAsync()
        {
            ReloadVideoStreamButton.Visibility = Visibility.Collapsed;
            ShowBusyIndicator("Initializing...");

            try
            {
                currentState = RecordingState.NotInitialized;

                PreviewMediaElement.Source = null;

                ShowBusyIndicator("starting video device...");

                mediaCapture = new MediaCapture();

                App.MediaCaptureManager = mediaCapture;

                selectedCamera = await FindBestCameraAsync();

                if (selectedCamera == null)
                {
                    await new MessageDialog("There are no cameras connected, please connect a camera and try again.").ShowAsync();
                    await DisposeMediaCaptureAsync();
                    HideBusyIndicator();
                    return;
                }

                await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings { VideoDeviceId = selectedCamera.Id });

                if (mediaCapture.MediaCaptureSettings.VideoDeviceId != "" && mediaCapture.MediaCaptureSettings.AudioDeviceId != "")
                {
                    ShowBusyIndicator("camera initialized..");
                    
                    mediaCapture.Failed += async (currentCaptureObject, currentFailure) =>
                    {
                        await TaskUtilities.RunOnDispatcherThreadAsync(async () =>
                        {
                            await new MessageDialog(currentFailure.Message, "MediaCaptureFailed Fired").ShowAsync();

                            await DisposeMediaCaptureAsync();

                            ReloadVideoStreamButton.Visibility = Visibility.Visible;
                        });
                    };
                }
                else
                {
                    ShowBusyIndicator("camera error!");
                }

                //------starting preview----------//

                ShowBusyIndicator("starting preview...");

                PreviewMediaElement.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();

                currentState = RecordingState.Previewing;

            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"InitializeVideo UnauthorizedAccessException\r\n {ex}");

                ShowBusyIndicator("Unauthorized Access Error");

                await new MessageDialog("-----Unauthorized Access Error!-----\r\n\n" +
                                        "This can happen for a couple reasons:\r\n" +
                                        "-You have disabled Camera access to the app\r\n" +
                                        "-You have disabled Microphone access to the app\r\n\n" +
                                        "To fix this, go to Settings > Privacy > Camera (or Microphone) and reenable it.").ShowAsync();

                await DisposeMediaCaptureAsync();
            }
            catch (Exception ex)
            {
                ShowBusyIndicator("Initialize Video Error");
                await new MessageDialog("InitializeVideoAsync() Exception\r\n\nError Message: " + ex.Message).ShowAsync();

                currentState = RecordingState.NotInitialized;
                PreviewMediaElement.Source = null;
            }
            finally
            {
                HideBusyIndicator();
            }
        }

        private async Task DisposeMediaCaptureAsync()
        {
            try
            {
                ShowBusyIndicator("Freeing up resources...");

                switch (currentState)
                {
                    case RecordingState.Recording when mediaCapture != null:
                        ShowBusyIndicator("recording stopped...");
                        await mediaCapture.StopRecordAsync();
                        break;
                    case RecordingState.Previewing when mediaCapture != null:
                        ShowBusyIndicator("video preview stopped...");
                        await mediaCapture.StopPreviewAsync();
                        break;
                    case RecordingState.Stopped:
                        break;
                    case RecordingState.NotInitialized:
                        break;
                }

                currentState = RecordingState.Stopped;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DisposeAll Error: {ex.Message}");
                await new MessageDialog($"Error disposing MediaCapture: {ex.Message}").ShowAsync();
            }
            finally
            {
                if (mediaCapture != null)
                {
                    mediaCapture.Dispose();
                    mediaCapture = null;
                }

                PreviewMediaElement.Source = null;

                HideBusyIndicator();
            }
        }

        private static async Task<DeviceInformation> FindBestCameraAsync()
        {
            var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            Debug.WriteLine($"{devices.Count} devices found");

            // If there are no cameras connected to the device
            if (devices.Count == 0)
                return null;

            // If there is only one camera, return that one
            if (devices.Count == 1)
                return devices.FirstOrDefault();

            //check if the preferred device is available
            var frontCamera = devices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front);

            //if front camera is available return it, otherwise pick the first available camera
            return frontCamera ?? devices.FirstOrDefault();
        }
        
        #endregion

        #region Status messaging

        private void ShowBusyIndicator(string message)
        {
            PageViewModel.IsBusyMessage = message;
            PageViewModel.IsBusy = true;
        }

        private void HideBusyIndicator()
        {
            PageViewModel.IsBusyMessage = "";
            PageViewModel.IsBusy = false;
        }

        #endregion

        #region page lifecycle

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await InitializeVideoAsync();
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            await DisposeMediaCaptureAsync();

            base.OnNavigatedFrom(e);
        }

        #endregion
    }
}
