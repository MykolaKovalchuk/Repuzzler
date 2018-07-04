using System;
using System.Collections.Generic;
using System.Linq;
using Ravlyk.Common;
using Ravlyk.Drawing;
using Ravlyk.Drawing.ImageProcessor;

namespace Ravlyk.UI.ImageProcessor
{
	public class VisualAnchorsController : VisualZoomController
	{
		public VisualAnchorsController(IImageProvider imageProvider, Size imageBoxSize = default(Size)) : base(imageProvider, imageBoxSize)
		{
		}

		#region Anchors

		readonly List<Point> anchors = new List<Point>();
		readonly List<(int a, int b)> edges = new List<(int, int)>();

		public IEnumerable<Point> GetAllAnchors => anchors;

		public int AddAnchor(Point newAnchor)
		{
			if (anchors.Any(anchor => anchor == newAnchor))
			{
				throw new ArgumentException($"Similar anchor is already added to list: {newAnchor}");
			}

			anchors.Add(newAnchor);

			quickUpdate = true;
			UpdateVisualImage();

			return anchors.Count - 1;
		}

		public void AddEdge(int firstAnchorIndex, int secondAnchorIndex)
		{
			if (firstAnchorIndex == secondAnchorIndex)
			{
				throw new ArgumentException($"1st and 2nd anchor indexes are same: {firstAnchorIndex}");
			}
			if (firstAnchorIndex < 0 || firstAnchorIndex >= anchors.Count)
			{
				throw new ArgumentNullException($"1st anchor index is out of bounds: {firstAnchorIndex}");
			}
			if (secondAnchorIndex < 0 || secondAnchorIndex >= anchors.Count)
			{
				throw new ArgumentNullException($"2nd anchor index is out of bounds: {secondAnchorIndex}");
			}
			if (edges.Any(edge => edge.a == firstAnchorIndex && edge.b == secondAnchorIndex || edge.b == firstAnchorIndex && edge.a == secondAnchorIndex))
			{
				throw new ArgumentException($"Similar edge is already added to list: ({firstAnchorIndex}, {secondAnchorIndex})");
			}

			edges.Add((firstAnchorIndex, secondAnchorIndex));

			quickUpdate = true;
			UpdateVisualImage();
		}

		#endregion

		#region Update visual image

		bool quickUpdate;
		int[] originalVisualImagePixels;

		const int AnchorRadius = 8;
		readonly int anchorColor = ColorBytes.ToArgb(255, 255, 127, 0);
		readonly int edgeColor = ColorBytes.ToArgb(255, 255, 0, 0);

		protected override void UpdateParameters()
		{
			base.UpdateParameters();
			originalVisualImagePixels = null;
		}

		protected override void UpdateVisualImageCore()
		{
			if (!quickUpdate || originalVisualImagePixels == null)
			{
				base.UpdateVisualImageCore();
				using (VisualImage.LockPixels(out var visualPixels))
				{
					originalVisualImagePixels = new int[visualPixels.Length];
					visualPixels.CopyTo(originalVisualImagePixels, 0);
				}
			}

			DrawAnchorsCore(false);

			quickUpdate = false;
		}

		void DrawAnchorsCore(bool clear)
		{
			foreach (var edge in edges)
			{
				DrawEdge(edge, clear);
			}

			foreach (var anchor in anchors)
			{
				DrawAnchor(ZoomedShiftedPoint(anchor), clear);
			}
		}

		void DrawAnchor(Point anchor, bool clear)
		{
			var colorDecider = clear ? (Func<int, int>)(i => originalVisualImagePixels[i]) : i => edgeColor;

			ImagePainter.DrawAnyLineFat(VisualImage,
				new Point(anchor.X - AnchorRadius, anchor.Y - AnchorRadius), new Point(anchor.X + AnchorRadius, anchor.Y - AnchorRadius),
				colorDecider);
			ImagePainter.DrawAnyLineFat(VisualImage,
				new Point(anchor.X - AnchorRadius, anchor.Y + AnchorRadius), new Point(anchor.X + AnchorRadius, anchor.Y + AnchorRadius),
				colorDecider);
			ImagePainter.DrawAnyLineFat(VisualImage,
				new Point(anchor.X - AnchorRadius, anchor.Y - AnchorRadius), new Point(anchor.X - AnchorRadius, anchor.Y + AnchorRadius),
				colorDecider);
			ImagePainter.DrawAnyLineFat(VisualImage,
				new Point(anchor.X + AnchorRadius, anchor.Y - AnchorRadius), new Point(anchor.X + AnchorRadius, anchor.Y + AnchorRadius),
				colorDecider);
		}

		void DrawEdge((int a, int b) edge, bool clear)
		{
			var colorDecider = clear ? (Func<int, int>)(i => originalVisualImagePixels[i]) : i => edgeColor;
			ImagePainter.DrawAnyLineFat(VisualImage, ZoomedShiftedPoint(anchors[edge.a]), ZoomedShiftedPoint(anchors[edge.b]), colorDecider);
		}

		#endregion

		#region Touch actions

		protected override TouchPointerStyle GetTouchPointerStyleCore(Point imagePoint)
		{
			if (IsTouching)
			{
				return originalTouchPointerStyle;
			}

			foreach (var anchor in anchors)
			{
				if (IsOverAnchor(imagePoint, ZoomedPoint(anchor)))
				{
					return TouchPointerStyle.Cross;
				}
			}

			return base.GetTouchPointerStyleCore(imagePoint);
		}
		TouchPointerStyle originalTouchPointerStyle;

		protected override void OnTouchedCore(Point imagePoint)
		{
			base.OnTouchedCore(imagePoint);

			originalTouchPointerStyle = GetTouchPointerStyleCore(imagePoint);

			originalAnchorPointIndex = -1;
			for (int i = 0; i < anchors.Count; i++)
			{
				if (IsOverAnchor(imagePoint, ZoomedPoint(anchors[i])))
				{
					originalAnchorPointIndex = i;
					originalZoomedAnchorPoint = ZoomedPoint(anchors[i]);
					break;
				}
			}
		}
		int originalAnchorPointIndex;
		Point originalZoomedAnchorPoint;

		protected override void OnUntouchedCore(Point imagePoint)
		{
			base.OnUntouchedCore(imagePoint);

			originalTouchPointerStyle = TouchPointerStyle.None;
			originalAnchorPointIndex = -1;
		}

		protected override void OnShiftCore(Point imagePoint, Size shiftSize)
		{
			if (IsTouching && originalAnchorPointIndex >= 0)
			{
				DrawAnchorsCore(true);
				var newZoomedAnchorPoint = new Point(originalZoomedAnchorPoint.X + shiftSize.Width, originalZoomedAnchorPoint.Y + shiftSize.Height);
				originalZoomedAnchorPoint = newZoomedAnchorPoint;
				anchors[originalAnchorPointIndex] = UnZoomedPoint(newZoomedAnchorPoint);

				quickUpdate = true;
				UpdateVisualImage();
			}
			else
			{
				base.OnShiftCore(imagePoint, shiftSize);
			}
		}

		static bool IsOverAnchor(Point imagePoint, Point anchorPoint)
		{
			return IsInsideRadius(imagePoint.X, anchorPoint.X) && IsInsideRadius(imagePoint.Y, anchorPoint.Y);
		}

		static bool IsInsideRadius(int x1, int x2, int radius = AnchorRadius)
		{
			return Math.Abs(x1 - x2) <= radius;
		}

		#endregion
	}
}
