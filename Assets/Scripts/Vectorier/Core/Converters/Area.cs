using UnityEngine;

using System.Xml;

// -=-=-=- //

namespace Vectorier.Core.Components {
	public static class Area {
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

			XmlDocument xml = node.OwnerDocument;
			XmlElement mainElement = xml.CreateElement("Area");

			mainElement.SetAttribute("Name", objRegex);

			var (x, y) = Helpers.Get.Position(go);
			if (x != 0) { mainElement.SetAttribute("X", Helpers.Multiply(x, floatPrecision)); }
			if (y != 0) { mainElement.SetAttribute("Y", Helpers.Multiply(-y, floatPrecision)); }

			var (w, h) = Helpers.Get.Size(go);
			mainElement.SetAttribute("Width", Helpers.ToString(w));
			mainElement.SetAttribute("Height", Helpers.ToString(h));

			// Properties
			var areaProperties = go.GetComponent<AreaProperties>();
			if (areaProperties == null || !areaProperties.enabled) {
				switch (objRegex) {
					case "TriggerCatch":
					case "TriggerCatchFront":
						mainElement.SetAttribute("Type", "Catch");
						mainElement.SetAttribute("Distance", Helpers.ToString(AreaHelpers.ArrestDistance));
						break;
					case "TriggerCatchFast":
						mainElement.SetAttribute("Type", "Catch");
						mainElement.SetAttribute("Distance", "0");
						break;
					default:
						mainElement.SetAttribute("Type", "Animation");
						break;
				}
			} else {
				switch (areaProperties.Type) {
					case AreaProperties.EnumType.Help:
						mainElement.SetAttribute("Type", "Help");
						mainElement.SetAttribute("Key", areaProperties.Key.ToString());
						mainElement.SetAttribute("Description", areaProperties.Description);
						break;
					case AreaProperties.EnumType.Arrest:
						mainElement.SetAttribute("Type", "Arrest");
						mainElement.SetAttribute("Distance", areaProperties.Distance.ToString());
						break;
					case AreaProperties.EnumType.Animation:
						mainElement.SetAttribute("Type", "Animation");
						break;
					case AreaProperties.EnumType.None:
					default:
						break;
				}
			}

			// Mode
			Helpers.ApplyWriteMode(go, mainElement);

			// Parent
			XmlNode targetParent = parentElement ?? node.FirstChild;
			targetParent.AppendChild(mainElement);
		}

		public static class AreaHelpers {
			public static readonly float ArrestDistance = 300f;
		}
	}
}