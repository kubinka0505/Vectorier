using UnityEditor;
using UnityEngine;

using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

using Debug = Logger.Debug;

#nullable enable

// -=-=-=- //

namespace Vectorier.Utils.Assets.Audio {
	class Internals {
		public int GetSampleRate(string filePath) {
			using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
			using (var br = new BinaryReader(fs)) {
				// RIFF header
				if (br.ReadUInt32() != 0x46464952) {
					// "RIFF"
					throw new InvalidDataException("Not a valid RIFF file.");
				}

				fs.Seek(4, SeekOrigin.Current); // skip RIFF chunk size

				if (br.ReadUInt32() != 0x45564157) {
					// "WAVE"
					throw new InvalidDataException("Not a valid WAVE file.");
				}

				// Scan chunks
				while (fs.Position + 8 <= fs.Length) {
					uint chunkId   = br.ReadUInt32();
					uint chunkSize = br.ReadUInt32();

					// Validate chunk size
					if (fs.Position + chunkSize > fs.Length) {
						throw new InvalidDataException("Chunk size extends beyond end of file.");
					}

					// "fmt " chunk
					if (chunkId == 0x20746D66) {
						if (chunkSize < 16) {
							throw new InvalidDataException("fmt chunk too small.");
						}

						ushort audioFormat = br.ReadUInt16();
						ushort numChannels = br.ReadUInt16();
						int sampleRate     = br.ReadInt32();

						return sampleRate;
					} else {
						// Skip chunk payload
						fs.Seek(chunkSize, SeekOrigin.Current);

						// If chunk size is odd, skip pad byte (WAV padding rule)
						if ((chunkSize & 1) == 1 && fs.Position < fs.Length) {
							fs.Seek(1, SeekOrigin.Current);
						}
					}
				}
			}

			throw new InvalidDataException("fmt chunk not found in WAV file.");
		}

		public static void ProcessAudioFiles(
			IEnumerable<string> files,
			string directory,

			string package,

			int targetRate = 22050,
			object? targetChannels = null,

			IEnumerable<string>? excludedFiles = null,
			IEnumerable<string>? silentFiles = null,

			bool swapChannels = false,
			bool invertPolarity = false,

			bool rebuild = true,
			bool optimize = true
		) {
			// reset output directory
			if (Directory.Exists(directory)) {
				Directory.Delete(directory, true);
			}
			Directory.CreateDirectory(directory);

			string baseRoot = Vectorier.Utils.Paths.From(files).Top();

			string Normalize(string path)
				=> Vectorier.Utils.Paths.From(baseRoot).RelativeTo(path)
					.Replace('\\', '/');

			HashSet<string> excludedSet = excludedFiles != null
				? new HashSet<string>(
					excludedFiles.Select(Normalize),
					StringComparer.OrdinalIgnoreCase)
				: new HashSet<string>();

			HashSet<string> silentSet = silentFiles != null
				? new HashSet<string>(
					silentFiles.Select(Normalize),
					StringComparer.OrdinalIgnoreCase)
				: new HashSet<string>();

			SoundManager.ChannelTypes resolvedChannels = SoundManager.ChannelTypes.Original;

			if (targetChannels is int intVal) {
				resolvedChannels = intVal switch {
					1 => SoundManager.ChannelTypes.MonoCombined,
					2 => SoundManager.ChannelTypes.Stereo,
					_ => SoundManager.ChannelTypes.Original
				};
			} else if (targetChannels is SoundManager.ChannelTypes chanVal) {
				resolvedChannels = chanVal;
			}

			// -------------------------
			// COPY PHASE (RELATIVE PATHS)
			// -------------------------
			foreach (var file in files) {
				string filePath = Normalize(file);

				try {
					if (Path.GetFileName(filePath).StartsWith(".")) {
						continue;
					}

					if (excludedSet.Contains(filePath)) {
						continue;
					}

					string destPath = Path.Combine(directory, filePath);

					Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

					if (silentSet.Contains(filePath)) {
						CreateSilentAudioWave(destPath, 1, 1);
						continue;
					}

					if (!File.Exists(destPath) || rebuild) {
						Vectorier.Utils.Paths.SafeCopy(file, destPath);
					}
				}
				catch (Exception ex) {
					Debug.LogError($"Copy stage failed for {filePath}: {ex}");
				}
			}

			// -------------------------
			// PROCESS PHASE
			// -------------------------
			var wavFiles = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);

			Parallel.ForEach(wavFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, file => {
				// FIX: convert to SAME RELATIVE FORMAT as sets
				string relativeFile = file.Substring(directory.Length)
					.TrimStart('\\', '/')
					.Replace('\\', '/');

				if (silentSet.Contains(relativeFile)) {
					return;
				}

				string dir = Path.GetDirectoryName(file)!;
				string name = Path.GetFileNameWithoutExtension(file);
				string ext = Path.GetExtension(file);

				string tempOut = Path.Combine(dir, $"{name}_temp{ext}");

				var filters = new List<string> {
					$"aresample={targetRate}:cutoff=1"
				};

				if (targetRate != Vectorier.Core.Game.Properties.Audio.Sound.SampleRate) {
					filters.Add($"asetrate={targetRate}");
				}

				switch (resolvedChannels) {
					case SoundManager.ChannelTypes.MonoLeft:
						filters.Add("pan=mono|c0=FL");
						break;
					case SoundManager.ChannelTypes.MonoRight:
						filters.Add("pan=mono|c0=FR");
						break;
					case SoundManager.ChannelTypes.MonoCombined:
						filters.Add("pan=mono|c0=0.5*c0+0.5*c1");
						break;
					case SoundManager.ChannelTypes.Stereo:
						break;
				}

				if (invertPolarity) {
					filters.Add("volume=-1");
				}

				if (swapChannels) {
					filters.Add("pan=stereo|c0=c1|c1=c0");
				}

				string filterStr = string.Join(",", filters);

				string optStr = optimize ? "" : "-map 0 -map_metadata 0:s:0";
				string chanStr = filterStr.Contains("mono") ? "-ac 1" : "";

				string[] args = {
					"-y",
					"-loglevel", "8",
					"-hide_banner",
					"-stats",
					"-i", file,
					"-af", filterStr,
					chanStr,
					optStr,
					tempOut
				};

				Vectorier.Utils.Assets.Executable.Processes.Run(package, args);

				if (!File.Exists(tempOut)) {
					throw new Exception("Package did not produce output file.");
				}

				File.Delete(file);
				File.Move(tempOut, file);
			});
		}

		private static void CreateSilentAudioWave(string path, int sampleRate, int channels) {
			using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write)) {
				using (var bw = new BinaryWriter(fs)) {
					int bitsPerSample = 8;
					int byteRate = sampleRate * channels * bitsPerSample / 8;
					short blockAlign = (short)(channels * bitsPerSample / 8);
					int dataLength = channels * 1; // 1 sample, 8-bit

					// RIFF header
					bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
					bw.Write(36 + dataLength);
					bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

					// fmt chunk
					bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
					bw.Write(16);
					bw.Write((short)1); // PCM
					bw.Write((short)channels);
					bw.Write(sampleRate);
					bw.Write(byteRate);
					bw.Write(blockAlign);
					bw.Write((short)bitsPerSample);

					// data chunk
					bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
					bw.Write(dataLength);

					// write silence (0x80 for 8-bit unsigned PCM)
					for (int i = 0; i < channels; i++) {
						bw.Write((byte)128);
					}
				}
			}
		}
	}
}