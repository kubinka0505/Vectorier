using UnityEngine;

using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Collections.Generic;

// -=-=-=- //

namespace Vectorier.Utils.Assets.Management {
	public sealed class XmlFileInfo {
		public bool HasModel;
		public bool HasSet;
		public bool HasTrack;
		public bool HasTemplate;
		public bool IsMoves;

		public HashSet<string> Sets = new HashSet<string>();
		public HashSet<string> Models = new HashSet<string>();
		public HashSet<string> Sounds = new HashSet<string>();
		public HashSet<string> Animations = new HashSet<string>();
		public HashSet<string> ImagesStatic = new HashSet<string>();
	}

	public sealed class Hardcoded {
		public static readonly HashSet<string> Sounds = new HashSet<string>() {
			"bonus_pickup",
			"cash_register",
			"trick_activate",
			"ui_button_big_release",
			"ui_button_big_toggle",
			"ui_button_round_toggle",
			"ui_button_square_toggle",
			"ui_click",
			"ui_window_options",
			"ui_window_profile",
			"ui_window_shop"
		};

		public static readonly HashSet<string> Skins = new HashSet<string>() {
			// base skeleton
			"0",
			"1",

			// shop gear
			"GEAR_CAP",
			"GEAR_SCARF",
			"GEAR_SHORTS",
		};
	}

	public sealed class Isolator {
		// CONFIG
		public static readonly HashSet<string> UnusedFiles = new HashSet<string>() {
			// Beta Tracks
			"3",
			"4",

			// Resources
			"buildings_lab",
			"objects_lab",

			// Release Tracks
			"cs00",
			"cs01",
			"cs02",
			"cs01a",
			"DelayedTest",
			"jump_auto",
			"MovingTest",
			"DelayedTest",
			"CS_yard_cs01",
			"CS_yard_cs02",
			"Lift_example",
			"reverse_test",
			"NewMoveObject",
			"cs02withTruck",
			"ForceImageType",
			"CS_techpark_cs01",
			"CS_yard_cs02_new",
			"cs_bike_starting",
			"Techoparkgameplay",
			"landing_wait_jump",

			// Models
			"cs_bike_starting_one_model"

			// No occurence
			/*
			"2",
			"bike",
			"cap_2",
			"cs02_flash_memory",
			"cs_techpark_cs01_girl",
			"cs_techpark_cs01_glider",
			"cs_techpark_cs01_glidergirl",
			"cs_techpark_cs01_main_glider",
			*/
		};

		public static XmlFileInfo ParseFile(string file) => ParseInternal(file);

		// MAIN PARSER
		private static XmlFileInfo ParseInternal(string file) {
			var info = new XmlFileInfo();
			string fname = Path.GetFileNameWithoutExtension(file);

			if (IsUnused(fname)) {
				return info;
			}

			bool inSets = false;
			bool inMoves = false;

			using var reader = XmlReader.Create(file, new XmlReaderSettings {
				IgnoreComments = true,
				IgnoreWhitespace = true
			});

			while (reader.Read()) {
				if (reader.NodeType == XmlNodeType.EndElement) {
					HandleEndElement(reader.Name, ref inSets, ref inMoves);
					continue;
				}

				if (reader.NodeType != XmlNodeType.Element) {
					continue;
				}

				// ROOT VALIDATION
				HandleRootNode(reader, reader.Name, info, ref inSets, ref inMoves);

				// CONTENT PARSING
				HandleSets(reader, inSets, info);
				HandleImages(reader, reader.Name, info);
				HandleModels(reader, reader.Name, info);
				HandleSounds(reader, reader.Name, info);

				HandleMoves(reader, reader.Name, inMoves, info);
			}

			return info;
		}

		// VALIDATION HELPERS
		private static bool IsUnused(string fname) {
			return UnusedFiles.Contains(fname, StringComparer.OrdinalIgnoreCase);
		}

