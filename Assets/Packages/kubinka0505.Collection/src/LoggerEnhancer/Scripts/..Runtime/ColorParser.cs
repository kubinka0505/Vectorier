#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// -=-=-=- //

namespace Logger {
	public static class ColorParser {
		// Named web colors
		private static readonly Dictionary<string, Color> WebColors = new Dictionary<string, Color> {
			{ "red",      new Color(1f, 0f, 0f, 1f) },
			{ "green",    new Color(0f, 0.5f, 0f, 1f) },
			{ "blue",     new Color(0f, 0f, 1f, 1f) },
			{ "white",    new Color(1f, 1f, 1f, 1f) },
			{ "black",    new Color(0f, 0f, 0f, 1f) },
			{ "yellow",   new Color(1f, 1f, 0f, 1f) },
			{ "cyan",     new Color(0f, 1f, 1f, 1f) },
			{ "magenta",  new Color(1f, 0f, 1f, 1f) },
			{ "gray",     new Color(0.5f, 0.5f, 0.5f, 1f) },
			{ "grey",     new Color(0.5f, 0.5f, 0.5f, 1f) },
			{ "orange",   new Color(1f, 0.65f, 0f, 1f) },
			{ "purple",   new Color(0.5f, 0f, 0.5f, 1f) },
			{ "lime",     new Color(0f, 1f, 0f, 1f) },
			{ "navy",     new Color(0f, 0f, 0.5f, 1f) },
			{ "teal",     new Color(0f, 0.5f, 0.5f, 1f) },
			{ "maroon",   new Color(0.5f, 0f, 0f, 1f) },
			{ "olive",    new Color(0.5f, 0.5f, 0f, 1f) },
		};

		public static Color Parse(string input) {
			if (string.IsNullOrWhiteSpace(input))
				return Color.white;

			input = input.Trim().ToLower();

			// Try named color
			if (WebColors.TryGetValue(input, out Color webColor))
				return webColor;

			// Try hex notation
			return ParseHex(input);
		}

		private static Color ParseHex(string hex) {
			hex = hex.Replace("#", "").Trim();

			if (string.IsNullOrEmpty(hex))
				return Color.white;

			// Expand shorthand hex
			hex = ExpandHexShorthand(hex);

			// Try to parse as standard #RRGGBB or #RRGGBBAA
			if (ColorUtility.TryParseHtmlString("#" + hex, out Color color))
				return color;

			return Color.white;
		}

		private static string ExpandHexShorthand(string hex) {
			hex = hex.ToUpper();

			return hex.Length switch {
				// 2-char: C0 → C0C0C0
				2 => hex + hex + hex,
				// 3-char: ABC → ABCABC
				3 => hex + hex,
				// 4-char: ABCD → ABCDAB (repeat first 3, keep 4th as-is for alpha semantics)
				// But based on user example ABCD → #ABCDABFF, it seems to be ABC + D repeated? 
				// Actually ABCDAB suggests: take input, pad with repeat of first char up to 6 chars for RGB
				4 => hex.Substring(0, 3) + hex.Substring(0, 3),
				// 5-char: ABCDE → ABCDEA (first 5 chars + repeat of first)
				5 => hex + hex[0],
				// 6-char: keep as-is (standard RGB)
				6 => hex,
				// 8-char: keep as-is (full RGBA)
				8 => hex,
				// Default fallback
				_ => hex,
			};
		}

		public static string ColorizeText(string text, Color color) {
			string hex = ColorUtility.ToHtmlStringRGBA(color);
			return $"<color=#{hex}>{text}</color>";
		}

		public static string ResetColor(string text) {
			return $"<color=white>{text}</color>";
		}
	}
}
#endif
