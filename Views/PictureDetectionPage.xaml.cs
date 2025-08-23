using ClientApp.Core.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace ClientApp.Views;

public partial class PictureDetectionPage : ContentPage
{
    private readonly PictureDetectionViewModel _viewModel;

    public PictureDetectionPage(PictureDetectionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PictureDetectionViewModel.Detections) ||
                e.PropertyName == nameof(PictureDetectionViewModel.SelectedImage))
            {
                canvasView.InvalidateSurface();
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }

    void OnCanvasViewPaintSurface(object? sender, SKPaintSurfaceEventArgs args)
    {
        var detections = _viewModel.Detections;
        if (detections == null || detections.Count == 0 || _viewModel.ImageSize.IsZero)
        {
            args.Surface.Canvas.Clear();
            return;
        }

        var info = args.Info;
        var canvas = args.Surface.Canvas;
        canvas.Clear();

        var imageSize = _viewModel.ImageSize;
        float scaleX = info.Width / (float)imageSize.Width;
        float scaleY = info.Height / (float)imageSize.Height;
        float scale = Math.Min(scaleX, scaleY);
        float offsetX = (info.Width - (float)imageSize.Width * scale) / 2f;
        float offsetY = (info.Height - (float)imageSize.Height * scale) / 2f;

        using var boxPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Red,
            StrokeWidth = 4
        };

        using var textPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.Black,
            TextSize = 30,
            IsAntialias = true
        };

        foreach (var box in detections)
        {
            float scaledX = offsetX + box.TopLeftX * scale;
            float scaledY = offsetY + box.TopLeftY * scale;
            float scaledWidth = (box.BottomRightX - box.TopLeftX) * scale;
            float scaledHeight = (box.BottomRightY - box.TopLeftY) * scale;

            var rect = new SKRect(scaledX, scaledY, scaledX + scaledWidth, scaledY + scaledHeight);
            canvas.DrawRect(rect, boxPaint);

            string label = $"{box.Label}: {box.Score:P1}";
            canvas.DrawText(label, scaledX, scaledY - 10, textPaint);
        }
    }
}
