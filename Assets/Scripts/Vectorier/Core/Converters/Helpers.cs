using UnityEngine;

using System;
using System.Xml;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// -=-=-=- //

namespace Vectorier.Core {
	public static class Helpers {
		public static readonly string globalRegex = @"\s*(\((?>[^()]+|(?<open>\()|(?<-open>\)))*(?(open)(?!))\)|\[(?>[^\[\]]+|(?<open>\[)|(?<-open>\]))*(?(open)(?!))\])";

		public static string Multiply(
			float value,
			int? precision = null
		) {
			float scaledValue = value * Game.UnitScale;
			return ToString(scaledValue, precision);
		}

		public static string ToString(float value, int? precision = null) {
			int usedPrecision = precision ?? Game.UnitPrecision;
			usedPrecision = Mathf.Clamp(usedPrecision, Game.UnitPrecisionMin, Game.UnitPrecisionMax);

			string s = value.ToString("F" + usedPrecision, CultureInfo.InvariantCulture);

			if (s.Contains(".")) {
				s = s.TrimEnd('0').TrimEnd('.');
			}

			return s;
		}

		public static void ApplyWriteMode(
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

		public static class Get {
			public static string Name(
				object obj,
				string pattern = null
			) {
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
					pattern = globalRegex;
				}

				return Regex.Replace(name, pattern, string.Empty);
			}

			// todo: change output types
			public static (float X, float Y) Position(GameObject go) {
				float x = 0f;
				float y = 0f;

				if (go == null) {
					return (x, y);
				}

				x = go.transform.position.x;
				x *= Game.UnitScale / 100f;

				y = go.transform.position.y;
				y *= Game.UnitScale / 100f;

				return (x, y);
			}

			public static (float width, float height) Size(GameObject go) {
				var spriteRenderer = go.GetComponent<SpriteRenderer>();
				if (spriteRenderer == null || !spriteRenderer.enabled || spriteRenderer.sprite == null) {
					return (0f, 0f);
				}

				Bounds bounds = spriteRenderer.sprite.bounds;

				float width = bounds.size.x * Game.UnitScale;
				float height = bounds.size.y * Game.UnitScale;

				width *= go.transform.lossyScale.x;
				height *= go.transform.lossyScale.y;

				return (width, height);
			}
		}
	}
}