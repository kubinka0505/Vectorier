using UnityEngine;
using PathE;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;

// -=-=-=- //

namespace Logger {
	public struct LogOverrides {
		public string name;           // Custom level display name
		public string clr;            // Color override (web color, hex, or rgb notation)
		public int? pad;              // Padding width
		public string padStr;         // Padding character(s)
		public string bLeft;          // Left bracket
		public string bRight;         // Right bracket

		public static LogOverrides None => new LogOverrides {
			name = null,
			clr = null,
			pad = null,
			padStr = null,
			bLeft = null,
			bRight = null
		};

		public bool HasOverrides => name != null || clr != null || pad.HasValue || padStr != null || bLeft != null || bRight != null;
	}

	public static class Debug {
		private static void LogInternal(string level, object message, Object context, LogOverrides overrides = default) {
			string formattedMessage;

			#if UNITY_EDITOR
			// editor
			formattedMessage = Formatter(level, message, overrides);
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
		public delegate void LogMethodWithOverrides(object message, Object context, LogOverrides overrides);

		// Core logging methods (base overloads)
		public static void Log(object message, Object context = null) => LogInternal("INFO", message, context);
		public static void LogInfo(object message, Object context = null) => LogInternal("INFO", message, context);
		public static void LogSuccess(object message, Object context = null) => LogInternal("SUCCESS", message, context);
		public static void LogDebug(object message, Object context = null) => LogInternal("DEBUG", message, context);
		public static void LogWarning(object message, Object context = null) => LogInternal("WARNING", message, context);
		public static void LogError(object message, Object context = null) => LogInternal("ERROR", message, context);
		public static void LogFatal(object message, Object context = null) => LogInternal("FATAL", message, context);

		// Overload methods that accept LogOverrides with context
		public static void Log(object message, Object context, LogOverrides overrides) => LogInternal("INFO", message, context, overrides);
		public static void LogInfo(object message, Object context, LogOverrides overrides) => LogInternal("INFO", message, context, overrides);
		public static void LogSuccess(object message, Object context, LogOverrides overrides) => LogInternal("SUCCESS", message, context, overrides);
		public static void LogDebug(object message, Object context, LogOverrides overrides) => LogInternal("DEBUG", message, context, overrides);
		public static void LogWarning(object message, Object context, LogOverrides overrides) => LogInternal("WARNING", message, context, overrides);
		public static void LogError(object message, Object context, LogOverrides overrides) => LogInternal("ERROR", message, context, overrides);
		public static void LogFatal(object message, Object context, LogOverrides overrides) => LogInternal("FATAL", message, context, overrides);

		// Convenience overloads with null context
		public static void Log(object message, LogOverrides overrides) => LogInternal("INFO", message, null, overrides);
		public static void LogInfo(object message, LogOverrides overrides) => LogInternal("INFO", message, null, overrides);
		public static void LogSuccess(object message, LogOverrides overrides) => LogInternal("SUCCESS", message, null, overrides);
		public static void LogDebug(object message, LogOverrides overrides) => LogInternal("DEBUG", message, null, overrides);
		public static void LogWarning(object message, LogOverrides overrides) => LogInternal("WARNING", message, null, overrides);
		public static void LogError(object message, LogOverrides overrides) => LogInternal("ERROR", message, null, overrides);
		public static void LogFatal(object message, LogOverrides overrides) => LogInternal("FATAL", message, null, overrides);

		// Method to create overrides fluently
		public static LogOverrides CreateOverrides(string name = null, string clr = null, int? pad = null, string padStr = null, string bLeft = null, string bRight = null) {
			return new LogOverrides {
				name = name,
				clr = clr,
				pad = pad,
				padStr = padStr,
				bLeft = bLeft,
				bRight = bRight
			};
		}

		// Custom level logging
		public static void LogCustom(string levelKey, object message, Object context = null, LogOverrides overrides = default) {
			LogInternal(levelKey, message, context, overrides);
		}

#if UNITY_EDITOR
		private static string Formatter(string level, object message, LogOverrides overrides = default) {
			if (!LoggerPreferences.EnableLogger) {
				return message?.ToString() ?? "";
			}

			var (scriptPath, lineNumber) = PathUtils.GetTrace();
			scriptPath = PathUtils.Relative(scriptPath, PathUtils.dataPath);
			
			// Handle display mode for file path
			string fileName = scriptPath;
			if (LoggerPreferences.ScriptMode == LoggerPreferences.ScriptDisplayMode.Name) {
				fileName = System.IO.Path.GetFileName(scriptPath);  // Just filename with ext
			} else if (LoggerPreferences.ScriptMode == LoggerPreferences.ScriptDisplayMode.Basename) {
				fileName = System.IO.Path.GetFileNameWithoutExtension(scriptPath);  // No extension
			}
			// else: Path - use full scriptPath

			// Grab frame 1 (or whichever frame called the logger)
			StackFrame frame = new StackFrame(999, false);
			MethodBase callingMethod = frame.GetMethod();

			// Outputs: "public static void MyMethod(string msg, int value)"
			string funcName = Utils.FormatMethodName(callingMethod, LoggerPreferences.FuncMode);

			// per-level
			bool displayLine = LoggerPreferences.GetLevelDisplayLine(level);
			string lineSep = displayLine ? LoggerPreferences.GetCustomCharsSep() : "";
			string lineText = displayLine ? lineNumber.ToString() : "";

			// apply level display name and color
			string displayName = overrides.name ?? LoggerPreferences.GetLevelDisplayName(level);
			Color levelColor = string.IsNullOrEmpty(overrides.clr) 
				? LoggerPreferences.GetLevelColor(level) 
				: ColorParser.Parse(overrides.clr);
			bool levelVisible = LoggerPreferences.GetLevelUseColor(level) && levelColor.a > 0f;

			// Apply padding if specified
			if (overrides.pad.HasValue && !string.IsNullOrEmpty(overrides.padStr)) {
				displayName = displayName.PadRight(overrides.pad.Value, overrides.padStr[0]);
			}

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

			// file path
			string styledFilePath = Utils.ApplyStyle(
				Utils.Colorize(fileName, LoggerPreferences.ScriptUseColor, LoggerPreferences.ScriptColor),
				LoggerPreferences.ScriptBold,
				LoggerPreferences.ScriptItalic
			);

			// line number
			string styledLineNo = Utils.ApplyStyle(
				Utils.Colorize(displayLine ? lineText : "", LoggerPreferences.LineUseColor, LoggerPreferences.LineColor),
				LoggerPreferences.LineBold,
				LoggerPreferences.LineItalic
			);

			// function name
			string styledFuncName = Utils.ApplyStyle(
				Utils.Colorize(funcName, LoggerPreferences.LineUseColor, LoggerPreferences.LineColor),
				LoggerPreferences.LineBold,
				LoggerPreferences.LineItalic
			);

			// separator
			string styledSep = Utils.ApplyStyle(
				Utils.Colorize(lineSep, LoggerPreferences.LineUseColor, LoggerPreferences.LineColor),
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
			// Build variables dictionary for format string processing
			// NEW variable names with new naming convention
			var variables = new Dictionary<string, string> {
				{ Utils.Variables.FilePath, styledFilePath },
				{ Utils.Variables.FileLine, styledLineNo },
				{ Utils.Variables.FileFunc, styledFuncName },
				{ Utils.Variables.LogLevel, levelText },
				{ Utils.Variables.LogMsg, styledMessage },
				// Legacy variables for backward compatibility
				{ Utils.Variables.Script, styledFilePath },
				{ Utils.Variables.Separator, styledSep },
				{ Utils.Variables.Line, styledLineNo },
				{ Utils.Variables.Level, levelText },
				{ Utils.Variables.Message, styledMessage }
			};

			// Build char mappings for format string
			var charMappings = new Dictionary<string, string> {
				{ "open", LoggerPreferences.GetCustomCharsOpen() },
				{ "close", LoggerPreferences.GetCustomCharsClose() },
				{ "sep", LoggerPreferences.GetCustomCharsSep() }
			};

			// Process format string with color tokens, char tokens, and variable substitution
			string output = FormatStringProcessor.ProcessFormat(LoggerPreferences.Format, variables, charMappings);

			return output;
		}
#endif
	}
}