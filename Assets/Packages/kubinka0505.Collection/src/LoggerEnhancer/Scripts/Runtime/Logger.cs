using UnityEngine;

using PathE;

// -=-=-=- //

namespace Logger {
	public static class Debug {
		private static void LogInternal(string level, object message, Object context) {
			string formattedMessage;

#if UNITY_EDITOR
			// editor
			formattedMessage = Formatter(level, message);
#else
			// runtime
			formattedMessage = message?.ToString() ?? "";
#endif

			switch (level) {
				case "WARNING":
					UnityEngine.Debug.LogWarning(formattedMessage, context);
					break;
				case "ERROR":
				case "FATAL":
					UnityEngine.Debug.LogError(formattedMessage, context);
					break;
				default:
					UnityEngine.Debug.Log(formattedMessage, context);
					break;
			}
		}
		public delegate void LogMethod(object message, Object context = null);

		private static LogMethod CreateAlias(string level) => (msg, ctx) => LogInternal(level, msg, ctx);

		public static readonly LogMethod Log = CreateAlias("INFO");
		public static readonly LogMethod LogInfo = Log; // alias
		public static readonly LogMethod LogSuccess = CreateAlias("SUCCESS");
		public static readonly LogMethod LogDebug = CreateAlias("DEBUG");
		public static readonly LogMethod LogWarning = CreateAlias("WARNING");
		public static readonly LogMethod LogError = CreateAlias("ERROR");
		public static readonly LogMethod LogFatal = CreateAlias("FATAL");

#if UNITY_EDITOR
		private static string Formatter(string level, object message) {
			if (!LoggerPreferences.EnableLogger) {
				return message?.ToString() ?? "";
			}

			var (scriptPath, lineNumber) = PathUtils.GetTrace();
			scriptPath = PathUtils.Relative(scriptPath, PathUtils.dataPath);
			string scriptName = Utils.Repath(scriptPath, LoggerPreferences.ScriptMode == LoggerPreferences.ScriptDisplayMode.Name);

			// per-level
			bool displayLine = LoggerPreferences.GetLevelDisplayLine(level);
			string lineSep = displayLine ? ":" : "";
			string lineText = displayLine ? lineNumber.ToString() : "";

			// apply level display name and color
			string displayName = LoggerPreferences.GetLevelDisplayName(level);
			Color levelColor = LoggerPreferences.GetLevelColor(level);
			bool levelVisible = LoggerPreferences.GetLevelUseColor(level) && levelColor.a > 0f;

			string levelText = "";
			if (levelVisible) {
				levelText = Utils.ApplyStyle(
					Utils.Colorize(
						displayName,
						LoggerPreferences.GetLevelUseColor(level),
						levelColor
					),
					LoggerPreferences.GetLevelBold(level),
					LoggerPreferences.GetLevelItalic(level)
				);
			}

			// -=-=-=- //
			// styles

			// script
			string styledScript = Utils.ApplyStyle(
				Utils.Colorize(scriptName, LoggerPreferences.ScriptUseColor, LoggerPreferences.ScriptColor),
				LoggerPreferences.ScriptBold,
				LoggerPreferences.ScriptItalic
			);

			// line
			string styledLine = Utils.ApplyStyle(
				Utils.Colorize(displayLine ? lineSep + lineText : "", LoggerPreferences.LineUseColor, LoggerPreferences.LineColor),
				LoggerPreferences.LineBold,
				LoggerPreferences.LineItalic
			);

			// standalone line
			string styledLineOnly = Utils.ApplyStyle(
				Utils.Colorize(displayLine ? lineText : "", LoggerPreferences.LineUseColor, LoggerPreferences.LineColor),
				LoggerPreferences.LineBold,
				LoggerPreferences.LineItalic
			);

			// message
			string styledMessage = Utils.ApplyStyle(
				Utils.Colorize(message?.ToString() ?? "", LoggerPreferences.MessageUseColor, LoggerPreferences.MessageColor),
				LoggerPreferences.MessageBold,
				LoggerPreferences.MessageItalic
			);

			// -=-=-=- //

			// assemble final string
			string output = LoggerPreferences.Format
				.Replace($"{Utils.Variables.Script}", styledScript)
				.Replace($"{Utils.Variables.Separator}{Utils.Variables.Line}", styledLine)
				.Replace($"{Utils.Variables.Line}", styledLineOnly)
				.Replace($"{Utils.Variables.Level}", levelText)
				.Replace($"{Utils.Variables.Message}", styledMessage);

			return output;
		}
#endif
	}
}