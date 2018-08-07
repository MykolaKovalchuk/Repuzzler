using System;
using System.Collections.Generic;
using Ravlyk.Common;
using Ravlyk.Drawing.ImageProcessor;

namespace Ravlyk.UI.ImageProcessor
{
	public class VisualAnchorsController : VisualZoomController
	{
		public VisualAnchorsController(IImageProvider imageProvider, Size imageBoxSize = default(Size)) : base(imageProvider, imageBoxSize)
		{
		}

		#region Anchors

		public class Anchor
		{
			public Point Location { get; set; }
			public int Color { get; set; }
		}

		public class Edge
		{
			public Point StartXYAnchorIndexes { get; set; }
			public Size StartShift { get; set; }
			public Point EndXYAnchorIndexes { get; set; }
			public Size EndShift { get; set; }
			public int Color { get; set; }
		}

		readonly List<Anchor> anchors = new List<Anchor>();
		readonly List<Edge> edges = new List<Edge>();

		public IList<Anchor> AllAnchors => anchors;

		public void AddAnchor(Point location, int color)
		{
			AddAnchor(new Anchor { Location = location, Color = color });
		}

		void AddAnchor(Anchor newAnchor)
		{
			anchors.Add(newAnchor);

			quickUpdate = false;
			UpdateVisualImage();
		}

		public void AddEdge(int firstAnchorIndex, int secondAnchorIndex, int color)
		{
			AddEdge(new Point(firstAnchorIndex, firstAnchorIndex), new Point(secondAnchorIndex, secondAnchorIndex), color);
		}

		public void AddEdge(Point startIndexes, Point endIndexes, int color)
		{
			AddEdge(startIndexes, endIndexes, default(Size), default(Size), color);
		}

		public void AddEdge(Point startIndexes, Point endIndexes, Size startShift, Size endShift, int color)
		{
			if (startIndexes.X < 0 || startIndexes.X >= anchors.Count)
			{
				throw new ArgumentNullException($"1st anchor X index is out of bounds: {startIndexes.X}");
			}
			if (startIndexes.Y < 0 || startIndexes.Y >= anchors.Count)
			{
				throw new ArgumentNullException($"1st anchor Y index is out of bounds: {startIndexes.Y}");
			}
			if (endIndexes.X < 0 || endIndexes.X >= anchors.Count)
			{
				throw new ArgumentNullException($"2nd anchor X index is out of bounds: {endIndexes.X}");
			}
			if (endIndexes.Y < 0 || endIndexes.Y >= anchors.Count)
			{
				throw new ArgumentNullException($"2nd anchor Y index is out of bounds: {endIndexes.Y}");
			}

			edges.Add(new Edge
			{
				StartXYAnchorIndexes = startIndexes,
				StartShift = startShift,
				EndXYAnchorIndexes = endIndexes,
				EndShift = endShift,
				Color = color
			});

			quickUpdate = true;
			UpdateVisualImage();
		}

		public void ClearAllAnchors()
		{
			DrawAnchorsCore(true);
			anchors.Clear();
			edges.Clear();
			quickUpdate = false;
			UpdateVisualImage();
		}

		#endregion

		#region Update visual image

		bool quickUpdate;
		int[] originalVisualImagePixels;

		const int AnchorRadius = 8;

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
				DrawAnchor(anchor, clear);
			}
		}

		void DrawAnchor(Anchor anchor, bool clear)
		{
			var colorDecider = clear ? (Func<int, int>)(i => originalVisualImagePixels[i]) : i => anchor.Color;
			var point = ZoomedShiftedPoint(anchor.Location);

			ImagePainter.DrawAnyLineFat(VisualImage,
				new Point(point.X - AnchorRadius, point.Y - AnchorRadius), new Point(point.X + AnchorRadius, point.Y - AnchorRadius),
				colorDecider);
			ImagePainter.DrawAnyLineFat(VisualImage,
				new Point(point.X - AnchorRadius, point.Y + AnchorRadius), new Point(point.X + AnchorRadius, point.Y + AnchorRadius),
				colorDecider);
			ImagePainter.DrawAnyLineFat(VisualImage,
				new Point(point.X - AnchorRadius, point.Y - AnchorRadius), new Point(point.X - AnchorRadius, point.Y + AnchorRadius),
				colorDecider);
			ImagePainter.DrawAnyLineFat(VisualImage,
				new Point(point.X + AnchorRadius, point.Y - AnchorRadius), new Point(point.X + AnchorRadius, point.Y + AnchorRadius),
				colorDecider);
		}

		void DrawEdge(Edge edge, bool clear)
		{
			var colorDecider = clear ? (Func<int, int>)(i => originalVisualImagePixels[i]) : i => edge.Color;
			ImagePainter.DrawAnyLineFat(VisualImage,
				ZoomedShiftedPoint(
					new Point(
						anchors[edge.StartXYAnchorIndexes.X].Location.X + edge.StartShift.Width,
						anchors[edge.StartXYAnchorIndexes.Y].Location.Y + edge.StartShift.Height)),
				ZoomedShiftedPoint(
					new Point(
						anchors[edge.EndXYAnchorIndexes.X].Location.X + edge.EndShift.Width,
						anchors[edge.EndXYAnchorIndexes.Y].Location.Y + edge.EndShift.Height)),
				colorDecider);
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
				if (IsOverAnchor(imagePoint, ZoomedPoint(anchor.Location)))
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
				if (IsOverAnchor(imagePoint, ZoomedPoint(anchors[i].Location)))
				{
					originalAnchorPointIndex = i;
					originalZoomedAnchorPoint = ZoomedPoint(anchors[i].Location);
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
				anchors[originalAnchorPointIndex].Location = UnZoomedPoint(newZoomedAnchorPoint);

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
