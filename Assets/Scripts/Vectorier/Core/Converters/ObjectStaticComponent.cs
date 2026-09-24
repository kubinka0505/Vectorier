using UnityEngine;

using System.Xml;

#nullable enable

// -=-=-=- //

namespace Vectorier.Core.Components{
	public static class ObjectStaticComponent {
		public static void Convert(
			GameObject go,

			XmlNode node,
			XmlElement? parentElement = null,

			int floatPrecision = -1,
			bool legacy = false
		) {
			if (!Helpers.WriteMode.CanWrite(go, legacy)) {
				return;
			}

			string name = Helpers.Get.Name(go);
			bool isCamera = name == "Camera";

			var (x, y) = Helpers.Get.Position(go);

			if (isCamera && (x == 0 && y == 0)) {
				return;
			}
	
			XmlDocument xml = node.OwnerDocument!;
			XmlElement mainElement = xml.CreateElement(isCamera ? "Camera" : "Object");

			mainElement.SetAttribute("Name", name);
	
			if (x != 0) {
				mainElement.SetAttribute("X", Helpers.Format.Multiply(x, floatPrecision));
			}

			if (y != 0) {
				mainElement.SetAttribute("Y", Helpers.Format.Multiply(y, floatPrecision));
			}

			XmlElement propertiesElement = xml.CreateElement("Properties");

			Helpers.WriteMode.Apply(go, mainElement);

			if (propertiesElement.HasChildNodes) {
				mainElement.AppendChild(propertiesElement);
			}

			XmlNode targetParent = parentElement ?? node.FirstChild;
			targetParent?.AppendChild(mainElement);
		}
	}
}