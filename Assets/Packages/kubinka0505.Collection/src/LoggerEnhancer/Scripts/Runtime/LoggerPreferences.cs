#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

using System.IO;
using System.Linq;
using System.Collections.Generic;

using PathE;

// -=-=-=- //

namespace Logger {
	public static class LoggerPreferences {
		// predefined lazy properties
		private static string _lazyProgDir;
		private static string _lazyProgName;
		private static string _lazyPathIcons;

		private static string _progFile => PathUtils.Relative(PathUtils.GetTrace().file, PathUtils.dataPath);
		private static string _progDir => _lazyProgDir ??= ComputeProgDir();
		private static string _progName => _lazyProgName ??= (_progDir.StartsWith("Logger") ? _progDir : "LoggerEnhancer").Trim();

		private static string _progURL => "https://github.com/kubinka0505/UnityAddons";

		private static string _progDirFull => _lazyProgDir ??= Path.GetFullPath(
			Path.Combine(
				Path.GetDirectoryName(_progFile), // file location
				"..", ".."                         // go up to package root
			)
		);

		private static string _pathIcons => _lazyPathIcons ??= PathUtils.Relative(
			Path.Combine(_progDirFull, "Resources", "Icons"),
			PathUtils.dataPath // Assets
		);

		private static readonly int _charLimit = 50;
		private static readonly string _defaultFormatString = $"[{Utils.Variables.Script}{Utils.Variables.Separator}{Utils.Variables.Line}] [{Utils.Variables.Level}] {Utils.Variables.Message}";

		private static GUIStyle _headerStyle;
		private static readonly int _guiSpace = 10;

		// Helper method to compute progDir safely
		private static string ComputeProgDir() {
			try {
				string dir = Path.GetFileName(Path.GetFullPath(Path.Combine(
					Path.GetDirectoryName(_progFile),
					"..", ".."
				)));
				return string.IsNullOrEmpty(dir) ? "LoggerEnhancer" : dir;
			} catch {
				return "LoggerEnhancer";
			}
		}

		// Keys
		private static readonly string EnableKey = $"{_progName}.Enable";
		private static readonly string FormatStringKey = $"{_progName}.FormatString";
		private static readonly string ScriptModeKey = $"{_progName}.ScriptMode";

		private static readonly string ScriptUseColorKey = $"{_progName}.ScriptUseColor";
		private static readonly string ScriptColorKey = $"{_progName}.ScriptColor";
		private static readonly string ScriptBoldKey = $"{_progName}.ScriptBold";
		private static readonly string ScriptItalicKey = $"{_progName}.ScriptItalic";

		private static readonly string LineUseColorKey = $"{_progName}.LineUseColor";
		private static readonly string LineColorKey = $"{_progName}.LineColor";
		private static readonly string LineBoldKey = $"{_progName}.LineBold";
		private static readonly string LineItalicKey = $"{_progName}.LineItalic";

		private static readonly string MessageUseColorKey = $"{_progName}.MessageUseColor";
		private static readonly string MessageColorKey = $"{_progName}.";
		private static readonly string MessageBoldKey = $"{_progName}.MessageBold";
		private static readonly string MessageItalicKey = $"{_progName}.MessageItalic";

		// per-level key *prefix* helpers
		private static readonly string[] LogLevels = {
			"INFO",
			"WARNING",
			"ERROR",
			"SUCCESS",
			"DEBUG",
			"FATAL"
		};
		private static string LevelUseKey(string lvl) => $"{_progName}.Level.{lvl}.UseColor";
		private static string LevelColorKey(string lvl) => $"{_progName}.Level.{lvl}.Color";
		private static string LevelBoldKey(string lvl) => $"{_progName}.Level.{lvl}.Bold";
		private static string LevelItalicKey(string lvl) => $"{_progName}.Level.{lvl}.Italic";
		private static string LevelDisplayLineKey(string lvl) => $"{_progName}.Level.{lvl}.DisplayLine";

