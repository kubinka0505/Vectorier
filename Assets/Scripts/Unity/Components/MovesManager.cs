using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// -=-=-=- //
// Idea by TheLastCube.

[DisallowMultipleComponent]
public class MovesManager : MonoBehaviour {
	[System.Serializable]
	public class CameraSettings {
		[Range(0.1f, 1.3f)]
		public float ZoomMin = 0.1f;

		[Range(0.1f, 1.3f)]
		public float ZoomMax = 1.3f;

		[Range(0.1f, 1.3f)]
		public float ZoomCurrent = 0.5f;

		[Range(1f, 200f)]
		public float MaxSpeed = 100f;

		[Range(0f, 10f)]
		public float Fluency = 2f;
	}

	[System.Serializable]
	public class TaserSettings {
		// can be inverted
		[Range(100f, 250f)]
		public float Distance = 250f;

		[Range(0.1f, 1.0f)]
		public float Time = 0.35f;

		[Range(1f, 3f)]
		public float HeightFactor = 2f;
	}

	public CameraSettings Camera = new CameraSettings();
	public TaserSettings Taser = new TaserSettings();

	public static readonly Dictionary<string, float> DefaultValues = new Dictionary<string, float> {
		{ "Camera.ZoomMin", 0.1f },
		{ "Camera.ZoomMax", 1.3f },
		{ "Camera.ZoomCurrent", 0.5f },
		{ "Camera.MaxSpeed", 100f },
		{ "Camera.Fluency", 2f },

		{ "Taser.Distance", 250f },
		{ "Taser.Time", 0.35f },
		{ "Taser.HeightFactor", 2f },
	};

	public void OnEnable() {}
}

// -=-=-=- //

[CustomEditor(typeof(MovesManager))]
public class MovesManagerEditor : Editor {
	private MovesManager manager;

	// foldout states
	private static bool showCamera = true;
	private static bool showTaser = true;

	private void OnEnable() {
		manager = (MovesManager)target;
	}

	public override void OnInspectorGUI() {
		serializedObject.Update();

		// Camera
		showCamera = EditorGUILayout.BeginFoldoutHeaderGroup(showCamera, "Camera");
		if (showCamera) {
			EditorGUI.indentLevel++;
			DrawCameraSettings();
			EditorGUI.indentLevel--;
		}
		EditorGUILayout.EndFoldoutHeaderGroup();

		EditorGUILayout.Space(10);

		// Taser
		showTaser = EditorGUILayout.BeginFoldoutHeaderGroup(showTaser, "Taser");
		if (showTaser) {
			EditorGUI.indentLevel++;
			DrawTaserSettings();
			EditorGUI.indentLevel--;
		}
		EditorGUILayout.EndFoldoutHeaderGroup();

		EditorGUILayout.Space(15);
		if (GUILayout.Button("Restore all to defaults", GUILayout.Height(28))) {
			RestoreAllDefaults();
		}
		EditorGUILayout.Space(15);

		serializedObject.ApplyModifiedProperties();

		EditorGUILayout.HelpBox(Vectorier.SettingsHelpers.ParseHelpBoxString(
			"These affect all levels, use with caution."
		), MessageType.Warning);

		// not true apparently?
		/*
		EditorGUILayout.HelpBox(Vectorier.SettingsHelpers.ParseHelpBoxString(
			"Once saved, moves file loses all comments, making it unmaintainable!"
		), MessageType.Error);
		*/
	}

	private void DrawCameraSettings() {
		DrawFloatFieldWithRevert("Min Zoom", null, ref manager.Camera.ZoomMin, 0.1f, manager.Camera.ZoomMax, "Camera.ZoomMin");
		DrawFloatFieldWithRevert("Max Zoom", null, ref manager.Camera.ZoomMax, manager.Camera.ZoomMin, 1.3f, "Camera.ZoomMax");
		DrawFloatFieldWithRevert("Current Zoom", null, ref manager.Camera.ZoomCurrent, manager.Camera.ZoomMin, manager.Camera.ZoomMax, "Camera.ZoomCurrent");
		DrawFloatFieldWithRevert("Max Speed", null, ref manager.Camera.MaxSpeed, 1f, 200f, "Camera.MaxSpeed");
		DrawFloatFieldWithRevert("Fluency", null, ref manager.Camera.Fluency, 0f, 10f, "Camera.Fluency");
	}

	private void DrawTaserSettings() {
		DrawFloatFieldWithRevert("Distance", null, ref manager.Taser.Distance, 100f, 250f, "Taser.Distance");
		DrawFloatFieldWithRevert("Time", null, ref manager.Taser.Time, 0.1f, 1.0f, "Taser.Time");
		DrawFloatFieldWithRevert("Height Factor", null, ref manager.Taser.HeightFactor, 1f, 3f, "Taser.HeightFactor");
	}

	private void DrawFloatFieldWithRevert(
		string label,
		string tooltip,
		ref float value,
		float min,
		float max,
		string key
	) {
		EditorGUILayout.BeginHorizontal();

		// Create GUIContent with optional tooltip
		GUIContent content = new GUIContent(label, tooltip);

		// Draw slider with tooltip
		value = EditorGUILayout.Slider(content, value, min, max);

		if (GUILayout.Button(new GUIContent("↺", "Revert to default"), GUILayout.Width(22))) {
			value = MovesManager.DefaultValues[key];
		}

		EditorGUILayout.EndHorizontal();
	}

	private void RestoreAllDefaults() {
		manager.Camera.ZoomMin = MovesManager.DefaultValues["Camera.ZoomMin"];
		manager.Camera.ZoomMax = MovesManager.DefaultValues["Camera.ZoomMax"];
		manager.Camera.ZoomCurrent = MovesManager.DefaultValues["Camera.ZoomCurrent"];
		manager.Camera.MaxSpeed = MovesManager.DefaultValues["Camera.MaxSpeed"];
		manager.Camera.Fluency = MovesManager.DefaultValues["Camera.Fluency"];

		manager.Taser.Distance = MovesManager.DefaultValues["Taser.Distance"];
		manager.Taser.Time = MovesManager.DefaultValues["Taser.Time"];
		manager.Taser.HeightFactor = MovesManager.DefaultValues["Taser.HeightFactor"];

		EditorUtility.SetDirty(manager);
	}
}