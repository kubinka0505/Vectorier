using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

#nullable enable

// -=-=-=- //

public class VectorierSettingsProvider : SettingsProvider {
    private bool useShortcutLaunch;

    private bool saveSceneBeforeBuildMap;
    private bool validateScene;

    private bool randomizedTriggerSuffixes;

	// -=-=-=- //

	private bool showDropdownScene = false;
	private bool showDropdownTriggers = false;

    public VectorierSettingsProvider(string path, SettingsScope scopes, IEnumerable<string>? keywords = null) : base(path, scopes, keywords) {
        useShortcutLaunch = EditorPrefs.GetBool(VectorierSettings.UseShortcutLaunchKey, false);

        saveSceneBeforeBuildMap = EditorPrefs.GetBool(VectorierSettings.SaveSceneBeforeBuildMapKey, false);
        validateScene = EditorPrefs.GetBool(VectorierSettings.ValidateSceneKey, false);

        randomizedTriggerSuffixes = EditorPrefs.GetBool(VectorierSettings.RandomizedTriggerSuffixesKey, false);
    }

    public override void OnActivate(string searchContext, VisualElement rootElement) {
        // pass
    }

	public override void OnGUI(string searchContext) {
		EditorGUILayout.BeginVertical(new GUIStyle {
			stretchWidth = true,
			padding = new RectOffset(0, 0, 15, 0)
		});

		GUIStyle headerStyle = new GUIStyle(GUI.skin.label) {
			fontSize = 20,
			fontStyle = FontStyle.Bold
		};

		// Set a unified label width
		var unifiedLabelWidth = 225f;
		EditorGUIUtility.labelWidth = unifiedLabelWidth;

		var gameDirectory = VectorierSettings.GameDirectory;
		var gameShortcutPath = VectorierSettings.GameShortcutPath;

		MakeTextField(ref gameDirectory, "Game Directory", "Game location directory / Steam URL");
		MakeTextField(ref gameShortcutPath, "Game Shortcut Path", "Game shortcut location directory (Vector.exe - Shortcut)");

		useShortcutLaunch = MakeToggle(useShortcutLaunch, "Use Shortcut Launch", "Launch the game using shortcut");

		EditorGUILayout.EndVertical();

		EditorPrefs.SetString(VectorierSettings.GameDirectoryKey, gameDirectory);
		EditorPrefs.SetString(VectorierSettings.GameShortcutKey, gameShortcutPath);
		EditorPrefs.SetBool(VectorierSettings.UseShortcutLaunchKey, useShortcutLaunch);
		EditorPrefs.SetBool(VectorierSettings.SaveSceneBeforeBuildMapKey, saveSceneBeforeBuildMap);

		DrawHorizontalLine();

		// Global settings

		// Use the custom style when drawing the label
		GUILayout.Label("Global settings", headerStyle);

		// --- FOLDOUTS ---
		// Triggers
		showDropdownTriggers = EditorGUILayout.Foldout(showDropdownTriggers, "Triggers", true);
		if (showDropdownTriggers) {
			EditorGUI.indentLevel++;
			randomizedTriggerSuffixes = MakeToggle(randomizedTriggerSuffixes, "Randomized Trigger Suffixes", 
				"Fixes writing \"Dynamic Trigger\" objects inside \"Dynamic\" components if these triggers have:\n\n- empty \"Transformation Name\" field\n- no \"Multiple Transformation\" checked\n  - ..or having less than 1 of them\n\n..by adding \"_n\" suffix to their node's \"Name\" attribute, where \"n\" is random number.");
			EditorGUI.indentLevel--;
			EditorPrefs.SetBool(VectorierSettings.RandomizedTriggerSuffixesKey, randomizedTriggerSuffixes);

			EditorGUILayout.Space(5f);
		}

		// Scene
		showDropdownScene = EditorGUILayout.Foldout(showDropdownScene, "Scene", true);
		if (showDropdownScene) {
			EditorGUI.indentLevel++;
			saveSceneBeforeBuildMap = MakeToggle(saveSceneBeforeBuildMap, "Save scene before map build", "Save the scene before map building");
			EditorGUILayout.HelpBox("Applicable only if the scene file exists, otherwise False.", MessageType.Warning);
			EditorGUILayout.Space(5f);
			EditorGUI.indentLevel--;
			EditorPrefs.SetBool(VectorierSettings.SaveSceneBeforeBuildMapKey, saveSceneBeforeBuildMap);

			EditorGUI.indentLevel++;
			validateScene = MakeToggle(validateScene, "Validate scene", "Partially validates the scene before map building");
			EditorGUI.indentLevel--;
			EditorPrefs.SetBool(VectorierSettings.ValidateSceneKey, validateScene);

			EditorGUILayout.Space(5f);
		}

		// Reset label width after foldouts
		EditorGUIUtility.labelWidth = 0;
	}

    private void MakeTextField(ref string? value, string textFieldLabel, string tooltip) {
        value ??= "";
        var guiContent = new GUIContent(textFieldLabel, tooltip);
        value = EditorGUILayout.TextField(guiContent, value);
    }

    private bool MakeToggle(bool value, string toggleLabel, string tooltip) {
        var guiContent = new GUIContent(toggleLabel, tooltip);
        return EditorGUILayout.Toggle(guiContent, value);
    }

    private void DrawHorizontalLine() {
        GUILayout.Box("", new GUIStyle {
            fixedHeight = 1,
            margin = new RectOffset(0, 0, 5, 5),
            border = new RectOffset(0, 0, 1, 1),
            normal = { background = EditorGUIUtility.whiteTexture }
        });
    }

    [SettingsProvider]
    public static SettingsProvider CreateVectorierSettingsProvider() {
        return new VectorierSettingsProvider("Project/Vectorier", SettingsScope.Project);
    }
}