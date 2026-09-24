using UnityEditor;
using UnityEngine;

using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

using Vector3 = UnityEngine.Vector3;

// -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- //

namespace Vectorier.Core.Components.Animation {
	public class Handler : MonoBehaviour {
		[MenuItem("Vectorier/Miscellaneous/Animation/Decompile from BIN")]
		public static void Decompile_Single() => Process(false, true);

		[MenuItem("Vectorier/Miscellaneous/Animation/Decompile from BIN (Batch)")]
		public static void Decompile_Batch() => Process(true, true);

		[MenuItem("Vectorier/Miscellaneous/Animation/Compile from XML")]
		public static void Compile_Single() => Process(false, false);

		[MenuItem("Vectorier/Miscellaneous/Animation/Compile from XML (Batch)")]
		public static void Compile_Batch() => Process(true, false);

		private static void Process(bool batch, bool decompile) {
			string baseDir = GetAnimationDirectory();
			string cwd = Application.dataPath;

			string inputPath;
			string[] files;

			if (!batch) {
				inputPath = decompile
					? EditorUtility.OpenFilePanel("Select .bin file", baseDir, Vectorier.Core.Game.Properties.Extensions.File.Animation)
					: EditorUtility.OpenFilePanel("Select .xml file", baseDir, Vectorier.Core.Game.Properties.Extensions.File.XML);

				if (string.IsNullOrEmpty(inputPath)) {
					return;
				}

				files = new[] { inputPath };
			} else {
				inputPath = EditorUtility.OpenFolderPanel("Select folder", baseDir, "");

				if (string.IsNullOrEmpty(inputPath)) {
					return;
				}

				files = Directory.GetFiles(inputPath, decompile ? ("*." + Vectorier.Core.Game.Properties.Extensions.File.Animation) : ("*." + Vectorier.Core.Game.Properties.Extensions.File.XML));
			}

			foreach (string file in files) {
				try {
					if (decompile) {
						HandleDecompile(file, cwd);
					} else {
						HandleCompile(file, cwd);
					}

					if (!batch) {
						Debug.Log($"Processed: {file}");
					}

					if (Array.IndexOf(files, file) == files.Length - 1) {
						string output = batch
							? Directory.GetParent(file).ToString()
							: file;

						EditorUtility.DisplayDialog("Success",
							$"Finished processing:\n{output}", "OK");
					}
				} catch (Exception ex) {
					Debug.LogError($"Error processing {file}: {ex.Message}");

					EditorUtility.DisplayDialog("Error",
						$"{file}\n\n{ex.Message}", "OK");

					if (batch) {
						return;
					}
				}
			}
		}

		private static void HandleDecompile(string file, string cwd) {
			XmlDocument xml = ParseBin(file);
			if (xml == null) {
				return;
			}

			string outputPath = Path.ChangeExtension(file, ".xml");

			using (StringWriter sw = new StringWriter())
			using (XmlTextWriter xtw = new XmlTextWriter(sw)) {
				xtw.Formatting = Formatting.Indented;
				xtw.Indentation = 1;
				xtw.IndentChar = '\t';

				xml.WriteTo(xtw);

				string formatted = sw.ToString().Replace(" />", "/>");
				File.WriteAllText(outputPath, formatted, Encoding.UTF8);
			}
		}

		public static XmlDocument ParseBin(string inputPath) {
			if (!File.Exists(inputPath)) {
				return null;
			}

			using (FileStream fs = new FileStream(inputPath, FileMode.Open))
			using (BinaryReader reader = new BinaryReader(fs)) {
				XmlDocument doc = new XmlDocument();
				XmlElement root = doc.CreateElement("Frames");

				int frameCount = reader.ReadInt32();
				root.SetAttribute("Count", frameCount.ToString());
				doc.AppendChild(root);

				for (int i = 0; i < frameCount; i++) {
					reader.ReadByte(); // unused
					int setCount = reader.ReadInt32();

					XmlElement frame = doc.CreateElement($"Frame_{i + 1}");

					for (int j = 0; j < setCount; j++) {
						Vector3 vctr = new Vector3 {
							x = reader.ReadSingle(),
							y = -reader.ReadSingle(),
							z = reader.ReadSingle()
						};

						string[] nodes = Vectorier.Core.Components.ModelComponent.ModelHelpers.Skeleton.Nodes.Ordered;
						string nodeName = j < nodes.Length ? nodes[j] : $"Unknown_{j}";

						XmlElement node = doc.CreateElement("Node");
						node.SetAttribute("Name", nodeName);

						node.SetAttribute("X", vctr.x.ToString("F6", CultureInfo.InvariantCulture));
						node.SetAttribute("Y", vctr.y.ToString("F6", CultureInfo.InvariantCulture));
						node.SetAttribute("Z", vctr.z.ToString("F6", CultureInfo.InvariantCulture));

						frame.AppendChild(node);
					}

					root.AppendChild(frame);
				}

				return doc;
			}
		}

		private static void HandleCompile(string file, string cwd) {
			string outputPath = Path.ChangeExtension(file, "." + Vectorier.Core.Game.Properties.Extensions.File.Animation);

			XmlDocument xml = new XmlDocument();
			xml.Load(file);

			XmlElement root = xml.DocumentElement;
			if (root == null || root.Name != "Frames" || !root.HasAttribute("Count")) {
				throw new Exception("Invalid XML format.");
			}

			using (FileStream fs = new FileStream(outputPath, FileMode.Create))
			using (BinaryWriter writer = new BinaryWriter(fs)) {
				int frameCount = int.Parse(root.GetAttribute("Count"));
				writer.Write(frameCount);

				foreach (XmlNode frameNode in root.ChildNodes) {
					if (
						frameNode.NodeType != XmlNodeType.Element ||
						!frameNode.Name.StartsWith("Frame_")
					) {
						continue;
					}

					writer.Write((byte)0);

					List<Vector3> vectors = new List<Vector3>();

					foreach (XmlNode node in frameNode.ChildNodes) {
						if (node.Name != "Node") {
							continue;
						}

						float x = float.Parse(node.Attributes["X"].Value, CultureInfo.InvariantCulture);
						float y = -float.Parse(node.Attributes["Y"].Value, CultureInfo.InvariantCulture);
						float z = float.Parse(node.Attributes["Z"].Value, CultureInfo.InvariantCulture);

						vectors.Add(new Vector3(x, y, z));
					}

					writer.Write(vectors.Count);

					foreach (var v in vectors) {
						writer.Write(v.x);
						writer.Write(v.y);
						writer.Write(v.z);
					}
				}
			}
		}

		private static string GetAnimationDirectory() {
			string path;

			if (Directory.Exists(Vectorier.Settings.Core.GameDirectoryLegacy)) {
				path = Vectorier.Settings.Core.GameDirectoryLegacy;
			} else {
				path = Path.Combine(Vectorier.Settings.Core.GameDirectoryModern, "Vector_Data", "StreamingAssets");
			}

			return Path.Combine(path, "animations");
		}
	}
}