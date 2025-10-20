
using ClientApp.Core.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClientApp.Core.Data
{

    /// <summary>
    /// Stores sightings in memory. Temporary implementation before adding a backend.
    /// </summary>
    public class InMemorySightingRepository : ISightingRepository
    {
        /// <summary>
        /// In-memory list of sightings.
        /// </summary>
        private readonly List<Sighting> _sightings = new();

        /// <summary>
        /// Event raised when a new sighting is added.
        /// </summary>
        public event EventHandler<Sighting>? SightingAdded;

        /// <summary>
        /// Add a sighting to the repository.
        /// </summary>
        public Task AddSightingAsync(Sighting sighting)
        {
            _sightings.Add(sighting);
            Console.WriteLine($"Sighting added: {sighting.ObservationType} at {sighting.Location?.Latitude},{sighting.Location?.Longitude}");
            
            // Raise the event to notify subscribers
            SightingAdded?.Invoke(this, sighting);
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Retrieve all sightings.
        /// </summary>
        public Task<List<Sighting>> GetAllSightingsAsync()
        {
            // Return a copy to prevent external modification
            return Task.FromResult(_sightings.ToList());
        }
    }
} 