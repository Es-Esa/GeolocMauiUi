
using ClientApp.Core.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClientApp.Core.Data
{
    /// <summary>
    /// IsightingRepository rajapinta määrittelee metodit havainnointien lisäämiseen ja hakemiseen.
    /// </summary>
    public interface ISightingRepository
    {

        /// <summary>
        /// Tämä metodi lisää havainnon muistiin.
        /// </summary>
        /// <param name="sighting"></param>
        /// <returns></returns>
        Task AddSightingAsync(Sighting sighting);
        Task<List<Sighting>> GetAllSightingsAsync();
    }
} 