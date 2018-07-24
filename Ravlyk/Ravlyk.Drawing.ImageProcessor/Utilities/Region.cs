using Ravlyk.Common;

namespace Ravlyk.Drawing.ImageProcessor.Utilities
{
	public static class Region
	{
		public static Rectangle CorrectRect(IndexedImage image, Rectangle rect)
		{
			if (rect.Left < 0)
			{
				rect.Width += rect.Left;
				rect.Left = 0;
			}
			if (rect.Top < 0)
			{
				rect.Height += rect.Top;
				rect.Top = 0;
			}

			var maxWidth = image.Size.Width - rect.Left;
			if (rect.Width > maxWidth)
			{
				rect.Width = maxWidth;
			}

			var maxHeight = image.Size.Height - rect.Top;
			if (rect.Height > maxHeight)
			{
				rect.Height = maxHeight;
			}

			return rect;
		}
	}
}
