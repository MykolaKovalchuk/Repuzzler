using System;
using System.Collections.Generic;
using System.IO;
using Ravlyk.Common;
using Ravlyk.Drawing;
using Ravlyk.Drawing.ImageProcessor;
using TensorFlow;

namespace ImageConverter.Utils
{
	public class ModelInferrence : IDisposable
	{
		public ModelInferrence(string modelFile, Size inputSize)
		{
			Graph = new TFGraph();
			var model = File.ReadAllBytes(modelFile);
			Graph.Import(new TFBuffer(model));

			InputSize = inputSize;
		}
		
		Size InputSize { get; }

		TFGraph Graph { get; set; }
		TFSession Session { get; set; }
		
		public IEnumerable<Point> GetEdges(IndexedImage image)
		{
			if (Graph == null)
			{
				yield break;
			}
			
			image = CropAndResize(image, out var shift, out var scaleX, out var scaleY);
			var tensor = ImageToTensor(image);

			if (Session == null)
			{
				Session = new TFSession(Graph);
			}
			
			var runner = Session.GetRunner();
			runner.AddInput(Graph["input_1"][0], tensor).Fetch(Graph["output_1"][0]);
			var output = runner.Run();
			var flatPoints = (float[,])output[0].GetValue();
			for (int ix = 0, iy = 1; iy < flatPoints.GetLength(1); ix += 2, iy += 2)
			{
				yield return new Point(
					shift.X + (int)(flatPoints[0, ix] * InputSize.Width / scaleX + 0.5f),
					shift.Y + (int)(flatPoints[0, iy] * InputSize.Height / scaleY + 0.5f));
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
		
		IndexedImage CropAndResize(IndexedImage sourceImage, out Point shift, out float scaleX, out float scaleY)
		{
			var sx = (float)InputSize.Width / (float)sourceImage.Size.Width;
			var sy = (float)InputSize.Height / (float)sourceImage.Size.Height;
			var scale = Math.Max(sx, sy);

			var newSize = new Size(
				Math.Min((int)(InputSize.Width / scale + 0.5f), sourceImage.Size.Width),
				Math.Min((int)(InputSize.Height / scale + 0.5f), sourceImage.Size.Height));
			
			shift = new Point((sourceImage.Size.Width - newSize.Width) / 2, (sourceImage.Size.Height - newSize.Height) / 2);
			scaleX = (float)InputSize.Width / (float)newSize.Width;
			scaleY = (float)InputSize.Height / (float)newSize.Height;

			var croppedImage = ImageCropper.Crop(sourceImage, new Rectangle(shift.X, shift.Y, newSize.Width, newSize.Height));
			var scaledImage = new ImageResampler().Resample(croppedImage, InputSize, ImageResampler.FilterType.Lanczos3);

			return scaledImage;
		}

		public void Dispose()
		{
			if (Session != null)
			{
				Session.Dispose();
				Session = null;
			}
			if (Graph != null)
			{
				Graph.Dispose();
				Graph = null;
			}
		}
	}
}
