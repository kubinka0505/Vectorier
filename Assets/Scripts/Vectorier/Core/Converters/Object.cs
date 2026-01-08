using UnityEngine;

using System.Xml;

// -=-=-=- //

namespace Vectorier.Core.Components {
	public static class Object {
		public static void Convert(
			GameObject go,

			XmlNode node,
			XmlElement parentElement = null,

			int floatPrecision = -1
		) {
			string objRegex = Helpers.Get.Name(go);

			bool isCamera = objRegex == "Camera";

			// Element
			XmlDocument xml = node.OwnerDocument;
			XmlElement mainElement = xml.CreateElement(isCamera ? "Camera" : "Object");

			if (!isCamera) {
				mainElement.SetAttribute("Name", objRegex);
			}

			// Position
			var (x, y) = Helpers.Get.Position(go);
			if (x != 0) { mainElement.SetAttribute("X", Helpers.Multiply(x, floatPrecision)); }
			if (y != 0) { mainElement.SetAttribute("Y", Helpers.Multiply(-y, floatPrecision)); }

			// scale attempt...
			/*
			var lsX = go.transform.lossyScale.x;
			var lsY = go.transform.lossyScale.y;

			bool hasForbiddenComponent =
				go.GetComponent<SpriteRenderer>() != null ||
				go.GetComponent<TriggerSettings>() != null;

			if (!hasForbiddenComponent) {
				if (lsX != 1) { mainElement.SetAttribute("ScaleX", Helpers.ToString(lsX)); }
				if (lsY != 1) { mainElement.SetAttribute("ScaleY", Helpers.ToString(lsY)); }
			}
			*/

			// Properties
			XmlElement propertiesElement = xml.CreateElement("Properties");

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