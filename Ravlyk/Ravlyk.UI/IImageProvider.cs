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
}
