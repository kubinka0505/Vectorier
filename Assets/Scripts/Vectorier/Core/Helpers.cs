using UnityEngine;
using UnityEditor;

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
			if (go == null) {
				return (0f, 0f);
			}

			bool hasVisuals = (go.tag == "Image" || go.tag == "Backdrop" || go.tag == "Top Image") &&
				(go.GetComponent<SpriteRenderer>() != null || go.GetComponent<MeshFilter>() != null || go.GetComponent<MeshRenderer>() != null);

			if (hasVisuals) {
				var norm = BoundNormalizer.ExtractNormalized(go);

				if (norm.IsValid) {
					float ppu = Game.Properties.UnitScale;
					
					// Convert to pixels if ExtractNormalized returns meters
					return (norm.X * ppu, norm.Y * ppu);
				}
			}

			// Fallback for Holder/Container GameObjects:
			// Direct transform position converted into Flash pixel space
			Vector3 pos = go.transform.position;
			return (pos.x, -pos.y);
		}

		public static (float width, float height) Size(GameObject go) {
			if (go == null) {
				return (0f, 0f);
			}

			var norm = BoundNormalizer.ExtractNormalized(go);
			return norm.IsValid ? (norm.Width, norm.Height) : (0f, 0f);
		}

		public static bool IsCenterPivot(GameObject go) {
			if (go == null) {
				return false;
			}

			var sr = go.GetComponent<SpriteRenderer>();
			if (sr == null || sr.sprite == null) {
				return false;
			}

			Vector2 pivot = sr.sprite.pivot;
			Rect rect = sr.sprite.rect;

			return Mathf.Approximately(pivot.x, rect.width / 2f) &&
				   Mathf.Approximately(pivot.y, rect.height / 2f);
		}

		public static string Name(
			object obj,
			string? pattern = null
		) {
			string globalRegex = @"\s*(\((?>[^()]+|(?<open>\()|(?<-open>\)))*(?(open)(?!))\)|\[(?>[^\[\]]+|(?<open>\[)|(?<-open>\]))*(?(open)(?!))\])";
			string name;

			if (obj is GameObject go) {
				/*if (PrefabUtility.IsPartOfRegularPrefab(go)) {
					GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(go);

					name = prefab != null ? prefab.name : go.name;
				} else {*/ 
					name = go.name;
				//
			} else if (obj is string str) {
				name = str;
			} else {
				name = obj?.ToString() ?? string.Empty;
			}

			if (string.IsNullOrEmpty(pattern))
				pattern = globalRegex;

			name = name.Trim();

			return Regex.Replace(name, pattern, string.Empty);
		}
	}
}