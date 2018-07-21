using System;
using System.Collections.Generic;
using System.IO;
using Ravlyk.Common;
using Ravlyk.Drawing;
using Ravlyk.Drawing.ImageProcessor;
using TensorFlow;

namespace ImageConverter.Utils
{
	public class ModelInferrence
	{
		public ModelInferrence(string modelFile, Size inputSize)
		{
			Graph = new TFGraph();
			var model = File.ReadAllBytes(modelFile);
			Graph.Import(new TFBuffer(model));

			InputSize = inputSize;
		}
		
		TFGraph Graph { get; }
		
		Size InputSize { get; }

		public IEnumerable<Point> GetEdges(IndexedImage image)
		{
			image = CropAndResize(image, out var shift, out var scale);
			var tensor = ImageToTensor(image);
			
			using (var session = new TFSession(Graph))
			{
				var runner = session.GetRunner();
				runner.AddInput(Graph["input_1"][0], tensor).Fetch(Graph["output_node0"][0]);
				var output = runner.Run();
				var flatPoints = (float[,])output[0].GetValue(jagged: false);
				for (int ix = 0, iy = 1; iy < flatPoints.GetLength(1); ix += 2, iy += 2)
				{
					yield return new Point(
						shift.X + (int)(flatPoints[0, ix] * InputSize.Width / scale + 0.5f),
						shift.Y + (int)(flatPoints[0, iy] * InputSize.Height / scale + 0.5f));
				}
			}
		}

		TFTensor ImageToTensor(IndexedImage image)
		{
			var matrix = new float[1, image.Size.Height, image.Size.Width, 3];
			
			using (image.LockPixels(out var pixels))
			{
				for (var iy = 0; iy < image.Size.Height; iy++)
				{
					for (int ix = 0, index = iy * image.Size.Width; ix < image.Size.Width; ix++, index++)
					{
						var pixel = pixels[index];
						matrix[0, iy, ix, 0] = pixel.Blue() / 255.0f;
						matrix[0, iy, ix, 1] = pixel.Green() / 255.0f;
						matrix[0, iy, ix, 2] = pixel.Red() / 255.0f;
					}
				}
			}

			TFTensor tensor = matrix;
			return tensor;
		}
		
		IndexedImage CropAndResize(IndexedImage sourceImage, out Point shift, out float scale)
		{
			var sx = (float)InputSize.Width / (float)sourceImage.Size.Width;
			var sy = (float)InputSize.Height / (float)sourceImage.Size.Height;
			scale = Math.Max(sx, sy);

			var newSize = new Size((int)(InputSize.Width / scale), (int)(InputSize.Height / scale));
			shift = new Point((sourceImage.Size.Width - newSize.Width) / 2, (sourceImage.Size.Height - newSize.Height) / 2);

			var croppedImage = ImageCropper.Crop(sourceImage, new Rectangle(shift.X, shift.Y, newSize.Width, newSize.Height));
			var scaledImage = new ImageResampler().Resample(croppedImage, InputSize, ImageResampler.FilterType.Lanczos3);

			return scaledImage;
		}
	}
}
