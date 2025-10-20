#if ANDROID
using Android.Graphics;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using Java.Util.Concurrent;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;

namespace ClientApp.Platforms.Android.Services
{
    /// <summary>
    /// High-performance Android video frame provider using CameraX ImageAnalysis.
    /// Bypasses Camera.MAUI for direct frame access at native FPS.
    /// </summary>
    public class AndroidVideoFrameService : ClientApp.Core.Services.IVideoFrameService
    {
        private ICamera? _camera;
        private ProcessCameraProvider? _cameraProvider;
        private ImageAnalysis? _imageAnalysis;
        private IExecutorService? _cameraExecutor;
        private readonly object _lock = new();
        private int _targetWidth = 640;
        private int _targetHeight = 480;

        public event EventHandler<ClientApp.Core.Services.VideoFrameEventArgs>? FrameAvailable;
        public bool IsCapturing { get; private set; }

        public async Task<bool> StartCaptureAsync(int targetFps = 30)
        {
            if (IsCapturing) return true;

            try
            {
                var activity = Platform.CurrentActivity 
                    ?? throw new InvalidOperationException("No current activity");

                _cameraExecutor = Executors.NewSingleThreadExecutor();

                // Get CameraProvider
                var cameraProviderFuture = ProcessCameraProvider.GetInstance(activity);
                _cameraProvider = await cameraProviderFuture.GetAsync() as ProcessCameraProvider
                    ?? throw new InvalidOperationException("Failed to get CameraProvider");

                // Unbind all previous use cases
                _cameraProvider.UnbindAll();

                // Create Preview (for UI display - optional)
                var preview = new Preview.Builder().Build();

                // Create ImageAnalysis for ML processing
                _imageAnalysis = new ImageAnalysis.Builder()
                    .SetTargetResolution(new global::Android.Util.Size(_targetWidth, _targetHeight))
                    .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest) // Drop frames if processing is slow
                    .SetOutputImageFormat(ImageAnalysis.OutputImageFormatRgba8888) // Use RGBA instead of YUV
                    .Build();

                // Set analyzer
                _imageAnalysis.SetAnalyzer(_cameraExecutor, new FrameAnalyzer(this));

                // Select back camera
                var cameraSelector = new CameraSelector.Builder()
                    .RequireLensFacing(CameraSelector.LensFacingBack)
                    .Build();

                // Bind to lifecycle
                var lifecycleOwner = activity as ILifecycleOwner;
                if (lifecycleOwner == null)
                    throw new InvalidOperationException("Current activity does not implement ILifecycleOwner. CameraX requires an activity that implements ILifecycleOwner.");

                _camera = _cameraProvider.BindToLifecycle(
                    lifecycleOwner,
                    cameraSelector,
                    preview,
                    _imageAnalysis
                );

                IsCapturing = true;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AndroidVideoFrameService StartCapture error: {ex}");
                return false;
            }
        }

        public Task StopCaptureAsync()
        {
            lock (_lock)
            {
                _cameraProvider?.UnbindAll();
                _cameraExecutor?.Shutdown();
                _camera = null;
                _imageAnalysis = null;
                IsCapturing = false;
            }
            return Task.CompletedTask;
        }

        public Task<ClientApp.Core.Services.VideoResolution[]> GetAvailableResolutionsAsync()
        {
            return Task.FromResult(new[]
            {
                ClientApp.Core.Services.VideoResolution.Low,
                ClientApp.Core.Services.VideoResolution.Medium,
                ClientApp.Core.Services.VideoResolution.High
            });
        }

        public Task SetResolutionAsync(ClientApp.Core.Services.VideoResolution resolution)
        {
            _targetWidth = resolution.Width;
            _targetHeight = resolution.Height;

            // Restart capture if active
            if (IsCapturing)
            {
                return Task.Run(async () =>
                {
                    await StopCaptureAsync();
                    await StartCaptureAsync();
                });
            }
            return Task.CompletedTask;
        }

        private void OnFrameReceived(IImageProxy imageProxy)
        {
            try
            {
                // Convert YUV to RGBA (or pass YUV directly to YOLO if it supports it)
                var planes = imageProxy.GetPlanes();
                if (planes == null || planes.Length == 0) return;

                int width = imageProxy.Width;
                int height = imageProxy.Height;
                int rotation = imageProxy.ImageInfo?.RotationDegrees ?? 0;

                // YUV420 to RGB conversion (simplified - you may need NV21ToRGB helper)
                byte[] rgbaData = ConvertYuvToRgba(imageProxy, planes, width, height);

                var frameArgs = new ClientApp.Core.Services.VideoFrameEventArgs
                {
                    Data = rgbaData,
                    Width = width,
                    Height = height,
                    PixelFormat = "RGBA32",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Rotation = rotation
                };

                // Fire event on background thread to avoid blocking camera
                Task.Run(() => FrameAvailable?.Invoke(this, frameArgs));
            }
            finally
            {
                imageProxy.Close();
            }
        }

        private byte[] ConvertYuvToRgba(IImageProxy imageProxy, IImageProxyPlaneProxy[] planes, int width, int height)
        {
            // Fast YUV420 (NV21) to RGBA conversion
            var yPlane = planes[0];
            var uPlane = planes[1];
            var vPlane = planes[2];

            var yBuffer = yPlane.Buffer;
            var uBuffer = uPlane.Buffer;
            var vBuffer = vPlane.Buffer;

            int ySize = yBuffer?.Remaining() ?? 0;
            int uSize = uBuffer?.Remaining() ?? 0;
            int vSize = vBuffer?.Remaining() ?? 0;

            byte[] nv21 = new byte[ySize + uSize + vSize];

            yBuffer?.Get(nv21, 0, ySize);
            vBuffer?.Get(nv21, ySize, vSize); // V before U for NV21
            uBuffer?.Get(nv21, ySize + vSize, uSize);

            // Convert NV21 to RGBA
            byte[] rgba = new byte[width * height * 4];
            Nv21ToRgba(nv21, rgba, width, height);

            return rgba;
        }

        private void Nv21ToRgba(byte[] nv21, byte[] rgba, int width, int height)
        {
            int frameSize = width * height;
            int yIndex = 0;
            int uvIndex = frameSize;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int Y = nv21[yIndex++] & 0xff;
                    int V = nv21[uvIndex + (y / 2) * width + (x & ~1)] & 0xff;
                    int U = nv21[uvIndex + (y / 2) * width + (x & ~1) + 1] & 0xff;

                    // YUV to RGB conversion
                    int R = (int)(Y + 1.370705f * (V - 128));
                    int G = (int)(Y - 0.337633f * (U - 128) - 0.698001f * (V - 128));
                    int B = (int)(Y + 1.732446f * (U - 128));

                    // Clamp values
                    R = Math.Clamp(R, 0, 255);
                    G = Math.Clamp(G, 0, 255);
                    B = Math.Clamp(B, 0, 255);

                    int rgbaIndex = (y * width + x) * 4;
                    rgba[rgbaIndex] = (byte)R;
                    rgba[rgbaIndex + 1] = (byte)G;
                    rgba[rgbaIndex + 2] = (byte)B;
                    rgba[rgbaIndex + 3] = 255; // Alpha
                }
            }
        }

        /// <summary>
        /// Internal analyzer class that receives frames from CameraX.
        /// </summary>
        private class FrameAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
        {
            private readonly AndroidVideoFrameService _service;

            public FrameAnalyzer(AndroidVideoFrameService service)
            {
                _service = service;
            }

            public void Analyze(IImageProxy imageProxy)
            {
                _service.OnFrameReceived(imageProxy);
            }
        }
    }
}
#endif
