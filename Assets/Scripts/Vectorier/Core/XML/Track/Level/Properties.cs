using UnityEngine;
using UnityEditor;

using System;
using System.Xml;
using System.Linq;
using System.Collections.Generic;

using static Vectorier.Core.Game;
using Vectorier.Core.Components;

using Debug = Logger.Debug;

// -=-=-=- //

namespace Vectorier.Core.XML.Track.Level {
	public static class Properties {

		public static void SetSets(
			XmlDocument xml,
			GameObject[] gos
		) {
			XmlNode rootNode = xml.DocumentElement ?? xml.AppendChild(xml.CreateElement("Root"));

			XmlNode setsNode = xml.SelectSingleNode("/Root/Sets");
			if (setsNode == null) {
				setsNode = xml.CreateElement("Sets");
				rootNode.AppendChild(setsNode);
			}

			SetsManager setsManager = gos
				.Select(obj => obj.GetComponent<SetsManager>())
				.FirstOrDefault(sm => sm != null);

			while (setsNode.HasChildNodes) {
				setsNode.RemoveChild(setsNode.FirstChild);
			}

			if (setsManager == null || !setsManager.enabled || setsManager.tags == null || setsManager.tags.Count == 0) {
				AppendSet(xml, setsNode, "City", "buildings.xml");

				AppendSet(xml, setsNode, "City", "buildings_downtown.xml");
				//AppendSet(xml, setsNode, "City", "buildings_construction.xml");
				//AppendSet(xml, setsNode, "City", "buildings_techpark.xml");

				AppendSet(xml, setsNode, "Ground", "objects.xml");

				AppendSet(xml, setsNode, "Ground", "objects_downtown.xml");
				AppendSet(xml, setsNode, "Ground", "objects_construction.xml");
				AppendSet(xml, setsNode, "Ground", "objects_techpark.xml");

				// AppendSet(xml, setsNode, "Ground", "objects_lab.xml");

				AppendSet(xml, setsNode, "Ground", "objects_custom.xml");

				return;
			}

			for (int i = 0; i < setsManager.tags.Count; i++) {
				string tag = setsManager.tags[i];
				if (string.IsNullOrEmpty(tag)) continue;

				int selection = (i < setsManager.dropdownSelections.Count)
					? setsManager.dropdownSelections[i]
					: 0;

				string type = selection switch {
					0 => "City",
					1 => "Ground",
					2 => "Library",
					_ => "Unknown"
				};

				string fileName = tag.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
					? tag
					: $"{tag}.xml";

				AppendSet(xml, setsNode, type, fileName);
			}
		}

		private static void AppendSet(
			XmlDocument xml,
			XmlNode parent,

			string type,
			string fileName
		) {
			XmlElement element = xml.CreateElement(type);
			XmlAttribute attr = xml.CreateAttribute("FileName");

			attr.Value = fileName;

			element.Attributes.Append(attr);
			parent.AppendChild(element);
		}

