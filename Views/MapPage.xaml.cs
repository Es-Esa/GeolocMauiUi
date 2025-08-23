using ClientApp.Core.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace ClientApp.Views
{
    /// <summary>
    /// Page that displays all sightings on a map.
    /// </summary>
    public partial class MapPage : ContentPage
    {
        private readonly MapPageViewModel _viewModel;

        public MapPage(MapPageViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        /// <summary>
        /// Load sightings when the page appears.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
            mapView.Pins.Clear();
            Location? firstLocation = null;
            foreach (var sighting in _viewModel.Sightings)
            {
                if (sighting.Location != null)
                {
                    firstLocation ??= sighting.Location;
                    var pin = new Pin
                    {
                        Label = $"{sighting.ObservationType} ({sighting.Confidence:P0})",
                        Address = sighting.Timestamp.ToLocalTime().ToString("g"),
                        Location = sighting.Location,
                        Type = PinType.Place
                    };
                    mapView.Pins.Add(pin);
                }
            }

            if (firstLocation != null)
            {
                mapView.MoveToRegion(MapSpan.FromCenterAndRadius(firstLocation, Distance.FromKilometers(1)));
            }
            else
            {
                mapView.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(60.1699, 24.9384), Distance.FromKilometers(5)));
            }
        }
    }
}