using UnityEngine;

using System.Xml;
using System.Globalization;

// -=-=-=- //

namespace Vectorier.Core.Components {
	public static class Item {
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

			var itemProperties = go.GetComponent<ItemProperties>();
			if (itemProperties == null || !itemProperties.enabled) {
				Debug.LogError($@"No ItemProperties on GameObject ""{objRegex}""", go);
				return;
			}

			// Element
			XmlDocument xml = node.OwnerDocument;
			XmlElement mainElement = xml.CreateElement("Item");

			// Position
			var (x, y) = Helpers.Get.Position(go);
			if (x != 0) { mainElement.SetAttribute("X", Helpers.Multiply(x, floatPrecision)); }
			if (y != 0) { mainElement.SetAttribute("Y", Helpers.Multiply(-y, floatPrecision)); }

			// Variables
			int _ItemType = itemProperties.Type.ToString().ToLower().StartsWith("coin") ? 1 : 0;
			int _ItemScore = itemProperties.Score;
			int _ItemGroup = itemProperties.GroupID;

			// Type
			if (_ItemType == 0) {
				_ItemScore = 100;
			}

			// Group
			if (_ItemGroup == -1 && _ItemType == 1) {
				_ItemGroup = UnityEngine.Random.Range((int)1E8, (int)1E9 - 1);
			}

			if (_ItemType != 0) {
				mainElement.SetAttribute("Type", Vectorier.Core.Helpers.ToString(_ItemType));
			}

			mainElement.SetAttribute("Score", Vectorier.Core.Helpers.ToString(_ItemScore));
			mainElement.SetAttribute("Radius", Vectorier.Core.Helpers.ToString(itemProperties.Radius));

			if (_ItemType != 0) {
				mainElement.SetAttribute("GroupId", _ItemGroup.ToString());
			};

			// Mode
			Helpers.ApplyWriteMode(go, mainElement);

			// Parent
			XmlNode targetParent = parentElement ?? node.FirstChild;
			targetParent.AppendChild(mainElement);
		}
	}
}