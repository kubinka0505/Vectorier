using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Vectorier;

using Debug = Logger.Debug;

// -=-=-=- //

#nullable enable
#pragma warning disable CS8602 // nullables
#pragma warning disable CS8603 // possible null returns
#pragma warning disable CS8604 // possible null returns

public class BuildMap : MonoBehaviour {
	public static readonly string XmlDir = "XML";
	public static readonly string XmlDzipDir = Path.Combine(XmlDir, "dzip");

	// Uncompiled XML directories
	public static readonly string XmlDzipTexturesBaseDir = Path.Combine("Resources", "Textures");
	public static readonly string XmlDzipSoundDir = Path.Combine("XML", "dzip", "archives", "uncompiled", "sound");
	public static readonly string XmlDzipLvlDir = Path.Combine("XML", "dzip", "archives", "uncompiled", "level_xml");
	// public static readonly string XmlDzipLvlDirBackup = Path.Combine("XML", "dzip", "archives", "uncompiled", "_backup", "level_xml");

	// Assets prefix is mandatory for directories that are NOT built on fly
	public static readonly string XmlCommonDir = Path.Combine("Assets", "XML", "dzip", "archives", "uncompiled", ".common_xml");
	public static readonly string XmlGui2048Dir = Path.Combine("Assets", "XML", "dzip", "archives", "uncompiled", ".GUI_2048_1536");

	// Template file
	public static readonly string XmlTrackTemplateFile = Path.Combine("Assets", "XML", "_template" + "." + Vectorier.Core.Game.Extensions.File.XML);

	// New compiler paths
	public static readonly string XmlCompiledDir = Path.Combine("Assets", "XML", "dzip", "archives", ".compiled"); // where compiled archives are output

	// Compilator directories
	public static readonly string XmlCompilerRoot = Path.Combine("Assets", "XML", "dzip", "_compilators");
	public static readonly string XmlCompilerStandardDir = Path.Combine("Assets", "XML", "dzip", "_compilators", "standard");
	public static readonly string XmlCompilerOptimizedDir = Path.Combine("Assets", "XML", "dzip", "_compilators", "optimized");

	public static readonly string XmlCompilerStandardConfigDir = Path.Combine("Assets", "XML", "dzip", "_compilators", "standard", "_config");
	public static readonly string XmlCompilerOptimizedConfigDir = Path.Combine("Assets", "XML", "dzip", "_compilators", "optimized", "_config");

	public static readonly string XmlCompilerStandardBuildDir = Path.Combine("Assets", "XML", "dzip", "_compilators", "standard", "build");
	public static readonly string XmlCompilerOptimizedBuildDir = Path.Combine("Assets", "XML", "dzip", "_compilators", "optimized", "build");

	public static string? gameDirectoryPath;
	public static string? gameExecutablePath;

	public static event Action? MapBuilt;

	// flag to indicate if the build is for running the game
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
	public float MusicVolume = 0.3f;

