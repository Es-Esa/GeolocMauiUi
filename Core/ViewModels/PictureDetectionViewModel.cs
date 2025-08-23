using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using ClientApp.Core.Detection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;

namespace ClientApp.Core.ViewModels;

public partial class PictureDetectionViewModel : BaseViewModel
{
    private readonly IObjectDetector _objectDetector;
    private bool _isModelInitialized;
    private Stream? _selectedImageStream;

    [ObservableProperty]
    private ImageSource? selectedImage;

    [ObservableProperty]
    private bool isSelectImageEnabled;

    [ObservableProperty]
    private bool isAnalyzeEnabled;

    public ObservableCollection<string> Results { get; } = new();

    [ObservableProperty]
    private List<YoloBoundingBox> detections = new();

    [ObservableProperty]
    private Size imageSize;

    public PictureDetectionViewModel(IObjectDetector objectDetector)
    {
        _objectDetector = objectDetector;
        Title = "Image Detection";
    }

    public async Task InitializeAsync()
    {
        if (_isModelInitialized) return;
        IsBusy = true;
        IsSelectImageEnabled = false;
        IsAnalyzeEnabled = false;
        try
        {
            await _objectDetector.InitializeAsync();
            _isModelInitialized = true;
            IsSelectImageEnabled = true;
            await Shell.Current.DisplayAlert("Success", "Model initialized successfully.", "OK");
        }
        catch (Exception ex)
        {
            _isModelInitialized = false;
            await Shell.Current.DisplayAlert("Error", $"Model initialization failed: {ex}", "OK");
        }
        finally
        {
            IsBusy = false;
            IsAnalyzeEnabled = _isModelInitialized && _selectedImageStream != null;
            SelectImageCommand.NotifyCanExecuteChanged();
            AnalyzeCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanSelectImage))]
    private async Task SelectImageAsync()
    {
        if (!MediaPicker.Default.IsCaptureSupported)
        {
            await Shell.Current.DisplayAlert("Error", "Photo capture/picking may not be fully supported on this device.", "OK");
            return;
        }

        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo != null)
            {
                Results.Clear();
                _selectedImageStream?.Dispose();
                var photoStream = await photo.OpenReadAsync();
                using var ms = new MemoryStream();
                await photoStream.CopyToAsync(ms);
                photoStream.Dispose();
                var imageBytes = ms.ToArray();
                _selectedImageStream = new MemoryStream(imageBytes);
                using (var img = PlatformImage.FromStream(new MemoryStream(imageBytes)))
                {
                    ImageSize = new Size(img.Width, img.Height);
                }
                SelectedImage = ImageSource.FromStream(() => new MemoryStream(imageBytes));
                Detections = new List<YoloBoundingBox>();
                IsAnalyzeEnabled = _isModelInitialized;
            }
            else
            {
                ClearSelectedImage();
            }
        }
        catch (PermissionException pEx)
        {
            await Shell.Current.DisplayAlert("Permission Error", $"Permission needed to access photos: {pEx.Message}", "OK");
            ClearSelectedImage();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to select image: {ex.Message}", "OK");
            ClearSelectedImage();
        }
        finally
        {
            AnalyzeCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanSelectImage() => !IsBusy && _isModelInitialized;

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeAsync()
    {
        if (!_isModelInitialized)
        {
            await Shell.Current.DisplayAlert("Error", "Model is not initialized.", "OK");
            return;
        }
        if (_selectedImageStream == null || !_selectedImageStream.CanRead || _selectedImageStream.Length == 0)
        {
            await Shell.Current.DisplayAlert("Error", "Please select a valid image first.", "OK");
            return;
        }

        IsBusy = true;
        IsSelectImageEnabled = false;
        IsAnalyzeEnabled = false;
        Results.Clear();

        try
        {
            _selectedImageStream.Position = 0;
            var detections = await _objectDetector.DetectAsync(_selectedImageStream);
            Detections = detections?.ToList() ?? new List<YoloBoundingBox>();
            if (Detections.Any())
            {
                foreach (var detection in Detections)
                {
                    string resultString = $"{detection.Label}: {detection.Score:P1} [X:{detection.TopLeftX},Y:{detection.TopLeftY},W:{detection.BottomRightX - detection.TopLeftX},H:{detection.BottomRightY - detection.TopLeftY}]";
                    Results.Add(resultString);
                }
            }
            else
            {
                Results.Add("No detections found.");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Analysis failed: {ex}", "OK");
            Results.Add("Analysis error.");
        }
        finally
        {
            IsBusy = false;
            IsSelectImageEnabled = true;
            IsAnalyzeEnabled = true;
            if (_selectedImageStream != null && _selectedImageStream.CanSeek)
            {
                _selectedImageStream.Position = 0;
            }
            SelectImageCommand.NotifyCanExecuteChanged();
            AnalyzeCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanAnalyze() => !IsBusy && _isModelInitialized && _selectedImageStream != null;

    private void ClearSelectedImage()
    {
        _selectedImageStream?.Dispose();
        _selectedImageStream = null;
        SelectedImage = null;
        Detections = new List<YoloBoundingBox>();
        ImageSize = Size.Zero;
        IsAnalyzeEnabled = false;
        AnalyzeCommand.NotifyCanExecuteChanged();
    }

    public void OnDisappearing()
    {
        _selectedImageStream?.Dispose();
        _selectedImageStream = null;
        Detections = new List<YoloBoundingBox>();
    }

    protected override void OnIsBusyChanged(bool value)
    {
        SelectImageCommand.NotifyCanExecuteChanged();
        AnalyzeCommand.NotifyCanExecuteChanged();
    }
}

