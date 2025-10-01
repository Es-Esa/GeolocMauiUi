using System.Collections.Generic;
using System.Linq;
using ClientApp.Core.ViewModels;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.Utilities;

using Location = Microsoft.Maui.Devices.Sensors.Location;

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
            EnsureMapInitialized();
            UpdateSightingsLayer();
        }

        private void EnsureMapInitialized()
        {
            if (mapView.Map != null)
            {
                return;
            }

            var map = new Map { CRS = "EPSG:3857" };
            map.Layers.Add(OpenStreetMap.CreateTileLayer());
            mapView.Map = map;
        }

        private void UpdateSightingsLayer()
        {
            if (mapView.Map == null)
            {
                return;
            }

            var existingLayer = mapView.Map.Layers.FirstOrDefault(layer => layer.Name == SightingsLayerName);
            if (existingLayer != null)
            {
                mapView.Map.Layers.Remove(existingLayer);
            }

            var features = new List<IFeature>();
            MPoint? firstPoint = null;

            foreach (var sighting in _viewModel.Sightings)
            {
                if (sighting.Location is not Location location)
                {
                    continue;
                }

                var worldPosition = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
                firstPoint ??= worldPosition;

                var feature = new PointFeature(worldPosition)
                {
                    ["Title"] = $"{sighting.ObservationType} ({sighting.Confidence:P0})",
                    ["Timestamp"] = sighting.Timestamp.ToLocalTime().ToString("g")
                };

                feature.Styles.Add(ImageStyles.CreatePinStyle());
                feature.Styles.Add(new CalloutStyle
                {
                    Title = (string)feature["Title"],
                    Content = (string)feature["Timestamp"]
                });

                features.Add(feature);
            }

            if (features.Count > 0)
            {
                var sightingsLayer = new MemoryLayer
                {
                    Name = SightingsLayerName,
                    Features = features
                };

                mapView.Map.Layers.Add(sightingsLayer);

                var boundingBox = CreateBoundingBox(firstPoint!, 1000);
                mapView.Navigator.ZoomToBox(boundingBox, MBoxFit.Fit);
            }
            else
            {
                var defaultCenter = SphericalMercator.FromLonLat(DefaultCenterLongitude, DefaultCenterLatitude);
                var boundingBox = CreateBoundingBox(defaultCenter, 5000);
                mapView.Navigator.ZoomToBox(boundingBox, MBoxFit.Fit);
            }
        }

        private static MRect CreateBoundingBox(MPoint center, double radiusMeters)
        {
            return new MRect(
                center.X - radiusMeters,
                center.Y - radiusMeters,
                center.X + radiusMeters,
                center.Y + radiusMeters);
        }

        private const string SightingsLayerName = "Sightings";
        private const double DefaultCenterLatitude = 60.1699;
        private const double DefaultCenterLongitude = 24.9384;
    }
}
