using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ClientApp.Core.Detection;

namespace ClientApp.Core.Services
{
    /// <summary>
    /// Asynchronous YOLO processing queue with frame skipping and throttling.
    /// Processes frames in background thread to avoid UI blocking.
    /// </summary>
    public class AsyncYoloProcessor : IDisposable
    {
        private readonly IObjectDetector _detector;
        private readonly ConcurrentQueue<VideoFrameEventArgs> _frameQueue = new();
        private readonly SemaphoreSlim _processingLock = new(1, 1);
        private CancellationTokenSource? _cancellationSource;
        private Task? _processingTask;
        private bool _isProcessing;
        private int _framesDropped;
        private int _framesProcessed;
        private DateTime _lastStatsTime = DateTime.UtcNow;

        /// <summary>
        /// Maximum frames to keep in queue (older frames are dropped).
        /// </summary>
        public int MaxQueueSize { get; set; } = 2;

        /// <summary>
        /// Process only every Nth frame (1 = all frames, 2 = every other, etc).
        /// </summary>
        public int FrameSkipFactor { get; set; } = 1;

        /// <summary>
        /// Event fired when detections are ready (always on background thread).
        /// </summary>
        public event EventHandler<DetectionResultEventArgs>? DetectionReady;

        /// <summary>
        /// Event fired when processing stats are updated.
        /// </summary>
        public event EventHandler<ProcessingStatsEventArgs>? StatsUpdated;

        private int _frameCounter;

        public AsyncYoloProcessor(IObjectDetector detector)
        {
            _detector = detector ?? throw new ArgumentNullException(nameof(detector));
        }

        /// <summary>
        /// Start the background processing loop.
        /// </summary>
        public void Start()
        {
            if (_isProcessing) return;

            _cancellationSource = new CancellationTokenSource();
            _isProcessing = true;
            _frameCounter = 0;
            _framesDropped = 0;
            _framesProcessed = 0;
            _lastStatsTime = DateTime.UtcNow;

            _processingTask = Task.Run(ProcessingLoop, _cancellationSource.Token);
        }

        /// <summary>
        /// Stop the background processing loop.
        /// </summary>
        public async Task StopAsync()
        {
            if (!_isProcessing) return;

            _cancellationSource?.Cancel();
            _isProcessing = false;

            if (_processingTask != null)
            {
                await _processingTask.ConfigureAwait(false);
            }

            // Clear queue
            while (_frameQueue.TryDequeue(out _)) { }
        }

        /// <summary>
        /// Queue a frame for processing. Drops oldest frames if queue is full.
        /// </summary>
        public void EnqueueFrame(VideoFrameEventArgs frame)
        {
            if (!_isProcessing) return;

            _frameCounter++;

            // Frame skipping
            if (FrameSkipFactor > 1 && _frameCounter % FrameSkipFactor != 0)
            {
                _framesDropped++;
                return;
            }

            // Drop oldest frame if queue is full
            if (_frameQueue.Count >= MaxQueueSize)
            {
                if (_frameQueue.TryDequeue(out _))
                {
                    _framesDropped++;
                }
            }

            _frameQueue.Enqueue(frame);
        }