		public static void SetBackground(
			XmlDocument xml,
			string pathImage,
			string pathImageMirror,

			float x,
			float y,

			float width,
			float height
		) {
			if (x == 0) x = -3740;
			if (y == 0) y = 500;

			XmlNode objNode = xml.SelectSingleNode("/Root/Track/Object[@Factor='0.05']");
			if (objNode == null) {
				XmlNode trackNode = xml.SelectSingleNode("/Root/Track") ?? xml.CreateElement("Track");
				if (trackNode.ParentNode == null) {
					xml.DocumentElement.AppendChild(trackNode);
				}

				objNode = xml.CreateElement("Object");
				objNode.Attributes.Append(CreateAttribute(xml, "Factor", "0.05"));
				trackNode.AppendChild(objNode);
			}

			XmlNode contentNode = objNode.SelectSingleNode("Content") ?? objNode.AppendChild(xml.CreateElement("Content"));

			XmlNodeList imageNodes = contentNode.SelectNodes("Image");
			while (imageNodes.Count < 2) {
				XmlNode newImageNode = xml.CreateElement("Image");

				newImageNode.Attributes.Append(CreateAttribute(xml, "ClassName", "defaultBackground"));
				newImageNode.Attributes.Append(CreateAttribute(xml, "X", "0"));
				newImageNode.Attributes.Append(CreateAttribute(xml, "Y", "0"));
				newImageNode.Attributes.Append(CreateAttribute(xml, "Width", "0"));
				newImageNode.Attributes.Append(CreateAttribute(xml, "Height", "0"));

				contentNode.AppendChild(newImageNode);
				imageNodes = contentNode.SelectNodes("Image");
			}

			for (int i = 0; i < imageNodes.Count; i++) {
				XmlNode imageNode = imageNodes[i];
				string className = (i % 2 == 0) ? (!string.IsNullOrEmpty(pathImage) ? pathImage : pathImageMirror) : (!string.IsNullOrEmpty(pathImageMirror) ? pathImageMirror : pathImage);

				imageNode.Attributes["ClassName"].Value = className;
				imageNode.Attributes["Width"].Value = Helpers.ToString(width);
				imageNode.Attributes["Height"].Value = Helpers.ToString(height);

				float xPos = i * width;
				float yPos = -500 + y;

				XmlElement imageElement = (XmlElement)imageNodes[i];
				if (xPos != 0) {
					imageElement.SetAttribute("X", Helpers.ToString(xPos));
				} else imageElement.RemoveAttribute("X"); {
				}

				if (yPos != 0) {
					imageElement.SetAttribute("Y", Helpers.ToString(yPos));
				} else {
					imageElement.RemoveAttribute("Y");
				}
			}
		}

		public static void SetMusic(
			XmlDocument xml,

			string musicPath,
			float musicVolume
		) {
			XmlNode musicNode = xml.DocumentElement.SelectSingleNode("/Root/Music");

			if (musicNode == null) {
				XmlNode rootNode = xml.DocumentElement ?? xml.AppendChild(xml.CreateElement("Root"));
				musicNode = xml.CreateElement("Music");
				rootNode.AppendChild(musicNode);
			}

			if (
				musicVolume > 0 &&
				!string.IsNullOrEmpty(musicPath)
			) {
				XmlAttribute nameAttr = musicNode.Attributes["Name"] ?? xml.CreateAttribute("Name");
				if (nameAttr.ParentNode == null) {
					musicNode.Attributes.Append(nameAttr);
				}

				nameAttr.Value = musicPath;

				XmlAttribute volumeAttr = musicNode.Attributes["Volume"] ?? xml.CreateAttribute("Volume");
				if (volumeAttr.ParentNode == null) {
					musicNode.Attributes.Append(volumeAttr);
				}

				volumeAttr.Value = Helpers.ToString(musicVolume);
			} else if (musicNode != null) {
				musicNode.ParentNode.RemoveChild(musicNode);
			}
		}

		private static XmlAttribute CreateAttribute(
			XmlDocument doc,

			string name,
			string value
		) {
			XmlAttribute attr = doc.CreateAttribute(name);
			attr.Value = value;
			return attr;
		}

		public static bool DetectBikeStock(GameObject[] gos) {
			foreach (GameObject go in gos) {
				if (go.name.Equals("cs_bike_starting_bike", StringComparison.OrdinalIgnoreCase) && go.CompareTag("Model")) {
					return true;
				}
			}
			return false;
		}

		public static void SetSkins(
			XmlDocument xml,
			XmlNode node,

			string skins,
			string skinDefault = "0"
		) {
			XmlAttribute skinsAttr = node.Attributes["Skins"] ?? node.OwnerDocument.CreateAttribute("Skins");
			if (skinsAttr.ParentNode == null) {
				node.Attributes.Append(skinsAttr);
			}

			string parsedSkins = Utils.ParseSkins(skins) as string;
			if (string.IsNullOrEmpty(skins) || string.IsNullOrEmpty(parsedSkins)) {
				skinsAttr.Value = string.IsNullOrEmpty(skins) ? skinDefault : UnityEngine.Random.Range((int)1E8, (int)1E9 - 1).ToString();
			} else {
				skinsAttr.Value = parsedSkins;
			}
		}

