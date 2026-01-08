using UnityEngine;

using System.Xml;

// -=-=-=- //

namespace Vectorier.Core.Components {
	public static class Camera {
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

			var customZoom = go.GetComponent<CustomZoom>();
			if (customZoom == null && !customZoom.enabled) {
				Debug.LogWarning("SpriteRenderer or CustomZoom component is missing on " + go.name, go);
				return;
			}

			var spriteRenderer = go.GetComponent<SpriteRenderer>();
			if (spriteRenderer == null || !spriteRenderer.enabled || spriteRenderer.sprite == null) {
				return;
			}

			string objName = (spriteRenderer != null && spriteRenderer.enabled && spriteRenderer.sprite != null)
				? spriteRenderer.sprite.name
				: objRegex;

			// Element
			XmlDocument xml = node.OwnerDocument;
			XmlElement mainElement = xml.CreateElement("Trigger");

			// Obsolete...
			if (!objRegex.StartsWith(">") && objRegex != objName && !string.IsNullOrEmpty(go.name)) {
				mainElement.SetAttribute("Name", objRegex);
			}

			var (x, y) = Helpers.Get.Position(go);
			if (x != 0) { mainElement.SetAttribute("X", Helpers.Multiply(x)); }
			if (y != 0) { mainElement.SetAttribute("Y", Helpers.Multiply(-y)); }

			var (w, h) = Helpers.Get.Size(go);
			if (w != 0) { mainElement.SetAttribute("Width", Helpers.ToString(w)); }
			if (h != 0) { mainElement.SetAttribute("Height", Helpers.ToString(h)); }

			XmlElement contentElement = xml.CreateElement("Content");
			XmlElement initElement = xml.CreateElement("Init");

			// Trigger
			string[] variableNames = { "$Active", "$Node", "Zoom", "$AI", "Flag1" };
			string[] variableValues = { "1", "COM", customZoom.ZoomAmount.ToString(), "0", "0" };

			for (int i = 0; i < variableNames.Length; i++) {
				XmlElement setVariableElement = xml.CreateElement("SetVariable");
				setVariableElement.SetAttribute("Name", variableNames[i]);
				setVariableElement.SetAttribute("Value", variableValues[i]);
				initElement.AppendChild(setVariableElement);
			}

			XmlElement templateElement = xml.CreateElement("Template");
			templateElement.SetAttribute("Name", "CameraZoom");

			contentElement.AppendChild(initElement);
			contentElement.AppendChild(templateElement);
			mainElement.AppendChild(contentElement);

			// Parent
			XmlNode targetParent = parentElement ?? node.FirstChild;

			// Mode
			Helpers.ApplyWriteMode(go, mainElement);

			targetParent.AppendChild(mainElement);
		}
	}
}