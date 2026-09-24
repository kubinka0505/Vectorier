using UnityEditor;
using UnityEngine;

using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Collections.Generic;

// -=-=-=- //

#nullable enable

namespace Vectorier.Utils.Assets.Management {
	public class Isolator {
		private readonly XmlDocument _doc;
		private readonly XmlElement? _root;

		public Isolator(XmlDocument doc) {
			_doc = doc;
			_root = doc?.DocumentElement;
		}

		public static Isolator Isolate(XmlDocument doc) {
			return new Isolator(doc);
		}

		// ANIMATIONS

		public HashSet<string> Animations() {
			var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			if (_root == null) {
				return result;
			}

			// stack-based traversal avoids recursion; avoids XmlNodeList allocation
			var stack = new Stack<XmlNode>();
			stack.Push(_root);

			while (stack.Count > 0) {
				var node = stack.Pop();

				if (node is XmlElement elem) {
					var fileName = elem.GetAttribute("FileName");

					if (!string.IsNullOrEmpty(fileName)) {
						fileName = fileName.Trim();

						if (fileName.Length > 0) {
							var name = Path.ChangeExtension(fileName, null);
							result.Add(name);
						}
					}
				}

				// push children
				var children = node.ChildNodes;

				for (int i = 0; i < children.Count; i++) {
					stack.Push(children[i]);
				}
			}

			return result;
		}

		// IMAGES STATIC

		public HashSet<string> ImagesStatic() {
			var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			if (_root == null) {
				return result;
			}

			var nodes = _root.GetElementsByTagName("Image");

			foreach (XmlNode node in nodes) {
				var elem = node as XmlElement;

				if (elem == null) {
					continue;
				}


				var className = elem.GetAttribute("ClassName");

				if (string.IsNullOrWhiteSpace(className)) {
					continue;
				}

				var cleaned = className.Trim()
					.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

				result.Add(cleaned);
			}

			return result;
		}

		public HashSet<string> ImagesAnimated() {
			var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			if (_root == null) {
				return result;
			}

			var nodes = _root.GetElementsByTagName("Animation");

			foreach (XmlNode node in nodes) {
				var elem = node as XmlElement;

				if (elem == null) {
					continue;
				}

				var className = elem.GetAttribute("ClassName");

				if (string.IsNullOrWhiteSpace(className)) {
					continue;
				}

				var cleaned = className.Trim()
					.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

				result.Add(cleaned);
			}

			return result;
		}

		// MODELS

		public HashSet<string> Models(bool writeGears = true) {
			var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			if (_root == null) {
				return result;
			}

			if (_root.Name.Equals("models", StringComparison.OrdinalIgnoreCase)) {
				return result;
			}

			var nodes = _root.GetElementsByTagName("Model");

			foreach (XmlNode node in nodes) {
				var elem = node as XmlElement;

				if (elem == null) {
					continue;
				}

				var skins = elem.GetAttribute("Skins");

				if (!string.IsNullOrEmpty(skins)) {
					foreach (var p in skins.Split(Vectorier.Core.Game.Properties.AttributeSeparator.ToCharArray())) {
						var cleaned = p.Trim();

						if (!string.IsNullOrEmpty(cleaned)) {
							result.Add(cleaned);
						}
					}
				}

				var className = elem.GetAttribute("ClassName");

				if (!string.IsNullOrEmpty(className)) {
					result.Add(className.Trim());
				}
			}

			if (writeGears) {
				string[] gears = { "cap", "scarf", "shorts" };

				foreach (var gear in gears) {
					result.Add("GEAR_" + gear.ToUpperInvariant());
				}
			}

			return result;
		}

		// SOUNDS

		public HashSet<string> Sounds() {
			var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			if (_root == null) {
				return result;
			}

			if (_root.Name.Equals("sounds", StringComparison.OrdinalIgnoreCase)) {
				return result;
			}

			// --- <Sound Name="...">
			var soundNodes = _root.GetElementsByTagName("Sound");

			foreach (XmlNode node in soundNodes) {
				var elem = node as XmlElement;

				if (elem == null) {
					continue;
				}


				var nameAttr = elem.GetAttribute("Name");

				if (string.IsNullOrEmpty(nameAttr)) {
					continue;
				}

				foreach (var p in nameAttr.Split(Vectorier.Core.Game.Properties.AttributeSeparator.ToCharArray())) {
					var cleaned = p.ToLowerInvariant().Trim();

					if (!string.IsNullOrEmpty(cleaned)) {
						result.Add(cleaned);
					}
				}
			}

			// --- <SetVariable Name="Sound" Value="...">
			var setVars = _root.GetElementsByTagName("SetVariable");

			foreach (XmlNode node in setVars) {
				var elem = node as XmlElement;

				if (elem == null) {
					continue;
				}

				var nameAttr = elem.GetAttribute("Name");

				if (string.IsNullOrEmpty(nameAttr)) {
					continue;
				}

				if (nameAttr.IndexOf("Sound", StringComparison.OrdinalIgnoreCase) < 0) {
					continue;
				}

				var value = elem.GetAttribute("Value");

				if (string.IsNullOrEmpty(value)) {
					continue;
				}

				foreach (var p in value.Split(Vectorier.Core.Game.Properties.AttributeSeparator.ToCharArray())) {
					var cleaned = p.ToLowerInvariant().Trim();

					if (!string.IsNullOrEmpty(cleaned)) {
						result.Add(cleaned);
					}
				}
			}

			return result;
		}

		// -=-=-=- //

		private IEnumerable<string> CollectTextures(GameObject[] gos) {
			var set = new HashSet<string>();

			foreach (var go in gos) {
				if (Vectorier.Utils.GameObjectParser.Parse(go).IsVisible()) {
					continue;
				}

				if (!go.TryGetComponent<SpriteRenderer>(out var sr)) {
					continue;
				}

				var sprite = sr.sprite;
				if (sprite == null) {
					continue;
				}
				#if UNITY_EDITOR
				string path = AssetDatabase.GetAssetPath(sprite.texture);
				string atlas = Path.ChangeExtension(path, Vectorier.Core.Game.Properties.Extensions.File.Image.Atlas);

				if (!string.IsNullOrEmpty(path)) {
					set.Add(path);
				}

				if (File.Exists(atlas)) {
					set.Add(atlas);
				}
				#endif
			}

			return set;
		}

		private string ResolveTexture(string texPath) {
			#if UNITY_EDITOR
			if (texPath.StartsWith("Assets" + Path.AltDirectorySeparatorChar.ToString())) {
				string fullPath = Path.Combine(
					Path.GetDirectoryName(Application.dataPath),
					texPath
				);

				return fullPath.Replace(
					Path.AltDirectorySeparatorChar.ToString(),
					Path.DirectorySeparatorChar.ToString()
				);
			}
			#endif

			return texPath;
		}
	}
}