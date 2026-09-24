using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Debug = Logger.Debug;

#nullable enable
#pragma warning disable CS8602 // nullables

// -=-=-=- //

namespace Vectorier.Track.Compile {	
	class AssetMover {
		private class ArchiveResult {
			public string Name;
			public long TimeMs;

			public ArchiveResult(string name, long timeMs) {
				Name = name;
				TimeMs = timeMs;
			}
		}

		public static string? projectTempRoot = Path.Combine(Path.GetTempPath(), "Unity", "Projects", "_Vectorier");

		public static readonly string gameDirectoryPathLegacy = Path.GetFullPath(Vectorier.Settings.Core.GameDirectoryLegacy);
		public static readonly string gameDirectoryPathModern = Path.GetFullPath(Vectorier.Settings.Core.GameDirectoryModern);

		public static readonly string gameExecutablePathLegacy = Path.GetFullPath(Vectorier.Settings.Core.GameExecutableLegacy);
		public static readonly string gameExecutablePathModern = Path.GetFullPath(Vectorier.Settings.Core.GameExecutableModern);

		public static Dictionary<string, string> Move(
			GameObject[] gos,
			string compressionAlgorithm,
			string? modernAssetsPath = null,
			bool legacy = false
		) {
			string dataPath = Application.dataPath.Replace(
				Path.AltDirectorySeparatorChar,
				Path.DirectorySeparatorChar
			);

			string XmlDir = "XML";
			string XmlDzipDir = Path.Combine(XmlDir, "dzip");

			// Uncompiled XML directories
			string XmlDzipTexturesBaseDir = Path.Combine("Resources", "Textures");

			string dirXml = Path.Combine(dataPath, "XML");
			string dirRoot = Path.Combine(dirXml, "dzip");
			string dirCompilators = Path.Combine(dirRoot, "_compilators");
			string dirArchivesCompiled = Path.Combine(dirRoot, "archives", ".compiled");
			string dirArchivesUncompiled = Path.Combine(dirRoot, "archives", "uncompiled");
			string XmlDzipLvlDir = Path.Combine(dirArchivesUncompiled, "level_xml");
			string XmlDzipSoundDir = Path.Combine(dirArchivesUncompiled, "sound");
			string XmlDzipAnimationsDir = Path.Combine(dirArchivesUncompiled, "animations");

			// Assets prefix is mandatory for directories that are NOT built on fly
			string XmlCommonDir = Path.Combine("Assets", "XML", "dzip", "archives", "uncompiled", ".common_xml");
			string XmlGui2048Dir = Path.Combine("Assets", "XML", "dzip", "archives", "uncompiled", ".GUI_2048_1536");

			string pathListPayed = Path.Combine(XmlCommonDir, "List_Payed" + "." + Vectorier.Core.Game.Properties.Extensions.File.XML);
			string pathLocalization = Path.Combine(XmlCommonDir, "localization_all" + "." + Vectorier.Core.Game.Properties.Extensions.File.XML);


			if (modernAssetsPath == null) {
				legacy = true;
			}

			var buildMap = GameObject.FindObjectOfType<BuildMap>();
			var result = new Dictionary<string, string>();

			List<int> gameProcesses = Vectorier.Utils.Assets.Executable.Processes.GetPidsByPath(
				legacy ? gameExecutablePathLegacy : gameExecutablePathModern
			);

			string pathThumbnail = Path.Combine(
				dataPath,
				Vectorier.Utils.Internal.SceneUtil.GetPath()[0],
				buildMap.thumbnailImagePath
			);

			string pathLevelAbsolute = Path.Combine(dataPath, XmlDzipLvlDir);

			string pathMoves = Path.Combine(
				gameDirectoryPathLegacy,
				"Moves_new." + Vectorier.Core.Game.Properties.Extensions.File.XML
			);

			// LEGACY DIRS
			string pathLevel = Path.Combine(
				pathLevelAbsolute,
				buildMap.mapToOverride + "." + Vectorier.Core.Game.Properties.Extensions.File.XML
			);

			string pathMusicFull = Path.Combine(
				gameDirectoryPathLegacy,
				buildMap.levelMusic + "." + Vectorier.Core.Game.Properties.Extensions.File.Audio.Music
			);

			string dirExecutables = Path.Combine(dirRoot, "executable");

			// =========================================================
			// PHASE 1: COLLECT XML DOCS
			// =========================================================

			List<string> xmlAll = Directory.GetFiles(
				Path.Combine(dataPath, XmlDzipLvlDir),
				"*." + Vectorier.Core.Game.Properties.Extensions.File.XML,
				SearchOption.AllDirectories
			).ToList();

			xmlAll.Add(pathMoves);

			var sw = System.Diagnostics.Stopwatch.StartNew();

			// =========================================================
			// PHASE 2: BUILD DEPENDENCY GRAPH
			// =========================================================

			var dependencies = new {
				Skins = new HashSet<string>(),
				Objects = new HashSet<string>(),
				Sets = new HashSet<string>(),
				Tracks = new HashSet<string>(),

				Animations = new HashSet<string>(),
				ImagesStatic = new HashSet<string>(),
				Models = new HashSet<string>(),
				Sounds = new HashSet<string>()
			};

			dependencies.Sounds.Add("cash_register");
			dependencies.Sounds.Add("trick_activate");
			dependencies.Sounds.Add("ui_button_big_release");
			dependencies.Sounds.Add("ui_button_big_toggle");
			dependencies.Sounds.Add("ui_button_round_toggle");
			dependencies.Sounds.Add("ui_button_square_toggle");
			dependencies.Sounds.Add("ui_click");
			dependencies.Sounds.Add("ui_window_options");
			dependencies.Sounds.Add("ui_window_profile");
			dependencies.Sounds.Add("ui_window_shop");

			dependencies.Skins.Add("1");

			foreach (var file in xmlAll) {
				// main perpetrator for long compilation time
				//
				// game itself has no idea what xml files
				// it needs so it has to be that way
				var doc = new XmlDocument();
				doc.Load(file);

				var isolator = new Vectorier.Utils.Assets.Management.Isolator(doc);

				if (Vectorier.Core.Components.Model.Validators.IsSkin(doc)) {
					dependencies.Skins.Add(file);
				}

				if (Vectorier.Core.Components.Model.Validators.IsObject(doc)) {
					dependencies.Objects.Add(file);
				}

				bool isSet = Vectorier.Core.XML.Utils.IsSet(doc);
				bool isTrack = Vectorier.Core.XML.Utils.IsTrack(doc);
				bool isMoves = Vectorier.Core.XML.Utils.IsMoves(doc);
				bool isTemplate = Vectorier.Core.XML.Utils.IsTemplate(doc);

				if (isSet || isTemplate) {
					dependencies.Sets.Add(file);
				}

				if (isTrack) {
					dependencies.Tracks.Add(file);
				}

				if (isSet || isTrack) {
					dependencies.ImagesStatic.UnionWith(isolator.ImagesStatic());
					dependencies.Models.UnionWith(isolator.Models(writeGears: true));
				}

				if (isSet || isTrack || isMoves) {
					dependencies.Sounds.UnionWith(isolator.Sounds());
				}

				if (isMoves) {
					dependencies.Animations.UnionWith(isolator.Animations());
				}
			}

			// RESOLVE

			HashSet<string> resolvedModels = new HashSet<string>(
				dependencies.Models.Select(x => {
					string normalized = x
						.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
						.TrimStart(Path.DirectorySeparatorChar);

					return Path.Combine(
						pathLevelAbsolute,
						normalized + "." + Vectorier.Core.Game.Properties.Extensions.File.XML
					);
				})
			);
			// replace contents instead of property
			dependencies.Models.Clear();
			dependencies.Models.UnionWith(resolvedModels);

			var xmlSets = dependencies.Sets.ToList();
			var xmlTracks = dependencies.Tracks.ToList();
			var xmlObjects = dependencies.Objects.ToList();

			// =========================================================
			// PHASE 3: RESOLVE FILESYSTEM PATHS
			// =========================================================

			HashSet<string> activeAnimations = new HashSet<string>();
			HashSet<string> activeImagesStatic = new HashSet<string>();
			HashSet<string> activeModels = new HashSet<string>();
			HashSet<string> activeSounds = new HashSet<string>();

			// =========================================================
			// ANIMATIONS
			// =========================================================

			foreach (var entry in dependencies.Animations) {
				string name = entry.Replace(
					Path.AltDirectorySeparatorChar,
					Path.DirectorySeparatorChar
				);

				string filePath = Path.Combine(
					dataPath,
					XmlDzipAnimationsDir,
					name + "." + Vectorier.Core.Game.Properties.Extensions.File.Animation
				);

				if (File.Exists(filePath)) {
					activeAnimations.Add(filePath);
				}
			}

			string? ResolveImage(string basePath, string entry, string[] exts) {
				entry = entry.Replace(
					Path.AltDirectorySeparatorChar,
					Path.DirectorySeparatorChar
				);

				void TryAddAtlas(string filePath){
					string atlasExt = Vectorier.Core.Game.Properties.Extensions.File.Image.Atlas;
					string atlas = Path.ChangeExtension(filePath, atlasExt);

					if (File.Exists(atlas)) {
						activeImagesStatic.Add(atlas);
					}
				}

				// local helper to avoid duplication
				bool Try(string path, out string? result) {
					foreach (var ext in exts) {
						result = Path.Combine(path, entry + "." + ext);

						if (File.Exists(result)) {
							TryAddAtlas(result);
							return true;
						}
					}

					result = null;
					return false;
				}

				// 1. full path
				if (Try(basePath, out var found)) {
					return found;
				}

				// 2. fallback flat
				string name = Path.GetFileNameWithoutExtension(entry);

				if (Try(basePath, out found)) {
					return found;
				}

				return null;
			}

			// =========================================================
			// STATIC IMAGES
			// =========================================================
			string staticImagesBasePath = Path.Combine(dataPath, XmlDzipTexturesBaseDir);

			foreach (var entry in dependencies.ImagesStatic) {
				string? found = ResolveImage(
					staticImagesBasePath,
					entry,
					Vectorier.Core.Game.Properties.Extensions.File.Image.Static
				);

				if (found != null) {
					activeImagesStatic.Add(found);
				}
			}

			// =========================================================
			// Models

			foreach (var entry in dependencies.Models) {
				string filePath = entry.Replace(
					Path.AltDirectorySeparatorChar,
					Path.DirectorySeparatorChar
				);

				if (File.Exists(filePath)) {
					activeModels.Add(filePath);
				}
			}

			// =========================================================
			// SOUNDS
			// =========================================================

			foreach (var entry in dependencies.Sounds) {
				string name = entry.Replace(
					Path.AltDirectorySeparatorChar,
					Path.DirectorySeparatorChar
				);

				string filePath = Path.Combine(
					dataPath,
					XmlDzipSoundDir,
					name + "." + Vectorier.Core.Game.Properties.Extensions.File.Audio.Sound
				);

				if (File.Exists(filePath)) {
					activeSounds.Add(filePath);
				}
			}

			List<ArchiveResult> messages = new List<ArchiveResult>();

			Debug.Log($"{sw.ElapsedMilliseconds}ms");

			// =========================================================
			// VALUES CHECKER
			// =========================================================

			/*
			void CheckSet(string name, IEnumerable<string> set, string target) {
				foreach (var f in set) {
					var norm = f.Replace(
						Path.AltDirectorySeparatorChar,
						Path.DirectorySeparatorChar
					);

					if (norm.EndsWith(target, StringComparison.OrdinalIgnoreCase)) {
						Debug.Log($"Found in {name}: {f}");
					}
				}
			}
			*/

			// -=-=-=- //
			// COPY ANIMATIONS
			string animationsName = "animations";
			string dirAnimationsAssets = Path.Combine(animationsName, "data");

			if (legacy) {
				string fileAnimationsBatch = Path.Combine(
					dirCompilators,
					compressionAlgorithm == "dz" ? "standard" : "optimized",
					"build",
					animationsName.Trim('.') + ".bat"
				);

				string fileAnimationsConfig = Path.Combine(
					dirCompilators,
					compressionAlgorithm == "dz" ? "standard" : "optimized",
					"_config",
					animationsName.Trim('.') + "." + Vectorier.Core.Game.Properties.Extensions.File.Archive.Config
				);

				string fileAnimationsArchiveDestination = Path.Combine(
					gameDirectoryPathLegacy,
					animationsName + "." + Vectorier.Core.Game.Properties.Extensions.File.Archive.Compiled
				);

				string fileAnimationsArchiveSource = Path.Combine(
					dirArchivesCompiled,
					Path.GetFileName(fileAnimationsArchiveDestination)
				);

				string dirAnimationsFull = Path.Combine(
					dataPath,
					dirArchivesUncompiled,
					animationsName
				);

				WriteDclConfig(
					config: fileAnimationsConfig,
					archive: fileAnimationsArchiveSource,
					basedir: dirAnimationsFull,
					files: activeAnimations,
					index: 0,
					algorithm: compressionAlgorithm
				);

				var compileResult = CompileAndCopy(
					launcher: fileAnimationsBatch,
					archiveSource: fileAnimationsArchiveSource,
					archiveDestination: fileAnimationsArchiveDestination,
					processesToKill: gameProcesses
				);

				messages.Add(compileResult);
			} else {
				// IMPORTANT: avoid Parallel.ForEach if Vectorier.Utils.Paths.SafeCopy touches shared cache state heavily
				foreach (var file in activeAnimations) {
					string dst = Path.Combine(
						modernAssetsPath,
						dirAnimationsAssets,
						Path.GetFileName(file)
					);

					Vectorier.Utils.Paths.SafeCopy(file, dst);
				}

				string movesDst = Path.Combine(
					modernAssetsPath,
					"animations",
					"moves" + Path.GetExtension(pathMoves)
				);

				Vectorier.Utils.Paths.SafeCopy(pathMoves, movesDst);
			}

			// -=-=-=- //
			// COPY LEVEL TRICKS & LEVEL NAME
			string commonsName = ".common_xml";
			string dirCommonsAssets = "commons";

			if (legacy) {
				string fileCommonsBatch = Path.Combine(
					dirCompilators,
					compressionAlgorithm == "dz" ? "standard" : "optimized",
					"build",
					commonsName.Trim('.') + ".bat"
				);

				string fileCommonsConfig = Path.Combine(
					dirCompilators,
					compressionAlgorithm == "dz" ? "standard" : "optimized",
					"_config",
					commonsName.Trim('.') + "." + Vectorier.Core.Game.Properties.Extensions.File.Archive.Config
				);

				string fileCommonsArchiveDestination = Path.Combine(
					gameDirectoryPathLegacy,
					commonsName.Trim('.') + "." + Vectorier.Core.Game.Properties.Extensions.File.Archive.Compiled
				);

				string fileCommonsArchiveSource = Path.Combine(
					dirArchivesCompiled,
					Path.GetFileName(fileCommonsArchiveDestination)
				);

				string dirCommonsFull = Path.Combine(
					dataPath,
					dirArchivesUncompiled,
					commonsName
				);

				var commonsFiles = Directory.GetFiles(
					dirCommonsFull,
					"*",
					SearchOption.AllDirectories
				);

				WriteDclConfig(
					config: fileCommonsConfig,
					archive: fileCommonsArchiveSource,
					basedir: dirCommonsFull,
					files: new HashSet<string>(commonsFiles),
					index: 0,
					algorithm: compressionAlgorithm
				);

				var compileResult = CompileAndCopy(
					launcher: fileCommonsBatch,
					archiveSource: fileCommonsArchiveSource,
					archiveDestination: fileCommonsArchiveDestination,
					processesToKill: gameProcesses
				);

				messages.Add(compileResult);
			} else {
				Vectorier.Utils.Paths.SafeCopy(
					pathListPayed,
					Path.Combine(modernAssetsPath, dirCommonsAssets, Path.GetFileName(pathListPayed))
				);

				Vectorier.Utils.Paths.SafeCopy(
					pathLocalization,
					Path.Combine(modernAssetsPath, "localization", Path.GetFileName(pathLocalization))
				);
			}

			// =========================================================
			// COPY LEVEL ICONS / THUMBNAIL
			// =========================================================

			string iconsName = ".GUI_2048_1536";
			string dirIconsAssets = Path.Combine("icons", "locations");

			if (legacy) {
				string fileIconsBatch = Path.Combine(
					dirCompilators,
					compressionAlgorithm == "dz" ? "standard" : "optimized",
					"build",
					iconsName.Trim('.') + ".bat"
				);

				string fileIconsConfig = Path.Combine(
					dirCompilators,
					compressionAlgorithm == "dz" ? "standard" : "optimized",
					"_config",
					iconsName.Trim('.') + "." + Vectorier.Core.Game.Properties.Extensions.File.Archive.Config
				);

				string fileIconsArchiveDestination = Path.Combine(
					gameDirectoryPathLegacy,
					iconsName.Trim('.') + "." + Vectorier.Core.Game.Properties.Extensions.File.Archive.Compiled
				);

				string fileIconsArchiveSource = Path.Combine(
					dirArchivesCompiled,
					Path.GetFileName(fileIconsArchiveDestination)
				);

				string dirIconsFull = Path.Combine(
					dataPath,
					dirArchivesUncompiled,
					iconsName
				);

				var iconFiles = Directory.GetFiles(
					dirIconsFull,
					"*",
					SearchOption.AllDirectories
				);

				WriteDclConfig(
					config: fileIconsConfig,
					archive: fileIconsArchiveSource,
					basedir: dirIconsFull,
					files: new HashSet<string>(iconFiles),
					index: 0,
					algorithm: compressionAlgorithm
				);

				var compileResult = CompileAndCopy(
					launcher: fileIconsBatch,
					archiveSource: fileIconsArchiveSource,
					archiveDestination: fileIconsArchiveDestination,
					processesToKill: gameProcesses
				);

				messages.Add(compileResult);
			} else {
				if (File.Exists(pathThumbnail)) {
					Vectorier.Utils.Paths.SafeCopy(
						pathThumbnail,
						Path.Combine(modernAssetsPath, dirIconsAssets, Path.GetFileName(pathThumbnail))
					);
				}
			}

			// sw_t.Stop();
			// Debug.Log("thumb: " + sw_t.Elapsed.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture));
			string xmlRootName = "level_xml";

			if (legacy) {
				string fileXmlRootBatch = Path.Combine(
					dirCompilators,
					compressionAlgorithm == "dz" ? "standard" : "optimized",
					"build",
					xmlRootName.Trim('.') + ".bat"
				);

				string fileXmlRootConfig = Path.Combine(
					dirCompilators,
					compressionAlgorithm == "dz" ? "standard" : "optimized",
					"_config",
					xmlRootName.Trim('.') + "." + Vectorier.Core.Game.Properties.Extensions.File.Archive.Config
				);

				string fileXmlRootArchiveDestination = Path.Combine(
					gameDirectoryPathLegacy,
					xmlRootName.Trim('.') + "." + Vectorier.Core.Game.Properties.Extensions.File.Archive.Compiled
				);

				string fileXmlRootArchiveSource = Path.Combine(
					dirArchivesCompiled,
					Path.GetFileName(fileXmlRootArchiveDestination)
				);

				string dirXmlRootFull = Path.Combine(
					dataPath,
					dirArchivesUncompiled,
					xmlRootName
				);

				var uniqued = new HashSet<string>(
					activeModels
						.Concat(xmlSets)
						.Concat(xmlTracks)
						.Concat(xmlObjects)
				);

				WriteDclConfig(
					config: fileXmlRootConfig,
					archive: fileXmlRootArchiveSource,
					basedir: dirXmlRootFull,
					files: uniqued,
					index: 0,
					algorithm: compressionAlgorithm
				);

				var compileResult = CompileAndCopy(
					launcher: fileXmlRootBatch,
					archiveSource: fileXmlRootArchiveSource,
					archiveDestination: fileXmlRootArchiveDestination,
					processesToKill: gameProcesses
				);

				messages.Add(compileResult);
			} else {
				foreach (var file in activeModels) {
					string relative = Vectorier.Utils.Paths.From(
						Vectorier.Utils.Paths.From(xmlTracks).Top()
					).RelativeTo(file);
					relative = relative.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

					string dst = Path.Combine(
						modernAssetsPath,
						"models",
						relative
					);

					Vectorier.Utils.Paths.SafeCopy(file, dst);
				}

				Vectorier.Utils.Paths.SafeCopy(
					pathLevel,
					Path.Combine(
						modernAssetsPath,
						"xmlroot",
						"levels",
						Path.GetFileName(pathLevel)
					)
				);

				foreach (var file in xmlSets) {
					string relative = Vectorier.Utils.Paths.From(
						Vectorier.Utils.Paths.From(xmlTracks).Top()
					).RelativeTo(file);
					relative = relative.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

					Vectorier.Utils.Paths.SafeCopy(
						file,
						Path.Combine(
							modernAssetsPath,
							"xmlroot",
							relative
						)
					);
				}

				foreach (var file in xmlTracks) {
					string relative = Vectorier.Utils.Paths.From(
						Vectorier.Utils.Paths.From(xmlTracks).Top()
					).RelativeTo(file);
					relative = relative.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

					Vectorier.Utils.Paths.SafeCopy(
						file,
						Path.Combine(
							modernAssetsPath,
							"xmlroot",
							"levels",
							relative
						)
					);
				}
			}

			// =========================================================
			// MUSIC
			// =========================================================

			if (!legacy) {
				string relative = Vectorier.Utils.Paths.From(gameDirectoryPathLegacy).RelativeTo(pathMusicFull);

				Vectorier.Utils.Paths.SafeCopy(
					pathMusicFull,
					Path.Combine(modernAssetsPath, "music", relative)
				);
			}

			// =========================================================
			// SOUND PROCESSING
			// =========================================================

			string soundsName = "sound";
			string tempSoundDir = Path.Combine(projectTempRoot, "sound");

			var soundManager = GameObject.FindObjectOfType<SoundManager>();
			bool soundManagerActive = soundManager != null && soundManager.enabled;

			string pkg = "ffmpeg";

			int rate = Vectorier.Core.Game.Properties.Audio.Sound.SampleRate;
			int chans = Vectorier.Core.Game.Properties.Audio.Sound.Channels;

			if (!legacy) {
				rate = soundManager.SampleRate;
				chans = soundManager.Channel == SoundManager.ChannelTypes.MonoLeft ||
						soundManager.Channel == SoundManager.ChannelTypes.MonoCombined ||
						soundManager.Channel == SoundManager.ChannelTypes.MonoRight
						? 2
						: (int)soundManager.Channel;

				if (rate < 44100) {
					rate = 44100;
				}
			}

			Vectorier.Utils.Assets.Audio.Internals.ProcessAudioFiles(
				files: activeSounds,
				directory: legacy ? tempSoundDir : Path.Combine(modernAssetsPath, "sounds"),
				package: Vectorier.Utils.Internal.Package.GetPath(pkg) ?? Path.Combine(dirExecutables, pkg),
				targetRate: rate,
				targetChannels: chans,
				excludedFiles: !soundManagerActive ? null : soundManager._ExcludedNames,
				silentFiles: !soundManagerActive ? null : soundManager._SilentNames,
				swapChannels: !soundManagerActive ? false : soundManager.SwapChannels,
				invertPolarity: !soundManagerActive ? false : soundManager.InvertPolarity,
				optimize: buildMap.optimizeWrittenTrack,
				rebuild: !soundManagerActive ? false : soundManager.Rebuild
			);

			if (legacy) {
				string fileSoundsBatch = Path.Combine(
					dirCompilators,
					compressionAlgorithm == "dz" ? "standard" : "optimized",
					"build",
					soundsName.Trim('.') + ".bat"
				);

				string fileSoundsConfig = Path.Combine(
					dirCompilators,
					compressionAlgorithm == "dz" ? "standard" : "optimized",
					"_config",
					soundsName.Trim('.') + "." + Vectorier.Core.Game.Properties.Extensions.File.Archive.Config
				);

				string fileSoundsArchiveDestination = Path.Combine(
					gameDirectoryPathLegacy,
					soundsName.Trim('.') + "." + Vectorier.Core.Game.Properties.Extensions.File.Archive.Compiled
				);

				string fileSoundsArchiveSource = Path.Combine(
					dirArchivesCompiled,
					Path.GetFileName(fileSoundsArchiveDestination)
				);

				WriteDclConfig(
					config: fileSoundsConfig,
					archive: fileSoundsArchiveSource,
					basedir: tempSoundDir,
					files: null,
					index: 0,
					algorithm: compressionAlgorithm
				);

				var compileResult = CompileAndCopy(
					launcher: fileSoundsBatch,
					archiveSource: fileSoundsArchiveSource,
					archiveDestination: fileSoundsArchiveDestination,
					processesToKill: gameProcesses
				);

				messages.Add(compileResult);
			}

			// Copy static images
			string imagesStaticName = "track_content_2048";

			if (legacy) {
				string fileImagesStaticBatch =
					Path.Combine(dirCompilators,
						compressionAlgorithm == "dz" ? "standard" : "optimized",
						"build",
						imagesStaticName.Trim('.') + ".bat");

				string fileImagesStaticConfig =
					Path.Combine(dirCompilators,
						compressionAlgorithm == "dz" ? "standard" : "optimized",
						"_config",
						imagesStaticName.Trim('.') + "." + Vectorier.Core.Game.Properties.Extensions.File.Archive.Config);

				string fileImagesStaticArchiveDestination =
					Path.Combine(gameDirectoryPathLegacy,
						imagesStaticName.Trim('.') + "." + Vectorier.Core.Game.Properties.Extensions.File.Archive.Compiled);

				string fileImagesStaticArchiveSource =
					Path.Combine(
						dirArchivesCompiled,
						Path.GetFileName(fileImagesStaticArchiveDestination)
					);

				WriteDclConfig(
					config: fileImagesStaticConfig,
					archive: fileImagesStaticArchiveSource,
					basedir: staticImagesBasePath,
					files: activeImagesStatic,
					index: 0,
					algorithm: compressionAlgorithm
				);

				var compileResult = CompileAndCopy(
					launcher: fileImagesStaticBatch,
					archiveSource: fileImagesStaticArchiveSource,
					archiveDestination: fileImagesStaticArchiveDestination,
					processesToKill: gameProcesses
				);

				messages.Add(compileResult);
			} else {
				string top = Vectorier.Utils.Paths.From(activeImagesStatic).Top();

				foreach (var file in activeImagesStatic) {
					string relative = Vectorier.Utils.Paths.From(top).RelativeTo(file);
					string dst = Path.Combine(
						modernAssetsPath,
						"textures",
						relative
					);

					//Debug.Log(dst);

					Vectorier.Utils.Paths.SafeCopy(file, dst);
				}
			}

			// -=-=-=- //
			// Copy animated textures
			string universalsName = "track_content_universal";
			string dirUniversalsAssets = "animatedtextures";

			string fileUniversalsBatch =
				Path.Combine(dirCompilators,
					compressionAlgorithm == "dz" ? "standard" : "optimized",
					"build",
					universalsName.Trim('.') + ".bat");

			string fileUniversalsConfig =
				Path.Combine(dirCompilators,
					compressionAlgorithm == "dz" ? "standard" : "optimized",
					"_config",
					universalsName.Trim('.') + "." + Vectorier.Core.Game.Properties.Extensions.File.Archive.Config);

			string fileUniversalsArchiveDestination =
				Path.Combine(gameDirectoryPathLegacy,
					universalsName.Trim('.') + "." + Vectorier.Core.Game.Properties.Extensions.File.Archive.Compiled);

			string fileUniversalsArchiveSource =
				Path.Combine(dirArchivesCompiled,
					Path.GetFileName(fileUniversalsArchiveDestination));

			string dirUniversalsFull =
				Path.Combine(dataPath, dirArchivesUncompiled, universalsName);

			// BUILD FILE LIST (CLEAN)
			// todo: verify?
			var imageFiles = Directory.GetFiles(dirUniversalsFull, "*", SearchOption.AllDirectories);

			// COPY MODE
			if (legacy) {
				WriteDclConfig(
					config: fileUniversalsConfig,
					archive: fileUniversalsArchiveSource,
					basedir: dirUniversalsFull,
					files: new HashSet<string>(imageFiles),
					index: 0,
					algorithm: compressionAlgorithm
				);

				messages.Add(CompileAndCopy(
					launcher: fileUniversalsBatch,
					archiveSource: fileUniversalsArchiveSource,
					archiveDestination: fileUniversalsArchiveDestination,
					processesToKill: gameProcesses
				));
			} else {
				string root = dirUniversalsFull.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

				foreach (var file in imageFiles) {
					string normalizedFile = file.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

					string relative = normalizedFile.StartsWith(root)
						? normalizedFile.Substring(root.Length).TrimStart(Path.AltDirectorySeparatorChar)
						: Path.GetFileName(normalizedFile);

					string dst = Path.Combine(modernAssetsPath, dirUniversalsAssets, relative);

					Vectorier.Utils.Paths.SafeCopy(file, dst);
				}
			}

			// -=-=-=- //

			if (legacy) {
				Debug.Log(
					$"Compiled {messages.Count} archives succesfully [click for details]\n" +
					string.Join("\n", messages.Select(m => {
						var time = TimeSpan.FromMilliseconds(m.TimeMs);

						string nameFormatted = Path.GetFileName(m.Name);
						string timeFormatted = $"{time.Seconds}s {time.Milliseconds}ms";

						return $"- <color=#AAAAAA>{timeFormatted}</color>\t<b>{nameFormatted}</b>";
					}))
				);
			} else {
				Vectorier.Utils.Assets.Executable.Processes.Kill(pids: gameProcesses);
			}

			if (modernAssetsPath != null) {
				result["StreamingAssets"] = modernAssetsPath;
			}

			return result;
		}

