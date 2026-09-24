#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Collections.Generic;

// -=-=-=- //

namespace Logger {
	public static class FormatStringProcessor {
		private static readonly Regex ColorTokenPattern = new Regex(@"\{color\.([^}]+)\}");
		private static readonly Regex RgbFunctionPattern = new Regex(@"rgb\(""([^""]+)""\)");
		private static readonly Regex CharsTokenPattern = new Regex(@"\{chars\.([^}]+)\}");

		public static string ProcessFormat(string format, Dictionary<string, string> variables, Dictionary<string, string> charMappings = null) {
			if (charMappings == null) {
				charMappings = new Dictionary<string, string> {
					{ "open", "[" },
					{ "close", "]" }
				};
			}

			// Replace char tokens first
			string result = ProcessCharTokens(format, charMappings);

			// Process color tokens
			result = ProcessColorTokens(result);

			// Finally, replace variables (keys already include braces from Utils.Variables)
			foreach (var kvp in variables) {
				result = result.Replace(kvp.Key, kvp.Value);
			}

			return result;
		}

		private static string ProcessCharTokens(string format, Dictionary<string, string> mappings) {
			return CharsTokenPattern.Replace(format, match => {
				string charKey = match.Groups[1].Value;
				return mappings.ContainsKey(charKey) ? mappings[charKey] : match.Value;
			});
		}

		private static string ProcessColorTokens(string format) {
			return ColorTokenPattern.Replace(format, match => {
				string colorSpec = match.Groups[1].Value;

				// Handle {color.init} - use default Unity log color
				if (colorSpec == "init") {
					Color defaultColor = GetDefaultLogColor();
					string hex = ColorUtility.ToHtmlStringRGBA(defaultColor);
					return $"<color=#{hex}>";
				}

				// Handle {color.rgb(...)} - parse color and convert
				var rgbMatch = RgbFunctionPattern.Match(colorSpec);
				if (rgbMatch.Success) {
					string colorArg = rgbMatch.Groups[1].Value;
					Color color = ColorParser.Parse(colorArg);
					string hex = ColorUtility.ToHtmlStringRGBA(color);
					return $"<color=#{hex}>";
				}

				// Handle named color {color.gray}, {color.red}, etc.
				Color namedColor = ColorParser.Parse(colorSpec);
				string namedHex = ColorUtility.ToHtmlStringRGBA(namedColor);
				return $"<color=#{namedHex}>";
			});
		}

		private static Color GetDefaultLogColor() {
			// Detect Unity theme and return appropriate default log text color
			if (EditorGUIUtility.isProSkin) {
				// Dark theme: light gray/white text
				return new Color(0.9f, 0.9f, 0.9f, 1f);  // Light gray
			} else {
				// Light theme: dark gray/black text
				return new Color(0.1f, 0.1f, 0.1f, 1f);  // Near black
			}
		}

		// Utility to extract color from a spec and wrap text
		public static string WrapWithColor(string text, string colorSpec) {
			Color color = ColorParser.Parse(colorSpec);
			string hex = ColorUtility.ToHtmlStringRGBA(color);
			return $"<color=#{hex}>{text}</color>";
		}
	}
}
#endif
