using Microsoft.Maui.Devices.Sensors;
using System.Threading.Tasks;

namespace ClientApp.Core.Services
{

    /// <summary>
    /// ILocationService rajapinta m��rittelee metodit sijainnin hakemiseen.
    /// </summary>
    public interface ILocationService
    {
        /// <summary>
        /// /// GetCurrentLocationAsync metodi hakee laitteen nykyisen sijainnin.
        /// </summary>
        /// <returns></returns>
        Task<Location?> GetCurrentLocationAsync();
    }
} 