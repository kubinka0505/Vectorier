using System;

using UnityEngine;
using UnityEditor;

using System.Linq;
using System.Collections.Generic;

// -=-=-=- //

#nullable enable

namespace Vectorier.Settings {
	public static class GUILayoutHelpers {
		public static float globalLabelWidth = 300;

		private static string IndentText(
			string text,
			int indentLevel,

			bool top,
			bool left,
			bool right,
			bool bottom
		) {
			if (string.IsNullOrEmpty(text)) {
				return "";
			}

			string indent = new string(' ', indentLevel * 4);

			var lines = text.Split('\n');
			var result = new List<string>();

			// TOP padding = indentLevel newlines
			if (top) {
				for (int i = 0; i < indentLevel; i++) {
					result.Add("");
				}
			}

			for (int i = 0; i < lines.Length; i++) {
				string line = lines[i].TrimEnd('\r');

				if (left) {
					line = indent + line;
				}

				if (right) {
					line += indent;
				}

				result.Add(line);
			}

			// BOTTOM padding = indentLevel newlines
			if (bottom) {
				for (int i = 0; i < indentLevel; i++) {
					result.Add("");
				}
			}

			return string.Join("\n", result);
		}

		public static void LabelField(
			string label,

			int? fontSize = null,

			bool? bold = null,
			bool? italic = null,

			string? tooltip = null,
			float? space = null
		) {
			float labelWidth = space ?? globalLabelWidth;

			GUIStyle style = new GUIStyle(EditorStyles.label);

			if (fontSize.HasValue) {
				style.fontSize = fontSize.Value;
			}

			if (bold.HasValue) {
				style.fontStyle = bold.Value
					? (italic.HasValue && italic.Value ? FontStyle.BoldAndItalic : FontStyle.Bold)
					: (italic.HasValue && italic.Value ? FontStyle.Italic : FontStyle.Normal);
			}

			EditorGUILayout.LabelField(
				new GUIContent(label, tooltip),
				style,
				GUILayout.Width(labelWidth)
			);
		}

		public static void TextAreaDisplay(
			string text,

			float? width = null,
			float? height = null,

			bool expandWidth = false,
			bool expandHeight = false,

			bool padTop = true,
			bool padLeft = true,
			bool padRight = true,
			bool padBottom = true,

			int padLevel = 1
		) {
			string display = padLevel > 0 ? IndentText(
				text, padLevel,

				top: padTop,
				left: padLeft,
				right: padRight,
				bottom: padBottom
			) : text;

			GUI.enabled = false;

			var options = new List<GUILayoutOption>();

			if (width.HasValue) {
				options.Add(GUILayout.Width(width.Value));
			}

			if (height.HasValue) {
				options.Add(GUILayout.Height(height.Value));
			}

			options.Add(GUILayout.ExpandWidth(expandWidth));
			options.Add(GUILayout.ExpandHeight(expandHeight));

			GUILayout.TextArea(display, options.ToArray());

			GUI.enabled = true;
		}

		public static string TextField(
			string label,
			string? tooltip,

			string value,

			float? space = null
		) {
			float labelWidth = space ?? globalLabelWidth;

			EditorGUILayout.BeginHorizontal();

			LabelField(
				label: label,
				tooltip: tooltip,
				space: labelWidth
			);
			value = EditorGUILayout.TextField(value, GUILayout.ExpandWidth(true));

			EditorGUILayout.EndHorizontal();

			return value;
		}

		public static bool CheckBox(
			string label,
			string? tooltip,

			bool value,

			float? space = null
			// bool? left = null;
		) {
			float labelWidth = space ?? globalLabelWidth;
			// bool alignLeft = left ?? true;

			EditorGUILayout.BeginHorizontal();

			LabelField(
				label: label,
				tooltip: tooltip,
				space: labelWidth
			);
			value = EditorGUILayout.Toggle(value, GUILayout.ExpandWidth(true));

			EditorGUILayout.EndHorizontal();

			return value;
		}

		public static void DrawHorizontalLine(
			float height = 1.25f,

			float margin_top = 8f,
			float margin_bottom = 8f,

			Color? color = null
		) {
			Color lineColor;

			if (color.HasValue) {
				lineColor = color.Value;
			} else {
				float col = EditorGUIUtility.isProSkin ? col = 0.4f : 0.6f;
				lineColor = new Color(col, col, col, 1f);
			}

			GUILayout.Space(margin_top);

			Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(height));
			EditorGUI.DrawRect(rect, lineColor);

			GUILayout.Space(margin_bottom);
		}

		public static string ParseHelpBoxString(
			string value,
			char sep = ' ',
			int sepMul = 3,
			int wrap = 50,
			bool wrapEndWord = true
		) {
			if (string.IsNullOrEmpty(value)) {
				return "";
			}

			string indent = new string(sep, sepMul);

			var lines = value
				.Trim()
				.Split('\n')
				.Select(line => line.TrimEnd())
				.ToList();

			var result = new List<string>();

			foreach (var line in lines) {
				// preserve empty lines
				if (string.IsNullOrWhiteSpace(line)) {
					result.Add("");
					continue;
				}

				var processedLine = indent + line.TrimStart();

				if (wrap > 0) {
					result.AddRange(WrapLine(processedLine, wrap, wrapEndWord, indent));
				} else {
					result.Add(processedLine);
				}
			}

			return "\n" + string.Join("\n", result) + "\n";
		}

		private static IEnumerable<string> WrapLine(
			string line,
			int wrap,
			bool wrapEndWord,
			string indent
		) {
			int start = 0;
			bool firstLine = true;

			while (start < line.Length) {
				int remaining = line.Length - start;
				int count = Math.Min(wrap, remaining);

				if (wrapEndWord && count < remaining) {
					int lastSpace = line.LastIndexOf(' ', start + count);
					if (lastSpace > start) count = lastSpace - start;
				}

				string segment = line.Substring(start, count).TrimEnd();

				// first line already has indentation
				// subsequent wrapped lines keep same indent
				if (!firstLine) {
					yield return indent + segment;
				} else {
					yield return segment;
				}

				firstLine = false;
				start += count;

				// skip leading spaces
				while (start < line.Length && line[start] == ' ') start++;
			}
		}
	}
}