		public static void SetModelColor(XmlNode node, Color color) {
			if (color == Color.black) {
				return;
			}

			XmlAttribute colorAttr = node.Attributes["Color"] ?? node.OwnerDocument.CreateAttribute("Color");
			if (colorAttr.ParentNode == null) {
				node.Attributes.Append(colorAttr);
			}

			// convert Unity Color to byte
			byte r = (byte)Mathf.RoundToInt(color.r * 255f);
			byte g = (byte)Mathf.RoundToInt(color.g * 255f);
			byte b = (byte)Mathf.RoundToInt(color.b * 255f);

			if (color.a < 1f) {
				Debug.LogWarning("Model color opacity changed, ignored.");
			}

			colorAttr.Value = $"00{r:X2}{g:X2}{b:X2}";
		}

		public static void ConfigureModel(
			XmlDocument xml,
			XmlNode node,

			string modelName,
			float spawnTime,
			string spawnName,
			float lifeTime,
			Color modelColor,
			string skins,
			bool hasBikeStock = false,
			bool bikeStock = false,
			bool allowTricks = false,
			string allowedSpawns = null,
			int? ai = null,
			bool? icon = null,
			bool isHelper = false
		) {
			XmlAttribute nameAttr = node.Attributes["Name"] ?? node.OwnerDocument.CreateAttribute("Name");
			XmlAttribute timeAttr = node.Attributes["Time"] ?? node.OwnerDocument.CreateAttribute("Time");
			XmlAttribute lifeAttr = node.Attributes["LifeTime"] ?? node.OwnerDocument.CreateAttribute("LifeTime");
			XmlAttribute birthAttr = node.Attributes["BirthSpawn"] ?? node.OwnerDocument.CreateAttribute("BirthSpawn");

			if (nameAttr.ParentNode == null) {
				node.Attributes.Append(nameAttr);
			}
			nameAttr.Value = modelName;

			if (spawnTime < 0) {
				Debug.LogError($"{modelName} spawn time is lower than 0.");
				return;
			} else if (spawnTime == 0) {
				node.Attributes.RemoveNamedItem("Time");
			}

			if (timeAttr.ParentNode == null) {
				node.Attributes.Append(timeAttr);
			}
			timeAttr.Value = Helpers.ToString(spawnTime);

			if (lifeAttr.ParentNode == null) {
				node.Attributes.Append(lifeAttr);
			}
			lifeAttr.Value = Helpers.ToString(lifeTime);

			if (birthAttr.ParentNode == null) {
				node.Attributes.Append(birthAttr);
			}
			birthAttr.Value = spawnName;

			if (!string.IsNullOrEmpty(allowedSpawns)) {
				XmlAttribute allowedAttr = node.Attributes["AllowedSpawns"] ?? node.OwnerDocument.CreateAttribute("AllowedSpawns");
				if (allowedAttr.ParentNode == null) {
					node.Attributes.Append(allowedAttr);
				}
				allowedAttr.Value = allowedSpawns;
			}

			XmlElement element = (XmlElement)node;
			XmlAttribute stocksAttr = element.Attributes["Stocks"];
			if (stocksAttr == null && hasBikeStock && bikeStock) {
				stocksAttr = element.OwnerDocument.CreateAttribute("Stocks");
				element.Attributes.Append(stocksAttr);
			} else {
				element.RemoveAttribute("Stocks");
			}

			if (hasBikeStock && bikeStock) {
				stocksAttr.Value = "Bike";

				XmlAttribute allowedAttr = node.Attributes["AllowedSpawns"];
				if (allowedAttr == null) {
					allowedAttr = node.OwnerDocument.CreateAttribute("AllowedSpawns");
					node.Attributes.Append(allowedAttr);
					allowedAttr.Value = "BikeScene";
				} else if (!allowedAttr.Value.Contains("BikeScene")) {
					allowedAttr.Value = allowedAttr.Value.Trim(Vectorier.Core.Game.AttributeSeparator.ToCharArray())
						+ Vectorier.Core.Game.AttributeSeparator
						+ "BikeScene";
				}
			} else if (isHelper) {
				node.Attributes.Remove(stocksAttr);
			}

			if (ai.HasValue) {
				XmlAttribute aiAttr = node.Attributes["AI"] ?? node.OwnerDocument.CreateAttribute("AI");

				if (aiAttr.ParentNode == null) {
					node.Attributes.Append(aiAttr);
				}

				aiAttr.Value = ai.Value.ToString();
			}

			if (icon.HasValue) {
				XmlAttribute iconAttr = node.Attributes["Icon"] ?? node.OwnerDocument.CreateAttribute("Icon");

				if (iconAttr.ParentNode != null) {
					node.Attributes.Append(iconAttr);
				}

				iconAttr.Value = icon.Value ? "1" : "0";

				if (iconAttr.Value == "0") {
					element.RemoveAttribute("Icon");
				}
			}

			if (allowTricks) {
				XmlAttribute trickAttr = node.Attributes["Trick"] ?? node.OwnerDocument.CreateAttribute("Trick");

				if (trickAttr.ParentNode == null) {
					node.Attributes.Append(trickAttr);
				}

				trickAttr.Value = "1";
			}

			SetSkins(
				xml,
				node,

				skins,

				isHelper ? "1" : "hunter"
			);
	
			SetModelColor(
				node,
				modelColor
			);
		}

