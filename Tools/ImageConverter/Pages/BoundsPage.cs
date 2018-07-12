using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Ravlyk.Drawing;
using Ravlyk.Drawing.SD;
using Ravlyk.UI;
using Ravlyk.UI.ImageProcessor;
using Ravlyk.UI.WinForms;

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
			
			var buttonNext = new Button
			{
				Text = "Save and Next",
				Width = 120,
				Location = new Point(8, 8)
			};
			buttonNext.Click += ButtonNextOnClick;
			panelButtons.Controls.Add(buttonNext);

			var buttonSource = new Button
			{
				Text = "Images Folder",
				Width = 100,
				Location = new Point(buttonNext.Right + 16, 8)
			};
			buttonSource.Click += ButtonSourceOnClick;
			panelButtons.Controls.Add(buttonSource);
			
			labelImages = new Label
			{
				Text = "",
				AutoSize = true,
				Location = new Point(buttonSource.Right + 16, 4)
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

		List<string> files;
		Random random;
		string currentFileName;

		ImageProvider imageProvider;
		VisualAnchorsController anchorsController;

		void ButtonSourceOnClick(object sender, EventArgs e)
		{
			using (var foldersDialog = new FolderBrowserDialog { ShowNewFolderButton = true })
			{
				if (foldersDialog.ShowDialog() == DialogResult.OK)
				{
					ImagesFolder = foldersDialog.SelectedPath;
					BoundsFolder = Path.Combine(ImagesFolder, "Bounds");
					
					labelImages.Text = $"Images: {ImagesFolder}{Environment.NewLine}Bounds: {BoundsFolder}";

					files = new DirectoryInfo(ImagesFolder)
						.GetFiles("*.png")
						.Select(fi => fi.FullName)
						.Where(fileName => !File.Exists(GetBoundsFileName(fileName)))
						.ToList();
					
					random = new Random();
				}
			}
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
			
			imageProvider.SetImage(IndexedImageExtensions.FromBitmapFile(currentFileName));
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
			foreach (var anchor in anchorsController.GetAllAnchors)
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

		#endregion
	}
}