		private static bool IsTemplate(string name) {
			return string.Equals(name, "template", StringComparison.OrdinalIgnoreCase);
		}

		// todo

		private static bool IsTrack(string name) {
			return string.Equals(name, "track", StringComparison.OrdinalIgnoreCase);
		}

		private static bool IsSetRoot(string name) {
			return string.Equals(name, "objects", StringComparison.OrdinalIgnoreCase);
		}

		private static bool IsMoves(string name) {
			return string.Equals(name, "moves", StringComparison.OrdinalIgnoreCase);
		}

		private static bool IsSetsRoot(string name) {
			return string.Equals(name, "sets", StringComparison.OrdinalIgnoreCase);
		}

		// ROOT HANDLING (ORDER-SAFE)
		private static void HandleRootNode(
			XmlReader reader,
			string name,

			XmlFileInfo info,

			ref bool inSets,
			ref bool inMoves
		) {
			if (reader.Depth != 1) {
				return;
			}

			if (IsTrack(name)) {
				info.HasTrack = true;
				return;
			}

			if (IsSetRoot(name)) {
				info.HasSet = true;
				return;
			}

			if (IsMoves(name)) {
				info.IsMoves = true;
				inMoves = true;
				return;
			}

			if (IsSetsRoot(name)) {
				inSets = true;
				return;
			}

			if (IsTemplate(name)) {
				info.HasTemplate = true;
				return;
			}
		}

		private static void HandleEndElement(
			string name,
			
			ref bool inSets,
			ref bool inMoves
		) {
			switch (name.ToLowerInvariant()) {
				case "sets": inSets = false; break;
				case "moves": inMoves = false; break;
			}
		}

		// SETS
		private static void HandleSets(
			XmlReader reader,

			bool inSets,

			XmlFileInfo info
		) {
			if (!inSets) {
				return;
			}

			if (reader.MoveToAttribute("FileName")) {
				var value = reader.Value?.Trim();

				if (!string.IsNullOrEmpty(value)) {
					info.Sets.Add(value);
				}
			}
		}

		// IMAGES
		private static void HandleImages(
			XmlReader reader,

			string nodeName,

			XmlFileInfo info
		) {
			if (!IsElement(nodeName, "image")) {
				return;
			}

			var value = reader.GetAttribute("ClassName");

			if (!string.IsNullOrWhiteSpace(value)) {
				info.ImagesStatic.Add(value.Trim());
			}
		}

		// MODELS
		private static bool IsElement(string a, string b) {
			return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
		}

		private static void HandleModels(
			XmlReader reader,

			string nodeName,

			XmlFileInfo info
		) {
			if (!IsElement(nodeName, "model")) {
				return;
			}

			info.HasModel = true;

			var className = reader.GetAttribute("ClassName");

			if (!string.IsNullOrWhiteSpace(className)) {
				info.Models.Add(className.Trim());
				return;
			}

			var skins = reader.GetAttribute("Skins");

			if (!string.IsNullOrWhiteSpace(skins)) {
				foreach (var value in skins.Split(
					Vectorier.Core.Game.Properties.AttributeSeparator.ToCharArray(),
					StringSplitOptions.RemoveEmptyEntries)
				) {
					info.Models.Add(value.Trim());
				}
			}
		}

