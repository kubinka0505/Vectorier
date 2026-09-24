using UnityEngine;
using System.Xml;

#nullable enable
#pragma warning disable CS8618

// -=-=-=- //

namespace Vectorier.Core.Components {
	public static class TrapezoidComponent {
		public static void Convert(
			GameObject go,
			XmlNode node,
			XmlElement? parentElement = null,
			int floatPrecision = -1,
			bool legacy = false
		) {
			if (!TrapezoidRules.CanExport(go, legacy)) {
				return;
			}

			var data = TrapezoidExtractor.Extract(go);
			var xml = TrapezoidXmlBuilder.Build(node.OwnerDocument!, go, data, floatPrecision);

			var parent = parentElement ?? node.FirstChild;
			if (parent == null) {
				return;
			}

			parent.AppendChild(xml);
		}
	}

	internal static class TrapezoidRules {
		public static bool CanExport(GameObject go, bool legacy) {
			if (!Helpers.WriteMode.CanWrite(go, legacy)) {
				return false;
			}

			string name = Helpers.Get.Name(go);
			return name == "trapezoid_type1" || name == "trapezoid_type2";
		}
	}

	internal static class TrapezoidExtractor {
		public static TrapezoidData Extract(GameObject go) {
			var sr = go.GetComponent<SpriteRenderer>();

			if (sr == null || sr.sprite == null) {
				throw new System.Exception($"Missing SpriteRenderer or sprite on {go.name}");
			}

			return new TrapezoidData {
				Name = Helpers.Get.Name(go),
				Sprite = sr.sprite,
				Transform = go.transform,

				IsFlipped = sr.flipX,
				OrigType = Helpers.Get.Name(go).EndsWith("type1"),

				Bounds = sr.sprite.bounds
			};
		}
	}

	internal static class TrapezoidGeometry {
		public static (float width, float height, bool type1) Compute(TrapezoidData d) {
			float width = d.Bounds.size.x * Game.Properties.UnitScale;
			float height = d.Bounds.size.y * Game.Properties.UnitScale;

			float aw = width * d.Transform.lossyScale.x;
			float ah = height * d.Transform.lossyScale.y;

			bool actualType1 = d.IsFlipped ? !d.OrigType : d.OrigType;

			return (aw, ah, actualType1);
		}
	}

	internal static class TrapezoidXmlBuilder {
		public static XmlElement Build(
			XmlDocument xml,
			GameObject go,
			TrapezoidData data,
			int precision
		) {
			var el = xml.CreateElement("Trapezoid");

			var (width, height, type1) = TrapezoidGeometry.Compute(data);

			Vector3 pos = data.Transform.position;

			// Position (only if not flipped)
			if (!data.IsFlipped) {
				if (pos.x != 0) {
					el.SetAttribute("X", Helpers.Format.Multiply(pos.x, precision));
				}

				if (pos.y != 0) {
					el.SetAttribute("Y", Helpers.Format.Multiply(-pos.y, precision));
				}
			}

			// Width
			el.SetAttribute("Width", Helpers.Format.ToString(width, precision));

			// Height + Type
			if (type1) {
				el.SetAttribute("Height", "1");
				el.SetAttribute("Height1", Helpers.Format.ToString(height + 1, precision));
				el.SetAttribute("Type", "1");
			} else {
				el.SetAttribute("Height", Helpers.Format.ToString(height + 1, precision));
				el.SetAttribute("Height1", "1");
				el.SetAttribute("Type", "2");
			}

			Helpers.WriteMode.Apply(go, el);

			return el;
		}
	}

	internal class TrapezoidData {
		public string Name;
		public Sprite Sprite;
		public Transform Transform;

		public bool IsFlipped;
		public bool OrigType;

		public Bounds Bounds;
	}
}