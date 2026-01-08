using UnityEngine;

// -=-=-=- //

namespace Vectorier {
	public static class Paths {
		public static readonly float FrameRate = 60f;

		public static readonly float UnitScale = 100f;
		public static readonly float UnitValue =  (25f / 32f) / UnitScale; // 0.0078125;

		public static readonly int UnitPrecision = 3; // standard
		public static readonly int UnitPrecisionMin = 0;
		public static readonly int UnitPrecisionMax = 6;

		public static readonly string AttributeSeparator = "|";

		public static class Extensions {
			public static class File {
				public static readonly string XML = "xml";
				public static readonly string Shockwave = "swf";

				public static class Archive {
					public static readonly string Compiled = "dz";
					public static readonly string Config = "dcl";
				}

				public static class Audio {
					public static readonly string Music = "mp3";
					public static readonly string Sound = "wav";
				}

				public static class Image {
					public static readonly string[] Static = { "png", "jpg", "jpeg" };
					public static readonly string Atlas = "plist";
				}

				public static readonly string Animation = "bin";
			}
		}

		public static class Animation {
			public static float FrameRate = 20f;
		}

		public static class Audio {
			public static class Sound {
				public static readonly int SampleRate = 22050;
				public static readonly int Channels = 1;
			}
		}
	}
}