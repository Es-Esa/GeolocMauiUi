
using ClientApp.Core.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClientApp.Core.Data
{

    /// <summary>
    /// InMemorySightingRepository on muistissa toimiva havainnointien tallennusluokka.
    /// Se tallentaa havainnot muistiin ja tarjoaa metodeja niiden lisäämiseen ja hakemiseen.
    /// Tämä on väliaikainen toteutus, sillä myöhemmin teemme rajapinnan jolla lähetämme tiedot palvelimelle.
    /// </summary>
    public class InMemorySightingRepository : ISightingRepository
    {
        /// <summary>
        /// Tämä lista tallentaa havainnot muistiin.
        /// </summary>
        private readonly List<Sighting> _sightings = new List<Sighting>();

        /// <summary>
        /// Tämä metodi lisää havainnon muistiin.
        /// </summary>
        /// <param name="sighting"></param>
        /// <returns></returns>
        public Task AddSightingAsync(Sighting sighting)
        {
            _sightings.Add(sighting);
            // In a real scenario, you might save to a database here
            Console.WriteLine($"Sighting added: {sighting.ObservationType} at {sighting.Location?.Latitude},{sighting.Location?.Longitude}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Tämä metodi hakee kaikki havainnot muistista.
        /// </summary>
        /// <returns></returns>
        public Task<List<Sighting>> GetAllSightingsAsync()
        {
            // Return a copy to prevent external modification
            return Task.FromResult(_sightings.ToList());
        }
    }
} 