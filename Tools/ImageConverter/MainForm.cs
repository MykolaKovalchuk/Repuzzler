using System;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Ravlyk.Drawing.ImageProcessor;
using Ravlyk.Drawing.SD;
using Ravlyk.UI.ImageProcessor;

namespace ImageConverter
{
	public class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
		}

		void buttonLoad_Click(object sender, EventArgs e)
		{
			const string SupportedExtensions = "*.png; *.jpg; *.bmp; *.gif; *.tif";
		
			using (var openDialog = new OpenFileDialog { Filter = $"Images ({SupportedExtensions})|{SupportedExtensions}"})
			{
				if (openDialog.ShowDialog(this) == DialogResult.OK)
				{
					var image = IndexedImageExtensions.FromBitmapFile(openDialog.FileName);
					var zoomCropController = new VisualZoomCropController(new ImageCropController(new ImageCropManipulator(image)) { CropKind = ImageCropper.CropKind.Rectangle });
					zoomCropController.ZoomPercent = 100;
					visualControl.Controller = zoomCropController;
				}
			}
		}

		void buttonSave_Click(object sender, EventArgs e)
		{
			if (visualControl.Controller == null)
			{
				return;
			}
			
			using (var saveDialog = new SaveFileDialog { Filter = "PNG image (*.png)|*.png|JPG image (*.jpg)|*.jpg" })
			{
				if (saveDialog.ShowDialog(this) == DialogResult.OK)
				{
					var bitmap = visualControl.Controller.VisualImage.ToBitmap();

					switch (saveDialog.FilterIndex)
					{
						case 1:
							bitmap.Save(saveDialog.FileName, ImageFormat.Png);
							break;
						case 2:
							bitmap.Save(saveDialog.FileName, ImageFormat.Jpeg);
							break;
						default:
							bitmap.Save(saveDialog.FileName);
							break;
					}
				}
			}
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.panel1 = new System.Windows.Forms.Panel();
			this.buttonLoad = new System.Windows.Forms.Button();
			this.visualControl = new Ravlyk.UI.WinForms.VisualControl();
			this.buttonSave = new System.Windows.Forms.Button();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.buttonSave);
			this.panel1.Controls.Add(this.buttonLoad);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(734, 46);
			this.panel1.TabIndex = 3;
			// 
			// buttonLoad
			// 
			this.buttonLoad.Location = new System.Drawing.Point(12, 12);
			this.buttonLoad.Name = "buttonLoad";
			this.buttonLoad.Size = new System.Drawing.Size(75, 23);
			this.buttonLoad.TabIndex = 1;
			this.buttonLoad.Text = "Load";
			this.buttonLoad.UseVisualStyleBackColor = true;
			this.buttonLoad.Click += new System.EventHandler(this.buttonLoad_Click);
			// 
			// pictureBox1
			// 
			this.visualControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.visualControl.Location = new System.Drawing.Point(0, 46);
			this.visualControl.Name = "visualControl";
			this.visualControl.Size = new System.Drawing.Size(734, 547);
			this.visualControl.TabIndex = 4;
			this.visualControl.TabStop = false;
			// 
			// buttonSave
			// 
			this.buttonSave.Location = new System.Drawing.Point(93, 12);
			this.buttonSave.Name = "buttonSave";
			this.buttonSave.Size = new System.Drawing.Size(75, 23);
			this.buttonSave.TabIndex = 1;
			this.buttonSave.Text = "Save";
			this.buttonSave.UseVisualStyleBackColor = true;
			this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(734, 593);
			this.Controls.Add(this.visualControl);
			this.Controls.Add(this.panel1);
			this.Name = "MainForm";
			this.Text = "MainForm";
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Button buttonLoad;
		private System.Windows.Forms.Button buttonSave;
		private Ravlyk.UI.WinForms.VisualControl visualControl;

		#endregion
	}
}
