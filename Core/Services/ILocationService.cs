using Microsoft.Maui.Devices.Sensors;
using System.Threading.Tasks;

namespace ClientApp.Core.Services
{

    /// <summary>
    /// Provides access to the device's current location.
    /// </summary>
    public interface ILocationService
    {
        /// <summary>
        /// Get the device's current location.
        /// </summary>
        Task<Location?> GetCurrentLocationAsync();
    }
}