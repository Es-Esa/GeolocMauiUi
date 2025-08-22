using Microsoft.Spatial;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace ClientApp.Core.Domain
{
	public class GeolocatedFrame()
	{
		public required IImage Frame;
		public required GeometryPoint Location;
		public double? HorizontalBearing;
		public double? VerticalAngle;

		public override bool Equals(object? obj)
		{
			return obj is GeolocatedFrame frame &&
				   EqualityComparer<GeometryPoint>.Default.Equals(Location, frame.Location) &&
				   HorizontalBearing == frame.HorizontalBearing &&
				   VerticalAngle == frame.VerticalAngle;
		}

		public override int GetHashCode() => HashCode.Combine(Frame, Location, HorizontalBearing, VerticalAngle);

	}
}
