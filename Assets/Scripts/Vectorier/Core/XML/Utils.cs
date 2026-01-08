using UnityEngine;

using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Debug = Logger.Debug;

using Vectorier.Core.Components;

// -=-=-=- //

namespace Vectorier.Core.XML {
	public class Utf8StringWriter : StringWriter {
		public override Encoding Encoding => Encoding.UTF8;
	}

	public static class Utils {
		public static string[] UnusedTracks = {
			"cs00.xml",
			"cs01.xml",
			"cs02.xml",
			"cs01a.xml",
			"DelayTest.xml",
			"jump_auto.xml",
			"MovingTest.xml",
			"DelayedTest.xml",
			"CS_yard_cs01.xml",
			"CS_yard_cs02.xml",
			"Lift_example.xml",
			"reverse_test.xml",
			"NewMoveObject.xml",
			"cs02withTruck.xml",
			"ForceImageType.xml",
			"CS_techpark_cs01.xml",
			"CS_yard_cs02_new.xml",
			"cs_bike_starting.xml",
			"Techoparkgameplay.xml",
			"landing_wait_jump.xml",
			"cs_bike_starting_one_model.xml"
		};

		public static string[] Attributes = {
			// basic
			"Name",
			"ClassName",

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
			"LifeTime",

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

		public static object ParseSkins(
			string SkinsInput,
			bool list = false
		) {
			// Split input by newline and remove any carriage returns
			List<string> SkinsList = SkinsInput
				.Replace(
					Path.DirectorySeparatorChar,
					Path.AltDirectorySeparatorChar
				)
				.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries)
				.Select(line => line.Trim('\r'))
				.ToList();

			// Split each entry by "|" and collect all parts
			List<string> splitList = new List<string>();

			foreach (var line in SkinsList) {
				splitList.AddRange(line.Split('|'));
			}

			// Remove empty entries
			splitList = splitList.Where(elem => !string.IsNullOrWhiteSpace(elem)).ToList();

			// Remove entries starting with ">"
			splitList = splitList.Where(elem => !elem.StartsWith(">")).ToList();

			if (list) {
				return splitList;
			}

			// Join all remaining entries with pipe
			return string.Join("|", splitList);
		}

		public static string Validate(
			string fileInput
		) {
			if (!File.Exists(fileInput)) {
				return $"File not found: {fileInput}";
			}

			try {
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(fileInput);

				return null;
			} catch (XmlException ex) {
				return $"XML Error: {ex.Message} (Line {ex.LineNumber}, Position {ex.LinePosition})";
			} catch (Exception ex) {
				return $"Unexpected Error: {ex.Message}";
			}
		}

		public static class Optimize {
			public static void General(
				string fileInput,
				string fileOutput = null,

				string nodeName = "Root",

				string newlineCharacter = "\r\n",
				string indentCharacter = "\t",

				bool removeFlatNodes = false,
				bool shortenLineBreaks = true
			) {
				if (fileOutput == null) {
					fileOutput = fileInput;
				}

				if (!newlineCharacter.EndsWith("\n")) {
					indentCharacter = "";
				}

				XDocument doc = XDocument.Load(fileInput, LoadOptions.PreserveWhitespace);
				XElement node = doc.Root?.Element(nodeName);

				if (node != null) {
					RemoveEmptyFactorsFromXml(
						root: node,

						descendantsNames: new[] { "Object" },
						elementNames: new[] { "Content" }
					);

					if (removeFlatNodes) {
						RemoveFlatNodes(node);
					}
				}

				XmlWriterSettings settings = new XmlWriterSettings {
					Indent = !string.IsNullOrEmpty(indentCharacter),
					IndentChars = indentCharacter,
					NewLineChars = newlineCharacter,
					NewLineHandling = NewLineHandling.Replace,
					OmitXmlDeclaration = false
				};

				string xmlContent;
				using (var stringWriter = new Utf8StringWriter())
				using (var writer = XmlWriter.Create(stringWriter, settings)) {
					doc.Save(writer);
					writer.Flush();
					xmlContent = stringWriter.ToString();
				}

				if (indentCharacter == "\t") {
					xmlContent = Regex.Replace(xmlContent, @"^( +)", match => {
						int spaceCount = match.Value.Length;
						int tabCount = spaceCount / 2; // XmlWriter typically uses 2 spaces

						return new string('\t', tabCount);
					}, RegexOptions.Multiline);
				}

				if (shortenLineBreaks) {
					xmlContent = Regex.Replace(xmlContent, @"\s+/>", "/>");
				}

				xmlContent = Regex.Replace(xmlContent, @"^\s*$\r?\n", "", RegexOptions.Multiline);

				File.WriteAllText(fileOutput, xmlContent, Encoding.UTF8);
			}

