using System;

using Ravlyk.Common;
using Ravlyk.Drawing;
using Ravlyk.Drawing.ImageProcessor;

namespace Ravlyk.UI.ImageProcessor
{
    public class ImageCropManipulator : ImageManipulator
    {
        public ImageCropManipulator(IndexedImage sourceImage) : base(sourceImage) { }
        public ImageCropManipulator(ImageManipulator parentManipulator) : base(parentManipulator) { }

        public void CropRect(Rectangle cropRect)
        {
            ImageCropper.Crop(SourceImage, cropRect, ImageCropper.CropKind.Rectangle, ManipulatedImage);
            OnImageChanged();
        }

        public void CropArc(Rectangle cropRect)
        {
            ImageCropper.Crop(SourceImage, cropRect, ImageCropper.CropKind.Arc, ManipulatedImage);
            OnImageChanged();
        }
    }
}
