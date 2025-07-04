using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEditor;

using Vector3 = UnityEngine.Vector3;

// -=-=-=- //
// Functions

namespace Vectorier {
	public static class MapUtils {
		public static class Collision {
			public static void Validator(GameObject go) {
				// Height
				float platformLocalHeight = go.transform.localScale.y;
				float platformGlobalHeight = go.transform.lossyScale.y;

				// Get the local Y position
				float platformLocalPositionY = go.transform.position.y;

				// Global position is directly available from transform.position
				float platformGlobalPositionY = go.transform.position.y;

				// Check if the parent is a prefab
				bool isParentPrefab = go.transform.parent != null &&
					PrefabUtility.GetPrefabAssetType(go.transform.parent.gameObject) != PrefabAssetType.NotAPrefab;

				// Check for height
				if (platformGlobalHeight % Engine.Math.GameUnit != 0) {
					float nearestLocalPositiveHeight = Mathf.Ceil(platformLocalHeight / Engine.Math.GameUnit) * Engine.Math.GameUnit;
					float nearestLocalNegativeHeight = Mathf.Floor(platformLocalHeight / Engine.Math.GameUnit) * Engine.Math.GameUnit;

					string warningMessageScaleHeight;
					
					// If it's a prefab, change the message to include prefab-specific information
					if (!isParentPrefab) {
						warningMessageScaleHeight = string.Format(
							CultureInfo.InvariantCulture,
							@"Global scale of the ""{0}"" % {1} != 0. Set it to {2} ( up) / {3} (down) to avoid issues [click to toggle]",
							go.name,
							Engine.Math.GameUnit.ToString(CultureInfo.InvariantCulture),
							nearestLocalPositiveHeight.ToString(CultureInfo.InvariantCulture),
							nearestLocalNegativeHeight.ToString(CultureInfo.InvariantCulture)
						);

						Debug.LogWarning(warningMessageScaleHeight, go);
					}
				}

				// Check for Position (y)
				if (platformGlobalPositionY % Engine.Math.GameUnit != 0) {
					float nearestLocalPositivePositionY = Mathf.Ceil(platformLocalPositionY / Engine.Math.GameUnit) * Engine.Math.GameUnit;
					float nearestLocalNegativePositionY = Mathf.Floor(platformLocalPositionY / Engine.Math.GameUnit) * Engine.Math.GameUnit;

					string warningMessagePositionY;
					
					// If it's a prefab, change the message to include prefab-specific information
					if (!isParentPrefab) {
						warningMessagePositionY = string.Format(
							CultureInfo.InvariantCulture,
							@"Global Y pos. of ""{0}"" % {1} != 0. Set it to {2} (up) / {3} (down) to avoid issues [click to toggle]",
							go.name,
							Engine.Math.GameUnit.ToString(CultureInfo.InvariantCulture),
							nearestLocalPositivePositionY.ToString(CultureInfo.InvariantCulture),
							nearestLocalNegativePositionY.ToString(CultureInfo.InvariantCulture)
						);

						Debug.LogWarning(warningMessagePositionY, go);
					};
				}
			}
		}
	}

	public partial class BuildMap {
		public static string gloalRegex = @" ?\((.*?)\)| ?\[.*?\]";
	}

	public static class Engine {
		public static class Math {
			public static float GameUnit = (25f / 32f) / 100f; // 0.0078125
			public static class GameUnits {
				public static string Multiply(float value, bool round = false) {
					float calculated = value * 100;

					if (round) {
						calculated = (float)System.Math.Round(calculated);
					}

					string formatted = calculated.ToString("F3", CultureInfo.InvariantCulture);
					return formatted.TrimEnd('0').TrimEnd('.');
				}

				public static float Divide(string value) {
					return float.Parse(value, CultureInfo.InvariantCulture) / 100;
				}

				public static Vector3 DivideVector(XmlNode contentNode, string ItemName1 = "X", string ItemName2 = "Y") {
					return new Vector3(
						GameUnits.Divide(contentNode.Attributes.GetNamedItem(ItemName1).Value),
						-GameUnits.Divide(contentNode.Attributes.GetNamedItem(ItemName2).Value),
						0
					);
				}
			}
		}
	}

	public static class Animations {
		public static string[] NodesOrdered = {
			"NHip_1", "NHip_2", "NStomach", "NChest", "NNeck", "NShoulder_1", "NShoulder_2", "NKnee_1", "NKnee_2",
			"NAnkle_1", "NAnkle_2", "NToe_1", "NHeel_1", "NToeTip_1", "NToeS_1", "NHeel_2", "NToe_2", "NToeTip_2", "NToeS_2",
			"NElbow_1", "NElbow_2", "NWrist_1", "NWrist_2", "NKnuckles_1", "NFingertips_1", "NKnucklesS_1", "NKnuckles_2", "NFingertips_2",
			"NKnucklesS_2", "NHead", "NTop", "NChestS_1", "NChestS_2", "NStomachS_1", "NStomachS_2", "NChestF", "NStomachF", "NPelvisF",
			"NHeadS_1", "NHeadS_2", "NHeadF", "NPivot", "DetectorH", "DetectorV", "COM", "Camera"
		};

		public static XmlDocument Parse(string inputPath) {
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
						string nodeName = j < Animations.NodesOrdered.Length ? Animations.NodesOrdered[j] : $"Unknown_{j}";

						XmlElement nodeElement = xmlDoc.CreateElement("Node");
						nodeElement.SetAttribute("Name", nodeName);
						nodeElement.SetAttribute("X", vector3.x.ToString("F6", CultureInfo.InvariantCulture));
						nodeElement.SetAttribute("Y", vector3.y.ToString("F6", CultureInfo.InvariantCulture));
						nodeElement.SetAttribute("Z", vector3.z.ToString("F6", CultureInfo.InvariantCulture));
						frameElement.AppendChild(nodeElement);
					}
					
					root.AppendChild(frameElement);
				}

				return xmlDoc;
			}
		}
	}
}