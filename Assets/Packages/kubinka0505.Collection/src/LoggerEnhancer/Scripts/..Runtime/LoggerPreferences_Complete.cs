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
		// Lazy-loaded properties
		private static string _lazyProgDir;
		private static string _lazyProgName;
		private static bool _funcFoldout = false;

		private static string _progFile => PathUtils.Relative(PathUtils.GetTrace().file, PathUtils.dataPath);
		private static string _progDir => _lazyProgDir ??= ComputeProgDir();
		private static string _progName => _lazyProgName ??= (_progDir.StartsWith("Logger") ? _progDir : "LoggerEnhancer").Trim();

		private static string ComputeProgDir() {
			try {
				string dir = Path.GetFileName(Path.GetFullPath(Path.Combine(
					Path.GetDirectoryName(_progFile), "..", "..")));
				return string.IsNullOrEmpty(dir) ? "LoggerEnhancer" : dir;
			} catch {
				return "LoggerEnhancer";
			}
		}

		// Default format using new variable names
		private static readonly string _defaultFormatString = "{chars.open}{file.path}{chars.sep}{file.line}{chars.close} {chars.open}{log.lvl}{chars.close} {log.msg}";
		private static readonly int _guiSpace = 10;

		// Display mode
		public enum ScriptDisplayMode {
			Name,      // file.ext
			Basename,  // file (no extension)
			Path       // full/path/to/file.cs
		}

		// Func type
		public enum FuncDisplayMode {
			NameOnly,     // SomeFunction
			FullSignature // public static void SomeFunction(int arg)
		}

		// UI state
		private static Vector2 _previewScrollPosition = Vector2.zero;
		private static bool _formatFoldout = false;
		private static bool _levelsFoldout = false;
		private static bool _scriptFoldout = false;
		private static bool _lineFoldout = false;
		private static bool _messageFoldout = false;
		private static readonly bool[] _levelNestedFoldouts = new bool[6];

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
		private static readonly string MessageColorKey = $"{_progName}.MessageColor";
		private static readonly string MessageBoldKey = $"{_progName}.MessageBold";
		private static readonly string MessageItalicKey = $"{_progName}.MessageItalic";

		private static readonly string FuncUseColorKey = $"{_progName}.FuncUseColor";
		private static readonly string FuncColorKey = $"{_progName}.FuncColor";
		private static readonly string FuncBoldKey = $"{_progName}.FuncBold";
		private static readonly string FuncItalicKey = $"{_progName}.FuncItalic";
		private static readonly string FuncModeKey = $"{_progName}.FuncMode";

		public static bool FuncUseColor => EditorPrefs.GetBool(FuncUseColorKey, false);
		public static Color FuncColor => Utils.GetColor(FuncColorKey, Color.cyan);
		public static bool FuncBold => EditorPrefs.GetBool(FuncBoldKey, false);
		public static bool FuncItalic => EditorPrefs.GetBool(FuncItalicKey, false);

		// Log levels
		private static readonly string[] LogLevels = {
			"INFO", "WARNING", "ERROR", "SUCCESS", "DEBUG", "FATAL"
		};

		private static string LevelUseKey(string lvl) => $"{_progName}.Level.{lvl}.UseColor";
		private static string LevelColorKey(string lvl) => $"{_progName}.Level.{lvl}.Color";
		private static string LevelBoldKey(string lvl) => $"{_progName}.Level.{lvl}.Bold";
		private static string LevelItalicKey(string lvl) => $"{_progName}.Level.{lvl}.Italic";
		private static string LevelDisplayLineKey(string lvl) => $"{_progName}.Level.{lvl}.DisplayLine";

		// Defaults
		private static readonly Dictionary<string, Color> DefaultLevelColors = new Dictionary<string, Color> {
			{ "INFO",    new Color(0.27f, 0.67f, 1f, 1f) },
			{ "WARNING", new Color(1f, 0.8f, 0f, 1f) },
			{ "ERROR",   new Color(1f, 0f, 0f, 1f) },
			{ "SUCCESS", new Color(0f, 0.8f, 0f, 1f) },
			{ "DEBUG",   new Color(1f, 1f, 1f, 1f) },
			{ "FATAL",   new Color(0.75f, 0f, 0f, 1f) }
		};

		private static readonly Dictionary<string, bool> DefaultLevelUse = new Dictionary<string, bool> {
			{ "INFO", true }, { "WARNING", true }, { "ERROR", true },
			{ "SUCCESS", true }, { "DEBUG", true }, { "FATAL", true }
		};

		private static readonly Dictionary<string, bool> DefaultLevelBold = new Dictionary<string, bool> {
			{ "INFO", true }, { "WARNING", true }, { "ERROR", true },
			{ "SUCCESS", true }, { "DEBUG", true }, { "FATAL", true }
		};

		private static readonly Dictionary<string, bool> DefaultLevelItalic = new Dictionary<string, bool> {
			{ "INFO", false }, { "WARNING", false }, { "ERROR", false },
			{ "SUCCESS", false }, { "DEBUG", false }, { "FATAL", false }
		};

		private static readonly Dictionary<string, bool> DefaultLevelDisplayLine = new Dictionary<string, bool> {
			{ "INFO", false }, { "WARNING", true }, { "ERROR", true },
			{ "SUCCESS", false }, { "DEBUG", true }, { "FATAL", true }
		};

		// -=-=-=- //
		// PUBLIC ACCESSORS
		// -=-=-=- //

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

		public static FuncDisplayMode FuncMode {
			get => (FuncDisplayMode)EditorPrefs.GetInt(FuncModeKey, 0);
			set => EditorPrefs.SetInt(FuncModeKey, (int)value);
		}

		public static string GetCustomCharsOpen() {
			return EditorPrefs.GetString($"{_progName}.CustomCharsOpen", "[");
		}

		public static string GetCustomCharsClose() {
			return EditorPrefs.GetString($"{_progName}.CustomCharsClose", "]");
		}

		public static string GetCustomCharsSep() {
			return EditorPrefs.GetString($"{_progName}.CustomCharsSep", ":");
		}

		public static Color GetLevelColor(string level) {
			if (LogLevel.TryGet(level, out var customLevel)) {
				return customLevel.color;
			}
			return Utils.GetColor(LevelColorKey(level), DefaultLevelColors.ContainsKey(level) ? DefaultLevelColors[level] : Color.white);
		}

		public static bool GetLevelUseColor(string level) {
			if (LogLevel.TryGet(level, out var customLevel)) {
				return customLevel.useColor;
			}
			return EditorPrefs.HasKey(LevelUseKey(level)) ? EditorPrefs.GetBool(LevelUseKey(level)) : DefaultLevelUse.ContainsKey(level) && DefaultLevelUse[level];
		}

		public static bool GetLevelBold(string level) {
			if (LogLevel.TryGet(level, out var customLevel)) {
				return customLevel.bold;
			}
			return EditorPrefs.HasKey(LevelBoldKey(level)) ? EditorPrefs.GetBool(LevelBoldKey(level)) : DefaultLevelBold.ContainsKey(level) && DefaultLevelBold[level];
		}

		public static bool GetLevelItalic(string level) {
			if (LogLevel.TryGet(level, out var customLevel)) {
				return customLevel.italic;
			}
			return EditorPrefs.HasKey(LevelItalicKey(level)) ? EditorPrefs.GetBool(LevelItalicKey(level)) : DefaultLevelItalic.ContainsKey(level) && DefaultLevelItalic[level];
		}

		public static bool GetLevelDisplayLine(string level) {
			if (LogLevel.TryGet(level, out var customLevel)) {
				return customLevel.displayLine;
			}
			return EditorPrefs.HasKey(LevelDisplayLineKey(level)) ? EditorPrefs.GetBool(LevelDisplayLineKey(level)) : DefaultLevelDisplayLine.ContainsKey(level) && DefaultLevelDisplayLine[level];
		}

		public static string GetLevelDisplayName(string level) {
			if (LogLevel.TryGet(level, out var customLevel)) {
				return customLevel.displayName;
			}
			string displayNameKey = $"{_progName}.Level.{level}.DisplayName";
			
			// Fix: Fall back to 'level' instead of calling GetLevelDisplayName recursively
			return EditorPrefs.GetString(displayNameKey, level);
		}

		// -=-=-=- //
		// SHARED LIVE PREVIEW FUNCTION
		// -=-=-=- //

		public static string[] GenerateLivePreview() {
			string scriptExamplePath = _progFile;
			string displayName = Utils.Repath(scriptExamplePath, ScriptMode == ScriptDisplayMode.Path ? false : true);
			
			// Handle Basename mode (no extension)
			if (ScriptMode == ScriptDisplayMode.Basename) {
				displayName = Path.GetFileNameWithoutExtension(displayName);
			}

			var previewLines = new List<string>();
			string customSep = GetCustomCharsSep();
			string customOpen = GetCustomCharsOpen();
			string customClose = GetCustomCharsClose();

			foreach (var lvl in LogLevels) {
				string script = Utils.ApplyStyle(
					Utils.Colorize(displayName, ScriptUseColor, ScriptColor),
					ScriptBold, ScriptItalic
				);

				string lineNumber = UnityEngine.Random.Range(10, 99).ToString();
				string line = Utils.ApplyStyle(
					Utils.Colorize(lineNumber, LineUseColor, LineColor),
					LineBold, LineItalic
				);

				string funcName = Utils.ApplyStyle(
					Utils.Colorize("SomeFunction", FuncUseColor, FuncColor),
					FuncBold, FuncItalic
				);

				string message = lvl.ToLower() switch {
					"info"    => "Hello World!",
					"warning" => "Something went wrong",
					"error"   => "An error occurred",
					"success" => "Building done",
					"debug"   => "Debug information",
					"fatal"   => "Fatal error occurred",
					_         => "Log message"
				};

				message = Utils.ApplyStyle(
					Utils.Colorize(message, MessageUseColor, MessageColor),
					MessageBold, MessageItalic
				);

				if (!EnableLogger) {
					previewLines.Add(message);
					continue;
				}

				// lvl is valid here
				string levelText = Utils.ApplyStyle(
					Utils.Colorize(GetLevelDisplayName(lvl), GetLevelUseColor(lvl), GetLevelColor(lvl)),
					GetLevelBold(lvl),
					GetLevelItalic(lvl)
				);

				bool showLine = true;
				string lineText = showLine ? line : "";

				var variables = new Dictionary<string, string> {
					{ Utils.Variables.FilePath, script },
					{ Utils.Variables.FileLine, lineText },
					{ Utils.Variables.FileFunc, funcName },
					{ Utils.Variables.LogLevel, levelText },
					{ Utils.Variables.LogMsg, message }
				};

				var charMappings = new Dictionary<string, string> {
					{ "open", customOpen },
					{ "close", customClose },
					{ "sep", customSep }
				};

				string result = FormatStringProcessor.ProcessFormat(Format, variables, charMappings);
				previewLines.Add(result.Trim());
			} // Make sure the closing brace ends AFTER the dictionary logic

			return previewLines.ToArray();
		}

		// -=-=-=- //
		// SETTINGS PROVIDER UI
		// -=-=-=- //

		[SettingsProvider]
		public static SettingsProvider CreateSettingsProvider() {
			var provider = new SettingsProvider("Preferences/Logger Enhancer", SettingsScope.User) {
				label = "Logger Enhancer",
				guiHandler = OnGUI,
			};
			return provider;
		}

		private static void OnGUI(string searchContext) {
			EditorGUILayout.Space(_guiSpace);
			GUILayout.Label("Logger Enhancer Settings", EditorStyles.boldLabel);
			EditorGUILayout.Space(_guiSpace);

			// Master enable
			bool enable = EditorGUILayout.ToggleLeft("Enable Logger", EnableLogger);
			if (enable != EnableLogger) {
				EnableLogger = enable;
			}

			EditorGUILayout.Space(_guiSpace);

			// LIVE PREVIEW AT TOP
			DrawLivePreviewBox();

			EditorGUILayout.Space(_guiSpace);

			// FORMATTING SCOPE
			_formatFoldout = EditorGUILayout.Foldout(_formatFoldout, "Formatting", true);
			if (_formatFoldout) {
				DrawFormattingScope();
			}

			EditorGUILayout.Space(_guiSpace);

			// LOG LEVELS SCOPE
			_levelsFoldout = EditorGUILayout.Foldout(_levelsFoldout, "Log Levels", true);
			if (_levelsFoldout) {
				DrawLogLevelsScope();
			}

			EditorGUILayout.Space(_guiSpace);

			// Reset button
			if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30))) {
				if (EditorUtility.DisplayDialog("Reset Logger Preferences", "Reset all settings to defaults?", "Yes", "No")) {
					ResetToDefaults();
				}
			}
		}

		private static void DrawLivePreviewBox() {
			EditorGUILayout.LabelField("Live Preview", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("(Updates in real-time)", EditorStyles.miniLabel);
			
			var previewLines = GenerateLivePreview();

			// Define a style that enables Rich Text parsing
			GUIStyle richStyle = new GUIStyle(EditorStyles.label) {
				richText = true
			};

			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			foreach (var line in previewLines) {
				EditorGUILayout.SelectableLabel(line, richStyle, GUILayout.MinHeight(18));
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.Space(_guiSpace / 2);
		}

		private static void DrawFormattingScope() {
			EditorGUILayout.Space(_guiSpace / 2);

			// Format string editor
			EditorGUILayout.LabelField("Format String", EditorStyles.boldLabel);
			string currentFormat = Format;
			string newFormat = EditorGUILayout.TextArea(currentFormat, GUILayout.Height(60));
			if (newFormat != currentFormat) {
				Format = newFormat;
			}

			EditorGUILayout.Space(_guiSpace / 2);

			// Available variables
			EditorGUILayout.LabelField("Available Variables:", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox(
				"{file.path} - Script file name/path\n" +
				"{file.line} - Line number\n" +
				"{file.func} - Function/method name\n" +
				"{chars.sep} - Separator (customizable)\n" +
				"{log.lvl} - Level display name\n" +
				"{log.msg} - Log message\n" +
				"{chars.open} - Opening bracket (customizable)\n" +
				"{chars.close} - Closing bracket (customizable)",
				MessageType.Info
			);

			EditorGUILayout.Space(_guiSpace / 2);

			// Color tokens
			EditorGUILayout.LabelField("Color Tokens:", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox(
				"{color.red} - Named colors (red, blue, gray, etc.)\n" +
				"{color.rgb(\"#FF0000\")} - Hex notation\n" +
				"{color.init} - Default log color (theme-aware)",
				MessageType.Info
			);

			EditorGUILayout.Space(_guiSpace / 2);

			// Display mode
			ScriptDisplayMode newMode = (ScriptDisplayMode)EditorGUILayout.EnumPopup("File Display Mode", ScriptMode);
			if (newMode != ScriptMode) {
				ScriptMode = newMode;
			}

			EditorGUILayout.Space(_guiSpace / 2);

			// Styling sections
			DrawStyleSettings("Script Path", ref _scriptFoldout, ScriptUseColor, ScriptColor, ScriptBold, ScriptItalic,
				ScriptUseColorKey, ScriptColorKey, ScriptBoldKey, ScriptItalicKey);

			EditorGUILayout.Space(_guiSpace / 2);

			DrawStyleSettings("Line Number", ref _lineFoldout, LineUseColor, LineColor, LineBold, LineItalic,
				LineUseColorKey, LineColorKey, LineBoldKey, LineItalicKey);

			EditorGUILayout.Space(_guiSpace / 2);

			// STEP C: Function Name styling section
			DrawStyleSettings("Function Name", ref _funcFoldout, FuncUseColor, FuncColor, FuncBold, FuncItalic,
				FuncUseColorKey, FuncColorKey, FuncBoldKey, FuncItalicKey);

			if (_funcFoldout) {
				EditorGUI.indentLevel++;
				FuncDisplayMode newFuncMode = (FuncDisplayMode)EditorGUILayout.EnumPopup("Function Display Mode", FuncMode);
				if (newFuncMode != FuncMode) {
					FuncMode = newFuncMode;
				}
				EditorGUI.indentLevel--;
				EditorGUILayout.Space(_guiSpace / 2);
			}

			EditorGUILayout.Space(_guiSpace / 2);

			DrawStyleSettings("Message", ref _messageFoldout, MessageUseColor, MessageColor, MessageBold, MessageItalic,
				MessageUseColorKey, MessageColorKey, MessageBoldKey, MessageItalicKey);

			EditorGUILayout.Space(_guiSpace / 2);
		}

		private static void DrawLogLevelsScope() {
			EditorGUILayout.Space(_guiSpace / 2);

			GUILayout.Label("Built-in Log Levels", EditorStyles.boldLabel);
			for (int i = 0; i < LogLevels.Length; i++) {
				string lvl = LogLevels[i];

				_levelNestedFoldouts[i] = EditorGUILayout.Foldout(_levelNestedFoldouts[i], lvl, true);
				if (!_levelNestedFoldouts[i]) continue;

				string displayNameKey = $"{_progName}.Level.{lvl}.DisplayName";
				string displayName = EditorPrefs.GetString(displayNameKey, GetLevelDisplayName(lvl));
				displayName = EditorGUILayout.TextField("Display Name", displayName);
				if (displayName.Length > 50) displayName = displayName.Substring(0, 50);
				EditorPrefs.SetString(displayNameKey, displayName);

				bool useColor = EditorPrefs.GetBool(LevelUseKey(lvl), GetLevelUseColor(lvl));
				useColor = EditorGUILayout.ToggleLeft("Use Color", useColor);
				EditorPrefs.SetBool(LevelUseKey(lvl), useColor);

				Color currentColor = Utils.GetColor(LevelColorKey(lvl), GetLevelColor(lvl));
				if (useColor) {
					Color newColor = EditorGUILayout.ColorField("Color", currentColor);
					if (newColor != currentColor) {
						Utils.SetColor(LevelColorKey(lvl), newColor);
					}
				}

				bool bold = EditorPrefs.GetBool(LevelBoldKey(lvl), GetLevelBold(lvl));
				bold = EditorGUILayout.ToggleLeft("Bold", bold);
				EditorPrefs.SetBool(LevelBoldKey(lvl), bold);

				bool italic = EditorPrefs.GetBool(LevelItalicKey(lvl), GetLevelItalic(lvl));
				italic = EditorGUILayout.ToggleLeft("Italic", italic);
				EditorPrefs.SetBool(LevelItalicKey(lvl), italic);

				bool displayLine = EditorPrefs.GetBool(LevelDisplayLineKey(lvl), GetLevelDisplayLine(lvl));
				displayLine = EditorGUILayout.ToggleLeft("Display Line Number", displayLine);
				EditorPrefs.SetBool(LevelDisplayLineKey(lvl), displayLine);

				EditorGUILayout.Space(_guiSpace / 2);
			}

			// Custom levels
			EditorGUILayout.Space(_guiSpace);
			GUILayout.Label("Custom Log Levels", EditorStyles.boldLabel);
			var customLevels = LogLevelRegistry.Instance.GetAllLevels();
			if (customLevels.Count == 0) {
				EditorGUILayout.HelpBox("No custom log levels. Use LogLevel.Register() to add them.", MessageType.Info);
			} else {
				foreach (var custom in customLevels) {
					EditorGUILayout.LabelField($"[{custom.displayName}]", EditorStyles.boldLabel);
					EditorGUILayout.ColorField("Color", custom.color);
					EditorGUILayout.ToggleLeft("Bold", custom.bold);
					EditorGUILayout.ToggleLeft("Italic", custom.italic);
					EditorGUILayout.ToggleLeft("Display Line", custom.displayLine);
					EditorGUILayout.Space(_guiSpace / 2);
				}
			}
		}

		private static void DrawStyleSettings(string label, ref bool foldout, bool currentUseColor, Color currentColor, bool currentBold, bool currentItalic,
			string useColorKey, string colorKey, string boldKey, string italicKey) {
			foldout = EditorGUILayout.Foldout(foldout, label, true);
			if (!foldout) return;

			EditorGUILayout.Space(_guiSpace / 2);

			bool useColor = currentUseColor;
			useColor = EditorGUILayout.ToggleLeft("Use Color", useColor);
			EditorPrefs.SetBool(useColorKey, useColor);

			if (useColor) {
				Color newColor = EditorGUILayout.ColorField("Color", currentColor);
				if (newColor != currentColor) {
					Utils.SetColor(colorKey, newColor);
				}
			}

			bool bold = currentBold;
			bold = EditorGUILayout.ToggleLeft("Bold", bold);
			EditorPrefs.SetBool(boldKey, bold);

			bool italic = currentItalic;
			italic = EditorGUILayout.ToggleLeft("Italic", italic);
			EditorPrefs.SetBool(italicKey, italic);

			EditorGUILayout.Space(_guiSpace / 2);
		}

		private static void ResetToDefaults() {
			EditorPrefs.DeleteKey(FormatStringKey);
			EditorPrefs.DeleteKey(ScriptModeKey);

			EditorPrefs.DeleteKey(ScriptUseColorKey);
			EditorPrefs.DeleteKey(ScriptColorKey);
			EditorPrefs.DeleteKey(ScriptBoldKey);
			EditorPrefs.DeleteKey(ScriptItalicKey);

			EditorPrefs.DeleteKey(LineUseColorKey);
			EditorPrefs.DeleteKey(LineColorKey);
			EditorPrefs.DeleteKey(LineBoldKey);
			EditorPrefs.DeleteKey(LineItalicKey);

			EditorPrefs.DeleteKey(MessageUseColorKey);
			EditorPrefs.DeleteKey(MessageColorKey);
			EditorPrefs.DeleteKey(MessageBoldKey);
			EditorPrefs.DeleteKey(MessageItalicKey);

			foreach (var lvl in LogLevels) {
				EditorPrefs.DeleteKey(LevelUseKey(lvl));
				EditorPrefs.DeleteKey(LevelColorKey(lvl));
				EditorPrefs.DeleteKey(LevelBoldKey(lvl));
				EditorPrefs.DeleteKey(LevelItalicKey(lvl));
				EditorPrefs.DeleteKey(LevelDisplayLineKey(lvl));
				EditorPrefs.DeleteKey($"{_progName}.Level.{lvl}.DisplayName");
			}

			foreach (var kv in DefaultLevelColors) { Utils.SetColor(LevelColorKey(kv.Key), kv.Value); }
			foreach (var kv in DefaultLevelUse) { EditorPrefs.SetBool(LevelUseKey(kv.Key), kv.Value); }
			foreach (var kv in DefaultLevelBold) { EditorPrefs.SetBool(LevelBoldKey(kv.Key), kv.Value); }
			foreach (var kv in DefaultLevelItalic) { EditorPrefs.SetBool(LevelItalicKey(kv.Key), kv.Value); }
			foreach (var kv in DefaultLevelDisplayLine) { EditorPrefs.SetBool(LevelDisplayLineKey(kv.Key), kv.Value); }
		}
	}
}
#endif