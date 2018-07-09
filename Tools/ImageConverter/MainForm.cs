using System.Windows.Forms;
using ImageConverter.Pages;

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
			AutoScaleMode = AutoScaleMode.Font;
			WindowState = FormWindowState.Maximized;

			var tabControl = new TabControl
			{
				Dock = DockStyle.Fill
			};
			tabControl.SuspendLayout();
			Controls.Add(tabControl);
			
			var tabBounds = new TabPage
			{
				Text = "Bounds"
			};
			tabBounds.SuspendLayout();
			tabControl.TabPages.Add(tabBounds);

			var boundsPage = new BoundsPage();
			tabBounds.Controls.Add(boundsPage);

			tabBounds.ResumeLayout();
			tabControl.ResumeLayout();
			ResumeLayout();
		}

		#endregion
	}
}
