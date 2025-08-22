using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace ClientApp.Core.Detection
{
	public class YoloParser
	{
		public bool FilterByMinConfidence { get; set; }

		public YoloParser()
		{
			FilterByMinConfidence = true;
		}

		public List<YoloBoundingBox> Parse(YoloOutput output, int inputWidth, int inputHeight, Dictionary<int, string> classNames, int originalTopLeftX = 0, int originalTopLeftY = 0)
		{
			if (classNames == null || classNames.Count == 0)
			{
				throw new ArgumentNullException(nameof(classNames), "Class names dictionary cannot be null or empty.");
			}

			List<YoloBoundingBox> boundingBoxes = [];
			for (int i = 0; i < YoloSettings.Output.MaxDetectionsPerInput; i++)
			{
				var boundingBoxStartIdx = i * YoloSettings.Output.BoundingBox.ValuesPerBoundingBox;
				if (boundingBoxStartIdx >= output.BoundingBoxes.Length || boundingBoxStartIdx + YoloSettings.Output.BoundingBox.ValuesPerBoundingBox > output.BoundingBoxes.Length)
					break;

				int topLeftX = (int) output.BoundingBoxes[boundingBoxStartIdx + YoloSettings.Output.BoundingBox.TopLeftXIdx];
				int topLeftY = (int) output.BoundingBoxes[boundingBoxStartIdx + YoloSettings.Output.BoundingBox.TopLeftYIdx];
				int bottomRightX = (int) output.BoundingBoxes[boundingBoxStartIdx + YoloSettings.Output.BoundingBox.BottomRightXIdx];
				int bottomRightY = (int) output.BoundingBoxes[boundingBoxStartIdx + YoloSettings.Output.BoundingBox.BottomRightYIdx];
				float score = output.BoundingBoxes[boundingBoxStartIdx + YoloSettings.Output.BoundingBox.ScoreIdx];
				int labelIndex = (int) output.BoundingBoxes[boundingBoxStartIdx + YoloSettings.Output.BoundingBox.LabelIdx];
				
				string label = classNames.TryGetValue(labelIndex, out var name) ? name : YoloSettings.Output.UnknownLabel;

				if (FilterByMinConfidence && score < YoloSettings.Output.MinConfidence)
					continue;

				boundingBoxes.Add(
					new YoloBoundingBox
					{
						InputWidth = inputWidth,
						InputHeight = inputHeight,
						TopLeftX = topLeftX + originalTopLeftX,
						TopLeftY = topLeftY + originalTopLeftY,
						BottomRightX = bottomRightX + originalTopLeftX,
						BottomRightY = bottomRightY + originalTopLeftY,
						Score = score,
						Label = label
					}
				);
			}
			return boundingBoxes;
		}
	}
}
