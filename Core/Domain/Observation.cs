using ClientApp.Core.Enums;
using ClientApp.Core.Domain.Observers;
using Microsoft.Spatial;
using ClientApp.Core.Detection;

namespace ClientApp.Core.Domain
{
	public class Observation()
	{
		public required Observer Observer;
		public required GeometryPoint Location;
		public required ObservationType ObservationType;
		public required float Confidence;
		public ObservationArea? ObservationArea;
		public DateTime Timestamp = DateTime.Now;

		public static Observation CreateObservation(
			Observer observer,
			GeolocatedFrame geolocatedFrame,
			YoloBoundingBox boundingBox,
			ObservationArea? observationArea = null
			)
		{
			var location = EstimateDetectionLocation(observer, geolocatedFrame, boundingBox);
			return new Observation
			{
				Observer = observer,
				Location = location,
				ObservationType = boundingBox.ObservationType,
				Confidence = boundingBox.Score,
				ObservationArea = observationArea
			};
		}

		public static GeometryPoint EstimateDetectionLocation(
			Observer observer,
			GeolocatedFrame geolocatedFrame,
			YoloBoundingBox boundingBox
			)
		{
			if (observer.Location == null || observer.Camera == null)
				throw new InvalidOperationException("Observer must have position, camera angle, and bearing.");

			// Assume that when an average person fills the whole frame, the distance
			// is approximately two meters. In reality camera lens properties and zoom
			// level play into these, but let's start with something. 
			double assumedFillDistance = 2.0;

			// When proportion equals 1, distance is 2 meters. When proportion is 2,
			// the distance is 4 meters and so on. The direction is "towards" the 
			// image, hence the depth dimension Z. 
			double distanceZ = assumedFillDistance * geolocatedFrame.Frame.Height / boundingBox.Box.Height;

			// In proportions, average human height is 7.5 head lengths and
			// shoulder width is 2 head lengths. Comes from artsy stuff.
			double assumedPersonHeight = 1.75;
			double assumedPersonWidth = assumedPersonHeight / 7.5 * 2.0;

			// Calculate the angles for horizontal and vertical deviations from image
			// center to locate the person in relation to the direction where the
			// camera is facing. Distance sign is important.
			double distanceImageXPixels = boundingBox.Box.MidX - (geolocatedFrame.Frame.Width / 2);
			double distanceImageYPixels = boundingBox.Box.MidY - (geolocatedFrame.Frame.Height / 2);
			double distanceImageX = distanceImageXPixels != 0 
				? assumedPersonWidth * boundingBox.Box.Width / distanceImageXPixels 
				: 0;
			double distanceImageY = distanceImageYPixels != 0 
				? assumedPersonHeight * boundingBox.Box.Height / distanceImageYPixels 
				: 0;
			double deltaImageXRad = Math.Atan(distanceImageX / distanceZ);
			double deltaImageYRad = Math.Atan(distanceImageY / distanceZ);

			// Calculate the location of the detection in relation to the observer
			double detectionBearingRad = observer.Camera.BearingRad + deltaImageXRad;
			double detectionTiltRad = observer.Camera.TiltRad + deltaImageYRad;
			double detectionGroundDistance = distanceZ * Math.Cos(detectionTiltRad);
			double deltaLocationX = detectionGroundDistance * Math.Cos(detectionBearingRad);
			double deltaLocationY = detectionGroundDistance * Math.Sin(detectionBearingRad);

			return GeometryPoint.Create(observer.Location.X + deltaLocationX, observer.Location.Y + deltaLocationY);
		}
	}
}