		// --- CONFIGURATORS ---

		// Common Mode
		public static void ConfigurePlayerCM(XmlDocument xml, XmlNode node,
			string modelName, float spawnTime, string spawnName, float lifeTime, Color color, string skins, bool hasBikeStock, bool bikeStock) =>

			ConfigureModel(xml, node, modelName, spawnTime, spawnName, lifeTime, color, skins, hasBikeStock, bikeStock);

		public static void ConfigureHunterCM(XmlDocument xml, XmlNode node,
			string modelName, float spawnTime, string spawnName, float lifeTime, Color color, string skins,

			bool hasBikeStock, bool bikeStock, bool allowTricks, string allowedSpawns, int ai, bool icon) =>

			ConfigureModel(xml, node, modelName, spawnTime, spawnName, lifeTime, color, skins, hasBikeStock, bikeStock, allowTricks, allowedSpawns, ai, icon);

		// Helper – Common Mode
		public static void ConfigureHelperCM(XmlDocument xml, XmlNode node, string modelName, float spawnTime, string spawnName, float lifeTime, Color color, string skins,
			bool hasBikeStock, bool bikeStock, bool spawnEnabled, string allowedSpawns) {

			if (!spawnEnabled) {
				node.ParentNode.RemoveChild(node);
				return;
			}

			ConfigureModel(xml, node, modelName, spawnTime, spawnName, lifeTime, color, skins, hasBikeStock, bikeStock, false, allowedSpawns, null, null, true);
		}

		// Player – Hunter Mode
		public static void ConfigurePlayerHM(XmlDocument xml, XmlNode node, string modelName, float spawnTime, float spawnIncrement, string spawnName, float lifeTime, Color color, string skins, bool hasBikeStock, bool bikeStock) {
			float totalSpawn = spawnTime <= -1f ? spawnTime : spawnTime + spawnIncrement;

			ConfigureModel(xml, node, modelName, totalSpawn, spawnName, lifeTime, color, skins, hasBikeStock, bikeStock);
		}

		// Hunter – Hunter Mode
		public static void ConfigureHunterHM(XmlDocument xml, XmlNode node, string modelName, float baseSpawnTime, float spawnIncrement, string spawnName, float lifeTime, Color color, string skins, bool hasBikeStock, bool bikeStock, string allowedSpawns) {
			float totalSpawn = baseSpawnTime <= -1f ? baseSpawnTime : baseSpawnTime + spawnIncrement;

			ConfigureModel(xml, node, modelName, totalSpawn, spawnName, lifeTime, color, skins, hasBikeStock, bikeStock, false, allowedSpawns);
		}

		// Helper – Hunter Mode
		public static void ConfigureHelperHM(XmlDocument xml, XmlNode node, string modelName, float baseSpawnTime, string spawnName, float lifeTime, Color color, string skins, bool hasBikeStock, bool bikeStock, bool spawnEnabled, string allowedSpawns, float spawnIncrement) {

			if (!spawnEnabled) {
				node.ParentNode.RemoveChild(node);
				return;
			}

			float totalSpawn = baseSpawnTime <= -1f ? baseSpawnTime : baseSpawnTime + spawnIncrement;

			ConfigureModel(xml, node, modelName, totalSpawn, spawnName, lifeTime, color, skins, hasBikeStock, bikeStock, false, allowedSpawns, null, null, true);
		}

		// --- APPLY DEFAULT PROPERTIES ---

