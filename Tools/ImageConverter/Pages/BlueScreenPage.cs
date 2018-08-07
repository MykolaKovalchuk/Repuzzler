using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImageConverter.Utils;
using Ravlyk.Common;
using Ravlyk.UI;
using Ravlyk.UI.ImageProcessor;
using Ravlyk.UI.WinForms;
using Ravlyk.Drawing;
using Ravlyk.Drawing.ImageProcessor;
using Ravlyk.Drawing.SD;
using Point = System.Drawing.Point;

namespace ImageConverter.Pages
{
	public class BlueScreenPage : UserControl
	{
		public BlueScreenPage()
		{
			InitializeComponents();
		}

		#region Design

		void InitializeComponents()
		{
			SuspendLayout();

			Dock = DockStyle.Fill;

			visualControl = new VisualControl
			{
				Dock = DockStyle.Fill
			};
			Controls.Add(visualControl);

			var panelButtons = new Panel
			{
				Height = 40,
				Dock = DockStyle.Top
			};
			Controls.Add(panelButtons);

			var buttonSample = panelButtons.Controls.AddButton(new Point(8, 8), "Load Sample", 100, ButtonSampleOnClick);
			var buttonDescreen = panelButtons.Controls.AddButton(new Point(buttonSample.Right + 8, 8), "Descreen", 100, ButtonDescreenOnClick);
			var buttonReset = panelButtons.Controls.AddButton(new Point(buttonDescreen.Right + 8, 8), "Reset", 80, ButtonResetOnClick);

			textBoxHue = panelButtons.Controls.AddTextBoxWithLabel(new Point(buttonReset.Right + 16, 2), "Hue", "0.05", 60);
			textBoxHueStrict = panelButtons.Controls.AddTextBoxWithLabel(new Point(textBoxHue.Right + 4, 2), "Hue Strict", "0.01", 60);
			textBoxSaturation = panelButtons.Controls.AddTextBoxWithLabel(new Point(textBoxHueStrict.Right + 4, 2), "Saturation", "0.1", 60);
			textBoxValue = panelButtons.Controls.AddTextBoxWithLabel(new Point(textBoxSaturation.Right + 4, 2), "Value", "0.1", 60);
			textBoxRGB = panelButtons.Controls.AddTextBoxWithLabel(new Point(textBoxValue.Right + 4, 2), "RGB", "0.1", 60);
			textBoxDarkLimit = panelButtons.Controls.AddTextBoxWithLabel(new Point(textBoxRGB.Right + 4, 2), "Dark Limit", "0.25", 60);

			buttonProcessAll = panelButtons.Controls.AddButton(new Point(panelButtons.Width - 100 - 8, 8), "Process All", 100, ProcessAllOnClick,
				AnchorStyles.Right | AnchorStyles.Top);

			ResumeLayout();
		}

		VisualControl visualControl;

		TextBox textBoxHue;
		TextBox textBoxHueStrict;
		TextBox textBoxSaturation;
		TextBox textBoxValue;
		TextBox textBoxRGB;
		TextBox textBoxDarkLimit;

		Button buttonProcessAll;

		#endregion

		#region Operations

		ImageProvider imageProvider;
		VisualAnchorsController visualController;
		IndexedImage sourceImage;
		string sourceImageFileName;

		void ButtonSampleOnClick(object sender, EventArgs e)
		{
			if (visualControl.Controller == null)
			{
				InitializeController();
			}

			var files = new DirectoryInfo(Settings.ImagesFolder)
				.GetFiles("*.png")
				.Select(fi => fi.FullName)
				//.Where(fileName => File.Exists(Settings.GetBoundsFileName(fileName))) // Use sample with available bounds
				.ToList();

			var randomIndex = files.Count > 1 ? new Random().Next(files.Count) : files.Count - 1;
			if (randomIndex < 0)
			{
				return;
			}
			sourceImageFileName = files[randomIndex];

			sourceImage = IndexedImageExtensions.FromBitmapFile(sourceImageFileName);
			imageProvider.SetImage(sourceImage);
		}

