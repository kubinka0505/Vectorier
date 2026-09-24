using UnityEngine;
using System.Xml;

#nullable enable

// -=-=-=- //

namespace Vectorier.Core.Components {
	public static class ItemComponent {
		public static void Convert(
			GameObject go,
			XmlNode node,
			XmlElement? parentElement = null,
			int floatPrecision = -1,
			bool legacy = false
		) {
			if (!ItemRules.CanExport(go, legacy)) {
				return;
			}

			var data = ItemExtractor.Extract(go);
			var model = ItemRules.Resolve(data);
			var xml = ItemXmlBuilder.Build(node.OwnerDocument!, model, floatPrecision);

			var parent = parentElement ?? node.FirstChild;
			if (parent == null)
				return;

			parent.AppendChild(xml);
		}
	}

	internal static class ItemRules {
		public static bool CanExport(GameObject go, bool legacy) {
			if (!Helpers.WriteMode.CanWrite(go, legacy)) {
				return false;
			}

			return true;
		}

		public static ItemModel Resolve(ItemData d) {
			int type = (d.Type == ItemProperties.ItemTypes.Coin) ? 0 : 1;

			int score = d.Score;
			int group = d.GroupId;

			if (type == 0) {
				score = 100;
			}

			if (group == -1 && type == 1) {
				group = Random.Range(100_000_000, 999_999_999);
			}

			return new ItemModel(
				d.X,
				d.Y,
				type,
				score,
				group,
				d.Radius
			);
		}
	}

	internal static class ItemExtractor {
		public static ItemData Extract(GameObject go) {
			var props = go.GetComponent<ItemProperties>();

			if (props == null || !props.enabled) {
				throw new System.Exception($"Missing item properties component on {go.name}");
			}

			var (x, y) = Helpers.Get.Position(go);

			return new ItemData {
				Name = Helpers.Get.Name(go),

				X = x,
				Y = y,

				Type = props.Type,

				Score = props.Score,
				GroupId = props.GroupID,
				Radius = props.Radius
			};
		}
	}

	internal static class ItemXmlBuilder {
		public static XmlElement Build(
			XmlDocument xml,
			ItemModel m,
			int precision
		) {
			var el = xml.CreateElement("Item");

			if (m.X != 0) {
				el.SetAttribute("X", Helpers.Format.Multiply(m.X, precision));
			}

			if (m.Y != 0) {
				el.SetAttribute("Y", Helpers.Format.Multiply(-m.Y, precision));
			}

			if (m.Type != 0)
				el.SetAttribute("Type", Helpers.Format.ToString(m.Type));

			el.SetAttribute("Score", Helpers.Format.ToString(m.Score));
			el.SetAttribute("Radius", Helpers.Format.ToString(m.Radius));

			if (m.Type != 0) {
				el.SetAttribute("GroupId", m.GroupId.ToString());
			}

			return el;
		}
	}

	internal class ItemData {
		public string? Name;

		public float X;
		public float Y;

		public ItemProperties.ItemTypes Type;

		public int Score;
		public int GroupId;
		public float Radius;
	}

	internal readonly struct ItemModel {
		public readonly float X;
		public readonly float Y;

		public readonly int Type;
		public readonly int Score;
		public readonly int GroupId;
		public readonly float Radius;

		public ItemModel(
			float x,
			float y,
			int type,
			int score,
			int groupId,
			float radius
		) {
			X = x;
			Y = y;
			Type = type;
			Score = score;
			GroupId = groupId;
			Radius = radius;
		}
	}
}