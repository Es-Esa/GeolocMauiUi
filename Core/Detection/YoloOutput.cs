using Microsoft.ML.Data;

namespace ClientApp.Core.Detection
{
	public class YoloOutput
	{
		[VectorType(YoloSettings.Output.MaxDetectionsPerInput, YoloSettings.Output.BoundingBox.ValuesPerBoundingBox)]
		[ColumnName(YoloSettings.LayerNames.NMSOutput)]
		public float[] BoundingBoxes { get; set; }

		public YoloOutput()
		{
			BoundingBoxes = Array.Empty<float>();
		}

		public override bool Equals(object? obj)
		{
			return obj is YoloOutput output && EqualityComparer<float[]>.Default.Equals(BoundingBoxes, output.BoundingBoxes);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(BoundingBoxes);
		}
	}
}