		void InitializeController()
		{
			var edgeColor = ColorBytes.ToArgb(255, 255, 0, 0);

			imageProvider = new ImageProvider(new IndexedImage { Size = new Ravlyk.Common.Size(10, 10) });

			visualController = new VisualAnchorsController(imageProvider, new Ravlyk.Common.Size(visualControl.Width, visualControl.Height))
			{
				ZoomPercent = 100
			};
			visualController.AddAnchor(new Ravlyk.Common.Point(100, 100), edgeColor);
			visualController.AddAnchor(new Ravlyk.Common.Point(200, 200), edgeColor);

			visualController.AddEdge(new Ravlyk.Common.Point(0, 0), new Ravlyk.Common.Point(0, 1), edgeColor);
			visualController.AddEdge(new Ravlyk.Common.Point(0, 1), new Ravlyk.Common.Point(1, 1), edgeColor);
			visualController.AddEdge(new Ravlyk.Common.Point(1, 1), new Ravlyk.Common.Point(1, 0), edgeColor);
			visualController.AddEdge(new Ravlyk.Common.Point(1, 0), new Ravlyk.Common.Point(0, 0), edgeColor);

			visualControl.Controller = visualController;
		}

		void ButtonDescreenOnClick(object sender, EventArgs e)
		{
			var result = DescreenImage(sourceImage, sourceImageFileName);
			PaintCheckerBoard(result);
			imageProvider.SetImage(result);
		}

		void ButtonResetOnClick(object sender, EventArgs e)
		{
			if (sourceImage != null && imageProvider != null)
			{
				imageProvider.SetImage(sourceImage);
			}
		}

		void ProcessAllOnClick(object sender, EventArgs e)
		{
			var files = new DirectoryInfo(Settings.ImagesFolder)
				.GetFiles("*.png")
				.Select(fi => fi.FullName)
				//.Where(fileName => File.Exists(Settings.GetBoundsFileName(fileName))) // Use sample with available bounds
				.ToList();

			if (!Directory.Exists(Settings.DescreenedFolder))
			{
				Directory.CreateDirectory(Settings.DescreenedFolder);
			}

			for (int i = 0; i < files.Count; i++)
			{
				var fileName = files[i];

				buttonProcessAll.Text = $"{i} of {files.Count}";
				buttonProcessAll.Invalidate();
				buttonProcessAll.Update();

				var descreenedFileName = Settings.GetDescreenedFileName(fileName);
				if (File.Exists(descreenedFileName))
				{
					continue;
				}

				var image = IndexedImageExtensions.FromBitmapFile(fileName);
				var result = DescreenImage(image, fileName);
				result.ToBitmapFile(descreenedFileName);
			}

			buttonProcessAll.Text = "Process All";
		}

		#endregion

		#region Remove Color

		ColorRemover ColorRemover
		{
			get
			{
				var colorRemover = new ColorRemover();

				colorRemover.HueTolerance = double.Parse(textBoxHue.Text);
				colorRemover.HueToleranceStrict = double.Parse(textBoxHueStrict.Text);
				colorRemover.SaturationTolerance = double.Parse(textBoxSaturation.Text);
				colorRemover.ValueTolerance = double.Parse(textBoxValue.Text);
				colorRemover.RGBTolerance = double.Parse(textBoxRGB.Text);
				colorRemover.DarkValueLimiter = double.Parse(textBoxDarkLimit.Text);

				return colorRemover;
			}
		}