		// -=-=-=- //
		// Processes helpers

		public static void RunBatch(string batchPath) {
			string absBatchPath = Path.GetFullPath(batchPath);
			string batchDir = Path.GetDirectoryName(absBatchPath);

			if (!File.Exists(absBatchPath)) {
				Debug.LogError($"Batch file not found: {absBatchPath}");
				return;
			}

			var process = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = absBatchPath,
					WorkingDirectory = batchDir, // make sure it's the batch folder
					UseShellExecute = true,
					CreateNoWindow = true
				}
			};

			try {
				process.Start();
				if (!process.WaitForExit(2 * 60 * 1000)) {
					Debug.LogError($"Timeout waiting for {absBatchPath}");
					process.Kill();
				}
			} finally {
				process.Close();
			}
		}

		// -=-=-=- //
		// Archive builders

		public static void WriteDclConfig(
			string config,

			string archive,
			string basedir,

			HashSet<string>? files,

			int index,
			string algorithm
		) {

			if (files == null) {
				files = new HashSet<string>(Directory.GetFiles(basedir, "*", SearchOption.AllDirectories));
			}

			HashSet<string> unique = new HashSet<string>(files, StringComparer.OrdinalIgnoreCase);

			// sort into a List
			List<string> sortable = unique
				.OrderBy(f => {
					string name = Path.GetFileNameWithoutExtension(f);
					return int.TryParse(name, out int n) ? n : int.MaxValue;
				})
				.ThenBy(f => f, StringComparer.OrdinalIgnoreCase)
				.ToList();

			using (StreamWriter fileObj = new StreamWriter(config)) {
				fileObj.WriteLine($"archive \"{archive}\"");
				fileObj.WriteLine($"basedir \"{basedir}\"");

				// Write to file
				for (int i = 0; i < sortable.Count; i++) {
					string file = sortable[i];
					file = Vectorier.Utils.Paths.From(basedir).RelativeTo(file);

					string line = $"file \"{file}\" {index} {algorithm.ToLower()}";

					if (i < sortable.Count - 1) {
						fileObj.WriteLine(line);
					} else {
						fileObj.Write(line);
					}
				}
			}
		}

		private static ArchiveResult CompileAndCopy(
			string launcher,
			string archiveSource,
			string archiveDestination,
			List<int>? processesToKill = null
		) {
			if (!File.Exists(launcher)) {
				Debug.LogError($"{launcher} file cannot be found");
				return new ArchiveResult(string.Empty, 0);
			}

			var stopwatch = System.Diagnostics.Stopwatch.StartNew();

			if (processesToKill != null) {
				Vectorier.Utils.Assets.Executable.Processes.Kill(pids: processesToKill);
				RunBatch(launcher);
			}

			string src = Path.GetFullPath(archiveSource);
			string dst = Path.GetFullPath(archiveDestination);

			if (!File.Exists(src)) {
				Debug.LogError($"Expected archive not found: {src}");
				stopwatch.Stop();
				return new ArchiveResult(string.Empty, 0);
			}

			Directory.CreateDirectory(Path.GetDirectoryName(dst));
			Vectorier.Utils.Paths.SafeCopy(src, dst);

			stopwatch.Stop();

			long timeMs = stopwatch.ElapsedMilliseconds;

			return new ArchiveResult(dst, timeMs);
		}
	}
}