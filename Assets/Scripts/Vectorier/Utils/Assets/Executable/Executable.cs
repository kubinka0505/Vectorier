using UnityEngine;

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Debug = Logger.Debug;

// -=-=-=- //

#nullable enable

namespace Vectorier.Utils.Assets.Executable {
	public class Verify {
		private readonly string _path;

		public Verify(string path) {
			_path = path;
		}

		public string Path => _path;

		public bool IsLegacy() {
			var company = Details("CompanyName") ?? "";

			return File.Exists(_path) && company.IndexOf("marmalade", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		public bool IsModern() {
			var description = Details("FileDescription");
			var copyright = Details("LegalCopyright");

			bool isUnity = !string.IsNullOrEmpty(copyright) && copyright.IndexOf("unity", StringComparison.OrdinalIgnoreCase) >= 0;
			bool isCrashHandler = !string.IsNullOrEmpty(description) && description.IndexOf("crash handler", StringComparison.OrdinalIgnoreCase) >= 0;

			return !IsLegacy() && isUnity && !isCrashHandler;
		}

		public bool IsEngineless() {
			var version = Details("ProductVersion");

			return !IsLegacy() && !IsModern() && version.Contains("+");
		}

		// -=-=-=- //

		// VERSION.DLL
		[DllImport("version.dll", CharSet = CharSet.Unicode)]
		private static extern bool GetFileVersionInfo(string file, int handle, int size, IntPtr data);

		[DllImport("version.dll", CharSet = CharSet.Unicode)]
		private static extern bool VerQueryValue(IntPtr block, string subBlock, out IntPtr buffer, out uint len);

		[DllImport("version.dll")]
		private static extern int GetFileVersionInfoSize(string file, out int handle);

		// public access
		public string Details(string key, int langIndex = -1) {
			return LoadVersionInfoDynamic(key, langIndex);
		}

		public Dictionary<string, string> DumpAll(int langIndex = -1) {
			var result = new Dictionary<string, string>();

			string[] keys = {
				"ProductName",
				"CompanyName",
				"FileVersion",
				"ProductVersion",
				"FileDescription",
				"InternalName",
				"OriginalFilename",
				"LegalCopyright"
			};

			foreach (var k in keys) {
				var v = LoadVersionInfoDynamic(k, langIndex);

				if (!string.IsNullOrEmpty(v)) {
					result[k] = v;
				}
			}

			return result;
		}

		// core version reader
		private string LoadVersionInfoDynamic(string key, int langIndex) {
			if (!File.Exists(_path)) {
				return "";
			}

			int handle;
			int size = GetFileVersionInfoSize(_path, out handle);

			if (size <= 0) {
				return "";
			}

			IntPtr buffer = Marshal.AllocHGlobal(size);

			try {
				if (!GetFileVersionInfo(_path, 0, size, buffer)) {
					return "";
				}

				if (!VerQueryValue(buffer, "\\VarFileInfo\\Translation", out var langPtr, out var len))
					return "";

				int count = (int)len / 4;
				if (count <= 0) {
					return "";
				}

				int index = langIndex;

				if (index < 0) {
					index = count - 1;
				}

				if (index >= count) {
					index = count - 1;
				}

				ushort lang = (ushort)Marshal.ReadInt16(langPtr, index * 4);
				ushort codepage = (ushort)Marshal.ReadInt16(langPtr, index * 4 + 2);

				string langCode = $"{lang:X4}{codepage:X4}";
				string query = $"\\StringFileInfo\\{langCode}\\{key}";

				if (VerQueryValue(buffer, query, out var ptr, out _)) {
					return Marshal.PtrToStringUni(ptr) ?? "";
				}

				return "";
			} catch (Exception ex) {
				Debug.LogWarning(ex.Message);
				return "";
			} finally {
				Marshal.FreeHGlobal(buffer);
			}
		}
	}

	public class Processes {
		public static void Run(string fileName, params string[] arguments) {
			var startInfo = new ProcessStartInfo {
				FileName = fileName,
				Arguments = string.Join(" ", arguments),
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};

			using var process = new Process { StartInfo = startInfo };

			process.Start();

			string output = process.StandardOutput.ReadToEnd();
			string error = process.StandardError.ReadToEnd();

			process.WaitForExit();

			if (process.ExitCode != 0) {
				Debug.LogError($"Process failed: {fileName}\nArgs: {arguments}\nOutput: {output}\nError: {error}");
			}
		}

		public static List<int> GetPidsByPath(string path) {
			var pids = new List<int>();

			foreach (var process in Process.GetProcesses()) {
				try {
					if (process.HasExited) {
						continue;
					}

					string? exePath = process.MainModule?.FileName;

					if (exePath != null &&
						exePath.Equals(path, StringComparison.OrdinalIgnoreCase)) {
						pids.Add(process.Id);
					}
				} catch {
					// ignore unaccessible processes
				}
			}

			return pids;
		}

		public static void Kill(List<string>? paths = null, List<int>? pids = null) {
			List<int> _pids = new List<int>();

			if (pids == null) {
				if (paths == null) {
					return;
				}

				foreach (var path in paths) {
					_pids.AddRange(
						GetPidsByPath(path)
					);
				}
			} else {
				_pids = pids;
			}

			foreach (int id in _pids) {
				try {
					var process = Process.GetProcessById(id);

					process.Kill();
					process.WaitForExit();
				} catch {
					// ignore conds
				}
			}
		}
	}
}