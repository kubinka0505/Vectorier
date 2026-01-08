using UnityEngine;

using System.Xml;

// -=-=-=- //

namespace Vectorier.Core.Components {
	public static class Platform {
		public static void Convert(
			GameObject go,

			XmlNode node,			
			XmlElement parentElement = null,

			int floatPrecision = -1
		) {
			string objRegex = Helpers.Get.Name(go);

			if (objRegex == "Camera") {
				return;
			}

			var spriteRenderer = go.GetComponent<SpriteRenderer>();
			if (spriteRenderer == null || !spriteRenderer.enabled || spriteRenderer.sprite == null) {
				return;
			}

			if (go.transform.lossyScale.x == 0 && go.transform.lossyScale.y == 0) {
				return;
			}

			// Element
			XmlDocument xml = node.OwnerDocument;
			XmlElement mainElement = xml.CreateElement("Platform");

			// Position
			var (x, y) = Helpers.Get.Position(go);
			if (x != 0) { mainElement.SetAttribute("X", Helpers.Multiply(x, floatPrecision)); }
			if (y != 0) { mainElement.SetAttribute("Y", Helpers.Multiply(-y, floatPrecision)); }

			// Size
			var (w, h) = Helpers.Get.Size(go);
			mainElement.SetAttribute("Width", Helpers.ToString(w, floatPrecision));
			mainElement.SetAttribute("Height", Helpers.ToString(h, floatPrecision));

			// Mode
			Helpers.ApplyWriteMode(go, mainElement);

			// Parent
			XmlNode targetParent = parentElement ?? node.FirstChild;
			targetParent.AppendChild(mainElement);
		}
	}
}