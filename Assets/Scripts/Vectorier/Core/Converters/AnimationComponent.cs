using UnityEngine;
using System.Xml;

#nullable enable
#pragma warning disable CS8618

// -=-=-=- //

namespace Vectorier.Core.Components {
	public static class AnimationComponent {
		public static void Convert(
			GameObject go,
			XmlNode node,
			XmlElement? parentElement = null,
			int floatPrecision = -1,
			bool legacy = false
		) {
			if (!AnimationRules.CanExport(go, legacy)) {
				return;
			}

			var data = AnimationExtractor.Extract(go, floatPrecision);

			var xml = AnimationXmlBuilder.Build(
				node.OwnerDocument!,
				go,
				data,
				floatPrecision
			);

			var parent = parentElement ?? node.FirstChild;
			if (parent == null) {
				return;
			}

			parent.AppendChild(xml);
		}
	}

	internal class AnimationData {
		public string Name;

		public Sprite Sprite;
		public Transform Transform;
		public AnimationProperties Properties;

		public (float x, float y) Position;

		public float Width;
		public float Height;

		public int Precision;
	}

	internal static class AnimationRules {
		public static bool CanExport(GameObject go, bool legacy) {
			if (!Helpers.WriteMode.CanWrite(go, legacy)) {
				return false;
			}

			string name = Helpers.Get.Name(go);

			var spriteRenderer = go.GetComponent<SpriteRenderer>();
			var props = go.GetComponent<AnimationProperties>();

			if (props == null || !props.enabled) {
				Debug.LogWarning(
					$@"Animation properties component is missing on ""{name}""",
					go
				);
				return false;
			}

			if (spriteRenderer == null || !spriteRenderer.enabled || spriteRenderer.sprite == null) {
				Debug.LogWarning(
					$@"Animation component is missing on ""{name}""",
					go
				);

				return false;
			}

			return true;
		}
	}

	internal static class AnimationExtractor {
		public static AnimationData Extract(GameObject go, int precision) {
			var spriteRenderer = go.GetComponent<SpriteRenderer>();
			var props = go.GetComponent<AnimationProperties>();

			return new AnimationData {
				Name = Helpers.Get.Name(go),

				Sprite = spriteRenderer.sprite,
				Transform = go.transform,
				Properties = props,

				Position = Helpers.Get.Position(go),

				Width = props.Width,
				Height = props.Height,

				Precision = precision
			};
		}
	}

	internal static class AnimationXmlBuilder {
		public static XmlElement Build(
			XmlDocument xml,
			GameObject go,
			AnimationData data,
			int precision
		) {
			XmlElement el = xml.CreateElement("Animation");

			// Position
			var (x, y) = data.Position;

			if (x != 0) {
				el.SetAttribute("X", Helpers.Format.Multiply(x, precision));
			}

			if (y != 0) {
				el.SetAttribute("Y", Helpers.Format.Multiply(y, precision));
			}

			// Sprite name
			el.SetAttribute("ClassName", data.Sprite.name);

			// Size
			if (data.Width != 0) {
				el.SetAttribute("Width", Helpers.Format.ToString(data.Width));
			}

			if (data.Height != 0) {
				el.SetAttribute("Height", Helpers.Format.ToString(data.Height));
			}

			// Scale
			float sx = data.Transform.lossyScale.x;
			float sy = data.Transform.lossyScale.y;

			if (sx != 0) {
				el.SetAttribute("ScaleX", Helpers.Format.ToString(sx));
			}

			if (sy != 0) {
				el.SetAttribute("ScaleY", Helpers.Format.ToString(sy));
			}

			// Type
			int type = data.Properties.PlayOnce ? 0 : 1;
			if (type != 0) {
				el.SetAttribute("Type", Helpers.Format.ToString(type));
			}

			// Direction
			el.SetAttribute(
				"Direction",
				string.Join(
					Vectorier.Core.Game.Properties.AttributeSeparator,
					Helpers.Format.ToString(data.Properties.DirectionX),
					Helpers.Format.ToString(data.Properties.DirectionY)
				)
			);

			// Acceleration
			el.SetAttribute(
				"Acceleration",
				string.Join(
					Vectorier.Core.Game.Properties.AttributeSeparator,
					Helpers.Format.ToString(data.Properties.AccelerationX),
					Helpers.Format.ToString(data.Properties.AccelerationY)
				)
			);

			// Time
			float time = data.Properties.Time;

			if (time == -1f) {
				time = (float)ushort.MaxValue;
			}

			if (time != 0f) {
				el.SetAttribute("Time", Helpers.Format.ToString(time));
			}

			Helpers.WriteMode.Apply(go, el);

			return el;
		}
	}
}