		// per-level defaults
		private static readonly Dictionary<string, Color> DefaultLevelColors = new Dictionary<string, Color> {
			{ "INFO",    new Color(0.27f, 0.67f, 1f, 1f) }, // #4AFF
			{ "WARNING", new Color(1f, 0.8f, 0f, 1f) },     // #FC0
			{ "ERROR",   new Color(1f, 0f, 0f, 1f) },       // #F00
			{ "SUCCESS", new Color(0f, 0.8f, 0f, 1f) },     // #0C0
			{ "DEBUG",   new Color(1f, 1f, 1f, 1f) },       // #FFF
			{ "FATAL",   new Color(0.75f, 0f, 0f, 1f) }     // #C00000
		};

		// use color
		private static readonly Dictionary<string, bool> DefaultLevelUse = new Dictionary<string, bool> {
			{ "INFO", true },
			{ "WARNING", true },
			{ "ERROR", true },
			{ "SUCCESS", true },
			{ "DEBUG", true },
			{ "FATAL", true }
		};

		// bold
		private static readonly Dictionary<string, bool> DefaultLevelBold = new Dictionary<string, bool> {
			{ "INFO", true },
			{ "WARNING", true },
			{ "ERROR", true },
			{ "SUCCESS", true },
			{ "DEBUG", true },
			{ "FATAL", true }
		};

		// italic
		private static readonly Dictionary<string, bool> DefaultLevelItalic = new Dictionary<string, bool> {
			{ "INFO", false },
			{ "WARNING", false },
			{ "ERROR", false },
			{ "SUCCESS", false },
			{ "DEBUG", false },
			{ "FATAL", false }
		};

		private static readonly Dictionary<string, bool> DefaultLevelDisplayLine = new Dictionary<string, bool> {
			{ "INFO", false },
			{ "WARNING", true },
			{ "ERROR", true },
			{ "SUCCESS", false },
			{ "DEBUG", true },
			{ "FATAL", true }
		};

		// script mode
		public enum ScriptDisplayMode {
			Name,
			FullPath
		}

		// foldouts
		private static bool _scriptFoldout = false;
		private static bool _lineFoldout = false;
		private static bool _messageFoldout = false;
		private static bool _levelFoldout = false;
		private static readonly bool[] _levelNestedFoldouts = new bool[6];

		// accessors (global)
		public static bool EnableLogger {
			get => EditorPrefs.GetBool(EnableKey, true);
			set => EditorPrefs.SetBool(EnableKey, value);
		}
		public static string Format {
			get => EditorPrefs.GetString(FormatStringKey, _defaultFormatString);
			set => EditorPrefs.SetString(FormatStringKey, value);
		}
		public static ScriptDisplayMode ScriptMode {
			get => (ScriptDisplayMode)EditorPrefs.GetInt(ScriptModeKey, 0);
			set => EditorPrefs.SetInt(ScriptModeKey, (int)value);
		}

		// script styles
		public static bool ScriptUseColor {
			get => EditorPrefs.GetBool(ScriptUseColorKey, true);
			set => EditorPrefs.SetBool(ScriptUseColorKey, value);
		}
		public static Color ScriptColor {
			get => Utils.GetColor(ScriptColorKey, new Color(1f, 1f, 1f, 0.5f));
			set => Utils.SetColor(ScriptColorKey, value);
		}
		public static bool ScriptBold {
			get => EditorPrefs.GetBool(ScriptBoldKey, false);
			set => EditorPrefs.SetBool(ScriptBoldKey, value);
		}
		public static bool ScriptItalic {
			get => EditorPrefs.GetBool(ScriptItalicKey, false);
			set => EditorPrefs.SetBool(ScriptItalicKey, value);
		}

		// line styles
		public static bool LineUseColor {
			get => EditorPrefs.GetBool(LineUseColorKey, false);
			set => EditorPrefs.SetBool(LineUseColorKey, value);
		}
		public static Color LineColor {
			get => Utils.GetColor(LineColorKey, new Color(1f, 1f, 0f, 1f));
			set => Utils.SetColor(LineColorKey, value);
		}
		public static bool LineBold {
			get => EditorPrefs.GetBool(LineBoldKey, false);
			set => EditorPrefs.SetBool(LineBoldKey, value);
		}
		public static bool LineItalic {
			get => EditorPrefs.GetBool(LineItalicKey, false);
			set => EditorPrefs.SetBool(LineItalicKey, value);
		}

