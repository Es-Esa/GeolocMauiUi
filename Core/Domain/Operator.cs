using ClientApp.Core.Domain.Observers;

namespace ClientApp.Core.Domain
{
	public class Operator()
	{
		public required int Id;
		public required string Name;
		public required int CoordinateReferenceSystem;
		public List<ObservationArea> ObservationAreas = [];
		public List<Observer> Observers = [];
	}
}
