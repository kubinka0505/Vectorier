using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// -=-=-=- //

namespace Vectorier.Core.XML.Utils {
	public static class Optimize {
		public static int DefaultMode = 2;

		public static string Pipeline(
			string fileInput,
			string fileOutput = null,

			string nodeName = "Root"
		) {
			if (string.IsNullOrWhiteSpace(fileInput)) {
				throw new ArgumentException("File path is null or empty.", nameof(fileInput));
			}

			if (fileOutput == null) {
				fileOutput = fileInput;
			}

			XDocument doc = XDocument.Load(fileInput, LoadOptions.PreserveWhitespace);

			// Remove whitespace-only text nodes
			foreach (var element in doc.Descendants().ToList()) {
				element.Nodes()
					.Where(n => n.NodeType == XmlNodeType.Text && string.IsNullOrWhiteSpace(((XText)n).Value))
					.Remove();
			}

			XElement node = doc.Root?.Element(nodeName);
			if (node != null) {
				RemoveEmptyFactorsFromXml(
					root: node,
					descendantsNames: new[] { "Object" },
					elementNames: new[] { "Content" }
				);

				var xmlDoc = new XmlDocument();
				xmlDoc.LoadXml(node.ToString());
				Objects(xmlDoc.DocumentElement);
			}

			return Write(doc, fileOutput);
		}

		public static string Write(object doc, string fileOutput, int? mode = null, bool simulate = false) {
			if (doc == null) {
				throw new ArgumentNullException(nameof(doc));
			}

			if (string.IsNullOrWhiteSpace(fileOutput)) {
				throw new ArgumentException("File output path is null or empty.", nameof(fileOutput));
			}

			if (mode == null) {
				mode = DefaultMode;
			}

			if (mode < 1 || mode > 3) {
				throw new ArgumentException("Mode must be 1, 2, or 3.", nameof(mode));
			}

			XDocument xdoc = doc as XDocument;
			if (xdoc == null) {
				if (doc is XmlNode xmlNode) {
					xdoc = XDocument.Parse(xmlNode.OuterXml);
				} else if (doc is XmlDocument xmlDoc) {
					xdoc = XDocument.Parse(xmlDoc.OuterXml);
				} else if (doc is string filePath) {
					xdoc = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);
				}
			}

			if (xdoc == null) {
				throw new ArgumentException("Doc must be XDocument, XmlNode, XmlDocument, or file path string.", nameof(doc));
			}

			string xmlContent = mode switch {
				1 => Operations.Linearize(xdoc),
				2 => Operations.Standard(xdoc),
				3 => Operations.PrettyPrint(xdoc),
				_ => ""
			};

			if (simulate) {
				return xmlContent;
			} else {
				File.WriteAllText(fileOutput, xmlContent, Encoding.UTF8);
				return fileOutput;	
			}
		}

		public static string Beautify(XmlNode node) {
			if (node == null) {
				throw new ArgumentNullException(nameof(node));
			}

			XmlDocument doc = new XmlDocument();
			doc.LoadXml(node.OuterXml);

			return Operations.Standard(XDocument.Parse(doc.OuterXml));
		}