		public static void ApplyDefaultModelProperties(
			XmlDocument xml, XmlNode rootNode,

			string playerModelName, float playerSpawnTime, string playerSpawnName, float playerLifeTime, Color playerModelColor, string playerSkins, bool playerHasBikeStock, bool playerBikeStock,

			string hunterModelName, float hunterSpawnTime, string hunterSpawnName, float hunterLifeTime, Color hunterModelColor, string hunterSkins, bool hunterHasBikeStock, bool hunterBikeStock, bool hunterAllowTricks, string hunterAllowedSpawns, int hunterAIType, bool hunterIcon,

			string helperModelName, float helperSpawnTime, string helperSpawnName, float helperLifeTime, Color helperModelColor, string helperSkins, bool helperHasBikeStock, bool helperBikeStock, bool helperSpawnEnabled, string helperAllowedSpawns) {

			foreach (XmlNode modelsNode in rootNode.SelectNodes("Models")) {
				var variantAttr = modelsNode.Attributes["Variant"]?.Value;

				if (!string.IsNullOrEmpty(variantAttr) && variantAttr != "CommonMode") {
					continue;
				}

				foreach (XmlNode modelNode in modelsNode.ChildNodes) {
					if (modelNode.NodeType != XmlNodeType.Element) {
						continue;
					}

					string modelName = modelNode.Attributes["Name"]?.Value;
					if (string.IsNullOrEmpty(modelName)) {
						continue;
					}

					switch (modelName) {
						case "Player":
						case var name when name == playerModelName:
							ConfigurePlayerCM(xml, modelNode, playerModelName, playerSpawnTime, playerSpawnName, playerLifeTime, playerModelColor, playerSkins, playerHasBikeStock, playerBikeStock);
							break;
						case "Hunter":
						case var name when name == hunterModelName:
							ConfigureHunterCM(xml, modelNode, hunterModelName, hunterSpawnTime, hunterSpawnName, hunterLifeTime, hunterModelColor, hunterSkins, hunterHasBikeStock, hunterBikeStock, hunterAllowTricks, hunterAllowedSpawns, hunterAIType, hunterIcon);
							break;
						case "Helper":
						case var name when name == helperModelName:
							ConfigureHelperCM(xml, modelNode, helperModelName, helperSpawnTime, helperSpawnName, helperLifeTime, helperModelColor, helperSkins, helperHasBikeStock, helperBikeStock, helperSpawnEnabled, helperAllowedSpawns);
							break;
					}
				}
			}
		}

		public static void ApplyCustomModelProperties(
			XmlDocument xml,
			XmlNode node,

			string modelProperties,
			string variant = null // null = all variants
		) {
			// always operate from root to ensure proper search
			XmlNode root = xml.DocumentElement ?? node;

			// select all <Models> nodes with a Variant attribute
			var modelsNodes = root.SelectNodes("/Root/Models[@Variant]");
			if (modelsNodes == null || modelsNodes.Count == 0) {
				Debug.LogWarning("No <Models> nodes with a Variant attribute found in XML.");
				return;
			}

			// flatten input
			string cleaned = modelProperties
				.Replace("\r", "")
				.Replace("  ", "\n")
				.Replace("\n", " ")
				.Replace("\t", "")
				.Trim();

			// remove comments safely
			cleaned = System.Text.RegularExpressions.Regex.Replace(
				cleaned,
				"<!--.*?-->",
				string.Empty,
				System.Text.RegularExpressions.RegexOptions.Singleline
			);

			// ensure valid XML root
			if (!cleaned.StartsWith("<x>") && !cleaned.Contains("<x>")) {
				cleaned = $"<x>{cleaned}</x>";
			}

			// parse XML
			XmlDocument tempDoc = new XmlDocument();
			try {
				tempDoc.LoadXml(cleaned);
			} catch (XmlException e) {
				Debug.LogError($"Invalid XML in custom model properties: {e.Message}\n{cleaned}");
				return;
			}

			// apply properties into each <Models Variant="...">
			foreach (XmlNode modelsNode in modelsNodes) {
				if (!string.IsNullOrEmpty(variant) && modelsNode.Attributes["Variant"]?.Value != variant) {
					continue;
				}

				// clear existing child nodes
				while (modelsNode.HasChildNodes) {
					modelsNode.RemoveChild(modelsNode.FirstChild);
				}

				// append imported model nodes
				foreach (XmlNode childNode in tempDoc.DocumentElement.ChildNodes) {
					if (childNode.NodeType != XmlNodeType.Element) continue;
					XmlNode importedNode = xml.ImportNode(childNode, true);
					modelsNode.AppendChild(importedNode);
				}
			}
		}

