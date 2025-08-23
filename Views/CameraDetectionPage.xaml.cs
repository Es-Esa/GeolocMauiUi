using Camera.MAUI;
using ClientApp.Core.Detection;
using Microsoft.Maui.Graphics;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Dispatching;
using System.Linq;
using Microsoft.Maui.Graphics.Platform;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using ClientApp.Core.Domain;
using ClientApp.Core.Services;
using ClientApp.Core.Data;
using Microsoft.Maui.Devices.Sensors;
using ClientApp.Core.ViewModels;

namespace ClientApp.Views
{
    /// <summary>
    /// Displays a live camera feed and runs object detection.
    /// </summary>
    public partial class CameraDetectionPage : ContentPage
    {
        private readonly IObjectDetector _objectDetector;
        private readonly ILocationService _locationService;
        private readonly ISightingRepository _sightingRepository;
        private readonly CameraDetectionViewModel _viewModel;
        private readonly INavigationService _navigationService;
        private bool _isProcessingFrame = false;
        private bool _isDetectorInitialized = false;
        private bool _isCameraStarted = false;
        private System.Threading.Timer? _frameProcessingTimer;
        private const int FrameProcessingIntervalMs = 100;
        private List<YoloBoundingBox> _currentDetections = new List<YoloBoundingBox>();
        private Size _cameraFeedSize = Size.Zero;
        private Size _frameSize = Size.Zero;


        /// <summary>
        /// Initialize page with required services.
        /// </summary>
        public CameraDetectionPage(CameraDetectionViewModel viewModel, IObjectDetector objectDetector, ILocationService locationService, ISightingRepository sightingRepository, INavigationService navigationService)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
            _objectDetector = objectDetector;
            _locationService = locationService;
            _sightingRepository = sightingRepository;
            _navigationService = navigationService;
        }

        /// <summary>
        /// Request permissions and start detector when the page appears.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.IsBusy = true;
            _currentDetections.Clear();
            canvasView.InvalidateSurface();

            await RequestCameraPermission();
            if (!_isDetectorInitialized)
            {
                InitializeDetectorAsync();
            }

            if (cameraView.Camera == null && cameraView.Cameras.Count > 0)
            {
                cameraView.Camera = cameraView.Cameras.FirstOrDefault(c => c.Position == CameraPosition.Back);
                cameraView.Camera ??= cameraView.Cameras.FirstOrDefault();
            }

