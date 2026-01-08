using UnityEngine;

using System.Xml;

using Vectorier.Core.Components;

// -=-=-=- //

namespace Vectorier.Core.Components {
	public static class Animation_ {
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

			var animationProperties = go.GetComponent<AnimationProperties>();
			if (animationProperties == null || !animationProperties.enabled) {
				Debug.LogWarning($@"AnimationProperties component is missing on ""{go.name}""", go);
				return;
			}

			// Element
			XmlDocument xml = node.OwnerDocument;
			XmlElement mainElement = xml.CreateElement("Animation");

			// Position
			var (x, y) = Helpers.Get.Position(go);
			if (x != 0) { mainElement.SetAttribute("X", Helpers.Multiply(x, floatPrecision)); }
			if (y != 0) { mainElement.SetAttribute("Y", Helpers.Multiply(-y, floatPrecision)); }

			// Attributes
			mainElement.SetAttribute("ClassName", objRegex);

			mainElement.SetAttribute("Width", animationProperties.Width);
			mainElement.SetAttribute("Height", animationProperties.Height);
			mainElement.SetAttribute("ScaleX", animationProperties.ScaleX);
			mainElement.SetAttribute("ScaleY", animationProperties.ScaleY);

			mainElement.SetAttribute("Type", animationProperties.Type);

			if (!string.IsNullOrEmpty(animationProperties.Direction)) {
				mainElement.SetAttribute("Direction", animationProperties.Direction);
			}

			if (!string.IsNullOrEmpty(animationProperties.Acceleration)) {
				mainElement.SetAttribute("Acceleration", animationProperties.Acceleration);
			}

			if (!string.IsNullOrEmpty(animationProperties.Time)) {
				mainElement.SetAttribute("Time", animationProperties.Time);
			}

			// Mode
			Helpers.ApplyWriteMode(go, mainElement);

			// Parent
			XmlNode targetParent = parentElement ?? node.FirstChild;
			targetParent.AppendChild(mainElement);
		}
	}
}