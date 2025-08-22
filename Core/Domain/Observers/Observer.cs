using Microsoft.Spatial;
using ClientApp.Core.Enums;

namespace ClientApp.Core.Domain.Observers
{
	public class Observer()
	{
		public required int Id;

		public required string Name;
		public OperationStatus Status = OperationStatus.Passive;
		public GeometryPoint? Location;
		public CameraTelemetry? Camera;
		public GeolocatedFrame? CurrentFrame;
		public GeolocatedFrame? PreviousFrame;

		public virtual void UpdateFrames(GeolocatedFrame frame)
		{
			PreviousFrame = CurrentFrame;
			CurrentFrame = frame;
		}
	}
}
