using Microsoft.Spatial;
using ClientApp.Core.Domain.Observers;

namespace ClientApp.Core.Domain
{
	public class ObservationArea()
	{
		public required int Id;
		public required string Name;
		public required GeometryPolygon Extent;
		public List<GeometryPolygon> Buildings = [];
		public List<Observer> Observers = [];
	}
}
