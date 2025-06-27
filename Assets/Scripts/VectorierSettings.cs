using UnityEditor;

#nullable enable

// -=-=-=- //

public static class VectorierSettings {
    public const string SettingsPath = "Assets/Settings/VectorierSettings.asset";

    internal const string GameDirectoryKey = "VectorierSettings.GameDirectory";
    public static string? GameDirectory => EditorPrefs.GetString(GameDirectoryKey, "");

    internal const string GameShortcutKey = "VectorierSettings.GameShortcutPath";
    public static string? GameShortcutPath => EditorPrefs.GetString(GameShortcutKey, "");

	// -=-=-=- //

    internal const string UseShortcutLaunchKey = "VectorierSettings.UseShortcutLaunch";
    public static bool UseShortcutLaunch => EditorPrefs.GetBool(UseShortcutLaunchKey, false);

	// -=-=-=- //
	// Scene

    internal const string SaveSceneBeforeBuildMapKey = "VectorierSettings.SaveSceneBeforeBuildMap";
    public static bool SaveSceneBeforeBuildMap => EditorPrefs.GetBool(SaveSceneBeforeBuildMapKey, true);

	internal const string ValidateSceneKey = "VectorierSettings.ValidateScene";
	public static bool ValidateScene => EditorPrefs.GetBool(ValidateSceneKey, true);

	// -=-=-=- //
	// Trigers

    internal const string RandomizedTriggerSuffixesKey = "VectorierSettings.RandomizedTriggerSuffixes";
	public static bool RandomizedTriggerSuffixes => EditorPrefs.GetBool(RandomizedTriggerSuffixesKey, true);
}