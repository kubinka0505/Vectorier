using UnityEditor;
using UnityEngine;

using System.IO;
using System.Xml;
using System.Collections.Generic;

// -=-=-=- //

namespace Vectorier.Core.Components {
	public static class TopImage {
		public static void Convert(
			GameObject go,

			XmlNode node,
			XmlElement parentElement = null,

			int floatPrecision = -1,
			bool legacy = false,

			Dictionary<string, string> classNameToPath = null
		) {
			if (!Helpers.WriteMode.CanWrite(go, legacy)) {
				return;
			}

			string objRegex = Helpers.Get.Name(go);

			if (objRegex == "Camera") {
				return;
			}

			// Validation
			var spriteRenderer = go.GetComponent<SpriteRenderer>();
			if (spriteRenderer == null || !spriteRenderer.enabled || spriteRenderer.sprite == null) {
				return;
			}

			// Set name
			string objName = AssetDatabase.GetAssetPath(spriteRenderer.sprite);

			objName = Vectorier.Utils.Paths.From(
				Path.Combine(Application.dataPath, "Resources", "Textures")
			).RelativeTo(
				objName
			);

			objName = Path.ChangeExtension(objName, null);

			objName = objName.Replace(
				Path.DirectorySeparatorChar,
				Path.AltDirectorySeparatorChar
			);

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
			Helpers.WriteMode.Apply(go, mainElement);

			// Parent
			XmlNode targetParent = parentElement ?? node.FirstChild;
			targetParent.AppendChild(mainElement);
		}
	}
}