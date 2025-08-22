using Microsoft.Maui.Devices.Sensors;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ClientApp.Core.Services
{

    /// <summary>
    /// ILocationService rajapinta määrittelee metodit sijainnin hakemiseen.
    /// </summary>
    public class LocationService : ILocationService
    {
        private bool _isCheckingLocation;
        private CancellationTokenSource? _cancelTokenSource;

        /// <summary>
        /// GetCurrentLocationAsync metodi hakee laitteen nykyisen sijainnin.
        /// </summary>
        /// <returns></returns>
        public async Task<Location?> GetCurrentLocationAsync()
        {
            if (_isCheckingLocation) return null; 

            try
            {
                _isCheckingLocation = true;


                // tarkistaa onko laitteessa GPS käytössä
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        Debug.WriteLine("Location permission not granted.");
                        
                        return null;
                    }
                }

                // annetaan sijainti tarkkuus ja aikaraja
                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                _cancelTokenSource = new CancellationTokenSource();
                Location? location = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token);

                return location; 
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                Debug.WriteLine($"Geolocation not supported on device: {fnsEx.Message}");
                return null;
            }
            catch (PermissionException pEx)
            {
                Debug.WriteLine($"Location permission error: {pEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting location: {ex.Message}");
                return null;
            }
            finally
            {
                _isCheckingLocation = false;
                _cancelTokenSource?.Dispose();
                _cancelTokenSource = null;
            }
        }
    }
} 