using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Ravlyk.Common;
using Ravlyk.Drawing.ImageProcessor.Utilities;

namespace Ravlyk.Drawing.ImageProcessor
{
	public static class ImagePainter
	{
		public static void FillRect(IndexedImage image, Rectangle rect, int argb)
		{
			rect = Region.CorrectRect(image, rect);

			if (rect.Width <= 0 || rect.Height <= 0)
			{
				return;
			}

			Parallel.For(rect.Top, rect.BottomExclusive,
				y =>
				{
					for (int x = rect.Left, index = y * image.Size.Width + rect.Left; x < rect.RightExclusive; x++, index++)
					{
						image.Pixels[index] = argb;
					}
				});
		}

		public static void FillRect(IndexedImage image, Rectangle rect, Color toColor)
		{
			rect = Region.CorrectRect(image, rect);

			if (rect.Width <= 0 || rect.Height <= 0)
			{
				return;
			}

			for (int y = rect.Top; y < rect.BottomExclusive; y++)
			{
				for (int x = rect.Left; x < rect.RightExclusive; x++)
				{
					image[x, y] = toColor;
				}
			}
		}

		public static void DrawHorizontalLine(IndexedImage image, int x, int y, int length, int argb, int width = 1)
		{
			if (x < 0)
			{
				length += x;
				x = 0;
			}

			var maxLength = image.Size.Width - x;
			if (length >= maxLength)
			{
				length = maxLength;
			}

			var maxWidth = image.Size.Height - y;
			if (width > maxWidth)
			{
				width = maxWidth;
			}

			if (length <= 0 || y < 0 || y >= image.Size.Height)
			{
				return;
			}

			for (int i = 0, startIndex = y * image.Size.Width + x; i < width; i++, startIndex += image.Size.Width)
			{
				DrawLine(image, startIndex, startIndex + length, 1, argb);
			}
		}

		public static void DrawVerticalLine(IndexedImage image, int x, int y, int length, int argb, int width = 1)
		{
			if (y < 0)
			{
				length += y;
				y = 0;
			}

			var maxLength = image.Size.Height - y;
			if (length >= maxLength)
			{
				length = maxLength;
			}

			var maxWidth = image.Size.Width - x;
			if (width > maxWidth)
			{
				width = maxWidth;
			}

			if (length <= 0 || x < 0 || x >= image.Size.Width)
			{
				return;
			}

			for (int i = 0, startIndex = y * image.Size.Width + x, lastIndexExclusive = startIndex + length * image.Size.Width; i < width; i++, startIndex++, lastIndexExclusive++)
			{
				DrawLine(image, startIndex, lastIndexExclusive, image.Size.Width, argb);
			}
		}

		static void DrawLine(IndexedImage image, int startIndex, int lastIndexExclusive, int step, int argb)
		{
			for (int index = startIndex; index < lastIndexExclusive; index += step)
			{
				image.Pixels[index] = argb;
			}
		}

		#region DrawAnyLine

		public static void DrawAnyLineFat(IndexedImage image, Point a, Point b, Func<int, int> colorGetter)
		{
			DrawAnyLine(image, new Point(a.X, a.Y), new Point(b.X, b.Y), colorGetter);
			DrawAnyLine(image, new Point(a.X + 1, a.Y), new Point(b.X + 1, b.Y), colorGetter);
			DrawAnyLine(image, new Point(a.X, a.Y + 1), new Point(b.X, b.Y + 1), colorGetter);
		}

		public static void DrawAnyLine(IndexedImage image, Point a, Point b, Func<int, int> colorGetter)
		{
			if (Math.Abs(b.Y - a.Y) < Math.Abs(b.X - a.X))
			{
				DrawLineLow(image, a, b, colorGetter);
			}
			else
			{
				DrawLineHigh(image, a, b, colorGetter);
			}
		}

		static void DrawLineLow(IndexedImage image, Point a, Point b, Func<int, int> colorGetter)
		{
			if (a.X > b.X)
			{
				var c = a;
				a = b;
				b = c;
			}

			var dx = b.X - a.X;
			var dy = b.Y - a.Y;
			var yi = 1;
			if (dy < 0)
			{
				yi = -1;
				dy = -dy;
			}
			var dx2 = dx * 2;
			var dy2 = dy * 2;
			var d = dy2 - dx;

			var y = a.Y;
			for (int x = a.X; x <= b.X; x++)
			{
				DrawPoint(image, x, y, colorGetter);

				if (d > 0)
				{
					y += yi;
					d -= dx2;
				}
				d += dy2;
			}
		}

		static void DrawLineHigh(IndexedImage image, Point a, Point b, Func<int, int> colorGetter)
		{
			if (a.Y > b.Y)
			{
				var c = a;
				a = b;
				b = c;
			}

			var dx = b.X - a.X;
			var dy = b.Y - a.Y;
			var xi = 1;
			if (dx < 0)
			{
				xi = -1;
				dx = -dx;
			}
			var dx2 = dx * 2;
			var dy2 = dy * 2;
			var d = dx2 - dy;

			var x = a.X;
			for (int y = a.Y; y <= b.Y; y++)
			{
				DrawPoint(image, x, y, colorGetter);

				if (d > 0)
				{
					x += xi;
					d -= dy2;
				}
				d += dx2;
			}
		}

		static void DrawPoint(IndexedImage image, int x, int y, Func<int, int> colorGetter)
		{
			if (x >= 0 && y >= 0 && x < image.Size.Width && y < image.Size.Height)
			{
				var pixelIndex = y * image.Size.Width + x;
				image.Pixels[pixelIndex] = colorGetter(pixelIndex);
			}
		}

		#endregion

		public static void ShadeImage(IndexedImage image, int argb, IndexedImage maskImage = null)
		{
			Debug.Assert(maskImage == null || maskImage.Size.Equals(image.Size), "maskImage should have same size as image");

			for (int i = 0; i < image.Pixels.Length; i++)
			{
				var shadeArgb = maskImage != null ? ColorBytes.ShadeColor(argb, maskImage.Pixels[i]) : argb;
				image.Pixels[i] = ColorBytes.ShadeColor(image.Pixels[i], shadeArgb);
			}
		}

		/// <summary>
		/// Fill region of one color with specified color.
		/// </summary>
		/// <param name="image"></param>
		/// <param name="color"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <remarks>Causes <see cref="IndexedImage.PixelChanged"/> event for each changed pixel.</remarks>
		public static void Fill(IndexedImage image, Color color, int x, int y)
		{
			var fromColor = image[x, y];
			if (fromColor.Equals(color))
			{
				return;
			}

			image.Palette.Add(color);
			FloodFill(image, fromColor, color, x, y);
		}

		static void FloodFill(IndexedImage image, Color fromColor, Color toColor, int x, int y)
		{
			if (fromColor.Equals(toColor))
			{
				return;
			}

			var width = image.Size.Width;
			var height = image.Size.Height;
			var startingX = x;

			var steps = new List<int>();

			while (x >= 0 && image[x, y].Equals(fromColor))
			{
				image[x, y] = toColor;
				steps.Add(x);
				x--;
			}

			x = startingX + 1;
			while (x < width && image[x, y].Equals(fromColor))
			{
				image[x, y] = toColor;
				steps.Add(x);
				x++;
			}

			foreach (var step in steps)
			{
				if (y > 0 && image[step, y - 1].Equals(fromColor))
				{
					FloodFill(image, fromColor, toColor, step, y - 1);
				}
				if (y < height - 1 && image[step, y + 1].Equals(fromColor))
				{
					FloodFill(image, fromColor, toColor, step, y + 1);
				}
			}
		}
	}
}
