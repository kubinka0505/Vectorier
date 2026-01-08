using UnityEditor;
using UnityEngine;

using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

using Vectorier;
using Vector3 = UnityEngine.Vector3;

// -=-=-=- //

public class AnimationDecompile : MonoBehaviour {
	[MenuItem("Vectorier/Miscellaneous/Animation/Decompile from BIN")]
	public static void AnimationDecompile_Singleton() {
		AnimationDecompileWrite(false);
	}

	[MenuItem("Vectorier/Miscellaneous/Animation/Decompile from BIN (Batch)")]
	public static void AnimationDecompile_Batch() {
		AnimationDecompileWrite(true);
	}

	public static XmlDocument ParseBin(string inputPath) {
		if (!File.Exists(inputPath)) {
			return null;
		}

		using (FileStream fileStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read))
		using (BinaryReader reader = new BinaryReader(fileStream, Encoding.UTF8)) {
			XmlDocument xmlDoc = new XmlDocument();
			XmlElement root = xmlDoc.CreateElement("Frames");
			root.SetAttribute("Count", reader.ReadInt32().ToString());
			xmlDoc.AppendChild(root);
			
			int blockCount = int.Parse(root.GetAttribute("Count"));

			for (int i = 0; i < blockCount; i++) {
				// Skip unused byte
				reader.ReadByte();
				int setCount = reader.ReadInt32();
				XmlElement frameElement = xmlDoc.CreateElement($"Frame_{i + 1}");
				
				for (int j = 0; j < setCount; j++) {
					Vector3 vector3 = new Vector3 {
						x = reader.ReadSingle(),
						y = -reader.ReadSingle(), // Inverse
						z = reader.ReadSingle()
					};
					
					// Animations.NodesOrdered are parts of Vectorier (namespace),
					string[] nodes = Vectorier.Core.Components.Model.ModelHelpers.Skeleton.Nodes.Ordered;
					string nodeName = j < nodes.Length ? nodes[j] : $"Unknown_{j}";

					XmlElement nodeElement = xmlDoc.CreateElement("Node");
					nodeElement.SetAttribute("Name", nodeName);

					string precision = "F6";

					nodeElement.SetAttribute("X", vector3.x.ToString(precision, CultureInfo.InvariantCulture));
					nodeElement.SetAttribute("Y", vector3.y.ToString(precision, CultureInfo.InvariantCulture));
					nodeElement.SetAttribute("Z", vector3.z.ToString(precision, CultureInfo.InvariantCulture));

					frameElement.AppendChild(nodeElement);
				}
				
				root.AppendChild(frameElement);
			}

			return xmlDoc;
		}
	}

	public static void AnimationDecompileWrite(bool batch) {
		string cwd = Application.dataPath;
		string pardir = Path.Combine(Vectorier.Settings.GameDirectory, "animations");
		string inputPath;
		string[] inputFiles;

		if (pardir == null) {
			pardir = Directory.GetParent(cwd).ToString();
		}

		if (!batch) {
			inputPath = EditorUtility.OpenFilePanel("Select .bin file", pardir, "bin");

			if (string.IsNullOrEmpty(inputPath)) {
				return;
			}

			inputFiles = new string[] { inputPath };
		} else {
			inputPath = EditorUtility.OpenFolderPanel("Select directory containing .bin files", pardir, "");

			if (string.IsNullOrEmpty(inputPath)) {
				return;
			}

			inputFiles = Directory.GetFiles(inputPath, "*.bin");
		}

		foreach (string file in inputFiles) {
			try {
				string outputPath = Path.ChangeExtension(file, ".xml");
				XmlDocument outputFileContent = ParseBin(file);

				using (StringWriter stringWriter = new StringWriter()) {
					using (XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter) {
						Formatting = Formatting.Indented,
						Indentation = 1,
						IndentChar = '\t'
					}) {
						outputFileContent.WriteTo(xmlTextWriter);
					}

					string formattedXml = stringWriter.ToString().Replace(" />", "/>");
					File.WriteAllText(outputPath, formattedXml, Encoding.UTF8);
				}

				outputPath = outputPath
					.Substring(cwd.Length + 1)
					.Replace(Path.DirectorySeparatorChar, '/')
					.Trim('/');

				if (!batch) {
					Debug.Log($"File written to: {outputPath}");
				}

				if (Array.IndexOf(inputFiles, file) == inputFiles.Length - 1) {
					if (batch) {
						outputPath = Directory.GetParent(outputPath).ToString();
					}
					EditorUtility.DisplayDialog("Success", $"Files successfully written to: {outputPath}", "OK");
				}
				
			} catch (Exception ex) {
				Debug.LogError($"Error processing {file}: {ex.Message}");

				string message = batch 
					? $"Failed to process: {file}\nError: {ex.Message}"
					: $"An error occurred while processing the file:\n{ex.Message}";

				EditorUtility.DisplayDialog("Error", message, "OK");

				if (batch) return;
			}
		}
	}
}