using UnityEngine;
using UnityEditor;

using System.IO;
using System.Xml;
using System.Collections.Generic;

using Vectorier;

// -=-=-=- //

[AddComponentMenu("Vectorier/Area Properties")]
public class AreaProperties : MonoBehaviour {
	public enum EnumType {
		None,
		Help,
		Arrest,
		Animation
	}

	public enum EnumKey {
		Up,
		Down,
		Left,
		Right,
		None
	}

	[Tooltip("Area type")]
	public EnumType Type = EnumType.Animation;

	[Range(0, 1000)]
	[Tooltip("⚠️ Applicable for \"Arrest\" area type only")]
	public int Distance = 300;

	[Tooltip(@"String key from the ""common_xml.dz/localization_all.xml"" values table displayed while help.")]
	public string Description = "";

	[Tooltip(@"Key which if pressed finishes the area event.")]
	public EnumKey Key;

	public void OnEnable() {}
}

// component //

#if UNITY_EDITOR
[CustomEditor(typeof(AreaProperties))]
public class AreaPropertiesEditor : Editor {
	private static readonly string _VAL_DESC = "Description";
	private static readonly int _VAL_SPACE = 8;

	private static string localizationPath;
	private static List<DictionaryEntry> localizationKeys = new List<DictionaryEntry>();
	private static bool localizationLoaded = false;

	private enum SortMode {Length, Alphabetically}
	private static SortMode currentSortMode = SortMode.Alphabetically;
	private static bool ascending = true;
	private static bool showKey = false;

	private void OnEnable() {
		if (string.IsNullOrEmpty(localizationPath)) {
			localizationPath = Path.Combine(Application.dataPath, "XML", "dzip", ".common_xml", "localization_all.xml");
		}
	}

	// -=-=-=- //
	// main

	public override void OnInspectorGUI() {
		serializedObject.Update();

		var TypeProp = serializedObject.FindProperty("Type");
		var DistanceProp = serializedObject.FindProperty("Distance");
		var DescriptionProp = serializedObject.FindProperty(_VAL_DESC);
		var KeyProp = serializedObject.FindProperty("Key");

		// type dropdown
		EditorGUILayout.PropertyField(TypeProp, new GUIContent("Type", "Type of area behavior."));
		var type = (AreaProperties.EnumType)TypeProp.enumValueIndex;

		if (type != AreaProperties.EnumType.None && type != AreaProperties.EnumType.Animation) {
			GUILayout.Space(_VAL_SPACE);
		}

		switch (type) {
			case AreaProperties.EnumType.Arrest:
				EditorGUILayout.PropertyField(DistanceProp, new GUIContent("Distance", "Applicable for 'Arrest' area type only."));
				break;

			case AreaProperties.EnumType.Help:
				if (!localizationLoaded)
					localizationKeys = LoadNodeKey(localizationPath, "log", "eng");

				// key dropdown with arrows
				string[] keyDisplayOptions = new string[] { "↑ Up", "↓ Down", "← Left", "→ Right", "None" };
				int currentKeyIndex = (int)KeyProp.enumValueIndex;
				int selectedKeyIndex = EditorGUILayout.Popup(
					new GUIContent("Key", "Key which if pressed finishes the area event."),
					currentKeyIndex, keyDisplayOptions
				);
				KeyProp.enumValueIndex = selectedKeyIndex;

				if (localizationKeys.Count > 0) {
					// apply sorting on the actual displayed string
					localizationKeys.Sort((a, b) => {
						string aDisplay = showKey ? $"{a.Key} | {a.Value}" : a.Value;
						string bDisplay = showKey ? $"{b.Key} | {b.Value}" : b.Value;

						switch (currentSortMode) {
							case SortMode.Length:
								return ascending
									? aDisplay.Length.CompareTo(bDisplay.Length)
									: bDisplay.Length.CompareTo(aDisplay.Length);
							case SortMode.Alphabetically:
							default:
								return ascending
									? string.Compare(aDisplay, bDisplay)
									: string.Compare(bDisplay, aDisplay);
						}
					});

					// description dropdown
					int currentIndex = localizationKeys.FindIndex(k => k.Key == DescriptionProp.stringValue);
					if (currentIndex < 0) currentIndex = 0;

					string[] displayOptions = localizationKeys.ConvertAll(
						k => showKey ? $"{k.Key} | {k.Value}" : $"{k.Value}"
					).ToArray();

					int selectedIndex = EditorGUILayout.Popup(
						new GUIContent("Description", "String key from localization file."),
						currentIndex,
						displayOptions
					);

					// assign the key from sorted list to avoid mismatches
					DescriptionProp.stringValue = localizationKeys[selectedIndex].Key;

					// horizontal row for sorting options
					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("");
					showKey = EditorGUILayout.ToggleLeft("Show Key", showKey, GUILayout.Width(80));
					ascending = EditorGUILayout.ToggleLeft("Ascending", ascending, GUILayout.Width(80));
					EditorGUILayout.EndHorizontal();

					// sorting dropdown below Description
					currentSortMode = (SortMode)EditorGUILayout.EnumPopup(
						new GUIContent("Sorting", "How the dropdown is sorted."),
						currentSortMode
					);
				} else {
					EditorGUILayout.HelpBox(SettingsHelpers.ParseHelpBoxString(
						"Localization keys not found or file missing!"),
						MessageType.Warning
					);
					EditorGUILayout.PropertyField(DescriptionProp, new GUIContent(_VAL_DESC, "String key from localization file."));
				}
				break;

			case AreaProperties.EnumType.None:
			case AreaProperties.EnumType.Animation:
			default:
				break;
		}

		serializedObject.ApplyModifiedProperties();
	}

	// -=-=-=- //

	// small struct for localization entries
	private struct DictionaryEntry {
		public string Key;
		public string Value;

		public DictionaryEntry(string key, string value) {
			Key = key;
			Value = value;
		}
	}

	// load node + key
	private static List<DictionaryEntry> LoadNodeKey(string fileInput, string nodeParent, string nodeValue) {
		List<DictionaryEntry> keys = new List<DictionaryEntry>();
		localizationLoaded = true;

		if (string.IsNullOrEmpty(fileInput) || !File.Exists(fileInput)) {
			Debug.LogWarning($"File not found at path:\n{fileInput}");
			return keys;
		}

		try {
			var xml = new XmlDocument();
			xml.Load(fileInput);

			var logNode = xml.SelectSingleNode(nodeParent);

			if (logNode == null) {
				Debug.LogWarning($"Node '{nodeParent}' not found in XML file.");
				return keys;
			}

			foreach (XmlNode child in logNode.ChildNodes) {
				if (child.NodeType != XmlNodeType.Element) {
					continue;
				}

				string key = child.Name;
				string value = child.Attributes?[nodeValue]?.Value ?? key;

				keys.Add(new DictionaryEntry(key, value));
			}
		} catch (System.Exception ex) {
			Debug.LogError("Failed to load file: " + ex.Message);
		}

		return keys;
	}
}
#endif