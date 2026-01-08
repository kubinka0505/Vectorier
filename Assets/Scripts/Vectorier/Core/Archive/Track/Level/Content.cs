using UnityEngine;

using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Collections.Generic;

using Debug = Logger.Debug;

// -=-=-=- //

namespace Vectorier.Core.Archive.Track.Level {
	public static class Content {
		public static string GetLevelNumber(
			string value
		) {
			var parts = value.Split('_');
			return parts.Length > 0 ? parts[parts.Length - 1] : string.Empty;
		}

		public static string NormalizeLocationName(
			string value
		) {
			value = value.ToLower();

			if (value.Contains("downtown")) {
				return "DOWNTOWN";
			}

			if (value.Contains("construction")) {
				return "CONSTRUCTION";
			};

			if (value.Contains("techpark")) {
				return "TECHPARK";
			};

			return string.Empty;
		}

		// -=-=-=- //

		public static void Properties(
			GameObject[] gos,

			string level,

			string originalPath,
			string backupPath = null
		) {
			if (backupPath == null) {
				backupPath = Path.Combine(
					Directory.GetParent(originalPath).FullName,
					"_original",
					Path.GetFileName(originalPath)
				);
			}

			File.Delete(originalPath);
			File.Copy(backupPath, originalPath);

			ProcessTrackXML(gos, originalPath, level);
		}

		// -=-=-=- //

		public static void ProcessTrackXML(
			GameObject[] gos,

			string xmlPath,
			string level
		) {
			XmlDocument xmlDoc = new XmlDocument();
			try {
				xmlDoc.Load(xmlPath);
			} catch {
				Debug.LogError($"Failed to load XML file at {xmlPath}");
				return;
			}

			string levelNumber = GetLevelNumber(level);
			string baseGroupName = NormalizeLocationName(level);
			string subGroupName = level.ToLower().Contains("_story") ? "STORY" :
				level.ToLower().Contains("_bonus") ? "BONUS" : "";

			XmlNode locationListNode = xmlDoc.SelectSingleNode("//LocationList");
			if (locationListNode == null) {
				Debug.LogError("No <LocationList> node found in XML.");
				return;
			}

			string[] locationsNames = {
				baseGroupName,
				baseGroupName + "_HUNTER"
			};

			foreach (string locationNameElement in locationsNames) {
				XmlNode locationNode = locationListNode.SelectSingleNode($"//Location[@Name='{locationNameElement}']");
				if (locationNode == null) {
					Debug.LogError($"Location '{locationNameElement}' not found in XML.");
					return;
				}

				XmlNode groupsNode = locationNode.SelectSingleNode("Groups");
				if (groupsNode == null) {
					Debug.LogError("No <Groups> node found.");
					return;
				}

				bool isHunterMode = locationNameElement.Contains("_HUNTER");
				string trackName = $"{baseGroupName}_{subGroupName}_{levelNumber}" + (isHunterMode ? "_HUNTER" : "");

				XmlNode trackNode = groupsNode.SelectSingleNode($".//Track[@Name='{trackName}']");
				if (trackNode == null) {
					Debug.LogError($"Track '{trackName}' not found in XML.");
					return;
				}

				XmlNode node = trackNode.SelectSingleNode("Tricks") ?? xmlDoc.CreateElement("Tricks");
				if (node.ParentNode == null) {
					trackNode.AppendChild(node);
				}

				node.RemoveAll();

				AppendTricks(gos, node);
			}

			SaveXml(xmlDoc, xmlPath);
		}

		public static void AppendTricks(
			GameObject[] gos,

			XmlNode node
		) {
			// HashSets for processed tricks
			HashSet<string> processedSpritesCommon = new HashSet<string>();
			HashSet<string> processedSpritesHunter = new HashSet<string>();

			foreach (GameObject go in gos) {
				// only consider GameObjects whose parent is tagged "Object"
				if (go.transform.parent == null || go.transform.parent.tag != "Object") {
					continue;
				}

				SpriteRenderer spriteRenderer = go.GetComponent<SpriteRenderer>();
				if (spriteRenderer == null || spriteRenderer.sprite == null) {
					continue;
				}

				string spriteName = spriteRenderer.sprite.name;
				if (string.IsNullOrEmpty(spriteName) || !spriteName.Contains("TRACK_")) {
					continue;
				}

				string trickName = string.Join("_", spriteName.Split('_').Skip(1));

				VectorierWriteMode writeMode = go.GetComponent<VectorierWriteMode>();
				string writeModeValue = writeMode?.GetWriteModeValue()?.Trim().ToLower() ?? "any";

				if (writeMode == null || writeModeValue.StartsWith("any")) {
					processedSpritesCommon.Add(trickName);
					processedSpritesHunter.Add(trickName);
				} else {
					if (writeModeValue.StartsWith("common")) {
						processedSpritesCommon.Add(trickName);
					}

					if (writeModeValue.StartsWith("hunter")) {
						processedSpritesHunter.Add(trickName);
					}
				}
			}

			// defaults if empty
			if (processedSpritesCommon.Count < 1) {
				processedSpritesCommon.Add("TRICK_JUMPTUMBLE");
			}

			if (processedSpritesHunter.Count < 1) {
				processedSpritesHunter.Add("TRICK_JUMPTUMBLE");
			}

			// append to XML and log once per unique trick
			HashSet<string> allTricks = new HashSet<string>(processedSpritesCommon);
			allTricks.UnionWith(processedSpritesHunter);

			foreach (string trickName in allTricks) {
				XmlElement trickElement = node.OwnerDocument.CreateElement("Trick");
				trickElement.SetAttribute("Name", trickName);
				node.AppendChild(trickElement);

				bool inCommon = processedSpritesCommon.Contains(trickName);
				bool inHunter = processedSpritesHunter.Contains(trickName);

				string modeText = inCommon && inHunter ? "to both modes" :
					inCommon ? "to common mode" :
					"to hunter mode";

				// Debug.LogSuccess($"Appended trick: \"{trickName}\" ({modeText})");
			}
		}

