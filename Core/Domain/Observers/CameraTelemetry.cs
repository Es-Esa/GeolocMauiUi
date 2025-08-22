namespace ClientApp.Core.Domain.Observers
{
	public class CameraTelemetry()
	{
		public required double BearingDeg;
		public required double TiltDeg;

		public double BearingRad { get { return BearingDeg * (Math.PI / 180.0); } }
		public double TiltRad { get { return TiltDeg * (Math.PI / 180.0); } }
	}
}
