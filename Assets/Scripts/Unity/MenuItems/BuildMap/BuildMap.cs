using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;

using Debug = Logger.Debug;



// -=-=-=- //

#nullable enable
#pragma warning disable CS8321 // unused code
#pragma warning disable CS8602 // nullables
#pragma warning disable CS1582 // nullables
#pragma warning disable CS8603 // possible null returns
#pragma warning disable CS8604 // possible null returns

public class BuildMap : MonoBehaviour {
	public static readonly string XmlDir = "XML";
	public static readonly string XmlDzipDir = Path.Combine(XmlDir, "dzip");

	// Uncompiled XML directories
	public static readonly string XmlDzipTexturesBaseDir = Path.Combine("Resources", "Textures");
	public static readonly string XmlDzipAnimationsDir = Path.Combine("XML", "dzip", "archives", "uncompiled", "animations");
	public static readonly string XmlDzipSoundDir = Path.Combine("XML", "dzip", "archives", "uncompiled", "sound");
	public static readonly string XmlDzipLvlDir = Path.Combine("XML", "dzip", "archives", "uncompiled", "level_xml");
	// public static readonly string XmlDzipLvlDirBackup = Path.Combine("XML", "dzip", "archives", "uncompiled", "_backup", "level_xml");

	// Assets prefix is mandatory for directories that are NOT built on fly
	public static readonly string XmlCommonDir = Path.Combine("Assets", "XML", "dzip", "archives", "uncompiled", ".common_xml");
	public static readonly string XmlGui2048Dir = Path.Combine("Assets", "XML", "dzip", "archives", "uncompiled", ".GUI_2048_1536");

	// Template file
	public static readonly string XmlTrackTemplateFile = Path.Combine("Assets", "XML", "_template" + "." + Vectorier.Core.Game.Properties.Extensions.File.XML);

	// New compiler paths
	public static readonly string XmlCompiledDir = Path.Combine("Assets", "XML", "dzip", "archives", ".compiled"); // where compiled archives are output

	// Compilator directories
	public static readonly string XmlCompilerRoot = Path.Combine("Assets", "XML", "dzip", "_compilators");
	public static readonly string XmlCompilerStandardDir = Path.Combine(XmlCompilerRoot, "standard");
	public static readonly string XmlCompilerOptimizedDir = Path.Combine(XmlCompilerRoot, "optimized");

	public static readonly string XmlCompilerStandardConfigDir = Path.Combine(XmlCompilerStandardDir, "_config");
	public static readonly string XmlCompilerOptimizedConfigDir = Path.Combine(XmlCompilerOptimizedDir, "_config");

	public static readonly string XmlCompilerStandardBuildDir = Path.Combine(XmlCompilerStandardDir, "build");
	public static readonly string XmlCompilerOptimizedBuildDir = Path.Combine(XmlCompilerOptimizedDir, "build");

	public static string? gameDirectoryPathLegacy;
	public static string? gameExecutablePathLegacy;

	public static string? gameDirectoryPathModern;
	public static string? gameExecutablePathModern;
	public static string? gameAssetsPathModern;

	public static event Action? MapBuilt;

	// flag to indicate if the build is for running the game
	public static bool buildForRunGame { get; set; } = false;

	// -=-=-=- //
	// Level Settings

	[Header("Level Settings")]

	// -=-=-=- //

