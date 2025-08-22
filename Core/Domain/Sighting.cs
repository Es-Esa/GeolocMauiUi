using Microsoft.Maui.Devices.Sensors;
using ClientApp.Core.Enums;


namespace ClientApp.Core.Domain
{
    /// <summary>
    /// Sighting luokka m��rittelee havainnon ominaisuudet.
    /// </summary>
    public class Sighting
    {
        public Guid Id { get; set; }
        public Location? Location { get; set; }
        public DateTime Timestamp { get; set; }
        public ObservationType ObservationType { get; set; }
        public float Confidence { get; set; }

        public Sighting()
        {
            Id = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
        }
    }
} 