using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ImageConverter.Pages
{
	public class SettingsPage : UserControl
	{
		public SettingsPage()
		{
			InitializeComponents();

			ReadSettings();
		}

		#region Design

		void InitializeComponents()
		{
			SuspendLayout();

			Dock = DockStyle.Fill;

			Controls.Add(new Label
			{
				Text = "Bounds Model:",
				AutoSize = true,
				Location = new Point(8, 10)
			});
			textBoxBoundsModel = new TextBox
			{
				Location = new Point(150, 8),
				Width = 400,
				ReadOnly = true
			};
			Controls.Add(textBoxBoundsModel);
			var buttonBoundsModel = new Button
			{
				Text = "Browse...",
				Location = new Point(textBoxBoundsModel.Right + 4, 8),
				Width = 80
			};
			Controls.Add(buttonBoundsModel);
			buttonBoundsModel.Click += ButtonBoundsModelOnClick;
			
			Controls.Add(new Label
			{
				Text = "Images Folder:",
				AutoSize = true,
				Location = new Point(8, 40)
			});
			textBoxImagesFolder = new TextBox
			{
				Location = new Point(150, 38),
				Width = 400,
				ReadOnly = true
			};
			Controls.Add(textBoxImagesFolder);
			var buttonImagesFolder = new Button
			{
				Text = "Browse...",
				Location = new Point(textBoxImagesFolder.Right + 4, 38),
				Width = 80
			};
			Controls.Add(buttonImagesFolder);
			buttonImagesFolder.Click += ButtonImagesFolderOnClick;

			Controls.Add(new Label
			{
				Text = "Bounds Subfolder:",
				AutoSize = true,
				Location = new Point(8, 70)
			});
			textBoxBoundsSubfolder = new TextBox
			{
				Location = new Point(150, 68),
				Width = 200
			};
			Controls.Add(textBoxBoundsSubfolder);
			textBoxBoundsSubfolder.TextChanged += TextBoxBoundsSubfolderOnTextChanged;

			Controls.Add(new Label
			{
				Text = "Descreened Subfolder:",
				AutoSize = true,
				Location = new Point(8, 100)
			});
			textBoxDescreenedSubfolder = new TextBox
			{
				Location = new Point(150, 98),
				Width = 200
			};
			Controls.Add(textBoxDescreenedSubfolder);
			textBoxDescreenedSubfolder.TextChanged += TextBoxDescreenedSubfolderOnTextChanged;

			ResumeLayout();
		}

		TextBox textBoxBoundsModel;
		TextBox textBoxImagesFolder;
		TextBox textBoxBoundsSubfolder;
		TextBox textBoxDescreenedSubfolder;

		#endregion

		#region Settings

		public string BoundsModelFileName
		{
			get => textBoxBoundsModel.Text;
			set
			{
				textBoxBoundsModel.Text = value;
				Settings.BoundsModel = value;
			}
		}

		public string ImagesFolder
		{
			get => textBoxImagesFolder.Text;
			set
			{
				textBoxImagesFolder.Text = value;
				Settings.ImagesFolder = value;
			}
		}

		public string BoundsSubfolder
		{
			get => textBoxBoundsSubfolder.Text;
			set
			{
				textBoxBoundsSubfolder.Text = value;
				Settings.BoundsSubfolder = value;
			}
		}

		public string DescreenedSubfolder
		{
			get => textBoxDescreenedSubfolder.Text;
			set
			{
				textBoxDescreenedSubfolder.Text = value;
				Settings.DescreenedSubfolder = value;
			}
		}

		void ReadSettings()
		{
			BoundsModelFileName = Settings.BoundsModel;
			ImagesFolder = Settings.ImagesFolder;
			BoundsSubfolder = Settings.BoundsSubfolder;
			DescreenedSubfolder = Settings.DescreenedSubfolder;
		}

		void UpdateSettings()
		{
			Settings.BoundsModel = BoundsModelFileName;
			Settings.ImagesFolder = ImagesFolder;
			Settings.BoundsModel = BoundsSubfolder;
			Settings.DescreenedSubfolder = DescreenedSubfolder;
		}

		#endregion

		#region Operations

		void ButtonBoundsModelOnClick(object sender, EventArgs e)
		{
			using (var openFileDialog = new OpenFileDialog { InitialDirectory = Path.GetDirectoryName(BoundsModelFileName) })
			{
				if (openFileDialog.ShowDialog() == DialogResult.OK)
				{
					BoundsModelFileName = openFileDialog.FileName;
				}
			}
		}

		void ButtonImagesFolderOnClick(object sender, EventArgs e)
		{
			using (var foldersDialog = new FolderBrowserDialog { ShowNewFolderButton = false, SelectedPath = ImagesFolder })
			{
				if (foldersDialog.ShowDialog() == DialogResult.OK)
				{
					ImagesFolder = foldersDialog.SelectedPath;
				}
			}
		}

		void TextBoxBoundsSubfolderOnTextChanged(object sender, EventArgs e)
		{
			Settings.BoundsSubfolder = BoundsSubfolder;
		}

		void TextBoxDescreenedSubfolderOnTextChanged(object sender, EventArgs e)
		{
			Settings.DescreenedSubfolder = DescreenedSubfolder;
		}

		#endregion
	}
}
