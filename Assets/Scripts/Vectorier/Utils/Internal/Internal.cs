using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

#nullable enable

// -=-=-=- //

namespace Vectorier.Utils.Internal {
	class SceneUtil {
		public static string[] GetPath() {
			var active = SceneManager.GetActiveScene();

			string path = active.path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

			var parts = path.Split(new[] { Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

			if (parts.Length == 0) {
				return new[] { string.Empty, active.name + Path.GetExtension(path) };
			}

			// if there is only filename (no folder)
			if (parts.Length == 1) {
				return new[] { string.Empty, active.name + Path.GetExtension(path) };
			}

			// drop first folder segment dynamically
			string dir = string.Join(Path.AltDirectorySeparatorChar.ToString(), parts.Skip(1).Take(parts.Length - 2));
			if (parts.Length == 2) {
				dir = string.Empty;
			}

			string file = active.name + Path.GetExtension(path);

			return new[] { dir, file };
		}
	}

	class Script {
		public static string GetPath() {
			StackTrace stackTrace = new StackTrace(true);

			StackFrame? frame = null;

			for (int i = 0; i < stackTrace.FrameCount; i++) {
				var currentFrame = stackTrace.GetFrame(i);

				if (currentFrame?.GetMethod() == MethodBase.GetCurrentMethod()) {
					frame = stackTrace.GetFrame(i + 1);
					break;
				}
			}

			if (frame == null) {
				throw new Exception("Frame not found");
			}

			return frame.GetFileName();
		}
	}

	class Package {
		public static string? GetPath(string package, string? envVar = "PATH") {
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
			return candidates.OrderByDescending(path => {
				try {
					var dir = Path.GetDirectoryName(path);

					if (dir == null) {
						return DateTime.MinValue;
					}

					return Directory.GetLastWriteTimeUtc(dir);
				} catch {
					return DateTime.MinValue;
				}
			}).First();
		}
	}
}