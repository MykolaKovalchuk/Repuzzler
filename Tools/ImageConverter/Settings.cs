using System.IO;

namespace ImageConverter
{
	public static class Settings
	{
		public static string BoundsModel { get; set; } = "/mnt/DA92812D92810F67/Rubik/Models/L1Rubik/1807221237-VGG16-SGD.pb";
		public static string ImagesFolder { get; set; } = "/mnt/DA92812D92810F67/Rubik";
		public static string BoundsSubfolder { get; set; } = "Bounds";
		public static string DescreenedSubfolder { get; set; } = "Descreened";

		public static string BoundsFolder => Path.Combine(ImagesFolder, BoundsSubfolder);
		public static string DescreenedFolder => Path.Combine(ImagesFolder, DescreenedSubfolder);

		public static string GetBoundsFileName(string imageFileName)
		{
			return Path.Combine(BoundsFolder, Path.ChangeExtension(Path.GetFileName(imageFileName), "bounds"));
		}
	}
}
