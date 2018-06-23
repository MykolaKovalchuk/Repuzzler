using System;
using Ravlyk.Drawing;

namespace Ravlyk.UI
{
	public interface IImageProvider
	{
		IndexedImage Image { get; }

		bool SupportsChangedEvent { get; }

		event EventHandler ImageChanged;
	}

	public class ImageProvider : IImageProvider
	{
		public ImageProvider(IndexedImage image)
		{
			SetImage(image);
		}

		public void SetImage(IndexedImage image)
		{
			Image = image;
			ImageChanged?.Invoke(this, EventArgs.Empty);
		}

		public IndexedImage Image { get; private set; }

		public bool SupportsChangedEvent => true;

		public event EventHandler ImageChanged;
	}
}
