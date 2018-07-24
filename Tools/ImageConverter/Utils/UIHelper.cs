using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageConverter.Utils
{
	public static class UIHelper
	{
		public static Button AddButton(this Control.ControlCollection controls, Point location, string text, int width, EventHandler handler,
			AnchorStyles anchor = AnchorStyles.Top | AnchorStyles.Left)
		{
			var button = new Button
			{
				Text = text,
				Width = width,
				Location = location,
				Anchor = anchor
			};
			if (handler != null)
			{
				button.Click += handler;
			}
			controls.Add(button);

			return button;
		}

		public static Label AddLabel(this Control.ControlCollection controls, Point location, string caption)
		{
			var label = new Label
			{
				Text = caption,
				AutoSize = true,
				Location = location
			};
			controls.Add(label);

			return label;
		}

		public static TextBox AddTextBoxWithLabel(this Control.ControlCollection controls, Point location, string caption, string text, int width)
		{
			controls.AddLabel(location, caption);

			var textBox = new TextBox
			{
				Text = text,
				Width = width,
				Location = new Point(location.X, location.Y + 14)
			};
			controls.Add(textBox);

			return textBox;
		}
	}
}