		// SOUNDS
		private static void HandleSounds(
			XmlReader reader,

			string nodeName,

			XmlFileInfo info
		) {
			// =====================================================
			// <Sound Name="birds4"/>
			// ignore "_Sound"
			// =====================================================

			if (IsElement(nodeName, "sound")) {
				var soundName = reader.GetAttribute("Name");

				if (!string.IsNullOrWhiteSpace(soundName)) {
					soundName = soundName.Trim();

					if (!string.Equals(soundName, "_Sound", StringComparison.Ordinal)) {
						var parts = soundName.Split(
							Vectorier.Core.Game.Properties.AttributeSeparator.ToCharArray(),
							StringSplitOptions.RemoveEmptyEntries
						);

						foreach (var p in parts) {
							var value = p.Trim();

							if (value.Length > 0) {
								info.Sounds.Add(value);
							}
						}
					}
				}

				return;
			}

			// =====================================================
			// <SetVariable Name="Second_Sound" Value="birds2"/>
			// =====================================================

			if (IsElement(nodeName, "setvariable")) {
				var variableName = reader.GetAttribute("Name");

				if (string.IsNullOrWhiteSpace(variableName)) {
					return;
				}

				if (
					variableName.IndexOf(
						"sound",
						StringComparison.OrdinalIgnoreCase
					) < 0
				) {
					return;
				}

				var valueAttr = reader.GetAttribute("Value");

				if (string.IsNullOrWhiteSpace(valueAttr)) {
					return;
				}

				var parts = valueAttr.Split(
					Vectorier.Core.Game.Properties.AttributeSeparator.ToCharArray(),
					StringSplitOptions.RemoveEmptyEntries
				);

				foreach (var p in parts) {
					var value = p.Trim();

					if (value.Length > 0) {
						info.Sounds.Add(value);
					}
				}
			}
		}

		// MOVES (sounds + animations)
		private static bool TryGetString(
			XmlReader reader,

			string attr,

			out string value
		) {
			value = reader.GetAttribute(attr);
			return !string.IsNullOrEmpty(value);
		}

		private static bool TryGetInt(
			XmlReader reader,
		
			string attr,
		
			out int value
		) {
			string raw = reader.GetAttribute(attr);

			if (string.IsNullOrWhiteSpace(raw)) {
				value = default;
				return false;
			}

			return int.TryParse(raw, out value);
		}

		private static void HandleMoves(
			XmlReader reader,

			string name,
			bool inMoves,

			XmlFileInfo info
		) {
			if (!inMoves) {
				return;
			}

			HandleMoveSounds(reader, name, info);
			HandleMoveAnimations(reader, info);
		}

		private static void HandleMoveSounds(
			XmlReader reader,

			string nodeName,

			XmlFileInfo info
		) {
			if (!IsElement(nodeName, "sound")) {
				return;
			}

			var name = reader.GetAttribute("Name");

			if (string.IsNullOrWhiteSpace(name)) {
				return;
			}

			var parts = name.Split(
				Vectorier.Core.Game.Properties.AttributeSeparator.ToCharArray(),
				StringSplitOptions.RemoveEmptyEntries
			);

			foreach (var p in parts) {
				var value = p.Trim();

				if (value.Length > 0) {
					info.Sounds.Add(value);
				}
			}
		}

		private static void HandleMoveAnimations(
			XmlReader reader,

			XmlFileInfo info
		) {
			// MUST be on an element with FileName
			var fileAnim = reader.GetAttribute("FileName");
			if (string.IsNullOrWhiteSpace(fileAnim)) {
				return;
			}

			// OPTIONAL pivot (do NOT hard fail)
			var pivotNode = reader.GetAttribute("PivotNode");
			if (string.IsNullOrWhiteSpace(pivotNode)) {
				return;
			}

			// OPTIONAL ID (soft validation instead of hard reject)
			var idRaw = reader.GetAttribute("ID");
			if (
				!string.IsNullOrWhiteSpace(idRaw) &&
				!int.TryParse(idRaw, out _)
			) {
				return;
			}

			// OPTIONAL frames
			var firstRaw = reader.GetAttribute("FirstFrame");
			var endRaw = reader.GetAttribute("EndFrame");

			if (
				int.TryParse(firstRaw, out int firstFrame) &&
				int.TryParse(endRaw, out int endFrame))
			{
				if (firstFrame > endFrame) {
					return;
				}
			}

			// FINAL: store animation
			info.Animations.Add(
				Path.GetFileNameWithoutExtension(fileAnim.Trim())
			);
		}
	}
}