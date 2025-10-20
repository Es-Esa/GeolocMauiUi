using System;
using System.Threading.Tasks;

namespace ClientApp.Core.Services
{
    /// <summary>
    /// Platform-agnostic interface for high-performance video frame capture.
    /// Provides low-level camera frame access for real-time processing.
    /// </summary>
    public interface IVideoFrameService
    {
        /// <summary>
        /// Event fired when a new video frame is available.
        /// </summary>
        event EventHandler<VideoFrameEventArgs>? FrameAvailable;

        /// <summary>
        /// Start capturing video frames at the specified FPS.
        /// </summary>
        Task<bool> StartCaptureAsync(int targetFps = 30);

        /// <summary>
        /// Stop capturing video frames.
        /// </summary>
        Task StopCaptureAsync();

        /// <summary>
        /// Check if capture is currently active.
        /// </summary>
        bool IsCapturing { get; }

        /// <summary>
        /// Get available camera resolutions.
        /// </summary>
        Task<VideoResolution[]> GetAvailableResolutionsAsync();

        /// <summary>
        /// Set the capture resolution (lower = faster processing).
        /// </summary>
        Task SetResolutionAsync(VideoResolution resolution);
    }

    /// <summary>
    /// Video frame data with minimal overhead.
    /// </summary>
    public class VideoFrameEventArgs : EventArgs
    {
        /// <summary>
        /// Frame data as byte array (RGBA or YUV format depending on platform).
        /// </summary>
        public byte[] Data { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Frame width in pixels.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Frame height in pixels.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Pixel format (RGBA32, NV21, etc).
        /// </summary>
        public string PixelFormat { get; set; } = "RGBA32";

        /// <summary>
        /// Frame timestamp for synchronization.
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Frame rotation in degrees (0, 90, 180, 270).
        /// </summary>
        public int Rotation { get; set; }
    }

    /// <summary>
    /// Camera resolution presets optimized for ML processing.
    /// </summary>
    public class VideoResolution
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Name { get; set; } = string.Empty;

        // Optimized presets for YOLO
        public static VideoResolution Low => new() { Width = 320, Height = 240, Name = "Low (Fast)" };
        public static VideoResolution Medium => new() { Width = 640, Height = 480, Name = "Medium (Balanced)" };
        public static VideoResolution High => new() { Width = 1280, Height = 720, Name = "High (Quality)" };

        public override string ToString() => $"{Width}x{Height} ({Name})";
    }
}
