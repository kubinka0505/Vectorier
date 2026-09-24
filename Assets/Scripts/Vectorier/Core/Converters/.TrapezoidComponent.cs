using System.Xml;
using UnityEngine;

#nullable enable

// -=-=-=- //

using TrapezoidData = Vectorier.Core.Helpers.BoundNormalizer.TrapezoidTransformData;

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

			TrapezoidData data = TrapezoidExtractor.Extract(go);
			if (!data.IsValid) {
				return;
			}

			XmlDocument doc = node.OwnerDocument ?? (XmlDocument)node;
			XmlElement mainElement = TrapezoidXmlBuilder.Build(doc, go, data, floatPrecision);

			XmlNode targetParent = parentElement ?? node.FirstChild;
			targetParent?.AppendChild(mainElement);
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

				if (sr == null || !sr.enabled || sr.sprite == null) {
					return new TrapezoidData {
						IsValid = false
					};
				}

				bool isType1 = Helpers.Get.Name(go).EndsWith("type1");
				float unitScale = Game.Properties.UnitScale;

				Vector2 unscaledBounds = sr.sprite.bounds.size;
				float baseW = unscaledBounds.x * unitScale;
				float baseH = unscaledBounds.y * unitScale;

				// Corner anchors relative to pivot
				Vector2 pA = isType1 ? new Vector2(0, 0) : new Vector2(0, baseH);
				Vector2 pB = isType1 ? new Vector2(0, baseH) : new Vector2(0, 0);
				Vector2 pC = isType1 ? new Vector2(baseW, baseH) : new Vector2(baseW, 0);

				if (sr.flipX) {
					pA.x = baseW - pA.x;
					pB.x = baseW - pB.x;
					pC.x = baseW - pC.x;
				}

				if (sr.flipY) {
					pA.y = baseH - pA.y;
					pB.y = baseH - pB.y;
					pC.y = baseH - pC.y;
				}

				Vector3 scale = go.transform.lossyScale;
				pA = Vector2.Scale(pA, new Vector2(scale.x, scale.y));
				pB = Vector2.Scale(pB, new Vector2(scale.x, scale.y));
				pC = Vector2.Scale(pC, new Vector2(scale.x, scale.y));

				Quaternion rot = Quaternion.Euler(go.transform.eulerAngles);
				Vector3 rawPos = go.transform.position;

				Vector3 worldA = rawPos + rot * new Vector3(pA.x / unitScale, pA.y / unitScale, 0);
				Vector3 worldB = rawPos + rot * new Vector3(pB.x / unitScale, pB.y / unitScale, 0);
				Vector3 worldC = rawPos + rot * new Vector3(pC.x / unitScale, pC.y / unitScale, 0);

				// Convert to Pixel Space
				Vector2 pixA = new Vector2(worldA.x * unitScale, -worldA.y * unitScale);
				Vector2 pixB = new Vector2(worldB.x * unitScale, -worldB.y * unitScale);
				Vector2 pixC = new Vector2(worldC.x * unitScale, -worldC.y * unitScale);

				bool isGoingUp = pixC.y < pixB.y;

				float finalWidth = Mathf.Abs(pixC.x - pixA.x);
				float finalHeight = Mathf.Abs(pixB.y - pixA.y);

				float finalX = Mathf.Min(pixA.x, pixC.x);
				float finalY = Mathf.Min(pixA.y, pixB.y);

				float h = 1f;
				float h1 = finalHeight + 1f;

				TrapezoidData.Types type = isGoingUp ? TrapezoidData.Types.Type1 : TrapezoidData.Types.Type2;
				if (type == TrapezoidData.Types.Type2) {
					float temp = h;
					h = h1;
					h1 = temp;
				}

				return new TrapezoidData {
					X = finalX / unitScale,
					Y = finalY / unitScale,
					Width = finalWidth,
					Height = h,
					Height1 = h1,
					Type = type,
					IsValid = true
				};
			}
		}

		internal static class TrapezoidXmlBuilder {
			public static XmlElement Build(
				XmlDocument doc,
				GameObject go,
				TrapezoidData data,
				int floatPrecision
			) {
				XmlElement mainElement = doc.CreateElement("Trapezoid");

				if (data.X != 0) {
					mainElement.SetAttribute("X", Helpers.Format.Multiply(data.X, floatPrecision));
				}

				if (data.Y != 0) {
					mainElement.SetAttribute("Y", Helpers.Format.Multiply(data.Y, floatPrecision));
				}

				if (data.Width != 0) {
					mainElement.SetAttribute("Width", Helpers.Format.ToString(data.Width, floatPrecision));
				}

				mainElement.SetAttribute("Height", Helpers.Format.ToString(data.Height, floatPrecision));
				mainElement.SetAttribute("Height1", Helpers.Format.ToString(data.Height1, floatPrecision));
				mainElement.SetAttribute("Type", Helpers.Format.ToString((int)data.Type, floatPrecision));

				Helpers.WriteMode.Apply(go, mainElement);

				return mainElement;
			}
		}
	}
}