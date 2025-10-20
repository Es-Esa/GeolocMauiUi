using ClientApp.Core.ViewModels;
using ClientApp.Core.Data;
using ClientApp.Core.Domain;
using Mapsui;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using System;
using System.Linq;

namespace ClientApp.Views;

public partial class MapPage : ContentPage
{
    private readonly MapPageViewModel _viewModel;
    private readonly ISightingRepository _sightingRepository;

    public MapPage(MapPageViewModel viewModel, ISightingRepository sightingRepository)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _sightingRepository = sightingRepository;

        mapView.Map?.Layers.Add(OpenStreetMap.CreateTileLayer());
        mapView.PinClicked += OnPinClicked;
        
        // Subscribe to new sightings
        _sightingRepository.SightingAdded += OnSightingAdded;
    }

    private void OnPinClicked(object? sender, PinClickedEventArgs e)
    {
        if (e.Pin != null)
        {
            e.Handled = true;
            var label = e.Pin.Label ?? string.Empty;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                DisplayAlert("Sighting", label, "OK");
            });
        }
    }

    private void OnSightingAdded(object? sender, Sighting sighting)
    {
        // Add pin to map in real-time when detection occurs
        MainThread.BeginInvokeOnMainThread(() =>
        {
            AddPinForSighting(sighting);
        });
    }

    private void AddPinForSighting(Sighting sighting)
    {
        if (sighting.Location == null) return;

        var pin = new Pin(mapView)
        {
            Label = $"{sighting.ObservationType} ({sighting.Confidence:P0}) - {sighting.Timestamp:HH:mm:ss}",
            Position = new Position(sighting.Location.Latitude, sighting.Location.Longitude),
            Type = PinType.Pin,
            Color = sighting.ObservationType == Core.Enums.ObservationType.Human ? Colors.Red : Colors.Blue
        };
        mapView.Pins.Add(pin);

        // Auto-center map on new detection
        var (x, y) = SphericalMercator.FromLonLat(sighting.Location.Longitude, sighting.Location.Latitude);
        var center = new MPoint(x, y);
        var navigator = mapView.Map?.Navigator;
        if (navigator != null)
        {
            navigator.CenterOn(center);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();

        // Load existing sightings
        mapView.Pins.Clear();
        foreach (var s in _viewModel.Sightings)
        {
            AddPinForSighting(s);
        }

        // Center on most recent or first sighting
        var firstSighting = _viewModel.Sightings.FirstOrDefault(s => s.Location != null);
        if (firstSighting?.Location != null)
        {
            var (x, y) = SphericalMercator.FromLonLat(firstSighting.Location.Longitude, firstSighting.Location.Latitude);
            var center = new MPoint(x, y);
            var navigator = mapView.Map?.Navigator;
            if (navigator != null)
            {
                navigator.CenterOn(center);
                var resolutions = navigator.Resolutions;
                if (resolutions != null && resolutions.Count > 0)
                {
                    var idx = Math.Min(14, resolutions.Count - 1);
                    navigator.ZoomTo(resolutions[idx]);
                }
            }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Unsubscribe to prevent memory leaks
        _sightingRepository.SightingAdded -= OnSightingAdded;
    }
}
