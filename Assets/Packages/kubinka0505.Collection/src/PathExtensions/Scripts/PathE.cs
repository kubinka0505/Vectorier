using System;
using System.Reflection;
using System.Diagnostics;

using UnityEditor;
using UnityEngine;

using Debug = UnityEngine.Debug;

// -=-=-=- //

namespace PathE {
    public static class PathUtils {
		public static char sep = System.IO.Path.DirectorySeparatorChar;

		private static string _dataPath;
		public static string dataPath => _dataPath ??= System.IO.Directory.GetParent(Application.dataPath).FullName;

		public static string NormPath(string path) {
			return path.Replace(sep, '/');
		}

		public static string Relative(string file, string dir) {
			string path = NormPath(file);
			dir = NormPath(dir);

			if (!path.StartsWith(dir)) {
				// fallback: return normalized path as-is
				return path;
			}

			int startIndex = dir.Length;

			// remove leading slash if present
			if (path.Length > startIndex && (path[startIndex] == '/' || path[startIndex] == '\\')) {
				startIndex++;
			}

			return path.Substring(startIndex);
		}

		public static (string file, int line) GetTrace() {
			string filePath = "unknown";
			int lineNumber = 0;

			try {
				var trace = new StackTrace(true);

				for (int i = 0; i < trace.FrameCount; i++) {
					var frame = trace.GetFrame(i);
					var method = frame.GetMethod();
					if (method == null) {
						continue;
					}

					var declaringType = method.DeclaringType;
					if (declaringType == null) {
						continue;
					}

					string ns = declaringType.Namespace ?? "";
					string className = declaringType.Name;

					// skip Unity/editor frames
					if (ns.StartsWith("UnityEngine") || ns.StartsWith("UnityEditor")) {
						continue;
					}

					// skip logger/util package frames
					if (ns.StartsWith("Logger") || ns.StartsWith("PathE")) {
						continue;
					}

					string candidatePath = frame.GetFileName();
					if (!string.IsNullOrEmpty(candidatePath)) {
						filePath = candidatePath;
						lineNumber = frame.GetFileLineNumber();
						break;
					}
				}

				// fallback: take first frame with a file
				if ((filePath == "unknown" || string.IsNullOrEmpty(filePath)) && trace.FrameCount > 0) {
					foreach (var frame in trace.GetFrames()) {
						string candidate = frame.GetFileName();

						if (!string.IsNullOrEmpty(candidate)) {
							filePath = candidate;
							lineNumber = frame.GetFileLineNumber();
							break;
						}
					}
				}
			}
			catch {
				// ignore
			}

			return (filePath, lineNumber);
		}
	}
}