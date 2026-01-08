#if UNITY_EDITOR
using UnityEngine;

using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Text.RegularExpressions;

// -=-=-=- //

namespace Logger {
	public static class Utils {
		public static class Variables {
			public static string Script    = "{script}";
			public static string Separator = "{sep}";
			public static string Line      = "{line}";
			public static string Level     = "{lvl}";
			public static string Message   = "{msg}";
		}

		// apply bold and italic
		public static string ApplyStyle(string text, bool bold, bool italic) {
			if (bold) {
				text = $"<b>{text}</b>";
			}

			if (italic) {
				text = $"<i>{text}</i>";
			};

			return text;
		}

		// apply color styling
		public static string Colorize(string text, bool useColor, Color color) {
			if (!useColor) {
				return text;
			}

			string hex = ColorUtility.ToHtmlStringRGBA(color);
			return $"<color=#{hex}>{text}</color>";
		}

		// apply path
		public static string Repath(string path, bool nameOnly) {
			return nameOnly ? Path.GetFileNameWithoutExtension(path) : path;
		}

		// set color in EditorPrefs
		public static void SetColor(string key, Color color) {
			UnityEditor.EditorPrefs.SetString(key, ColorUtility.ToHtmlStringRGBA(color));
		}

		// get color from EditorPrefs
		public static Color GetColor(string key, Color defaultColor) {
			if (UnityEditor.EditorPrefs.HasKey(key)) {
				if (ColorUtility.TryParseHtmlString("#" + UnityEditor.EditorPrefs.GetString(key), out Color color)) {
					return color;
				}
			}

			return defaultColor;
		}

		// -=-=-=- //

		public static string[] SeparateCase(string input) {
			if (string.IsNullOrEmpty(input)) {
				return new string[0];
			}

			return Regex.Split(input, @"(?=[A-Z])");
		}
	}
}
#endif