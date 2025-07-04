using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

using Vectorier;

using Debug = UnityEngine.Debug;

// -=-=-=- //

public class BuildMap : MonoBehaviour {
	// ReSharper disable once InconsistentNaming
	internal string vectorFilePath { get; set; }

	void Awake() {
		vectorFilePath = VectorierSettings.GameDirectory;
	}

	public static event Action MapBuilt;

	// Flag to indicate if the build is for running the game
	public static bool buildForRunGame { get; set; } = false;

	// -=-=-=- //
	// Level Settings

	[Header("Level Settings")]

	// -=-=-=- //

	// common_xml.dz/localization_all.xml
	[Tooltip(@"Level name, displayed in the game.

Unchanged if string is empty or none")]
	public string title = "";

	// GUI_2048_1536.dz/{n}.png
	[Tooltip(@"Unity path to level thumbnail, displayed in game.

If path is not found file is not changed.
⚠️ Can't start with a dot (`.`)
⚠️ Relative to the scene file (`.unity`) location!
⚠️ Will throw a warning if given image is not PNG or does not have 512x340 (w:h) dimensions.")]
	public string thumbnailImagePath = "";

	[Tooltip(@"Author of the level.

Optional parameter used in external scripts.")]
	public string authors = "";

	[Header("")]

	[Tooltip(@"Replaces in-game GUI buttons (Restart & Pause) to transparent images.

Idea by Mohammed Taher")]
	public bool transparentInterfaceButtons = false;

	[Tooltip("Decreases the written track size.")]
	public bool optimizeWrittenTrack = true;
	
	[Header("")]

	// -=-=-=- //

	[Tooltip(@"Level that will get overridden.

⚠️ Will throw an error if given XML will not be detected as a level!")]
	public string mapToOverride = "DOWNTOWN_STORY_02";
	// public string mapToOverride = "DOWNTOWN_STORY_01";

	[Tooltip("Music that will be played on the level.")]
	public string levelMusic = "music_dinamic";

	[Tooltip("Volume of the music.")]
	public string MusicVolume = "0.3";

	[Tooltip(@"Background image.

If empty, inherited from ""customBackgroundMirror"" variable.

⚠️ Has to be located inside “track_content_2048.dz” file archive base!")]
	public string customBackground = "v_bg";

	[Tooltip(@"Background image for left side. (even nodes)

If empty, inherited from ""customBackground"" variable.

⚠️ Has to be located inside “track_content_2048.dz” file archive base!")]
	public string customBackgroundMirror = "";

	[Tooltip("Background width.")]
	public float backgroundWidth = 2121f;

	[Tooltip("Background height.")]
	public float backgroundHeight = 1116f;

	[Tooltip(@"Background horizontal position + 1120. (offset)

⚠️ Affects mobile gameplay, use with caution.")]
	public float backgroundXPosition = 0f;

	[Tooltip(@"Background vertical position.

⚠️ Affects mobile gameplay, use with caution.")]
	public float backgroundYPosition = 0f;

	// Gameplay
	[Serializable]
	public class PlayerSettings {
		public string playerModelName = "Player";

		[Tooltip("Player's spawn name.")]
		public string playerSpawnName = "PlayerSpawn";

		[Tooltip("Duration until the player appears, in seconds.")]
		public float playerSpawnTime;

		[Tooltip("Time after player disappears after death (Default: 3)")]
		public float playerLifeTime = 3;

		[Tooltip(@"Player Appearance (default: “1”)

Accepts multiline entries, pipe characters are treated as newlines, then newlines are treated like pipes.

Lines starting with “>” mean comments. Empty lines are removed.")]
		[TextArea(3, 8)]
		public string playerSkins = "1";

		public Color modelColor = Color.black;
	}

	[Serializable]
	public class HunterSettings {
		public string hunterModelName = "Hunter";

		[Tooltip("Hunter's spawn name.")]
		public string hunterSpawnName = "DefaultSpawn";

		[Tooltip("Duration until the hunter appears, in seconds.")]
		public float hunterSpawnTime = 2f;

		[Tooltip("Time after hunter disappears after death (Default: 3)")]
		public float hunterLifeTime = 3;

		[Tooltip(@"Time (ms) until hunter arrests the player when $Node variable content node is located inside the `TriggerCatch` or `TriggerCatchFront` objects (Default: 300)

⚠️ Not applicable to `TriggerCatchFast`.")]
		public int hunterCatchDistance = 300;

		[Tooltip("Hunter respawn name.")]
		public string hunterAllowedSpawn = "Respawn";

		[Tooltip(@"Hunter model appearance (default: “hunter”)

Accepts multiline entries, pipe characters are treated as newlines, then newlines are treated like pipes.

Lines starting with “>” mean comments. Empty lines are removed.")]
		[TextArea(3, 8)]
		public string hunterSkins = "hunter";

		public Color modelColor = Color.black;

		[Tooltip("Determines whether hunter is able do to tricks.")]
		public bool hunterTrickAllowed;

		[Tooltip("Determines whether hunter's icon is shown.")]
		public bool hunterIcon = true;

		[Tooltip("Hunter AI type number (Default: 1)")]
		public int hunterAIType = 1;
	}

	[Serializable]
	public class HelperSettings {
		public string helperModelName = "Helper";

		[Tooltip("Helper's spawn name.")]
		public string helperSpawnName = "HelperSpawn";

		[Tooltip("Duration until the helper appears, in seconds.")]
		public float helperSpawnTime = 99999;

		[Tooltip("Time after player disappears after death (Default: 3)")]
		public float helperLifeTime = 3;

		[Tooltip(@"Helper Appearance (default: “1”)

Accepts multiline entries, pipe characters are treated as newlines, then newlines are treated like pipes.

Lines starting with “>” mean comments. Empty lines are removed.")]
		[TextArea(3, 8)]
		public string helperSkins = "helper\n\nshirt\ncap";

		public Color modelColor = Color.black;

		[Tooltip("Determines whether helper should be spawned.")]
		public bool spawn = false;
	}

	// -=-=-=- //
	// Hunter Mode

	[Serializable]
	public class PlayerSettings_HunterMode {
		[Tooltip(@"Duration until the player appears on the map, in seconds.

⚠️ If `-1`, inherited from Common Mode settings and increased by `playerSpawnTimeIncrement` variable.")]
		public float playerSpawnTime = -1.0f;

		[Tooltip(@"If the `playerSpawnTime` variable meets the requirements to be inherited from the Common Mode, the contents of this variable will be added to it.

⚠️ If the result of `playerSpawnTime` + `playerSpawnTimeIncrement` equation will be lower, an error will be thrown.")]
		public float playerSpawnTimeIncrement;

		[Tooltip("Hunter respawn name.")]
		public string playerAllowedSpawn = "PlayerSpawn";
	}

	[Serializable]
	public class HunterSettings_HunterMode {
		[Tooltip(@"Duration until the hunter appears on the map, in seconds.

⚠️ If `-1`, inherited from Common Mode settings and increased by `hunterSpawnTimeIncrement` variable.")]
		public float hunterSpawnTime = -1.0f;

		[Tooltip(@"If the `hunterSpawnTime` variable meets the requirements to be inherited from the Common Mode, the contents of this variable will be added to it.

⚠️ If the result of `hunterSpawnTime` + `hunterSpawnTimeIncrement` equation will be lower, an error will be thrown.")]
		public float hunterSpawnTimeIncrement;

		[Tooltip("Hunter respawn name.")]
		public string hunterAllowedSpawn = "DefaultSpawn";
	}

	[Serializable]
	public class HelperSettings_HunterMode {
		[Tooltip(@"Duration until the helper appears on the map, in seconds.

⚠️ If `-1`, inherited from Common Mode settings and increased by `helperSpawnTimeIncrement` variable.")]
		public float helperSpawnTime = -1.0f;

		[Tooltip(@"If the `helperSpawnTime` variable meets the requirements to be inherited from the Common Mode, the contents of this variable will be added to it.

⚠️ If the result of `helperSpawnTime` + `helperSpawnTimeIncrement` equation will be lower, an error will be thrown.")]
		public float helperSpawnTimeIncrement;

		[Tooltip("Helper respawn name.")]
		public string helperAllowedSpawn = "HelperSpawn";

		[Tooltip("Determines whether helper should be spawned.")]
		public bool spawn = false;
	}

	[Header("Gameplay (Hunter Mode)")]

	[SerializeField]
	private PlayerSettings_HunterMode PlayerHM;

	[SerializeField]
	private HunterSettings_HunterMode HunterHM;

	[SerializeField]
	private HelperSettings_HunterMode HelperHM;

	// -=-=-=- //

	[Header("Gameplay (Common Mode)")]
	[SerializeField]
	private PlayerSettings Player;

	[SerializeField]
	private HunterSettings Hunter;

	[SerializeField]
	private HelperSettings Helper;

	// Miscellaneous
	[Header("Miscellaneous")]

	[Tooltip(@"Uses custom properties instead of prefixed

⚠️ Ignores the above settings for player and hunter!")]
	public bool useCustomProperties;

	[TextArea(5, 20)]
	public string CustomModelProperties = @"<Model
	Name=""Player""
	Type=""1""
	Color=""0""
	BirthSpawn=""PlayerSpawn""
	AI=""0""
	Time=""0""
	Skins=""1""
	Respawns=""Hunter""
	ForceBlasts=""Hunter""
	Trick=""1""
	Item=""1""
	Victory=""1""
	Lose=""1""
	LifeTime=""3""
/>

<Model
	Name=""Hunter""
	Type=""0""
	Color=""0""
	BirthSpawn=""DefaultSpawn""
	AI=""1""
	Time=""0.8""
	AllowedSpawns=""Respawn""
	Skins=""hunter""
	Murders=""Player|Helper""
	Arrests=""Player""
	Icon=""1""
	LifeTime=""3""
/>

<!-- Uncomment those lines to add more models --/>

<!-- Model
	Name=""Hunter2""
	Type=""0""
	Color=""0""
	BirthSpawn=""DefaultSpawn""
	AI=""2""
	Time=""0.8""
	AllowedSpawns=""Respawn""
	Skins=""hunter""
	Murders=""Player|Helper""
	Arrests=""Player""
	Icon=""1""
	LifeTime=""3""
--/>

<!--Model
	Name=""Helper""
	Type=""0""
	Color=""0""
	BirthSpawn=""HelperSpawn""
	AI=""3""
	Time=""0.3""
	AllowedSpawns=""RespawnHelper""
	Skins=""revolution_girl""
	Trick=""0""
	Item=""0""
	Victory=""0""
	Lose=""0""
	LifeTime=""3""
--/>";

	[Header("Miscellaneous")]

	[Tooltip("Outputs objects writing to console while building the map.")]
	public bool logObjectWriting;
	public bool hunterPlaced;

	[Tooltip("Divide GameObject's position by it's layer object factor.")]
	public bool correctFactorPosition = true;

	// -=-=-=- //
	// Menu Items

	[MenuItem("Vectorier/BuildMap")]
	public static void BuildDZ() {
		Build(true, true);
	}

	[MenuItem("Vectorier/BuildMap (Fast) #&B")]
	public static void BuildZlib() {
		// Build(true, true);
		Build(false, true);
	}

	[MenuItem("Vectorier/BuildMap XML Only")]
	public static void BuildXml() {
		Build(false, false);
	}

	// -=-=-=- //
	// Variables
	public string globalRegex = @" ?\((.*?)\)| ?\[.*?\]";

	// -=-=-=- //
	// Functions

	#if UNITY_EDITOR

	public bool IsVisible(GameObject obj) {
		return !obj.CompareTag("EditorOnly") && 
			!obj.CompareTag("Unused") && 
			!SceneVisibilityManager.instance.IsHidden(obj) && 
			obj.activeInHierarchy;
	}

	#endif

	public void xmlEdit(string path, string rootNode, string subNode, string attributeName, string attributeValue) {
		// Load the XML document from the specified file path
		XmlDocument xmlDoc = new XmlDocument();
		
		try {
			xmlDoc.Load(path);
		} catch (System.Exception e) {
			Debug.LogError($"Failed to load XML file at {path}: {e.Message}");
			return;
		}

		// Find the root node
		XmlNode root = xmlDoc.SelectSingleNode($"/{rootNode}");
		if (root == null) {
			Debug.LogError($"Root node '{rootNode}' not found in XML.");
			return;
		}

		// Find the subnode within the root node
		XmlNode targetNode = root.SelectSingleNode(subNode);
		if (targetNode == null) {
			Debug.LogError($"Subnode '{subNode}' not found in root node '{rootNode}'.");
			return;
		}

		// Update attribute(s) based on the provided attributeName
		if (attributeName == "*") {
			// Modify all attributes of the subnode
			if (targetNode.Attributes != null) {
				foreach (XmlAttribute attr in targetNode.Attributes) {
					attr.Value = attributeValue;
				}
			} else {
				Debug.LogWarning($"No attributes found in node '{subNode}'.");
			}
		} else {
			// Modify the specific attribute if it exists
			XmlAttribute attribute = targetNode.Attributes[attributeName];
			if (attribute != null) {
				attribute.Value = attributeValue;
			} else {
				Debug.LogError($"Attribute '{attributeName}' not found in node '{subNode}'.");
				return;
			}
		}

		// Save the modified XML document back to the file
		try {
			xmlDoc.Save(path);
		} catch (System.Exception e) {
			Debug.LogError($"Failed to save XML file at {path}: {e.Message}");
		}
	}

	public string BytesToString(long byteCount, CultureInfo culture = null) {
		// Default culture is the current culture
		culture ??= CultureInfo.CurrentCulture;

		string[] suf = {"B", "KB", "MB", "GB", "TB", "PB", "EB"};

		if (byteCount == 0) {
			return "0 " + suf[0];
		}

		long bytes = Math.Abs(byteCount);
		int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
		double num = bytes / Math.Pow(1024, place);

		string formattedNumber = num.ToString("N2", culture);
		return (Math.Sign(byteCount) * num).ToString("N2", culture) + " " + suf[place];
	}

	public object ParseSkins(string SkinsInput, bool list = false) {
		// Split input by newline and remove any carriage returns
		List<string> SkinsList = SkinsInput
			.Replace(Path.DirectorySeparatorChar, '/')
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

	static string GetLevelNumber(string levelName) {
		var parts = levelName.Split('_');
		// Check if the array has at least one element, then get the last element
		return parts.Length > 0 ? parts[parts.Length - 1] : string.Empty;
	}

	// -=-=-=- //
	// Main

	public static void Build(bool useDZ, bool compileMap) {
		// This is used to cache the BuildMap component. This is done to avoid the FindObjectOfType method in loop and other places.
		// This is a slow operation.
		var buildMap = FindObjectOfType<BuildMap>();
		int validationErrors = 0;
		int maxValue = Int16.MaxValue;

		if (buildMap == null || buildMap.mapToOverride == null) {
			Debug.LogError("No GameObject with map building script attached has been found!");
			validationErrors++;
			return;
		}

		// -=-=-=- //
		// Variables
		string buildMapXML = Path.Combine(Application.dataPath, "XML", "build-map.xml");
		string mapEmptyXML = Path.Combine(Application.dataPath, "XML", "empty-map-DONT-MODIFY.xml");
		string writtenTrackPath = Path.Combine(Application.dataPath, "XML", "dzip", "level_xml", buildMap.mapToOverride + ".xml");

		// Find BuildMap
		string bmNotFoundMsg = "";

		if (buildMap == null) {
			// Check if buildMap is null first
			bmNotFoundMsg = @"Couldn't compile a track - No map building script instance was found in any GameObject";
		} else if (!buildMap.IsVisible(buildMap.gameObject)) {
			// Then check visibility
			bmNotFoundMsg = @"Couldn't compile a track - GameObject containing map building script was set to inactive";
		}

		if (!string.IsNullOrEmpty(bmNotFoundMsg)) {
			Debug.LogError(bmNotFoundMsg);
			validationErrors++;
		}

		// -=-=-=- //
		// Validators

		if (VectorierSettings.ValidateScene) {
			// Track name
			string[] trackNameValidators = { "STORY", "BONUS" };
			string trackToOverrideName = Path.GetFileNameWithoutExtension(buildMap.mapToOverride);
			
			foreach (string elem in trackNameValidators) {
				string[] parts = trackToOverrideName.Split('_');
				string secondLast = parts[parts.Length - 2];

				if (secondLast.ToUpper() == elem) {
					break;
				} else {
					Debug.LogError($@"Map was not classified as a level (""{trackToOverrideName}"")", buildMap.gameObject);
					validationErrors++;
				}
			}

			// Level music
			try {
				buildMap.levelMusic = buildMap.levelMusic.Substring(
					(VectorierSettings.GameDirectory + Path.DirectorySeparatorChar).Length
				);

				buildMap.levelMusic = buildMap.levelMusic.Trim('"');
				buildMap.levelMusic = buildMap.levelMusic.Replace(Path.DirectorySeparatorChar, '/').Trim('/');
				buildMap.levelMusic = Path.ChangeExtension(buildMap.levelMusic, null); // remove extension
			} catch {
				// pass
			}

			if (!string.IsNullOrEmpty(buildMap.levelMusic)) {
				// Combine the game directory and level music path to form a full path
				string musicPathFull = Path.Combine(VectorierSettings.GameDirectory, buildMap.levelMusic + ".mp3");

				// Check if the file exists in the game directory
				if (!File.Exists(musicPathFull)) {
					Debug.LogError("Level music file was not found in the game directory");
					validationErrors++;
				} else {
					// Check if the level music file has an extension (it should not if it's supposed to be a directory)
					if (Path.HasExtension(buildMap.levelMusic)) {
						string[] levelMusicExtensions = buildMap.levelMusic.Split('.');
						string levelMusicWithoutLastExtension = string.Join(".", levelMusicExtensions.Take(levelMusicExtensions.Length - 1));

						// Check if the original filename has an extension, but it shouldn't
						if (buildMap.levelMusic != levelMusicWithoutLastExtension) {
							Debug.LogError("Level music file exists but has an extension");
							validationErrors++;
						}
					}

					// Check if the path is absolute (and not relative to the game directory)
					if (Path.IsPathRooted(buildMap.levelMusic) && buildMap.levelMusic[1] == ':') {
						Debug.LogError("Level music is an absolute operating system path, not relative to the game directory");
						validationErrors++;
					}
				}
			} else {
				Debug.LogError("Level music was not provided, level will be silent");
			}

			// Music volume (deprecated)
			if (float.Parse(buildMap.MusicVolume, CultureInfo.InvariantCulture) < 0) {
				Debug.LogError("Music volume is not positive");
				validationErrors++;
			}

			// Level backgrounds
			string[] levelBackgrounds = { buildMap.customBackground, buildMap.customBackgroundMirror };
			foreach (var bg in levelBackgrounds) {
				if (!string.IsNullOrEmpty(bg)) {
					if (Path.HasExtension(bg)) {
						Debug.LogWarning($@"Despite possible file existence, custom background mirror ""{bg}"" is not an image file basename without extension");
					} else if (bg.Replace(Path.DirectorySeparatorChar, '/').Contains("/")) {
						Debug.LogError($@"Custom background mirror ""{bg}"" is a directory-structured path, not an image file basename without extension");
						validationErrors++;
					}
				}
			}

			// -=-=-=-//
			// Player settings

			// Player Model Name
			if (string.IsNullOrEmpty(buildMap.Player.playerModelName)) {
				Debug.LogError("Player model name is empty");
				validationErrors++;
			}

			if (string.IsNullOrEmpty(buildMap.Player.playerSpawnName)) {
				Debug.LogError("Initial spawn point for Player model was not provided");
			}

			// Player Spawn Time
			if (buildMap.Player.playerSpawnTime < 0) {
				Debug.LogError("Player model spawn time is not positive");
				validationErrors++;
			}

			// Player Life Time
			if (buildMap.Player.playerLifeTime < 0) {
				Debug.LogWarning($"Player model spawn time is not positive, it will be treated as {maxValue}");
				buildMap.Player.playerLifeTime = maxValue;
			}

			// Player Skins
			if (string.IsNullOrEmpty(buildMap.Player.playerSkins)) {
				Debug.LogWarning("No skins for Player model has been input, model will be invisible");
			}

			// -=-=-=- //
			// Hunter settings

			if (string.IsNullOrEmpty(buildMap.Hunter.hunterSpawnName)) {
				Debug.LogError("Initial spawn point for Hunter model was not provided");
				validationErrors++;
			}

			// Hunter Model Name
			if (string.IsNullOrEmpty(buildMap.Hunter.hunterModelName)) {
				Debug.LogError("Hunter model name is empty");
				validationErrors++;
			}

			// Hunter Spawn Time
			if (buildMap.Hunter.hunterSpawnTime < 0) {
				Debug.LogError("Hunter model spawn time is not positive");
				validationErrors++;
			}

			// Hunter Life time
			if (buildMap.Hunter.hunterLifeTime < 0) {
				Debug.LogWarning($"Hunter model spawn time is not positive, it will be treated as {maxValue}");
				buildMap.Hunter.hunterLifeTime = maxValue;
			}

			// Hunter Catch Distance
			int minDistanceCatch = 200;
			if (buildMap.Hunter.hunterCatchDistance < minDistanceCatch) {
				Debug.LogWarning($"Hunter model catch distance being below {minDistanceCatch} units may be considered too invasive");
			}

			// Hunter Allowed Spawns
			if (buildMap.Hunter.hunterAllowedSpawn.Split('|').Length < 1) {
				Debug.LogError("Hunter model has no spawnpoints");
				validationErrors++;
			}

			if (string.IsNullOrEmpty(buildMap.Hunter.hunterSkins)) {
				Debug.LogWarning("No skins for Hunter model has been input, model will be invisible");
			}

			if (buildMap.Hunter.hunterAIType < 0) {
				validationErrors++;
			}

			// -=-=-=- //

			if (validationErrors > 0) {
				// Debug.Log($"Found {validationErrors} found.");
				return;
			}
		}

		// Save scene before build map
		if (VectorierSettings.SaveSceneBeforeBuildMap) {
			if (!string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) {
				EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
			}
		}

		if (string.IsNullOrEmpty(buildMap.vectorFilePath)) {
			buildMap.vectorFilePath = VectorierSettings.GameDirectory;
		}

		// Start the stopwatch
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();

		GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>()
			.OrderBy(obj => obj.name)
			.OrderBy(obj => obj.transform.position.y)
			.OrderBy(obj => obj.transform.position.x)
			.ToArray();

		// Get all GameObjects
		GameObject[] objectsInScene = allObjects
			.Where(obj => obj.CompareTag("Object"))
			.ToArray();

		// ...with tag "Image", then arrange them based on sorting order
		GameObject[] imagesInScene = allObjects
			.Where(obj => obj.CompareTag("Image"))
			.OrderBy(obj => obj.GetComponent<SpriteRenderer>().sortingOrder)
			.ToArray();

		foreach (GameObject go in allObjects) {
			// Scan incorrectly-named textures
			if (go.CompareTag("Image")) {
				string objRegex = Regex.Replace(go.name, buildMap.globalRegex, string.Empty);

				SpriteRenderer sr = go.GetComponent<SpriteRenderer>();

				try {
					if (objRegex != sr.sprite.name) {
						string fullPath = GetGameObjectPath(go);

						string wrongSpriteNameMsg = $@"GameObject named ""{go.name}"" doesn't match its SpriteRenderer sprite name (""{sr.sprite.name}"") [click to toggle]";
						wrongSpriteNameMsg += "\n\n" + fullPath + "\n";

						Debug.LogWarning(wrongSpriteNameMsg, go);
					}
				} catch {
					string fullPath = GetGameObjectPath(go);

					string wrongSpriteNameMsg = $@"GameObject named ""{go.name}"" has SpriteRenderer component attached, but without texture [click to toggle]";
					wrongSpriteNameMsg += "\n\n" + fullPath + "\n";

					Debug.LogError(wrongSpriteNameMsg, go);
					return;
				}
			}

			// Find dynamic missing tag
			if (go.GetComponent<Dynamic>() && !go.CompareTag("Dynamic")) {
				string fullPathDynamic = GetGameObjectPath(go);

				string wrongDynamicNameMsg = $@"GameObject named ""{go.name}"" has ""Dynamic"" component but no dynamic tag set. [click to toggle]";
				wrongDynamicNameMsg += "\n\n" + fullPathDynamic + "\n";

				Debug.LogError(wrongDynamicNameMsg, go);
				//return;
			}
		}

		string GetGameObjectPath(GameObject obj) {
			// Get the scene name for the root object
			string sceneName = Path.GetFileName(obj.scene.path);

			if (string.IsNullOrEmpty(sceneName)) {
				sceneName = "Current scene";
			}

			string sep = " → ";

			if (obj.transform.parent == null) {
				// Root object, include the scene name
				return $"{sceneName}{sep}{obj.name}";
			}

			Transform parent = obj.transform.parent;

			// Determine the name-based index of the object among siblings with the same name
			int nameBasedIndex = 0;
			foreach (Transform sibling in parent) {
				if (sibling == obj.transform) {
					// Found the object; stop counting
					break;
				}

				if (sibling.name == obj.name) {
					nameBasedIndex++;
				}
			}

			// Check if there are other siblings with the same name
			bool hasDuplicateNames = false;
			foreach (Transform sibling in parent) {
				if (sibling != obj.transform && sibling.name == obj.name) {
					hasDuplicateNames = true;
					break;
				}
			}

			// Recursively get the parent's path
			string parentPath = GetGameObjectPath(parent.gameObject);

			// Only append the index if duplicates exist
			string currentSegment = hasDuplicateNames ? $"{obj.name}[{nameBasedIndex}]" : obj.name;

			return $"{parentPath}{sep}{currentSegment}";
		}

		Debug.Log("Building...");

		// -=-=-=- //
		// Edit level properties

		// PC
		string listPayedXml = Path.Combine("Assets", "XML", "dzip", ".common_xml", "List_Payed.xml");
		string listPayedXmlOriginal = Path.Combine(
			Directory.GetParent(listPayedXml).FullName,
			"_original",
			Path.GetFileName(listPayedXml)
		);

		File.Delete(listPayedXml);
		File.Copy(listPayedXmlOriginal, listPayedXml);

		// Android
		string listPayedXml_Mobile = Path.Combine("Assets", "XML", "dzip", ".common_xml", "list_paid_mob.xml");
		string listPayedXmlOriginal_Mobile = Path.Combine(
			Directory.GetParent(listPayedXml_Mobile).FullName,
			"_original",
			Path.GetFileName(listPayedXml)
		);

		File.Delete(listPayedXml_Mobile);
		File.Copy(listPayedXmlOriginal_Mobile, listPayedXml_Mobile);

		// Load the List_Payed.xml file
		// - Mobile is equivalent
		string[] filesToProcess = { listPayedXml, listPayedXml_Mobile };

		bool notifiedTrick = false;

		foreach (string fileToProcess in filesToProcess) {
			XmlDocument xmlDoc = new XmlDocument();
			try {
				xmlDoc.Load(fileToProcess);
			} catch (Exception e) {
				Debug.LogError($"Failed to load XML file at {fileToProcess}: {e.Message}");
				return;
			}

			// Extract level number from mapToOverride (e.g., "DOWNTOWN_STORY_02" -> "02")
			string levelNumber = GetLevelNumber(buildMap.mapToOverride);

			// Determine the base group based on mapToOverride
			string baseGroupName = buildMap.mapToOverride.ToLower() switch {
				string s when s.Contains("downtown") => "DOWNTOWN",
				string s when s.Contains("construction") => "CONSTRUCTION",
				string s when s.Contains("techpark") => "TECHPARK",
				_ => ""
			};

			// Select the root <LocationList> node
			XmlNode locationListNode = xmlDoc.SelectSingleNode("// LocationList");
			if (locationListNode == null) {
				Debug.LogError("No <LocationList> node found in XML.");
				return;
			}

			// Determine the subgroup based on map type (STORY, BONUS, etc.)
			string subGroupName = buildMap.mapToOverride.ToLower().Contains("_story") ? "STORY" :
				buildMap.mapToOverride.ToLower().Contains("_bonus") ? "BONUS" : "";

			string[] locationsNames = { baseGroupName, baseGroupName + "_HUNTER" };

			// -=-=-=- //
			// Write tricks depending on mode

			foreach (string locationNameElement in locationsNames) {
				// Search for the specific location
				XmlNode locationNode = locationListNode.SelectSingleNode($"// Location[@Name='{locationNameElement}']");
				if (locationNode == null) {
					Debug.LogError($"Location '{locationNameElement}' not found in XML.");
					return;
				}

				// Navigate to the <Groups> node
				XmlNode groupsNode = locationNode.SelectSingleNode("// Groups");
				if (groupsNode == null) {
					Debug.LogError("No <Groups> node found.");
					return;
				}

				// Navigate to the <Group Name="..."> node
				XmlNode groupNode = groupsNode.SelectSingleNode($"// Group[@Name='{subGroupName}']");

				// Determine the mode
				bool isHunterMode = locationNameElement.Contains("_HUNTER");

				// Build the track name
				string trackName = $"{baseGroupName}_{subGroupName}_{levelNumber}" + (isHunterMode ? "_HUNTER" : "");

				// Select the <Track> node
				XmlNode trackNode = groupsNode.SelectSingleNode($"// Track[@Name='{trackName}']");
				if (trackNode == null) {
					Debug.LogError($"Track '{trackName}' not found in XML.");
					return;
				}

				// Navigate or create the <Tricks> node
				XmlNode tricksNode = trackNode.SelectSingleNode("Tricks") ?? xmlDoc.CreateElement("Tricks");

				if (tricksNode.ParentNode == null) {
					trackNode.AppendChild(tricksNode);
				} else {
					// Clear current contents if it exists
					tricksNode.RemoveAll();
				}

				// Initialize HashSets
				HashSet<string> processedSpritesCommon = new HashSet<string>();
				HashSet<string> processedSpritesHunter = new HashSet<string>();

				// Collect and process all GameObjects with SpriteRenderer
				foreach (GameObject go in allObjects) {
					if (!buildMap.IsVisible(go)) {
						continue;
					}

					SpriteRenderer spriteRenderer = go.GetComponent<SpriteRenderer>();
					if (spriteRenderer == null) {
						continue;
					}

					string spriteName = spriteRenderer.sprite?.name;
					if (string.IsNullOrEmpty(spriteName) || !spriteName.Contains("TRACK_")) {
						continue;
					}

					// Extract the trick name
					string trickName = string.Join("_", spriteName.Split('_').Skip(1));

					// Determine the write mode value
					VectorierWriteMode writeMode = go.GetComponent<VectorierWriteMode>();
					string writeModeValue = writeMode?.GetWriteModeValue()?.Trim().ToLower() ?? "any";

					// Add trickName to appropriate HashSet
					
					// bugged, wontfix
					if (writeMode == null || writeModeValue.StartsWith("any")) {
						processedSpritesCommon.Add(trickName);
						processedSpritesHunter.Add(trickName);
					}

					if (writeModeValue.StartsWith("common")) {
						processedSpritesCommon.Add(trickName);
					}

					if (writeModeValue.StartsWith("hunter")) {
						processedSpritesHunter.Add(trickName);
					}
				}

				// Ensure default tricks are added if no tricks detected
				if (processedSpritesCommon.Count < 1) {
					processedSpritesCommon.Add("TRICK_JUMPTUMBLE");
					if (!notifiedTrick) {
						Debug.Log("Common mode track has no tricks detected, defaulting to Jump Tumble.", buildMap.gameObject);
					}
				}

				if (processedSpritesHunter.Count < 1) {
					processedSpritesHunter.Add("TRICK_JUMPTUMBLE");
					/*
					if (!notifiedTrick) {
						Debug.Log("Hunter mode track has no tricks detected, defaulting to Jump Tumble.", buildMap.gameObject);
					}
					*/
				}

				// Append tricks to the <Tricks> node
				IEnumerable<string> tricksToAdd = isHunterMode ? processedSpritesHunter : processedSpritesCommon;

				foreach (string trickName in processedSpritesCommon) {
					XmlElement trickElement = xmlDoc.CreateElement("Trick");

					trickElement.SetAttribute("Name", trickName);
					tricksNode.AppendChild(trickElement);

					if (buildMap.logObjectWriting) {
						Debug.Log($"Added Trick Node: {trickElement.GetAttribute("Name")}");
					}
				}
			}

			// Save the modified XML document
			try {
				XmlWriterSettings settings = new XmlWriterSettings {
					Indent = true,
					IndentChars = "\t",
					NewLineChars = "\n",
					NewLineHandling = NewLineHandling.Replace,
					Encoding = System.Text.Encoding.UTF8,
					OmitXmlDeclaration = true
				};

				using (XmlWriter writer = XmlWriter.Create(fileToProcess, settings)) {
					xmlDoc.Save(writer);
				}
			} catch (Exception e) {
				Debug.LogError($"Failed to save XML: {e.Message}");
			}
		}

		// Level thumbnail
		string scenePath = SceneManager.GetActiveScene().path;

		if (string.IsNullOrEmpty(scenePath)) {
			Debug.LogError("Scene was not saved, skipping level thumbnail writing");
		} else {
			string thumbnailImageValue = Path.Combine(
				Directory.GetParent(scenePath).FullName,
				buildMap.thumbnailImagePath
			);

			// Thumbnail paths
			string thumbnailLevelDirectory = @"Assets/XML/dzip/.GUI_2048_1536";
			string thumbnailLevelDirectoryOriginal = Path.Combine(thumbnailLevelDirectory, "_original");
			string levelThumbnailDirectoryFile = Path.Combine(thumbnailLevelDirectory, buildMap.mapToOverride + ".png");
			string levelThumbnailDirectoryFileOriginal = Path.Combine(thumbnailLevelDirectoryOriginal, buildMap.mapToOverride + ".png");

			bool levelThumbnailBackup = false;

			if (String.IsNullOrEmpty(buildMap.thumbnailImagePath)) {
				levelThumbnailBackup = true;
			} else if (File.Exists(thumbnailImageValue)) {
				string relativePath = "Assets" + thumbnailImageValue.Substring(Application.dataPath.Length);
				relativePath = relativePath.Replace(Path.DirectorySeparatorChar.ToString(), "/");
				Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);

				if (texture.width != 512 && texture.height != 340) {
					try {
						Debug.LogWarning("Level thumbnail image dimensions are not 512x340. Image will be scaled from center in game", buildMap.gameObject);
					} catch {
						// pass
					};
				}

				File.Delete(levelThumbnailDirectoryFile);
				File.Copy(thumbnailImageValue, levelThumbnailDirectoryFile);
			} else {
				Debug.LogWarning("Level thumbnail image file was not found, performing backup.", buildMap.gameObject);
				levelThumbnailBackup = true;
			}

			if (levelThumbnailBackup) {
				File.Delete(levelThumbnailDirectoryFile);
				File.Copy(levelThumbnailDirectoryFileOriginal, levelThumbnailDirectoryFile);
			}
		}

		// Transparent GUI paths
		string guiLevelDirectory = @"Assets/XML/dzip/.GUI_2048_1536";
		string guiLevelDirectoryOriginal = Path.Combine(guiLevelDirectory, "_original");
		string inGameInterfaceFile = Path.Combine(guiLevelDirectory, "scene_buttons_2048.png");
		string inGameInterfaceFileOriginal = Path.Combine(guiLevelDirectoryOriginal, Path.GetFileName(inGameInterfaceFile));
		string inGameInterfaceFileOriginalEmpty = Path.Combine(guiLevelDirectoryOriginal, "scene_buttons_2048_empty.png");

		File.Delete(inGameInterfaceFile);
		File.Copy(
			buildMap.transparentInterfaceButtons ? inGameInterfaceFileOriginalEmpty : inGameInterfaceFileOriginal,
			Path.Combine(guiLevelDirectory, Path.GetFileName(inGameInterfaceFile))
		);

		// -=-=-=- //
		// Level name

		string levelNameXml = @"Assets/XML/dzip/.common_xml/localization_all.xml";
		string levelNameXmlOriginal = Path.Combine(
			Directory.GetParent(levelNameXml).FullName,
			"_original",
			Path.GetFileName(levelNameXml)
		);

		if (String.IsNullOrEmpty(buildMap.title)) {
			Debug.LogWarning("Empty level name field, not changing and performing a backup.", buildMap.gameObject);

			File.Delete(levelNameXml);
			File.Copy(levelNameXmlOriginal, levelNameXml);
		} else {
			buildMap.xmlEdit(
				levelNameXml,
				"log", "item_" + buildMap.mapToOverride,
				"*", buildMap.title
			);
		}

		// -=-=-=- //

		// Erase last build
		File.Delete(buildMapXML);
		File.Copy(mapEmptyXML, buildMapXML);

		// Open the object.xml
		XmlDocument xml = new XmlDocument();
		xml.Load(buildMapXML);

		// Search for the selected object in the object.xml
		XmlNode rootNode = xml.DocumentElement.SelectSingleNode("/Root/Track");

		int counter1 = 0;

		foreach (XmlNode node in rootNode) {
			string objectNodeType = node.Attributes.GetNamedItem("Label").Value.ToLower();
			string objectFactorValue = node.Attributes.GetNamedItem("Factor").Value;

			// Set the properties into the level
			if (counter1 == 0) {
				buildMap.SetLevelProperties(xml, node);
			}

			counter1++;

			if (objectNodeType == "default") {
				// Write every GameObject with tag "Object", "Image", "Platform", "Trapezoid", "Area" and "Trigger" in the build-map.xml
				foreach (GameObject spawnInScene in GameObject.FindGameObjectsWithTag("Spawn")) {
					buildMap.ConvertToSpawn(node, xml, spawnInScene);
				}

				foreach (GameObject itemInScene in GameObject.FindGameObjectsWithTag("Item")) {
					if (!buildMap.IsVisible(itemInScene)) {
						continue;
					}

					UnityEngine.Transform parent = itemInScene.transform.parent;

					if (parent != null && (parent.CompareTag("Dynamic") || parent.CompareTag("Object"))) {
						continue;
					}

					buildMap.ConvertToItem(node, xml, itemInScene);
				}

				// Platforms
				foreach (GameObject platformInScene in GameObject.FindGameObjectsWithTag("Platform")) {
					if (!buildMap.IsVisible(platformInScene)) {
						continue;
					}

					UnityEngine.Transform parent = platformInScene.transform.parent;

					if (parent != null && parent.CompareTag("Dynamic")) {
						// If the parent has the tag "Dynamic" skip this GameObject and continue.
						continue;
					}

					buildMap.ConvertToPlatform(node, xml, platformInScene);
				}

				// Trapezoid
				foreach (GameObject trapezoidInScene in GameObject.FindGameObjectsWithTag("Trapezoid")) {
					if (!buildMap.IsVisible(trapezoidInScene)) {
						continue;
					}

					UnityEngine.Transform parent = trapezoidInScene.transform.parent;
	
					if (parent != null && parent.CompareTag("Dynamic")) {
						// If the parent has the tag "Dynamic" skip this GameObject and continue.
						continue;
					}

					buildMap.ConvertToTrapezoid(node, xml, trapezoidInScene);
				}
			}

			// Trigger
			foreach (GameObject triggerInScene in GameObject.FindGameObjectsWithTag("Trigger")) {
				if (!buildMap.IsVisible(triggerInScene)) {
					continue;
				}

				UnityEngine.Transform parent = triggerInScene.transform.parent;

				if (parent != null && parent.CompareTag("Dynamic")) {
					// If the parent has the tag "Dynamic" skip this GameObject and continue.
					continue;
				}

				// Get the SpriteRenderer of the current GameObject
				SpriteRenderer spriteRenderer = triggerInScene.GetComponent<SpriteRenderer>();

				// Get the parent GameObject, if it exists
				Transform triggerParent = triggerInScene.transform.parent;

				if (triggerParent != null) {
					// Check if the parent has a SpriteRenderer
					SpriteRenderer childSpriteRenderer = triggerParent.GetComponent<SpriteRenderer>();

					if (childSpriteRenderer != null && childSpriteRenderer.sortingLayerName == "Overlay") {
						// If the parent's SpriteRenderer sortingLayerName is "Overlay" and objectNodeType is "overlay"
						if (objectNodeType == "overlay") {
							buildMap.ConvertToTrigger(node, xml, triggerInScene);
						}
					}
				}

				// If the current GameObject's SpriteRenderer sortingLayerName is "Default"
				if (spriteRenderer != null && spriteRenderer.sortingLayerName == "Default") {
					if (objectNodeType == "default") {
						buildMap.ConvertToTrigger(node, xml, triggerInScene);
					}
				}
			}

			if (objectNodeType == "default") {
				// Area
				foreach (GameObject areaInScene in GameObject.FindGameObjectsWithTag("Area")) {
					if (!buildMap.IsVisible(areaInScene)) {
						continue;
					}

					UnityEngine.Transform parent = areaInScene.transform.parent;

					if (parent != null && parent.CompareTag("Dynamic")) {
						continue;
					}

					buildMap.ConvertToArea(node, xml, areaInScene);
				}

				// Model
				foreach (GameObject modelInScene in GameObject.FindGameObjectsWithTag("Model")) {
					if (!buildMap.IsVisible(modelInScene)) {
						continue;
					}

					UnityEngine.Transform parent = modelInScene.transform.parent;

					if (parent != null && parent.CompareTag("Dynamic")) {
						// If the parent has the tag "Dynamic" skip this GameObject and continue.
						continue;
					}

					buildMap.ConvertToModel(node, xml, modelInScene);
				}

				// Camera
				foreach (GameObject camInScene in GameObject.FindGameObjectsWithTag("Camera")) {
					// Note: This is actually a trigger, but with camera zoom properties
					if (!buildMap.IsVisible(camInScene)) {
						continue;
					}

					buildMap.ConvertToCamera(node, xml, camInScene);
				}

				// Animation
				foreach (GameObject animationInScene in GameObject.FindGameObjectsWithTag("Animation")) {
					if (!buildMap.IsVisible(animationInScene)) {
						continue;
					}

					UnityEngine.Transform parent = animationInScene.transform.parent;

					if (parent != null && parent.CompareTag("Dynamic")) {
						// If the parent has the tag "Dynamic" skip this GameObject and continue.
						continue;
					}

					buildMap.ConvertToAnimation(node, xml, animationInScene);
				}
			}

			// Image
			foreach (GameObject imageInScene in imagesInScene) {
				if (!buildMap.IsVisible(imageInScene)) {
					continue;
				}

				UnityEngine.Transform parent = imageInScene.transform.parent;

				if (parent != null && parent.CompareTag("Dynamic")) {
					// If the parent has the tag "Dynamic" skip this GameObject and continue.
					continue;
				}

				SpriteRenderer spriteRenderer = imageInScene.GetComponent<SpriteRenderer>();

				if (objectNodeType == "default" && spriteRenderer.sortingLayerName == "Default") {
					buildMap.ConvertToImage(node, xml, imageInScene);
				} else if (objectNodeType == "overlay" && spriteRenderer.sortingLayerName == "Overlay") {
					buildMap.ConvertToImage(node, xml, imageInScene);
				}
			}

			if (objectNodeType == "default") {
				// Dynamic
				foreach (GameObject dynamicInScene in GameObject.FindGameObjectsWithTag("Dynamic")) {
					if (!buildMap.IsVisible(dynamicInScene)) {
						continue;
					}

					UnityEngine.Transform dynamicInSceneTransform = dynamicInScene.transform;

					buildMap.ConvertToDynamic(node, xml, dynamicInScene, dynamicInSceneTransform);
				}

				// Combine the two arrays into one									 
				foreach (GameObject objectInScene in objectsInScene) {
					if (!buildMap.IsVisible(objectInScene)) {
						continue;
					}

					UnityEngine.Transform parent = objectInScene.transform.parent;

					if (parent != null && parent.CompareTag("Dynamic")) {
						// If the parent has the tag "Dynamic" skip this GameObject and continue.
						continue;
					}

					buildMap.ConvertToObject(node, xml, objectInScene);
				}
			}

			// Backdrop

			// Get all GameObjects with tag "Backdrop", then arrange them based on sorting order
			GameObject[] BackdropsInScene = GameObject.FindGameObjectsWithTag("Backdrop")
				.OrderBy(obj => obj.GetComponent<SpriteRenderer>().sortingOrder)
				.ToArray();

			// Get all GameObjects with tag "Top Image", then arrange them based on sorting order
			GameObject[] topImagesInScene = GameObject.FindGameObjectsWithTag("Top Image")
				.OrderBy(obj => obj.GetComponent<SpriteRenderer>().sortingOrder)
				.ToArray();

			// Define a mapping of objectNodeType to sorting layer and factor
			Dictionary<string, List<(string sortingLayer, float factor)>> objectTypeMapBackdrop = new Dictionary<string, List<(string, float)>> {
				{ "0.8", new List<(string, float)> { ("Factor_0.8", 0.8f) } },
				{ "0.5", new List<(string, float)> {
					("Default", 0.5f),
					("Factor_0.5 [Deprecated]", 0.5f)
				}},
				{ "0.25", new List<(string, float)> { ("Factor_0.25", 0.25f) } },
				{ "0.1", new List<(string, float)> { ("Factor_0.1", 0.1f) } }
			};

			if (objectTypeMapBackdrop.TryGetValue(objectNodeType, out var mappingsBackdrop)) {
				foreach (var mappingBackdrop in mappingsBackdrop) {
					foreach (GameObject BackdropInScene in BackdropsInScene) {
						if (!buildMap.IsVisible(BackdropInScene)) {
							continue;
						}

						SpriteRenderer spriteRenderer = BackdropInScene.GetComponent<SpriteRenderer>();
						if (spriteRenderer != null && spriteRenderer.sortingLayerName == mappingBackdrop.sortingLayer) {
							buildMap.ConvertToBackdrop(node, xml, BackdropInScene, mappingBackdrop.factor);
						}
					}
				}
			}

			//-----//

			// Define a mapping of objectNodeType to sorting layer and factor
			Dictionary<string, (string sortingLayer, float factor)> objectTypeMapTopImage = new Dictionary<string, (string, float)> {
				{ "frontdrop", ("Default", 1.0f) },
				{ "1.125", ("Factor_1.125", 1.125f) },
				{ "1.25", ("Factor_1.25", 1.25f) },
				{ "1.375", ("Factor_1.375", 1.375f) }
			};

			if (objectTypeMapTopImage.TryGetValue(objectNodeType, out var mappingTopImage)) {
				foreach (GameObject topImageInScene in topImagesInScene) {
					if (!buildMap.IsVisible(topImageInScene)) {
						continue;
					}

					SpriteRenderer spriteRenderer = topImageInScene.GetComponent<SpriteRenderer>();
					if (spriteRenderer != null && spriteRenderer.sortingLayerName == mappingTopImage.sortingLayer) {
						buildMap.ConvertToTopImage(node, xml, topImageInScene);
					}
				}
			}
		}

		// Replace ` />` or `/>` with the desired replacement (e.g., ` />` -> `/ >`)
		string content = File.ReadAllText(writtenTrackPath);
		string modifiedContent = Regex.Replace(content, @"\s*/>", "/>", RegexOptions.Compiled);
		modifiedContent = modifiedContent.Replace("  ", "\t");
		File.WriteAllText(writtenTrackPath, modifiedContent);

		// Optimization
		long optimizedXmlSizeBytes;
		string optimizedXmlSize;

		XmlWriterSettings optimizeSettings = new XmlWriterSettings {
			Indent = true,
			IndentChars = ("\t"),
			NewLineHandling = NewLineHandling.Replace,
			OmitXmlDeclaration = false
		};

		// Create a memory stream to hold the optimized XML
		using (MemoryStream memoryStream = new MemoryStream()) {
			// Use XmlWriter to write the XML content to the memory stream
			if (!compileMap) {
				using (XmlWriter writer = XmlWriter.Create(memoryStream, optimizeSettings)) {
					if (buildMap.optimizeWrittenTrack) {
						xml.Normalize();
						xml.Save(writer);
					}
				}
			}

			optimizedXmlSizeBytes = memoryStream.Length;
			optimizedXmlSize = buildMap.BytesToString(optimizedXmlSizeBytes);
		}

		// Build level directly into Vector (sweet !)
		if (compileMap) {
			buildMap.StartDzip(useDZ);
			buildMap.hunterPlaced = false;
		}		

		// Show Stopwatch
		stopwatch.Stop();
		TimeSpan ts = stopwatch.Elapsed;

		long writtenTrackFileOriginalSizeBytes = (long)File.ReadAllText(writtenTrackPath).Length;
		string writtenTrackFileOriginalSize = buildMap.BytesToString(writtenTrackFileOriginalSizeBytes, CultureInfo.InvariantCulture);
		string formattedTime = ts.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture);

		/*
		string optimizedXmlSizeDisplay = "";
		if (buildMap.optimizeWrittenTrack) {
			optimizedXmlSizeDisplay = $" {optimizedXmlSizeBytes}";
		}
		*/

		Debug.Log($"Building done! ({formattedTime} seconds) [{writtenTrackFileOriginalSize}]", buildMap.gameObject);

		// -=-=-=- //

		// If the build was for running the game, invoke the MapBuilt event
		if (buildForRunGame) {
			MapBuilt?.Invoke();

			// Reset the flag after the build
			buildForRunGame = false;
		}
	}

	void KillGame(string processName = "Vector") {
		Process[] processes = Process.GetProcessesByName(processName);

		foreach (Process process in processes) {
			if (!process.HasExited) {
				Debug.LogWarning("Closing game (be careful next time)");
				
				process.Kill();
				process.WaitForExit();
			}
		}
	}

	void StartDzip(bool useDZ) {
		// Check if Vector.exe is running - if yes, close it
		KillGame();

		// Update DCL files
		string batchFilesDir = Path.Combine(Application.dataPath, "XML", "dzip");
		string levelXmlDirName = "level_xml";
		string levelXmlDir = Path.Combine(batchFilesDir, levelXmlDirName);

		// Get skins
		var buildMap = FindObjectOfType<BuildMap>();

		List<string> levelXmlDirFiles = Directory.GetFiles(
			levelXmlDir,
			"*.*",
			buildMap.useCustomProperties ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly
		)
			.Where(f => Path.GetExtension(f).ToLower() != ".meta")
			.ToList();

		if (!buildMap.useCustomProperties) {
			List<string> allSkins = new List<string>();

			allSkins.AddRange( ParseSkins(buildMap.Player.playerSkins, true) as List<string> );
			allSkins.AddRange( ParseSkins(buildMap.Hunter.hunterSkins, true) as List<string> );

			allSkins = allSkins.Select(elem => elem + ".xml").ToList();
			levelXmlDirFiles.AddRange(allSkins);
		}

		levelXmlDirFiles.Sort();

		foreach (string file in Directory.GetFiles(batchFilesDir)) {
			if (file.Contains("config") && Path.GetExtension(file) == ".dcl") {
				using (StreamWriter fileObj = new StreamWriter(file)) {
					fileObj.WriteLine($"archive \"{levelXmlDirName}.dz\"");

					// Default
					string levelXmlDirNameWrite = levelXmlDirName;

					if (file.ToLower().Contains("backup.")) {
						levelXmlDirNameWrite = levelXmlDirName + "_backup";
					} else {
						levelXmlDirNameWrite = levelXmlDirName;
					}

					fileObj.WriteLine($"basedir \"{levelXmlDirNameWrite}\"");

					// Now iterate over the sorted list
					foreach (string xmlFile in levelXmlDirFiles) {
						string relativePath = xmlFile.Replace(levelXmlDir, "").TrimStart(Path.DirectorySeparatorChar);
						relativePath = relativePath.Replace(Path.DirectorySeparatorChar, '/');
						string compressionType = useDZ ? "dz" : "zlib";
						string newLine = $"file \"{relativePath}\" 0 {compressionType}";

						fileObj.WriteLine(newLine);
					}
				}
			}
		}

		// Start compressing levels into level_xml.dz
		string batchFileName = useDZ ? "compile-map.bat" : "compile-map-optimized.bat";
		string batchFilePath = Path.Combine(batchFilesDir, batchFileName);
		string batchDirectory = Path.GetDirectoryName(batchFilePath);

		// In-game attributes
		string batchFilePath_name = Path.Combine(batchFilesDir, "compile-common_xml.bat");

		Process batchProcess_name = new Process {
			StartInfo = {
				FileName = batchFilePath_name,
				UseShellExecute = false,
				RedirectStandardOutput = false,
				RedirectStandardError = false,
				CreateNoWindow = true,
				WorkingDirectory = batchDirectory
			}
		};

		try {
			batchProcess_name.Start();
			batchProcess_name.WaitForExit();
		} finally {
			batchProcess_name.Close();
		}

		File.Delete(
			Path.Combine(vectorFilePath, "common_xml.dz")
		);
		File.Copy(
			Path.Combine(batchDirectory, "common_xml.dz"),
			Path.Combine(vectorFilePath, "common_xml.dz")
		);

		// -=-=-=- //

		string batchFilePath_image = Path.Combine(batchFilesDir, "compile-gui_2048_1536.bat");

		Process batchProcess_image = new Process {
			StartInfo = {
				FileName = batchFilePath_image,
				UseShellExecute = false,
				RedirectStandardOutput = false,
				RedirectStandardError = false,
				CreateNoWindow = true,
				WorkingDirectory = batchDirectory
			}
		};

		try {
			batchProcess_image.Start();
			batchProcess_image.WaitForExit();
		} finally {
			batchProcess_image.Close();
		}

		File.Delete(
			Path.Combine(vectorFilePath, "GUI_2048_1536.dz")
		);
		File.Copy(
			Path.Combine(batchDirectory, "GUI_2048_1536.dz"),
			Path.Combine(vectorFilePath, "GUI_2048_1536.dz")
		);

		// -=-=-=- //

		if (!File.Exists(batchFilePath)) {
			Debug.LogError($"Batch file not found: {batchFilePath}");
			return;
		}

		Process batchProcess = new Process {
			StartInfo = {
				FileName = batchFilePath,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				WorkingDirectory = batchDirectory
			}
		};

		// Start the process
		try {
			batchProcess.Start();

			// Wait for the process to exit
			batchProcess.WaitForExit();

			// Check exit code if necessary
			if (batchProcess.ExitCode != 0) {
				string errorOutput = batchProcess.StandardError.ReadToEnd();
				Debug.LogError($"dzip package encountered an error: {errorOutput}");
			} else {
				// Move the file if the process succeeded
				string sourceFilePath = Path.Combine(Application.dataPath, "XML", "dzip", "level_xml.dz");
				string destinationFilePath = Path.Combine(vectorFilePath, "level_xml.dz");

				if (File.Exists(sourceFilePath)) {
					if (File.Exists(destinationFilePath)) {
						File.Delete(destinationFilePath);
					}

					File.Copy(sourceFilePath, destinationFilePath);
				} else {
					Debug.LogError("level_xml.dz was not found! Check if your Vector path is correct");
				}
			}
		} catch (Exception e) {
			Debug.LogError($"Failed to start dzip: {e.Message}");
		} finally {
			// Ensure to close the process resources
			batchProcess.Close();
		}

		// Trigger the event if the build was intended for running the game
		if (buildForRunGame) {
			MapBuilt?.Invoke();

			// Reset flag after building
			buildForRunGame = false;
		}
	}

	// -=-=-=-=-=- //

	void ConvertToAnimation(XmlNode node, XmlDocument xml, GameObject animationInScene) {
		// Animation Properties Component
		AnimationProperties AnimationComponent = animationInScene.GetComponent<AnimationProperties>();

		if (animationInScene.name == "Camera") {
			return;
		}

		// Create a new node from scratch
		XmlElement animationElement = xml.CreateElement("Animation");

		// Add X position (Refit into the Vector units)
		animationElement.SetAttribute("X", Engine.Math.GameUnits.Multiply(animationInScene.transform.position.x));

		// Add Y position (Negative because Vector see the world upside down)
		animationElement.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-animationInScene.transform.position.y));

		// Add a Width
		animationElement.SetAttribute("Width", AnimationComponent.Width);

		// Add a Height
		animationElement.SetAttribute("Height", AnimationComponent.Height);

		// Type (default: 1)
		animationElement.SetAttribute("Type", AnimationComponent.Type);

		// Add a ScaleX
		animationElement.SetAttribute("ScaleX", AnimationComponent.ScaleX);

		// Add a ScaleY
		animationElement.SetAttribute("ScaleY", AnimationComponent.ScaleY);

		// Add a name
		animationElement.SetAttribute("ClassName", Regex.Replace(animationInScene.name, globalRegex, string.Empty));

		if (AnimationComponent == null) {
			Debug.LogWarning($@"AnimationComponent is missing on ""{animationInScene.name}""", animationInScene);
		}

		if (!string.IsNullOrEmpty(AnimationComponent.Direction)) {
			// Direction (ex: Direction="10|-1.5")
			animationElement.SetAttribute("Direction", AnimationComponent.Direction);
		}

		if (!string.IsNullOrEmpty(AnimationComponent.Acceleration)) {
			// Acceleration (ex: Acceleration="0.02|-0.1")
			animationElement.SetAttribute("Acceleration", AnimationComponent.Acceleration);
		}

		if (!string.IsNullOrEmpty(AnimationComponent.Time)) {
			// Add a Time
			animationElement.SetAttribute("Time", AnimationComponent.Time);
		}

		// -- Writing mode --
		string writeModeValue = "any";

		VectorierWriteMode writeMode = animationInScene.GetComponent<VectorierWriteMode>();

		if (writeMode != null) {
			writeModeValue = writeMode.GetWriteModeValue();
			writeMode.AddWriteModeProperties(xml, animationElement, writeModeValue);
		}

		// Place it into the Object node
		node.FirstChild.AppendChild(animationElement);

		// Apply the modification to the build-map.xml file}
		xml.Save(Path.Combine(
			Application.dataPath,
			"XML", "dzip", "level_xml",
			mapToOverride + ".xml"
		));
	}

	void ConvertToTopImage(XmlNode node, XmlDocument xml, GameObject topImageInScene) {
		string objRegex = Regex.Replace(topImageInScene.name, globalRegex, string.Empty);
		string objName;

		if (topImageInScene.name == "Camera") {
			return;
		}

		// Create a new node from scratch
		XmlElement TI_element = xml.CreateElement("Image");

		// Add X position (Refit into the Vector units)
		TI_element.SetAttribute("X", Engine.Math.GameUnits.Multiply(topImageInScene.transform.position.x));

		// Add Y position (Negative because Vector see the world upside down)
		TI_element.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-topImageInScene.transform.position.y * 100));

		// Add a name based on the sprite or fallback to the regex result
		SpriteRenderer spriteRenderer = topImageInScene.GetComponent<SpriteRenderer>();
		objName = (spriteRenderer != null && spriteRenderer.sprite != null) ? spriteRenderer.sprite.name : objRegex;
		TI_element.SetAttribute("ClassName", objName);

		XmlElement propertiesElement = xml.CreateElement("Properties");
		XmlElement staticElement = xml.CreateElement("Static");

		bool staticElementHasContent = false;
		// Get the Image Size in Width and Height
		if (spriteRenderer != null && spriteRenderer.sprite != null) {
			// Check the rotation

			// Normalize to 0-360 degrees
			float rotationAngle = topImageInScene.transform.eulerAngles.z % 360;

			float imagePosX = topImageInScene.transform.position.x * 100;
			float imagePosY = -topImageInScene.transform.position.y * 100;

			bool flipX = spriteRenderer.flipX;
			bool flipY = spriteRenderer.flipY;

			// If exactly one of them is flipped, negate the angle
			if (flipX ^ flipY) {
				rotationAngle = -rotationAngle;
			}

			if ((rotationAngle != 0 && rotationAngle != 360) || flipX || flipY) {
				// Convert the rotation to the Marmalade transformation matrix
				float A, B, C, D, Tx, Ty;
				
				// Get Native resolution of the sprite
				int nativeWidth = spriteRenderer.sprite.texture.width;
				int nativeHeight = spriteRenderer.sprite.texture.height;
				
				// Get the image scale using sprite resolution * local scale
				float imageWidth = nativeWidth * topImageInScene.transform.localScale.x;
				float imageHeight = nativeHeight * topImageInScene.transform.localScale.y;

				if (rotationAngle == 90) {
					A = 0;
					B = -imageWidth;
					C = imageHeight;
					D = 0;
				} else if (rotationAngle == 180) {
					A = -imageWidth;
					B = 0;
					C = 0;
					D = -imageHeight;
				} else if (rotationAngle == 270) {
					A = 0;
					B = imageWidth;
					C = -imageHeight;
					D = 0;
				} else {
					float radians = rotationAngle * Mathf.Deg2Rad;
					float cosTheta = Mathf.Cos(radians);
					float sinTheta = Mathf.Sin(radians);

					A = imageWidth * cosTheta;
					B = -imageWidth * sinTheta;
					C = imageHeight * sinTheta;
					D = imageHeight * cosTheta;
				}

				// Apply FlipX and FlipY
				if (flipX) {
					A = -A;
					C = -C;
				}

				if (flipY) {
					B = -B;
					D = -D;
				}

				// Compute translation
				float topLeftX = imagePosX + Math.Min(0, A) + Math.Min(0, C);
				float topLeftY = imagePosY + Math.Min(0, B) + Math.Min(0, D);

				Tx = imagePosX - topLeftX;
				Ty = imagePosY - topLeftY;

				// Setting all of the attributes
				TI_element.SetAttribute("X", Math.Round(topLeftX).ToString());
				TI_element.SetAttribute("Y", Math.Round(topLeftY).ToString());
				TI_element.SetAttribute("Width", imageWidth.ToString(CultureInfo.InvariantCulture));
				TI_element.SetAttribute("Height", imageHeight.ToString(CultureInfo.InvariantCulture));
				TI_element.SetAttribute("NativeX", nativeWidth.ToString(CultureInfo.InvariantCulture));
				TI_element.SetAttribute("NativeY", nativeHeight.ToString(CultureInfo.InvariantCulture));

				// Apply transformation matrix
				XmlElement matrixElement = xml.CreateElement("Matrix");
				matrixElement.SetAttribute("A", A.ToString("0.#####", CultureInfo.InvariantCulture));
				matrixElement.SetAttribute("B", B.ToString("0.#####", CultureInfo.InvariantCulture));
				matrixElement.SetAttribute("C", C.ToString("0.#####", CultureInfo.InvariantCulture));
				matrixElement.SetAttribute("D", D.ToString("0.#####", CultureInfo.InvariantCulture));
				matrixElement.SetAttribute("Tx", Tx.ToString("0.#####", CultureInfo.InvariantCulture));
				matrixElement.SetAttribute("Ty", Ty.ToString("0.#####", CultureInfo.InvariantCulture));

				staticElement.AppendChild(matrixElement);
				propertiesElement.AppendChild(staticElement);
				TI_element.AppendChild(propertiesElement);
			} else {
				Bounds bounds = spriteRenderer.sprite.bounds;
				Vector3 scale = topImageInScene.transform.localScale;
				float width = bounds.size.x * 100;
				float height = bounds.size.y * 100;

				TI_element.SetAttribute("X", Math.Round(imagePosX).ToString());
				TI_element.SetAttribute("Y", Math.Round(imagePosY).ToString());
				TI_element.SetAttribute("Width", (width * scale.x).ToString(CultureInfo.InvariantCulture));
				TI_element.SetAttribute("Height", (height * scale.y).ToString(CultureInfo.InvariantCulture));

				if (width != width * scale.x) {
					TI_element.SetAttribute("NativeX", width.ToString(CultureInfo.InvariantCulture));
				}

				if (height != height * scale.y) {
					TI_element.SetAttribute("NativeY", height.ToString(CultureInfo.InvariantCulture));
				}
			}

			// Color application
			Color color = spriteRenderer.color;
			if (
				(color.r != 1.000f || color.g != 1.000f || color.b != 1.000f || color.a != 1.000f)
				&& objName != "v_black"
			) {
				string alphaHex = Mathf.RoundToInt(color.a * 255).ToString("X2");
				string rgbaColor = ColorUtility.ToHtmlStringRGB(color) + alphaHex;

				XmlElement colorElement = xml.CreateElement("StartColor");
				colorElement.SetAttribute("Color", $"#{rgbaColor}");

				staticElement.AppendChild(colorElement);
				staticElementHasContent = true;
			}

			if (staticElementHasContent) {
				propertiesElement.AppendChild(staticElement);
			}

			// Dynamic Color
			DynamicColor dynamicColor = topImageInScene.GetComponent<DynamicColor>();
			if (dynamicColor != null) {
				XmlElement dynamicElement = xml.CreateElement("Dynamic");
				XmlElement transformationElement = xml.CreateElement("Transformation");
				XmlElement colorElement = xml.CreateElement("Color");

				transformationElement.SetAttribute("Name", dynamicColor.TransformationName);

				Color currentColor = spriteRenderer.color;
				string startColorHex = ColorUtility.ToHtmlStringRGB(currentColor) + Mathf.RoundToInt(currentColor.a * 255).ToString("X2");
				string finishColorHex = ColorUtility.ToHtmlStringRGB(dynamicColor.ChangeToColor) + Mathf.RoundToInt(dynamicColor.ChangeToColor.a * 255).ToString("X2");

				colorElement.SetAttribute("ColorStart", $"#{startColorHex}");
				colorElement.SetAttribute("ColorFinish", $"#{finishColorHex}");

				int frames = dynamicColor.Duration > 0 ? Mathf.CeilToInt(dynamicColor.Duration * 60) : 1;
				colorElement.SetAttribute("Frames", frames.ToString(CultureInfo.InvariantCulture));

				transformationElement.AppendChild(colorElement);
				dynamicElement.AppendChild(transformationElement);
				propertiesElement.AppendChild(dynamicElement);
			}

			if (propertiesElement.HasChildNodes) {
				TI_element.AppendChild(propertiesElement);
			}
		}

		// -- Writing mode --
		string writeModeValue = "any";

		VectorierWriteMode writeMode = topImageInScene.GetComponent<VectorierWriteMode>();

		if (writeMode != null) {
			writeModeValue = writeMode.GetWriteModeValue();
			writeMode.AddWriteModeProperties(xml, TI_element, writeModeValue);
		}

		// Place it into the Object node
		node.FirstChild.AppendChild(TI_element);

		// Apply the modification to the build-map.xml file}
		xml.Save(Path.Combine(
			Application.dataPath,
			"XML", "dzip", "level_xml",
			mapToOverride + ".xml"
		));
	}

	// hunter handling?
	void ConvertToSpawn(XmlNode node, XmlDocument xml, GameObject spawnInScene) {
		// Respawn component
		Respawn RespawnComponent = spawnInScene.GetComponent<Respawn>();

		// Spawn component
		Spawn Spawn = spawnInScene.GetComponent<Spawn>();
		XmlElement spawnElement = xml.CreateElement("Spawn");
		Spawn[] SpawnComponent = FindObjectsOfType<Spawn>();

		if (RespawnComponent != null) {
			// Root
			XmlElement objectElement = xml.CreateElement("Object");
			objectElement.SetAttribute("X", "0");
			objectElement.SetAttribute("Y", "0");

			// Content
			XmlElement contentElement = xml.CreateElement("Content");

			foreach (Spawn spawns in SpawnComponent) {
				// Check every GameObject that has the spawn component
				GameObject gameObjwithSpawnComponent = spawns.gameObject;
				if (RespawnComponent.RespawnName == gameObjwithSpawnComponent.GetComponent<Spawn>().SpawnName) {
					if (gameObjwithSpawnComponent.GetComponent<Spawn>().RefersToRespawn) {
						// Spawn element
						XmlElement spawnInsideElement = xml.CreateElement("Spawn");

						// Add X position (Refit into the Vector units)
						spawnInsideElement.SetAttribute("X", Engine.Math.GameUnits.Multiply(gameObjwithSpawnComponent.transform.position.x));

						// Add Y position (Negative because Vector see the world upside down)
						spawnInsideElement.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-gameObjwithSpawnComponent.transform.position.y));

						// Add a name
						spawnInsideElement.SetAttribute("Name", gameObjwithSpawnComponent.GetComponent<Spawn>().SpawnName);
						spawnInsideElement.SetAttribute("Animation", gameObjwithSpawnComponent.GetComponent<Spawn>().SpawnAnimation);

						contentElement.AppendChild(spawnInsideElement);
					}
				}
			}

			// Trigger element
			XmlElement triggerElement = xml.CreateElement("Trigger");
			triggerElement.SetAttribute("Name", RespawnComponent.TriggerName);

			// Add X position (Refit into the Vector units)
			triggerElement.SetAttribute("X", Engine.Math.GameUnits.Multiply(spawnInScene.transform.position.x));

			// Add Y position (Negative because Vector see the world upside down)
			triggerElement.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-spawnInScene.transform.position.y));

			SpriteRenderer spriteRenderer = spawnInScene.GetComponent<SpriteRenderer>();

			// Get the Sprite Size in Width and Height
			if (spriteRenderer != null && spriteRenderer.sprite != null) {
				// Get the bounds of the sprite
				Bounds bounds = spriteRenderer.sprite.bounds;

				// Get the GameObject scale
				Vector3 scale = spawnInScene.transform.localScale;

				// Retrieve the image resolution of the sprite
				float width = bounds.size.x * 100;
				float height = bounds.size.y * 100;

				// -=-=-=- //
				// Set the width and height accordingly to the scale in the editor

				// Width of the Image
				triggerElement.SetAttribute("Width", (width * scale.x).ToString(CultureInfo.InvariantCulture));

				// Height of the Image
				triggerElement.SetAttribute("Height", (height * scale.y).ToString(CultureInfo.InvariantCulture));
			}

			// Create the properties element and its child static element
			XmlElement propertiesElement = xml.CreateElement("Properties");
			XmlElement staticElement = xml.CreateElement("Static");
			XmlElement selectionElement = xml.CreateElement("Selection");
			selectionElement.SetAttribute("Choice", "AITriggers");

			// Hunter mode
			selectionElement.SetAttribute("Variant", RespawnComponent.HunterModeRespawn ? "HunterMode" : "CommonMode");

			staticElement.AppendChild(selectionElement);
			propertiesElement.AppendChild(staticElement);
			triggerElement.AppendChild(propertiesElement);

			// Create content element inside trigger element
			XmlElement triggerContentElement = xml.CreateElement("Content");

			// Create the init element and its child setVariable element
			XmlElement initElement = xml.CreateElement("Init");

			float Frames = RespawnComponent.RespawnSecond * 60;

			string[][] setVariables = {
				new[] { "Name", "$Active", "Value", "1" },
				new[] { "Name", "$Node", "Value", "COM" },
				new[] { "Name", "Spawn", "Value", RespawnComponent.RespawnName },
				new[] { "Name", "Frames", "Value", Mathf.Round(Frames).ToString() },
				new[] { "Name", "SpawnModel", "Value", RespawnComponent.Spawnmodel },
				new[] { "Name", "Reversed", "Value", "0" },
				new[] { "Name", "$AI", "Value", "0" },
				new[] { "Name", "Flag1", "Value", "0" },
			};

			// Add each setVariable element to the init element
			foreach (var setVariable in setVariables) {
				XmlElement setVariableElement = xml.CreateElement("SetVariable");
				setVariableElement.SetAttribute(setVariable[0], setVariable[1]);
				setVariableElement.SetAttribute(setVariable[2], setVariable[3]);
				initElement.AppendChild(setVariableElement);
			}

			triggerContentElement.AppendChild(initElement);

			// Create template element inside content element
			if (RespawnComponent.RespawnOnScreen) {
				XmlElement templateElement = xml.CreateElement("Loop");
				templateElement.SetAttribute("Template", "Respawn_OnScreen.Player");

				XmlElement templateElement2 = xml.CreateElement("Loop");
				templateElement2.SetAttribute("Template", "Respawn_OnScreen.Timeout");

				triggerContentElement.AppendChild(templateElement);
				triggerContentElement.AppendChild(templateElement2);
			} else {
				XmlElement templateElement = xml.CreateElement("Template");
				templateElement.SetAttribute("Name", "Respawn_OnScreen");
				triggerContentElement.AppendChild(templateElement);
			}

			triggerElement.AppendChild(triggerContentElement);
			contentElement.AppendChild(triggerElement);
			objectElement.AppendChild(contentElement);

			node.FirstChild.AppendChild(objectElement);
		} else if (RespawnComponent == null && Spawn != null) {
			if (Spawn.RefersToRespawn == false) {
				// Add X position (Refit into the Vector units)
				spawnElement.SetAttribute("X", Engine.Math.GameUnits.Multiply(spawnInScene.transform.position.x));

				// Add Y position (Negative because Vector see the world upside down)
				spawnElement.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-spawnInScene.transform.position.y));

				// Name in the spawn component
				spawnElement.SetAttribute("Name", Spawn.SpawnName);

				// Spawn Animation in Spawn component
				spawnElement.SetAttribute("Animation", Spawn.SpawnAnimation);

				// Place it into the Object node
				node.FirstChild.AppendChild(spawnElement);
			}
		}

		// -- Writing mode --
		string writeModeValue = "any";

		VectorierWriteMode writeMode = spawnInScene.GetComponent<VectorierWriteMode>();

		if (writeMode != null) {
			writeModeValue = writeMode.GetWriteModeValue();
			writeMode.AddWriteModeProperties(xml, spawnElement, writeModeValue);
		}

		// Apply the modification to the build-map.xml file}
		xml.Save(Path.Combine(
			Application.dataPath,
			"XML", "dzip", "level_xml",
			mapToOverride + ".xml"
		));
	}

	void SetLevelProperties(XmlDocument xml, XmlNode objectNode) {
		// Find all objects
		GameObject[] allObj = FindObjectsOfType<GameObject>(); // won't find from Build void

		XmlNode rootNode = xml.DocumentElement.SelectSingleNode("/Root");

		// -=-=-=- //

		// Set the background
		XmlNode objNode = xml.SelectSingleNode("/Root/Track/Object[@Factor='0.05']");
		if (objNode != null) {
			XmlNode contentNode = objNode.SelectSingleNode("Content");

			if (contentNode != null) {
				XmlNodeList imageNodes = contentNode.SelectNodes("Image");

				// Assign new X values and update attributes
				for (int i = 0; i < imageNodes.Count; i++) {
					XmlNode imageNode = imageNodes[i];

					if (i % 2 == 0) {
						imageNode.Attributes["ClassName"].Value = 
							!string.IsNullOrEmpty(customBackground) ? customBackground : 
							(string.IsNullOrEmpty(customBackgroundMirror) ? "defaultBackground" : customBackgroundMirror);
					} else {
						imageNode.Attributes["ClassName"].Value = 
							!string.IsNullOrEmpty(customBackgroundMirror) ? customBackgroundMirror : 
							(string.IsNullOrEmpty(customBackground) ? "defaultBackground" : customBackground);
					}

					imageNode.Attributes["Width"].Value = backgroundWidth.ToString(CultureInfo.InvariantCulture);
					imageNode.Attributes["Height"].Value = backgroundHeight.ToString(CultureInfo.InvariantCulture);

					imageNode.Attributes["X"].Value = (-3740 + (i * backgroundWidth)).ToString(CultureInfo.InvariantCulture);
					imageNode.Attributes["Y"].Value = (-500 + backgroundYPosition).ToString(CultureInfo.InvariantCulture);
				}
			}
		}

		// Set the music
		if (levelMusic != null) {
			XmlNode musicNode = xml.DocumentElement.SelectSingleNode("/Root/Music");
			XmlAttribute musicAttribute = musicNode.Attributes["Name"];
			XmlAttribute musicVolAttribute = musicNode.Attributes["Volume"];

			if (musicAttribute.Value != null) {
				musicAttribute.Value = levelMusic;
				musicVolAttribute.Value = MusicVolume;
			}
		} else {
			Debug.LogWarning("No music name specified.");
		}

		// BIKE SETTING
		bool hasBikeStock_Player = false;
		foreach (GameObject go in allObj) {
			if (go.name.ToLower() == "cs_bike_starting_bike" && go.CompareTag("Model")) {
				hasBikeStock_Player = true;
			}
		}

		// Set player, hunter properties
		
		// if custom properties are used
		if (useCustomProperties) {
			foreach (XmlNode modelsNode in rootNode) {
				// Search for the models node
				if (modelsNode.Name == "Models" && modelsNode.Attributes["Variant"].Value == "CommonMode") {

					// If there is child node then remove it {
					while (modelsNode.HasChildNodes) {
						// I'm not gonna lie, just trying to remove childnode took me solid 2 hours
						modelsNode.RemoveChild(modelsNode.FirstChild);
					}

					XmlDocument tempDoc = new XmlDocument();
					tempDoc.LoadXml($"<root>{CustomModelProperties}</root>");

					foreach (XmlNode childNode in tempDoc.DocumentElement.ChildNodes) {
						XmlNode importedNode = xml.ImportNode(childNode, true);
						modelsNode.AppendChild(importedNode);
					}
				}
			}
		} else {
			foreach (XmlNode modelsNode in rootNode) {
				if (modelsNode.Name == "Models" && modelsNode.Attributes["Variant"].Value == "CommonMode") {
					foreach (XmlNode modelNode in modelsNode.ChildNodes) {
						// Player
						if (modelNode.Attributes["Name"].Value == "Player" || modelNode.Attributes["Name"].Value == Player.playerModelName) {
							// Player model name
							modelNode.Attributes["Name"].Value = Player.playerModelName;

							// Spawn time
							if (Player.playerSpawnTime < 0) {
								Debug.LogError($"Player spawn time is lower than 0.");
								return;
							}

							modelNode.Attributes["Time"].Value = Player.playerSpawnTime.ToString(CultureInfo.InvariantCulture);

							// Spawn name
							modelNode.Attributes["BirthSpawn"].Value = Player.playerSpawnName;

							// Life Time
							(modelNode.Attributes["LifeTime"] ?? modelNode.Attributes.Append(modelNode.OwnerDocument.CreateAttribute("LifeTime"))).Value = Player.playerLifeTime.ToString(CultureInfo.InvariantCulture);

							// Bike stock
							if (hasBikeStock_Player) {
								(modelNode.Attributes["Stocks"] ?? modelNode.Attributes.Append(modelNode.OwnerDocument.CreateAttribute("Stocks"))).Value = "Bike";
							}

							// Skins
							XmlAttribute playerSkins = xml.CreateAttribute("Skins");

							// Check if playerSkins are specified
							if (string.IsNullOrEmpty(Player.playerSkins)) {
								playerSkins.Value = "1";
								Debug.LogWarning("Player skin isn't specified, setting to default");
							} else {
								playerSkins.Value = ParseSkins(Player.playerSkins) as string;

								if (string.IsNullOrEmpty(playerSkins.Value)) {
									playerSkins.Value = "blank";
								}
							}

							// Color
							if (Player.modelColor != Color.black) {
								var colorAttr = modelNode.Attributes["Color"]
									?? modelNode.Attributes.Append(modelNode.OwnerDocument.CreateAttribute("Color"));

								byte r = (byte)Mathf.RoundToInt(Player.modelColor.r * 255);
								byte g = (byte)Mathf.RoundToInt(Player.modelColor.g * 255);
								byte b = (byte)Mathf.RoundToInt(Player.modelColor.b * 255);

								if (Player.modelColor.a < 1) {
									Debug.LogWarning($"Player model color has changed opacity, which is not supported and will be ignored.");
								}

								colorAttr.Value = $"00{r:X2}{g:X2}{b:X2}";
							}

							modelNode.Attributes.Append(playerSkins);
						}

						// Hunter
						if (modelNode.Attributes["Name"].Value == "Hunter" || modelNode.Attributes["Name"].Value == Hunter.hunterModelName) {
							// Hunter model name
							modelNode.Attributes["Name"].Value = Hunter.hunterModelName;

							// Spawn time
							if (Hunter.hunterSpawnTime < 0) {
								Debug.LogError($"Hunter spawn time is lower than 0.");
								return;
							}

							modelNode.Attributes["Time"].Value = Hunter.hunterSpawnTime.ToString(CultureInfo.InvariantCulture);

							// Spawn name
							modelNode.Attributes["BirthSpawn"].Value = Hunter.hunterSpawnName;

							// AI number 
							modelNode.Attributes["AI"].Value = Hunter.hunterAIType.ToString();

							// Icon
							modelNode.Attributes["Icon"].Value = Hunter.hunterIcon ? "1" : "0";

							// Life Time
							(modelNode.Attributes["LifeTime"] ?? modelNode.Attributes.Append(modelNode.OwnerDocument.CreateAttribute("LifeTime"))).Value = Hunter.hunterLifeTime.ToString(CultureInfo.InvariantCulture);

							// Skins
							XmlAttribute hunterSkins = xml.CreateAttribute("Skins");

							// Check if hunterSkins are specified
							if (string.IsNullOrEmpty(Hunter.hunterSkins)) {
								hunterSkins.Value = "hunter";
								Debug.LogWarning("Hunter skin isn't specified, setting to default");
							} else {
								hunterSkins.Value = ParseSkins(Hunter.hunterSkins) as string;

								if (string.IsNullOrEmpty(hunterSkins.Value)) {
									hunterSkins.Value = "blank";
								}
							}

							modelNode.Attributes.Append(hunterSkins);

							// Color
							if (Hunter.modelColor != Color.black) {
								var colorAttr = modelNode.Attributes["Color"]
									?? modelNode.Attributes.Append(modelNode.OwnerDocument.CreateAttribute("Color"));

								byte r = (byte)Mathf.RoundToInt(Hunter.modelColor.r * 255);
								byte g = (byte)Mathf.RoundToInt(Hunter.modelColor.g * 255);
								byte b = (byte)Mathf.RoundToInt(Hunter.modelColor.b * 255);

								if (Hunter.modelColor.a < 1) {
									Debug.LogWarning($"Hunter model color has changed opacity, which is not supported and will be ignored.");
								}

								colorAttr.Value = $"00{r:X2}{g:X2}{b:X2}";
							}

							// Check if hunter is allowed to do tricks
							if (Hunter.hunterTrickAllowed) {
								XmlAttribute hunterTrick = xml.CreateAttribute("Trick");
								hunterTrick.Value = "1";
								modelNode.Attributes.Append(hunterTrick);
							}
						}

						// Helper
						if (modelNode.Attributes["Name"].Value == "Helper" || modelNode.Attributes["Name"].Value == Helper.helperModelName) {
							if (!Helper.spawn) {
								modelsNode.RemoveChild(modelNode);
								continue;
							}

							// Helper model name
							modelNode.Attributes["Name"].Value = Helper.helperModelName;

							// Spawn time
							if (Helper.helperSpawnTime < 0) {
								Debug.LogError($"Helper spawn time is lower than 0.");
								return;
							}

							modelNode.Attributes["Time"].Value = Helper.helperSpawnTime.ToString(CultureInfo.InvariantCulture);

							// Spawn name
							modelNode.Attributes["BirthSpawn"].Value = Helper.helperSpawnName;

							// Life Time
							(modelNode.Attributes["LifeTime"] ?? modelNode.Attributes.Append(modelNode.OwnerDocument.CreateAttribute("LifeTime"))).Value = Helper.helperLifeTime.ToString(CultureInfo.InvariantCulture);

							// Skins
							XmlAttribute helperSkins = xml.CreateAttribute("Skins");

							// Check if helperSkins are specified
							if (string.IsNullOrEmpty(Helper.helperSkins)) {
								helperSkins.Value = "1";
								Debug.LogWarning("Helper skin isn't specified, setting to default");
							} else {
								helperSkins.Value = ParseSkins(Helper.helperSkins) as string;

								if (string.IsNullOrEmpty(helperSkins.Value)) {
									helperSkins.Value = "blank";
								}
							}

							modelNode.Attributes.Append(helperSkins);

							// Color
							if (Helper.modelColor != Color.black) {
								var colorAttr = modelNode.Attributes["Color"]
									?? modelNode.Attributes.Append(modelNode.OwnerDocument.CreateAttribute("Color"));

								byte r = (byte)Mathf.RoundToInt(Helper.modelColor.r * 255);
								byte g = (byte)Mathf.RoundToInt(Helper.modelColor.g * 255);
								byte b = (byte)Mathf.RoundToInt(Helper.modelColor.b * 255);

								if (Helper.modelColor.a < 1) {
									Debug.LogWarning($"Helper model color has changed opacity, which is not supported and will be ignored.");
								}

								colorAttr.Value = $"00{r:X2}{g:X2}{b:X2}";
							}
						}
					}
				}
			}

			// Hunter mode
			foreach (XmlNode modelsNode in rootNode) {
				if (modelsNode.Name == "Models" && modelsNode.Attributes["Variant"].Value == "HunterMode") {
					foreach (XmlNode modelNode in modelsNode.ChildNodes) {
						// Hunter
						if (modelNode.Attributes["Name"].Value == "Hunter" || modelNode.Attributes["Name"].Value == Hunter.hunterModelName) {
							// Hunter model name
							modelNode.Attributes["Name"].Value = Hunter.hunterModelName;

							// Spawn time
							float totalSpawnTime = HunterHM.hunterSpawnTime == -1.0f ? Hunter.hunterSpawnTime : HunterHM.hunterSpawnTime;
							totalSpawnTime += HunterHM.hunterSpawnTimeIncrement;

							if (totalSpawnTime < 0) {
								Debug.LogError($"Hunter spawn time is lower than 0.");
								return;
							}

							modelNode.Attributes["Time"].Value = totalSpawnTime.ToString(CultureInfo.InvariantCulture);

							// Life Time
							(modelNode.Attributes["LifeTime"] ?? modelNode.Attributes.Append(modelNode.OwnerDocument.CreateAttribute("LifeTime"))).Value = Hunter.hunterLifeTime.ToString(CultureInfo.InvariantCulture);

							// Spawn
							string HMHunterSpawn = string.IsNullOrEmpty(HunterHM.hunterAllowedSpawn) ? Hunter.hunterAllowedSpawn: HunterHM.hunterAllowedSpawn;
							(modelNode.Attributes["BirthSpawn"] ?? modelNode.Attributes.Append(modelNode.OwnerDocument.CreateAttribute("BirthSpawn"))).Value = HMHunterSpawn;

							// Victory
							if (modelNode.Attributes["Victory"] != null) {
								modelNode.Attributes.RemoveNamedItem("Victory");
							}

							// Bike stock
							if (hasBikeStock_Player) {
								(modelNode.Attributes["Stocks"] ?? modelNode.Attributes.Append(modelNode.OwnerDocument.CreateAttribute("Stocks"))).Value = "Bike";
							}

							// Skins
							XmlAttribute hunterSkins = xml.CreateAttribute("Skins");

							// Check if hunterSkins are specified
							if (string.IsNullOrEmpty(Hunter.hunterSkins)) {
								hunterSkins.Value = "hunter";
								Debug.LogWarning("Hunter skin isn't specified, setting to default");
							} else {
								hunterSkins.Value = ParseSkins(Hunter.hunterSkins) as string;

								if (string.IsNullOrEmpty(hunterSkins.Value)) {
									hunterSkins.Value = "blank";
								}
							}
 
							modelNode.Attributes.Append(hunterSkins);

							// Color
							if (Hunter.modelColor != Color.black) {
								var colorAttr = modelNode.Attributes["Color"]
									?? modelNode.Attributes.Append(modelNode.OwnerDocument.CreateAttribute("Color"));

								byte r = (byte)Mathf.RoundToInt(Hunter.modelColor.r * 255);
								byte g = (byte)Mathf.RoundToInt(Hunter.modelColor.g * 255);
								byte b = (byte)Mathf.RoundToInt(Hunter.modelColor.b * 255);

								if (Hunter.modelColor.a < 1) {
									Debug.LogWarning($"Hunter model color has changed opacity, which is not supported and will be ignored.");
								}

								colorAttr.Value = $"00{r:X2}{g:X2}{b:X2}";
							}
						}

						// Player
						if (modelNode.Attributes["Name"].Value == "Player" || modelNode.Attributes["Name"].Value == Player.playerModelName) {
							// Player model name
							modelNode.Attributes["Name"].Value = Player.playerModelName;

							// Spawn time
							float totalSpawnTime = PlayerHM.playerSpawnTime == -1.0f ? Player.playerSpawnTime : PlayerHM.playerSpawnTime;
							totalSpawnTime += PlayerHM.playerSpawnTimeIncrement;
								
							modelNode.Attributes["Time"].Value = totalSpawnTime.ToString(CultureInfo.InvariantCulture);

							// Life Time
							(modelNode.Attributes["LifeTime"] ?? modelNode.Attributes.Append(modelNode.OwnerDocument.CreateAttribute("LifeTime"))).Value = Player.playerLifeTime.ToString(CultureInfo.InvariantCulture);

							// Spawn
							string HMPlayerSpawn = string.IsNullOrEmpty(PlayerHM.playerAllowedSpawn) ? Player.playerSpawnName : PlayerHM.playerAllowedSpawn;
							(modelNode.Attributes["BirthSpawn"] ?? modelNode.Attributes.Append(modelNode.OwnerDocument.CreateAttribute("BirthSpawn"))).Value = HMPlayerSpawn;

							// Skins
							XmlAttribute playerSkins = xml.CreateAttribute("Skins");

							// Check if hunterSkins are specified
							if (string.IsNullOrEmpty(Player.playerSkins)) {
								playerSkins.Value = "1";
								Debug.LogWarning("Player skin isn't specified, setting to default");
							} else {
								playerSkins.Value = ParseSkins(Player.playerSkins) as string;

								if (string.IsNullOrEmpty(playerSkins.Value)) {
									playerSkins.Value = "blank";
								}
							}

							modelNode.Attributes.Append(playerSkins);

							// Color
							if (Player.modelColor != Color.black) {
								var colorAttr = modelNode.Attributes["Color"]
									?? modelNode.Attributes.Append(modelNode.OwnerDocument.CreateAttribute("Color"));

								byte r = (byte)Mathf.RoundToInt(Player.modelColor.r * 255);
								byte g = (byte)Mathf.RoundToInt(Player.modelColor.g * 255);
								byte b = (byte)Mathf.RoundToInt(Player.modelColor.b * 255);

								if (Player.modelColor.a < 1) {
									Debug.LogWarning($"Player model color has changed opacity, which is not supported and will be ignored.");
								}

								colorAttr.Value = $"00{r:X2}{g:X2}{b:X2}";
							}
						}

						// Helper
						if (modelNode.Attributes["Name"].Value == "Helper" || modelNode.Attributes["Name"].Value == Helper.helperModelName) {
							if (!HelperHM.spawn) {
								modelsNode.RemoveChild(modelNode);
								continue;
							}

							// Helper model name
							modelNode.Attributes["Name"].Value = Helper.helperModelName;

							// Spawn time
							float totalSpawnTime = HelperHM.helperSpawnTime == -1.0f ? Helper.helperSpawnTime : HelperHM.helperSpawnTime;
							totalSpawnTime += HelperHM.helperSpawnTimeIncrement;
								
							modelNode.Attributes["Time"].Value = totalSpawnTime.ToString(CultureInfo.InvariantCulture);

							// Life Time
							(modelNode.Attributes["LifeTime"] ?? modelNode.Attributes.Append(modelNode.OwnerDocument.CreateAttribute("LifeTime"))).Value = Helper.helperLifeTime.ToString(CultureInfo.InvariantCulture);

							// Spawn
							string HMHelperSpawn = string.IsNullOrEmpty(HelperHM.helperAllowedSpawn) ? Helper.helperSpawnName : HelperHM.helperAllowedSpawn;
							(modelNode.Attributes["BirthSpawn"] ?? modelNode.Attributes.Append(modelNode.OwnerDocument.CreateAttribute("BirthSpawn"))).Value = HMHelperSpawn;

							// Skins
							XmlAttribute helperSkins = xml.CreateAttribute("Skins");

							// Check if hunterSkins are specified
							if (string.IsNullOrEmpty(Helper.helperSkins)) {
								helperSkins.Value = "1";
								Debug.LogWarning("Helper skin isn't specified, setting to default");
							} else {
								helperSkins.Value = ParseSkins(Helper.helperSkins) as string;

								if (string.IsNullOrEmpty(helperSkins.Value)) {
									helperSkins.Value = "blank";
								}
							}

							modelNode.Attributes.Append(helperSkins);

							// Color
							if (Helper.modelColor != Color.black) {
								var colorAttr = modelNode.Attributes["Color"]
									?? modelNode.Attributes.Append(modelNode.OwnerDocument.CreateAttribute("Color"));

								byte r = (byte)Mathf.RoundToInt(Helper.modelColor.r * 255);
								byte g = (byte)Mathf.RoundToInt(Helper.modelColor.g * 255);
								byte b = (byte)Mathf.RoundToInt(Helper.modelColor.b * 255);

								if (Helper.modelColor.a < 1) {
									Debug.LogWarning($"Helper model color has changed opacity, which is not supported and will be ignored.");
								}

								colorAttr.Value = $"00{r:X2}{g:X2}{b:X2}";
							}
						}
					}
				}
			}
		}
	}

	void ConvertToBackdrop(XmlNode node, XmlDocument xml, GameObject bdInScene, float FactorAmount) {
		// Debug in log every backdrop it writes
		string objRegex = Regex.Replace(bdInScene.name, globalRegex, string.Empty);
		string objName;

		if (logObjectWriting) {
			Debug.Log("Writing object: " + objRegex, bdInScene);
		}

		// Check if the GameObject has a SpriteRenderer component
		SpriteRenderer spriteRenderer = bdInScene.GetComponent<SpriteRenderer>();

		if (bdInScene.name == "Camera") {
			return;
		}

		// Alternative backdrops scaling
		BuildMap buildMapInstance = FindObjectOfType<BuildMap>();
		Vector3 DefaultPosition = bdInScene.transform.position;
		float positionX = DefaultPosition.x;
		float positionY = DefaultPosition.y;

		if (buildMapInstance != null && buildMapInstance.correctFactorPosition) {
			positionX /= (1 / FactorAmount);
			positionY /= (1 / FactorAmount);
		}

		XmlElement BD_element = null;

		if (spriteRenderer == null) {
			// Create a new node from scratch
			BD_element = xml.CreateElement("Object");

			// Add a name
			BD_element.SetAttribute("Name", objRegex);
		} else {
			// Create a new node from scratch
			BD_element = xml.CreateElement("Image");

			// Add a name based on the sprite or fallback to the regex result
			objName = spriteRenderer.sprite != null ? spriteRenderer.sprite.name : objRegex;
			BD_element.SetAttribute("ClassName", objName);

			// Create Properties and Static elements for additional attributes like rotation and color
			XmlElement propertiesElement = xml.CreateElement("Properties");
			XmlElement staticElement = xml.CreateElement("Static");

			// Check the rotation

			// Normalize to 0-360 degrees
			float rotationAngle = bdInScene.transform.eulerAngles.z % 360;

			bool flipX = spriteRenderer.flipX;
			bool flipY = spriteRenderer.flipY;

			// If exactly one of them is flipped, negate the angle
			if (flipX ^ flipY) {
				rotationAngle = -rotationAngle;
			}

			float imagePosX = positionX * 100;
			float imagePosY = -positionY * 100;

			if ((rotationAngle != 0 && rotationAngle != 360) || flipX || flipY) {
				// Convert the rotation to the Marmalade transformation matrix
				float A, B, C, D, Tx, Ty;
				
				// Get Native resolution of the sprite
				int nativeWidth = spriteRenderer.sprite.texture.width;
				int nativeHeight = spriteRenderer.sprite.texture.height;
				
				// Get the image scale using sprite resolution * local scale
				float imageWidth = nativeWidth * bdInScene.transform.localScale.x;
				float imageHeight = nativeHeight * bdInScene.transform.localScale.y;

				if (rotationAngle == 90) {
					A = 0;
					B = -imageWidth;
					C = imageHeight;
					D = 0;
				} else if (rotationAngle == 180) {
					A = -imageWidth;
					B = 0;
					C = 0;
					D = -imageHeight;
				} else if (rotationAngle == 270) {
					A = 0;
					B = imageWidth;
					C = -imageHeight;
					D = 0;
				} else {
					float radians = rotationAngle * Mathf.Deg2Rad;
					float cosTheta = Mathf.Cos(radians);
					float sinTheta = Mathf.Sin(radians);

					A = imageWidth * cosTheta;
					B = -imageWidth * sinTheta;
					C = imageHeight * sinTheta;
					D = imageHeight * cosTheta;
				}

				// Apply FlipX and FlipY
				if (flipX) {
					A = -A;
					C = -C;
				}

				if (flipY) {
					B = -B;
					D = -D;
				}

				// Compute translation
				float topLeftX = imagePosX + Math.Min(0, A) + Math.Min(0, C);
				float topLeftY = imagePosY + Math.Min(0, B) + Math.Min(0, D);

				Tx = imagePosX - topLeftX;
				Ty = imagePosY - topLeftY;

				// Setting all of the attributes
				BD_element.SetAttribute("X", Math.Round(topLeftX).ToString());
				BD_element.SetAttribute("Y", Math.Round(topLeftY).ToString());
				BD_element.SetAttribute("Width", imageWidth.ToString("0.#####", CultureInfo.InvariantCulture));
				BD_element.SetAttribute("Height", imageHeight.ToString("0.#####", CultureInfo.InvariantCulture));
				BD_element.SetAttribute("NativeX", nativeWidth.ToString(CultureInfo.InvariantCulture));
				BD_element.SetAttribute("NativeY", nativeHeight.ToString(CultureInfo.InvariantCulture));

				// Apply transformation matrix
				XmlElement matrixElement = xml.CreateElement("Matrix");
				matrixElement.SetAttribute("A", A.ToString("0.#####", CultureInfo.InvariantCulture));
				matrixElement.SetAttribute("B", B.ToString("0.#####", CultureInfo.InvariantCulture));
				matrixElement.SetAttribute("C", C.ToString("0.#####", CultureInfo.InvariantCulture));
				matrixElement.SetAttribute("D", D.ToString("0.#####", CultureInfo.InvariantCulture));
				matrixElement.SetAttribute("Tx", Tx.ToString("0.#####", CultureInfo.InvariantCulture));
				matrixElement.SetAttribute("Ty", Ty.ToString("0.#####", CultureInfo.InvariantCulture));

				staticElement.AppendChild(matrixElement);
				propertiesElement.AppendChild(staticElement);
				BD_element.AppendChild(propertiesElement);
			} else {
				Bounds bounds = spriteRenderer.sprite.bounds;
				Vector3 scale = bdInScene.transform.localScale;
				float width = bounds.size.x * 100;
				float height = bounds.size.y * 100;

				BD_element.SetAttribute("X", Engine.Math.GameUnits.Multiply(positionX));
				BD_element.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-positionY));
				BD_element.SetAttribute("Width", (width * scale.x).ToString("0.#####", CultureInfo.InvariantCulture));
				BD_element.SetAttribute("Height", (height * scale.y).ToString("0.#####", CultureInfo.InvariantCulture));

				if (width != width * scale.x) {
					BD_element.SetAttribute("NativeX", width.ToString(CultureInfo.InvariantCulture));
				}

				if (height != height * scale.y) {
					BD_element.SetAttribute("NativeY", height.ToString(CultureInfo.InvariantCulture));
				}
			}

			// Color application
			Color color = spriteRenderer.color;

			// Check if the color is different from white (fully opaque)
			if (
				(color.r != 1.000f || color.g != 1.000f || color.b != 1.000f || color.a != 1.000f)
				&& objName != "v_black"
			) {
				string alphaHex = Mathf.RoundToInt(color.a * 255).ToString("X2");
				string rgbaColor = ColorUtility.ToHtmlStringRGB(color) + alphaHex;

				// Create and set attributes for the color element
				XmlElement colorElement = xml.CreateElement("StartColor");
				colorElement.SetAttribute("Color", $"#{rgbaColor}");

				// Add the color element to the static element
				staticElement.AppendChild(colorElement);
				propertiesElement.AppendChild(staticElement);
				BD_element.AppendChild(propertiesElement);
			}

			// Dynamic Color
			DynamicColor dynamicColor = bdInScene.GetComponent<DynamicColor>();
			if (dynamicColor != null) {
				XmlElement dynamicElement = xml.CreateElement("Dynamic");
				XmlElement transformationElement = xml.CreateElement("Transformation");
				XmlElement colorElementImage = xml.CreateElement("Color");

				// Transformation Name
				transformationElement.SetAttribute("Name", dynamicColor.TransformationName);

				// Set ColorStart (current sprite color) and ColorFinish (target color)
				Color currentColor = spriteRenderer.color;
				string startColorHex = ColorUtility.ToHtmlStringRGB(currentColor) + Mathf.RoundToInt(currentColor.a * 255).ToString("X2");
				string finishColorHex = ColorUtility.ToHtmlStringRGB(dynamicColor.ChangeToColor) + Mathf.RoundToInt(dynamicColor.ChangeToColor.a * 255).ToString("X2");

				colorElementImage.SetAttribute("ColorStart", $"#{startColorHex}");
				colorElementImage.SetAttribute("ColorFinish", $"#{finishColorHex}");

				// Calculate Frames (Duration * 60) or 1 if Duration is 0
				int frames = dynamicColor.Duration > 0 ? Mathf.CeilToInt(dynamicColor.Duration * 60) : 1;
				colorElementImage.SetAttribute("Frames", frames.ToString());

				transformationElement.AppendChild(colorElementImage);
				dynamicElement.AppendChild(transformationElement);
				propertiesElement.AppendChild(dynamicElement);
				BD_element.AppendChild(propertiesElement);
			}
		}

		// -- Writing mode --
		string writeModeValue = "any";

		VectorierWriteMode writeMode = bdInScene.GetComponent<VectorierWriteMode>();

		if (writeMode != null) {
			writeModeValue = writeMode.GetWriteModeValue();
			writeMode.AddWriteModeProperties(xml, BD_element, writeModeValue);
		}

		// Place it into the Object node
		node.FirstChild.AppendChild(BD_element);

		// Apply the modification to the build-map.xml file
		xml.Save(Path.Combine(
			Application.dataPath,
			"XML", "dzip", "level_xml",
			mapToOverride + ".xml"
		));
	}

	void ConvertToImage(XmlNode node, XmlDocument xml, GameObject imageInScene) {
		string objRegex = Regex.Replace(imageInScene.name, globalRegex, string.Empty);
		string objName;

		// Debug in log every image it writes
		if (logObjectWriting) {
			Debug.Log("Writing object: " + objRegex, imageInScene);
		}

		// Skip processing if the GameObject is named "Camera"
		if (imageInScene.name == "Camera") {
			return;
		}

		// Create a new node from scratch
		XmlElement Ielement = xml.CreateElement("Image");

		// Add a name based on the sprite or fallback to the regex result
		SpriteRenderer spriteRenderer = imageInScene.GetComponent<SpriteRenderer>();
		objName = (spriteRenderer != null && spriteRenderer.sprite != null) ? spriteRenderer.sprite.name : objRegex;
		Ielement.SetAttribute("ClassName", objName);

		XmlElement propertiesElement = xml.CreateElement("Properties");
		XmlElement staticElement = xml.CreateElement("Static");

		bool staticElementHasContent = false;

		if (spriteRenderer != null && spriteRenderer.sprite != null) {
			// Check the rotation

			// Normalize to 0-360 degrees
			float rotationAngle = imageInScene.transform.eulerAngles.z % 360;

			float imagePosX = imageInScene.transform.position.x * 100;
			float imagePosY = -imageInScene.transform.position.y * 100;

			bool flipX = spriteRenderer.flipX;
			bool flipY = spriteRenderer.flipY;

			// If exactly one of them is flipped, negate the angle
			if (flipX ^ flipY) {
				rotationAngle = -rotationAngle;
			}

			if ((rotationAngle != 0 && rotationAngle != 360) || flipX || flipY) {
				// Convert the rotation to the Marmalade transformation matrix
				float A, B, C, D, Tx, Ty;
				
				// Get Native resolution of the sprite
				int nativeWidth = spriteRenderer.sprite.texture.width;
				int nativeHeight = spriteRenderer.sprite.texture.height;
				
				// Get the image scale using sprite resolution * local scale
				float imageWidth = nativeWidth * imageInScene.transform.localScale.x;
				float imageHeight = nativeHeight * imageInScene.transform.localScale.y;

				if (rotationAngle == 90) {
					A = 0;
					B = -imageWidth;
					C = imageHeight;
					D = 0;
				} else if (rotationAngle == 180) {
					A = -imageWidth;
					B = 0;
					C = 0;
					D = -imageHeight;
				} else if (rotationAngle == 270) {
					A = 0;
					B = imageWidth;
					C = -imageHeight;
					D = 0;
				} else {
					float radians = rotationAngle * Mathf.Deg2Rad;
					float cosTheta = Mathf.Cos(radians);
					float sinTheta = Mathf.Sin(radians);

					A = imageWidth * cosTheta;
					B = -imageWidth * sinTheta;
					C = imageHeight * sinTheta;
					D = imageHeight * cosTheta;
				}

				// Apply FlipX and FlipY
				if (flipX) {
					A = -A;
					C = -C;
				}

				if (flipY) {
					B = -B;
					D = -D;
				}

				// Compute translation
				float topLeftX = imagePosX + Math.Min(0, A) + Math.Min(0, C);
				float topLeftY = imagePosY + Math.Min(0, B) + Math.Min(0, D);

				Tx = imagePosX - topLeftX;
				Ty = imagePosY - topLeftY;

				// Setting all of the attributes
				Ielement.SetAttribute("X", Math.Round(topLeftX).ToString());
				Ielement.SetAttribute("Y", Math.Round(topLeftY).ToString());
				Ielement.SetAttribute("Width", imageWidth.ToString(CultureInfo.InvariantCulture));
				Ielement.SetAttribute("Height", imageHeight.ToString(CultureInfo.InvariantCulture));
				Ielement.SetAttribute("NativeX", nativeWidth.ToString(CultureInfo.InvariantCulture));
				Ielement.SetAttribute("NativeY", nativeHeight.ToString(CultureInfo.InvariantCulture));

				// Apply transformation matrix
				XmlElement matrixElement = xml.CreateElement("Matrix");
				matrixElement.SetAttribute("A", A.ToString("0.#####", CultureInfo.InvariantCulture));
				matrixElement.SetAttribute("B", B.ToString("0.#####", CultureInfo.InvariantCulture));
				matrixElement.SetAttribute("C", C.ToString("0.#####", CultureInfo.InvariantCulture));
				matrixElement.SetAttribute("D", D.ToString("0.#####", CultureInfo.InvariantCulture));
				matrixElement.SetAttribute("Tx", Tx.ToString("0.#####", CultureInfo.InvariantCulture));
				matrixElement.SetAttribute("Ty", Ty.ToString("0.#####", CultureInfo.InvariantCulture));

				staticElement.AppendChild(matrixElement);
				propertiesElement.AppendChild(staticElement);
				Ielement.AppendChild(propertiesElement);
			} else {
				Bounds bounds = spriteRenderer.sprite.bounds;
				Vector3 scale = imageInScene.transform.localScale;
				float width = bounds.size.x * 100;
				float height = bounds.size.y * 100;

				Ielement.SetAttribute("X", Math.Round(imagePosX).ToString());
				Ielement.SetAttribute("Y", Math.Round(imagePosY).ToString());
				Ielement.SetAttribute("Width", (width * scale.x).ToString(CultureInfo.InvariantCulture));
				Ielement.SetAttribute("Height", (height * scale.y).ToString(CultureInfo.InvariantCulture));

				if (width != width * scale.x) {
					Ielement.SetAttribute("NativeX", width.ToString(CultureInfo.InvariantCulture));
				}

				if (height != height * scale.y) {
					Ielement.SetAttribute("NativeY", height.ToString(CultureInfo.InvariantCulture));
				}
			}

			// Color application
			Color color = spriteRenderer.color;
			if (
				(color.r != 1.000f || color.g != 1.000f || color.b != 1.000f || color.a != 1.000f)
				&& objName != "v_black"
			) {
				string alphaHex = Mathf.RoundToInt(color.a * 255).ToString("X2");
				string rgbaColor = ColorUtility.ToHtmlStringRGB(color) + alphaHex;

				XmlElement colorElement = xml.CreateElement("StartColor");
				colorElement.SetAttribute("Color", $"#{rgbaColor}");

				staticElement.AppendChild(colorElement);
				staticElementHasContent = true;
			}

			if (staticElementHasContent) {
				propertiesElement.AppendChild(staticElement);
			}

			DynamicColor dynamicColor = imageInScene.GetComponent<DynamicColor>();
			DynamicRotate dynamicRotate = imageInScene.GetComponent<DynamicRotate>();
			DynamicSize dynamicSize = imageInScene.GetComponent<DynamicSize>();

			// Check for dynamics
			var dynamicComponents = new Component[] {
				dynamicColor,
				dynamicRotate,
				dynamicSize,
			};

			// Create node if > 0 dynamics present
			XmlElement transformationElement = null;
			if (dynamicComponents.Any(component => component != null)) {
				transformationElement = xml.CreateElement("Transformation");
				// Ielement.SetAttribute("Type", "3");
			}

			// Dynamic Color Handling
			if (dynamicColor != null) {
				XmlElement dynamicElement = xml.CreateElement("Dynamic");
				XmlElement colorElement = xml.CreateElement("Color");

				transformationElement.SetAttribute("Name", dynamicColor.TransformationName);

				Color currentColor = spriteRenderer.color;
				string startColorHex = ColorUtility.ToHtmlStringRGB(currentColor) + Mathf.RoundToInt(currentColor.a * 255).ToString("X2", CultureInfo.InvariantCulture);
				string finishColorHex = ColorUtility.ToHtmlStringRGB(dynamicColor.ChangeToColor) + Mathf.RoundToInt(dynamicColor.ChangeToColor.a * 255).ToString("X3", CultureInfo.InvariantCulture);

				colorElement.SetAttribute("ColorStart", $"#{startColorHex}");
				colorElement.SetAttribute("ColorFinish", $"#{finishColorHex}");

				int frames = dynamicColor.Duration > 0 ? Mathf.CeilToInt(dynamicColor.Duration * 60) : 1;
				colorElement.SetAttribute("Frames", frames.ToString());

				transformationElement.AppendChild(colorElement);
				dynamicElement.AppendChild(transformationElement);
				propertiesElement.AppendChild(dynamicElement);
			}

			// Dynamic Rotate
			if (dynamicRotate != null && (dynamicRotate.Rotation.Angle != 0 && dynamicRotate.Rotation.Duration != 0)) {
				transformationElement.SetAttribute("Name", dynamicRotate.TransformationName);

				int nativeWidth = spriteRenderer.sprite.texture.width;
				int nativeHeight = spriteRenderer.sprite.texture.height;

				float imageWidth = nativeWidth * imageInScene.transform.localScale.x;
				float imageHeight = nativeHeight * imageInScene.transform.localScale.y;

				// Default Top-Left
				string anchorValue = "0|0";
				switch (dynamicRotate.Rotation.Anchor) {
					case DynamicRotate.AnchorPoints.TopRight:
						anchorValue = $"{imageWidth}|0";
						break;
					case DynamicRotate.AnchorPoints.BottomLeft:
						anchorValue = $"0|{imageHeight}";
						break;
					case DynamicRotate.AnchorPoints.BottomRight:
						anchorValue = $"{imageWidth}|{imageHeight}";
						break;
					case DynamicRotate.AnchorPoints.Center:
						anchorValue = $"{imageWidth / 2}|{imageHeight / 2}";
						break;
				}

				XmlElement dynamicElement = xml.CreateElement("Dynamic");

				// Calculate total frames (based on duration)
				int totalFrames = Mathf.CeilToInt(dynamicRotate.Rotation.Duration * 60);
				float totalAngle = -dynamicRotate.Rotation.Angle;

				List<float> frameSteps = new List<float>();

				// Build frame steps [1, 2, 4, 8, ...] without exceeding totalFrames
				if (dynamicRotate.Rotation.Linear) {
					frameSteps.Add(totalAngle);
				} else {
					int current = 1;
					int used = 0;
					while (used + current <= totalFrames) {
						frameSteps.Add(current);
						used += current;
						current *= 2;
					}

					// If frames are left over, add as last step
					if (used < totalFrames) {
						frameSteps.Add(totalFrames - used);
					}
				}

				// Divide total angle across the number of steps
				float anglePerStep = totalAngle / frameSteps.Count;

				foreach (int frameCount in frameSteps) {
					XmlElement rotationElement = xml.CreateElement("Rotation");
					rotationElement.SetAttribute("Angle", anglePerStep.ToString("0.######", CultureInfo.InvariantCulture));
					rotationElement.SetAttribute("Anchor", anchorValue.Replace(',', '.'));
					rotationElement.SetAttribute("Frames", (frameSteps.Count < 2 ? totalFrames : frameCount).ToString());

					transformationElement.AppendChild(rotationElement);
				}

				dynamicElement.AppendChild(transformationElement);
				propertiesElement.AppendChild(dynamicElement);
				Ielement.AppendChild(propertiesElement);
			}

			// Dynamic Size
			if (dynamicSize != null) {
				XmlElement dynamicElement = xml.CreateElement("Dynamic");
				XmlElement sizeElement = xml.CreateElement("Size");
				transformationElement.SetAttribute("Name", dynamicSize.TransformationName);

				int frames = dynamicSize.Size.MoveDuration > 0 ? Mathf.CeilToInt(dynamicSize.Size.MoveDuration * 60) : 1;
				sizeElement.SetAttribute("Frames", frames.ToString());
				sizeElement.SetAttribute("FinalWidth", dynamicSize.Size.FinalWidth.ToString());
				sizeElement.SetAttribute("FinalHeight", dynamicSize.Size.FinalHeight.ToString());
				
				transformationElement.AppendChild(sizeElement);
				dynamicElement.AppendChild(transformationElement);
				propertiesElement.AppendChild(dynamicElement);
				Ielement.AppendChild(propertiesElement);
			}
		}

		if (propertiesElement.HasChildNodes) {
			Ielement.AppendChild(propertiesElement);
		}

		// Writing mode
		string writeModeValue = "any";
		VectorierWriteMode writeMode = imageInScene.GetComponent<VectorierWriteMode>();

		if (writeMode != null) {
			writeModeValue = writeMode.GetWriteModeValue();
			writeMode.AddWriteModeProperties(xml, Ielement, writeModeValue);
		}

		// Place it into the Object node
		node.FirstChild.AppendChild(Ielement);

		// Apply the modification to the build-map.xml file
		xml.Save(Path.Combine(
			Application.dataPath,
			"XML", "dzip", "level_xml",
			mapToOverride + ".xml"
		));
	}

	void ConvertToModel(XmlNode node, XmlDocument xml, GameObject modelInScene) {
		if (modelInScene.name == "Camera") {
			return;
		}

		ModelProperties modelProperties = modelInScene.GetComponent<ModelProperties>();

		// Check if ModelProperties is not null
		if (modelProperties == null) {
			Debug.LogWarning($@"ModelProperties component is missing on ""{modelInScene.name}""", modelInScene);
			return;
		}

		// Create a new node from scratch
		XmlElement Melement = xml.CreateElement("Model");

		// Add a name
		Melement.SetAttribute("ClassName", Regex.Replace(modelInScene.name, globalRegex, string.Empty));

		// Add X position (Refit into the Vector units)
		Melement.SetAttribute("X", Engine.Math.GameUnits.Multiply(modelInScene.transform.position.x));

		// Add Y position (Negative because Vector see the world upside down)
		Melement.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-modelInScene.transform.position.y));

		// Add model type
		Melement.SetAttribute("Type", modelProperties.Type.ToString());

		// Conditionally add LifeTime attribute if applicable
		if (modelProperties.UseLifeTime) {
			// Add LifeTime attribute
			Melement.SetAttribute("LifeTime", modelProperties.LifeTime);
		}

		// -- Writing mode --
		string writeModeValue = "any";

		VectorierWriteMode writeMode = modelInScene.GetComponent<VectorierWriteMode>();

		if (writeMode != null) {
			writeModeValue = writeMode.GetWriteModeValue();
			writeMode.AddWriteModeProperties(xml, Melement, writeModeValue);
		}

		// Place it into the Object node
		node.FirstChild.AppendChild(Melement);

		// Apply the modification to the build-map.xml file
		xml.Save(Path.Combine(
			Application.dataPath,
			"XML", "dzip", "level_xml",
			mapToOverride + ".xml"
		));
	}

	void ConvertToObject(XmlNode node, XmlDocument xml, GameObject objectInScene) {
		string objRegex = Regex.Replace(objectInScene.name, globalRegex, string.Empty);

		// Debug in log every object it writes
		if (logObjectWriting) {
			Debug.Log("Writing object: " + objRegex, objectInScene);
		}

		bool isCamera = objRegex == "Camera";

		// Create new node from scratch
		XmlElement O_element = xml.CreateElement(isCamera ? "Camera" : "Object");

		// Set a name
		O_element.SetAttribute("Name", isCamera ? "" : objRegex);

		if (isCamera) {
			float cameraDefaultZoom169 = 137.2238f;

			Camera camera = objectInScene.GetComponent<Camera>();
			O_element.SetAttribute(
				"Zoom",
				Math.Round(cameraDefaultZoom169 / camera.fieldOfView, 3).ToString(CultureInfo.InvariantCulture)
			);
		}

		if (objectInScene.transform.position.x != 0) {
			O_element.SetAttribute("X", Engine.Math.GameUnits.Multiply(objectInScene.transform.position.x));
		}

		if (objectInScene.transform.position.y != 0) {
			O_element.SetAttribute("Y",
				Engine.Math.GameUnits.Multiply(
					-objectInScene.transform.position.y,
					LayerMask.LayerToName(objectInScene.layer).ToLower().Contains("round y pos")
				)
			);
		}

		DynamicSize dynamicSize = objectInScene.GetComponent<DynamicSize>();

		// Check for dynamics
		var dynamicComponents = new Component[] {
			dynamicSize
		};

		// Create node if > 0 dynamics present
		XmlElement transformationElement = null;
		XmlElement propertiesElement = null;

		if (dynamicComponents.Any(component => component != null)) {
			propertiesElement = xml.CreateElement("Properties");
			transformationElement = xml.CreateElement("Transformation");
		}

		// -- Writing mode --
		string writeModeValue = "any";

		VectorierWriteMode writeMode = objectInScene.GetComponent<VectorierWriteMode>();

		if (writeMode != null) {
			writeModeValue = writeMode.GetWriteModeValue();
			writeMode.AddWriteModeProperties(xml, O_element, writeModeValue);
		}

		// Place it into the Object node
		node.FirstChild.AppendChild(O_element);

		// Save the XML document
		xml.Save(Path.Combine(
			Application.dataPath,
			"XML", "dzip", "level_xml",
			mapToOverride + ".xml"
		));
	}

	void ConvertToPlatform(XmlNode node, XmlDocument xml, GameObject platformInScene) {
		string objRegex = Regex.Replace(platformInScene.name, globalRegex, string.Empty);

		if (logObjectWriting) {
			Debug.Log("Writing object: " + objRegex, platformInScene);
		}

		if (platformInScene.name == "Camera") {
			return;
		}

		SpriteRenderer spriteRenderer = platformInScene.GetComponent<SpriteRenderer>();

		if (spriteRenderer == null || spriteRenderer.sprite == null) {
			return;
		}

		XmlElement P_element = xml.CreateElement("Platform");

		P_element.SetAttribute("X", Engine.Math.GameUnits.Multiply(platformInScene.transform.position.x));
		P_element.SetAttribute("Y",
			Engine.Math.GameUnits.Multiply(
				-platformInScene.transform.position.y,
				LayerMask.LayerToName(platformInScene.layer).ToLower().Contains("round y pos")
			)
		);

		Bounds bounds = spriteRenderer.sprite.bounds;
		Vector3 scale = platformInScene.transform.localScale;

		float width = bounds.size.x * 100;
		float height = bounds.size.y * 100;

		P_element.SetAttribute("Width", (width * scale.x).ToString("0.######", CultureInfo.InvariantCulture));
		P_element.SetAttribute("Height", (height * scale.y).ToString("0.######", CultureInfo.InvariantCulture));

		// -- Writing mode --
		string writeModeValue = "any";

		VectorierWriteMode writeMode = platformInScene.GetComponent<VectorierWriteMode>();

		if (writeMode != null) {
			writeModeValue = writeMode.GetWriteModeValue();
			writeMode.AddWriteModeProperties(xml, P_element, writeModeValue);
		}

		// Place it into the Object node
		node.FirstChild.AppendChild(P_element);

		// Apply the modification to the build-map.xml file
		xml.Save(Path.Combine(
			Application.dataPath,
			"XML", "dzip", "level_xml",
			mapToOverride + ".xml"
		));
	}

	void ConvertToTrapezoid(XmlNode node, XmlDocument xml, GameObject trapezoidInScene) {
		string objRegex = Regex.Replace(trapezoidInScene.name, globalRegex, string.Empty);

		// Debug in log every platform it writes
		if (logObjectWriting) {
			Debug.Log("Writing object: " + objRegex, trapezoidInScene);
		}

		// Return if it's not the expected trapezoid type
		if (objRegex != "trapezoid_type1" && objRegex != "trapezoid_type2") {
			return;
		}

		// Get the GameObject scale
		Vector3 scale = trapezoidInScene.transform.localScale;
		Vector3 position = trapezoidInScene.transform.position;

		if (scale.x != scale.y) {
			Debug.LogWarning($@"Trapezoid-tagged GameObject named ""{trapezoidInScene.name}"" doesn't have proportional scaling", trapezoidInScene);
		}

		SpriteRenderer spriteRenderer = trapezoidInScene.GetComponent<SpriteRenderer>();

		// Create a new node from scratch
		XmlElement T_element = xml.CreateElement("Trapezoid");

		bool origType1 = objRegex == "trapezoid_type1";

		// Get the Sprite Size in Width and Height
		if (spriteRenderer != null && spriteRenderer.sprite != null) {
			Bounds bounds = spriteRenderer.sprite.bounds;

			float width = bounds.size.x * 100;
			float height = bounds.size.y * 100;

			float adjustedWidth = width * scale.x;
			float adjustedHeight = height * scale.y;

			string finalX;
			string finalY;

			bool isFlipped = spriteRenderer.flipX;
			bool actualType1 = origType1 ^ isFlipped;

			if (isFlipped) {
				float x = (100 * position.x) - adjustedWidth;
				float y = (-position.y * 100) + (actualType1 ? adjustedHeight : -adjustedHeight);

				finalX = x.ToString("0.###", CultureInfo.InvariantCulture);
				finalY = y.ToString("0.###", CultureInfo.InvariantCulture);
			} else {
				finalX = Engine.Math.GameUnits.Multiply(position.x);
				finalY = Engine.Math.GameUnits.Multiply(-position.y);
			}

			T_element.SetAttribute("X", finalX);
			T_element.SetAttribute("Y", finalY);
			T_element.SetAttribute("Width", (width * scale.x).ToString(CultureInfo.InvariantCulture));

			if (actualType1) {
				T_element.SetAttribute("Height", "1");
				T_element.SetAttribute("Height1", (height * scale.y + 1).ToString(CultureInfo.InvariantCulture));
				T_element.SetAttribute("Type", "1");
			} else {
				T_element.SetAttribute("Height", (height * scale.y + 1).ToString(CultureInfo.InvariantCulture));
				T_element.SetAttribute("Height1", "1");
				T_element.SetAttribute("Type", "2");
			}
		}

		// -- Writing mode --
		string writeModeValue = "any";

		VectorierWriteMode writeMode = trapezoidInScene.GetComponent<VectorierWriteMode>();

		if (writeMode != null) {
			writeModeValue = writeMode.GetWriteModeValue();
			writeMode.AddWriteModeProperties(xml, T_element, writeModeValue);
		}

		// Place it into the Object node
		node.FirstChild.AppendChild(T_element);

		// Apply the modification to the build-map.xml file
		xml.Save(Path.Combine(
			Application.dataPath,
			"XML", "dzip", "level_xml",
			mapToOverride + ".xml"
		));
	}

	void ConvertToTrigger(XmlNode node, XmlDocument xml, GameObject triggerInScene) {
		string objRegex = Regex.Replace(triggerInScene.name, globalRegex, string.Empty);

		// Debug in log every trigger it writes
		if (logObjectWriting) {
			Debug.Log("Writing object: " + objRegex, triggerInScene);
		}

		// Skip if the object is a Camera
		if (triggerInScene.name == "Camera") {
			return;
		}

		// Determine if the trigger has a settings component
		TriggerSettings triggerSettings = triggerInScene.GetComponent<TriggerSettings>();
		DynamicTrigger dynamicTrigger = triggerInScene.GetComponent<DynamicTrigger>();

		if (triggerSettings && dynamicTrigger) {
			Debug.LogError($"GameObject '{triggerInScene.name}' cannot contain both TriggerSetting and DynamicTrigger.", triggerInScene);
		} else if (!triggerSettings && !dynamicTrigger) {
			Debug.LogError($"GameObject '{triggerInScene.name}' must contain at least TriggerSetting or DynamicTrigger.", triggerInScene);
		}

		XmlElement T_element = xml.CreateElement("Trigger");

		T_element.SetAttribute("Name", objRegex);
		T_element.SetAttribute("X", Engine.Math.GameUnits.Multiply(triggerInScene.transform.position.x));
		T_element.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-triggerInScene.transform.position.y));

		SpriteRenderer spriteRenderer = triggerInScene.GetComponent<SpriteRenderer>();

		if (spriteRenderer != null && spriteRenderer.sprite != null) {
			Bounds bounds = spriteRenderer.sprite.bounds;
			Vector3 scale = triggerInScene.transform.localScale;

			float width = bounds.size.x * 100;
			float height = bounds.size.y * 100;

			T_element.SetAttribute("Width", (width * scale.x).ToString(CultureInfo.InvariantCulture));
			T_element.SetAttribute("Height", (height * scale.y).ToString(CultureInfo.InvariantCulture));
		}

		if (dynamicTrigger != null) {
			XmlElement initElement = xml.CreateElement("Init");

			// Add SetVariable elements to Init
			XmlElement setVariable1 = xml.CreateElement("SetVariable");
			setVariable1.SetAttribute("Name", "$Active");
			setVariable1.SetAttribute("Value", "1");
			initElement.AppendChild(setVariable1);

			XmlElement setVariable2 = xml.CreateElement("SetVariable");
			setVariable2.SetAttribute("Name", "$AI");
			setVariable2.SetAttribute("Value", dynamicTrigger.AIAllowed.ToString());
			initElement.AppendChild(setVariable2);

			XmlElement setVariable3 = xml.CreateElement("SetVariable");
			setVariable3.SetAttribute("Name", "$Node");
			setVariable3.SetAttribute("Value", string.IsNullOrEmpty(dynamicTrigger.modelNode) ? "COM" : dynamicTrigger.modelNode);
			initElement.AppendChild(setVariable3);

			string[] splittedAnimations = dynamicTrigger.Animations.Split('|');
			if (!string.IsNullOrEmpty(dynamicTrigger.Animations)) {
				bool multiple = splittedAnimations.Length > 1;
				int animationCounter = 1;

				foreach (string animName in splittedAnimations) {
					XmlElement animationNamesNode = xml.CreateElement("SetVariable");

					animationNamesNode.SetAttribute("Name", (multiple ? animationCounter.ToString() + "_" : "") + "ReqAnim");
					animationNamesNode.SetAttribute("Value", animName);

					initElement.AppendChild(animationNamesNode);
					animationCounter++;
				}
			}

			if (dynamicTrigger.PlaySound) {
				XmlElement setVariable4 = xml.CreateElement("SetVariable");
				setVariable4.SetAttribute("Name", "Sound");
				setVariable4.SetAttribute("Value", dynamicTrigger.Sound);
				initElement.AppendChild(setVariable4);
			}

			XmlElement setVariable5 = xml.CreateElement("SetVariable");
			setVariable5.SetAttribute("Name", "Flag1");
			setVariable5.SetAttribute("Value", "0");
			initElement.AppendChild(setVariable5);

			XmlElement triggerContentElement = xml.CreateElement("Content");
			triggerContentElement.AppendChild(initElement);

			XmlElement loopElement = xml.CreateElement("Loop");

			// Create Events element and EventBlock element
			XmlElement eventsElement = xml.CreateElement("Events");

			// Enter or exit
			XmlElement eventBlock_entry = xml.CreateElement("EventBlock");

			eventBlock_entry.SetAttribute("Template", "FreqUsed." + dynamicTrigger.EventType.ToString());

			eventsElement.AppendChild(eventBlock_entry);

			if (!string.IsNullOrEmpty(dynamicTrigger.Animations)) {
				XmlElement eventBlock_ReqAnim = xml.CreateElement("EventBlock");

				eventBlock_ReqAnim.SetAttribute("Template", "CommonLib.RequiredAnimation");

				eventsElement.AppendChild(eventBlock_ReqAnim);
			}

			// Append Events to Loop
			loopElement.AppendChild(eventsElement);

			if (!string.IsNullOrEmpty(dynamicTrigger.Animations)) {
				XmlElement conditionsElement = xml.CreateElement("Conditions");

				XmlElement conditionBlock = xml.CreateElement("ConditionBlock");
				conditionBlock.SetAttribute("Template", "CommonLib.RequiredAnimation");
				conditionsElement.AppendChild(conditionBlock);

				if (splittedAnimations.Length > 1) {
					XmlElement conditionsOperatorElement = xml.CreateElement("Operator");
					conditionsOperatorElement.SetAttribute("Type", "Or");

					int animationCounter = 1;
					foreach (string name in splittedAnimations) {
						XmlElement conditionBlock_reqAnim = xml.CreateElement("ConditionBlock");
		
						conditionBlock_reqAnim.SetAttribute("Template", "CommonLib.RequiredAnimation");
						conditionBlock_reqAnim.SetAttribute("Prefix", $"{animationCounter}_");

						conditionsOperatorElement.AppendChild(conditionBlock_reqAnim);
						animationCounter++;
					}

					conditionsElement.AppendChild(conditionsOperatorElement);
				}

				loopElement.AppendChild(conditionsElement);
			}

			// Create Actions element and ActionBlock element
			XmlElement actionsElement = xml.CreateElement("Actions");

			string randInt = UnityEngine.Random.Range((int)1E8, (int)1E9 - 1).ToString();

			if (dynamicTrigger.MultipleTransformation) {
				XmlElement chooseElement = xml.CreateElement("Choose");
				chooseElement.SetAttribute("Order", dynamicTrigger.Order.ToString());
				chooseElement.SetAttribute("Set", dynamicTrigger.Set.ToString());

				foreach (string transformationName in dynamicTrigger.TransformationNames) {
					XmlElement transformElement = xml.CreateElement("Transform");
					transformElement.SetAttribute("Name", transformationName);
					chooseElement.AppendChild(transformElement);
				}

				actionsElement.AppendChild(chooseElement);
			} else {
				XmlElement transformElement = xml.CreateElement("Transform");
				string tr_name = dynamicTrigger.TriggerTransformName;

				if (string.IsNullOrEmpty(tr_name)) {
					tr_name = randInt;
				}

				transformElement.SetAttribute("Name", tr_name);
				actionsElement.AppendChild(transformElement);
			}

			if (dynamicTrigger.PlaySound) {
				// Create ActionsBlock for sound
				XmlElement actionBlockSoundElement = xml.CreateElement("ActionBlock");
				actionBlockSoundElement.SetAttribute("Template", "CommonLib.Sound");
				actionsElement.AppendChild(actionBlockSoundElement);

				if (dynamicTrigger.Latency > 0f) {
					XmlElement waitAttr = xml.CreateElement("Wait");
					waitAttr.SetAttribute("Frames", Math.Round(dynamicTrigger.Latency * 60).ToString());
					actionsElement.AppendChild(waitAttr);
				}
			}

			if (!dynamicTrigger.Reusable) {
				XmlElement actionBlockElement = xml.CreateElement("SetVariable");
				actionBlockElement.SetAttribute("Name", "$Active");
				actionBlockElement.SetAttribute("Value", "0");
				actionsElement.AppendChild(actionBlockElement);
			}

			// Append Actions to Loop
			loopElement.AppendChild(actionsElement);

			// Append Loop to Trigger
			triggerContentElement.AppendChild(loopElement);

			// Append Content to Trigger
			T_element.AppendChild(triggerContentElement);
		}

		if (triggerSettings != null) {
			XmlElement contentElement = xml.CreateElement("Content");

			// XML does not format correctly, so we load them into a separate doc
			XmlDocument tempDoc = new XmlDocument();
			tempDoc.LoadXml("<Content>" + triggerSettings.Content + "</Content>");

			foreach (XmlNode childNode in tempDoc.DocumentElement.ChildNodes) {
				XmlNode importedNode = xml.ImportNode(childNode, true);
				contentElement.AppendChild(importedNode);
			}

			T_element.AppendChild(contentElement);
		}

		// -- Writing mode --
		string writeModeValue = "any";

		VectorierWriteMode writeMode = triggerInScene.GetComponent<VectorierWriteMode>();

		if (writeMode != null) {
			writeModeValue = writeMode.GetWriteModeValue();
			writeMode.AddWriteModeProperties(xml, T_element, writeModeValue);
		}

		// Place the trigger into the Object node
		node.FirstChild.AppendChild(T_element);

		// Apply the modification to the build-map.xml with proper format
		XmlWriterSettings settings = new XmlWriterSettings {
			Indent = true,
			IndentChars = "	 ",
			NewLineChars = "\r\n",
			NewLineHandling = NewLineHandling.Replace
		};

		using (XmlWriter writer = XmlWriter.Create(
			Path.Combine(
				Application.dataPath,
				"XML", "dzip", "level_xml",
				mapToOverride + ".xml"
			),
			settings)
		) {
			xml.Save(writer);
		}
	}

	void ConvertToItem(XmlNode node, XmlDocument xml, GameObject itemInScene) {
		string objRegex = Regex.Replace(itemInScene.name, globalRegex, string.Empty);

		// Debug in log every Area it writes
		if (logObjectWriting) {
			Debug.Log("Writing object: " + objRegex, itemInScene);
		}

		// Skip if the object is a Camera
		if (itemInScene.name == "Camera") {
			return;
		}

		ItemProperties itemProperties = itemInScene.GetComponent<ItemProperties>();
		XmlElement ItemElement = xml.CreateElement("Item");

		// Add X position (Refit into the Vector units)
		ItemElement.SetAttribute("X", Engine.Math.GameUnits.Multiply(itemInScene.transform.position.x));
		ItemElement.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-itemInScene.transform.position.x));

		if (itemProperties != null) {
			int _ItemType = itemProperties.Type.ToString().ToLower().StartsWith("coin") ? 1 : 0;
			int _ItemScore = itemProperties.Score;
			int _ItemGroup = itemProperties.GroupID;

			if (_ItemType == 0) {
				_ItemScore = 100;
			}

			if (_ItemGroup == -1 && _ItemType == 1) {
				_ItemGroup = UnityEngine.Random.Range((int)1E8, (int)1E9 - 1);
			}

			// Set properties from ItemProperties
			ItemElement.SetAttribute("Type", _ItemType.ToString());
			ItemElement.SetAttribute("Score", _ItemScore.ToString());
			ItemElement.SetAttribute("Radius", itemProperties.Radius.ToString(CultureInfo.InvariantCulture));
			if (_ItemType != 0) {
				ItemElement.SetAttribute("GroupId", _ItemGroup.ToString());
			}
		} else {
			Debug.LogError($@"No item properties in ""Item"" tagged ""{objRegex}"" GameObject", itemInScene);
			return;
		}

		// -- Writing mode --
		string writeModeValue = "any";

		VectorierWriteMode writeMode = itemInScene.GetComponent<VectorierWriteMode>();

		if (writeMode != null) {
			writeModeValue = writeMode.GetWriteModeValue();
			writeMode.AddWriteModeProperties(xml, ItemElement, writeModeValue);
		}

		// Append the area node to the XML
		node.FirstChild.AppendChild(ItemElement);

		// Apply the modification to the build-map.xml file
		xml.Save(Path.Combine(
			Application.dataPath,
			"XML", "dzip", "level_xml",
			mapToOverride + ".xml"
		));
	}

	void ConvertToArea(XmlNode node, XmlDocument xml, GameObject areaInScene) {
		string objRegex = Regex.Replace(areaInScene.name, globalRegex, string.Empty);

		// Debug in log every Area it writes
		if (logObjectWriting) {
			Debug.Log("Writing object: " + objRegex, areaInScene);
		}

		// Skip if the object is a Camera
		if (areaInScene.name == "Camera") {
			return;
		}

		// Create a new node for the area
		XmlElement A_element = xml.CreateElement("Area");

		// Add a name to the area
		A_element.SetAttribute("Name", objRegex);

		// Add position (Refit into the Vector units)
		A_element.SetAttribute("X", Engine.Math.GameUnits.Multiply(areaInScene.transform.position.x));
		A_element.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-areaInScene.transform.position.y));

		SpriteRenderer spriteRenderer = areaInScene.GetComponent<SpriteRenderer>();

		// Set width and height based on the sprite's size and the object's scale
		if (spriteRenderer != null && spriteRenderer.sprite != null) {
			// Get properties
			Bounds bounds = spriteRenderer.sprite.bounds;
			Vector3 scale = areaInScene.transform.localScale;

			// Calculate width and height based on the sprite's bounds and scale
			float width = bounds.size.x * 100;
			float height = bounds.size.y * 100;

			// Set the width and height accordingly to the scale in the editor
			A_element.SetAttribute("Width", (width * scale.x).ToString(CultureInfo.InvariantCulture));
			A_element.SetAttribute("Height", (height * scale.y).ToString(CultureInfo.InvariantCulture));
		}

		// Catch handling
		if (areaInScene.name == "TriggerCatch" || areaInScene.name == "TriggerCatchFront" || areaInScene.name == "TriggerCatchFast") {
			A_element.SetAttribute("Type", "Catch");
		} else {
			A_element.SetAttribute("Type", "Animation");
		}

		// Set distance
		if (areaInScene.name == "TriggerCatch" || areaInScene.name == "TriggerCatchFront") {
			A_element.SetAttribute("Distance", Engine.Math.GameUnits.Multiply(Hunter.hunterCatchDistance));
		} else if (areaInScene.name == "TriggerCatchFast") {
			A_element.SetAttribute("Distance", "0");
		}

		// -- Writing mode --
		string writeModeValue = "any";

		VectorierWriteMode writeMode = areaInScene.GetComponent<VectorierWriteMode>();

		if (writeMode != null) {
			writeModeValue = writeMode.GetWriteModeValue();
			writeMode.AddWriteModeProperties(xml, A_element, writeModeValue);
		}

		// Append the area node to the XML
		node.FirstChild.AppendChild(A_element);

		// Apply the modification to the build-map.xml file
		xml.Save(Path.Combine(
			Application.dataPath,
			"XML", "dzip", "level_xml",
			mapToOverride + ".xml"
		));
	}

	void ConvertToCamera(XmlNode node, XmlDocument xml, GameObject camInScene) {
		// Important Note: If the specific TriggerZoom already exists in the object.xml, no need to tag those as Camera. Instead, tag it as an Object!
		string objRegex = Regex.Replace(camInScene.name, globalRegex, string.Empty);

		// Debug log for every camera object being processed
		if (logObjectWriting) {
			Debug.Log("Writing object: " + objRegex, camInScene);
		}

		// Skip processing if the GameObject is named "Camera"
		if (camInScene.name == "Camera") {
			return;
		}

		SpriteRenderer spriteRenderer = camInScene.GetComponent<SpriteRenderer>();
		CustomZoom customZoomValue = camInScene.GetComponent<CustomZoom>();

		if (spriteRenderer == null || customZoomValue == null) {
			Debug.LogWarning("SpriteRenderer or CustomZoom component is missing on " + camInScene.name, camInScene);
			// Exit if necessary components are not found
			return;
		}

		// Calculate sprite dimensions and scaling
		Bounds bounds = spriteRenderer.sprite.bounds;
		Vector3 scale = camInScene.transform.localScale;

		float width = bounds.size.x * 100;
		float height = bounds.size.y * 100;

		// Create XML elements
		XmlElement triggerElement = xml.CreateElement("Trigger");
		triggerElement.SetAttribute("Name", Regex.Replace(camInScene.name, globalRegex, string.Empty));
		triggerElement.SetAttribute("X", Engine.Math.GameUnits.Multiply(camInScene.transform.position.x));
		triggerElement.SetAttribute("Y", Engine.Math.GameUnits.Multiply(-camInScene.transform.position.y));
		triggerElement.SetAttribute("Width", (width * scale.x).ToString(CultureInfo.InvariantCulture));
		triggerElement.SetAttribute("Height", (height * scale.y).ToString(CultureInfo.InvariantCulture));

		// -- Writing mode --
		string writeModeValue = "any";

		VectorierWriteMode writeMode = camInScene.GetComponent<VectorierWriteMode>();

		if (writeMode != null) {
			writeModeValue = writeMode.GetWriteModeValue();
			writeMode.AddWriteModeProperties(xml, triggerElement, writeModeValue);
		}

		// -=-=-=- //

		// Initialize Content and Init elements
		XmlElement contentElement = xml.CreateElement("Content");
		XmlElement initElement = xml.CreateElement("Init");

		// Variable names and values for trigger initialization
		string[] variableNames = { "$Active", "$Node", "Zoom", "$AI", "Flag1" };
		string[] variableValues = { "1", "COM", customZoomValue.ZoomAmount.ToString(), "0", "0" };

		// Create SetVariable elements
		for (int i = 0; i < variableNames.Length; i++) {
			XmlElement setVariableElement = xml.CreateElement("SetVariable");
			setVariableElement.SetAttribute("Name", variableNames[i]);
			setVariableElement.SetAttribute("Value", variableValues[i]);
			initElement.AppendChild(setVariableElement);
		}

		// Create Template element
		XmlElement templateElement = xml.CreateElement("Template");
		templateElement.SetAttribute("Name", "CameraZoom");

		// Append elements to construct the XML structure
		contentElement.AppendChild(initElement);
		contentElement.AppendChild(templateElement);
		triggerElement.AppendChild(contentElement);
		node.FirstChild.AppendChild(triggerElement);

		// Save the modifications to the XML file
		xml.Save(Path.Combine(
			Application.dataPath,
			"XML", "dzip", "level_xml",
			mapToOverride + ".xml"
		));
	}

	void ConvertToDynamic(XmlNode node, XmlDocument xml, GameObject dynamicInScene, UnityEngine.Transform dynamicInSceneTransform) {
		var buildMap = FindObjectOfType<BuildMap>();

		// Dynamic component in the hierarchy
		Dynamic dynamicComponent = dynamicInScene.GetComponent<Dynamic>();

		// Object
		XmlElement objectElement = xml.CreateElement("Object");
		objectElement.SetAttribute("X", "0");
		objectElement.SetAttribute("Y", "0");

		// Properties
		XmlElement propertiesElement = xml.CreateElement("Properties");

		// Dynamic
		XmlElement dynamicElement = xml.CreateElement("Dynamic");

		// Create Transformation element
		XmlElement transformationElement = xml.CreateElement("Transformation");
		transformationElement.SetAttribute("Name", dynamicComponent.TransformationName);

		// Create Move element
		XmlElement moveElement = xml.CreateElement("Move");

		// Move Intervals array
		var moveIntervals = new[] {
			new { UseMovement = dynamicComponent.MovementUsage.UseMovement1, Interval = dynamicComponent.MoveInterval1, Number = "1" },
			new { UseMovement = dynamicComponent.MovementUsage.UseMovement2, Interval = dynamicComponent.MoveInterval2, Number = "2" },
			new { UseMovement = dynamicComponent.MovementUsage.UseMovement3, Interval = dynamicComponent.MoveInterval3, Number = "3" },
			new { UseMovement = dynamicComponent.MovementUsage.UseMovement4, Interval = dynamicComponent.MoveInterval4, Number = "4" },
			new { UseMovement = dynamicComponent.MovementUsage.UseMovement5, Interval = dynamicComponent.MoveInterval5, Number = "5" },
			new { UseMovement = dynamicComponent.MovementUsage.UseMovement6, Interval = dynamicComponent.MoveInterval6, Number = "6" },
			new { UseMovement = dynamicComponent.MovementUsage.UseMovement7, Interval = dynamicComponent.MoveInterval7, Number = "7" },
			new { UseMovement = dynamicComponent.MovementUsage.UseMovement8, Interval = dynamicComponent.MoveInterval8, Number = "8" },
			new { UseMovement = dynamicComponent.MovementUsage.UseMovement9, Interval = dynamicComponent.MoveInterval9, Number = "9" },
			new { UseMovement = dynamicComponent.MovementUsage.UseMovement10, Interval = dynamicComponent.MoveInterval10, Number = "10" },
			new { UseMovement = dynamicComponent.MovementUsage.UseMovement11, Interval = dynamicComponent.MoveInterval11, Number = "11" },
			new { UseMovement = dynamicComponent.MovementUsage.UseMovement12, Interval = dynamicComponent.MoveInterval12, Number = "12" },
			new { UseMovement = dynamicComponent.MovementUsage.UseMovement13, Interval = dynamicComponent.MoveInterval13, Number = "13" },
			new { UseMovement = dynamicComponent.MovementUsage.UseMovement14, Interval = dynamicComponent.MoveInterval14, Number = "14" },
			new { UseMovement = dynamicComponent.MovementUsage.UseMovement15, Interval = dynamicComponent.MoveInterval15, Number = "15" },
			new { UseMovement = dynamicComponent.MovementUsage.UseMovement16, Interval = dynamicComponent.MoveInterval16, Number = "16" }
		};

		// Loop through each move interval
		foreach (var moveInterval in moveIntervals) {
			if (moveInterval.UseMovement) {
				XmlElement moveIntervalElement = xml.CreateElement("MoveInterval");
				moveIntervalElement.SetAttribute("Number", moveInterval.Number);

				// Ensure at least 1 frame
				int framesToMove = Mathf.Max(1, Mathf.RoundToInt(moveInterval.Interval.MoveDuration * 60));

				// Multiply by 60 frames per second
				int delayFrames = Mathf.RoundToInt(moveInterval.Interval.Delay * 60);
				moveIntervalElement.SetAttribute("FramesToMove", framesToMove.ToString());
				moveIntervalElement.SetAttribute("Delay", delayFrames.ToString());

				// Create Points (Start, Support, Finish)
				XmlElement startPointElement = xml.CreateElement("Point");
				startPointElement.SetAttribute("Name", "Start");
				startPointElement.SetAttribute("X", "0.0");
				startPointElement.SetAttribute("Y", "0.0");

				XmlElement supportPointElement = xml.CreateElement("Point");
				supportPointElement.SetAttribute("Name", "Support");
				supportPointElement.SetAttribute("Number", moveInterval.Number);
				supportPointElement.SetAttribute("X", (moveInterval.Interval.SupportXAxis * 100).ToString("0.###", CultureInfo.InvariantCulture));
				supportPointElement.SetAttribute("Y", (-moveInterval.Interval.SupportYAxis * 100).ToString("0.###", CultureInfo.InvariantCulture));

				XmlElement finishPointElement = xml.CreateElement("Point");
				finishPointElement.SetAttribute("Name", "Finish");
				finishPointElement.SetAttribute("X", (moveInterval.Interval.MoveXAxis * 100).ToString("0.###", CultureInfo.InvariantCulture));
				finishPointElement.SetAttribute("Y", (-moveInterval.Interval.MoveYAxis * 100).ToString("0.###", CultureInfo.InvariantCulture));

				// Append points to MoveInterval
				moveIntervalElement.AppendChild(startPointElement);
				moveIntervalElement.AppendChild(supportPointElement);
				moveIntervalElement.AppendChild(finishPointElement);

				moveElement.AppendChild(moveIntervalElement);
			}
		}

		transformationElement.AppendChild(moveElement);
		dynamicElement.AppendChild(transformationElement);
		propertiesElement.AppendChild(dynamicElement);
		objectElement.AppendChild(propertiesElement);

		// Create Content element
		XmlElement contentElement = xml.CreateElement("Content");

		// Image list for the dynamic
		List<GameObject> imageObjects = new List<GameObject>();

		// Add image to the list
		foreach (UnityEngine.Transform child in dynamicInSceneTransform) {
			if (!buildMap.IsVisible(child.gameObject)) {
				continue;
			};

			if (child.gameObject.CompareTag("Image")) {
				imageObjects.Add(child.gameObject);
			}
		}

		imageObjects = imageObjects.OrderBy(x => x.name.Length).ToList();
		imageObjects = imageObjects.OrderBy(x => x.name).ToList();

		// Sort the list based on order in layer
		imageObjects.Sort((a, b) => {
			SpriteRenderer rendererA = a.GetComponent<SpriteRenderer>();
			SpriteRenderer rendererB = b.GetComponent<SpriteRenderer>();

			int orderA = rendererA ? rendererA.sortingOrder : 0;
			int orderB = rendererB ? rendererB.sortingOrder : 0;

			return orderA.CompareTo(orderB);
		});

		foreach (GameObject imageObject in imageObjects) {
			XmlElement Ielement = xml.CreateElement("Image");
			// Add X position (Refit into the Vector units)
			Ielement.SetAttribute("X", (imageObject.transform.position.x * 100).ToString(CultureInfo.InvariantCulture));
			// Add Y position (Negative because Vector see the world upside down)
			Ielement.SetAttribute("Y", (-imageObject.transform.position.y * 100).ToString(CultureInfo.InvariantCulture));

			// Add a name
			Ielement.SetAttribute("ClassName", Regex.Replace(imageObject.name, globalRegex, string.Empty));
			Ielement.SetAttribute("ClassName", Regex.Replace(imageObject.name, globalRegex, string.Empty));

			SpriteRenderer spriteRenderer = imageObject.GetComponent<SpriteRenderer>();
			if (spriteRenderer != null && spriteRenderer.sprite != null) {
				XmlElement imagePropertiesElement = xml.CreateElement("Properties");
				XmlElement staticElement = xml.CreateElement("Static");
				bool hasStaticContent = false;

				float rotationAngle = imageObject.transform.eulerAngles.z % 360; 

				float imagePosX = imageObject.transform.position.x * 100;
				float imagePosY = -imageObject.transform.position.y * 100;

				bool flipX = spriteRenderer.flipX;
				bool flipY = spriteRenderer.flipY;

				if (flipX ^ flipY) {
					rotationAngle = -rotationAngle;
				}

				if ((rotationAngle != 0 && rotationAngle != 360) || flipX || flipY) {
					float A, B, C, D, Tx, Ty;

					int nativeWidth = spriteRenderer.sprite.texture.width;
					int nativeHeight = spriteRenderer.sprite.texture.height;

					float imageWidth = nativeWidth * imageObject.transform.localScale.x;
					float imageHeight = nativeHeight * imageObject.transform.localScale.y;

					if (rotationAngle == 90) {
						A = 0;
						B = -imageWidth;
						C = imageHeight;
						D = 0;
					}
					else if (rotationAngle == 180) {
						A = -imageWidth;
						B = 0;
						C = 0;
						D = -imageHeight;
					}
					else if (rotationAngle == 270) {
						A = 0;
						B = imageWidth;
						C = -imageHeight;
						D = 0;
					}
					else {
						float radians = rotationAngle * Mathf.Deg2Rad;
						float cosTheta = Mathf.Cos(radians);
						float sinTheta = Mathf.Sin(radians);

						A = imageWidth * cosTheta;
						B = -imageWidth * sinTheta;
						C = imageHeight * sinTheta;
						D = imageHeight * cosTheta;
					}

					if (flipX) {
						A = -A;
						C = -C;
					}

					if (flipY) {
						B = -B;
						D = -D;
					}

					float topLeftX = imagePosX + Math.Min(0, A) + Math.Min(0, C);
					float topLeftY = imagePosY + Math.Min(0, B) + Math.Min(0, D);

					Tx = imagePosX - topLeftX;
					Ty = imagePosY - topLeftY;

					Ielement.SetAttribute("X", Math.Round(topLeftX).ToString(CultureInfo.InvariantCulture));
					Ielement.SetAttribute("Y", Math.Round(topLeftY).ToString(CultureInfo.InvariantCulture));
					Ielement.SetAttribute("Width", imageWidth.ToString(CultureInfo.InvariantCulture));
					Ielement.SetAttribute("Height", imageHeight.ToString(CultureInfo.InvariantCulture));
					Ielement.SetAttribute("NativeX", nativeWidth.ToString(CultureInfo.InvariantCulture));
					Ielement.SetAttribute("NativeY", nativeHeight.ToString(CultureInfo.InvariantCulture));

					XmlElement matrixElement = xml.CreateElement("Matrix");
					matrixElement.SetAttribute("A", A.ToString("0.#####", CultureInfo.InvariantCulture));
					matrixElement.SetAttribute("B", B.ToString("0.#####", CultureInfo.InvariantCulture));
					matrixElement.SetAttribute("C", C.ToString("0.#####", CultureInfo.InvariantCulture));
					matrixElement.SetAttribute("D", D.ToString("0.#####", CultureInfo.InvariantCulture));
					matrixElement.SetAttribute("Tx", Tx.ToString("0.#####", CultureInfo.InvariantCulture));
					matrixElement.SetAttribute("Ty", Ty.ToString("0.#####", CultureInfo.InvariantCulture));

					staticElement.AppendChild(matrixElement);
					hasStaticContent = true;
				} else {
					Bounds bounds = spriteRenderer.sprite.bounds;
					Vector3 scale = imageObject.transform.localScale;
					float width = bounds.size.x * 100;
					float height = bounds.size.y * 100;

					Ielement.SetAttribute("X", Math.Round(imagePosX).ToString());
					Ielement.SetAttribute("Y", Math.Round(imagePosY).ToString());
					Ielement.SetAttribute("Width", (width * scale.x).ToString(CultureInfo.InvariantCulture));
					Ielement.SetAttribute("Height", (height * scale.y).ToString(CultureInfo.InvariantCulture));
					Ielement.SetAttribute("NativeX", width.ToString(CultureInfo.InvariantCulture));
					Ielement.SetAttribute("NativeY", height.ToString(CultureInfo.InvariantCulture));
				}

				Color color = spriteRenderer.color;
				if (
					(color.r != 1.000 || color.g != 1.000 || color.b != 1.000 || color.a != 1.000)
					&& !imageObject.name.ToLower().StartsWith("v_black")
				) {
					string alphaHex = Mathf.RoundToInt(color.a * 255).ToString("X2");
					string rgbaColor = ColorUtility.ToHtmlStringRGB(color) + alphaHex;

					XmlElement colorElement = xml.CreateElement("StartColor");
					colorElement.SetAttribute("Color", $"#{rgbaColor}");

					staticElement.AppendChild(colorElement);
					hasStaticContent = true;
				}

				if (hasStaticContent) {
					imagePropertiesElement.AppendChild(staticElement);
				}

				DynamicColor dynamicColor = imageObject.GetComponent<DynamicColor>();
				if (dynamicColor != null) {
					XmlElement dynamicElementImage = xml.CreateElement("Dynamic");
					XmlElement transformationElementImage = xml.CreateElement("Transformation");
					XmlElement colorElementImage = xml.CreateElement("Color");

					transformationElementImage.SetAttribute("Name", dynamicColor.TransformationName);

					Color currentColor = spriteRenderer.color;
					string startColorHex = ColorUtility.ToHtmlStringRGB(currentColor) + Mathf.RoundToInt(currentColor.a * 255).ToString("X2");
					string finishColorHex = ColorUtility.ToHtmlStringRGB(dynamicColor.ChangeToColor) + Mathf.RoundToInt(dynamicColor.ChangeToColor.a * 255).ToString("X2");

					colorElementImage.SetAttribute("ColorStart", $"#{startColorHex}");
					colorElementImage.SetAttribute("ColorFinish", $"#{finishColorHex}");

					int frames = dynamicColor.Duration > 0 ? Mathf.CeilToInt(dynamicColor.Duration * 60) : 1;
					colorElementImage.SetAttribute("Frames", frames.ToString());

					transformationElementImage.AppendChild(colorElementImage);
					dynamicElementImage.AppendChild(transformationElementImage);
					imagePropertiesElement.AppendChild(dynamicElementImage);
				}

				string writeModeValue = "any";
				VectorierWriteMode writeMode = imageObject.GetComponent<VectorierWriteMode>();

				if (writeMode != null) {
					writeModeValue = writeMode.GetWriteModeValue();
					writeMode.AddWriteModeProperties(xml, Ielement, writeModeValue);
				}

				if (imagePropertiesElement.HasChildNodes) {
					Ielement.AppendChild(imagePropertiesElement);
				}

				contentElement.AppendChild(Ielement);
			}
		}

		foreach (UnityEngine.Transform childObject in dynamicInSceneTransform) {
			if (childObject.gameObject.CompareTag("Object")) {
				if (childObject.name == "Camera") {
					continue;
				}

				// Create a new node from scratch
				XmlElement objElement = xml.CreateElement("Object");

				// Add a name
				objElement.SetAttribute("Name", Regex.Replace(childObject.name, globalRegex, string.Empty));

				// Add X position (Refit into the Vector units)
				objElement.SetAttribute("X", (childObject.transform.position.x * 100).ToString(CultureInfo.InvariantCulture));

				// Add Y position (Negative because Vector see the world upside down)
				objElement.SetAttribute("Y", (-childObject.transform.position.y * 100).ToString("0.###", CultureInfo.InvariantCulture));

				// Write mode
				string writeModeValue = "any";

				VectorierWriteMode writeMode = childObject.GetComponent<VectorierWriteMode>();
				if (writeMode != null) {
					writeModeValue = writeMode.GetWriteModeValue();
					writeMode.AddWriteModeProperties(xml, objElement, writeModeValue);
				}

				contentElement.AppendChild(objElement);
			} else if (childObject.gameObject.CompareTag("Platform")) {
				// Platform
				if (childObject.name == "Camera") {
					continue;
				}

				// Create a new node from scratch
				XmlElement P_element = xml.CreateElement("Platform");

				// Add X position (Refit into the Vector units)
				P_element.SetAttribute("X", (Mathf.Round(childObject.transform.position.x * 100f)).ToString(CultureInfo.InvariantCulture));

				// Add Y position (Negative because Vector see the world upside down)
				P_element.SetAttribute("Y", (Mathf.Round(-childObject.transform.position.y * 100f)).ToString("0.###", CultureInfo.InvariantCulture));

				// Get the Sprite Size in Width and Height
				SpriteRenderer spriteRenderer = childObject.GetComponent<SpriteRenderer>();
				if (spriteRenderer != null && spriteRenderer.sprite != null) {
					// Get the bounds of the sprite
					Bounds bounds = spriteRenderer.sprite.bounds;

					// Get the GameObject scale
					Vector3 scale = childObject.transform.localScale;

					// Retrieve the image resolution of the sprite
					float width = bounds.size.x * 100;
					float height = bounds.size.y * 100;

					// Set the width and height accordingly to the scale in the editor
					// Width of the Image
					P_element.SetAttribute("Width", (width * scale.x).ToString(CultureInfo.InvariantCulture));

					// Height of the Image
					P_element.SetAttribute("Height", (height * scale.y).ToString(CultureInfo.InvariantCulture));
				}

				// Write mode
				string writeModeValue = "any";

				VectorierWriteMode writeMode = childObject.GetComponent<VectorierWriteMode>();
				if (writeMode != null) {
					writeModeValue = writeMode.GetWriteModeValue();
					writeMode.AddWriteModeProperties(xml, P_element, writeModeValue);
				}

				contentElement.AppendChild(P_element);
			} else if (childObject.gameObject.CompareTag("Trapezoid")) {
				string objRegex = Regex.Replace(childObject.name, globalRegex, string.Empty);

				if (logObjectWriting) {
					Debug.Log("Writing object: " + objRegex, childObject);
				}

				// Return early if not trapezoid_type1 or trapezoid_type2
				if (objRegex != "trapezoid_type1" && objRegex != "trapezoid_type2") {
					return;
				}

				Vector3 scale = childObject.transform.localScale;
				Vector3 position = childObject.transform.position;

				if (scale.x != scale.y) {
					Debug.LogWarning($@"Trapezoid-tagged GameObject named ""{childObject.name}"" doesn't have proportional scaling", childObject);
				}

				SpriteRenderer spriteRenderer = childObject.GetComponent<SpriteRenderer>();

				if (spriteRenderer != null && spriteRenderer.sprite != null) {
					Bounds bounds = spriteRenderer.sprite.bounds;
					float width = bounds.size.x * 100;
					float height = bounds.size.y * 100;

					float adjustedWidth = width * scale.x;
					float adjustedHeight = height * scale.y;

					string finalX;
					string finalY;

					bool origType1 = objRegex == "trapezoid_type1";
					bool isFlipped = spriteRenderer.flipX;
					bool actualType1 = origType1 ^ isFlipped;

					if (isFlipped) {
						float x = (100 * position.x) - adjustedWidth;
						float y = (-position.y * 100) + (actualType1 ? adjustedHeight : -adjustedHeight);

						finalX = x.ToString("0.###", CultureInfo.InvariantCulture);
						finalY = y.ToString("0.###", CultureInfo.InvariantCulture);
					} else {
						finalX = Engine.Math.GameUnits.Multiply(position.x);
						finalY = Engine.Math.GameUnits.Multiply(-position.y);
					}

					XmlElement T_element = xml.CreateElement("Trapezoid");

					T_element.SetAttribute("X", finalX);
					T_element.SetAttribute("Y", finalY);
					T_element.SetAttribute("Width", adjustedWidth.ToString(CultureInfo.InvariantCulture));

					if (actualType1) {
						T_element.SetAttribute("Height", "1");
						T_element.SetAttribute("Height1", (adjustedHeight + 1).ToString(CultureInfo.InvariantCulture));
						T_element.SetAttribute("Type", "1");
					} else {
						T_element.SetAttribute("Height", (adjustedHeight + 1).ToString(CultureInfo.InvariantCulture));
						T_element.SetAttribute("Height1", "1");
						T_element.SetAttribute("Type", "2");
					}

					// Write mode
					string writeModeValue = "any";
					VectorierWriteMode writeMode = childObject.GetComponent<VectorierWriteMode>();
					if (writeMode != null) {
						writeModeValue = writeMode.GetWriteModeValue();
						writeMode.AddWriteModeProperties(xml, T_element, writeModeValue);
					}

					contentElement.AppendChild(T_element);
				}
			} else if (childObject.gameObject.CompareTag("Area")) {
				if (childObject.name == "TriggerCatch" || childObject.name == "TriggerCatchFront") {
					// Create a new node from scratch
					XmlElement A_element = xml.CreateElement("Area");

					// Add an name
					A_element.SetAttribute("Name", Regex.Replace(childObject.name, globalRegex, string.Empty));

					// Add X position (Refit into the Vector units)
					A_element.SetAttribute("X", (childObject.transform.position.x * 100).ToString(CultureInfo.InvariantCulture));

					// Add Y position (Negative because Vector see the world upside down)
					A_element.SetAttribute("Y", (-childObject.transform.position.y * 100).ToString(CultureInfo.InvariantCulture));

					// Get the Sprite Size in Width and Height
					SpriteRenderer spriteRenderer = childObject.GetComponent<SpriteRenderer>();
					if (spriteRenderer != null && spriteRenderer.sprite != null) {
						// Get the bounds of the sprite
						Bounds bounds = spriteRenderer.sprite.bounds;

						// Get the GameObject scale
						Vector3 scale = childObject.transform.localScale;

						// Retrieve the image resolution of the sprite
						float width = bounds.size.x * 100;
						float height = bounds.size.y * 100;

						// Set the width and height accordingly to the scale in the editor
						// Width of the Image
						A_element.SetAttribute("Width", (width * scale.x).ToString(CultureInfo.InvariantCulture));

						// Height of the Image
						A_element.SetAttribute("Height", (height * scale.y).ToString(CultureInfo.InvariantCulture));
					}
					
					// Type="Catch"/>
					A_element.SetAttribute("Type", "Catch");

					// Distance="300"/>
					A_element.SetAttribute("Distance", buildMap.Hunter.hunterCatchDistance.ToString(CultureInfo.InvariantCulture));

					// Place it into the Object node
					contentElement.AppendChild(A_element);
				} else if (childObject.name == "TriggerCatchFast") {
					// Create a new node from scratch
					XmlElement A_element = xml.CreateElement("Area");

					// Add an name
					A_element.SetAttribute("Name", Regex.Replace(childObject.name, globalRegex, string.Empty));

					// Add X position (Refit into the Vector units)
					A_element.SetAttribute("X", (childObject.transform.position.x * 100).ToString(CultureInfo.InvariantCulture));

					// Add Y position (Negative because Vector see the world upside down)
					A_element.SetAttribute("Y", (-childObject.transform.position.y * 100).ToString(CultureInfo.InvariantCulture));

					// Get the Sprite Size in Width and Height
					SpriteRenderer spriteRenderer = childObject.GetComponent<SpriteRenderer>();
					if (spriteRenderer != null && spriteRenderer.sprite != null) {

						// Get the bounds of the sprite
						Bounds bounds = spriteRenderer.sprite.bounds;

						// Get the GameObject scale
						Vector3 scale = childObject.transform.localScale;

						// Retrieve the image resolution of the sprite
						float width = bounds.size.x * 100;
						float height = bounds.size.y * 100;

						// Set the width and height accordingly to the scale in the editor
						// Width of the Image
						A_element.SetAttribute("Width", (width * scale.x).ToString(CultureInfo.InvariantCulture));

						// Height of the Image
						A_element.SetAttribute("Height", (height * scale.y).ToString(CultureInfo.InvariantCulture));
					}

					// Type="Catch"/>
					A_element.SetAttribute("Type", "Catch");

					// Distance="0"/>
					A_element.SetAttribute("Distance", "0");

					// Place it into the Object node
					contentElement.AppendChild(A_element);
				} else {
					// Create a new node from scratch
					XmlElement A_element = xml.CreateElement("Area");

					// Add an name
					A_element.SetAttribute("Name", Regex.Replace(childObject.name, globalRegex, string.Empty));

					// Add X position (Refit into the Vector units)
					A_element.SetAttribute("X", (childObject.transform.position.x * 100).ToString(CultureInfo.InvariantCulture));

					// Add Y position (Negative because Vector see the world upside down)
					A_element.SetAttribute("Y", (-childObject.transform.position.y * 100).ToString(CultureInfo.InvariantCulture));

					// Get the Sprite Size in Width and Height
					SpriteRenderer spriteRenderer = childObject.GetComponent<SpriteRenderer>();
					if (spriteRenderer != null && spriteRenderer.sprite != null) {
						// Get the bounds of the sprite
						Bounds bounds = spriteRenderer.sprite.bounds;

						// Get the GameObject scale
						Vector3 scale = childObject.transform.localScale;

						// Retrieve the image resolution of the sprite
						float width = bounds.size.x * 100;
						float height = bounds.size.y * 100;

						// Set the width and height accordingly to the scale in the editor

						// Width of the Image
						A_element.SetAttribute("Width", (width * scale.x).ToString(CultureInfo.InvariantCulture));

						// Height of the Image
						A_element.SetAttribute("Height", (height * scale.y).ToString(CultureInfo.InvariantCulture));
					}

					// Type="Catch"/>
					A_element.SetAttribute("Type", "Animation");

					// Place it into the Object node
					contentElement.AppendChild(A_element);
				}
			} else if (childObject.gameObject.CompareTag("Trigger")) {
				DynamicTrigger dynamicTrigger = childObject.GetComponent<DynamicTrigger>();
				TriggerSettings triggerSettings = childObject.GetComponent<TriggerSettings>();

				XmlElement T_element = xml.CreateElement("Trigger");
				T_element.SetAttribute("X", (childObject.transform.position.x * 100).ToString(CultureInfo.InvariantCulture));
				T_element.SetAttribute("Y", (-childObject.transform.position.y * 100).ToString(CultureInfo.InvariantCulture));

				SpriteRenderer spriteRenderer = childObject.GetComponent<SpriteRenderer>();
				if (spriteRenderer != null && spriteRenderer.sprite != null) {
					Bounds bounds = spriteRenderer.sprite.bounds;
					Vector3 scale = childObject.transform.localScale;

					float width = bounds.size.x * 100;
					float height = bounds.size.y * 100;

					T_element.SetAttribute("Width", (width * scale.x).ToString(CultureInfo.InvariantCulture));
					T_element.SetAttribute("Height", (height * scale.y).ToString(CultureInfo.InvariantCulture));
				}

				if (dynamicTrigger != null) {
					T_element.SetAttribute("Name", Regex.Replace(childObject.name, globalRegex, string.Empty));

					XmlElement triggerContentElement = xml.CreateElement("Content");
					XmlElement initElement = xml.CreateElement("Init");

					XmlElement setVariable1 = xml.CreateElement("SetVariable");
					setVariable1.SetAttribute("Name", "$Active");
					setVariable1.SetAttribute("Value", "1");
					initElement.AppendChild(setVariable1);

					XmlElement setVariable2 = xml.CreateElement("SetVariable");
					setVariable2.SetAttribute("Name", "$AI");
					setVariable2.SetAttribute("Value", dynamicTrigger.AIAllowed.ToString());
					initElement.AppendChild(setVariable2);

					XmlElement setVariable3 = xml.CreateElement("SetVariable");
					setVariable3.SetAttribute("Name", "$Node");
					setVariable3.SetAttribute("Value", string.IsNullOrEmpty(dynamicTrigger.modelNode) ? "COM" : dynamicTrigger.modelNode);
					initElement.AppendChild(setVariable3);

					var splittedAnimations = dynamicTrigger.Animations
						.Split('|')
						.Where(s => !string.IsNullOrWhiteSpace(s))
						.Select(s => s.Trim())
						.Distinct()
						.OrderBy(s => s)
						.ThenBy(s => s.Length)
						.ToList();

					bool splittedAnimationsMultiple = splittedAnimations.Count > 1;

					if (!string.IsNullOrEmpty(dynamicTrigger.Animations)) {
						int animationCounter = 1;

						foreach (string animName in splittedAnimations) {
							XmlElement animationNamesNode = xml.CreateElement("SetVariable");

							animationNamesNode.SetAttribute("Name", (splittedAnimationsMultiple ? animationCounter.ToString() + "_" : "") + "ReqAnim");
							animationNamesNode.SetAttribute("Value", animName);

							initElement.AppendChild(animationNamesNode);
							animationCounter++;
						}
					}

					if (dynamicTrigger.PlaySound) {
						XmlElement setVariable4 = xml.CreateElement("SetVariable");
						setVariable4.SetAttribute("Name", "Sound");
						setVariable4.SetAttribute("Value", dynamicTrigger.Sound);
						initElement.AppendChild(setVariable4);
					}

					XmlElement setVariable5 = xml.CreateElement("SetVariable");
					setVariable5.SetAttribute("Name", "Flag1");
					setVariable5.SetAttribute("Value", "0");
					initElement.AppendChild(setVariable5);

					triggerContentElement.AppendChild(initElement);
					XmlElement loopElement = xml.CreateElement("Loop");

					XmlElement eventsElement = xml.CreateElement("Events");

					// Enter or exit
					XmlElement eventBlock_entry = xml.CreateElement("EventBlock");

					eventBlock_entry.SetAttribute("Template", "FreqUsed." + dynamicTrigger.EventType.ToString());

					eventsElement.AppendChild(eventBlock_entry);

					if (!string.IsNullOrEmpty(dynamicTrigger.Animations)) {
						XmlElement eventBlock_ReqAnim = xml.CreateElement("EventBlock");

						eventBlock_ReqAnim.SetAttribute("Template", "CommonLib.RequiredAnimation");

						eventsElement.AppendChild(eventBlock_ReqAnim);
					}

					loopElement.AppendChild(eventsElement);

					if (!string.IsNullOrEmpty(dynamicTrigger.Animations)) {
						XmlElement conditionsElement = xml.CreateElement("Conditions");

						if (splittedAnimations.Count < 2) {
							XmlElement conditionBlock = xml.CreateElement("ConditionBlock");
							conditionBlock.SetAttribute("Template", "CommonLib.RequiredAnimation");
							conditionsElement.AppendChild(conditionBlock);
						}

						if (splittedAnimations.Count > 1) {
							XmlElement conditionsOperatorElement = xml.CreateElement("Operator");
							conditionsOperatorElement.SetAttribute("Type", "Or");

							int animationCounter = 1;
							foreach (string name in splittedAnimations) {
								XmlElement conditionBlock_reqAnim = xml.CreateElement("ConditionBlock");
		
								conditionBlock_reqAnim.SetAttribute("Template", "CommonLib.RequiredAnimation");
								conditionBlock_reqAnim.SetAttribute("Prefix", $"{animationCounter}_");

								conditionsOperatorElement.AppendChild(conditionBlock_reqAnim);
								animationCounter++;
							}

							conditionsElement.AppendChild(conditionsOperatorElement);
						}

						loopElement.AppendChild(conditionsElement);
					}

					XmlElement actionsElement = xml.CreateElement("Actions");
					XmlElement transformElement = xml.CreateElement("Transform");

					string randInt = UnityEngine.Random.Range((int)1E8, (int)1E9 - 1).ToString();
					if (
						dynamicComponent != null && 
						(!dynamicTrigger.MultipleTransformation || dynamicTrigger.TransformationNames.Count < 1) &&
						string.IsNullOrEmpty(dynamicTrigger.TriggerTransformName) &&
						!string.IsNullOrEmpty(dynamicComponent.TransformationName)
					) {
						transformElement.SetAttribute("Name", dynamicComponent.TransformationName + $"_{randInt}");
					} else {
						transformElement.SetAttribute("Name", dynamicTrigger.TriggerTransformName);
					}

					if (dynamicTrigger.Latency > 0) {
						XmlElement chooseElement = xml.CreateElement("Choose");
						chooseElement.SetAttribute("Order", "Sync");
						chooseElement.AppendChild(transformElement);

						XmlElement nestedChooseElement = xml.CreateElement("Choose");
						nestedChooseElement.SetAttribute("Order", "Straight");

						XmlElement waitElement = xml.CreateElement("Wait");
						waitElement.SetAttribute("Frames", Math.Round(dynamicTrigger.Latency * 60).ToString());
						nestedChooseElement.AppendChild(waitElement);

						if (dynamicTrigger.PlaySound) {
							XmlElement soundElement = xml.CreateElement("Sound");
							soundElement.SetAttribute("Name", "_Sound");
							nestedChooseElement.AppendChild(soundElement);
						}

						XmlElement setActiveElement = xml.CreateElement("SetVariable");
						setActiveElement.SetAttribute("Name", "$Active");
						setActiveElement.SetAttribute("Value", "0");
						nestedChooseElement.AppendChild(setActiveElement);

						chooseElement.AppendChild(nestedChooseElement);
						actionsElement.AppendChild(chooseElement);
					} else {
						if (dynamicTrigger.PlaySound) {
							XmlElement soundElement = xml.CreateElement("Sound");
							soundElement.SetAttribute("Name", "_Sound");
							actionsElement.AppendChild(soundElement);
						}
						actionsElement.AppendChild(transformElement);
						
						XmlElement setActiveElement = xml.CreateElement("SetVariable");
						setActiveElement.SetAttribute("Name", "$Active");
						setActiveElement.SetAttribute("Value", "0");
						actionsElement.AppendChild(setActiveElement);
					}

					loopElement.AppendChild(actionsElement);
					triggerContentElement.AppendChild(loopElement);
					T_element.AppendChild(triggerContentElement);
				};

				if (triggerSettings != null) {
					XmlElement contentElement2 = xml.CreateElement("Content");

					// XML does not format correctly, so we load them into a separate doc
					XmlDocument tempDoc = new XmlDocument();
					tempDoc.LoadXml("<Content>" + triggerSettings.Content + "</Content>");

					foreach (XmlNode childNode in tempDoc.DocumentElement.ChildNodes) {
						XmlNode importedNode = xml.ImportNode(childNode, true);
						contentElement2.AppendChild(importedNode);
					}

					T_element.AppendChild(contentElement2);
				}

				string writeModeValue = "any";

				VectorierWriteMode writeMode = childObject.GetComponent<VectorierWriteMode>();
				if (writeMode != null) {
					writeModeValue = writeMode.GetWriteModeValue();
					writeMode.AddWriteModeProperties(xml, T_element, writeModeValue);
				}

				contentElement.AppendChild(T_element);
			} /*else if (childObject.gameObject.CompareTag("Model")) {
				if (childObject.name == "Camera") {
					continue;
				}
				
				ModelProperties modelProperties = childObject.GetComponent<ModelProperties>();

				// Create a new node from scratch
				XmlElement Modelelement = xml.CreateElement("Model");

				// Add X position (Refit into the Vector units)
				Modelelement.SetAttribute("X", (childObject.transform.position.x * 100).ToString(CultureInfo.InvariantCulture));
				// Add Y position (Negative because Vector see the world upside down)
				Modelelement.SetAttribute("Y", (-childObject.transform.position.y * 100).ToString(CultureInfo.InvariantCulture));

				// Add model type
				Modelelement.SetAttribute("Type", "1");

				// Add an name
				Modelelement.SetAttribute("ClassName", Regex.Replace(childObject.name, globalRegex, string.Empty));

				// Add an name
				if (modelProperties.UseLifeTime) {
					Modelelement.SetAttribute("LifeTime", modelProperties.LifeTime);
				}

				contentElement.AppendChild(Modelelement);
			} */ else if (childObject.gameObject.CompareTag("Animation")) {
				// Animation Properties Component
				AnimationProperties AnimationComponent = childObject.GetComponent<AnimationProperties>();

				if (childObject.name == "Camera") {
					continue;
				}
				
				// Create a new node from scratch
				XmlElement animationElement = xml.CreateElement("Animation");

				// Add X position (Refit into the Vector units)
				animationElement.SetAttribute("X", (childObject.transform.position.x * 100).ToString(CultureInfo.InvariantCulture));

				// Add Y position (Negative because Vector see the world upside down)
				animationElement.SetAttribute("Y", (-childObject.transform.position.y * 100).ToString(CultureInfo.InvariantCulture));

				// Add a Width
				animationElement.SetAttribute("Width", AnimationComponent.Width);

				// Add a Height
				animationElement.SetAttribute("Height", AnimationComponent.Height);

				// Type (default: 1)
				animationElement.SetAttribute("Type", AnimationComponent.Type);

				if (!string.IsNullOrEmpty(AnimationComponent.Direction)) {
					// Direction (ex: Direction="10|-1.5")
					animationElement.SetAttribute("Direction", AnimationComponent.Direction);
				}

				if (!string.IsNullOrEmpty(AnimationComponent.Acceleration)) {
					// Acceleration (ex: Acceleration="0.02|-0.1")
					animationElement.SetAttribute("Acceleration", AnimationComponent.Acceleration);
				}

				// Add a ScaleX
				animationElement.SetAttribute("ScaleX", AnimationComponent.ScaleX);

				// Add a ScaleY
				animationElement.SetAttribute("ScaleY", AnimationComponent.ScaleY);

				if (!string.IsNullOrEmpty(AnimationComponent.Time)) {
					// Add a Time
					animationElement.SetAttribute("Time", AnimationComponent.Time);
				}

				// Add a name
				animationElement.SetAttribute("ClassName", Regex.Replace(childObject.name, globalRegex, string.Empty));

				contentElement.AppendChild(animationElement);
			}

			// Add content to the object
			objectElement.AppendChild(contentElement);
		}
		// Place it into the Object node
		node.FirstChild.AppendChild(objectElement);

		// Apply the modification to the build-map.xml file}
		xml.Save(Path.Combine(
			Application.dataPath,
			"XML", "dzip", "level_xml",
			mapToOverride + ".xml"
		));
	}
}