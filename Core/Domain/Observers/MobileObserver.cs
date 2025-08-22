using ClientApp.Core.Utils;
using Microsoft.Spatial;

namespace ClientApp.Core.Domain.Observers
{
    public class MobileObserver() : Observer()
    {
        public GeometryLineString? Path { get; set; }

        public override void UpdateFrames(GeolocatedFrame frame)
        {
            base.UpdateFrames(frame);
            if (PreviousFrame != null && PreviousFrame.HorizontalBearing == null)
            {
                PreviousFrame.HorizontalBearing = GeometryUtils.CalculateInitialBearing2D(PreviousFrame.Location, CurrentFrame!.Location);
                CurrentFrame.HorizontalBearing = GeometryUtils.CalculateFinalBearing2D(CurrentFrame.Location, PreviousFrame.Location);
            }
        }
    }
}