			public static void Objects(
				XmlNode root,

				int floatPrecision = -1
			) {
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

				// process from innermost to outermost <Object>
				foreach (XmlElement obj in objectNodes.Cast<XmlElement>().Reverse()) {
					if (obj.HasAttribute("Name")) {
						continue;
					}

					// collect all relevant children (<Object>, <Trigger>, etc.)
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

					// check if all nodes have both X and Y attributes
					bool allHaveXY = contentNodes.All(n => n.HasAttribute("X") && n.HasAttribute("Y"));

					// skip transformation if any node is missing X/Y
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

					// set parent coords
					if (Math.Abs(minX.Value) > EPS) {
						obj.SetAttribute("X", Helpers.ToString(minX.Value, floatPrecision));
					} else {
						obj.RemoveAttribute("X");
					}

					if (Math.Abs(minY.Value) > EPS)
						// keep game-space sign
						obj.SetAttribute("Y", Helpers.ToString(minY.Value, floatPrecision));
					else {
						obj.RemoveAttribute("Y");
					}

					// adjust children to be relative to new parent
					foreach (var node in contentNodes) {
						float relX = GetNumericAttr<float>(node, "X", 0) - minX.Value;
						float relY = GetNumericAttr<float>(node, "Y", 0) - minY.Value;

						if (Math.Abs(relX) > EPS) {
							node.SetAttribute("X", Helpers.ToString(relX, floatPrecision));
						} else {
							node.RemoveAttribute("X");
						}

						if (Math.Abs(relY) > EPS) {
							node.SetAttribute("Y", Helpers.ToString(relY, floatPrecision));
						} else {
							node.RemoveAttribute("Y");
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

				// Process this node
				if (root.Attributes != null && root.Attributes.Count > 0) {
					var ordered = new List<XmlAttribute>();
					var remaining = new List<XmlAttribute>();

					// separate attributes that are in the order list and those that aren't
					foreach (XmlAttribute attr in root.Attributes) {

						int index = System.Array.IndexOf(attrs, attr.Name);
						if (index >= 0) {
							ordered.Add(attr);
						} else {
							remaining.Add(attr);
						}
					}

					// Sort the 'ordered' list according to the attribute array
					ordered.Sort((a, b) => {
						int indexA = System.Array.IndexOf(attrs, a.Name);
						int indexB = System.Array.IndexOf(attrs, b.Name);

						return indexA.CompareTo(indexB);
					});

					// combine ordered + remaining
					ordered.AddRange(remaining);

					// rewrite attributes
					XmlAttribute[] original = new XmlAttribute[root.Attributes.Count];
					root.Attributes.CopyTo(original, 0);

					foreach (XmlAttribute attr in original) {
						root.Attributes.Remove(attr);
					}

					foreach (XmlAttribute attr in ordered) {
						root.Attributes.Append(attr);
					}
				}

				// recursively process child nodes
				foreach (XmlNode child in root.ChildNodes) {
					Attributes(child, attrs);
				}
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

			private static void RemoveFlatNodes(XElement root) {
				foreach (var elem in root.Descendants().ToList()) {
					// remove element if it has no children and no attributes
					if (!elem.HasElements && !elem.HasAttributes) {
						elem.Remove();
					}
				}
			}
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