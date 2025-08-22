using ClientApp.Core.Data;
using ClientApp.Core.Domain;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using System.Linq;
using System.Threading.Tasks;

namespace ClientApp.Views
{
    /// <summary>
    /// MapPage luokka on karttasivun määrittely joka sisältää kartan ja sen pinniä.
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
        /// Tämä metodi lataa kaikki havainnot ja lisää ne karttaan.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadSightingsAsync();
        }

        /// <summary>
        /// Tämä metodi lataa kaikki havainnot ja lisää ne karttaan.
        /// </summary>
        /// <returns></returns>
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