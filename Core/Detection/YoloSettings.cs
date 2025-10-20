namespace ClientApp.Core.Detection
{
	public struct YoloSettings
	{
		public const string ModelFileName = "yolo11n-nms.onnx";
		public const string ModelClassesFileName = "coco-classes.txt";

                public struct LayerNames
                {
                        // Layer names from Netron
                        public const string Input = "images";
                        public const string NMSOutput = "output0";
                };

		public struct Input
		{
			public const int SideLength = 640;
			public const int Channels = 3;
		};

		public struct Output
		{
			public const int MaxDetectionsPerInput = 300;
			public const string UnknownLabel = "unknown";
			public const float MinConfidence = 0.25f;

			public struct BoundingBox
			{
				public const int ValuesPerBoundingBox = 6;
				public const int TopLeftXIdx = 0;
				public const int TopLeftYIdx = 1;
				public const int BottomRightXIdx = 2;
				public const int BottomRightYIdx = 3;
				public const int ScoreIdx = 4;
				public const int LabelIdx = 5;
			}
		}
	}
}
