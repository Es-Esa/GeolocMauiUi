using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ClientApp.Core.Detection
{
    /// <summary>
    /// /// IObjectDetector rajapinta määrittelee metodit objektien tunnistamiseen ja mallin alustamiseen.
    /// </summary>
    public interface IObjectDetector
    {
        /// <summary>
        /// /// InitializeAsync metodi alustaa mallin ja tarkistaa onko se valmis analysoimaan kuvia.
        /// </summary>
        /// <returns></returns>
        Task InitializeAsync();

        /// <summary>
        /// /// DetectAsync metodi analysoi kuvan ja palauttaa listan tunnistetuista objekteista.
        /// </summary>
        /// <param name="imageStream"></param>
        /// <returns></returns>
        Task<List<YoloBoundingBox>> DetectAsync(Stream imageStream);
    }
} 