        private async Task ProcessingLoop()
        {
            while (!_cancellationSource?.Token.IsCancellationRequested ?? false)
            {
                try
                {
                    if (_frameQueue.TryDequeue(out var frame))
                    {
                        await ProcessFrameAsync(frame).ConfigureAwait(false);
                    }
                    else
                    {
                        // No frames, sleep briefly
                        await Task.Delay(10, _cancellationSource.Token).ConfigureAwait(false);
                    }

                    // Update stats every second
                    if ((DateTime.UtcNow - _lastStatsTime).TotalSeconds >= 1.0)
                    {
                        UpdateStats();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AsyncYoloProcessor error: {ex}");
                }
            }
        }

        private async Task ProcessFrameAsync(VideoFrameEventArgs frame)
        {
            await _processingLock.WaitAsync().ConfigureAwait(false);
            try
            {
                var startTime = DateTime.UtcNow;

                // Convert frame to stream for YOLO (supports RGBA and JPEG)
                using var stream = ConvertFrameToStream(frame);

                int frameWidth = frame.Width;
                int frameHeight = frame.Height;
                if ((frameWidth <= 0 || frameHeight <= 0) || string.Equals(frame.PixelFormat, "JPEG", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using var skStream = new SkiaSharp.SKManagedStream(stream, false);
                        using var codec = SkiaSharp.SKCodec.Create(skStream);
                        if (codec != null)
                        {
                            var info = codec.Info;
                            frameWidth = info.Width;
                            frameHeight = info.Height;
                        }
                        // Reset stream position for detection after probing
                        stream.Position = 0;
                    }
                    catch
                    {
                        // If probing fails, keep original dimensions
                        stream.Position = 0;
                    }
                }

                // Run YOLO detection
                var detections = await _detector.DetectAsync(stream).ConfigureAwait(false);

                var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _framesProcessed++;

                // Fire event with results
                var resultArgs = new DetectionResultEventArgs
                {
                    Detections = detections ?? new List<YoloBoundingBox>(),
                    FrameWidth = frameWidth,
                    FrameHeight = frameHeight,
                    ProcessingTimeMs = processingTime,
                    Timestamp = frame.Timestamp
                };

                DetectionReady?.Invoke(this, resultArgs);
            }
            finally
            {
                _processingLock.Release();
            }
        }

        private MemoryStream ConvertFrameToStream(VideoFrameEventArgs frame)
        {
            // If we already have a JPEG payload, return it directly
            if (string.Equals(frame.PixelFormat, "JPEG", StringComparison.OrdinalIgnoreCase))
            {
                return new MemoryStream(frame.Data, writable: false);
            }

            // Otherwise convert RGBA byte array to JPEG stream for YOLO
            using var bitmap = new SkiaSharp.SKBitmap(frame.Width, frame.Height, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Premul);

            unsafe
            {
                fixed (byte* ptr = frame.Data)
                {
                    bitmap.SetPixels((IntPtr)ptr);
                }
            }

            // Rotate if needed
            if (frame.Rotation != 0)
            {
                using var rotated = new SkiaSharp.SKBitmap(frame.Height, frame.Width);
                using var canvas = new SkiaSharp.SKCanvas(rotated);
                canvas.Translate(frame.Height / 2f, frame.Width / 2f);
                canvas.RotateDegrees(frame.Rotation);
                canvas.Translate(-frame.Width / 2f, -frame.Height / 2f);
                canvas.DrawBitmap(bitmap, 0, 0);

                return EncodeToJpegStream(rotated);
            }

            return EncodeToJpegStream(bitmap);
        }

        private MemoryStream EncodeToJpegStream(SkiaSharp.SKBitmap bitmap)
        {
            var stream = new MemoryStream();
            using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 85); // 85% quality for speed
            data.SaveTo(stream);
            stream.Position = 0;
            return stream;
        }

        private void UpdateStats()
        {
            var elapsed = (DateTime.UtcNow - _lastStatsTime).TotalSeconds;
            var fps = _framesProcessed / elapsed;
            var dropRate = _framesDropped / elapsed;

            StatsUpdated?.Invoke(this, new ProcessingStatsEventArgs
            {
                ProcessingFps = fps,
                FramesDroppedPerSecond = dropRate,
                QueueSize = _frameQueue.Count
            });

            _framesProcessed = 0;
            _framesDropped = 0;
            _lastStatsTime = DateTime.UtcNow;
        }

        public void Dispose()
        {
            StopAsync().Wait();
            _processingLock?.Dispose();
            _cancellationSource?.Dispose();
        }
    }

    /// <summary>
    /// Detection results event args.
    /// </summary>
    public class DetectionResultEventArgs : EventArgs
    {
        public List<YoloBoundingBox> Detections { get; set; } = new();
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
        public double ProcessingTimeMs { get; set; }
        public long Timestamp { get; set; }
    }

    /// <summary>
    /// Processing statistics event args.
    /// </summary>
    public class ProcessingStatsEventArgs : EventArgs
    {
        public double ProcessingFps { get; set; }
        public double FramesDroppedPerSecond { get; set; }
        public int QueueSize { get; set; }
    }
}
