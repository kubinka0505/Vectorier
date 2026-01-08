using UnityEngine;

using System.Xml;

using static Vectorier.Core.Game;

// -=-=-=- //

namespace Vectorier.Core.Components {
	public static class Trapezoid {
		public static void Convert(
			GameObject go,

			XmlNode node,
			XmlElement parentElement = null,

			int floatPrecision = -1
		) {
			string objRegex = Helpers.Get.Name(go);

			// Skip invalid types
			if (objRegex != "trapezoid_type1" && objRegex != "trapezoid_type2") {
				return;
			}

			var spriteRenderer = go.GetComponent<SpriteRenderer>();
			if (spriteRenderer == null || !spriteRenderer.enabled || spriteRenderer.sprite == null) {
				return;
			}

			// Get transform data
			Vector3 scale = go.transform.lossyScale;
			Vector3 position = go.transform.position;

			if (scale.x != scale.y) {
				Debug.LogWarning(
					$@"Trapezoid-tagged GameObject named ""{go.name}"" doesn't have proportional scaling",
					go
				);
			}

			// Element
			XmlDocument xml = node.OwnerDocument;
			XmlElement mainElement = xml.CreateElement("Trapezoid");

			TrapezoidHelpers.ApplyDimensions(go, mainElement, objRegex.ToLower().EndsWith("trapezoid_type1"));

			// Mode
			Helpers.ApplyWriteMode(go, mainElement);

			// Parent
			XmlNode targetParent = parentElement ?? node.FirstChild;
			targetParent.AppendChild(mainElement);
		}

		// Helpers
		public static class TrapezoidHelpers {
			public static void ApplyDimensions(
				GameObject go,

				XmlElement element,
		
				bool origType,

				int floatPrecision = -1
			) {
				var spriteRenderer = go.GetComponent<SpriteRenderer>();
				if (spriteRenderer == null || !spriteRenderer.enabled || spriteRenderer.sprite == null) {
					return;
				}

				Bounds bounds = spriteRenderer.sprite.bounds;

				float width = bounds.size.x * Vectorier.Core.Game.UnitScale;
				float height = bounds.size.y * Vectorier.Core.Game.UnitScale;

				float adjustedWidth = width * go.transform.lossyScale.x;
				float adjustedHeight = height * go.transform.lossyScale.y;

				string finalX;
				string finalY;

				bool isFlipped = spriteRenderer.flipX;
				bool actualType1 = origType ^ isFlipped;

				float x, y;

				if (isFlipped) {
					x = (go.transform.position.x * Vectorier.Core.Game.UnitScale) - adjustedWidth;
					y = (-go.transform.position.y * Vectorier.Core.Game.UnitScale) + (actualType1 ? adjustedHeight : -adjustedHeight);

					finalX = Helpers.ToString(x, floatPrecision);
					finalY = Helpers.ToString(y, floatPrecision);
				} else {
					x = go.transform.position.x;
					y = -go.transform.position.y;

					finalX = Helpers.Multiply(x, floatPrecision);
					finalY = Helpers.Multiply(y, floatPrecision);
				}

				if (Mathf.Abs(x) > float.Epsilon) { element.SetAttribute("X", finalX); }
				if (Mathf.Abs(y) > float.Epsilon) { element.SetAttribute("Y", finalY); }

				element.SetAttribute("Width", Helpers.ToString(adjustedWidth, floatPrecision));

				if (actualType1) {
					element.SetAttribute("Height", "1");
					element.SetAttribute("Height1", Helpers.ToString(adjustedHeight + 1, floatPrecision));
					element.SetAttribute("Type", "1");
				} else {
					element.SetAttribute("Height", Helpers.ToString(adjustedHeight + 1, floatPrecision));
					element.SetAttribute("Height1", "1");
					element.SetAttribute("Type", "2");
				}
			}
		}
	}
}