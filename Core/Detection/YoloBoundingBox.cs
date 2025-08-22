using ClientApp.Core.Enums;
using SkiaSharp;

namespace ClientApp.Core.Detection
{
	public class YoloBoundingBox
	{
		public required int InputWidth;
		public required int InputHeight;
		public required int TopLeftX;
		public required int TopLeftY;
		public required int BottomRightX;
		public required int BottomRightY;
		public required float Score;
		public required string Label;
		public SKRect Box
		{
			get { return new SKRect(TopLeftX, TopLeftY, BottomRightX, BottomRightY); }
		}
		public ObservationType ObservationType
		{
			get
			{
				switch (Label)
				{
					case "person":
						return ObservationType.Human;
					case "car":
						return ObservationType.Car;
					case "motorbike":
						return ObservationType.Motorbike;
					case "truck":
						return ObservationType.Truck;
					case "bicycle":
						return ObservationType.Bicycle;
					case YoloSettings.Output.UnknownLabel:
						return ObservationType.Unknown;
					default:
						return ObservationType.Other;
				}
			}
		}

		public override bool Equals(object? obj)
		{
			return obj is YoloBoundingBox box &&
				   InputWidth == box.InputWidth &&
				   InputHeight == box.InputHeight &&
				   TopLeftX == box.TopLeftX &&
				   TopLeftY == box.TopLeftY &&
				   BottomRightX == box.BottomRightX &&
				   BottomRightY == box.BottomRightY &&
				   Label == box.Label &&
				   Score == box.Score &&
				   Box.Equals(box.Box);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(InputWidth, InputHeight, TopLeftY, BottomRightX, BottomRightY, Label, Score, Box.GetHashCode());
		}
	}

}