		// --- APPLY HUNTER MODE ---

		public static void ApplyHunterMode(
			XmlDocument xml, XmlNode rootNode,

			string playerModelName, float playerSpawnTime, string playerSpawnName, float playerLifeTime, Color playerModelColor, string playerSkins, bool playerHasBikeStock, bool playerBikeStock,

			string hunterModelName, float hunterSpawnTime, string hunterSpawnName, float hunterLifeTime, Color hunterModelColor, string hunterSkins, bool hunterHasBikeStock, bool hunterBikeStock, bool hunterAllowTricks, string hunterAllowedSpawns, int hunterAIType, bool hunterIcon,

			string helperModelName, float helperSpawnTime, string helperSpawnName, float helperLifeTime, Color helperModelColor, string helperSkins, bool helperHasBikeStock, bool helperBikeStock, bool helperHM_SpawnEnabled, string helperAllowedSpawns,

			float playerHM_SpawnIncrement, float hunterHM_SpawnIncrement, float helperHM_SpawnIncrement
		) {

			foreach (XmlNode modelsNode in rootNode.SelectNodes("Models")) {
				if (modelsNode.Attributes["Variant"]?.Value != "HunterMode") {
					continue;
				}

				foreach (XmlNode modelNode in modelsNode.ChildNodes) {
					switch (modelNode.Attributes["Name"]?.Value) {
						case "Player":
						case string name when name == playerModelName:
							ConfigurePlayerHM(xml, modelNode, playerModelName, playerSpawnTime, playerHM_SpawnIncrement, playerSpawnName, playerLifeTime, playerModelColor, playerSkins, playerHasBikeStock, playerBikeStock);
							break;

						case "Hunter":
						case string name when name == hunterModelName:
							ConfigureHunterHM(xml, modelNode, hunterModelName, hunterSpawnTime, hunterHM_SpawnIncrement, hunterSpawnName, hunterLifeTime, hunterModelColor, hunterSkins, hunterHasBikeStock, hunterBikeStock, hunterAllowedSpawns);
							break;

						case "Helper":
						case string name when name == helperModelName:
							ConfigureHelperHM(xml, modelNode, helperModelName, helperSpawnTime, helperSpawnName, helperLifeTime, helperModelColor, helperSkins, helperHasBikeStock, helperBikeStock, helperHM_SpawnEnabled, helperAllowedSpawns, helperHM_SpawnIncrement);
							break;
					}
				}
			}
		}

		// --- APPLY COMMON MODE ---

		public static void ApplyCommonMode(
			XmlDocument xml, XmlNode rootNode,

			string playerModelName, float playerSpawnTime, string playerSpawnName, float playerLifeTime, Color playerModelColor, string playerSkins, bool playerHasBikeStock, bool playerBikeStock,

			string hunterModelName, float hunterSpawnTime, string hunterSpawnName, float hunterLifeTime, Color hunterModelColor, string hunterSkins, bool hunterHasBikeStock, bool hunterBikeStock, bool hunterAllowTricks, string hunterAllowedSpawns, int hunterAIType, bool hunterIcon,

			string helperModelName, float helperSpawnTime, string helperSpawnName, float helperLifeTime, Color helperModelColor, string helperSkins, bool helperHasBikeStock, bool helperBikeStock, bool helperSpawnEnabled, string helperAllowedSpawns
		) {

			ApplyDefaultModelProperties(
				xml,
				rootNode,

				playerModelName, playerSpawnTime, playerSpawnName, playerLifeTime, playerModelColor, playerSkins, playerHasBikeStock, playerBikeStock,

				hunterModelName, hunterSpawnTime, hunterSpawnName, hunterLifeTime, hunterModelColor, hunterSkins, hunterHasBikeStock, hunterBikeStock, hunterAllowTricks, hunterAllowedSpawns, hunterAIType, hunterIcon,

				helperModelName, helperSpawnTime, helperSpawnName, helperLifeTime, helperModelColor, helperSkins, helperHasBikeStock, helperBikeStock, helperSpawnEnabled, helperAllowedSpawns
			);
		}

