using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

#nullable enable

// -=-=-=- //

namespace Vectorier.Utils {
	public class Paths {
		private readonly List<string> _paths;
		private readonly string? _basePath;

		// Constructors
		public Paths(string basePath) {
			_basePath = Normalize(basePath ?? string.Empty);
			_paths = new List<string>();
		}

		public Paths(IEnumerable<string> paths) {
			_basePath = null;
			_paths = paths?.ToList() ?? new List<string>();
		}

		// Factory-style helpers
		public static Paths From(string basePath) {
			return new Paths(basePath);
		}

		public static Paths From(IEnumerable<string> paths) {
			return new Paths(paths);
		}

		// Instance API
		public string RelativeTo(string targetPath) {
			if (string.IsNullOrEmpty(_basePath)) {
				throw new InvalidOperationException("Base path not set.");
			}

			string basePath = _basePath!;
			string target = Normalize(targetPath);

			string[] baseDirs = basePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string[] targetDirs = target.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

			int i = 0;

			while (i < baseDirs.Length &&
				i < targetDirs.Length &&
				string.Equals(baseDirs[i], targetDirs[i], StringComparison.OrdinalIgnoreCase))
			{
				i++;
			}

			string[] relativeDirs =
				new string[baseDirs.Length - i + targetDirs.Length - i];

			for (int j = 0; j < baseDirs.Length - i; j++) {
				relativeDirs[j] = "..";
			}

			Array.Copy(
				targetDirs,
				i,
				relativeDirs,
				baseDirs.Length - i,
				targetDirs.Length - i
			);

			return string.Join(Path.DirectorySeparatorChar.ToString(), relativeDirs);
		}

		public string Normalize() {
			if (_basePath == null) {
				throw new Exception("Path null");
			}

			return _basePath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}

		public string Unnormalize() {
			if (_basePath == null) {
				throw new Exception("Path null");
			}

			return _basePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
		}

		public string Top() {
			if (_paths.Count == 0) {
				return string.Empty;
			}

			// Convert all paths to their containing directories first.
			var directories = _paths
				.Select(Normalize)
				.Select(p => Path.GetDirectoryName(p) ?? string.Empty)
				.ToList();

			if (directories.Count == 1) {
				return directories[0];
			}

			var splitPaths = directories
				.Select(p => p.Split(
					new[] {
						Path.DirectorySeparatorChar,
						Path.AltDirectorySeparatorChar
					},
					StringSplitOptions.RemoveEmptyEntries
				))
				.ToList();

			int minLength = splitPaths.Min(p => p.Length);

			var commonParts = new List<string>();

			for (int i = 0; i < minLength; i++) {
				string current = splitPaths[0][i];

				if (splitPaths.All(p =>
					string.Equals(p[i], current, StringComparison.OrdinalIgnoreCase))) {
					commonParts.Add(current);
				} else {
					break;
				}
			}

			if (commonParts.Count == 0) {
				return string.Empty;
			}

			// Preserve root (e.g. "C:")
			string result = string.Join(
				Path.DirectorySeparatorChar.ToString(),
				commonParts
			);

			// Add trailing separator after drive root if needed.
			if (commonParts.Count == 1 &&
				commonParts[0].EndsWith(":", StringComparison.Ordinal)) {
				result += Path.DirectorySeparatorChar;
			}

			return result;
		}

		// Helpers
		private static string Normalize(string path) {
			if (string.IsNullOrEmpty(path)) {
				return string.Empty;
			}

			return Path.GetFullPath(path)
				.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}

		public static bool SafeCopy(
			string src,
			string dst
		) {
			string dn = Path.GetDirectoryName(dst);
			if (!string.IsNullOrEmpty(dn)) {
				Directory.CreateDirectory(dn);
			}

			if (!File.Exists(src)) {
				return false;
			}

			if (!File.Exists(dst)) {
				File.Copy(src, dst, true);
				return true;
			}

			var srcHash = AssetCache.ComputeMD5(src);
			var dstHash = AssetCache.ComputeMD5(dst);

			bool equal = srcHash == dstHash;

			if (equal) {
				return false;
			}

			File.Copy(src, dst, true);
			return true;
		}
	}
}