            if (cameraView.Camera != null)
            {
                var result = await cameraView.StartCameraAsync();
                if (result == CameraResult.Success)
                {
                    _isCameraStarted = true;
                    statusLabel.Text = "Camera Started. Detecting...";
                    StartFrameProcessingLoop();
                }
            }
        }

        /// <summary>
        /// Stop camera and processing when the page closes.
        /// </summary>
        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            _frameProcessingTimer?.Dispose();
            _isCameraStarted = false;
            _currentDetections.Clear();
            _isProcessingFrame = false;
            MainThread.BeginInvokeOnMainThread(() => canvasView.InvalidateSurface());

            if (cameraView.Camera != null)
            {
                await cameraView.StopCameraAsync();
            }
        }

        /// <summary>
        /// Ensure camera permission is granted.
        /// </summary>
        private async Task RequestCameraPermission()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Permission Error", "Camera permission is required for live detection.", "OK");  
                    await _navigationService.GoToAsync("..");
                    return;
                }
            }
        }

        /// <summary>
        /// Initialize the object detector.
        /// </summary>
        private async void InitializeDetectorAsync()
        {
            try
            {
                statusLabel.Text = "Initializing Detector...";
                await _objectDetector.InitializeAsync();
                _isDetectorInitialized = true;
                statusLabel.Text = "Detector Initialized. Waiting for Camera...";
                StartFrameProcessingLoop();
            }
            catch (Exception ex)
            {
                _isDetectorInitialized = false;
                statusLabel.Text = "Detector Init Failed";
                System.Diagnostics.Debug.WriteLine($"Detector initialization failed: {ex}");
                await DisplayAlert("Error", $"Detector initialization failed: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Start the selected camera once it is loaded.
        /// </summary>
        private async void CameraView_CamerasLoaded(object? sender, EventArgs e)
        {
            if (cameraView.Cameras.Count > 0)
            {
                cameraView.Camera = cameraView.Cameras.FirstOrDefault(c => c.Position == CameraPosition.Back);
                cameraView.Camera ??= cameraView.Cameras.FirstOrDefault();

                if (cameraView.Camera != null)
                {
                    await cameraView.StopCameraAsync();
                    var result = await cameraView.StartCameraAsync();
                    if (result == CameraResult.Success)
                    {
                        _isCameraStarted = true;
                        statusLabel.Text = "Camera Started. Detecting...";
                        StartFrameProcessingLoop();
                    }
                    else
                    {
                        statusLabel.Text = "Failed to start camera.";
                        await DisplayAlert("Camera Error", "Could not start camera.", "OK");
                    }
                }
                else
                {
                    statusLabel.Text = "No suitable camera found.";
                    await DisplayAlert("Camera Error", "No suitable camera found on this device.", "OK");
                }
            }
            else
            {
                 statusLabel.Text = "No cameras found.";
            }
        }

        /// <summary>
        /// Begin periodic frame processing.
        /// </summary>
        private void StartFrameProcessingLoop()
        {
            if (!_isDetectorInitialized || !_isCameraStarted)
            {
                return;
            }

            _frameProcessingTimer?.Dispose();
            _frameProcessingTimer = new System.Threading.Timer(ProcessFrame, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(FrameProcessingIntervalMs));
            _viewModel.IsBusy = false;
            statusLabel.Text = "Detecting...";
        }

        /// <summary>
        /// Capture a frame, run detection, and log sightings.
        /// </summary>
        private async void ProcessFrame(object? state)
        {
            if (!_isProcessingFrame && _isDetectorInitialized && cameraView.Camera != null)
            {
                _isProcessingFrame = true;
                Stream? photoStream = null;
                MemoryStream? processingStream = null;
                Microsoft.Maui.Graphics.IImage? image = null;

                try
                {
                    
                    photoStream = await MainThread.InvokeOnMainThreadAsync(() => cameraView.TakePhotoAsync(Camera.MAUI.ImageFormat.JPEG));

                    if (photoStream != null && photoStream.Length > 0)
                    {
                       
                        processingStream = new MemoryStream();
                        await photoStream.CopyToAsync(processingStream);
                        processingStream.Position = 0;

                        
                        image = PlatformImage.FromStream(processingStream);
                        if (image != null)
                        {
                           
                            _cameraFeedSize = new Size(image.Width, image.Height);
                            _frameSize = new Size(canvasView.Width, canvasView.Height);
                        }
                        else
                        {
                             
                             System.Diagnostics.Debug.WriteLine("Warning: Could not read image dimensions from stream.");
                            _cameraFeedSize = new Size(cameraView.Width, cameraView.Height); 
                            _frameSize = new Size(canvasView.Width, canvasView.Height);
                        }
                        processingStream.Position = 0; 

                        
                        var detections = await _objectDetector.DetectAsync(processingStream);
                        
                       
                        bool personDetected = false;
                        if (detections != null)
                        {
                            foreach (var box in detections)
                            {
                                if (box.Label == "person")
                                {
                                    personDetected = true;
                                  
                                    break; 
                                }
                            }
                        }

                        if (personDetected)
                        {
                            System.Diagnostics.Debug.WriteLine("Person detected, attempting to get location...");
                         
                            Location? currentLocation = await MainThread.InvokeOnMainThreadAsync(async () => 
                                await _locationService.GetCurrentLocationAsync()
                            );

                            if (currentLocation != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"Location found: Lat {currentLocation.Latitude}, Lon {currentLocation.Longitude}");

                                var sighting = new Sighting
                                {
                                    Location = currentLocation,
                                    ObservationType = Core.Enums.ObservationType.Human,
                                    
                                    Confidence = detections?.Where(d => d.Label == "person").Max(d => d.Score) ?? 0f 
                                };
                                await _sightingRepository.AddSightingAsync(sighting);
                                System.Diagnostics.Debug.WriteLine("Sighting added to repository.");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("Could not get location for person sighting.");
                            }
                        }
                       
                        _currentDetections = detections ?? new List<YoloBoundingBox>();
                        MainThread.BeginInvokeOnMainThread(() => canvasView.InvalidateSurface());
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing frame: {ex.Message}");
                }
                finally
                {
                    
                    photoStream?.Dispose();
                    processingStream?.Dispose(); 
                    (image as IDisposable)?.Dispose(); 
                    _isProcessingFrame = false;
                }
            }
        }

        /// <summary>
        /// Draw detection boxes and labels on the canvas.
        /// </summary>
        void OnCanvasViewPaintSurface(object? sender, SKPaintSurfaceEventArgs args)
        {
           
            SKImageInfo info = args.Info;
            SKSurface surface = args.Surface;
            SKCanvas canvas = surface.Canvas;

            canvas.Clear();

            if (!_currentDetections.Any() || _frameSize.IsZero || _cameraFeedSize.IsZero)
            {
                return; 
            }

            _frameSize = new Size(info.Width, info.Height); 

           
            float scaleX = (float)(_frameSize.Width / _cameraFeedSize.Width);
            float scaleY = (float)(_frameSize.Height / _cameraFeedSize.Height);
            float scale = Math.Min(scaleX, scaleY);
            float offsetX = (float)((_frameSize.Width - (_cameraFeedSize.Width * scale)) / 2.0);
            float offsetY = (float)((_frameSize.Height - (_cameraFeedSize.Height * scale)) / 2.0);

           
            using var boxPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Red,
                StrokeWidth = 4
            };
            using var textPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.Yellow,
                TextSize = 30, 
                IsAntialias = true
            };

            foreach (var box in _currentDetections)
            {
                float scaledX = offsetX + (box.TopLeftX * scale);
                float scaledY = offsetY + (box.TopLeftY * scale);
                float scaledWidth = (box.BottomRightX - box.TopLeftX) * scale;
                float scaledHeight = (box.BottomRightY - box.TopLeftY) * scale;
                
                var skRect = new SKRect(scaledX, scaledY, scaledX + scaledWidth, scaledY + scaledHeight);

                canvas.DrawRect(skRect, boxPaint);

                string label = $"{box.Label}: {box.Score:P1}";
                canvas.DrawText(label, scaledX, scaledY - 10, textPaint);
            }
        }
    }
} 