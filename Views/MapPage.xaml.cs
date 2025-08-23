using ClientApp.Core.Data;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;


namespace ClientApp.Views
{
    /// <summary>
    /// Page that displays all sightings on a map.
    /// </summary>
    public partial class MapPage : ContentPage
    {
        private readonly ISightingRepository _sightingRepository;

        public MapPage(ISightingRepository sightingRepository)
        {
            InitializeComponent();
            _sightingRepository = sightingRepository;
        }

        /// <summary>
        /// Load sightings when the page appears.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadSightingsAsync();
        }

        /// <summary>
        /// Retrieve sightings and add pins to the map.
        /// </summary>
        private async Task LoadSightingsAsync()
        {
            mapView.Pins.Clear();
            
            var sightings = await _sightingRepository.GetAllSightingsAsync();
            Location? firstLocation = null;

            foreach (var sighting in sightings)
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