		// -=-=-=- //

		public static void WriteLevelThumbnail(
			string pathImage,
			string pathScene,

			string level,

			string originalPath,
			string backupPath = null,
			string backupFileFull = null
		) {
			if (string.IsNullOrEmpty(pathScene)) {
				Debug.LogWarning("Scene was not saved, skipping level thumbnail writing.");
				return;
			}

			// default base directories if not provided
			backupPath ??= Path.Combine(originalPath, "_original");

			// build paths
			string levelThumbnailFile = Path.Combine(originalPath, level + ".png");
			string thumbnailImageValue = !string.IsNullOrEmpty(pathImage)
				? Path.Combine(Directory.GetParent(pathScene).FullName, pathImage)
				: null;

			string levelThumbnailFileBackup = backupFileFull ?? Path.Combine(backupPath, level + ".png");

			bool useBackup = false;

			// try to use provided image first
			if (!string.IsNullOrEmpty(thumbnailImageValue) && File.Exists(thumbnailImageValue)) {
				try {
					if (File.Exists(levelThumbnailFile)) {
						File.Delete(levelThumbnailFile);
					}

					File.Copy(thumbnailImageValue, levelThumbnailFile, overwrite: true);
					Debug.Log($"Thumbnail updated from {thumbnailImageValue}");
				} catch (Exception e) {
					Debug.LogError($"Failed to copy custom thumbnail: {e.Message}");
					useBackup = true;
				}
			} else {
				useBackup = true;
			}

			// fallback: copy from backup/original
			if (useBackup) {
				if (!File.Exists(levelThumbnailFileBackup)) {
					Debug.LogWarning($"Backup thumbnail not found: {levelThumbnailFileBackup}");
					return;
				}

				try {
					if (File.Exists(levelThumbnailFile)) {
						File.Delete(levelThumbnailFile);
					}

					File.Copy(levelThumbnailFileBackup, levelThumbnailFile, overwrite: true);
					// Debug.Log($"Restored thumbnail from backup: {levelThumbnailFileBackup}");
				} catch (Exception e) {
					Debug.LogError($"Failed to restore thumbnail: {e.Message}");
				}
			}
		}

		public static void WriteInGameInterface(
			bool write,

			string originalPath,
			string originalFileFull,
			string backupFileEmpty,

			string backupPath = null,
			string backupFileFull = null
		) {
			if (backupPath == null) {
				backupPath = Path.Combine(originalPath, "_original");
			}

			if (backupFileFull == null) {
				backupPath = Path.Combine(
					Directory.GetParent(originalFileFull).FullName,
					"_original",
					Path.GetFileName(originalFileFull)
				);
			}

			File.Delete(originalFileFull);
			File.Copy(write ? backupFileEmpty : backupFileFull,
				Path.Combine(originalPath, Path.GetFileName(originalFileFull))
			);
		}

		public static void WriteLevelName(
			string value,

			string level,

			string originalPath,
			string backupPath = null
		) {
			if (backupPath == null) {
				backupPath = Path.Combine(
					Directory.GetParent(originalPath).FullName,
					"_original",
					Path.GetFileName(originalPath)
				);
			}

			// restore if title empty
			if (string.IsNullOrEmpty(value)) {
				Debug.LogWarning("Empty level name field, restoring from backup...");

				if (File.Exists(backupPath)) {
					File.Delete(originalPath);
					File.Copy(backupPath, originalPath);

					Debug.Log("Localization file restored from backup.");
				} else {
					Debug.LogWarning("Original localization file not found, restore skipped.");
				}

				return;
			}

			// load XML
			if (!File.Exists(originalPath)) {
				Debug.LogError($"Localization file not found: {originalPath}");
				return;
			}

			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.Load(originalPath);

			XmlNode root = xmlDoc.SelectSingleNode("/log");
			if (root == null) {
				Debug.LogError("Invalid localization XML structure - missing <log> root.");
				return;
			}

			string nodeName = $"item_{level}";
			XmlElement node = xmlDoc.SelectSingleNode($"/log/{nodeName}") as XmlElement;

			// create node if missing
			if (node == null) {
				node = xmlDoc.CreateElement(nodeName);
				root.AppendChild(node);
		
				Debug.Log($"Created new localization node: <{nodeName}>");
			}

			// replace all attribute values without clearing
			foreach (XmlAttribute attr in node.Attributes) {
				attr.Value = value;
			}

			// save
			SaveXml(xmlDoc, originalPath);

			// Debug.LogSuccess($"Level name \"{value}\" set for level \"{level}\"");
		}

		// -=-=-=- //

		public static void SaveXml(
			XmlDocument xmlDoc,
			string path
		) {
			XmlWriterSettings settings = new XmlWriterSettings {
				Indent = true,
				IndentChars = "\t",
				NewLineChars = "\n",
				NewLineHandling = NewLineHandling.Replace,
				Encoding = System.Text.Encoding.UTF8,
				OmitXmlDeclaration = true
			};

			using (XmlWriter writer = XmlWriter.Create(path, settings)) {
				xmlDoc.Save(writer);
			}
		}
	}
}