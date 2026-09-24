using UnityEditor;
using UnityEngine;

using System.IO;

// -=-=-=- //

#nullable enable

namespace Vectorier.Utils {
	public class GameObjectParser {
		private GameObject obj;

		private GameObjectParser(GameObject obj) {
			this.obj = obj;
		}

		public static GameObjectParser Parse(GameObject obj) {
			return new GameObjectParser(obj);
		}

		public bool IsVisible() {
			return !obj.CompareTag("EditorOnly") && 
				!obj.CompareTag("Unused") && 
				!SceneVisibilityManager.instance.IsHidden(obj) && 
				obj.activeInHierarchy;
		}

		public string GetPath(GameObject? obj = null) {
			if (obj == null) {
				return "<null>";
			}

			string sceneName = Path.GetFileName(obj.scene.path);

			if (string.IsNullOrEmpty(sceneName)) {
				sceneName = "Current scene";
			}

			string sep = " → ";

			if (obj.transform.parent == null) {
				return $"{sceneName}{sep}{obj.name}";
			}

			Transform parent = obj.transform.parent;

			int nameBasedIndex = 0;

			foreach (Transform sibling in parent) {
				if (sibling == obj.transform) {
					break;
				}

				if (sibling.name == obj.name) {
					nameBasedIndex++;
				}
			}

			bool hasDuplicateNames = false;

			foreach (Transform sibling in parent) {
				if (sibling != obj.transform && sibling.name == obj.name) {
					hasDuplicateNames = true;
					break;
				}
			}

			string parentPath = GetPath(parent.gameObject);

			string currentSegment = hasDuplicateNames
				? $"{obj.name}[{nameBasedIndex}]"
				: obj.name;

			return $"{parentPath}{sep}{currentSegment}";
		}
	}
}