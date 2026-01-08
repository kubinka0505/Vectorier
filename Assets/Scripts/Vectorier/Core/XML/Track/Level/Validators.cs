using UnityEngine;

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Globalization;
using System.Collections.Generic;

using Debug = Logger.Debug;

// -=-=-=- //

namespace Vectorier.Core.XML.Track.Level {
	public static class Validators {
		public class Models {
			public static float SpawnTime(string modelName, float value) {
				if (value < 0) {
					Debug.LogWarning($"{modelName}: Spawn time cannot be negative ({value}). Defaulting to 0.");
					return 0f;
				}
				return value;
			}

			public static string SpawnName(string modelName, string value) {
				if (string.IsNullOrEmpty(value)) {
					Debug.LogWarning($"{modelName}: Spawn name is empty. Using {modelName}Spawn as spawn name.");

					string newSpawn = modelName ?? "Unknown";
					return newSpawn + "Spawn";
				}

				return value;
			}

			public static float LifeTime(string modelName, float value) {
				if (value <= 0) {
					Debug.LogWarning($"{modelName}: LifeTime is not set or negative ({value}). Defaulting to 1.");
					return 1f;
				}

				return value;
			}

			public static string ModelName(string value) {
				return string.IsNullOrEmpty(value) ? "UnknownModel" : value;
			}
		}

		public static class Track {
			public static class Music {
				public static string File(
					string fileName,
					string directory = null
				) {
					if (
						fileName == null ||
						directory == null
					) {
						return "";
					}

					if (!Directory.Exists(directory)) {
						Debug.LogError("Music directory is invalid");
						return "";
					}

					if (string.IsNullOrEmpty(fileName)) {
						Debug.LogWarning("Music path is empty.");
						return "";
					}

					string pathAbsolute = Path.Combine(
						directory,
						fileName + "." + Vectorier.Core.Game.Extensions.File.Audio.Music
					);

					// todo: change the current function name to accept System.IO entries
					if (!System.IO.File.Exists(pathAbsolute)) {
						Debug.LogError($"Music file does not exist. ({pathAbsolute})");
						return "";
					}

					if (!IsMP3(pathAbsolute)) {
						Debug.LogError("Music file is invalid.");
						return "";
					}

					return fileName;
				}

				public static float Volume(float value) {
					float valueMin = 0.01f;
					float valueMax = 1f;

					float valueClamped = Mathf.Clamp(value, valueMin, valueMax);

					if (value <= 0) {
						Debug.LogError("Music is silent.");
						return 0f;
					}

					if (value != valueClamped) {
						Debug.LogWarning($"Volume value been truncated to range ({valueMin}, {valueMax}).");
					}

					return value;
				}

				private static bool IsMP3(
					string path,

					int bytes = 16384
				) {
					if (!System.IO.File.Exists(path)) {
						return false;
					}

					using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read)) {
						// too small for MP3
						if (fs.Length < 2) {
							Debug.LogError("File too short.");
							return false;
						}

						byte[] header = new byte[3];
						if (fs.Read(header, 0, 3) < 3) {
							Debug.LogError("File too small to contain header.");
							return false;
						}

						if (
							header[0] == 'I' &&
							header[1] == 'D' &&
							header[2] == '3'
						) {
							return true;
						}

						// rewind to start and read first `bytes` bytes to find MP3 frame sync
						fs.Seek(0, SeekOrigin.Begin);
						
						byte[] buffer = new byte[Math.Min(bytes, fs.Length)];
						if (fs.Read(buffer, 0, buffer.Length) < 2) {
							Debug.LogError($"After rewinding {bytes} out of {fs.Length} bytes, format frame sync was not found");
							return false;
						}

						for (int i = 0; i < buffer.Length - 1; i++) {
							// check for frame sync: 0xFF + 0xE?
							if (
								buffer[i] == 0xFF && // 255
								(buffer[i + 1] & 0xE0) == 0xE0 // 224
							) {
								return true;
							}
						}
					}

					Debug.Log("No format frame sync found in first {buffer.Length} bytes of file");
					return false;
				}

			}
		}
	}
}