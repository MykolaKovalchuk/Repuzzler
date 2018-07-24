using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ImageConverter.Utils;
using Ravlyk.Common;
using Ravlyk.Drawing;
using Ravlyk.Drawing.SD;
using Ravlyk.UI;
using Ravlyk.UI.ImageProcessor;
using Ravlyk.UI.WinForms;
using Point = System.Drawing.Point;

namespace ImageConverter.Pages
{
	public class BoundsPage : UserControl
	{
		public BoundsPage()
		{
			InitializeComponents();
		}

		public string ImagesFolder { get; set; }
		public string BoundsFolder { get; set; }

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

			var buttonSource = new Button
			{
				Text = "Load Images",
				Width = 100,
				Location = new Point(8, 8)
			};
			buttonSource.Click += ButtonSourceOnClick;
			panelButtons.Controls.Add(buttonSource);

			var buttonNext = new Button
			{
				Text = "Save and Next",
				Width = 100,
				Location = new Point(buttonSource.Right + 16, 8)
			};
			buttonNext.Click += ButtonNextOnClick;
			panelButtons.Controls.Add(buttonNext);

			labelImages = new Label
			{
				Text = "",
				AutoSize = true,
				Location = new Point(buttonNext.Right + 16, 4)
			};
			panelButtons.Controls.Add(labelImages);

			var buttonSkip = new Button
			{
				Text = "Skip",
				Width = 80,
				Anchor = AnchorStyles.Right | AnchorStyles.Top,
				Location = new Point(panelButtons.Width - 80 - 8, 8)
			};
			buttonSkip.Click += ButtonSkipOnClick;
			panelButtons.Controls.Add(buttonSkip);

			ResumeLayout();
		}

		VisualControl visualControl;
		Label labelImages;

		#endregion

		#region Operations

		ModelInferrence Model
		{
			get
			{
				if (model == null && !string.IsNullOrEmpty(Settings.BoundsModel))
				{
					model = new ModelInferrence(Settings.BoundsModel, new Size(299, 299));
				}
				return model;
			}
		}
		ModelInferrence model;

		List<string> files;
		Random random;
		string currentFileName;

		ImageProvider imageProvider;
		VisualAnchorsController anchorsController;

		void ButtonSourceOnClick(object sender, EventArgs e)
		{
			ImagesFolder = Settings.ImagesFolder;
			BoundsFolder = Path.Combine(ImagesFolder, Settings.BoundsSubfolder);

			files = new DirectoryInfo(ImagesFolder)
				.GetFiles("*.png")
				.Select(fi => fi.FullName)
				.Where(fileName => !File.Exists(GetBoundsFileName(fileName)))
				.ToList();

			UpdateLabel();

			random = new Random();

			DisposeModel();
		}

		void UpdateLabel()
		{
			labelImages.Text = $"Images: {ImagesFolder}{Environment.NewLine}Bounds: {BoundsFolder}{Environment.NewLine}Files: {files.Count}";
		}

		void ButtonNextOnClick(object sender, EventArgs e)
		{
			Next(true);
		}

		void ButtonSkipOnClick(object sender, EventArgs e)
		{
			Next(false);
		}

		void Next(bool save)
		{
			if (string.IsNullOrEmpty(ImagesFolder))
			{
				return;
			}

			if (visualControl.Controller == null)
			{
				InitializeController();
			}
			else
			{
				if (save)
				{
					SaveBounds();
				}
			}

			var nextIndex = files.Count > 1 ? random.Next(files.Count) : files.Count - 1;
			if (nextIndex < 0)
			{
				return;
			}

			currentFileName = files[nextIndex];
			files.RemoveAt(nextIndex);
			FindForm().Text = System.IO.Path.GetFileName(currentFileName);
			UpdateLabel();

			var image = IndexedImageExtensions.FromBitmapFile(currentFileName);
			PredictBoundsFromImage(image);
			imageProvider.SetImage(image);
		}

		void InitializeController()
		{
			imageProvider = new ImageProvider(new IndexedImage { Size = new Ravlyk.Common.Size(10, 10) });
			anchorsController = new VisualAnchorsController(imageProvider, new Ravlyk.Common.Size(visualControl.Width, visualControl.Height))
			{
				ZoomPercent = 100
			};

			anchorsController.AddAnchor(new Ravlyk.Common.Point(200, 200));
			anchorsController.AddAnchor(new Ravlyk.Common.Point(200, 300));
			anchorsController.AddAnchor(new Ravlyk.Common.Point(120, 250));
			anchorsController.AddAnchor(new Ravlyk.Common.Point(120, 150));
			anchorsController.AddAnchor(new Ravlyk.Common.Point(200, 100));
			anchorsController.AddAnchor(new Ravlyk.Common.Point(280, 150));
			anchorsController.AddAnchor(new Ravlyk.Common.Point(280, 250));

			anchorsController.AddEdge(0, 1);
			anchorsController.AddEdge(0, 3);
			anchorsController.AddEdge(0, 5);
			anchorsController.AddEdge(1, 2);
			anchorsController.AddEdge(2, 3);
			anchorsController.AddEdge(3, 4);
			anchorsController.AddEdge(4, 5);
			anchorsController.AddEdge(5, 6);
			anchorsController.AddEdge(6, 1);

			visualControl.Controller = anchorsController;
		}

		void SaveBounds()
		{
			if (string.IsNullOrEmpty(currentFileName))
			{
				return;
			}

			if (!Directory.Exists(BoundsFolder))
			{
				Directory.CreateDirectory(BoundsFolder);
			}

			var data = new StringBuilder();
			foreach (var anchor in anchorsController.AllAnchors)
			{
				data.Append(anchor.X).Append(',').Append(anchor.Y).AppendLine();
			}

			var boundsFileName = GetBoundsFileName(currentFileName);
			File.WriteAllText(boundsFileName, data.ToString());
		}

		string GetBoundsFileName(string imageFileName)
		{
			return Path.Combine(BoundsFolder, Path.ChangeExtension(Path.GetFileName(imageFileName), "bounds"));
		}

		void PredictBoundsFromImage(IndexedImage image)
		{
			if (Model == null)
			{
				return;
			}

			//var t = Environment.TickCount;
			var points = Model.GetEdges(image).ToList();
			//Console.WriteLine(Environment.TickCount - t);

			if (points.Count == anchorsController.AllAnchors.Count)
			{
				for (int i = 0; i < points.Count; i++)
				{
					anchorsController.AllAnchors[i] = points[i];
				}
			}
		}

		#endregion

		#region Dispose

		protected override void Dispose(bool disposing)
		{
			DisposeModel();

			base.Dispose(disposing);
		}

		void DisposeModel()
		{
			if (model != null)
			{
				model.Dispose();
				model = null;
			}
		}

		#endregion
	}
}
