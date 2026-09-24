using UnityEngine;

using System.Xml;
using System.Collections.Generic;

#nullable enable
#pragma warning disable CS8618

// -=-=-=- //

namespace Vectorier.Core.Components {
	public static class ModelComponent {
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

			string objName = Helpers.Get.Name(go);

			var modelProperties = go.GetComponent<ModelProperties>();

			if (modelProperties == null || !modelProperties.enabled) {
				Debug.LogWarning($"ModelProperties component is missing on \"{go.name}\"", go);
				return;
			}

			XmlDocument xml = node.OwnerDocument!;
			XmlElement mainElement = xml.CreateElement("Model");

			mainElement.SetAttribute("ClassName", objName);

			var (x, y) = Helpers.Get.Position(go);

			if (x != 0) {
				mainElement.SetAttribute("X", Helpers.Format.Multiply(x, floatPrecision));
			}

			if (y != 0) {
				mainElement.SetAttribute("Y", Helpers.Format.Multiply(y, floatPrecision));
			}

			if (modelProperties.Type != 0) {
				mainElement.SetAttribute("Type", modelProperties.Type.ToString());
			}

			if (modelProperties.UseLifeTime) {
				mainElement.SetAttribute("LifeTime", modelProperties.LifeTime.ToString());
			}

			Helpers.WriteMode.Apply(go, mainElement);

			XmlNode targetParent = parentElement ?? node.FirstChild;
			targetParent?.AppendChild(mainElement);
		}

		// =========================================================
		// VALIDATORS
		// =========================================================

		public static class Validators {
			private static readonly string[] NODES_SCENE = { "Nodes", "Edges", "Figures" };

			private static readonly Dictionary<string, string[]> RULE_SKELETON_POSITIVE =
				new Dictionary<string, string[]> {
					{ "Camera", new[] { "Type" } }
				};

			private static readonly Dictionary<string, string[]> RULE_OBJECT_POSITIVE =
				new Dictionary<string, string[]> {
					{ "NPivot", new[] { "Type" } }
				};

			private static XmlElement? GetRoot(XmlDocument doc) {
				return doc?.DocumentElement;
			}

			public static bool IsSkeleton(XmlDocument doc) {
				try {
					var root = GetRoot(doc);
					if (root == null) {
						return false;
					}

					foreach (var kv in RULE_SKELETON_POSITIVE) {
						var elem = root.SelectSingleNode($"./{NODES_SCENE[0]}/{kv.Key}") as XmlElement;

						if (elem == null) {
							return false;
						}

						foreach (var attr in kv.Value) {
							if (!elem.HasAttribute(attr)) {
								return false;
							}
						}
					}

					return true;
				} catch {
					return false;
				}
			}

			public static bool IsObject(XmlDocument doc) {
				try {
					var root = GetRoot(doc);
					if (root == null) {
						return false;
					}

					var scenePath = $"./{NODES_SCENE[0]}";

					foreach (var kv in RULE_OBJECT_POSITIVE) {
						var elem = root.SelectSingleNode($"{scenePath}/{kv.Key}") as XmlElement;

						if (elem == null) {
							return false;
						}

						foreach (var attr in kv.Value) {
							if (!elem.HasAttribute(attr)) {
								return false;
							}
						}
					}

					return true;
				} catch {
					return false;
				}
			}

			public static bool IsSkin(XmlDocument doc) {
				try {
					var root = GetRoot(doc);
					if (root == null) {
						return false;
					}

					bool hasScene = true;

					foreach (var node in NODES_SCENE) {
						if (root.SelectSingleNode($"./{node}") == null) {
							hasScene = false;
							break;
						}
					}

					return IsSkeleton(doc) || (hasScene && !IsObject(doc));
				} catch {
					return false;
				}
			}
		}

		// =========================================================
		// MODEL HELPERS (STATIC DATA ONLY)
		// =========================================================