	[Tooltip(@"Background image.

If empty, inherited from ""customBackgroundMirror"" variable.

⚠️ Has to be located inside ""track_content_2048.dz"" file archive base!")]
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

	[Tooltip(@"Amount of coins that will be received after completing the level.")]
	public int coinsReward = 40;

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

		[Tooltip("Determines whether player should be able to drive a bike.")]
		public bool bikeStock = false;
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

		[Tooltip("Hunter respawn name.")]
		public string hunterAllowedSpawn = "DefaultSpawn";

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

		[Tooltip("Determines whether hunter should be able to drive a bike.")]
		public bool bikeStock = false;
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

		[Tooltip("Hunter respawn name.")]
		public string helperAllowedSpawn = "HelperSpawn";

		[Tooltip("Determines whether helper should be spawned.")]
		public bool spawn = false;

		[Tooltip("Determines whether hunter should be able to drive a bike.")]
		public bool bikeStock = false;
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

		[Tooltip("Determines whether helper should be able to drive a bike.")]
		public bool bikeStock = false;
	}

	// -=-=-=- //

	[Header("Gameplay (Common Mode)")]
	[SerializeField]
	private PlayerSettings? Player;

	[SerializeField]
	private HunterSettings? Hunter;

	[SerializeField]
	private HelperSettings? Helper;

	// -=-=-=- //

	[Header("Gameplay (Hunter Mode)")]

	[SerializeField]
	private PlayerSettings_HunterMode? PlayerHM;

	[SerializeField]
	private HunterSettings_HunterMode? HunterHM;

	[SerializeField]
	private HelperSettings_HunterMode? HelperHM;

	// Miscellaneous
	[Header("Miscellaneous")]

	[Tooltip(@"Uses custom properties instead of prefixed

⚠️ Ignores the above settings for player and hunter!")]
	public bool useCustomProperties;

	[Tooltip(@"Custom properties for Common Mode.")]
	[TextArea(5, 10)]
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

	[Tooltip(@"Custom properties for Hunter Mode.")]
	[TextArea(5, 10)]
	public string CustomModelProperties_HM = @"<Model
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

	[Tooltip("Outputs objects writing to console while building the map.")]
	public bool logObjectWriting;
	public bool hunterPlaced;

	[Tooltip("Divide GameObject's position by it's layer object factor.")]
	public bool correctFactorPosition = true;

	// -=-=-=- //
	// Menu Items

	[MenuItem("Vectorier/Build (dz)")]
	public static void BuildDz() {
		Build(
			compressionAlgorithm: "dz",
			createArchives: true
		);
	}

	[MenuItem("Vectorier/Build (zlib)")]
	public static void BuildZlib() {
		Build(
			compressionAlgorithm: "zlib",
			createArchives: true
		);
	}

	/*
	// crashes the game
	[MenuItem("Vectorier/Build (zero)")]
	public static void BuildZero() {
		Build(
			compressionAlgorithm: "zero",
			createArchives: true
		);
	}
	*/
	[MenuItem("Vectorier/Build (copy) #&B")]
	public static void BuildCopy() {
		Build(
			compressionAlgorithm: "copy",
			createArchives: true
		);
	}

	[MenuItem("Vectorier/Build (XML Only)")]
	public static void BuildXml() {
		Build(
			compressionAlgorithm: "dz",
			createArchives: false
		);
	}

	// -=-=-=- //
	// Functions

	public string BytesToString(float byteCount) {
		if (byteCount <= 0) {
			return "0 B";
		}

		CultureInfo culture = CultureInfo.CurrentCulture;
		string[] suf = { "", "K", "M", "G", "T", "P" };

		double bytes = Math.Abs(byteCount);
		int place = (int)Math.Floor(Math.Log(bytes, 1024));

		place = Math.Min(place, suf.Length - 1);

		double num = bytes / Math.Pow(1024, place);
		string precision = place == 0 ? "N0" : "N2";

		string formatted = (Math.Sign(byteCount) * num).ToString(precision, CultureInfo.InvariantCulture);
		return $"{formatted} {suf[place]}B";
	}

	#if UNITY_EDITOR

	public bool IsVisible(GameObject obj) {
		return !obj.CompareTag("EditorOnly") && 
			!obj.CompareTag("Unused") && 
			!SceneVisibilityManager.instance.IsHidden(obj) && 
			obj.activeInHierarchy;
	}

	#endif

	// -=-=-=- //
	// Main

	public static void Build(
		string compressionAlgorithm,
		bool createArchives
	) {
		// lazy init
		gameDirectoryPath = Vectorier.Settings.GameDirectory;
		gameExecutablePath = Path.Combine(gameDirectoryPath, "Vector.exe");

		if (!Directory.Exists(gameDirectoryPath)) {
			Debug.LogFatal($"Game directory not found: {gameDirectoryPath}");
			return;
		}

		if (!File.Exists(gameExecutablePath)) {
			Debug.LogFatal($"Game executable was not found in game directory ({Path.GetFileName(gameExecutablePath)})");
			return;
		}

		var buildMap = FindObjectOfType<BuildMap>();

		if (buildMap == null || buildMap.mapToOverride == null) {
			Debug.LogFatal("No GameObject with map building script attached has been found!");
			return;
		}

		if (!buildMap.enabled) {
			Debug.LogFatal("GameObject with map building script is disabled.");
			return;
		}

		string scenePath = SceneManager.GetActiveScene().path;

		// -=-=-=- //
		// Variables

		string XmlTrackFile = Path.Combine(XmlDzipLvlDir, buildMap.mapToOverride + "." + Vectorier.Core.Game.Extensions.File.XML);
		string XmlTrackFileFull = Path.Combine(Application.dataPath, XmlTrackFile);

		// Find BuildMap
		string bmNotFoundMsg = "";

		if (buildMap == null && !buildMap.enabled) {
			// Check if buildMap is null first
			bmNotFoundMsg = @"Couldn't compile a track - No map building script instance was found active in any GameObject";
		} else if (!buildMap.IsVisible(buildMap.gameObject)) {
			// Then check visibility
			bmNotFoundMsg = @"Couldn't compile a track - GameObject containing map building script was set to inactive";
		}

		if (!string.IsNullOrEmpty(bmNotFoundMsg)) {
			Debug.LogError(bmNotFoundMsg);
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


		// -=-=-=- //
		// Validators

		if (Vectorier.Settings.ValidateScene) {
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
				}
			}
		}

		if (Vectorier.Settings.SaveSceneBeforeBuildMap) {
			if (!string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) {
				EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
			}
		}

		/*
		if (string.IsNullOrEmpty(buildMap.gameDirectoryPath)) {
			buildMap.gameDirectoryPath = Vectorier.Settings.GameDirectory;
		}
		*/

		// Start the stopwatch
		var stopwatch = Stopwatch.StartNew();

		GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>()
			.OrderBy(obj => obj.name)
			.OrderBy(obj => obj.transform.position.y)
			.OrderBy(obj => obj.transform.position.x)
			.ToArray();

		GameObject[] ImSort(GameObject[] gos, string? tag = null) {
			if (tag == null) {
				return new GameObject[] {};
			}
			return gos
				.Where(obj => obj.CompareTag(tag))
				.OrderBy(obj => obj.transform.position.y)
				.OrderBy(obj => obj.transform.position.x)
				//.OrderBy(obj => obj.transform.position.z)
				.OrderBy(obj => obj.GetComponent<SpriteRenderer>().sortingOrder)
				.ToArray();
		}

		// Get all GameObjects
		GameObject[] objectsInScene = allObjects
			.Where(obj => obj.CompareTag("Object"))
			.ToArray();

		// ...with tag "Image", then arrange them based on sorting order
		GameObject[] imagesInScene = ImSort(allObjects, "Image");

		foreach (GameObject go in allObjects) {
			// Scan incorrectly-named textures
			if (go.CompareTag("Image")) {
				string objRegex = Vectorier.Core.Helpers.Get.Name(go);
				var sr = go.GetComponent<SpriteRenderer>();

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
			}
		}

		// -=-=-=- //
		// Moves Manager Processing

		string movesPath = Path.Combine(gameDirectoryPath, "Moves_new" + "." + Vectorier.Core.Game.Extensions.File.XML);

		// Find all MovesManager components
		MovesManager[] movesManagers = FindObjectsOfType<MovesManager>();

		MovesManager? movesManager = null;
		if (movesManagers.Length > 0) {
			movesManager = movesManagers[0];
		}

		if (movesManager != null && movesManager.enabled) {
			XmlDocument movesNewXml = new XmlDocument();
			movesNewXml.Load(movesPath);

			// ensure root and config nodes exist
			XmlElement movesRootNode = movesNewXml.DocumentElement ?? movesNewXml.CreateElement("Root");
			if (movesNewXml.DocumentElement == null) {
				movesNewXml.AppendChild(movesRootNode);
			}

			XmlElement configNode = movesRootNode["Config"];
			if (configNode == null) {
				configNode = movesNewXml.CreateElement("Config");
				movesRootNode.AppendChild(configNode);
			}

			// camera section
			XmlElement cameraElement = Vectorier.Core.XML.Utils.GetOrCreateElement("Camera", configNode, movesNewXml);

			var cameraAttributes = new Dictionary<string, float> {
				{ "MinZoom", movesManager.Camera.ZoomMin },
				{ "MaxZoom", movesManager.Camera.ZoomMax },
				{ "CurrZoom", movesManager.Camera.ZoomCurrent },
				{ "MaxSpeed", movesManager.Camera.MaxSpeed },
				{ "Fluency", movesManager.Camera.Fluency }
			};

			foreach (var kvp in cameraAttributes) {
				if (kvp.Value == 0) {
		            cameraElement.RemoveAttribute(kvp.Key);
				} else {
					cameraElement.SetAttribute(kvp.Key, Vectorier.Core.Helpers.ToString(kvp.Value));
				}
			}

			// taser section
			XmlElement taserElement = Vectorier.Core.XML.Utils.GetOrCreateElement("Taser", configNode, movesNewXml);

			var taserAttributes = new Dictionary<string, float> {
				{ "Distance", movesManager.Taser.Distance },
				{ "Time", movesManager.Taser.Time },
				{ "HeightFactor", movesManager.Taser.HeightFactor }
			};

			foreach (var kvp in taserAttributes) {
				if (kvp.Value == 0) {
		            taserElement.RemoveAttribute(kvp.Key);
				} else {
					taserElement.SetAttribute(kvp.Key, Vectorier.Core.Helpers.ToString(kvp.Value));
				}
			}

			movesNewXml.Save(movesPath);

			if (buildMap.optimizeWrittenTrack) {
				Vectorier.Core.XML.Utils.Optimize.General(fileInput: movesPath);
			}
		}

		// -=-=-=- //

		// Open the map template
		XmlDocument xml = new XmlDocument();
		xml.Load(XmlTrackTemplateFile);

		// <Root>
		XmlNode mapRootNode = xml.DocumentElement ?? xml.AppendChild(xml.CreateElement("Root"));

		// <Sets>
		Vectorier.Core.XML.Track.Level.Properties.SetSets(xml, allObjects);

		// <Music>
		XmlNode musicNode = xml.SelectSingleNode("/Root/Music");
		if (musicNode == null) {
			musicNode = xml.CreateElement("Music");
			mapRootNode.AppendChild(musicNode);
		}

		Vectorier.Core.XML.Track.Level.Properties.SetMusic(
			xml,

			Vectorier.Core.XML.Track.Level.Validators.Track.Music.File(buildMap.levelMusic, gameDirectoryPath),
			Vectorier.Core.XML.Track.Level.Validators.Track.Music.Volume(buildMap.MusicVolume)
		);

		// <Models Variant="CommonMode">
		XmlNode commonModels = xml.SelectSingleNode("/Root/Models[@Variant='CommonMode']");
		if (commonModels == null) {
			commonModels = xml.CreateElement("Models");
			XmlAttribute choiceAttr = xml.CreateAttribute("Choice");
			choiceAttr.Value = "AITriggers";
			commonModels.Attributes.Append(choiceAttr);

			XmlAttribute variantAttr = xml.CreateAttribute("Variant");
			variantAttr.Value = "CommonMode";
			commonModels.Attributes.Append(variantAttr);

			mapRootNode.AppendChild(commonModels);
		}

		// <Models Variant="HunterMode">
		XmlNode hunterModels = xml.SelectSingleNode("/Root/Models[@Variant='HunterMode']");
		if (hunterModels == null) {
			hunterModels = xml.CreateElement("Models");
			XmlAttribute choiceAttr = xml.CreateAttribute("Choice");
			choiceAttr.Value = "AITriggers";
			hunterModels.Attributes.Append(choiceAttr);

			XmlAttribute variantAttr = xml.CreateAttribute("Variant");
			variantAttr.Value = "HunterMode";
			hunterModels.Attributes.Append(variantAttr);

			mapRootNode.AppendChild(hunterModels);
		}

		bool bs = Vectorier.Core.XML.Track.Level.Properties.DetectBikeStock(allObjects);

		Vectorier.Core.XML.Track.Level.Properties.SetModels(
			xml: xml,
			rootNode: mapRootNode,

			// Player
			playerModelName: buildMap.Player.playerModelName,
			playerSpawnTime: Vectorier.Core.XML.Track.Level.Validators.Models.SpawnTime(
				buildMap.Player.playerModelName,
				buildMap.Player.playerSpawnTime
			),
			playerSpawnName: Vectorier.Core.XML.Track.Level.Validators.Models.SpawnName(
				buildMap.Player.playerModelName,
				buildMap.Player.playerSpawnName
			),
			playerLifeTime: Vectorier.Core.XML.Track.Level.Validators.Models.LifeTime(
				buildMap.Player.playerModelName,
				buildMap.Player.playerLifeTime
			),
			playerModelColor: buildMap.Player.modelColor,
			playerSkins: buildMap.Player.playerSkins,
			playerHasBikeStock: bs,
			playerBikeStock: buildMap.Player.bikeStock,

			// Hunter
			hunterModelName: buildMap.Hunter.hunterModelName,
			hunterSpawnTime: Vectorier.Core.XML.Track.Level.Validators.Models.SpawnTime(
				buildMap.Hunter.hunterModelName,
				buildMap.Hunter.hunterSpawnTime
			),
			hunterSpawnName: Vectorier.Core.XML.Track.Level.Validators.Models.SpawnName(
				buildMap.Hunter.hunterModelName,
				buildMap.Hunter.hunterSpawnName
			),
			hunterLifeTime: Vectorier.Core.XML.Track.Level.Validators.Models.LifeTime(
				buildMap.Hunter.hunterModelName,
				buildMap.Hunter.hunterLifeTime
			),
			hunterModelColor: buildMap.Hunter.modelColor,
			hunterSkins: buildMap.Hunter.hunterSkins,
			hunterHasBikeStock: bs,
			hunterBikeStock: buildMap.Hunter.bikeStock,
			hunterAllowTricks: buildMap.Hunter.hunterTrickAllowed,
			hunterAllowedSpawns: buildMap.Hunter.hunterAllowedSpawn,
			hunterAIType: buildMap.Hunter.hunterAIType,
			hunterIcon: buildMap.transparentInterfaceButtons ? false : buildMap.Hunter.hunterIcon,

			// Helper
			helperModelName: buildMap.Helper.helperModelName,
			helperSpawnTime: Vectorier.Core.XML.Track.Level.Validators.Models.SpawnTime(
				buildMap.Helper.helperModelName,
				buildMap.Helper.helperSpawnTime
			),
			helperSpawnName: Vectorier.Core.XML.Track.Level.Validators.Models.SpawnName(
				buildMap.Helper.helperModelName,
				buildMap.Helper.helperSpawnName
			),
			helperLifeTime: Vectorier.Core.XML.Track.Level.Validators.Models.LifeTime(
				buildMap.Helper.helperModelName,
				buildMap.Helper.helperLifeTime
			),
			helperModelColor: buildMap.Helper.modelColor,
			helperSkins: buildMap.Helper.helperSkins,
			helperHasBikeStock: bs,
			helperBikeStock: buildMap.Helper.bikeStock,
			helperSpawnEnabled: buildMap.Helper.spawn,
			helperAllowedSpawns: buildMap.Helper.helperAllowedSpawn,

			// Hunter Mode
			playerHM_SpawnIncrement: buildMap.PlayerHM.playerSpawnTimeIncrement,
			hunterHM_SpawnIncrement: buildMap.HunterHM.hunterSpawnTimeIncrement,
			helperHM_SpawnIncrement: buildMap.HelperHM.helperSpawnTimeIncrement,

			helperHM_SpawnEnabled: buildMap.HelperHM.spawn,

			// Custom Properties
			useCustomProperties: buildMap.useCustomProperties,
			customProperties_CM: buildMap.CustomModelProperties,
			customProperties_HM: buildMap.CustomModelProperties_HM
		);

		// <Coins>
		XmlNode coinsNode = xml.SelectSingleNode("/Root/Coins");

		if (buildMap.coinsReward > 0) {
			Vectorier.Core.XML.Track.Level.Properties.SetCoins(
				xml: xml,
				rootNode: (XmlElement)mapRootNode,

				value: buildMap.coinsReward
			);

			int coinsNodeValue = Vectorier.Core.XML.Utils.GetNumericAttr<int>((XmlElement)coinsNode, "Value");

			if (
				Vectorier.Settings.ValidateScene &&
				coinsNodeValue % 10 != 0
			) {
				Debug.LogWarning($"It is reccomended to have coin reward set to number divisible by 10, currently it's {coinsNodeValue}.", buildMap);
			}
		} else if (coinsNode != null) {
			mapRootNode.RemoveChild(coinsNode);
		}

		// ensure <Track> node exists
		XmlNode trackNode = xml.SelectSingleNode("/Root/Track");

		if (trackNode == null) {
			trackNode = xml.CreateElement("Track");

			xml.DocumentElement.AppendChild(trackNode);
		}

		// Backdrop func
		void ProcessSomeSprites(
			GameObject[] gos,

			XmlNode node,
			string nodeType,

			BuildMap component
		) {
			Dictionary<string, List<Tuple<string, float>>> objectTypeMap = new Dictionary<string, List<Tuple<string, float>>> {
				{ "0.1", new List<Tuple<string, float>> { Tuple.Create("Factor_0.1", 0.1f) } },
				{ "0.5", new List<Tuple<string, float>> {
					Tuple.Create("Default", 0.5f),
					Tuple.Create("Factor_0.5 [Deprecated]", 0.5f)
				}},
				{ "0.8", new List<Tuple<string, float>> { Tuple.Create("Factor_0.8", 0.8f) } },
				{ "0.25", new List<Tuple<string, float>> { Tuple.Create("Factor_0.25", 0.25f) } },
				{ "1.125", new List<Tuple<string, float>> { Tuple.Create("Factor_1.125", 1.125f) } },
				{ "1.25", new List<Tuple<string, float>> { Tuple.Create("Factor_1.25", 1.25f) } },
				{ "1.375", new List<Tuple<string, float>> { Tuple.Create("Factor_1.375", 1.375f) } }
			};

			if (!objectTypeMap.TryGetValue(nodeType, out var mappings)) {
				return;
			}

			foreach (var mapping in mappings) {
				var (sortingLayer, factor) = mapping;

				foreach (GameObject go in gos) {
					if (!component.IsVisible(go)) {
						continue;
					}

					var spriteRenderer = go.GetComponent<SpriteRenderer>();

					if (spriteRenderer != null && spriteRenderer.enabled && spriteRenderer.sortingLayerName == sortingLayer) {
						Vectorier.Core.Components.Backdrop.Convert(
							go: go,
							node: node,
							factorValue: factor,
							correctFactorPosition: component.correctFactorPosition,
							floatPrecision: Vectorier.Settings.GetPrecision(Vectorier.Settings.Elements.BackdropPrecisionKey)
						);
					}
				}
			}
		}

		// loop through <Track> child nodes (or create default behavior)
		int counter1 = 0;

		foreach (XmlNode node in trackNode.ChildNodes) {
			XmlAttribute labelAttr = node.Attributes["Label"];
			XmlAttribute factorAttr = node.Attributes["Factor"];

			string objectNodeType = labelAttr?.Value?.ToLower() ?? "unknown";
			string objectFactorValue = factorAttr?.Value ?? "0";

			// Example: set the properties into the level
			if (counter1 == 0) {
				XmlNode firstNode = node;

				if (firstNode == null) {
					firstNode = xml.CreateElement("Object");
				}

				float bgXposf = buildMap.backgroundXPosition - 3740f;
				float bgYposf = buildMap.backgroundYPosition - 500f;

				if (bgXposf != 0) {
					XmlAttribute bgXpos = xml.CreateAttribute("X");
					bgXpos.Value = Vectorier.Core.Helpers.ToString(bgXposf);
					firstNode.Attributes.Append(bgXpos);
				}

				if (bgYposf != 0) {
					XmlAttribute bgYpos = xml.CreateAttribute("Y");
					bgYpos.Value = Vectorier.Core.Helpers.ToString(bgYposf);
					firstNode.Attributes.Append(bgYpos);
				}

				XmlAttribute factor = xml.CreateAttribute("Factor");
				factor.Value = "0.05";
				firstNode.Attributes.Append(factor);

				Vectorier.Core.XML.Track.Level.Properties.SetBackground(
					xml,

					buildMap.customBackground,
					buildMap.customBackgroundMirror,

					buildMap.backgroundXPosition,
					0f, //buildMap.backgroundYPosition,

					buildMap.backgroundWidth,
					buildMap.backgroundHeight
				);
			}

			counter1++;

			if (objectNodeType == "default") {
				// Write every GameObject with tag "Object", "Image", "Platform", "Trapezoid", "Area" and "Trigger"
				foreach (GameObject spawnInScene in GameObject.FindGameObjectsWithTag("Spawn")) {
					if (!buildMap.IsVisible(spawnInScene)) {
						continue;
					}

					Vectorier.Core.Components.Spawn_.Convert(
						go: spawnInScene,
						node: node,
						floatPrecision: Vectorier.Settings.GetPrecision(Vectorier.Settings.Elements.SpawnPrecisionKey)
					);
				}

				foreach (GameObject itemInScene in GameObject.FindGameObjectsWithTag("Item")) {
					if (!buildMap.IsVisible(itemInScene)) {
						continue;
					}

					UnityEngine.Transform parent = itemInScene.transform.parent;

					if (parent != null && (parent.CompareTag("Dynamic") || parent.CompareTag("Object"))) {
						continue;
					}

					Vectorier.Core.Components.Item.Convert(
						go: itemInScene,
						node: node,
						floatPrecision: Vectorier.Settings.GetPrecision(Vectorier.Settings.Elements.ItemPrecisionKey)
					);
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

					Vectorier.Core.Components.Platform.Convert(
						go: platformInScene,
						node: node,
						floatPrecision: Vectorier.Settings.GetPrecision(Vectorier.Settings.Elements.PlatformPrecisionKey)
					);
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

					Vectorier.Core.Components.Trapezoid.Convert(
						go: trapezoidInScene,
						node: node,
						floatPrecision: Vectorier.Settings.GetPrecision(Vectorier.Settings.Elements.TrapezoidPrecisionKey)
					);
				}
			}

			// Trigger
			foreach (GameObject triggerInScene in GameObject.FindGameObjectsWithTag("Trigger")) {
				if (!buildMap.IsVisible(triggerInScene)) {
					continue;
				}

				UnityEngine.Transform parent = triggerInScene.transform.parent;

				if (parent != null && parent.CompareTag("Dynamic")) {
					continue;
				}

				// Get the SpriteRenderer of the current GameObject
				var spriteRenderer = triggerInScene.GetComponent<SpriteRenderer>();

				// Get the parent GameObject, if it exists
				Transform triggerParent = triggerInScene.transform.parent;

				if (triggerParent != null) {
					// Check if the parent has a SpriteRenderer
					var childSpriteRenderer = triggerParent.GetComponent<SpriteRenderer>();

					if (childSpriteRenderer != null && childSpriteRenderer.enabled && childSpriteRenderer.sortingLayerName == "Overlay") {
						// If the parent's SpriteRenderer sortingLayerName is "Overlay" and objectNodeType is "overlay"

						if (objectNodeType == "overlay") {
							Vectorier.Core.Components.Trigger.Convert(
								go: triggerInScene,
								node: node,
								floatPrecision: Vectorier.Settings.GetPrecision(Vectorier.Settings.Elements.TriggerPrecisionKey)
							);
						}
					}
				}

				// If the current GameObject's SpriteRenderer sortingLayerName is "Default"
				if (spriteRenderer != null && spriteRenderer.sortingLayerName == "Default") {
					if (objectNodeType == "default") {
						try {
							Vectorier.Core.Components.Trigger.Convert(
								go: triggerInScene,
								node: node,
								floatPrecision: Vectorier.Settings.GetPrecision(Vectorier.Settings.Elements.TriggerPrecisionKey)
							);
						} catch (Exception e) {
							Debug.LogError($"An {e.GetType()} occured while parsing trigger content on GameObject named \"{triggerInScene.name}\". [click to toggle]\n{e.Message}", triggerInScene);
							return;
						}
					}
				}

				if (spriteRenderer != null && spriteRenderer.sortingLayerName == "Overlay") {
					if (objectNodeType == "overlay") {
						Vectorier.Core.Components.Trigger.Convert(
							go: triggerInScene,
							node: node,
							floatPrecision: Vectorier.Settings.GetPrecision(Vectorier.Settings.Elements.TriggerPrecisionKey)
						);
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

					Vectorier.Core.Components.Area.Convert(
						go: areaInScene,
						node: node,
						floatPrecision: Vectorier.Settings.GetPrecision(Vectorier.Settings.Elements.AreaPrecisionKey)
					);
				}

				foreach (GameObject modelInScene in GameObject.FindGameObjectsWithTag("Model")) {
					if (!buildMap.IsVisible(modelInScene)) {
						continue;
					}

					Transform parent = modelInScene.transform.parent;

					if (parent != null && parent.CompareTag("Dynamic")) {
						continue;
					}

					Vectorier.Core.Components.Model.Convert(
						go: modelInScene,
						node: node,
						floatPrecision: Vectorier.Settings.GetPrecision(Vectorier.Settings.Elements.ModelPrecisionKey)
					);
				}

				// Camera
				foreach (GameObject camInScene in GameObject.FindGameObjectsWithTag("Camera")) {
					// Note: This is actually a trigger, but with camera zoom properties
					if (!buildMap.IsVisible(camInScene)) {
						continue;
					}

					UnityEngine.Transform parent = camInScene.transform.parent;
					if (parent != null && parent.CompareTag("Dynamic")) {
						continue;
					}

					Vectorier.Core.Components.Camera.Convert(
						go: camInScene,
						node: node,
						floatPrecision: Vectorier.Settings.GetPrecision(Vectorier.Settings.Elements.CameraPrecisionKey)
					);
				}

				// Animation
				foreach (GameObject animationInScene in GameObject.FindGameObjectsWithTag("Animation")) {
					if (!buildMap.IsVisible(animationInScene)) {
						continue;
					}

					UnityEngine.Transform parent = animationInScene.transform.parent;

					if (parent != null && parent.CompareTag("Dynamic")) {
						continue;
					}

					Vectorier.Core.Components.Animation_.Convert(
						go: animationInScene,
						node: node,
						floatPrecision: Vectorier.Settings.GetPrecision(Vectorier.Settings.Elements.AnimationPrecisionKey)
					);
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

				var spriteRenderer = imageInScene.GetComponent<SpriteRenderer>();

				if (objectNodeType == "default" && spriteRenderer.sortingLayerName == "Default") {
					Vectorier.Core.Components.Image.Convert(
						go: imageInScene,
						node: node,
						floatPrecision: Vectorier.Settings.GetPrecision(Vectorier.Settings.Elements.ImagePrecisionKey)
					);
				} else if (objectNodeType == "overlay" && spriteRenderer.sortingLayerName == "Overlay") {
					Vectorier.Core.Components.Image.Convert(
						go: imageInScene,
						node: node,
						floatPrecision: Vectorier.Settings.GetPrecision(Vectorier.Settings.Elements.ImagePrecisionKey)
					);
				}
			}

			if (objectNodeType == "default") {
				// Dynamic
				foreach (GameObject dynamicInScene in GameObject.FindGameObjectsWithTag("Dynamic")) {
					if (!buildMap.IsVisible(dynamicInScene)) {
						continue;
					}

					// todo: modularize more
					buildMap.ConvertToDynamic(
						go: dynamicInScene,

						node: node,
						xml: xml
					);
				}

				foreach (GameObject objectInScene in GameObject.FindGameObjectsWithTag("Object")) {
					if (!buildMap.IsVisible(objectInScene)) {
						continue;
					}

					UnityEngine.Transform parent = objectInScene.transform.parent;
					if (parent != null && parent.CompareTag("Dynamic")) {
						// If the parent has the tag "Dynamic" skip this GameObject and continue.
						continue;
					}

					Vectorier.Core.Components.Object.Convert(
						go: objectInScene,
						node: node,
						floatPrecision: Vectorier.Settings.GetPrecision(Vectorier.Settings.Elements.ObjectPrecisionKey)
					);
				}
			}

			// todo: handle
			// MissingComponentException: There is no 'SpriteRenderer' attached to the "GameObject" game object, but a script is trying to access it.
			GameObject[] backdropsInScene = GameObject.FindGameObjectsWithTag("Backdrop")
				.OrderBy(obj => obj.GetComponent<SpriteRenderer>()?.sortingOrder ?? 0)
				.ToArray();

			GameObject[] topImagesInScene = GameObject.FindGameObjectsWithTag("Top Image")
				.OrderBy(obj => obj.GetComponent<SpriteRenderer>()?.sortingOrder ?? 0)
				.ToArray();

			// Combine arrays
			GameObject[] combinedSceneObjects = backdropsInScene.Concat(topImagesInScene).ToArray();

			ProcessSomeSprites(
				gos: combinedSceneObjects,

				node: node,
				nodeType: objectNodeType,

				component: buildMap
			);
		}

		// -=-=-=- //
		// Write properties

		// PC
		if (createArchives) {
			string originalPath_ListPayedPC = Path.Combine(XmlCommonDir, "List_Payed" + "." + Vectorier.Core.Game.Extensions.File.XML);
			string backupPath_ListPayedPC = Path.Combine(XmlCommonDir, "_original", "List_Payed" + "." + Vectorier.Core.Game.Extensions.File.XML);

			Vectorier.Core.Archive.Track.Level.Content.Properties(
				allObjects,

				buildMap.mapToOverride,

				originalPath_ListPayedPC,
				backupPath_ListPayedPC
			);

			// Android
			string originalPath_ListPayedMobile = Path.Combine(XmlCommonDir, "list_paid_mob" + "." + Vectorier.Core.Game.Extensions.File.XML);
			string backupPath_ListPayedMobile = Path.Combine(XmlCommonDir, "_original", "list_paid_mob" + "." + Vectorier.Core.Game.Extensions.File.XML);

			Vectorier.Core.Archive.Track.Level.Content.Properties(
				allObjects,

				buildMap.mapToOverride,

				originalPath_ListPayedMobile,
				backupPath_ListPayedMobile
			);

			string backupPath_GUI_2048 = Path.Combine(XmlGui2048Dir, "_original");

			Vectorier.Core.Archive.Track.Level.Content.WriteLevelThumbnail(
				buildMap.thumbnailImagePath,
				scenePath,

				buildMap.mapToOverride,

				XmlGui2048Dir,
				backupPath_GUI_2048
			);

			string originalFileFull_Interface1 = Path.Combine(XmlGui2048Dir, "scene_buttons_2048" + "." + Vectorier.Core.Game.Extensions.File.Image.Static[0]);
			string backupFileFull_Interface1 = Path.Combine(XmlGui2048Dir, "_original", "scene_buttons_2048" + "." + Vectorier.Core.Game.Extensions.File.Image.Static[0]);
			string backupFileEmpty_Interface1 = Path.Combine(XmlGui2048Dir, "_original", "scene_buttons_2048" + "_empty" + "." + Vectorier.Core.Game.Extensions.File.Image.Static[0]);

			Vectorier.Core.Archive.Track.Level.Content.WriteInGameInterface(
				buildMap.transparentInterfaceButtons,

				XmlGui2048Dir,
				originalFileFull_Interface1,
				backupFileEmpty_Interface1,

				backupPath_GUI_2048,
				backupFileFull_Interface1
			);

			string originalPath_LocalizationAll = Path.Combine(XmlCommonDir, "localization_all" + "." + Vectorier.Core.Game.Extensions.File.XML);
			string backupPath_LocalizationAll = Path.Combine(XmlCommonDir, "_original", "localization_all" + "." + Vectorier.Core.Game.Extensions.File.XML);

			Vectorier.Core.Archive.Track.Level.Content.WriteLevelName(
				buildMap.title,

				buildMap.mapToOverride,

				originalPath_LocalizationAll,
				backupPath_LocalizationAll
			);
		}

		// -=-=-=- //
		// Center objects

		if (Vectorier.Settings.CenterUnnamedObjects) {
			Vectorier.Core.XML.Utils.Optimize.Objects(
				trackNode,
				Vectorier.Settings.GetPrecision("VectorierSettings.Elements.Properties.Object.Precision")
			);
		}

		if (Vectorier.Settings.SortNodeAttributes) {
			Vectorier.Core.XML.Utils.Optimize.Attributes(
				mapRootNode,
				Vectorier.Settings.OrdredAttributes.ToArray()
			);
		}

		if (Vectorier.Settings.ValidateWrittenTrackXml) {
			bool ok = XmlConfirmationWindow.ShowModal(XmlTrackFileFull);

			if (!ok) {
				Debug.Log("XML confirmation canceled.");
				return;
			}
		}

		xml.Save(XmlTrackFileFull);

		// -=-=-=- //
		// XML

		int originalXmlSizeBytes = (int)new FileInfo(XmlTrackFileFull).Length;
		string originalXmlSize = buildMap.BytesToString(originalXmlSizeBytes);

		if (buildMap.optimizeWrittenTrack) {
			Vectorier.Core.XML.Utils.Optimize.General(
				fileInput: XmlTrackFileFull,
				nodeName: "Track"
			);
		}

		int optimizedXmlSizeBytes = (int)new FileInfo(XmlTrackFileFull).Length;
		string optimizedXmlSize = buildMap.BytesToString(optimizedXmlSizeBytes);

		Dictionary<string, string> finalPaths = new Dictionary<string, string>();
		if (createArchives) {
			finalPaths = buildMap.CreateArchives(compressionAlgorithm, allObjects);
			buildMap.hunterPlaced = false;
		}

		// -=-=-=- //

		// If the build was for running the game, invoke the MapBuilt event
		if (buildForRunGame) {
			MapBuilt?.Invoke();

			// Reset the flag after the build
			buildForRunGame = false;
		}

		// Stop stopwatch
		stopwatch.Stop();
		TimeSpan ts = stopwatch.Elapsed;

		// Build log message
		string algoString = "";

		if (createArchives) {
			if (!Directory.Exists(XmlCompiledDir)) {
				Directory.CreateDirectory(XmlCompiledDir);
			}

			algoString = $" with {compressionAlgorithm.ToUpper()}";
		}

		string logMessage = $"Building done{algoString} ({ts.TotalSeconds:F3} seconds) [XML: {optimizedXmlSize}]";

		// if promptConfirmation is false
		if (finalPaths == null) {
			// Debug.Log("No archive output paths were specified");
			return;
		} else if (finalPaths.Count > 0) {
			foreach (var kvp in finalPaths.Reverse()) {
				string key = kvp.Key;
				string value = kvp.Value;
				string sizeFile = buildMap.BytesToString((int)new FileInfo(value).Length);
				logMessage += $" [{key}: {sizeFile}]";
			}
		}

		if (buildMap.optimizeWrittenTrack) {
			string removedXmlBytes = buildMap.BytesToString(originalXmlSizeBytes - optimizedXmlSizeBytes);

			float percentage = originalXmlSizeBytes > 0
				? 100f - ((float)optimizedXmlSizeBytes / originalXmlSizeBytes * 100f)
				: 0f;

			logMessage +=
				$"\nOld size (XML): {originalXmlSize} (-{percentage:F2}%) [-{removedXmlBytes}]";
		}

		TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(
			Path.Combine("Assets", XmlTrackFile).Replace(
				Path.DirectorySeparatorChar,
				Path.AltDirectorySeparatorChar
			)
		);
		Debug.LogSuccess(logMessage, asset);
	}

	// -=-=-=- //
	// Processes helpers

	string WildcardToRegex(string pattern) {
		return "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
	}

	void RunProcess(string exePath, string arguments) {
		var startInfo = new ProcessStartInfo {
			FileName = exePath,
			Arguments = arguments,
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};

		using (var process = new Process { StartInfo = startInfo }) {
			process.Start();

			string output = process.StandardOutput.ReadToEnd();
			string error = process.StandardError.ReadToEnd();

			process.WaitForExit();

			if (process.ExitCode != 0) {
				Debug.LogError($"Process failed: {exePath}\nArgs: {arguments}\nOutput: {output}\nError: {error}");
			}
		}
	}

	void RunBatch(string batchPath) {
		string absBatchPath = Path.GetFullPath(batchPath);
		string batchDir = Path.GetDirectoryName(absBatchPath);

		if (!File.Exists(absBatchPath)) {
			Debug.LogError($"Batch file not found: {absBatchPath}");
			return;
		}

		var process = new Process {
			StartInfo = new ProcessStartInfo {
				FileName = absBatchPath,
				WorkingDirectory = batchDir, // make sure it's the batch folder
				UseShellExecute = true,
				CreateNoWindow = true
			}
		};

		try {
			process.Start();
			if (!process.WaitForExit(2 * 60 * 1000)) {
				Debug.LogError($"Timeout waiting for {absBatchPath}");
				process.Kill();
			}
		} finally {
			process.Close();
		}
	}

	void KillProcess(string? name = null) {
		if (name == null) {
			return;
		}

		foreach (Process process in Process.GetProcessesByName(name)) {
			if (!process.HasExited) {
				Debug.LogWarning($"Closing process named \"{name}\"");
				process.Kill();
				process.WaitForExit();
			}
		}
	}

	// -=-=-=- //
	// Archive builders

	public static string GetRelativePath(
		string basePath,
		string targetPath
	) {
		basePath = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		targetPath = Path.GetFullPath(targetPath);

		string[] baseDirs = basePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		string[] targetDirs = targetPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

		// find common root
		int i = 0;
		while (i < baseDirs.Length && i < targetDirs.Length && string.Equals(baseDirs[i], targetDirs[i], StringComparison.OrdinalIgnoreCase)) {
			i++;
		}

		// add pardir for each remaining baseDir
		string[] relativeDirs = new string[baseDirs.Length - i + targetDirs.Length - i];
		for (int j = 0; j < baseDirs.Length - i; j++) {
			relativeDirs[j] = "..";
		}

		Array.Copy(targetDirs, i, relativeDirs, baseDirs.Length - i, targetDirs.Length - i);

		return string.Join(Path.DirectorySeparatorChar.ToString(), relativeDirs);
	}

	void WriteDclConfig(
		string fileName,
		string configDir,

		string baseDir,

		List<string> files,

		string compressionAlgorithm
	) {
		foreach (string file in Directory.GetFiles(configDir, "*." + Vectorier.Core.Game.Extensions.File.Archive.Config)) {
			// auto extension add
			string file_n = Path.GetFileNameWithoutExtension(file) + "." + Vectorier.Core.Game.Extensions.File.Archive.Config;

			if (!file_n.Contains(fileName)) {
				continue;
			}

			using (StreamWriter fileObj = new StreamWriter(file)) {
				string basePath = Path.Combine(Application.dataPath, baseDir);
				
				// folder of the current archive config (or batch) to compute relative paths
				string dclDir = Path.GetDirectoryName(file);

				// relative archive path
				string archiveFullPath = Path.Combine(XmlCompiledDir, Path.GetFileNameWithoutExtension(file) + "." + Vectorier.Core.Game.Extensions.File.Archive.Compiled);
				string archiveRelative = GetRelativePath(dclDir, archiveFullPath);
				archiveRelative = archiveRelative.Replace(
					Path.AltDirectorySeparatorChar,
					Path.DirectorySeparatorChar
				);
				fileObj.WriteLine($"archive \"{archiveRelative}\"");

				// relative basedir path
				string baseDirRelative = GetRelativePath(dclDir, basePath);
				baseDirRelative = baseDirRelative.Replace(
					Path.AltDirectorySeparatorChar,
					Path.DirectorySeparatorChar
				);
				fileObj.WriteLine($"basedir \"{baseDirRelative}\"");

				// unique relative XML files
				HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				List<string> uniqueFiles = new List<string>();

				foreach (string xmlFile in files) {
					// compute relative path to basePath using your custom GetRelativePath
					string relativePath = GetRelativePath(basePath, xmlFile)
						// normalize separators
						.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

					// ensure uniqueness
					if (seen.Add(relativePath)) {
						uniqueFiles.Add(relativePath);
					}
				}

				// sort: numeric first, then alphabetical
				uniqueFiles = uniqueFiles
					.OrderBy(f =>
						{
							string name = Path.GetFileNameWithoutExtension(f);
							return int.TryParse(name, out int n) ? n : int.MaxValue; // numbers first
						}
					)
					.ThenBy(f => f, StringComparer.OrdinalIgnoreCase)
					.ToList();

				// Write to file
				for (int i = 0; i < uniqueFiles.Count; i++) {
					string line = $"file \"{uniqueFiles[i]}\" 0 {compressionAlgorithm.ToLower()}";
					if (i < uniqueFiles.Count - 1) {
						fileObj.WriteLine(line);
					} else {
						fileObj.Write(line);
					}
				}
			}
		}
	}

	void CompileAndCopy(
		string batchPath,

		string sourceDz,
		string destinationDz,

		string processToKill
	) {
		if (!File.Exists(batchPath)) {
			Debug.LogError($"{batchPath} file cannot be found");
			return;
		}

		var stopwatch = System.Diagnostics.Stopwatch.StartNew();

		KillProcess(processToKill);
		RunBatch(batchPath);

		string src = Path.GetFullPath(sourceDz);
		string dst = Path.GetFullPath(destinationDz);

		if (!File.Exists(src)) {
			Debug.LogError($"Expected archive not found: {src}");
			stopwatch.Stop();
			return;
		}

		Directory.CreateDirectory(Path.GetDirectoryName(dst));
		File.Copy(src, dst, true);

		stopwatch.Stop();
		TimeSpan ts = stopwatch.Elapsed;
		Debug.Log($"Compiled \"{Path.GetFileName(dst)}\" in {ts.TotalSeconds:F3} seconds");
	}

	// -=-=-=- //
	// Master build

	int GetSampleRate(string filePath) {
		using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
		using (var br = new BinaryReader(fs)) {
			// RIFF header
			if (br.ReadUInt32() != 0x46464952) {
				// "RIFF"
				throw new InvalidDataException("Not a valid RIFF file.");
			}

			fs.Seek(4, SeekOrigin.Current); // skip RIFF chunk size

			if (br.ReadUInt32() != 0x45564157) {
				// "WAVE"
				throw new InvalidDataException("Not a valid WAVE file.");
			}

			// Scan chunks
			while (fs.Position + 8 <= fs.Length) {
				uint chunkId   = br.ReadUInt32();
				uint chunkSize = br.ReadUInt32();

				// Validate chunk size
				if (fs.Position + chunkSize > fs.Length) {
					throw new InvalidDataException("Chunk size extends beyond end of file.");
				}

				// "fmt " chunk
				if (chunkId == 0x20746D66) {
					if (chunkSize < 16) {
						throw new InvalidDataException("fmt chunk too small.");
					}

					ushort audioFormat = br.ReadUInt16();
					ushort numChannels = br.ReadUInt16();
					int sampleRate     = br.ReadInt32();

					return sampleRate;
				} else {
					// Skip chunk payload
					fs.Seek(chunkSize, SeekOrigin.Current);

					// If chunk size is odd, skip pad byte (WAV padding rule)
					if ((chunkSize & 1) == 1 && fs.Position < fs.Length) {
						fs.Seek(1, SeekOrigin.Current);
					}
				}
			}
		}

		throw new InvalidDataException("fmt chunk not found in WAV file.");
	}

	public void ProcessSoundFiles(
		string baseDir,
		string tempDir,

		string executable,

		int targetRate = 22050,
		object? targetChannels = null, // can't be 1

		IEnumerable<string>? excludedFiles = null,
		IEnumerable<string>? silentFiles = null,

		bool swapChannels = false,
		bool invertPolarity = false,

		bool rebuild = true,
		bool optimize = true
	) {
		string projectTempRoot = Path.Combine(Path.GetTempPath(), "Unity", "Projects", "Vectorier");

		// delete it if it exists
		if (Directory.Exists(projectTempRoot)) {
			Directory.Delete(projectTempRoot, true);
		}

		Directory.CreateDirectory(tempDir);

		var createdDirs = new HashSet<string>();
		var soundFiles = Directory.GetFiles(
			baseDir,
			"*" + "." + Vectorier.Core.Game.Extensions.File.Audio.Sound,
			SearchOption.AllDirectories
		).Where(s => !Path.GetFileName(s).StartsWith("."));

		// normalize path lists
		HashSet<string> excludedSet = excludedFiles != null
			? new HashSet<string>(
				excludedFiles.Select(
					p => p.Replace(
						Path.DirectorySeparatorChar,
						Path.AltDirectorySeparatorChar
					)
				), StringComparer.OrdinalIgnoreCase
			)
			: new HashSet<string>();

		HashSet<string> silentSet = silentFiles != null
			? new HashSet<string>(
				silentFiles.Select(
					p => p.Replace(
						Path.DirectorySeparatorChar,
						Path.AltDirectorySeparatorChar
					)
				), StringComparer.OrdinalIgnoreCase
			)
			: new HashSet<string>();

		// copy or generate files
		foreach (var filePath in soundFiles) {
			string relativePath = GetRelativePath(baseDir, filePath).Replace(
				Path.DirectorySeparatorChar,
				Path.AltDirectorySeparatorChar
			);

			// skip excluded
			if (excludedSet.Contains(relativePath)) {
				continue;
			}

			string destPath = Path.Combine(tempDir, relativePath);
			string dir = Path.GetDirectoryName(destPath)!;
			if (!createdDirs.Contains(dir)) {
				Directory.CreateDirectory(dir);
				createdDirs.Add(dir);
			}

			// replace with silence if listed
			if (silentSet.Contains(relativePath)) {
				CreateSilentWav(destPath, 1, 1); // mono silence, 1 sample
				continue;
			}

			// normal copy
			if (!File.Exists(destPath) || rebuild) {
				File.Copy(filePath, destPath, true);
			}
		}

		// resolve target channel mode
		SoundManager.ChannelTypes resolvedChannels = SoundManager.ChannelTypes.Original;
		if (targetChannels is int intVal) {
			if (intVal == 1) {
				resolvedChannels = SoundManager.ChannelTypes.MonoCombined;
			} else if (intVal == 2) {
				resolvedChannels = SoundManager.ChannelTypes.Stereo;
			} else {
				Debug.LogWarning($"Invalid channel integer ({intVal}) passed to sound files processing function - using Original mode.");
				resolvedChannels = SoundManager.ChannelTypes.Original;
			}
		} else if (targetChannels is SoundManager.ChannelTypes chanVal) {
			resolvedChannels = chanVal;
		}

		// process WAV files in parallel
		var wavFiles = Directory.GetFiles(
			tempDir,
			"*" + "." + Vectorier.Core.Game.Extensions.File.Audio.Sound,
			SearchOption.AllDirectories
		);

		Parallel.ForEach(wavFiles, file => {
			// skip files that are silent placeholders
			if (silentSet.Any(s => file.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).EndsWith(s, StringComparison.OrdinalIgnoreCase))) {
				return;
			}

			int sampleRate = GetSampleRate(file);

			// skip if already correct
			//if (sampleRate == targetRate && sampleRate != 0) {
			//	return;
			//}

			string dir = Path.GetDirectoryName(file)!;
			string fileName = Path.GetFileNameWithoutExtension(file);
			string ext = Path.GetExtension(file);

			string tempOut = Path.Combine(dir, fileName + "_temp" + ext);

			// build filters
			var filters = new List<string>();

			// resample
			filters.Add($"aresample={targetRate}:cutoff=1");
			if (targetRate != Vectorier.Core.Game.Audio.Sound.SampleRate) {
				filters.Add($"asetrate={targetRate}");
			}

			// channel mapping
			switch (resolvedChannels) {
				case SoundManager.ChannelTypes.MonoLeft:
					filters.Add("pan=mono|c0=FL");
					break;
				case SoundManager.ChannelTypes.MonoRight:
					filters.Add("pan=mono|c0=FR");
					break;
				case SoundManager.ChannelTypes.Stereo:
					filters.Add("pan=stereo");
					break;
				case SoundManager.ChannelTypes.MonoCombined:
					filters.Add("pan=mono|c0=0.5*c0+0.5*c1");
					break;
				case SoundManager.ChannelTypes.Original:
				default:
					break;
			}

			// invert polarity
			if (invertPolarity) {
				filters.Add("volume=-1");
			}

			// swap channels
			if (swapChannels) {
				filters.Add("channelsplit=channel_layout=stereo[FL][FR];[FR][FL]amerge=inputs=2");
			}

			string filterStr = string.Join(",", filters);

			// optimization flags
			string optStr = optimize ? "" : "-map 0 -map_metadata 0:s:0";
			string chanStr = filterStr.ToLower().Contains("mono") ? "-ac 1" : "";

			string args = $"-y -loglevel 8 -hide_banner -stats -i \"{file}\" -af \"{filterStr}\" {chanStr} {optStr} \"{tempOut}\"";

			// UnityEngine.Debug.Log(args);

			RunProcess(executable, args);

			// replace original file safely
			File.Delete(file);
			File.Move(tempOut, file);
		});
	}

	private static void CreateSilentWav(string path, int sampleRate, int channels) {
		using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write)) {
			using (var bw = new BinaryWriter(fs)) {
				int bitsPerSample = 8;
				int byteRate = sampleRate * channels * bitsPerSample / 8;
				short blockAlign = (short)(channels * bitsPerSample / 8);
				int dataLength = channels * 1; // 1 sample, 8-bit

				// RIFF header
				bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
				bw.Write(36 + dataLength);
				bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

				// fmt chunk
				bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
				bw.Write(16);
				bw.Write((short)1); // PCM
				bw.Write((short)channels);
				bw.Write(sampleRate);
				bw.Write(byteRate);
				bw.Write(blockAlign);
				bw.Write((short)bitsPerSample);

				// data chunk
				bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
				bw.Write(dataLength);
				// write silence (0x80 for 8-bit unsigned PCM)
				for (int i = 0; i < channels; i++) {
					bw.Write((byte)128);
				}
			}
		}
	}

	string? GetPackagePath(string package, string? envVar = "PATH") {
		bool windows = Environment.OSVersion.Platform == PlatformID.Win32NT;
		string exeName = windows ? $"{package}.exe" : package;

		List<string> candidates = new List<string>();

		// CASE 1: Try `where` or `which` to find all occurrences
		try {
			string cmd = windows ? "where" : "which";

			var process = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = cmd,
					Arguments = package,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				}
			};

			process.Start();
			string? line;

			while ((line = process.StandardOutput.ReadLine()) != null) {
				if (File.Exists(line)) {
					candidates.Add(Path.GetFullPath(line));
				}
			}

			process.WaitForExit();
		} catch {
			// ignore
		}

