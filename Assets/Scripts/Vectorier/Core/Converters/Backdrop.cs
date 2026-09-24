using UnityEditor;
using UnityEngine;

using System.IO;
using System.Xml;
using System.Collections.Generic;

using static Vectorier.Core.Components.Image;

// -=-=-=- //

namespace Vectorier.Core.Components {
	public static class Backdrop {
		public static void Convert(
			GameObject go,
			XmlNode node,

			float factorValue,
			bool correctFactorPosition = true,

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

			// Transform
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
			float imagePosX = position.x * Vectorier.Core.Game.Properties.UnitScale;
			float imagePosY = -position.y * Vectorier.Core.Game.Properties.UnitScale;

			mainElement.SetAttribute("X", Helpers.Format.ToString(imagePosX, floatPrecision));
			mainElement.SetAttribute("Y", Helpers.Format.ToString(imagePosY, floatPrecision));

			// Size
			Bounds bounds = spriteRenderer.sprite.bounds;
			float width = bounds.size.x * Vectorier.Core.Game.Properties.UnitScale;
			float height = bounds.size.y * Vectorier.Core.Game.Properties.UnitScale;

			mainElement.SetAttribute("Width", Helpers.Format.ToString(width * scale.x));
			mainElement.SetAttribute("Height", Helpers.Format.ToString(height * scale.y));

			// Rotations unsupported for now, skipping
			/*
			if (width != width * scale.x) {
				mainElement.SetAttribute("NativeX", Helpers.Format.ToString(width, floatPrecision));
			}
			if (height != height * scale.y) {
				mainElement.SetAttribute("NativeY", Helpers.Format.ToString(height, floatPrecision));
			}
			*/

			// Properties
			XmlElement propertiesElement = xml.CreateElement("Properties");

			// todo: fix offset
			// ImageHelpers.ApplyStaticRotation(go, mainElement, propertiesElement, xml, leaveZeros: !legacy); // vision fix

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