		// message styles
		public static bool MessageUseColor {
			get => EditorPrefs.GetBool(MessageUseColorKey, false);
			set => EditorPrefs.SetBool(MessageUseColorKey, value);
		}
		public static Color MessageColor {
			get => Utils.GetColor(MessageColorKey, Color.white);
			set => Utils.SetColor(MessageColorKey, value);
		}
		public static bool MessageBold {
			get => EditorPrefs.GetBool(MessageBoldKey, false);
			set => EditorPrefs.SetBool(MessageBoldKey, value);
		}
		public static bool MessageItalic {
			get => EditorPrefs.GetBool(MessageItalicKey, false);
			set => EditorPrefs.SetBool(MessageItalicKey, value);
		}

		// -=-=-=- //
		// per-level getters
		private static readonly Dictionary<string, string> LevelDisplayNames = new Dictionary<string, string> {
			{ "INFO", "INFO" },
			{ "WARNING", "WARNING" },
			{ "ERROR", "ERROR" },
			{ "SUCCESS", "SUCCESS" },
			{ "DEBUG", "DEBUG" },
			{ "FATAL", "FATAL" }
		};

		// get display name (falls back to original level)
		public static string GetLevelDisplayName(string level) {
			string key = $"{_progName}.Level.{level}.DisplayName";

			if (EditorPrefs.HasKey(key)) {
				return EditorPrefs.GetString(key);
			}

			return LevelDisplayNames.ContainsKey(level) ? LevelDisplayNames[level] : level;
		}

		public static bool GetLevelUseColor(string level) {
			if (EditorPrefs.HasKey(LevelUseKey(level))) {
				return EditorPrefs.GetBool(LevelUseKey(level));
			}

			if (DefaultLevelUse.TryGetValue(level, out bool def)) {
				return def;
			}

			return true;
		}

		public static Color GetLevelColor(string level) {
			if (EditorPrefs.HasKey(LevelColorKey(level))) {
				// stored as RGBA hex string
				string hex = EditorPrefs.GetString(LevelColorKey(level));

				if (ColorUtility.TryParseHtmlString("#" + hex, out Color c)) {
					return c;
				}
			}

			if (DefaultLevelColors.TryGetValue(level, out Color defColor)) {
				return defColor;
			}

			return Color.white;
		}

		public static bool GetLevelBold(string level) {
			if (EditorPrefs.HasKey(LevelBoldKey(level))) {
				return EditorPrefs.GetBool(LevelBoldKey(level));
			}

			if (DefaultLevelBold.TryGetValue(level, out bool def)) {
				return def;
			}

			return false;
		}

		public static bool GetLevelItalic(string level) {
			if (EditorPrefs.HasKey(LevelItalicKey(level))) {
				return EditorPrefs.GetBool(LevelItalicKey(level));
			}

			if (DefaultLevelItalic.TryGetValue(level, out bool def)) {
				return def;
			}

			return false;
		}

		public static bool GetLevelDisplayLine(string level) {
			if (EditorPrefs.HasKey(LevelDisplayLineKey(level))) {
				return EditorPrefs.GetBool(LevelDisplayLineKey(level));
			}

			if (DefaultLevelDisplayLine.TryGetValue(level, out bool def)) {
				return def;
			}

			return true;
		}


