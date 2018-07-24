﻿using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Ravlyk.UI;
using Ravlyk.UI.ImageProcessor;
using Ravlyk.UI.WinForms;
using Ravlyk.Drawing;
using Ravlyk.Drawing.ImageProcessor;
using Ravlyk.Drawing.SD;
using Point = System.Drawing.Point;

namespace ImageConverter.Pages
{
	public class BlueScreePage : UserControl
	{
		public BlueScreePage()
		{
			InitializeComponents();
		}

		public string ImagesFolder { get; set; }
		public string ProcessedFolder { get; set; }

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

			var buttonSample = new Button
			{
				Text = "Load Sample",
				Width = 100,
				Location = new Point(8, 8)
			};
			buttonSample.Click += ButtonSampleOnClick;
			panelButtons.Controls.Add(buttonSample);

			var buttonDescreen = new Button
			{
				Text = "Descreen",
				Width = 100,
				Location = new Point(buttonSample.Right + 16, 8)
			};
			buttonDescreen.Click += ButtonDescreenOnClick;
			panelButtons.Controls.Add(buttonDescreen);

			panelButtons.Controls.Add(new Label
			{
				Text = "Hue",
				AutoSize = true,
				Location = new Point(buttonDescreen.Right + 16, 2)
			});
			textBoxHue = new TextBox
			{
				Text = "0.1",
				Width = 80,
				Location = new Point(buttonDescreen.Right + 16, 16)
			};
			panelButtons.Controls.Add(textBoxHue);

			panelButtons.Controls.Add(new Label
			{
				Text = "Hue Strict",
				AutoSize = true,
				Location = new Point(textBoxHue.Right + 4, 2)
			});
			textBoxHueStrict = new TextBox
			{
				Text = "0.01",
				Width = 80,
				Location = new Point(textBoxHue.Right + 4, 16)
			};
			panelButtons.Controls.Add(textBoxHueStrict);

			panelButtons.Controls.Add(new Label
			{
				Text = "Saturation",
				AutoSize = true,
				Location = new Point(textBoxHueStrict.Right + 4, 2)
			});
			textBoxSaturation = new TextBox
			{
				Text = "0.1",
				Width = 80,
				Location = new Point(textBoxHueStrict.Right + 4, 16)
			};
			panelButtons.Controls.Add(textBoxSaturation);

			panelButtons.Controls.Add(new Label
			{
				Text = "Value",
				AutoSize = true,
				Location = new Point(textBoxSaturation.Right + 4, 2)
			});
			textBoxValue = new TextBox
			{
				Text = "0.35",
				Width = 80,
				Location = new Point(textBoxSaturation.Right + 4, 16)
			};
			panelButtons.Controls.Add(textBoxValue);

			panelButtons.Controls.Add(new Label
			{
				Text = "RGB",
				AutoSize = true,
				Location = new Point(textBoxValue.Right + 4, 2)
			});
			textBoxRGB = new TextBox
			{
				Text = "0.25",
				Width = 80,
				Location = new Point(textBoxValue.Right + 4, 16)
			};
			panelButtons.Controls.Add(textBoxRGB);

			panelButtons.Controls.Add(new Label
			{
				Text = "Dark Limit",
				AutoSize = true,
				Location = new Point(textBoxRGB.Right + 4, 2)
			});
			textBoxDarkLimit = new TextBox
			{
				Text = "0.25",
				Width = 80,
				Location = new Point(textBoxRGB.Right + 4, 16)
			};
			panelButtons.Controls.Add(textBoxDarkLimit);

			var buttonReset = new Button
			{
				Text = "Reset",
				Width = 80,
				Anchor = AnchorStyles.Right | AnchorStyles.Top,
				Location = new Point(panelButtons.Width - 80 - 8, 8)
			};
			buttonReset.Click += ButtonResetOnClick;
			panelButtons.Controls.Add(buttonReset);

			ResumeLayout();
		}

		VisualControl visualControl;

		TextBox textBoxHue;
		TextBox textBoxHueStrict;
		TextBox textBoxSaturation;
		TextBox textBoxValue;
		TextBox textBoxRGB;
		TextBox textBoxDarkLimit;

		#endregion

		#region Operations

		ImageProvider imageProvider;
		VisualAnchorsController visualController;
		IndexedImage sourceImage;

		void ButtonSampleOnClick(object sender, EventArgs e)
		{
			ImagesFolder = Settings.ImagesFolder;
			ProcessedFolder = Path.Combine(ImagesFolder, Settings.DescreenedSubfolder);

			if (visualControl.Controller == null)
			{
				InitializeController();
			}

			var files = new DirectoryInfo(ImagesFolder)
				.GetFiles("*.png")
				.Select(fi => fi.FullName)
				.ToList();
			var randomIndex = files.Count > 1 ? new Random().Next(files.Count) : files.Count - 1;
			if (randomIndex < 0)
			{
				return;
			}

			sourceImage = IndexedImageExtensions.FromBitmapFile(files[randomIndex]);
			imageProvider.SetImage(sourceImage);
		}

		void InitializeController()
		{
			imageProvider = new ImageProvider(new IndexedImage { Size = new Ravlyk.Common.Size(10, 10) });

			visualController = new VisualAnchorsController(imageProvider, new Ravlyk.Common.Size(visualControl.Width, visualControl.Height))
			{
				ZoomPercent = 100
			};
			visualController.AddAnchor(new Ravlyk.Common.Point(100, 100));
			visualController.AddAnchor(new Ravlyk.Common.Point(200, 200));
			visualController.AddEdge(0, 1);

			visualControl.Controller = visualController;
		}

		void ButtonDescreenOnClick(object sender, EventArgs e)
		{
			var result = ColorRemover.RemoveColor(sourceImage, GetScreenColor(sourceImage));
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

		Color GetScreenColor(IndexedImage image)
		{
			var minx = Math.Min(visualController.AllAnchors[0].X, visualController.AllAnchors[1].X);
			var miny = Math.Min(visualController.AllAnchors[0].Y, visualController.AllAnchors[1].Y);
			var maxx = Math.Max(visualController.AllAnchors[0].X, visualController.AllAnchors[1].X);
			var maxy = Math.Max(visualController.AllAnchors[0].Y, visualController.AllAnchors[1].Y);

			int r = 0, g = 0, b = 0, count = 0;
			using (image.LockPixels(out var pixels))
			{
				for (int y = miny; y < maxy; y++)
				{
					for (int x = minx, index = y * image.Size.Width + x; x < maxx; x++, index++, count++)
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

		#endregion
	}
}