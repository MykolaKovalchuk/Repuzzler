using System.Collections.Generic;

namespace ImageConverter.Utils
{
	public static class RubikHelper
	{
		public static IEnumerable<(int a, int b)> RubikEdges =>
			rubikEdges ?? (rubikEdges = new[]
			{
				(0, 1),
				(0, 3),
				(0, 5),
				(1, 2),
				(2, 3),
				(3, 4),
				(4, 5),
				(5, 6),
				(6, 1),
			});
		static (int a, int b)[] rubikEdges;

		public static IEnumerable<(int a, int b)> HorizontalFlipPairs => new[] { (2, 6), (3, 5) };
	}
}