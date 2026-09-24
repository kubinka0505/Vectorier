using UnityEngine;

using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Text;
using System.Globalization;

using Debug = Logger.Debug;

#pragma warning disable CS0162

// -=-=-=- //

namespace Vectorier.Core.XML.Utils {
	public static class General {
		public class StandardStringWriter : StringWriter {
			public override Encoding Encoding => Encoding.UTF8;
		}

		public static string[] Attributes = {
			// basic
			"Name",
			"ClassName",
			"Label",
			"Labels",

			// triggers
			"Template",

			// advanced triggers
			"DefaultValue",
			"Value",
			"Value1",
			"Value2",
			"Than",
			"Not",

			// position
			"X",
			"Y",
			"InX",
			"InY",
			"DeltaX",
			"DeltaY",

			// dimensions
			"Width",
			"Width1",
			"Height",
			"Height1",
			"NativeX",
			"NativeY",

			// object related
			"Factor",

			// spawn related
			"Animation",

			// transformation
			"Number",
			"FramesToMove",

			// models etc
			"Type",
			"AI",
			"Color",
			"Skins",
			"Impuls",
			"LifeTime",
			"GeneratesBuildings",

			// item related
			"Score",
			"Radius",
			"GroupId",

			// EndGame
			"Result",

			// model & dynamic trigger related
			"Time",

			// dynamic color
			"ColorStart",

			// dynamic rotation
			"Angle",
			"Anchor",

			"Stop",
			"Frames",

			"Key",
			"Model",
			"Position",

			// transforms
			"Order",
			"Set",

			// again models
			"BirthSpawn",
			"AllowedSpawns",
			"Respawns",

			"Trick",
			"Item",

			"Victory",
			"Lose",

			"Arrests",
			"Murdrers",
			"ForceBlasts",

			"Stocks",

			// meta
			"FileName",

			"Choice",
			"Variant"
		};

		// -=-=-=- //

		private static bool HasChild(XmlElement root, string name) {
			foreach (XmlNode child in root.ChildNodes) {
				if (child.NodeType == XmlNodeType.Element &&
					child.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
				) {
					return true;
				}
			}

			return false;
		}

		// -=-=-=- //

		public static XmlElement GetOrCreateElement(
			string name,

			XmlElement parent,
			XmlDocument xml
		) {
			XmlElement element = parent[name];

			if (element == null) {
				element = xml.CreateElement(name);
				parent.AppendChild(element);
			}

			return element;
		}

		public static T GetNumericAttr<T>(
			XmlElement elem,
			string attrName,
			T defaultValue = default
		) where T : struct, IConvertible {
			if (elem == null || !elem.HasAttribute(attrName)) {
				return defaultValue;
			}

			CultureInfo culture = CultureInfo.InvariantCulture;
			string raw = elem.GetAttribute(attrName);

			try {
				if (typeof(T) == typeof(int)) {
					if (int.TryParse(raw, NumberStyles.Integer, culture, out int val)) {
						return (T)(object)val;
					}
				} else if (typeof(T) == typeof(float)) {
					if (float.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, culture, out float val)) {
						return (T)(object)val;
					}
				} else if (typeof(T) == typeof(double)) {
					if (double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, culture, out double val)) {
						return (T)(object)val;
					}
				}
			} catch {
				// ignore
			}

			return defaultValue;
		}
	}
}