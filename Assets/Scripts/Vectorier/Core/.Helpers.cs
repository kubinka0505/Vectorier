using UnityEngine;

using System;
using System.Xml;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#nullable enable

// -=-=-=- //

namespace Vectorier.Core.Helpers {
	public static class Format {
		public static string Multiply(
			float value,
			int? precision = null
		) {
			float scaledValue = value * Game.Properties.UnitScale;
			return ToString(scaledValue, precision);
		}

		public static string ToString(
			float value,

			int? precision = null
		) {
			int usedPrecision = precision ?? Game.Properties.UnitPrecision;
			usedPrecision = Mathf.Clamp(usedPrecision, Game.Properties.UnitPrecisionMin, Game.Properties.UnitPrecisionMax);

			string s = value.ToString("F" + usedPrecision, CultureInfo.InvariantCulture);

			if (s.Contains(".")) {
				s = s.TrimEnd('0').TrimEnd('.');
			}

			return s;
		}
	}

	public static class WriteMode {
		public static bool CanWrite(GameObject go, bool legacy) {
			var writeMode = go.GetComponent<GameWriteMode>();

			var mode =
				(writeMode != null && writeMode.enabled)
					? writeMode.Value
					: GameWriteMode.Mode.Any;

			return mode switch {
				GameWriteMode.Mode.Legacy => legacy,
				GameWriteMode.Mode.Modern => !legacy,
				GameWriteMode.Mode.Any => true,
				_ => true
			};
		}

		public static void Apply(
			GameObject go,
			XmlElement element
		) {
			XmlDocument xml = element.OwnerDocument;
			string writeModeValue = "any";

			var writeMode = go.GetComponent<VectorierWriteMode>();

			if (writeMode != null && writeMode.enabled) {
				writeModeValue = writeMode.GetWriteModeValue();
				writeMode.AddWriteModeProperties(xml, element, writeModeValue);
			}
		}
	}

	public static class Get {
		public static (float X, float Y) Position(GameObject go) {
			if (go == null) return (0f, 0f);
			var norm = ExtractNormalized(go);
			return norm.IsValid ? (norm.X, norm.Y) : (0f, 0f);
		}

		public static (float width, float height) Size(GameObject go) {
			if (go == null) return (0f, 0f);
			var norm = ExtractNormalized(go);
			return norm.IsValid ? (norm.Width, norm.Height) : (0f, 0f);
		}

		public static BoundNormalize.NormalizedTransform ExtractNormalized(GameObject go, int precision = 2) {
			if (go == null) return default;

			Vector3 rawPos = go.transform.position;
			Vector3 lossyScale = go.transform.lossyScale;
			Vector3 eulerAngles = go.transform.eulerAngles;
			Vector2 unscaledSpriteBounds = Vector2.zero;

			bool flipX = false;
			bool flipY = false;

			var sr = go.GetComponent<SpriteRenderer>();
			if (sr != null) {
				if (sr.sprite != null) {
					unscaledSpriteBounds = sr.sprite.bounds.size;
				}
				flipX = sr.flipX;
				flipY = sr.flipY;
			}

			bool isCenter = IsCenterPivot(go);

			var normalized = BoundNormalize.Normalize(
				rawPos,
				unscaledSpriteBounds,
				lossyScale,
				eulerAngles,
				flipX,
				flipY,
				isCenter,
				precision
			);

			if (!normalized.IsValid) {
				return normalized;
			}

			// Apply XML Unit Scale multiplier
			float scaleFactor = 1f / Game.Properties.UnitScale;

			return new BoundNormalize.NormalizedTransform {
				X = normalized.X * scaleFactor,
				Y = normalized.Y * scaleFactor,
				Z = normalized.Z * scaleFactor,
				Width = normalized.Width * scaleFactor,
				Height = normalized.Height * scaleFactor,
				IsValid = true
			};
		}

		public static bool IsCenterPivot(GameObject go) {
			if (go == null) return false;

			var sr = go.GetComponent<SpriteRenderer>();
			if (sr == null || sr.sprite == null) return false;

			Vector2 pivot = sr.sprite.pivot;
			Rect rect = sr.sprite.rect;

			return Mathf.Approximately(pivot.x, rect.width / 2f) &&
			       Mathf.Approximately(pivot.y, rect.height / 2f);
		}

		public static string Name(
			object obj,
			string? pattern = null
		) {
			string GlobalRegex = @"\s*(\((?>[^()]+|(?<open>\()|(?<-open>\)))*(?(open)(?!))\)|\[(?>[^\[\]]+|(?<open>\[)|(?<-open>\]))*(?(open)(?!))\])";
			string name;

			if (obj is GameObject go) {
				// obj is a GameObject
				name = go.name;
			} if (obj is string str) {
				// obj is already a string
				name = str;
			} else {
				// fallback: use ToString()
				name = obj?.ToString() ?? string.Empty;
			}

			if (string.IsNullOrEmpty(pattern)) {
				pattern = GlobalRegex;
			}

			name = name.Trim();

			return Regex.Replace(name, pattern, string.Empty);
		}
	}
}