using UnityEngine;

using System.Xml;

#nullable enable

// -=-=-=- //

namespace Vectorier.Core.Components {
	public static class AreaComponent {
		internal struct AreaData {
			public string Name;
			public float X;
			public float Y;
			public float Width;
			public float Height;
			public int Precision;
		}

		internal enum AreaType {
			Catch,
			CatchFast,
			Help,
			Arrest,
			Animation
		}

		internal static class AreaRules {
			public static AreaType ResolveType(
				GameObject go,
				AreaData data
			) {
				var props = go.GetComponent<AreaProperties>();

				if (props == null || !props.enabled) {
					switch (data.Name) {
						case "TriggerCatch":
						case "TriggerCatchFront":
							return AreaType.Catch;
						case "TriggerCatchFast":
							return AreaType.CatchFast;
						default:
							return AreaType.Animation;
					}
				}

				return props.Type switch {
					AreaProperties.EnumType.Help => AreaType.Help,
					AreaProperties.EnumType.Arrest => AreaType.Arrest,
					AreaProperties.EnumType.Animation => AreaType.Animation,
					_ => AreaType.Animation
				};
			}
		}

		internal static class AreaExtractor {
			public static AreaData Extract(GameObject go, int precision) {
				// Pass floatPrecision to ensure PSFix rounds before formatting
				var norm = Vectorier.Core.Helpers.BoundNormalizer.ExtractNormalized(go, precision);

				return new AreaData {
					Name = Helpers.Get.Name(go),
					X = norm.X * 100,
					Y = norm.Y * 100,
					Width = norm.Width * 10000,
					Height = norm.Height * 10000,
					Precision = precision
				};
			}
		}

		internal static class AreaXmlBuilder {
			public static XmlElement Build(
				XmlDocument xml,

				GameObject go,

				AreaData data,
				AreaType type,

				int precision
			) {
				var element = xml.CreateElement("Area");

				element.SetAttribute("Name", data.Name);

				if (data.X != 0) {
					element.SetAttribute("X", Helpers.Format.Multiply(data.X, precision));
				}

				if (data.Y != 0) {
					element.SetAttribute("Y", Helpers.Format.Multiply(-data.Y, precision));
				}

				if (data.Width != 0) {
					element.SetAttribute("Width", Helpers.Format.ToString(data.Width, precision));
				}

				if (data.Height != 0) {
					element.SetAttribute("Height", Helpers.Format.ToString(data.Height, precision));
				}

				ApplyType(element, go, type);

				Helpers.WriteMode.Apply(go, element);

				return element;
			}

			private static void ApplyType(
				XmlElement el,
				GameObject go,
				AreaType type
			) {
				var props = go.GetComponent<AreaProperties>();

				switch (type) {
					case AreaType.Catch:
						el.SetAttribute("Type", "Catch");
						el.SetAttribute("Distance", Helpers.Format.ToString(300f));
						break;

					case AreaType.CatchFast:
						el.SetAttribute("Type", "Catch");
						el.SetAttribute("Distance", "0");
						break;

					case AreaType.Help:
						el.SetAttribute("Type", "Help");
						el.SetAttribute("Key", props!.Key.ToString());
						el.SetAttribute("Description", props.Description);
						break;

					case AreaType.Arrest:
						el.SetAttribute("Type", "Arrest");
						el.SetAttribute("Distance", props!.Distance.ToString());
						break;

					case AreaType.Animation:
					default:
						el.SetAttribute("Type", "Animation");
						break;
				}
			}
		}

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

			var data = AreaExtractor.Extract(go, floatPrecision);
			var type = AreaRules.ResolveType(go, data);

			var xml = AreaXmlBuilder.Build(node.OwnerDocument!, go, data, type, floatPrecision);

			var parent = parentElement ?? node.FirstChild;
			if (parent == null) {
				return;
			}

			parent.AppendChild(xml);
		}
	}
}