	// common_xml.dz/localization_all.xml
	[Tooltip(@"Level name, displayed in the Game.Properties

Unchanged if string is empty or none")]
	public string title = "";

	// GUI_2048_1536.dz/{n}.png
	[Tooltip(@"Unity path to level thumbnail, displayed in Game.Properties

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

	[Tooltip(@"Background image. (even nodes)

If empty, inherited from ""customBackgroundMirror"" variable.

⚠️ Has to be located inside ""track_content_2048.dz"" file archive base!")]
	public string customBackground = "v_bg";

	[Tooltip(@"Background image for left side. (odd nodes)

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

	[MenuItem("Vectorier/Build/Modern")]
	public static void BuildModern() {
		Build(
			compressionAlgorithm: "copy",
			moveAssets: true,
			build: true,
			legacy: false
		);
	}
	[MenuItem("Vectorier/Build to compilation directory/Modern")]
	public static void MoveModern() {
		Build(
			compressionAlgorithm: "copy",
			moveAssets: true,
			launch: false,
			build: false,
			legacy: false
		);
	}

	[MenuItem("Vectorier/Build/Legacy/dz")]
	public static void BuildLegacyDz() {
		Build(
			compressionAlgorithm: "dz",
			moveAssets: true
		);
	}

	[MenuItem("Vectorier/Build/Legacy/zlib")]
	public static void BuildLegacyZlib() {
		Build(
			compressionAlgorithm: "zlib",
			moveAssets: true
		);
	}

	[MenuItem("Vectorier/Build/Legacy/copy #&B")]
	public static void BuildLegacyCopy() {
		Build(
			compressionAlgorithm: "copy",
			moveAssets: true
		);
	}

	[MenuItem("Vectorier/Build/XML Only (legacy)")]
	public static void BuildLegacyXml() {
		Build(
			compressionAlgorithm: "copy",
			moveAssets: false
		);
	}

	// -=-=-=- //
	// Main

	public static void Build(
		string? compressionAlgorithm,
		bool moveAssets,
		bool launch = true,

		bool build = true,
		bool legacy = true
	) {
		// lazy init
		gameDirectoryPathLegacy = Path.GetFullPath(Vectorier.Settings.Core.GameDirectoryLegacy);
		gameDirectoryPathModern = Path.GetFullPath(Vectorier.Settings.Core.GameDirectoryModern);

		gameExecutablePathLegacy = Vectorier.Settings.Core.GameExecutableLegacy;
		gameExecutablePathModern = Vectorier.Settings.Core.GameExecutableModern;

		var buildMap = FindObjectOfType<BuildMap>();

		if (!Directory.Exists(gameDirectoryPathLegacy)) {
			Debug.LogFatal($"Game directory not found: {gameDirectoryPathLegacy}");
			return;
		}

		if (!File.Exists(gameExecutablePathLegacy) && !legacy) {
			Debug.LogFatal($"Game executable was not found in game directory ({Path.GetFileName(gameExecutablePathLegacy)})");
			return;
		}

		if (buildMap == null || buildMap.mapToOverride == null) {
			Debug.LogFatal("No GameObject with map building script attached has been found!");
			return;
		}

		if (!buildMap.enabled) {
			Debug.LogFatal("GameObject with map building script is disabled.");
			return;
		}

		var today = DateTime.Now;

		if (build) {
			gameAssetsPathModern = Path.Combine(
				gameDirectoryPathModern,
				Path.GetFileNameWithoutExtension(gameExecutablePathModern) + "_Data",
				"StreamingAssets"
			);
		} else {
			gameAssetsPathModern = Path.Combine(
				gameDirectoryPathModern,
				"_Compiled",
				today.ToString("dd-MM-yyyy_HH-mm-ss"),
				string.Join(", ", buildMap.authors.Split(
					new string[] { ",", Vectorier.Core.Game.Properties.AttributeSeparator },
					StringSplitOptions.RemoveEmptyEntries
				)).Trim()
			);
		}

		Directory.CreateDirectory(gameAssetsPathModern);

		string scenePath = SceneManager.GetActiveScene().path;

		// -=-=-=- //
		// Variables

		string XmlTrackFile = Path.Combine(XmlDzipLvlDir, buildMap.mapToOverride + "." + Vectorier.Core.Game.Properties.Extensions.File.XML);
		string XmlTrackFileFull = Path.Combine(Application.dataPath, XmlTrackFile);

		// Find BuildMap
		string bmNotFoundMsg = "";

		if (buildMap == null && !buildMap.enabled) {
			// Check if buildMap is null first
			bmNotFoundMsg = @"Couldn't compile a track - No map building script instance was found active in any GameObject";
		} else if (!Vectorier.Utils.GameObjectParser.Parse(buildMap.gameObject).IsVisible()) {
			// Then check visibility
			bmNotFoundMsg = @"Couldn't compile a track - GameObject containing map building script was set to inactive";
		}

		if (!string.IsNullOrEmpty(bmNotFoundMsg)) {
			Debug.LogError(bmNotFoundMsg);
		}

		// -=-=-=- //
		// Validators

		if (Vectorier.Settings.Core.ValidateScene) {
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

		if (Vectorier.Settings.Core.SaveSceneBeforeBuildMap) {
			if (!string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) {
				EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
			}
		}

		/*
		if (string.IsNullOrEmpty(buildMap.gameDirectoryPathLegacy)) {
			buildMap.gameDirectoryPathLegacy = Vectorier.Settings.Core.GameDirectory;
		}
		*/

		// Start the stopwatch
		var stopwatch = Stopwatch.StartNew();

		GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

		List<GameObject> animationsInScene = new List<GameObject>();
		List<GameObject> areasInScene = new List<GameObject>();
		List<GameObject> camerasInScene = new List<GameObject>();
		List<GameObject> dynamicsInScene = new List<GameObject>();
		List<GameObject> itemsInScene = new List<GameObject>();
		List<GameObject> modelsInScene = new List<GameObject>();
		List<GameObject> platformsInScene = new List<GameObject>();
		List<GameObject> spawnsInScene = new List<GameObject>();
		List<GameObject> trapezoidsInScene = new List<GameObject>();
		List<GameObject> triggersInScene = new List<GameObject>();
		List<GameObject> objectsInScene = new List<GameObject>();

		foreach (GameObject go in allObjects) {
			if (!Vectorier.Utils.GameObjectParser.Parse(go).IsVisible()) {
				continue;
			}

			if (go.CompareTag("Animation")) {
				animationsInScene.Add(go);
			} else if (go.CompareTag("Area")) {
				areasInScene.Add(go);
			} else if (go.CompareTag("Camera")) {
				camerasInScene.Add(go);
			} else if (go.CompareTag("Dynamic")) {
				dynamicsInScene.Add(go);

				/*
				if (!go.TryGetComponent<Dynamic>(out _)) {
					string objPath = Vectorier.Utils.GameObjectParser.Parse(go).GetPath();

					Debug.LogWarning(
						$"GameObject \"{go.name}\" is missing dynamic transformation component - treating as standalone <Object> node\n\n{objPath}",
						go
					);
				}
				*/
			} else if (go.CompareTag("Item")) {
				itemsInScene.Add(go);
			} else if (go.CompareTag("Model")) {
				modelsInScene.Add(go);
			} else if (go.CompareTag("Platform")) {
				platformsInScene.Add(go);
			} else if (go.CompareTag("Spawn")) {
				spawnsInScene.Add(go);
			} else if (go.CompareTag("Trapezoid")) {
				trapezoidsInScene.Add(go);
			} else if (go.CompareTag("Trigger")) {
				triggersInScene.Add(go);
			} else if (go.CompareTag("Object")) {
				objectsInScene.Add(go);
			}
		}

		// quads
		areasInScene.Sort((a, b) => {
			int aOrder = a.GetComponent<SpriteRenderer>()?.sortingOrder ?? 0;
			int bOrder = b.GetComponent<SpriteRenderer>()?.sortingOrder ?? 0;
			return aOrder.CompareTo(bOrder);
		});

		triggersInScene.Sort((a, b) => {
			int aOrder = a.GetComponent<SpriteRenderer>()?.sortingOrder ?? 0;
			int bOrder = b.GetComponent<SpriteRenderer>()?.sortingOrder ?? 0;
			return aOrder.CompareTo(bOrder);
		});

		List<GameObject> imagesInScene = new List<GameObject>();
		List<GameObject> backdropsInScene = new List<GameObject>();
		List<GameObject> topImagesInScene = new List<GameObject>();

		foreach (var go in allObjects) {
			if (!go.CompareTag("Image") &&
				!go.CompareTag("Backdrop") &&
				!go.CompareTag("Top Image")
			)
				continue;

			var sr = go.GetComponent<SpriteRenderer>();

			if (sr == null || sr.sprite == null) {
				string objPath = Vectorier.Utils.GameObjectParser.Parse(go).GetPath();

				Debug.LogError(
					$"GameObject \"{go.name}\" has no SpriteRenderr or/but no sprite\n\n{objPath}",
					go
				);
			} else {
				string objName = Vectorier.Core.Helpers.Get.Name(go);
				string objPath = Vectorier.Utils.GameObjectParser.Parse(go).GetPath();

				if (objName != sr.sprite.name) {
					Debug.LogWarning(
						$"Possible GameObject \"{objName}\" sprite mismatch ({sr.sprite.name}) [click to toggle]\n\n{objPath}",
						go
					);
				}
			}

			if (go.CompareTag("Backdrop")) {
				backdropsInScene.Add(go);
			} else if (go.CompareTag("Top Image")) {
				topImagesInScene.Add(go);
			} else {
				imagesInScene.Add(go);
			}
		}

		// sprites
		imagesInScene.Sort((a, b) => {
			int aOrder = a.GetComponent<SpriteRenderer>()?.sortingOrder ?? 0;
			int bOrder = b.GetComponent<SpriteRenderer>()?.sortingOrder ?? 0;
			return aOrder.CompareTo(bOrder);
		});

		backdropsInScene.Sort((a, b) => {
			int aOrder = a.GetComponent<SpriteRenderer>()?.sortingOrder ?? 0;
			int bOrder = b.GetComponent<SpriteRenderer>()?.sortingOrder ?? 0;
			return aOrder.CompareTo(bOrder);
		});

		topImagesInScene.Sort((a, b) => {
			int aOrder = a.GetComponent<SpriteRenderer>()?.sortingOrder ?? 0;
			int bOrder = b.GetComponent<SpriteRenderer>()?.sortingOrder ?? 0;
			return aOrder.CompareTo(bOrder);
		});

		// -=-=-=- //
		// Moves Manager Processing

		string movesPath = Path.Combine(gameDirectoryPathLegacy, "Moves_new" + "." + Vectorier.Core.Game.Properties.Extensions.File.XML);

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
			XmlElement cameraElement = Vectorier.Core.XML.Utils.General.GetOrCreateElement("Camera", configNode, movesNewXml);

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
					cameraElement.SetAttribute(kvp.Key, Vectorier.Core.Helpers.Format.ToString(kvp.Value));
				}
			}

			// taser section
			XmlElement taserElement = Vectorier.Core.XML.Utils.General.GetOrCreateElement("Taser", configNode, movesNewXml);

			var taserAttributes = new Dictionary<string, float> {
				{ "Distance", movesManager.Taser.Distance },
				{ "Time", movesManager.Taser.Time },
				{ "HeightFactor", movesManager.Taser.HeightFactor }
			};

			foreach (var kvp in taserAttributes) {
				if (kvp.Value == 0) {
		            taserElement.RemoveAttribute(kvp.Key);
				} else {
					taserElement.SetAttribute(kvp.Key, Vectorier.Core.Helpers.Format.ToString(kvp.Value));
				}
			}

			movesNewXml.Save(movesPath);

			//if (buildMap.optimizeWrittenTrack) {
			//	Vectorier.Core.XML.Utils.Optimize.Pipeline(fileInput: movesPath);
			//}
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

			Vectorier.Core.XML.Track.Level.Validators.Track.Music.File(buildMap.levelMusic, gameDirectoryPathLegacy),
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

			int coinsNodeValue = Vectorier.Core.XML.Utils.General.GetNumericAttr<int>((XmlElement)coinsNode, "Value");

			if (
				Vectorier.Settings.Core.ValidateScene &&
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
			List<GameObject> gos,

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
					var spriteRenderer = go.GetComponent<SpriteRenderer>();

					if (spriteRenderer != null && spriteRenderer.enabled && spriteRenderer.sortingLayerName == sortingLayer) {
						Vectorier.Core.Components.Backdrop.Convert(
							go: go,
							node: node,
							factorValue: factor,
							correctFactorPosition: component.correctFactorPosition,
							floatPrecision: Vectorier.Settings.Core.GetPrecision(Vectorier.Settings.Core.Elements.BackdropPrecisionKey),
							legacy: legacy
						);
					}
				}
			}
		}

		// PRECOMPUTE OUTSIDE LOOP
		int spawnPrecision     = Vectorier.Settings.Core.GetPrecision(Vectorier.Settings.Core.Elements.SpawnPrecisionKey);
		int itemPrecision      = Vectorier.Settings.Core.GetPrecision(Vectorier.Settings.Core.Elements.ItemPrecisionKey);
		int platformPrecision  = Vectorier.Settings.Core.GetPrecision(Vectorier.Settings.Core.Elements.PlatformPrecisionKey);
		int trapezoidPrecision = Vectorier.Settings.Core.GetPrecision(Vectorier.Settings.Core.Elements.TrapezoidPrecisionKey);
		int triggerPrecision   = Vectorier.Settings.Core.GetPrecision(Vectorier.Settings.Core.Elements.TriggerPrecisionKey);
		int areaPrecision      = Vectorier.Settings.Core.GetPrecision(Vectorier.Settings.Core.Elements.AreaPrecisionKey);
		int modelPrecision     = Vectorier.Settings.Core.GetPrecision(Vectorier.Settings.Core.Elements.ModelPrecisionKey);
		int animationPrecision = Vectorier.Settings.Core.GetPrecision(Vectorier.Settings.Core.Elements.AnimationPrecisionKey);
		int imagePrecision     = Vectorier.Settings.Core.GetPrecision(Vectorier.Settings.Core.Elements.ImagePrecisionKey);
		int objectPrecision    = Vectorier.Settings.Core.GetPrecision(Vectorier.Settings.Core.Elements.ObjectPrecisionKey);

		// Cache lists into arrays for faster iteration
		GameObject[] spawnsArr     = spawnsInScene.ToArray();
		GameObject[] itemsArr      = itemsInScene.ToArray();
		GameObject[] platformsArr  = platformsInScene.ToArray();
		GameObject[] trapezoidsArr = trapezoidsInScene.ToArray();
		GameObject[] triggersArr   = triggersInScene.ToArray();
		GameObject[] areasArr      = areasInScene.ToArray();
		GameObject[] modelsArr     = modelsInScene.ToArray();
		GameObject[] animationsArr = animationsInScene.ToArray();
		GameObject[] imagesArr     = imagesInScene.ToArray();
		GameObject[] objectsArr    = objectsInScene.ToArray();
		GameObject[] dynamicsArr   = dynamicsInScene.ToArray();

		// Precombine static sprite sets once (IMPORTANT FIX)
		GameObject[] spriteCombinedArr = backdropsInScene.Concat(topImagesInScene).ToArray();

		// MAIN LOOP
		int nodeIndex = 0;

		foreach (XmlNode node in trackNode.ChildNodes) {
			var labelAttr = node.Attributes?["Label"];
			string type = labelAttr?.Value?.ToLower() ?? "unknown";

			// FIRST NODE SPECIAL CASE
			if (nodeIndex == 0) {
				float bgX = buildMap.backgroundXPosition - 3740f;
				float bgY = buildMap.backgroundYPosition - 500f;

				if (bgX != 0) {
					var a = xml.CreateAttribute("X");
					a.Value = Vectorier.Core.Helpers.Format.ToString(bgX);
					node.Attributes?.Append(a);
				}

				if (bgY != 0) {
					var a = xml.CreateAttribute("Y");
					a.Value = Vectorier.Core.Helpers.Format.ToString(bgY);
					node.Attributes?.Append(a);
				}

				var factor = xml.CreateAttribute("Factor");
				factor.Value = "0.05";
				node.Attributes?.Append(factor);

				Vectorier.Core.XML.Track.Level.Properties.SetBackground(
					xml: xml,

					pathImage: buildMap.customBackground,
					pathImageMirror: buildMap.customBackgroundMirror,

					x: buildMap.backgroundXPosition,
					y: 0f,

					width: buildMap.backgroundWidth,
					height: buildMap.backgroundHeight
				);
			}

			nodeIndex++;

			// TRIGGERS (always)
			foreach (var go in triggersArr) {
				if (Vectorier.Core.Components.ObjectDynamicComponent.Helpers.HasDynamicAncestor(go.transform)) {
					continue;
				}

				if (go.transform.parent != null &&
					go.transform.parent.CompareTag("Dynamic"))
					continue;

				var sr = go.GetComponent<SpriteRenderer>();
				var parent = go.transform.parent;
				var parentSr = parent ? parent.GetComponent<SpriteRenderer>() : null;

				if (type == "overlay" &&
					parentSr != null &&
					parentSr.enabled &&
					parentSr.sortingLayerName == "Overlay")
				{
					Vectorier.Core.Components.TriggerComponent.Convert(
						go: go,
						node: node,
						floatPrecision: triggerPrecision,
						legacy: legacy
					);

					continue;
				}

				if (type == "default" &&
					sr != null &&
					sr.sortingLayerName == "Default")
				{
					try {
						Vectorier.Core.Components.TriggerComponent.Convert(
							go: go,
							node: node,
							floatPrecision: triggerPrecision,
							legacy: legacy
						);
					} catch (Exception e) {
						Debug.LogError(
							$"Trigger error {e.GetType()} on {go.name}\n{e.Message}",
							go
						);

						return;
					}
				}
			}

			// IMAGES
			foreach (var go in imagesArr) {
				if (Vectorier.Core.Components.ObjectDynamicComponent.Helpers.HasDynamicAncestor(go.transform)) {
					continue;
				}

				if (
					go.transform.parent != null &&
					go.transform.parent.CompareTag("Dynamic")
				)
					continue;

				var sr = go.GetComponent<SpriteRenderer>();
				if (sr == null) {
					continue;
				}

				if (type == "default" && sr.sortingLayerName == "Default") {
					Vectorier.Core.Components.Image.Convert(
						go: go,
						node: node,
						floatPrecision: imagePrecision,
						legacy: legacy
					);
				} else if (type == "overlay" && sr.sortingLayerName == "Overlay") {
					Vectorier.Core.Components.Image.Convert(
						go: go,
						node: node,
						floatPrecision: imagePrecision,
						legacy: legacy
					);
				}
			}

			// SPRITES (BACKDROP + TOP)
			ProcessSomeSprites(
				gos: spriteCombinedArr.ToList(),
				node: node,
				nodeType: type,
				component: buildMap
			);

			// DEFAULT-TYPE ONLY BLOCKS
			if (type == "default") {
				foreach (var go in spawnsArr) {
					if (Vectorier.Core.Components.ObjectDynamicComponent.Helpers.HasDynamicAncestor(go.transform)) continue;
					Vectorier.Core.Components.SpawnComponent.Convert(
						go: go, node: node, floatPrecision: spawnPrecision, legacy: legacy
					);
				}

				foreach (var go in itemsArr) {
					if (Vectorier.Core.Components.ObjectDynamicComponent.Helpers.HasDynamicAncestor(go.transform)) continue;
					Vectorier.Core.Components.ItemComponent.Convert(
						go: go, node: node, floatPrecision: itemPrecision, legacy: legacy
					);
				}

				foreach (var go in platformsArr) {
					if (Vectorier.Core.Components.ObjectDynamicComponent.Helpers.HasDynamicAncestor(go.transform)) continue;
					Vectorier.Core.Components.PlatformComponent.Convert(
						go: go, node: node, floatPrecision: platformPrecision, legacy: legacy
					);
				}

				foreach (var go in trapezoidsArr) {
					if (Vectorier.Core.Components.ObjectDynamicComponent.Helpers.HasDynamicAncestor(go.transform)) continue;
					Vectorier.Core.Components.TrapezoidComponent.Convert(
						go: go, node: node, floatPrecision: trapezoidPrecision, legacy: legacy
					);
				}

				foreach (var go in areasArr) {
					if (Vectorier.Core.Components.ObjectDynamicComponent.Helpers.HasDynamicAncestor(go.transform)) continue;
					Vectorier.Core.Components.AreaComponent.Convert(
						go: go, node: node, floatPrecision: areaPrecision, legacy: legacy
					);
				}

				foreach (var go in modelsArr) {
					if (Vectorier.Core.Components.ObjectDynamicComponent.Helpers.HasDynamicAncestor(go.transform)) continue;
					Vectorier.Core.Components.ModelComponent.Convert(
						go: go, node: node, floatPrecision: modelPrecision, legacy: legacy
					);
				}

				foreach (var go in animationsArr) {
					if (Vectorier.Core.Components.ObjectDynamicComponent.Helpers.HasDynamicAncestor(go.transform)) continue;
					Vectorier.Core.Components.AnimationComponent.Convert(
						go: go, node: node, floatPrecision: animationPrecision, legacy: legacy
					);
				}

				foreach (var go in dynamicsArr) {
					if (go.transform.parent != null && Vectorier.Core.Components.ObjectDynamicComponent.Helpers.HasDynamicAncestor(go.transform)) {
						continue;
					}

					XmlElement targetContent = Vectorier.Core.XML.Utils.General.GetOrCreateElement("Content", (XmlElement)node, node.OwnerDocument);

					Vectorier.Core.Components.ObjectDynamicComponent.Convert(
						go: go, node: targetContent, legacy: legacy
					);
				}

				foreach (var go in objectsArr) {
					if (Vectorier.Core.Components.ObjectDynamicComponent.Helpers.HasDynamicAncestor(go.transform)) continue;
					Vectorier.Core.Components.ObjectStaticComponent.Convert(
						go: go, node: node, floatPrecision: objectPrecision, legacy: legacy
					);
				}
			}
		}

		// -=-=-=- //
		// Write properties

		// PC
		if (moveAssets) {
			string originalPath_ListPayedPC = Path.Combine(XmlCommonDir, "List_Payed" + "." + Vectorier.Core.Game.Properties.Extensions.File.XML);
			string backupPath_ListPayedPC = Path.Combine(XmlCommonDir, ".original", "List_Payed" + "." + Vectorier.Core.Game.Properties.Extensions.File.XML);

			Vectorier.Core.Archive.Track.Level.Content.Properties(
				allObjects,

				buildMap.mapToOverride,

				originalPath_ListPayedPC,
				backupPath_ListPayedPC
			);

			// Android
			string originalPath_ListPayedMobile = Path.Combine(XmlCommonDir, "list_paid_mob" + "." + Vectorier.Core.Game.Properties.Extensions.File.XML);
			string backupPath_ListPayedMobile = Path.Combine(XmlCommonDir, ".original", "list_paid_mob" + "." + Vectorier.Core.Game.Properties.Extensions.File.XML);

			Vectorier.Core.Archive.Track.Level.Content.Properties(
				allObjects,

				buildMap.mapToOverride,

				originalPath_ListPayedMobile,
				backupPath_ListPayedMobile
			);

			string backupPath_GUI_2048 = Path.Combine(XmlGui2048Dir, ".original");

			Vectorier.Core.Archive.Track.Level.Content.WriteLevelThumbnail(
				buildMap.thumbnailImagePath,
				scenePath,

				buildMap.mapToOverride,

				XmlGui2048Dir,
				backupPath_GUI_2048
			);

			string originalFileFull_Interface1 = Path.Combine(XmlGui2048Dir, "scene_buttons_2048" + "." + Vectorier.Core.Game.Properties.Extensions.File.Image.Static[0]);
			string backupFileFull_Interface1 = Path.Combine(XmlGui2048Dir, ".original", "scene_buttons_2048" + "." + Vectorier.Core.Game.Properties.Extensions.File.Image.Static[0]);
			string backupFileEmpty_Interface1 = Path.Combine(XmlGui2048Dir, ".original", "scene_buttons_2048" + "_empty" + "." + Vectorier.Core.Game.Properties.Extensions.File.Image.Static[0]);

			Vectorier.Core.Archive.Track.Level.Content.WriteInGameInterface(
				buildMap.transparentInterfaceButtons,

				XmlGui2048Dir,
				originalFileFull_Interface1,
				backupFileEmpty_Interface1,

				backupPath_GUI_2048,
				backupFileFull_Interface1
			);

			string originalPath_LocalizationAll = Path.Combine(XmlCommonDir, "localization_all" + "." + Vectorier.Core.Game.Properties.Extensions.File.XML);
			string backupPath_LocalizationAll = Path.Combine(XmlCommonDir, ".original", "localization_all" + "." + Vectorier.Core.Game.Properties.Extensions.File.XML);

			Vectorier.Core.Archive.Track.Level.Content.WriteLevelName(
				buildMap.title,

				buildMap.mapToOverride,

				originalPath_LocalizationAll,
				backupPath_LocalizationAll
			);
		}

		// -=-=-=- //
		// Center objects

		if (Vectorier.Settings.Core.CenterUnnamedObjects) {
			Vectorier.Core.XML.Utils.Optimize.Objects(
				trackNode,
				Vectorier.Settings.Core.GetPrecision("VectorierSettings.Elements.Properties.Object.Precision")
			);
		}

		if (Vectorier.Settings.Core.SortNodeAttributes) {
			Vectorier.Core.XML.Utils.Optimize.Attributes(
				mapRootNode,
				Vectorier.Settings.Core.OrdredAttributes.ToArray()
			);
		}

		if (Vectorier.Settings.Core.ValidateWrittenTrackXml) {
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
		string originalXmlSize = Vectorier.Utils.Files.BytesToString(originalXmlSizeBytes);

		if (buildMap.optimizeWrittenTrack) {
			Vectorier.Core.XML.Utils.Optimize.Pipeline(
				fileInput: XmlTrackFileFull,
				nodeName: "Track"
			);
		}

		int optimizedXmlSizeBytes = (int)new FileInfo(XmlTrackFileFull).Length;
		string optimizedXmlSize = Vectorier.Utils.Files.BytesToString(optimizedXmlSizeBytes);

		var modernVersionFlags = buildMap.gameObject.GetComponent<ModernVersionFlags>();

		if (!legacy && !modernVersionFlags) {
			Debug.LogError("ModernVersionFlags component cannot be found in BuildMap GameObject - level building has been cancelled.", buildMap.gameObject);
			return;
		}

		Dictionary<string, string> finalPaths = new Dictionary<string, string>();
		if (moveAssets) {
			finalPaths = Vectorier.Track.Compile.AssetMover.Move(
				gos: allObjects,
				levelName: buildMap.mapToOverride,
				compressionAlgorithm: compressionAlgorithm,

				modernAssetsPath: BuildMap.gameAssetsPathModern,
				killGame: legacy ? true : build ? true : false,
				legacy: legacy
			);

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

		if (moveAssets) {
			if (!Directory.Exists(XmlCompiledDir)) {
				Directory.CreateDirectory(XmlCompiledDir);
			}

			algoString = $" with {compressionAlgorithm.ToUpper()}";
		}

		string elapsed = ts.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture);

		string logMessage = $"Building done{algoString} ({elapsed} seconds) [XML: {optimizedXmlSize}]";

		string unary;
		// logic bugs, wontfix
		if (originalXmlSizeBytes > optimizedXmlSizeBytes) {
			unary = "-"; // File got larger
		} else if (originalXmlSizeBytes < optimizedXmlSizeBytes) {
			unary = "+"; // File got smaller
		} else {
			unary = "";
		}

		if (buildMap.optimizeWrittenTrack) {
			string displayBytesReadable = Vectorier.Utils.Files.BytesToString(originalXmlSizeBytes - optimizedXmlSizeBytes);

			float percentage = originalXmlSizeBytes > 0
				? 100f - ((float)optimizedXmlSizeBytes / originalXmlSizeBytes * 100f)
				: 0f;

			string displayPercentage = percentage.ToString("F2", CultureInfo.InvariantCulture).Trim('-');
			displayBytesReadable = displayBytesReadable.Trim('-');

			logMessage +=
				$"\nOld size (XML): {originalXmlSize} ({unary}{displayPercentage}%) [{unary}{displayBytesReadable}]";
		}

		TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(
			Path.Combine("Assets", XmlTrackFile).Replace(
				Path.DirectorySeparatorChar,
				Path.AltDirectorySeparatorChar
			)
		);

		Debug.LogSuccess(logMessage, asset);

		if (unary == "+") {
			Debug.LogWarning("Optimized XML size is higher than old one.\nℹ️ This may be cause of optimization mode.");
		}

		if (!legacy && launch) {
			Process process = new Process();
			process.StartInfo.FileName = gameExecutablePathModern;
			process.StartInfo.Arguments = modernVersionFlags.ParseArgs(buildMap.mapToOverride);
			process.Start();
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