		public static string Beautify(string filePath) {
			if (string.IsNullOrWhiteSpace(filePath)) {
				throw new ArgumentException("File path is null or empty.", nameof(filePath));
			}

			XDocument doc = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);
			return Operations.Standard(doc);
		}

		// Privates
		private static void RemoveEmptyFactorsFromXml(
			XElement root,
			string[] descendantsNames = null,
			string[] elementNames = null
		) {
			descendantsNames ??= Array.Empty<string>();
			elementNames ??= Array.Empty<string>();

			foreach (string descendant in descendantsNames) {
				foreach (string element in elementNames) {
					foreach (var obj in root.Descendants(descendant).ToList()) {
						var content = obj.Element(element);

						if (content != null && string.IsNullOrWhiteSpace(content.Value) && !content.HasElements) {
							obj.Remove();
						}
					}
				}
			}
		}

		public static void Attributes(
			XmlNode root,
			string[] attrs
		) {
			if (root == null) {
				return;
			}

			if (root.Attributes != null && root.Attributes.Count > 0) {
				var ordered = new List<XmlAttribute>();
				var remaining = new List<XmlAttribute>();

				foreach (XmlAttribute attr in root.Attributes) {
					int index = System.Array.IndexOf(attrs, attr.Name);
					if (index >= 0) {
						ordered.Add(attr);
					} else {
						remaining.Add(attr);
					}
				}

				ordered.Sort((a, b) => {
					int indexA = System.Array.IndexOf(attrs, a.Name);
					int indexB = System.Array.IndexOf(attrs, b.Name);
					return indexA.CompareTo(indexB);
				});

				ordered.AddRange(remaining);

				XmlAttribute[] original = new XmlAttribute[root.Attributes.Count];
				root.Attributes.CopyTo(original, 0);

				foreach (XmlAttribute attr in original) {
					root.Attributes.Remove(attr);
				}

				foreach (XmlAttribute attr in ordered) {
					root.Attributes.Append(attr);
				}
			}

			foreach (XmlNode child in root.ChildNodes) {
				Attributes(child, attrs);
			}
		}

		public static void Objects(XmlNode root, int floatPrecision = -1) {
			if (root == null) {
				return;
			}

			XmlDocument doc = root.OwnerDocument ?? (root as XmlDocument);
			if (doc == null) {
				return;
			}

			XmlNodeList objectNodes = root.SelectNodes(".//Object");
			if (objectNodes == null) {
				return;
			}

			const float EPS = 1e-4f;

			foreach (XmlElement obj in objectNodes.Cast<XmlElement>().Reverse()) {
				if (obj.HasAttribute("Name")) {
					continue;
				}

				var contentNodes = new List<XmlElement>();
				foreach (XmlNode child in obj.ChildNodes) {
					if (
						child is XmlElement childElem &&
						childElem.Name != "Content" &&
						childElem.Name != "Properties"
					) {
						contentNodes.Add(childElem);
					} else if (
						child is XmlElement contentElem &&
						contentElem.Name == "Content"
					) {
						foreach (XmlNode sub in contentElem.ChildNodes) {
							if (sub is XmlElement subElem) {
								contentNodes.Add(subElem);
							}
						}
					}
				}

				if (contentNodes.Count == 0) {
					continue;
				}

				bool allHaveXY = contentNodes.All(n => n.HasAttribute("X") && n.HasAttribute("Y"));
				if (!allHaveXY) {
					continue;
				}

				float? minX = null, minY = null;
				foreach (var node in contentNodes) {
					float x = GetNumericAttr<float>(node, "X", 0);
					float y = GetNumericAttr<float>(node, "Y", 0);

					if (minX == null || x < minX) {
						minX = x;
					}

					if (minY == null || y < minY) {
						minY = y;
					}
				}

				if (minX == null || minY == null) {
					continue;
				}

				if (Math.Abs(minX.Value) > EPS && Math.Abs(minX.Value) != 0f) {
					obj.SetAttribute("X", Helpers.Format.ToString(minX.Value, floatPrecision));
				} else {
					obj.RemoveAttribute("X");
				}

				if (Math.Abs(minY.Value) > EPS && Math.Abs(minY.Value) != 0f) {
					obj.SetAttribute("Y", Helpers.Format.ToString(minY.Value, floatPrecision));
				} else {
					obj.RemoveAttribute("Y");
				}

				foreach (var node in contentNodes) {
					float relX = GetNumericAttr<float>(node, "X", 0) - minX.Value;
					float relY = GetNumericAttr<float>(node, "Y", 0) - minY.Value;

					if (Math.Abs(relX) > EPS && Math.Abs(relX) != 0f) {
						node.SetAttribute("X", Helpers.Format.ToString(relX, floatPrecision));
					} else {
						node.RemoveAttribute("X");
					}

					if (Math.Abs(relY) > EPS && Math.Abs(relY) != 0f) {
						node.SetAttribute("Y", Helpers.Format.ToString(relY, floatPrecision));
					} else {
						node.RemoveAttribute("Y");
					}
				}
			}
		}

		private static T GetNumericAttr<T>(XmlElement elem, string attrName, T defaultValue) where T : struct {
			if (elem.HasAttribute(attrName) && elem.GetAttribute(attrName) is string attrVal) {
				if (typeof(T) == typeof(float) && float.TryParse(attrVal, out var floatVal)) {
					return (T)(object)floatVal;
				} else if (typeof(T) == typeof(int) && int.TryParse(attrVal, out var intVal)) {
					return (T)(object)intVal;
				}
			}

			return defaultValue;
		}
	}

	public static class Operations {
		private static void RemoveFlatNodes(XDocument doc) {
			foreach (var elem in doc.Descendants().ToList()) {
				if (!elem.HasElements && !elem.HasAttributes) {
					elem.Remove();
				}
			}
		}

		private static string RemoveLineBreaks(string xml) {
			return Regex.Replace(xml, @"\s+/>", "/>");
		}

		public static string Linearize(XDocument doc) {
			using (var stringWriter = new General.StandardStringWriter())
			using (var writer = XmlWriter.Create(stringWriter, new XmlWriterSettings {
				Indent = false,
				NewLineHandling = NewLineHandling.Replace,
				OmitXmlDeclaration = false
			})) {
				doc.Save(writer);
				writer.Flush();

				RemoveFlatNodes(doc);
				string result = stringWriter.ToString();
				// Merge declaration with content by removing all newlines
				result = Regex.Replace(result, @"\r?\n", "");
				return RemoveLineBreaks(result);
			}
		}

		public static string Standard(XDocument doc) {
			using (var stringWriter = new General.StandardStringWriter())
			using (var writer = XmlWriter.Create(stringWriter, new XmlWriterSettings {
				Indent = true,
				IndentChars = "\t",

				NewLineChars = "\r\n",
				NewLineHandling = NewLineHandling.Replace,

				OmitXmlDeclaration = false
			})) {
				doc.Save(writer);
				writer.Flush();

				RemoveFlatNodes(doc);
				return RemoveLineBreaks(stringWriter.ToString());
			}
		}

		public static string PrettyPrint(XDocument doc) {
			string xml = Operations.Standard(doc);

			var lines = xml.Split(new[] { "\r\n" }, StringSplitOptions.None);
			var result = new List<string>();

			foreach (var line in lines) {
				var match = Regex.Match(line, @"^(\s*)<([^\s>]+)\s+(.+?)(>|/>)$");

				if (match.Success) {
					string indent = match.Groups[1].Value;
					string tagName = match.Groups[2].Value;
					string attrsStr = match.Groups[3].Value;
					string ending = match.Groups[4].Value;

					var attrMatches = Regex.Matches(attrsStr, @"(\w+)=""[^""]*""");

					if (attrMatches.Count > 1) {
						result.Add($"{indent}<{tagName} {attrMatches[0].Value}");

						string alignSpace = new string(' ', tagName.Length + 2);

						for (int i = 1; i < attrMatches.Count; i++) {
							if (i == attrMatches.Count - 1) {
								result.Add($"{indent}{alignSpace}{attrMatches[i].Value}{ending}");
							} else {
								result.Add($"{indent}{alignSpace}{attrMatches[i].Value}");
							}
						}
					} else {
						result.Add(line);
					}
				} else {
					result.Add(line);
				}
			}

			return RemoveLineBreaks(string.Join("\r\n", result));
		}
	}
}