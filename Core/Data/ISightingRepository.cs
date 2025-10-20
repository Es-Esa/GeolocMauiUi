
using ClientApp.Core.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClientApp.Core.Data
{
    /// <summary>
    /// Defines methods for storing and retrieving sightings.
    /// </summary>
    public interface ISightingRepository
    {
        /// <summary>
        /// Event raised when a new sighting is added.
        /// </summary>
        event EventHandler<Sighting>? SightingAdded;

        /// <summary>
        /// Add a sighting to the repository.
        /// </summary>
        Task AddSightingAsync(Sighting sighting);

        /// <summary>
        /// Retrieve all stored sightings.
        /// </summary>
        Task<List<Sighting>> GetAllSightingsAsync();
    }
}