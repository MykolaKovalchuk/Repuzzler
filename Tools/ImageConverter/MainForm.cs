using System;
using System.Drawing.Imaging;
using System.Windows.Forms;
using ImageConverter.Pages;
using Ravlyk.Common;
using Ravlyk.Drawing.SD;
using Ravlyk.UI;
using Ravlyk.UI.ImageProcessor;

namespace ImageConverter
{
	public class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
		}

		#region Design

		void InitializeComponent()
		{
			SuspendLayout();
			
			AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			WindowState = FormWindowState.Maximized;

			var tabControl = new TabControl
			{
				Dock = DockStyle.Fill
			};
			Controls.Add(tabControl);
			
			var tabBounds = new TabPage();
			tabControl.TabPages.Add(tabBounds);

			var boundsPage = new BoundsPage();
			tabBounds.Controls.Add(boundsPage);
			
			ResumeLayout();
		}

		#endregion
	}
}
