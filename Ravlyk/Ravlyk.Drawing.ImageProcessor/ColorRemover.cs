using System;
using System.Threading.Tasks;
using Ravlyk.Drawing.ImageProcessor.Utilities;

namespace Ravlyk.Drawing.ImageProcessor
{
	public static class ColorRemover
	{
		const double HueTolerance = 0.2 * 0.5; // [0,1] Hue Tolerance
		const double HueToleranceStrict = 0.04 * 0.5;
		const double SaturationTolerance = 0.4; // [0,1] Saturation Tolerance
		const double ValueTolerance = 0.4; // [0,1] Value Tolerance
		const double DarkValueLimiter = 0.35;
		const double RGBTolerance = 0.4; // [0,1] RGB Tolerance
		const double RGBToleranceScaled = (int)(3.0 * 255.0 * 255.0 * RGBTolerance * RGBTolerance + 0.5);
		const double GrayUpperLimit = 0.15; // [0,1] Gray Upper Limit (S*V)
		const double SourcePreservPortion = 0.0; // [0,1] Portion of Non-Erased Color to Preserve
		const bool GrayMatchesAll = false; // [0,1] Gray Matches All Hues

		public static IndexedImage RemoveColor(IndexedImage source, Color matchColor, IndexedImage result = null)
		{
			if (result == null)
			{
				result = new IndexedImage { Size = source.Size };
			}

			bool matchIsGray = (matchColor.Value * matchColor.Saturation <= GrayUpperLimit);
			bool preservePortion = (SourcePreservPortion > 0.0);

			Parallel.For(0, result.Size.Height, y =>
			{
				for (int x = 0, index = y * result.Size.Width; x < result.Size.Width; x++, index++)
				{
					Color pixel = new Color(source.Pixels[index]);

					if (pixel.A == 0)
					{
						// Just pass through transparent pixels
						result.Pixels[index] = pixel.Argb;
						continue;
					}

					bool pixelIsGray = (pixel.Saturation * pixel.Value <= GrayUpperLimit);
					bool hueMatches;
					double distH = 0.0;
					if (pixelIsGray || matchIsGray)
					{
						hueMatches = GrayMatchesAll || (pixelIsGray && matchIsGray);
					}
					else
					{
						distH = Math.Abs(pixel.Hue - matchColor.Hue);
						if (distH > 0.5) // Handle color wheel wrap-around.
						{
							distH = 1.0 - distH;
						}
						hueMatches = (distH <= HueTolerance);
					}

					// Also do an RGB comparison, since this is sometimes better than HSV.
					int dist2RGB = ColorDistance.GetSquareDistance(pixel, matchColor);

					// See if pixels should be erased (or partially erased)
					if (hueMatches &&
						(Math.Abs(pixel.Saturation - matchColor.Saturation) <= SaturationTolerance) &&
						(Math.Abs(pixel.Value - matchColor.Value) <= ValueTolerance) &&
						(dist2RGB <= RGBToleranceScaled))
					{
						if (preservePortion)
						{
							pixel = RemoveColor(pixel, matchColor);
						}
						else
						{
							double alpha = 0.0;
							if (HueToleranceStrict < HueTolerance && distH > HueToleranceStrict)
							{
								alpha = (distH - HueToleranceStrict) / (HueTolerance - HueToleranceStrict) * 255.0 + 0.5;
							}

							if (DarkValueLimiter > 0.0 && pixel.Value < DarkValueLimiter)
							{
								alpha = Math.Max(alpha, (DarkValueLimiter - pixel.Value) / DarkValueLimiter * 255.0 + 0.5);
							}

							pixel = new Color((byte)alpha, pixel.R, pixel.G, pixel.B);
						}
					}

					result.Pixels[index] = pixel.Argb;
				}
			});

			return result;
		}

		// Get the color with the minimum alpha such that the source color can be
		// produced by alpha blending with the subtracted color.
		static Color RemoveColor(Color srcColor, Color matchColor)
		{
			double srcR = srcColor.R, srcG = srcColor.G, srcB = srcColor.B;
			double matchR = matchColor.R, matchG = matchColor.G, matchB = matchColor.B;
			double alpha = RemovedAlpha(srcR, srcG, srcB, matchR, matchG, matchB);

			if (alpha > 0.0)
			{
				double aRecip = 1.0 / alpha;
				int r = (int)(aRecip * (srcR - matchR) + matchR + 0.5);
				int g = (int)(aRecip * (srcG - matchG) + matchG + 0.5);
				int b = (int)(aRecip * (srcB - matchB) + matchB + 0.5);
				int a = (int)(alpha * SourcePreservPortion * (double)srcColor.A + 0.5);
				return new Color((byte)a, (byte)r, (byte)g, (byte)b);
			}

			return new Color(0, 0, 0, 0);
		}

		static double RemovedAlpha(double srcR, double srcG, double srcB, double matchR, double matchG, double matchB)
		{
			return Max3(MinAlpha(srcR, matchR),
						MinAlpha(srcG, matchG),
						MinAlpha(srcB, matchB));
		}

		static double Max3(double x, double y, double z)
		{
			return (x > y) ? ((x > z) ? x : z) : ((y > z) ? y : z);
		}

		// Get the minimum alpha (0 <= alpha <= 1) such that the color component
		// can be written as c = subC + alpha * (srcC - subC).
		static double MinAlpha(double srcC, double subC)
		{
			if (srcC < subC)
			{
				return (subC - srcC) / subC;
			}
			else if (srcC > subC)
			{
				return (srcC - subC) / (255.0 - subC);
			}
			else
			{
				return 0.0;
			}
		}
	}
}