		// UI
		[SettingsProvider]
		public static SettingsProvider CreateLoggerPreferencesProvider() {
			var provider = new SettingsProvider(_progName, SettingsScope.User) {
				label = string.Join(" ", Utils.SeparateCase(_progName).Where(s => !string.IsNullOrEmpty(s))),

				guiHandler = (searchContext) => {
					// main header style
					if (_headerStyle == null) {
						_headerStyle = new GUIStyle(EditorStyles.boldLabel) {
							fontSize = 14,
							fontStyle = FontStyle.Bold,
							richText = true,
							alignment = TextAnchor.MiddleLeft,
							padding = new RectOffset(0, 0, 6, 2)
						};
					}

					// -=-=-=- //

					EditorGUILayout.BeginVertical("box");

					// GUILayout.Label("General", _headerStyle);
					EnableLogger = EditorGUILayout.Toggle(
						new GUIContent(
							"Enable",
							"Enables the logger enhancing."
						),
						EnableLogger
					);

					EditorGUILayout.Space(_guiSpace);

					GUILayout.Label("Formatting", _headerStyle);

					Format = EditorGUILayout.TextField(
						new GUIContent(
							"String",
							string.Join("\n", new[] {
								"Debugging message format.",
								"",
								"Variables are:",
								$"* {Utils.Variables.Script} - Called script path",
								$"* {Utils.Variables.Separator} - Called script and line separator",
								$"* {Utils.Variables.Line} - Called script trace line",
								$"* {Utils.Variables.Level} - Logging level",
								$"* {Utils.Variables.Message} - Log message"
							})
						),
						Format,
						GUILayout.Height(20)
					);

					ScriptMode = (ScriptDisplayMode)EditorGUILayout.EnumPopup(
						new GUIContent(
							"Script Mode",
							"Determines whether the script display uses full path or file name without extension."
						),
						ScriptMode
					);

					EditorGUILayout.Space(_guiSpace);
					DrawDropdown(
						"Script",

						ref _scriptFoldout,

						ScriptUseColor, ScriptColor,
						ScriptBold, ScriptItalic,

						ScriptUseColorKey, ScriptColorKey,
						ScriptBoldKey, ScriptItalicKey
					);
					DrawDropdown(
						"Line",

						ref _lineFoldout,

						LineUseColor, LineColor,
						LineBold, LineItalic,

						LineUseColorKey, LineColorKey,
						LineBoldKey, LineItalicKey
					);
					DrawDropdown(
						"Message",

						ref _messageFoldout,

						MessageUseColor, MessageColor,
						MessageBold, MessageItalic,

						MessageUseColorKey, MessageColorKey,
						MessageBoldKey, MessageItalicKey
					);

					DrawLevelDropdown(ref _levelFoldout);

					EditorGUILayout.Space(_guiSpace);
					GUILayout.Label("Preview", _headerStyle);

					EditorGUILayout.BeginVertical("box");
					foreach (var line in GeneratePreview()) {
						GUILayout.Label(line, new GUIStyle(EditorStyles.wordWrappedLabel) { richText = true });
					}
					EditorGUILayout.EndVertical();

					if (GUILayout.Button("Reset to Defaults", GUILayout.Height(40))) {
						ResetToDefaults();
					}

					EditorGUILayout.EndVertical();

					// -=-=-=- //

					Debug.Log(Path.Combine(_pathIcons, "GitHub.png"));
					Texture2D iconGitHub = AssetDatabase.LoadAssetAtPath<Texture2D>(
						Path.Combine(_pathIcons, "GitHub.png")
					);

					if (GUILayout.Button(new GUIContent("  View source", iconGitHub), GUILayout.Height(30), GUILayout.Width(105))) {
						Application.OpenURL(_progURL);
					}
				}
			};
			return provider;
		}

		private static void DrawDropdown(
			string label,

			ref bool foldout,

			bool useColor, Color color,
			bool bold, bool italic,
			
			string useKey, string colorKey,
			string boldKey, string italicKey
		) {
		
			foldout = EditorGUILayout.Foldout(foldout, label, true);

			if (!foldout) {
				return;
			}

			useColor = EditorGUILayout.ToggleLeft("Use Color", useColor);
			EditorPrefs.SetBool(useKey, useColor);

			if (useColor) {
				Color newColor = EditorGUILayout.ColorField("Color", color);

				if (newColor != color) {
					Utils.SetColor(colorKey, newColor);
				}
			}

			bold = EditorGUILayout.ToggleLeft("Bold", bold);
			EditorPrefs.SetBool(boldKey, bold);

			italic = EditorGUILayout.ToggleLeft("Italic", italic);
			EditorPrefs.SetBool(italicKey, italic);

			EditorGUILayout.Space(_guiSpace / 2);
		}

