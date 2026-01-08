using UnityEngine;

using System.Xml;
using System.Text.RegularExpressions;

// -=-=-=- //

namespace Vectorier.Core.Components {
	public static class Backdrop {
		public static void Convert(
			GameObject go,
			XmlNode node,

			float factorValue,
			bool correctFactorPosition = true,

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

			string objName = spriteRenderer.sprite.name;

			Vector3 scale = go.transform.lossyScale;
			Vector3 position = go.transform.position;

			if (correctFactorPosition) {
				position.x /= (1 / factorValue);
				position.y /= (1 / factorValue);
			}

			XmlDocument xml = node.OwnerDocument;
			XmlElement mainElement = xml.CreateElement("Image");
			mainElement.SetAttribute("ClassName", objName);

			// Position
			float imagePosX = position.x * Vectorier.Core.Game.UnitScale;
			float imagePosY = -position.y * Vectorier.Core.Game.UnitScale;

			mainElement.SetAttribute("X", Helpers.ToString(imagePosX, floatPrecision));
			mainElement.SetAttribute("Y", Helpers.ToString(imagePosY, floatPrecision));

			// Size
			Bounds bounds = spriteRenderer.sprite.bounds;
			float width = bounds.size.x * Vectorier.Core.Game.UnitScale;
			float height = bounds.size.y * Vectorier.Core.Game.UnitScale;

			mainElement.SetAttribute("Width", Helpers.ToString(width * scale.x));
			mainElement.SetAttribute("Height", Helpers.ToString(height * scale.y));

			if (width != width * scale.x) {
				mainElement.SetAttribute("NativeX", Helpers.ToString(width, floatPrecision));
			}
			if (height != height * scale.y) {
				mainElement.SetAttribute("NativeY", Helpers.ToString(height, floatPrecision));
			}

			// Properties
			XmlElement propertiesElement = xml.CreateElement("Properties");

			// todo: fix offset
			// ImageHelpers.ApplyStaticRotation(go, mainElement, propertiesElement, xml);

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