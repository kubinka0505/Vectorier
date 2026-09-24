using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

// -=-=-=- //

namespace Logger {
	/// <summary>
	/// Definition of a custom log level with full styling options.
	/// </summary>
	[System.Serializable]
	public class LogLevelDefinition {
		public string key;
		public string displayName;
		public Color color;
		public bool useColor = true;
		public bool bold = false;
		public bool italic = false;
		public bool displayLine = true;

		public LogLevelDefinition(string key, string displayName, Color color) {
			this.key = key;
			this.displayName = displayName;
			this.color = color;
		}
	}

	public static class LogLevel {
		// Register a custom log level
		public static void Register(string key, string displayName, Color color) {
			var level = new LogLevelDefinition(key, displayName, color);
			LogLevelRegistry.Instance.RegisterLevel(level);
		}

		// Register with full styling options
		public static void Register(string key, string displayName, Color color, bool bold, bool italic, bool displayLine) {
			var level = new LogLevelDefinition(key, displayName, color) {
				bold = bold,
				italic = italic,
				displayLine = displayLine
			};
			LogLevelRegistry.Instance.RegisterLevel(level);
		}

		// Unregister a custom level
		public static void Unregister(string key) {
			LogLevelRegistry.Instance.UnregisterLevel(key);
		}

		// Check if a level exists
		public static bool Exists(string key) {
			// Check built-in levels
			var builtIn = new[] { "INFO", "WARNING", "ERROR", "SUCCESS", "DEBUG", "FATAL" };
			if (System.Array.Exists(builtIn, element => element == key))
				return true;

			// Check custom levels
			return LogLevelRegistry.Instance.TryGetLevel(key, out _);
		}

		// Get a level's configuration (built-in or custom)
		public static bool TryGet(string key, out LogLevelDefinition level) {
			return LogLevelRegistry.Instance.TryGetLevel(key, out level);
		}
	}
}