		private static void DrawLevelDropdown(ref bool foldout) {
			foldout = EditorGUILayout.Foldout(foldout, "Level", true);

			if (!foldout) {
				return;
			}

			EditorGUILayout.Space(_guiSpace / 2);
			GUILayout.Label("Overrides", EditorStyles.boldLabel);

			for (int i = 0; i < LogLevels.Length; i++) {
				string lvl = LogLevels[i];

				_levelNestedFoldouts[i] = EditorGUILayout.Foldout(_levelNestedFoldouts[i], lvl, true);
				if (!_levelNestedFoldouts[i]) {
					continue;
				}

				// display name
				string displayNameKey = $"{_progName}.Level.{lvl}.DisplayName";
				string displayNameDefault = LevelDisplayNames.ContainsKey(lvl) ? LevelDisplayNames[lvl] : lvl;
				string displayName = EditorPrefs.HasKey(displayNameKey)
					? EditorPrefs.GetString(displayNameKey)
					: displayNameDefault;

				displayName = EditorGUILayout.TextField("Display Name", displayName);
				if (displayName.Length > _charLimit) { 
					displayName = displayName.Substring(0, _charLimit);
				}
	
				EditorPrefs.SetString(displayNameKey, displayName);

				// use color
				bool useColor = EditorPrefs.HasKey(LevelUseKey(lvl)) ? EditorPrefs.GetBool(LevelUseKey(lvl)) : GetLevelUseColor(lvl);
				useColor = EditorGUILayout.ToggleLeft("Use Color", useColor);
				EditorPrefs.SetBool(LevelUseKey(lvl), useColor);

				Color currentColor = Utils.GetColor(LevelColorKey(lvl), GetLevelColor(lvl));

				if (useColor) {
					Color newColor = EditorGUILayout.ColorField("Color", currentColor);
					
					if (newColor != currentColor) {
						Utils.SetColor(LevelColorKey(lvl), newColor);
					}
				} else {
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.ColorField("Color", currentColor);
					EditorGUI.EndDisabledGroup();
				}

				// bold
				bool bold = EditorPrefs.HasKey(LevelBoldKey(lvl)) ? EditorPrefs.GetBool(LevelBoldKey(lvl)) : GetLevelBold(lvl);
				bold = EditorGUILayout.ToggleLeft("Bold", bold);
				EditorPrefs.SetBool(LevelBoldKey(lvl), bold);

				// italic
				bool italic = EditorPrefs.HasKey(LevelItalicKey(lvl)) ? EditorPrefs.GetBool(LevelItalicKey(lvl)) : GetLevelItalic(lvl);
				italic = EditorGUILayout.ToggleLeft("Italic", italic);
				EditorPrefs.SetBool(LevelItalicKey(lvl), italic);

				// display line
				bool displayLine = EditorPrefs.HasKey(LevelDisplayLineKey(lvl)) ? EditorPrefs.GetBool(LevelDisplayLineKey(lvl)) : GetLevelDisplayLine(lvl);
				displayLine = EditorGUILayout.ToggleLeft("Display Line", displayLine);
				EditorPrefs.SetBool(LevelDisplayLineKey(lvl), displayLine);

				EditorGUILayout.Space(_guiSpace / 2);
			}
		}

