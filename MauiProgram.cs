using Microsoft.Extensions.Logging;
using ClientApp.Core.Detection;
using Camera.MAUI;
using ClientApp.Views;
using ClientApp.Core.ViewModels;
using CommunityToolkit.Maui;
using SkiaSharp.Views.Maui.Controls.Hosting;
using ClientApp.Core.Services;
using ClientApp.Core.Data;
using ClientApp.Core.Domain;

namespace ClientApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCameraView()
                .UseMauiCommunityToolkit()
                .UseSkiaSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Core services
            builder.Services.AddSingleton<IObjectDetector, YoloDetector>();
            builder.Services.AddSingleton<ILocationService, LocationService>();
            builder.Services.AddSingleton<ISightingRepository, InMemorySightingRepository>();
            builder.Services.AddSingleton<INavigationService, ShellNavigationService>();

            // High-performance video frame service (platform-specific)
#if ANDROID
            builder.Services.AddSingleton<IVideoFrameService, ClientApp.Platforms.Android.Services.AndroidVideoFrameService>();
#elif IOS
            // TODO: Implement iOS version
            // builder.Services.AddSingleton<IVideoFrameService, ClientApp.Platforms.iOS.Services.iOSVideoFrameService>();
#else
            // Fallback for other platforms
            // builder.Services.AddSingleton<IVideoFrameService, FallbackVideoFrameService>();
#endif

            // ViewModels
            builder.Services.AddTransient<MainPageViewModel>();
            builder.Services.AddTransient<CameraDetectionViewModel>();
            builder.Services.AddTransient<MapPageViewModel>();
            builder.Services.AddTransient<PictureDetectionViewModel>();

            // Pages
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddTransient<CameraDetectionPage>(); // Old version
            builder.Services.AddTransient<CameraDetectionPageV2>(); // New optimized version
            builder.Services.AddTransient<MapPage>();
            builder.Services.AddTransient<PictureDetectionPage>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
