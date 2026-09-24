using UnityEngine;
using System.Xml;

#nullable enable

// -=-=-=- //

namespace Vectorier.Core.Components {
	public static class PlatformComponent {
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

			var spriteRenderer = go.GetComponent<SpriteRenderer>();

			if (
				spriteRenderer == null ||
				!spriteRenderer.enabled ||
				spriteRenderer.sprite == null
			) {
				return;
			}

			var scale = go.transform.lossyScale;

			if (scale.x == 0 && scale.y == 0) {
				return;
			}

			// Extract normalized quad bounds (handles origin offset, rotation snaps, scale)
			var norm = Vectorier.Core.Helpers.BoundNormalizer.ExtractNormalized(go, floatPrecision);

			if (!norm.IsValid) {
				return;
			}

			XmlDocument xml = node.OwnerDocument!;
			XmlElement mainElement = xml.CreateElement("Platform");

			float x = norm.X * 100f;
			float y = norm.Y * 100f;
			float w = norm.Width * 10000f;
			float h = norm.Height * 10000f;

			if (x != 0) {
				mainElement.SetAttribute("X", Helpers.Format.Multiply(x, floatPrecision));
			}

			if (y != 0) {
				mainElement.SetAttribute("Y", Helpers.Format.Multiply(-y, floatPrecision));
			}

			if (w != 0) {
				mainElement.SetAttribute("Width", Helpers.Format.ToString(w, floatPrecision));
			}

			if (h != 0) {
				mainElement.SetAttribute("Height", Helpers.Format.ToString(h, floatPrecision));
			}

			Helpers.WriteMode.Apply(go, mainElement);

			XmlNode targetParent = parentElement ?? node.FirstChild;

			targetParent?.AppendChild(mainElement);
		}
	}
}