		IndexedImage DescreenImage(IndexedImage image, string imageFileName)
		{
			IndexedImage result;

			var boundsFileName = Settings.GetBoundsFileName(imageFileName);
			if (File.Exists(boundsFileName))
			{
				result = image.Clone(false);

				int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
				foreach (var line in File.ReadLines(boundsFileName))
				{
					if (!string.IsNullOrWhiteSpace(line))
					{
						var coordinates = line.Split(',');
						var x = int.Parse(coordinates[0]);
						var y = int.Parse(coordinates[1]);

						if (x < minX)
						{
							minX = x;
						}
						if (x > maxX)
						{
							maxX = x;
						}
						if (y < minY)
						{
							minY = y;
						}
						if (y > maxY)
						{
							maxY = y;
						}
					}
				}

				var bottomX = Math.Max(0, minX);
				var bottomY = Math.Max(0, minY);
				var topX = Math.Min(image.Size.Width - 1, maxX);
				var topY = Math.Min(image.Size.Height - 1, maxY);

				const int Distance = 10;
				const int Dots = 10;

				var limits = new Rectangle(minX - Distance, minY - Distance, maxX - minX + Distance, maxY - minY + Distance);
				limits = Ravlyk.Drawing.ImageProcessor.Utilities.Region.CorrectRect(result, limits);

				for (int y = bottomY; y <= topY - Dots; y += Dots)
				{
					if (minX >= Distance)
					{
						result = ColorRemover.RemoveColor(result, GetScreenColor(image, minX - Distance, y, minX - Distance, y + Dots), result, limits);
					}
					if (maxX < image.Size.Width - Distance)
					{
						result = ColorRemover.RemoveColor(result, GetScreenColor(image, maxX + Distance, y, maxX + Distance, y + Dots), result, limits);
					}
				}

				for (int x = bottomX; x <= topX - Dots; x += Dots)
				{
					if (minY > Distance)
					{
						result = ColorRemover.RemoveColor(result, GetScreenColor(image, x, minY - Distance, x + Dots, minY - Distance), result, limits);
					}
					if (maxY < image.Size.Height - Distance)
					{
						result = ColorRemover.RemoveColor(result, GetScreenColor(image, x, maxY + Distance, x + Dots, maxY + Distance), result, limits);
					}
				}

				using (result.LockPixels(out var pixels))
				{
					Parallel.For(0, result.Size.Height, y =>
					{
						for (int x = 0, index = y * result.Size.Width; x < result.Size.Width; x++, index++)
						{
							if (!limits.ContainsPoint(new Ravlyk.Common.Point(x, y)))
							{
								pixels[index] = 0;
							}
						}
					});
				}
			}
			else
			{
				result = ColorRemover.RemoveColor(image, GetScreenColor(sourceImage));
			}

			return result;
		}

		Color GetScreenColor(IndexedImage image)
		{
			var minX = Math.Min(visualController.AllAnchors[0].Location.X, visualController.AllAnchors[1].Location.X);
			var minY = Math.Min(visualController.AllAnchors[0].Location.Y, visualController.AllAnchors[1].Location.Y);
			var maxX = Math.Max(visualController.AllAnchors[0].Location.X, visualController.AllAnchors[1].Location.X);
			var maxY = Math.Max(visualController.AllAnchors[0].Location.Y, visualController.AllAnchors[1].Location.Y);

			return GetScreenColor(image, minX, minY, maxX, maxY);
		}

		static Color GetScreenColor(IndexedImage image, int minX, int minY, int maxX, int maxY)
		{
			int r = 0, g = 0, b = 0, count = 0;
			using (image.LockPixels(out var pixels))
			{
				for (int y = minY; y <= maxY; y++)
				{
					for (int x = minX, index = y * image.Size.Width + x; x <= maxX; x++, index++, count++)
					{
						var c = pixels[index];
						r += c.Red();
						g += c.Green();
						b += c.Blue();
					}
				}
			}
			r /= count;
			g /= count;
			b /= count;

			return new Color((byte)r, (byte)g, (byte)b);
		}

		static void PaintCheckerBoard(IndexedImage image)
		{
			using (image.LockPixels(out var pixels))
			{
				for (int y = 0; y < image.Size.Height; y++)
				{
					for (int x = 0, index = y * image.Size.Width; x < image.Size.Width; x++, index++)
					{
						var c = pixels[index];
						var alpha = c.Alpha();
						if (alpha < 255)
						{
							var i = (x / 16 + y / 16) % 2;
							var backColor = (i == 0 ? 127 : 255) * (255 - alpha) / 255;

							var newColor = new Color(
								(byte)255,
								(byte)(c.Red() * alpha / 255 + backColor),
								(byte)(c.Green() * alpha / 255 + backColor),
								(byte)(c.Blue() * alpha / 255 + backColor));
							pixels[index] = newColor.Argb;
						}
					}
				}
			}
		}

		#endregion
	}
}
