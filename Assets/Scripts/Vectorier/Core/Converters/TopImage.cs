using UnityEngine;

using System.Xml;

// -=-=-=- //

namespace Vectorier.Core.Components {
	public static class TopImage {
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

			// Validation
			var spriteRenderer = go.GetComponent<SpriteRenderer>();

			if (spriteRenderer == null || !spriteRenderer.enabled || spriteRenderer.sprite == null) {
				return;
			}

			string objName = (spriteRenderer != null && spriteRenderer.enabled && spriteRenderer.sprite != null)
				? spriteRenderer.sprite.name
				: objRegex;

			// Element
			XmlDocument xml = node.OwnerDocument;
			XmlElement mainElement = xml.CreateElement("Image");

			mainElement.SetAttribute("ClassName", objName);

			// Properties
			XmlElement propertiesElement = xml.CreateElement("Properties");

			ImageHelpers.ApplyStaticRotation(go, mainElement, propertiesElement, xml);
			ImageHelpers.ApplyStaticColor(go, mainElement, propertiesElement, xml);
			ImageHelpers.ApplyDynamicColor(go, propertiesElement, xml);
			ImageHelpers.ApplyDynamicRotate(go, propertiesElement, xml);
			ImageHelpers.ApplyDynamicSize(go, propertiesElement, xml);

			if (propertiesElement.HasChildNodes) {
				mainElement.AppendChild(propertiesElement);
			}

			// Mode
			Helpers.ApplyWriteMode(go, mainElement);

			// Parent
			XmlNode targetParent = parentElement ?? node.FirstChild;
			targetParent.AppendChild(mainElement);
		}
	}
}