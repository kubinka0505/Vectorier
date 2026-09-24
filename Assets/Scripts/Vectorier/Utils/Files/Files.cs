using System;
using System.IO;
using System.Globalization;

// -=-=-=- //

namespace Vectorier.Utils {
	public static class Files {
		// Core formatter
		private static string FormatBytes(long bytes) {
			string[] suf = { "", "K", "M", "G", "T", "P" };

			double absBytes = Math.Abs((double)bytes);
			int place = absBytes > 0
				? (int)Math.Floor(Math.Log(absBytes, 1024))
				: 0;

			place = Math.Min(place, suf.Length - 1);

			double num = absBytes / Math.Pow(1024, place);
			string precision = place == 0 ? "N0" : "N2";

			string formatted = (Math.Sign(bytes) * num)
				.ToString(precision, CultureInfo.InvariantCulture);

			return $"{formatted} {suf[place]}B";
		}

		// From byte number
		public static string BytesToString(long bytes) {
			return FormatBytes(bytes);
		}

		// From file path
		public static string BytesToString(string filePath) {
			return FormatBytes(new FileInfo(filePath).Length);
		}

		// From FileInfo
		public static string BytesToString(FileInfo file) {
			return FormatBytes(file.Length);
		}
	}
}