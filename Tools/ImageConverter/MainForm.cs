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
			
			var tabBounds = new TabPage { Text = "Bounds" };
			tabBounds.SuspendLayout();
			boundsPage = new BoundsPage();
			tabBounds.Controls.Add(boundsPage);

			var tabBlueScreen = new TabPage { Text = "Blue Screen" };
			tabBlueScreen.SuspendLayout();
			blueScreenPage = new BlueScreenPage();
			tabBlueScreen.Controls.Add(blueScreenPage);

			var tabSettngs = new TabPage { Text = "Settings" };
			tabSettngs.SuspendLayout();
			settingsPage = new SettingsPage();
			tabSettngs.Controls.Add(settingsPage);

			tabControl.TabPages.Add(tabSettngs);
			tabControl.TabPages.Add(tabBounds);
			tabControl.TabPages.Add(tabBlueScreen);

			tabBounds.ResumeLayout();
			tabBlueScreen.ResumeLayout();
			tabSettngs.ResumeLayout();
			tabControl.ResumeLayout();
			ResumeLayout();
		}

		BoundsPage boundsPage;
		BlueScreenPage blueScreenPage;
		SettingsPage settingsPage;

		#endregion
	}
}
