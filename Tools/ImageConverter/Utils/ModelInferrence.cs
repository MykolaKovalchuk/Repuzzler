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

		public IEnumerable<Point> GetEdges(IndexedImage image1)
		{
			if (Graph == null)
			{
				yield break;
			}

			image1 = CropAndResize(image1, out var shift, out var scaleX, out var scaleY);
			var image2 = ImageRotator.FlipHorizontallyInPlace(image1.Clone(false));
			var image3 = ImageRotator.RotateCCWInPlace(image1.Clone(false));
			var image4 = ImageRotator.RotateCCWInPlace(image2.Clone(false));
			var tensor = ImagesToTensor(image1, image2, image3, image4);

			if (Session == null)
			{
				Session = new TFSession(Graph);
			}

			var runner = Session.GetRunner();
			runner.AddInput(Graph["input_1"][0], tensor).Fetch(Graph["output_1"][0]);
			var output = runner.Run();

			var flatPoints = (float[,])output[0].GetValue();
			FlipPointsHorizontally(flatPoints, 1, (2, 6), (3, 5));
			RotatePointsCoordinatesCW(flatPoints, 2);
			RotatePointsCoordinatesCW(flatPoints, 3);
			FlipPointsHorizontally(flatPoints, 3, (2, 6), (3, 5));
			NormalizePointsOrientation(flatPoints);

			for (int ix = 0, iy = 1; iy < flatPoints.GetLength(1); ix += 2, iy += 2)
			{
				float x = 0.0f, y = 0.0f;
				var imagesCount = flatPoints.GetLength(0);
				for (var imageIndex = 0; imageIndex < imagesCount; imageIndex++)
				{
					x += flatPoints[imageIndex, ix];
					y += flatPoints[imageIndex, iy];
				}
				x /= imagesCount;
				y /= imagesCount;

				yield return new Point(
					shift.X + (int)(x * InputSize.Width / scaleX + 0.5f),
					shift.Y + (int)(y * InputSize.Height / scaleY + 0.5f));
			}
		}

		#region Prepare tensor

		TFTensor ImagesToTensor(params IndexedImage[] images)
		{
			var matrix = new float[images.Length, images[0].Size.Height, images[0].Size.Width, 3];

			for (var i = 0; i < images.Length; i++)
			{
				var image = images[i];
				using (image.LockPixels(out var pixels))
				{
					for (var iy = 0; iy < image.Size.Height; iy++)
					{
						for (int ix = 0, index = iy * image.Size.Width; ix < image.Size.Width; ix++, index++)
						{
							var pixel = pixels[index];
							matrix[i, iy, ix, 0] = pixel.Blue() / 255.0f;
							matrix[i, iy, ix, 1] = pixel.Green() / 255.0f;
							matrix[i, iy, ix, 2] = pixel.Red() / 255.0f;
						}
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

		#endregion

		#region Normalize points

		void FlipPointsHorizontally(float[,] points, int imageIndex, params (int a, int b)[] pairs)
		{
			for (int ix = 0; ix < points.GetLength(1); ix += 2)
			{
				points[imageIndex, ix] = 1 - points[imageIndex, ix];
			}

			foreach (var pair in pairs)
			{
				for (var s = 0; s < 2; s++)
				{
					var t = points[imageIndex, pair.a * 2 + s];
					points[imageIndex, pair.a * 2 + s] = points[imageIndex, pair.b * 2 + s];
					points[imageIndex, pair.b * 2 + s] = t;
				}
			}
		}

		void RotatePointsCoordinatesCW(float[,] points, int imageIndex)
		{
			for (int ix = 0, iy = 1; iy < points.GetLength(1); ix += 2, iy += 2)
			{
				var x = points[imageIndex, ix];
				var y = points[imageIndex, iy];
				points[imageIndex, ix] = 1 - y;
				points[imageIndex, iy] = x;
			}
		}

		void NormalizePointsOrientation(float[,] points)
		{
			for (var imageIndex = 0; imageIndex < points.GetLength(0); imageIndex++)
			{
				if (points[imageIndex, 3 * 2 + 1] > points[imageIndex, 1 * 2 + 1] || points[imageIndex, 5 * 2 + 1] > points[imageIndex, 1 * 2 + 1])
				{
					if (points[imageIndex, 5 * 2 + 1] > points[imageIndex, 3 * 2 + 1])
					{
						RotatePointsOrder(points, imageIndex, new[] { 1, 5, 3 }, new[] { 2, 6, 4 });
					}
					else
					{
						RotatePointsOrder(points, imageIndex, new[] { 1, 3, 5 }, new[] { 2, 4, 6 });
					}
				}
			}
		}

		void RotatePointsOrder(float[,] points, int imageIndex, params int[][] groups)
		{
			foreach (var group in groups)
			{
				for (var s = 0; s < 2; s++)
				{
					var t = points[imageIndex, group[0] * 2 + s];
					for (var i = 0; i < group.Length - 1; i++)
					{
						points[imageIndex, group[i] * 2 + s] = points[imageIndex, group[i + 1] * 2 + s];
					}
					points[imageIndex, group[group.Length - 1] * 2 + s] = t;
				}
			}
		}

		#endregion

		#region Dispose

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

		#endregion
	}
}
