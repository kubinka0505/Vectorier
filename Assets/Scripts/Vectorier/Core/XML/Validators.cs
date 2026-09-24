using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

#nullable enable

// -=-=-=- //

namespace Vectorier.Core.XML.Utils {
	public static class Validators {
		private static XmlDocument? ToXmlDocument(object? input) {
			if (input == null) {
				return null;
			}

			try {
				return input switch {
					XmlDocument xmlDoc => xmlDoc,
					XmlNode xmlNode => ParseXmlString(xmlNode.OuterXml),
					XDocument xDoc => ParseXmlString(xDoc.ToString()),
					string strInput when File.Exists(strInput) => LoadXmlFromFile(strInput),
					string strInput when !string.IsNullOrWhiteSpace(strInput) => ParseXmlString(strInput),
					_ => null
				};
			} catch {
				return null;
			}
		}

		private static XmlDocument ParseXmlString(string xml) {
			var doc = new XmlDocument();
			doc.LoadXml(xml);

			return doc;
		}

		private static XmlDocument LoadXmlFromFile(string filePath) {
			var doc = new XmlDocument();
			doc.Load(filePath);

			return doc;
		}

		/// <summary>
		/// Checks if root is <Track> OR contains a child <Track> node.
		/// </summary>
		public static bool IsTrack(object? input) {
			var doc = ToXmlDocument(input);
			if (doc == null) {
				return false;
			}

			var root = doc.DocumentElement;
			if (root == null) {
				return false;
			}

			if (root.Name.Equals("track", StringComparison.OrdinalIgnoreCase)) {
				return true;
			}

			foreach (XmlNode child in root.ChildNodes) {
				if (child.NodeType == XmlNodeType.Element &&
					child.Name.Equals("track", StringComparison.OrdinalIgnoreCase)) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Has Root/Objects AND is NOT a Track.
		/// </summary>
		public static bool IsSet(object? input) {
			var doc = ToXmlDocument(input);
			if (doc == null) {
				return false;
			}

			// Must not be a track
			if (IsTrack(doc)) {
				return false;
			}

			var root = doc.DocumentElement;
			if (root == null) {
				return false;
			}

			// Check for Root/Objects
			foreach (XmlNode child in root.ChildNodes) {
				if (child.NodeType == XmlNodeType.Element &&
					child.Name.Equals("objects", StringComparison.OrdinalIgnoreCase)) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Has Root/Moves node.
		/// </summary>
		public static bool IsMoves(object? input) {
			var doc = ToXmlDocument(input);
			if (doc == null) {
				return false;
			}

			var root = doc.DocumentElement;
			if (root == null) {
				return false;
			}

			// Check for Root/Moves
			foreach (XmlNode child in root.ChildNodes) {
				if (child.NodeType == XmlNodeType.Element &&
					child.Name.Equals("moves", StringComparison.OrdinalIgnoreCase)) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Root node name is Template.
		/// </summary>
		public static bool IsTemplate(object? input) {
			var doc = ToXmlDocument(input);
			if (doc == null) {
				return false;
			}

			var root = doc.DocumentElement;
			return root != null && root.Name.Equals("template", StringComparison.OrdinalIgnoreCase);
		}
	}
}