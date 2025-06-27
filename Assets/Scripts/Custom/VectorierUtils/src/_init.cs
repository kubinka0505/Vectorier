using System;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using UnityEditor;

using Debug = UnityEngine.Debug;

// -=-=-=- //

public static class VectorierMenu {
	[MenuItem("Vectorier/⚙ Utils/", true, 1000)]
	private static void Utils() {
		// placeholder for name
	}
}

// -=-=-=- //
// Functions

namespace VectorierUtils {
	public static class Utils {

		public static void AdvancedLog(string title = "Information", string msg = "", string buttonText = "OK", string logType = "info", bool logOnly = false) {
			// Define func as a delegate
			Action<string> func = Debug.Log;

			// Convert to lowercase for comparisons
			string titleLower = title.ToLower();
			logType = logType.ToLower();

			// Determine based on title
			if (titleLower == "") {
				func = Debug.Log;
			} else if (titleLower.StartsWith("warn")) {
				func = Debug.LogWarning;
			} else if (titleLower.StartsWith("err")) {
				func = Debug.LogError;
			}

			if (logType.StartsWith("info")) {
				func = Debug.Log;
			} else if (logType.StartsWith("warn")) {
				func = Debug.LogWarning;
			} else if (logType.StartsWith("err")) {
				func = Debug.LogError;
			}

			// Show a dialog if not logOnly
			if (!logOnly) {
				EditorUtility.DisplayDialog(title, msg, buttonText);
			}

			// Log the message
			func(msg);
		}

		public static string GetScriptPath() {
			StackTrace stackTrace = new StackTrace(true);

			// Find the frame containing this method
			StackFrame frame = null;
			for (int i = 0; i < stackTrace.FrameCount; i++) {
				if (stackTrace.GetFrame(i).GetMethod() == MethodBase.GetCurrentMethod()) {
					// Get the frame where this method was called
					frame = stackTrace.GetFrame(i + 1);
					break;
				}
			}

			return frame.GetFileName();
		}
	}

	public class String {
		public static string GetRelativePath(string fromPath, string toPath) {
			Uri fromUri = new Uri(fromPath);
			Uri toUri = new Uri(toPath);

			Uri relativeUri = fromUri.MakeRelativeUri(toUri);
			string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

			return relativePath;
		}
	}
}