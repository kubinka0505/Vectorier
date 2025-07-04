using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class SetsManager : MonoBehaviour {
	[HideInInspector] public List<string> tags = new List<string> { "buildings", "buildings_downtown", "objects" };
	[HideInInspector] public List<int> dropdownSelections = new List<int> { 0, 0, 1 };

	private void Reset() {
		if (tags.Count <= 3) {
			tags.Add("");
			dropdownSelections.Add(1);
		}
	}
}

[CustomEditor(typeof(SetsManager))]
public class SetsManagerEditor : Editor {
	private readonly string[] dropdownOptions = new string[] { "City", "Ground", "Library" };
	private const string xmlRelativeRoot = "Assets/XML/dzip/level_xml";

	public override void OnInspectorGUI() {
		SetsManager setsManager = (SetsManager)target;

		EditorGUILayout.Space();

		for (int i = 0; i < setsManager.tags.Count; i++) {
			EditorGUILayout.BeginHorizontal();

			if (i < 3) {
				GUI.enabled = false;
				EditorGUILayout.TextArea(setsManager.tags[i], GUILayout.MinWidth(200));
				EditorGUILayout.Popup(setsManager.dropdownSelections[i], dropdownOptions, GUILayout.Width(80));
				GUI.enabled = true;
				EditorGUILayout.EndHorizontal();
				continue;
			}

			// Use DelayedTextField to update only on Enter or focus loss
			setsManager.tags[i] = EditorGUILayout.DelayedTextField(setsManager.tags[i], GUILayout.MinWidth(170));

			// File picker button after textarea
			bool filePick = GUILayout.Button("...", GUILayout.Width(25));

			setsManager.dropdownSelections[i] = EditorGUILayout.Popup(
				setsManager.dropdownSelections[i],
				dropdownOptions,
				GUILayout.Width(80)
			);

			if (GUILayout.Button("✕", GUILayout.Width(20))) {
				setsManager.tags.RemoveAt(i);
				setsManager.dropdownSelections.RemoveAt(i);
				EditorGUILayout.EndHorizontal();
				break;
			}

			EditorGUILayout.EndHorizontal();

		if (filePick) {
			string selectedPath = EditorUtility.OpenFilePanel("Select file", xmlRelativeRoot, "xml,swf");

			if (!string.IsNullOrEmpty(selectedPath)) {
				string absoluteXmlRoot = Path.GetFullPath(xmlRelativeRoot);
				string absoluteSelected = Path.GetFullPath(selectedPath);

				if (absoluteSelected.StartsWith(absoluteXmlRoot)) {
					FileInfo fileInfo = new FileInfo(absoluteSelected);

					if (fileInfo.Length < 1024 * 1024) {
						Debug.LogWarning($"{fileInfo.Name} may not be a valid set element ({fileInfo.Length / 1024} KB)");
					}

					string relative = absoluteSelected.Substring(absoluteXmlRoot.Length + 1);
					relative = relative.Replace("\\", "/");
					
					bool isSwf = false;
					if (relative.ToLower().EndsWith(".xml") || relative.ToLower().EndsWith(".swf")) {
						if (relative.ToLower().EndsWith(".swf")) {
							isSwf = true;
						}
						relative = relative.Substring(0, relative.Length - 4);
					}

					if (setsManager.tags.Contains(relative)) {
						Debug.LogError($"File '{relative}' is already in the list!");
					} else {
						setsManager.tags[i] = relative;

						// If .swf, set dropdown to "Library" and warn if previous was different
						if (isSwf) {
							if (setsManager.dropdownSelections[i] != 2) {
								Debug.LogWarning($"Dropdown changed to 'Library' for SWF file '{relative}'.");
							}
							setsManager.dropdownSelections[i] = 2; // Library index
						}
					}
				} else {
					Debug.LogWarning("Selected file is not inside the required directory: " + xmlRelativeRoot);
				}
			}
		}

		}

		if (GUILayout.Button("Add set +")) {
			setsManager.tags.Add("");
			setsManager.dropdownSelections.Add(1);
		}

		EditorUtility.SetDirty(setsManager);
	}
}