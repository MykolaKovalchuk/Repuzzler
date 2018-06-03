using System;
using Ravlyk.Drawing;
using Ravlyk.Drawing.ImageProcessor;
using Ravlyk.Drawing.SD;

namespace Descreener
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			const string sourceFile = @"/Users/mykola.kovalchuk/Projects/Repuzzler/TestFiles/src.png";
			const string targetFile = @"/Users/mykola.kovalchuk/Projects/Repuzzler/TestFiles/result.png";

			var image = IndexedImageExtensions.FromBitmapFile(sourceFile);

			int r = 0, g = 0, b = 0, count = 0;
			int[] pixels;
			using (image.LockPixels(out pixels))
			{
				for (int y = 0; y < 50; y++)
				{
					for (int x = 0, index1 = y * image.Size.Width, index2 = (y + 1) * image.Size.Width - 1; x < 50; x++, index1++, index2--, count += 2)
					{
						var c = pixels[index1];
						r += c.Red();
						g += c.Green();
						b += c.Blue();

						c = pixels[index2];
						r += c.Red();
						g += c.Green();
						b += c.Blue();
					}
				}
			}
			r /= count;
			g /= count;
			b /= count;

			var color = new Color((byte)r, (byte)g, (byte)b);

			var result = ColorRemover.RemoveColor(image, color);
			result.ToBitmapFile(targetFile);
		}
	}
}
