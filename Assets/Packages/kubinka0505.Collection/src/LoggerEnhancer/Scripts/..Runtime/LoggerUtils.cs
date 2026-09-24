#if UNITY_EDITOR
using UnityEngine;

using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

// -=-=-=- //

namespace Logger {
	public static class Utils {
		public static class Variables {
			// New naming convention
			public static string FilePath  = "{file.path}";
			public static string FileLine  = "{file.line}";
			public static string FileFunc  = "{file.func}";
			public static string LogLevel  = "{log.lvl}";
			public static string LogMsg    = "{log.msg}";

			// Legacy names (kept for backward compatibility)
			public static string Script    = "{script}";
			public static string Separator = "{sep}";
			public static string Line      = "{line}";
			public static string Level     = "{lvl}";
			public static string Message   = "{msg}";
		}

		// apply bold and italic
		public static string ApplyStyle(string text, bool bold, bool italic) {
			if (bold) {
				text = $"<b>{text}</b>";
			}

			if (italic) {
				text = $"<i>{text}</i>";
			};

			return text;
		}

		// apply color styling
		public static string Colorize(string text, bool useColor, Color color) {
			if (!useColor) {
				return text;
			}

			string hex = ColorUtility.ToHtmlStringRGBA(color);
			return $"<color=#{hex}>{text}</color>";
		}

		// apply path
		public static string Repath(string path, bool nameOnly) {
			return nameOnly ? Path.GetFileNameWithoutExtension(path) : path;
		}

		// set color in EditorPrefs
		public static void SetColor(string key, Color color) {
			UnityEditor.EditorPrefs.SetString(key, ColorUtility.ToHtmlStringRGBA(color));
		}

		// get color from EditorPrefs
		public static Color GetColor(string key, Color defaultColor) {
			if (UnityEditor.EditorPrefs.HasKey(key)) {
				if (ColorUtility.TryParseHtmlString("#" + UnityEditor.EditorPrefs.GetString(key), out Color color)) {
					return color;
				}
			}

			return defaultColor;
		}

		public static (MethodBase method, int lineNumber) GetCallerInfo() {
			// Need fNeedFileInfo: true to capture line numbers!
			var trace = new StackTrace(fNeedFileInfo: true);
			
			for (int i = 0; i < trace.FrameCount; i++) {
				var frame = trace.GetFrame(i);
				var method = frame?.GetMethod();
				
				if (method == null) continue;
				
				Type declaringType = method.DeclaringType;
				if (declaringType != null) {
					// Skip frames inside Logger namespace or internal C# system calls
					string ns = declaringType.Namespace ?? "";
				}

				int lineNumber = frame.GetFileLineNumber();
				return (method, lineNumber);
			}

			return (null, 0);
		}

		public static string FormatMethodName(MethodBase method, LoggerPreferences.FuncDisplayMode mode) {
			if (method == null) return "UnknownMethod";

			if (mode == LoggerPreferences.FuncDisplayMode.NameOnly) {
				return method.Name;
			}

			// Full Signature Formatting
			var sb = new StringBuilder();

			if (method.IsPublic) sb.Append("public ");
			else if (method.IsPrivate) sb.Append("private ");
			else if (method.IsFamily) sb.Append("protected ");
			else if (method.IsAssembly) sb.Append("internal ");

			if (method.IsStatic) sb.Append("static ");

			if (method is MethodInfo methodInfo) {
				sb.Append(GetFriendlyTypeName(methodInfo.ReturnType)).Append(" ");
			}

			sb.Append(method.Name).Append("(");

			var parameters = method.GetParameters();
			for (int i = 0; i < parameters.Length; i++) {
				sb.Append(GetFriendlyTypeName(parameters[i].ParameterType));
				sb.Append(" ");
				sb.Append(parameters[i].Name);
				if (i < parameters.Length - 1) sb.Append(", ");
			}
			sb.Append(")");

			return sb.ToString();
		}

		public static string GetFriendlyTypeName(Type type) {
			if (type == typeof(void)) return "void";
			if (type == typeof(int)) return "int";
			if (type == typeof(float)) return "float";
			if (type == typeof(bool)) return "bool";
			if (type == typeof(string)) return "string";
			if (type == typeof(object)) return "object";
			return type.Name;
		}

		// -=-=-=- //

		public static string[] SeparateCase(string input) {
			if (string.IsNullOrEmpty(input)) {
				return new string[0];
			}

			return Regex.Split(input, @"(?=[A-Z])");
		}

		// Extract function/method name from stack trace
		public static string GetFunctionName() {
			StackTrace stackTrace = new StackTrace(true);
			
			// Skip frames: 0=GetFunctionName, 1=GetTrace caller, 2+=actual caller
			for (int i = 2; i < stackTrace.FrameCount; i++) {
				StackFrame frame = stackTrace.GetFrame(i);
				MethodBase method = frame.GetMethod();
				
				if (method != null && method.DeclaringType != null) {
					// Skip Logger namespace internal calls
					if (method.DeclaringType.Namespace == "Logger" || method.DeclaringType.Namespace == "UnityEngine") {
						continue;
					}
					
					return method.Name;
				}
			}
			
			return "";
		}
	}
}
#endif