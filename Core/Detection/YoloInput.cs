using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Image;

namespace ClientApp.Core.Detection
{
	public class YoloInput()
	{
		[ImageType(YoloSettings.Input.SideLength, YoloSettings.Input.SideLength)]
		public required MLImage Image;
		public required int TopLeftX;
		public required int TopLeftY;
		public float Width = YoloSettings.Input.SideLength;
		public float Height = YoloSettings.Input.SideLength;

		public static List<YoloInput> TilesFromStream(Stream stream)
		{
			List<YoloInput> inputs = [];
			var image = MLImage.CreateFromStream(stream);
			var topLeftX = 0;
			var topLeftY = 0;
			if (image.Width <= YoloSettings.Input.SideLength && image.Height <= YoloSettings.Input.SideLength)
			{
				inputs.Add(new YoloInput { Image = image, TopLeftX = topLeftX, TopLeftY = topLeftY });
			}
			else
			{
				int widthSteps = (int) Math.Ceiling((double) image.Width / (double) YoloSettings.Input.SideLength);
				int heightSteps = (int) Math.Ceiling((double) image.Height / (double) YoloSettings.Input.SideLength);
				for (int heightStep = 0; heightStep < heightSteps; heightStep++)
				{
					topLeftY = heightStep * YoloSettings.Input.SideLength;
					for (int widthStep = 0; widthStep < widthSteps; widthStep++)
					{
						topLeftX = widthStep * YoloSettings.Input.SideLength;
						var clippedImage = ClipImage(image, topLeftX, topLeftY);
						inputs.Add(new YoloInput { Image = clippedImage, TopLeftX = topLeftX, TopLeftY = topLeftY });
					}
				}
			}
			return inputs;
		}

		private static MLImage ClipImage(MLImage image, int topLeftX, int topLeftY)
		{
			var bytesPerPixel = 4;
			byte[] frameBytes = new byte[YoloSettings.Input.SideLength * YoloSettings.Input.SideLength * bytesPerPixel];

			var imageLastByteIdx = image.Pixels.Length - 1;
			var imageWidthBytes = image.Width * bytesPerPixel;
			var frameWidthBytes = YoloSettings.Input.SideLength * bytesPerPixel;

			var widthSkipBytes = topLeftX * bytesPerPixel;
			var heightSkipBytes = topLeftY * image.Width * bytesPerPixel;

			for (int row = 0; row < YoloSettings.Input.SideLength; row++)
			{
				var rowStartByteIdx = Math.Min(heightSkipBytes + row * imageWidthBytes, imageLastByteIdx);
				var rowEndByteIdx = Math.Min(rowStartByteIdx + imageWidthBytes, imageLastByteIdx);

				if (rowStartByteIdx == imageLastByteIdx)
					continue;

				var frameRowStartByteIdx = rowStartByteIdx + widthSkipBytes;

				var sliceLength = Math.Min(frameWidthBytes, Math.Max(rowEndByteIdx - frameRowStartByteIdx, 0));
				var slicedPixels = image.Pixels.Slice(frameRowStartByteIdx, sliceLength);
				slicedPixels.ToArray().CopyTo(frameBytes, row * frameWidthBytes);
			}
			return MLImage.CreateFromPixels(YoloSettings.Input.SideLength, YoloSettings.Input.SideLength, image.PixelFormat, frameBytes);
		}

		public override bool Equals(object? obj)
		{
			return obj is YoloInput input &&
				   EqualityComparer<MLImage>.Default.Equals(Image, input.Image) &&
				   TopLeftX == input.TopLeftX &&
				   TopLeftY == input.TopLeftY &&
				   Width == input.Width &&
				   Height == input.Height;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Image.GetHashCode(), TopLeftX, TopLeftY, Width, Height);
		}
	}

}
