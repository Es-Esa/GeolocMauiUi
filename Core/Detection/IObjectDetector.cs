using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ClientApp.Core.Detection
{
    /// <summary>
    /// Defines methods for object detection and model initialization.
    /// </summary>
    public interface IObjectDetector
    {
        /// <summary>
        /// Initialize the detection model.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Analyze an image and return detected objects.
        /// </summary>
        Task<List<YoloBoundingBox>> DetectAsync(Stream imageStream);
    }
}