		public static class ModelHelpers {
			public static class Skeleton {
				public static class Nodes {
					public static readonly string[] Ordered = {
						"NHip_1","NHip_2","NStomach","NChest","NNeck",
						"NShoulder_1","NShoulder_2","NKnee_1","NKnee_2",
						"NAnkle_1","NAnkle_2","NToe_1","NHeel_1",
						"NToeTip_1","NToeS_1","NHeel_2","NToe_2",
						"NToeTip_2","NToeS_2","NElbow_1","NElbow_2",
						"NWrist_1","NWrist_2","NKnuckles_1","NFingertips_1",
						"NKnucklesS_1","NKnuckles_2","NFingertips_2","NKnucklesS_2",
						"NHead","NTop","NChestS_1","NChestS_2",
						"NStomachS_1","NStomachS_2","NChestF","NStomachF",
						"NPelvisF","NHeadS_1","NHeadS_2","NHeadF",
						"NPivot","DetectorH","DetectorV","COM","Camera"
					};

					public static readonly (string, string)[] Connections = {
						("NStomach","NHip_2"),
						("NStomach","NHip_1"),
						("NHip_2","NHip_1"),
						("NChest","NStomach"),
						("NNeck","NChest"),
						("NShoulder_1","NNeck"),
						("NShoulder_2","NNeck"),
						("NKnee_1","NHip_1"),
						("NKnee_2","NHip_2"),
						("NAnkle_1","NKnee_1"),
						("NAnkle_2","NKnee_2"),
						("NToe_1","NAnkle_1"),
						("NHeel_1","NAnkle_1"),
						("NHeel_1","NToe_1"),
						("NToe_1","NToeTip_1"),
						("NToe_1","NToeS_1"),
						("NToeTip_1","NToeS_1"),
						("NHeel_1","NToeS_1"),
						("NAnkle_1","NToeS_1"),
						("NHeel_2","NAnkle_2"),
						("NToe_2","NAnkle_2"),
						("NToe_2","NHeel_2"),
						("NToeTip_2","NToe_2"),
						("NToe_2","NToeS_2"),
						("NToeTip_2","NToeS_2"),
						("NToeS_2","NHeel_2"),
						("NToeS_2","NAnkle_2"),
						("NElbow_1","NShoulder_1"),
						("NWrist_1","NElbow_1"),
						("NKnuckles_1","NWrist_1"),
						("NFingertips_1","NKnuckles_1"),
						("NKnuckles_1","NKnucklesS_1"),
						("NKnucklesS_1","NWrist_1"),
						("NFingertips_1","NKnucklesS_1"),
						("NElbow_2","NShoulder_2"),
						("NWrist_2","NElbow_2"),
						("NKnuckles_2","NWrist_2"),
						("NFingertips_2","NKnuckles_2"),
						("NKnucklesS_2","NKnuckles_2"),
						("NKnucklesS_2","NWrist_2"),
						("NFingertips_2","NKnucklesS_2"),
						("NNeck","NHead"),
						("NHead","NTop"),
						("NChest","NChestS_1"),
						("NChestS_2","NChest"),
						("NStomach","NStomachS_1"),
						("NStomach","NStomachS_2"),
						("NNeck","NChestS_1"),
						("NChestS_2","NNeck"),
						("NStomachS_1","NChest"),
						("NStomachS_2","NChest"),
						("NChestS_2","NChestS_1"),
						("NStomachS_2","NStomachS_1"),
						("NChestS_1","NChestF"),
						("NChestS_2","NChestF"),
						("NStomachF","NStomachS_1"),
						("NStomachF","NStomachS_2"),
						("NChestF","NNeck"),
						("NStomachF","NChest"),
						("NChest","NChestF"),
						("NStomach","NStomachF"),
						("NPelvisF","NHip_1"),
						("NHip_2","NPelvisF"),
						("NStomach","NPelvisF"),
						("NHead","NHeadS_1"),
						("NHeadS_2","NHead"),
						("NTop","NHeadS_1"),
						("NHeadS_2","NTop"),
						("NHeadS_1","NHeadS_2"),
						("NHeadF","NHead"),
						("NHeadF","NHeadS_1"),
						("NHeadS_2","NHeadF"),
						("NHeadF","NTop"),
						("NStomach","NPivot"),
						("NPelvisF","NPivot"),
						("NHip_2","NPivot"),
						("NHip_1","NPivot")
					};
				}
			}
		}
	}
}