		// --- SET MODELS ENTRY POINT ---

		public static void SetModels(
			XmlDocument xml, XmlNode rootNode,

			string playerModelName, float playerSpawnTime, string playerSpawnName, float playerLifeTime, Color playerModelColor, string playerSkins, bool playerHasBikeStock, bool playerBikeStock,

			string hunterModelName, float hunterSpawnTime, string hunterSpawnName, float hunterLifeTime, Color hunterModelColor, string hunterSkins, bool hunterHasBikeStock, bool hunterBikeStock, bool hunterAllowTricks, string hunterAllowedSpawns, int hunterAIType, bool hunterIcon,

			string helperModelName, float helperSpawnTime, string helperSpawnName, float helperLifeTime, Color helperModelColor, string helperSkins, bool helperHasBikeStock, bool helperBikeStock, bool helperSpawnEnabled, string helperAllowedSpawns,

			float playerHM_SpawnIncrement, float hunterHM_SpawnIncrement, float helperHM_SpawnIncrement,

			bool helperHM_SpawnEnabled,

			bool useCustomProperties, string customProperties_CM, string customProperties_HM
		) {

			if (useCustomProperties) {
				ApplyCustomModelProperties(xml, rootNode.ParentNode, customProperties_CM, "CommonMode");
				ApplyCustomModelProperties(xml, rootNode.ParentNode, customProperties_HM, "HunterMode");
			} else {
				ApplyDefaultModelProperties(
					xml,
					rootNode,

					playerModelName, playerSpawnTime, playerSpawnName, playerLifeTime, playerModelColor, playerSkins, playerHasBikeStock, playerBikeStock,

					hunterModelName, hunterSpawnTime, hunterSpawnName, hunterLifeTime, hunterModelColor, hunterSkins, hunterHasBikeStock, hunterBikeStock, hunterAllowTricks, hunterAllowedSpawns, hunterAIType, hunterIcon,

					helperModelName, helperSpawnTime, helperSpawnName, helperLifeTime, helperModelColor, helperSkins, helperHasBikeStock, helperBikeStock, helperSpawnEnabled, helperAllowedSpawns
				);
			}

			ApplyHunterMode(
				xml,
				rootNode,

				playerModelName, playerSpawnTime, playerSpawnName, playerLifeTime, playerModelColor, playerSkins, playerHasBikeStock, playerBikeStock,

				hunterModelName, hunterSpawnTime, hunterSpawnName, hunterLifeTime, hunterModelColor, hunterSkins, hunterHasBikeStock, hunterBikeStock, hunterAllowTricks, hunterAllowedSpawns, hunterAIType, hunterIcon,

				helperModelName, helperSpawnTime, helperSpawnName, helperLifeTime, helperModelColor, helperSkins, helperHasBikeStock, helperBikeStock, helperHM_SpawnEnabled, helperAllowedSpawns,

				playerHM_SpawnIncrement,
				hunterHM_SpawnIncrement,
				helperHM_SpawnIncrement
			);
		}

		// -=-=-=- //

		public static void SetCoins(
			XmlDocument xml,
			XmlElement rootNode,

			int value
		) {
			int valueMin = 1;
			int valueMax = short.MaxValue;

			int valueClamped = Mathf.Clamp(value, valueMin, valueMax);

			XmlElement coinsElement = Vectorier.Core.XML.Utils.GetOrCreateElement(
				name: "Coins",
				parent: rootNode,
				xml: xml
			);

			XmlAttribute valueAttr = coinsElement.GetAttributeNode("Value");
			if (valueAttr == null) {
				valueAttr = xml.CreateAttribute("Value");
				coinsElement.Attributes.Append(valueAttr);
			}

			valueAttr.Value = Vectorier.Core.Helpers.ToString(valueClamped, 0);

			if (valueClamped > short.MaxValue || valueClamped < valueMin) {
				Debug.LogWarning($"Amount of coins has been truncated to range ({valueMin}, {valueMax}).");
				return;
			}
		}
	}
}