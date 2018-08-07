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

			var buttonSource = panelButtons.Controls.AddButton(new Point(8, 8), "Load Images", 100, ButtonSourceOnClick);
			buttonNext = panelButtons.Controls.AddButton(new Point(buttonSource.Right + 8, 8), "Save and Next", 100, ButtonNextOnClick);

			comboBoxLabelsKind = panelButtons.Controls.AddComboBox(new Point(buttonNext.Right + 8, 8), 80, LabelsKindChanged,
				LabelsKindCube, LabelsKindPoint);

			checkBoxVisualize = panelButtons.Controls.AddCheckBox(new Point(comboBoxLabelsKind.Right + 8, 8), "Visualize", CheckBoxVisualizeChanged);

			labelImages = panelButtons.Controls.AddLabel(new Point(checkBoxVisualize.Right + 8, 2), "");

			panelButtons.Controls.AddButton(new Point(panelButtons.Width - 80 - 8, 8), "Skip", 80, ButtonSkipOnClick,
				AnchorStyles.Right | AnchorStyles.Top);

			ResumeLayout();
		}

		VisualControl visualControl;
		Button buttonNext;
		Label labelImages;
		ComboBox comboBoxLabelsKind;
		CheckBox checkBoxVisualize;

		#endregion

		#region Operations

		const string LabelsKindCube = "Cube";
		const string LabelsKindPoint = "Point";

		ModelInferrence Model
		{
			get
			{
				if (model == null && !string.IsNullOrEmpty(Settings.BoundsModel) && File.Exists(Settings.BoundsModel))
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
			files = new DirectoryInfo(Settings.ImagesFolder)
				.GetFiles("*.png")
				.Select(fi => fi.FullName)
				.Where(fileName => !File.Exists(Settings.GetBoundsFileName(fileName)))
				.ToList();

			UpdateLabel();

			random = new Random();

			DisposeModel();
		}

		void UpdateLabel()
		{
			labelImages.Text = $"Images: {Settings.ImagesFolder}{Environment.NewLine}Bounds: {Settings.BoundsFolder}{Environment.NewLine}Files: {files.Count}";
		}

		void LabelsKindChanged(object sender, EventArgs e)
		{
			if (anchorsController == null)
			{
				return;
			}

			using (anchorsController.SuspendUpdateVisualImage())
			{
				anchorsController.ClearAllAnchors();
				SetupAnchors(comboBoxLabelsKind.Text);
			}
		}

		void CheckBoxVisualizeChanged(object sender, EventArgs e)
		{
			buttonNext.Enabled = !checkBoxVisualize.Checked;
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
			if (string.IsNullOrEmpty(Settings.ImagesFolder))
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

			SetupAnchors(comboBoxLabelsKind.Text);
			visualControl.Controller = anchorsController;
		}

		void SetupAnchors(string kind)
		{
			var edgeColor = ColorBytes.ToArgb(255, 255, 0, 0);

			switch (kind)
			{
				case LabelsKindCube:
					anchorsController.AddAnchor(new Ravlyk.Common.Point(200, 200), edgeColor);
					anchorsController.AddAnchor(new Ravlyk.Common.Point(200, 300), edgeColor);
					anchorsController.AddAnchor(new Ravlyk.Common.Point(120, 250), edgeColor);
					anchorsController.AddAnchor(new Ravlyk.Common.Point(120, 150), edgeColor);
					anchorsController.AddAnchor(new Ravlyk.Common.Point(200, 100), edgeColor);
					anchorsController.AddAnchor(new Ravlyk.Common.Point(280, 150), edgeColor);
					anchorsController.AddAnchor(new Ravlyk.Common.Point(280, 250), edgeColor);

					foreach (var edgePair in RubikHelper.RubikEdges)
					{
						anchorsController.AddEdge(edgePair.a, edgePair.b, edgeColor);
					}
					break;
				case LabelsKindPoint:
					anchorsController.AddAnchor(new Ravlyk.Common.Point(200, 200), edgeColor);
					break;
			}
		}

		void SaveBounds()
		{
			if (string.IsNullOrEmpty(currentFileName))
			{
				return;
			}

			if (!Directory.Exists(Settings.BoundsFolder))
			{
				Directory.CreateDirectory(Settings.BoundsFolder);
			}

			var data = new StringBuilder();
			foreach (var anchor in anchorsController.AllAnchors)
			{
				data.Append(anchor.Location.X).Append(',').Append(anchor.Location.Y).AppendLine();
			}

			var boundsFileName = Settings.GetBoundsFileName(currentFileName);
			File.WriteAllText(boundsFileName, data.ToString());
		}

		void PredictBoundsFromImage(IndexedImage image)
		{
			if (comboBoxLabelsKind.Text == LabelsKindPoint)
			{
				return;
			}

			if (Model == null)
			{
				return;
			}

			if (!checkBoxVisualize.Checked)
			{
				var t = Environment.TickCount;
				var points = Model.GetEdges(image).ToList();
				Console.WriteLine(Environment.TickCount - t);

				if (points.Count == anchorsController.AllAnchors.Count)
				{
					for (int i = 0; i < points.Count; i++)
					{
						anchorsController.AllAnchors[i].Location = points[i];
					}
				}
			}
			else
			{
				using (anchorsController.SuspendUpdateVisualImage())
				{
					anchorsController.ClearAllAnchors();
					anchorsController.AddAnchor(new Ravlyk.Common.Point(0, 0), 0);

					var colorsList = new int[]
					{
						ColorBytes.ToArgb(255, 255, 0, 0),
						ColorBytes.ToArgb(255, 255, 255, 0),
						ColorBytes.ToArgb(255, 255, 0, 255),
						ColorBytes.ToArgb(255, 0, 255, 255),
					};

					var pointsVariants = Model.GetEdgesVariants(image);
					for (var i = 0; i < pointsVariants.Count; i++)
					{
						var edgeColor = colorsList[i % colorsList.Length];
						foreach (var edgePair in RubikHelper.RubikEdges)
						{
							anchorsController.AddEdge(
								new Ravlyk.Common.Point(0, 0), new Ravlyk.Common.Point(0, 0),
								new Size(pointsVariants[i][edgePair.a].X, pointsVariants[i][edgePair.a].Y),
								new Size(pointsVariants[i][edgePair.b].X, pointsVariants[i][edgePair.b].Y),
								edgeColor);
						}
					}
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
