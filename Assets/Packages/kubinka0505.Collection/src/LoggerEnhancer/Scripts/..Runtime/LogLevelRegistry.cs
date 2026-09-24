using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

// -=-=-=- //

namespace Logger {
	[CreateAssetMenu(fileName = "LogLevelRegistry", menuName = "Logger/LogLevel Registry")]
	public class LogLevelRegistry : ScriptableObject {
		[SerializeField]
		private List<LogLevelDefinition> customLevels = new List<LogLevelDefinition>();

		private static LogLevelRegistry _instance;

		public static LogLevelRegistry Instance {
			get {
				if (_instance == null) {
					_instance = Resources.Load<LogLevelRegistry>("LogLevelRegistry");

					if (_instance == null) {
						#if UNITY_EDITOR
						_instance = CreateInstance<LogLevelRegistry>();
						string path = "Assets/Resources/LogLevelRegistry.asset";
						System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
						AssetDatabase.CreateAsset(_instance, path);
						AssetDatabase.SaveAssets();
						#endif
					}
				}

				return _instance;
			}
		}

		public bool TryGetLevel(string key, out LogLevelDefinition level) {
			level = customLevels.Find(l => l.key == key);
			return level != null;
		}

		public void RegisterLevel(LogLevelDefinition level) {
			if (!customLevels.Exists(l => l.key == level.key)) {
				customLevels.Add(level);

				#if UNITY_EDITOR
				EditorUtility.SetDirty(this);
				AssetDatabase.SaveAssets();
				#endif
			}
		}

		public void UnregisterLevel(string key) {
			customLevels.RemoveAll(l => l.key == key);

			#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssets();
			#endif
		}

		public List<LogLevelDefinition> GetAllLevels() => new List<LogLevelDefinition>(customLevels);
	}
}