		// CASE 2: If PATH variable exists, manually scan it
		if (!string.IsNullOrEmpty(envVar)) {
			string? paths = Environment.GetEnvironmentVariable(envVar);

			if (!string.IsNullOrEmpty(paths)) {
				foreach (
					string raw in paths.Split(
						new[] { Path.PathSeparator },
						StringSplitOptions.RemoveEmptyEntries
					)
				) {
					string entry = raw.Trim();

					// CASE 2A: PATH entry is directly an executable
					if (
						File.Exists(entry) && 
						string.Equals(
							Path.GetFileName(entry),
							exeName,
							StringComparison.OrdinalIgnoreCase
						)
					) {
						candidates.Add(Path.GetFullPath(entry));
						continue;
					}

					// CASE 2B: PATH entry is a directory
					if (Directory.Exists(entry)) {
						string full = Path.Combine(entry, exeName);

						if (File.Exists(full)) {
							candidates.Add(Path.GetFullPath(full));
						}
					}
				}
			}
		}

		if (candidates.Count < 1) {
			return null;
		}

		// sort by parent directory modification date (descending)
		return candidates
			.OrderByDescending(path =>
			{
				try
				{
					var dir = Path.GetDirectoryName(path);
					if (dir == null)
						return DateTime.MinValue;

					return Directory.GetLastWriteTimeUtc(dir);
				}
				catch
				{
					return DateTime.MinValue;
				}
			})
			.First();

	}

	Dictionary<string, string> CreateArchives(
		string compressionAlgorithm,

		GameObject[] gos
	) {
		string processToKill = Path.GetFileNameWithoutExtension(gameExecutablePath);

		bool isOptimized = compressionAlgorithm.ToLower() != "dz";

		string configDir = isOptimized ? XmlCompilerOptimizedConfigDir : XmlCompilerStandardConfigDir;
		string buildDir = isOptimized ? XmlCompilerOptimizedBuildDir : XmlCompilerStandardBuildDir;

		string finalCommonPath = Path.Combine(gameDirectoryPath, "common_xml" + "." + Vectorier.Core.Game.Extensions.File.Archive.Compiled);
		string finalGuiPath = Path.Combine(gameDirectoryPath, "GUI_2048_1536" + "." + Vectorier.Core.Game.Extensions.File.Archive.Compiled);
		string finalSoundPath = Path.Combine(gameDirectoryPath, "sound" + "." + Vectorier.Core.Game.Extensions.File.Archive.Compiled);
		string finalTrackContent2048Path = Path.Combine(gameDirectoryPath, "track_content_2048" + "." + Vectorier.Core.Game.Extensions.File.Archive.Compiled);
		string finalLevelPath = Path.Combine(gameDirectoryPath, "level_xml" + "." + Vectorier.Core.Game.Extensions.File.Archive.Compiled);

		string tempDir = Path.Combine(Path.GetTempPath(), /*"Unity", "Projects",*/ "_" + "Vectorier");
		string tempDirArchives = Path.Combine(tempDir, "archives");
		string tempTexturesDir = Path.Combine(tempDirArchives, "track_content_2048");
		string tempSoundDir = Path.Combine(tempDirArchives, "sound");

		var buildMap = FindObjectOfType<BuildMap>();

		// --------------------
		// LEVEL XML
		// --------------------

		// fetch level XML files
		IEnumerable<string> levelXmlFiles = Directory.GetFiles(
			Path.Combine(Application.dataPath, XmlDzipLvlDir),
			"*." + Vectorier.Core.Game.Extensions.File.XML,
			buildMap.useCustomProperties ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly
		);

		Regex[] excludedRegexes = Vectorier.Core.XML.Utils.UnusedTracks
			.Select(pattern => new Regex(WildcardToRegex(pattern), RegexOptions.IgnoreCase | RegexOptions.Compiled))
			.ToArray();

		if (!Vectorier.Settings.WriteUnusedTracks) {
			levelXmlFiles = levelXmlFiles.Where(f => !excludedRegexes.Any(r => r.IsMatch(Path.GetFileName(f))));
		}

		List<string> levelXmlDirFiles = levelXmlFiles.ToList();

		// add skins if not using custom properties
		if (!buildMap.useCustomProperties) {
			var allSkins = new List<string>();

			allSkins.AddRange(Vectorier.Core.XML.Utils.ParseSkins(buildMap.Player.playerSkins, true) as List<string>);
			allSkins.AddRange(Vectorier.Core.XML.Utils.ParseSkins(buildMap.Hunter.hunterSkins, true) as List<string>);
			allSkins.AddRange(Vectorier.Core.XML.Utils.ParseSkins(buildMap.Helper.helperSkins, true) as List<string>);

			allSkins = allSkins.Select(s => Path.Combine(Application.dataPath, XmlDzipLvlDir, s) + "." + Vectorier.Core.Game.Extensions.File.XML).ToList();

			levelXmlDirFiles.AddRange(allSkins);
		}

		levelXmlDirFiles.Sort();

		WriteDclConfig(
			Path.GetFileNameWithoutExtension(finalLevelPath),
			configDir,

			XmlDzipLvlDir,
			levelXmlDirFiles,

			compressionAlgorithm
		);

		// --------------------
		// TRACK_CONTENT_2048
		// --------------------

		// ensure temp directory is clean
		if (Directory.Exists(tempTexturesDir)) {
			Directory.Delete(tempTexturesDir, true);
		}

		Directory.CreateDirectory(tempTexturesDir);
		// Debug.Log($"Temp textures directory created: {tempTexturesDir}");

		// build extension set from all static fields in the Image class
		var allowedExtensions =
			new HashSet<string>(
				typeof(Vectorier.Core.Game.Extensions.File.Image)
				.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
				.SelectMany(f => {
					object v = f.GetValue(null);

					if (v is string s) {
						return new[] { s.ToLowerInvariant() };
					}

					if (v is IEnumerable<string> arr) {
						return arr.Select(x => x.ToLowerInvariant());
					}

					return Enumerable.Empty<string>();
				})
			);

		// fetch textures
		var levelTexturesRaw = Directory
			.EnumerateFiles(Path.Combine(Application.dataPath, XmlDzipTexturesBaseDir), "*", SearchOption.TopDirectoryOnly)
			.Where(f => allowedExtensions.Contains(Path.GetExtension(f).Trim('.').ToLower()))
			.ToList();

		var levelTextures = new List<string>();
		var normalizedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var path in levelTexturesRaw) {
			string norm = Path.GetFullPath(path).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);

			if (normalizedPaths.Add(norm)) {
				levelTextures.Add(path);
			}
		}

		// add spriteRenderer textures
		foreach (var go in gos) {
			var sr = go.GetComponent<SpriteRenderer>();

			if (sr != null && sr.enabled && sr.sprite != null && sr.sprite.texture != null) {
				string path = AssetDatabase.GetAssetPath(sr.sprite.texture);

				if (!string.IsNullOrEmpty(path)) {
					string norm = Path.GetFullPath(path).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);

					if (normalizedPaths.Add(norm)) {
						levelTextures.Add(path);
					}
				}
			}
		}

		// levelTextures.Sort();
		// Debug.Log($"Total textures collected: {levelTextures.Count}");

		// copy flattened files with duplicates check
		var fileNameSources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		foreach (var src in levelTextures) {
			string fileName = Path.GetFileName(src);

			if (fileNameSources.TryGetValue(fileName, out var existing)) {
				if (!string.Equals(existing, src, StringComparison.OrdinalIgnoreCase)) {
					Debug.LogError(
						$"Duplicate texture filename detected: \"{fileName}\"\n" +
						$"- {existing}\n" +
						$"- {src}\n"
					);
					return null;
				}
				// same file, skip
				continue;
			}

			fileNameSources[fileName] = src;

			string dst = Path.Combine(tempTexturesDir, fileName);

			try {
				if (!File.Exists(dst)) {
					File.Copy(src, dst, false);
				}
			} catch (Exception ex) {
				Debug.LogError($"Failed to copy file \"{src}\" → \"{dst}\"\n{ex}");
			}
		}

		// write config using flattened temp files
		var textureFilesFlattened = fileNameSources.Values
			.Select(p => Path.Combine(tempTexturesDir, Path.GetFileName(p)))
			.ToList();

		WriteDclConfig(
			Path.GetFileNameWithoutExtension(finalTrackContent2048Path),
			configDir,

			tempTexturesDir,
			textureFilesFlattened,

			compressionAlgorithm
		);

		// Debug.Log($"Written with {textureFilesFlattened.Count} textures.");

		if (Directory.Exists(tempTexturesDir)) {
			//Directory.Delete(tempTexturesDir, true);
		}

		// --------------------
		// SOUNDS
		// --------------------

		IEnumerable<string> soundFiles = Directory.GetFiles(
			Path.Combine(Application.dataPath, XmlDzipSoundDir),
			"*.wav",
			SearchOption.AllDirectories
		);

		List<string> soundDirFiles = soundFiles.ToList();

		soundDirFiles.Sort();

		// process sounds first
		var soundManager = FindObjectOfType<SoundManager>();
		bool soundManagerActive = soundManager != null && soundManager.enabled;

		string pkg = "ffmpeg";
		ProcessSoundFiles(
			baseDir: Path.Combine(Application.dataPath, XmlDzipSoundDir),
			tempDir: tempSoundDir,

			executable: GetPackagePath(pkg) ?? Path.Combine(Application.dataPath, XmlDzipDir, "executable", pkg),

			targetRate: !soundManagerActive ? 22050 : soundManager.SampleRate,
			targetChannels: !soundManagerActive ? (object)Vectorier.Core.Game.Audio.Sound.Channels : (object)soundManager.Channel, // i hate this

			excludedFiles: !soundManagerActive ? null : soundManager._ExcludedNames,
			silentFiles: !soundManagerActive ? null : soundManager._SilentNames,

			swapChannels: !soundManagerActive ? false : soundManager.SwapChannels,
			invertPolarity: !soundManagerActive ? false : soundManager.InvertPolarity,

			optimize: buildMap.optimizeWrittenTrack,
			rebuild: !soundManagerActive ? false : soundManager.Rebuild
		);

		// use temporary sound dir
		WriteDclConfig(
			Path.GetFileNameWithoutExtension(finalSoundPath),
			configDir,

			tempSoundDir,
			Directory.GetFiles(
				tempSoundDir,
				"*" + "." + Vectorier.Core.Game.Extensions.File.Audio.Sound,
				SearchOption.AllDirectories
			).ToList(),

			compressionAlgorithm
		);

		if (Directory.Exists(tempSoundDir)) {
			//Directory.Delete(tempSoundDir, true);
		}

		// --------------------
		// COMPILE ARCHIVES
		// --------------------

		KillProcess(processToKill);

		#if UNITY_EDITOR

		// level_xml
		CompileAndCopy(
			Path.Combine(buildDir, Path.GetFileNameWithoutExtension(finalLevelPath) + ".bat"),
			Path.Combine(XmlCompiledDir, Path.GetFileNameWithoutExtension(finalLevelPath) + "." + Vectorier.Core.Game.Extensions.File.Archive.Compiled),
			finalLevelPath,
			processToKill
		);

		// common_xml
		CompileAndCopy(
			Path.Combine(buildDir, Path.GetFileNameWithoutExtension(finalCommonPath) + ".bat"),
			Path.Combine(XmlCompiledDir, Path.GetFileNameWithoutExtension(finalCommonPath) + "." + Vectorier.Core.Game.Extensions.File.Archive.Compiled),
			finalCommonPath,
			processToKill
		);

		// track_content_2048
		CompileAndCopy(
			Path.Combine(buildDir, Path.GetFileNameWithoutExtension(finalTrackContent2048Path) + ".bat"),
			Path.Combine(XmlCompiledDir, Path.GetFileNameWithoutExtension(finalTrackContent2048Path) + "." + Vectorier.Core.Game.Extensions.File.Archive.Compiled),
			finalTrackContent2048Path,
			processToKill
		);

		// GUI_2048_1536
		CompileAndCopy(
			Path.Combine(buildDir, Path.GetFileNameWithoutExtension(finalGuiPath) + ".bat"),
			Path.Combine(XmlCompiledDir, Path.GetFileNameWithoutExtension(finalGuiPath) + "." + Vectorier.Core.Game.Extensions.File.Archive.Compiled),
			finalGuiPath,
			processToKill
		);

		// sound
		CompileAndCopy(
			Path.Combine(buildDir, Path.GetFileNameWithoutExtension(finalSoundPath) + ".bat"),
			Path.Combine(XmlCompiledDir, Path.GetFileNameWithoutExtension(finalSoundPath) + "." + Vectorier.Core.Game.Extensions.File.Archive.Compiled),
			finalSoundPath,
			processToKill
		);

		// -=-=-=- //

		// trigger event if building for game run
		if (buildForRunGame) {
			MapBuilt?.Invoke();
			buildForRunGame = false;
		}

		return new Dictionary<string, string> {
			{ Path.GetFileNameWithoutExtension(finalCommonPath), finalCommonPath },
			{ Path.GetFileNameWithoutExtension(finalGuiPath), finalGuiPath },
			{ Path.GetFileNameWithoutExtension(finalSoundPath), finalSoundPath },
			{ Path.GetFileNameWithoutExtension(finalLevelPath), finalLevelPath },
			{ Path.GetFileNameWithoutExtension(finalTrackContent2048Path), finalTrackContent2048Path },
		};
	}

	// -=-=-=- //
	// kind of salt in the eye

	public void ConvertToDynamic(
		GameObject go,

		XmlNode node,
		XmlDocument xml
	) {
		var buildMap = FindObjectOfType<BuildMap>();

		// MULTI-DYNAMIC SUPPORT
		var dynamicComponents = go.GetComponents<Dynamic>();

		bool dynComp = dynamicComponents == null || dynamicComponents.Length == 0;
		if (dynComp) {
			// Debug.LogWarning(@$"""{go.name}"" has no dynamic component, writing as standalone object node", go);
			// return;
		}

		UnityEngine.Transform transform = go.transform;

		XmlElement objectElement = xml.CreateElement("Object");

		if (dynComp) {
			objectElement.SetAttribute("Label", Vectorier.Core.Helpers.Get.Name(go));
		}

		XmlElement propertiesElement = xml.CreateElement("Properties");
		XmlElement dynamicRootElement = xml.CreateElement("Dynamic");

		// MULTIPLE DYNAMIC COMPONENTS LOOP
		foreach (var dynamicComponent in dynamicComponents) {
			if (!dynamicComponent.enabled) {
				continue;
			}

			if (string.IsNullOrWhiteSpace(dynamicComponent.TransformationName)) {
				Debug.LogError($"{go.name} dynamic component has no transformation name", go);
				continue;
			}

			// Create Transformation node for this Dynamic component
			XmlElement transformationElement = xml.CreateElement("Transformation");
			transformationElement.SetAttribute("Name", dynamicComponent.TransformationName);

			// MOVE element
			XmlElement moveElement = xml.CreateElement("Move");

			// Build list of intervals
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

			// Process each interval
			foreach (var moveInterval in moveIntervals) {
				if (moveInterval.UseMovement) {
					XmlElement moveIntervalElement = xml.CreateElement("MoveInterval");
					moveIntervalElement.SetAttribute("Number", moveInterval.Number);

					int framesToMove = Mathf.Max(1, Mathf.RoundToInt(moveInterval.Interval.MoveDuration * Vectorier.Core.Game.FrameRate));
					if (framesToMove != 0) {
						moveIntervalElement.SetAttribute("FramesToMove", framesToMove.ToString());
					}

					int delayFrames = Mathf.RoundToInt(moveInterval.Interval.Delay * Vectorier.Core.Game.FrameRate);
					if (delayFrames != 0) {
						moveIntervalElement.SetAttribute("Delay", delayFrames.ToString());
					}

					float moveXaxis = moveInterval.Interval.MoveXAxis * Vectorier.Core.Game.UnitScale;
					float moveYaxis = -moveInterval.Interval.MoveYAxis * Vectorier.Core.Game.UnitScale;

					float supportXaxis = moveInterval.Interval.SupportXAxis * Vectorier.Core.Game.UnitScale;
					float supportYaxis = -moveInterval.Interval.SupportYAxis * Vectorier.Core.Game.UnitScale;

					XmlElement startPointElement = xml.CreateElement("Point");
					startPointElement.SetAttribute("Name", "Start");

					XmlElement supportPointElement = xml.CreateElement("Point");
					supportPointElement.SetAttribute("Name", "Support");
					supportPointElement.SetAttribute("Number", moveInterval.Number);

					if (supportXaxis != 0) {
						supportPointElement.SetAttribute("X", Vectorier.Core.Helpers.ToString(supportXaxis));
					}

					if (supportYaxis != 0) {
						supportPointElement.SetAttribute("Y", Vectorier.Core.Helpers.ToString(supportYaxis));
					}

					XmlElement finishPointElement = xml.CreateElement("Point");
					finishPointElement.SetAttribute("Name", "Finish");

					if (moveXaxis != 0) {
						finishPointElement.SetAttribute("X", Vectorier.Core.Helpers.ToString(moveXaxis));
					}

					if (moveYaxis != 0) {
						finishPointElement.SetAttribute("Y", Vectorier.Core.Helpers.ToString(moveYaxis));
					}

					moveIntervalElement.AppendChild(startPointElement);
					moveIntervalElement.AppendChild(supportPointElement);
					moveIntervalElement.AppendChild(finishPointElement);

					moveElement.AppendChild(moveIntervalElement);
				}
			}

			if (moveElement.HasChildNodes) {
				transformationElement.AppendChild(moveElement);
			}

			if (transformationElement.HasChildNodes) {
				// append this transformation to <Dynamic>
				dynamicRootElement.AppendChild(transformationElement);
			}
		}

		// Add dynamic section to properties
		if (dynamicRootElement.HasChildNodes) {
			propertiesElement.AppendChild(dynamicRootElement);
		}

		if (propertiesElement.HasChildNodes) {
			objectElement.AppendChild(propertiesElement);
		}

		XmlElement contentElement = xml.CreateElement("Content");

		List<GameObject> imageObjects = new List<GameObject>();

		foreach (UnityEngine.Transform child in transform) {
			if (!buildMap.IsVisible(child.gameObject)) {
				continue;
			}

			if (child.gameObject.CompareTag("Image")) {
				imageObjects.Add(child.gameObject);
			}
		}

		imageObjects = imageObjects.OrderBy(x => x.name.Length).ThenBy(x => x.name).ToList();

		imageObjects.Sort((a, b) => {
			var rendererA = a.GetComponent<SpriteRenderer>();
			var rendererB = b.GetComponent<SpriteRenderer>();

			int orderA = rendererA ? rendererA.sortingOrder : 0;
			int orderB = rendererB ? rendererB.sortingOrder : 0;

			return orderA.CompareTo(orderB);
		});

		foreach (GameObject imageObject in imageObjects) {
			Vectorier.Core.Components.Image.Convert(
				go: imageObject,
				node: node,
				parentElement: contentElement,
				floatPrecision: Vectorier.Settings.GetPrecision(Vectorier.Settings.Elements.ImagePrecisionKey)
			);
		}

		// Remaining child object conversion untouched
		foreach (UnityEngine.Transform child in transform) {
			if (child.name == "Camera") {
				continue;
			}

			if (!buildMap.IsVisible(child.gameObject)) {
				continue;
			}

			// Platform
			if (child.gameObject.CompareTag("Platform")) {
				Vectorier.Core.Components.Platform.Convert(
					go: child.gameObject,
					node: node,
					parentElement: contentElement,
					floatPrecision: Vectorier.Settings.GetPrecision(Vectorier.Settings.Elements.PlatformPrecisionKey)
				);
			}

			// Trapezoid
			if (child.gameObject.CompareTag("Trapezoid")) {
				Vectorier.Core.Components.Trapezoid.Convert(
					go: child.gameObject,
					node: node,
					parentElement: contentElement,
					floatPrecision: Vectorier.Settings.GetPrecision(Vectorier.Settings.Elements.TrapezoidPrecisionKey)
				);
			}

			// Area
			if (child.gameObject.CompareTag("Area")) {
				Vectorier.Core.Components.Area.Convert(
					go: child.gameObject,
					node: node,
					parentElement: contentElement,
					floatPrecision: Vectorier.Settings.GetPrecision(Vectorier.Settings.Elements.AreaPrecisionKey)
				);
			}

			// Trigger
			if (child.gameObject.CompareTag("Trigger")) {
				var dynamicTrigger = child.GetComponent<DynamicTrigger>();
				var triggerSettings = child.GetComponent<TriggerSettings>();

				XmlElement T_element = xml.CreateElement("Trigger");
				T_element.SetAttribute("X", (child.transform.position.x * Vectorier.Core.Game.UnitScale).ToString(CultureInfo.InvariantCulture));
				T_element.SetAttribute("Y", (-child.transform.position.y * Vectorier.Core.Game.UnitScale).ToString(CultureInfo.InvariantCulture));

				var spriteRenderer = child.GetComponent<SpriteRenderer>();
				if (spriteRenderer != null && spriteRenderer.enabled && spriteRenderer.sprite != null) {
					Bounds bounds = spriteRenderer.sprite.bounds;
					Vector3 scale = child.transform.lossyScale;

					float width = bounds.size.x * Vectorier.Core.Game.UnitScale;
					float height = bounds.size.y * Vectorier.Core.Game.UnitScale;

					T_element.SetAttribute("Width", (width * scale.x).ToString(CultureInfo.InvariantCulture));
					T_element.SetAttribute("Height", (height * scale.y).ToString(CultureInfo.InvariantCulture));
				}

				if (dynamicTrigger != null && dynamicTrigger.enabled) {
					T_element.SetAttribute("Name", Vectorier.Core.Helpers.Get.Name(child));

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

					string[] sounds = dynamicTrigger.Sound.Split(Vectorier.Core.Game.AttributeSeparator.ToCharArray())
							.Where(s => !string.IsNullOrWhiteSpace(s))
							.Select(s => s.Trim())
							.Distinct()
							.OrderBy(s => s)
							.ThenBy(s => s.Length)
							.ToArray();

					if (dynamicTrigger.PlaySound && sounds.Length == 1) {
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

					XmlElement conditionsElement = xml.CreateElement("Conditions");

					// Direction
					DynamicTrigger.Directions direction = dynamicTrigger.Direction;
					if (direction != DynamicTrigger.Directions.Any) {
						if (direction != DynamicTrigger.Directions.Any) {
							XmlElement equalElement = xml.CreateElement("Equal");
							equalElement.SetAttribute("Value1", "?getModel[_$Model].direction");

							string directionInt = "";
							if (direction == DynamicTrigger.Directions.FromLeft) {
								directionInt = "1";
							} else {
								directionInt = "-1";
							}

							equalElement.SetAttribute("Value2", directionInt);
							conditionsElement.AppendChild(equalElement);
						}
					}

					// Animations
					XmlElement conditionBlock = xml.CreateElement("ConditionBlock");
					XmlElement conditionsOperatorElement = xml.CreateElement("Operator");

					if (!string.IsNullOrEmpty(dynamicTrigger.Animations)) {
						if (splittedAnimations.Count < 2) {
							conditionBlock.SetAttribute("Template", "CommonLib.RequiredAnimation");
							conditionsElement.AppendChild(conditionBlock);
						}

						if (splittedAnimations.Count > 1) {
							conditionsOperatorElement.SetAttribute("Type", "Or");

							int animationCounter = 1;
							foreach (string name in splittedAnimations) {
								XmlElement conditionBlock_reqAnim = xml.CreateElement("ConditionBlock");
		
								conditionBlock_reqAnim.SetAttribute("Template", "CommonLib.RequiredAnimation");
								conditionBlock_reqAnim.SetAttribute("Prefix", $"{animationCounter}_");

								conditionsOperatorElement.AppendChild(conditionBlock_reqAnim);
								animationCounter++;
							}							
						}
					}

					if (conditionsOperatorElement.HasChildNodes) {
						conditionsElement.AppendChild(conditionsOperatorElement);
					}

					if (conditionsElement.HasChildNodes) {
						loopElement.AppendChild(conditionsElement);
					}

					XmlElement actionsElement = xml.CreateElement("Actions");

					// sounds
					if (dynamicTrigger.Latency == 0 && dynamicTrigger.PlaySound && sounds.Length > 0) {
						if (sounds.Length > 1) {
							XmlElement sndChooseElement = xml.CreateElement("Choose");
							sndChooseElement.SetAttribute("Order", "Random");

							foreach (string sound in sounds) {
								XmlElement sndChooseElementSub = xml.CreateElement("Sound");
								sndChooseElementSub.SetAttribute("Name", sound);
								sndChooseElement.AppendChild(sndChooseElementSub);
							}

							if (sndChooseElement.HasChildNodes) {
								actionsElement.AppendChild(sndChooseElement);
							}
						} else if (sounds.Length == 1) {
							XmlElement soundElement = xml.CreateElement("Sound");
							soundElement.SetAttribute("Name", "_Sound");
							xml.AppendChild(soundElement);
						}
					}

					XmlElement transformElement = xml.CreateElement("Transform");

					var dynamicComponent = child.GetComponentInParent<Dynamic>();

					string randInt = UnityEngine.Random.Range((int)1E8, (int)1E9 - 1).ToString();
					if (
						dynamicComponent != null &&
						(!dynamicTrigger.MultipleTransformation || dynamicTrigger.TransformationNames.Count < 1) &&
						string.IsNullOrEmpty(dynamicTrigger.TriggerTransformName) &&
						!string.IsNullOrEmpty(dynamicComponent.TransformationName)
					) {
						if (dynamicTrigger.TriggerTransformName.StartsWith("_")) {
							Debug.LogError("Dynamic trigger name cannot start with \"_\", node entering bounds will result in game crash.", child);
						}

						if (dynamicTrigger.TriggerTransformName != dynamicComponent.TransformationName) {
							Debug.LogWarning("Trigger name does not match its parent transformation name", child);
						}

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
						waitElement.SetAttribute("Frames", Math.Round(dynamicTrigger.Latency * Vectorier.Core.Game.FrameRate).ToString());
						nestedChooseElement.AppendChild(waitElement);

						if (sounds.Length > 1) {
							XmlElement sndChooseElement = xml.CreateElement("Choose");
							sndChooseElement.SetAttribute("Order", "Random");

							foreach (string sound in sounds) {
								XmlElement sndChooseElementSub = xml.CreateElement("Sound");
								sndChooseElementSub.SetAttribute("Name", sound);
								sndChooseElement.AppendChild(sndChooseElementSub);
							}

							if (sndChooseElement.HasChildNodes) {
								nestedChooseElement.AppendChild(sndChooseElement);
							}
						} else if (sounds.Length == 1) {
							XmlElement soundElement = xml.CreateElement("Sound");
							soundElement.SetAttribute("Name", "_Sound");
							nestedChooseElement.AppendChild(soundElement);
						}

						if (!dynamicTrigger.Reusable) {
							XmlElement setActiveElement = xml.CreateElement("SetVariable");
							setActiveElement.SetAttribute("Name", "$Active");
							setActiveElement.SetAttribute("Value", "0");
							actionsElement.AppendChild(setActiveElement);
						}

						chooseElement.AppendChild(nestedChooseElement);
						actionsElement.AppendChild(chooseElement);
					} else {
						actionsElement.AppendChild(transformElement);

						if (!dynamicTrigger.Reusable) {
							XmlElement setActiveElement = xml.CreateElement("SetVariable");
							setActiveElement.SetAttribute("Name", "$Active");
							setActiveElement.SetAttribute("Value", "0");
							actionsElement.AppendChild(setActiveElement);
						}
					}

					if (actionsElement.HasChildNodes) {
						loopElement.AppendChild(actionsElement);
					}

					if (loopElement.HasChildNodes) {
						triggerContentElement.AppendChild(loopElement);
					}

					if (triggerContentElement.HasChildNodes) {
						T_element.AppendChild(triggerContentElement);
					}
				};

				if (triggerSettings != null && triggerSettings.enabled) {
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

				var writeMode = child.GetComponent<VectorierWriteMode>();
				if (writeMode != null && writeMode.enabled) {
					writeModeValue = writeMode.GetWriteModeValue();
					writeMode.AddWriteModeProperties(xml, T_element, writeModeValue);
				}

				XmlNode targetParent = contentElement ?? node.FirstChild;

				var repeater = child.GetComponent<AppendRepeater>();
				if (repeater != null && repeater.enabled) {
					for (int i = 0; i < repeater.Multiplier; i++) {
						XmlNode clone = T_element.CloneNode(true);
						targetParent.AppendChild(clone);
					}
				}

				if (T_element.HasChildNodes) {
					targetParent.AppendChild(T_element);
				}
			}

			// Model
			if (child.gameObject.CompareTag("Model")) {
				Vectorier.Core.Components.Model.Convert(
					go: child.gameObject,
					node: node,
					parentElement: contentElement,
					floatPrecision: Vectorier.Settings.GetPrecision(Vectorier.Settings.Elements.ModelPrecisionKey)
				);
			}

			// Animation
			if (child.gameObject.CompareTag("Animation")) {
				Vectorier.Core.Components.Animation_.Convert(
					go: child.gameObject,
					node: node,
					parentElement: contentElement,
					floatPrecision: Vectorier.Settings.GetPrecision(Vectorier.Settings.Elements.AnimationPrecisionKey)
				);
			}

			// Object
			if (child.gameObject.CompareTag("Object")) {
				Vectorier.Core.Components.Object.Convert(
					go: child.gameObject,
					node: node,
					parentElement: contentElement,
					floatPrecision: Vectorier.Settings.GetPrecision(Vectorier.Settings.Elements.ObjectPrecisionKey)
				);
			}
		}

		if (contentElement.HasChildNodes) {
			// add content to object
			objectElement.AppendChild(contentElement);
		}

		if (node.FirstChild != null) {
			node.FirstChild.AppendChild(objectElement);
		} else {
			node.AppendChild(objectElement);
		}
	}
}

// -=-=-=- //

#if UNITY_EDITOR
public class XmlConfirmationWindow : EditorWindow {
	string? filePath;
	string? fileContent;
	bool confirmed = false;

	Vector2 scrollPos;

	public static bool ShowModal(string xmlPath) {
		XmlConfirmationWindow window = CreateInstance<XmlConfirmationWindow>();
		window.filePath = xmlPath;
		window.fileContent = File.Exists(xmlPath) ? File.ReadAllText(xmlPath) : "";
		window.titleContent = new GUIContent("Confirm XML");

		// centered initial position
		Rect main = EditorGUIUtility.GetMainWindowPosition();

		float w = main.width * 0.5f;
		float h = main.height * 0.75f;
		float x = main.x + (main.width - w) / 2f;
		float y = main.y + (main.height - h) / 2f;

		window.position = new Rect(x, y, w, h);
		window.minSize = new Vector2(400, 300);

		window.ShowModalUtility();
		return window.confirmed;
	}

	void OnGUI() {
		EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));

		// scrollable text area
		scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
		fileContent = EditorGUILayout.TextArea(fileContent, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
		EditorGUILayout.EndScrollView();

		EditorGUILayout.Space(5);

		// buttons at bottom, full width
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Confirm", GUILayout.Height(50), GUILayout.ExpandWidth(true))) {
			File.WriteAllText(filePath, fileContent);
			confirmed = true;
			Close();
		}

		GUILayout.Space(10);

		if (GUILayout.Button("Cancel", GUILayout.Height(50), GUILayout.ExpandWidth(true))) {
			confirmed = false;
			Close();
		}
		EditorGUILayout.EndHorizontal();

		GUILayout.Space(3);

		EditorGUILayout.EndVertical();
	}
}
#endif
#endif