		private static string[] GeneratePreview() {
			string scriptExamplePath = _progFile;
			string scriptExample = Utils.Repath(scriptExamplePath, ScriptMode == ScriptDisplayMode.Name);

			string script = Utils.ApplyStyle(
				Utils.Colorize(
					scriptExample,
					ScriptUseColor, ScriptColor
				),
				ScriptBold, ScriptItalic
			);

			var previewLines = new List<string>();

			foreach (var lvl in LogLevels) {
				string line = Utils.ApplyStyle(
					Utils.Colorize(
						UnityEngine.Random.Range(10, 99).ToString(),
						LineUseColor, LineColor
					),
					LineBold, LineItalic
				);

				string message = lvl.ToLower() switch {
					"info"    => "Hello World!",
					"warning" => "Something went wrong, check log for details",
					"error"   => "An error occurred",
					"success" => "Building done",
					"debug"   => $"Variable <\u200Bx> is {line}",
					"fatal"   => "Fatal error occurred",
					_         => "Log message"
				};

				message = Utils.ApplyStyle(
					Utils.Colorize(
						message,
						MessageUseColor, MessageColor
					),
					MessageBold, MessageItalic
				);

				string currentTime = $"[{System.DateTime.Now:HH:mm:ss}]";
				string levelText = "";
				string result = $"{currentTime} ";

				if (LoggerPreferences.EnableLogger) {
					levelText = Utils.ApplyStyle(
						Utils.Colorize(
							LoggerPreferences.GetLevelDisplayName(lvl),
							LoggerPreferences.GetLevelUseColor(lvl),
							LoggerPreferences.GetLevelColor(lvl)
						),
						LoggerPreferences.GetLevelBold(lvl),
						LoggerPreferences.GetLevelItalic(lvl)
					);

					Color levelColor = LoggerPreferences.GetLevelColor(lvl);
					bool levelVisible = LoggerPreferences.GetLevelUseColor(lvl) && levelColor.a > 0f;
					if (!levelVisible) {
						levelText = "";
					}

					bool showLine = GetLevelDisplayLine(lvl);
					string lineSep = showLine ? ":" : "";
					string lineText = showLine ? line : "";

					result += LoggerPreferences.Format
						.Replace($"{Utils.Variables.Script}", script)
						.Replace($"{Utils.Variables.Separator}{Utils.Variables.Line}", showLine ? lineSep + lineText : "")
						.Replace($"{Utils.Variables.Line}", lineText)
						.Replace($"{Utils.Variables.Level}", levelText)
						.Replace($"{Utils.Variables.Message}", message);
				} else {
					result += message;
				}

				previewLines.Add(result.Trim());
			}

			return previewLines.ToArray();
		}

		private static void ResetToDefaults() {
			// clear all basic keys

			// EditorPrefs.DeleteKey(EnableKey);
			EditorPrefs.DeleteKey(FormatStringKey);
			EditorPrefs.DeleteKey(ScriptModeKey);

			// script
			EditorPrefs.DeleteKey(ScriptUseColorKey);
			EditorPrefs.DeleteKey(ScriptColorKey);
			EditorPrefs.DeleteKey(ScriptBoldKey);
			EditorPrefs.DeleteKey(ScriptItalicKey);

			// line
			EditorPrefs.DeleteKey(LineUseColorKey);
			EditorPrefs.DeleteKey(LineColorKey);
			EditorPrefs.DeleteKey(LineBoldKey);
			EditorPrefs.DeleteKey(LineItalicKey);

			// message
			EditorPrefs.DeleteKey(MessageUseColorKey);
			EditorPrefs.DeleteKey(MessageColorKey);
			EditorPrefs.DeleteKey(MessageBoldKey);
			EditorPrefs.DeleteKey(MessageItalicKey);

			// delete per-level keys and reapply default per-level flags
			foreach (var lvl in LogLevels) {
				EditorPrefs.DeleteKey(LevelUseKey(lvl));
				EditorPrefs.DeleteKey(LevelColorKey(lvl));
				EditorPrefs.DeleteKey(LevelBoldKey(lvl));
				EditorPrefs.DeleteKey(LevelItalicKey(lvl));
				EditorPrefs.DeleteKey(LevelDisplayLineKey(lvl));

				// <-- reset display name
				EditorPrefs.DeleteKey($"{_progName}.Level.{lvl}.DisplayName");
			}

			// set default per-level colors & flags explicitly
			// so UI shows defaults immediately
			foreach (var kv in DefaultLevelColors) { Utils.SetColor(LevelColorKey(kv.Key), kv.Value); }
			foreach (var kv in DefaultLevelUse) { EditorPrefs.SetBool(LevelUseKey(kv.Key), kv.Value); }
			foreach (var kv in DefaultLevelBold) { EditorPrefs.SetBool(LevelBoldKey(kv.Key), kv.Value); }
			foreach (var kv in DefaultLevelItalic) { EditorPrefs.SetBool(LevelItalicKey(kv.Key), kv.Value); }
			foreach (var kv in DefaultLevelDisplayLine) { EditorPrefs.SetBool(LevelDisplayLineKey(kv.Key), kv.Value); }

			// restore default display names
			foreach (var kv in LevelDisplayNames) {
				EditorPrefs.SetString($"{_progName}.Level.{kv.Key}.DisplayName", kv.Value);
			}
		}
	}
}
#endif