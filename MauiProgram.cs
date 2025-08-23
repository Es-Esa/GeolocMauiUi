using Microsoft.Extensions.Logging;
using ClientApp.Core.Detection;
using Camera.MAUI;
using ClientApp.Views;
using ClientApp.Core.ViewModels;
using CommunityToolkit.Maui;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Microsoft.Maui.Controls.Maps;
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
                .UseMauiMaps()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<IObjectDetector, YoloDetector>();
            builder.Services.AddSingleton<ILocationService, LocationService>();
            builder.Services.AddSingleton<ISightingRepository, InMemorySightingRepository>();
            builder.Services.AddSingleton<INavigationService, ShellNavigationService>();

            builder.Services.AddTransient<MainPageViewModel>();
            builder.Services.AddTransient<CameraDetectionViewModel>();
            builder.Services.AddTransient<MapPageViewModel>();
            builder.Services.AddTransient<PictureDetectionViewModel>();

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddTransient<CameraDetectionPage>();
            builder.Services.AddTransient<MapPage>();
            builder.Services.AddTransient<PictureDetectionPage>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
