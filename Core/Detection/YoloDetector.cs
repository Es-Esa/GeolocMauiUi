using System.Collections.Immutable;
using Microsoft.ML;
using Microsoft.ML.Transforms.Image;
using Microsoft.Maui.Storage;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Microsoft.ML.Data;
using System.Linq;

namespace ClientApp.Core.Detection
{
	public class YoloDetector : IObjectDetector
	{
		private readonly string _originalModelPath = Path.Combine("Models", YoloSettings.ModelFileName);
		private readonly string _originalClassNamesPath = Path.Combine("Models", YoloSettings.ModelClassesFileName);
		private readonly MLContext _context;
		private readonly YoloParser _parser;
		private PredictionEngine<YoloInput, YoloOutput>? _engine;
		private Dictionary<int, string>? _classNames;

		public YoloDetector()
		{
			_context = new MLContext();
			_parser = new YoloParser { FilterByMinConfidence = true };
		}

		public async Task InitializeAsync()
		{
			try
			{
				_classNames = await LoadClassNamesAsync(_originalClassNamesPath);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error loading class names: {ex}");
				throw new InvalidOperationException("Failed to load class names.", ex);
			}

			string modelPathToUse = _originalModelPath;

#if ANDROID
			string localModelPath = Path.Combine(FileSystem.AppDataDirectory, YoloSettings.ModelFileName);

			if (!File.Exists(localModelPath))
			{
				try
				{
					using var packagedModelStream = await FileSystem.OpenAppPackageFileAsync(_originalModelPath);
					using var fileStream = new FileStream(localModelPath, FileMode.Create, FileAccess.Write);
					await packagedModelStream.CopyToAsync(fileStream);
				}
				catch (FileNotFoundException ex)
				{
					Console.WriteLine($"Error: Model file '{_originalModelPath}' not found in app package. {ex.Message}");
					throw;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error copying model file: {ex.Message}");
					throw;
				}
			}
			modelPathToUse = localModelPath;
#endif

			_engine = await Task.Run(() => InitializePredictionEngine(modelPathToUse));

			if (_engine == null)
			{
				throw new InvalidOperationException("Failed to initialize the prediction engine.");
			}
		}

		private async Task<Dictionary<int, string>> LoadClassNamesAsync(string packagePath)
		{
			var classNames = new Dictionary<int, string>();
			try
			{
				using var stream = await FileSystem.OpenAppPackageFileAsync(packagePath);
				using var reader = new StreamReader(stream);
				
				string? line;
				int index = 0;
				while ((line = await reader.ReadLineAsync()) != null)
				{
					if (!string.IsNullOrWhiteSpace(line))
					{
						classNames[index] = line.Trim();
						index++;
					}
				}
			}
			catch (FileNotFoundException ex)
			{
				Console.WriteLine($"Error: Class names file '{packagePath}' not found in app package. {ex.Message}");
				throw;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error reading class names file: {ex.Message}");
				throw;
			}
			return classNames;
		}

		private PredictionEngine<YoloInput, YoloOutput> InitializePredictionEngine(string modelFilePath)
		{
			if (!File.Exists(modelFilePath))
			{
				string errorMsg = $"Error: Model file not found at path: {modelFilePath}";
				Console.WriteLine(errorMsg);
				throw new FileNotFoundException(errorMsg, modelFilePath);
			}

			try
			{
				IDataView data = _context.Data.LoadFromEnumerable(new List<YoloInput>());
				var pipelineResizeImages = _context.Transforms.ResizeImages(
						resizing: ImageResizingEstimator.ResizingKind.IsoPad,
						inputColumnName: nameof(YoloInput.Image),
						outputColumnName: YoloSettings.LayerNames.Input,
						imageWidth: YoloSettings.Input.SideLength,
						imageHeight: YoloSettings.Input.SideLength
				);
                                var pipelineExtractPixels = pipelineResizeImages.Append(
                                        _context.Transforms.ExtractPixels(
                                                inputColumnName: YoloSettings.LayerNames.Input,
                                                outputColumnName: YoloSettings.LayerNames.Input,
                                                interleavePixelColors: false,
                                                colorsToExtract: ImagePixelExtractingEstimator.ColorBits.Blue |
                                                                ImagePixelExtractingEstimator.ColorBits.Green |
                                                                ImagePixelExtractingEstimator.ColorBits.Red,
                                                scaleImage: 1f / 255f
                                        )
                                );
				var pipelineApplyOnnxModel = pipelineExtractPixels.Append(
					_context.Transforms.ApplyOnnxModel(
						modelFile: modelFilePath,
						inputColumnNames: [YoloSettings.LayerNames.Input],
						outputColumnNames: [YoloSettings.LayerNames.NMSOutput],
						shapeDictionary: new Dictionary<string, int[]>() {
							{ YoloSettings.LayerNames.Input, new[] { 1, YoloSettings.Input.Channels, YoloSettings.Input.SideLength, YoloSettings.Input.SideLength } },
							{ YoloSettings.LayerNames.NMSOutput, new[] { 1, YoloSettings.Output.MaxDetectionsPerInput, YoloSettings.Output.BoundingBox.ValuesPerBoundingBox } }
						},
						gpuDeviceId: null,
						fallbackToCpu: true,
						recursionLimit: 100
					)
				);

				var pipeline = pipelineApplyOnnxModel.Fit(data);
				return _context.Model.CreatePredictionEngine<YoloInput, YoloOutput>(pipeline);
			}
			catch (Exception ex)
			{
				string errorMsg = $"Error during prediction engine initialization: {ex.ToString()}";
				Console.WriteLine(errorMsg);
				throw new InvalidOperationException(errorMsg, ex);
			}
		}

		public Task<List<YoloBoundingBox>> DetectAsync(Stream imageStream)
		{
			if (_engine == null)
			{
				throw new InvalidOperationException("Prediction engine is not initialized. Call InitializeAsync first.");
			}

			if (_classNames == null)
			{
				throw new InvalidOperationException("Class names are not loaded. Call InitializeAsync first.");
			}

			return Task.Run(() =>
			{
				if (!imageStream.CanRead)
					throw new ArgumentException("Input stream is not readable.", nameof(imageStream));
				if (imageStream.CanSeek)
					imageStream.Position = 0;

				MLImage originalMlImage;
				using (var tempStream = new MemoryStream())
				{
					imageStream.CopyTo(tempStream);
					tempStream.Position = 0;
					originalMlImage = MLImage.CreateFromStream(tempStream);
				}
				imageStream.Position = 0;
				int inputWidth = originalMlImage.Width;
				int inputHeight = originalMlImage.Height;

				List<YoloInput> modelInputs = YoloInput.TilesFromStream(imageStream);

				List<YoloBoundingBox> boundingBoxes = [];
				foreach (YoloInput modelInput in modelInputs)
				{
					try
					{
						YoloOutput modelOutput = _engine.Predict(modelInput);
						List<YoloBoundingBox> parsedOutput = _parser.Parse(modelOutput, inputWidth, inputHeight, _classNames, modelInput.TopLeftX, modelInput.TopLeftY);
						boundingBoxes = [.. boundingBoxes, .. parsedOutput];
					}
					finally
					{
						modelInput.Image?.Dispose();
					}
				}

				return boundingBoxes;
			});
		}
	}
}
