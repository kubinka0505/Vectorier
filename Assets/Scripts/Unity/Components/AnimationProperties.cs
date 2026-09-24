using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// -=-=-=- // 

[AddComponentMenu("Vectorier/Animation Properties")]
public class AnimationProperties : MonoBehaviour {
	public float Width;
	public float Height;

	public bool PlayOnce;

	public float DirectionX = 0f;
	public float DirectionY = 0f;

	public float AccelerationX = 0f;
	public float AccelerationY = 0f;

	[Tooltip("Time for which animation plays, in seconds. -1 for 32767.")]
	public float Time = -1;

	public void OnEnable() {}
}

#if UNITY_EDITOR

[CustomEditor(typeof(AnimationProperties))]
public class AnimationPropertiesEditor : Editor {
	public override void OnInspectorGUI() {
		serializedObject.Update();

		EditorGUILayout.PropertyField(serializedObject.FindProperty("Width"));
		EditorGUILayout.PropertyField(serializedObject.FindProperty("Height"));

		EditorGUILayout.Space(10);

		EditorGUILayout.PropertyField(serializedObject.FindProperty("Time"));
		EditorGUILayout.PropertyField(serializedObject.FindProperty("PlayOnce"));

		EditorGUILayout.Space(10);

		// Direction
		EditorGUILayout.BeginHorizontal();

		EditorGUILayout.PrefixLabel("Direction");

		EditorGUILayout.LabelField("X", GUILayout.Width(10));
		EditorGUILayout.PropertyField(
			serializedObject.FindProperty("DirectionX"),
			GUIContent.none
		);

		EditorGUILayout.LabelField("Y", GUILayout.Width(10));
		EditorGUILayout.PropertyField(
			serializedObject.FindProperty("DirectionY"),
			GUIContent.none
		);

		EditorGUILayout.EndHorizontal();

		// Acceleration
		EditorGUILayout.BeginHorizontal();

		EditorGUILayout.PrefixLabel("Acceleration");

		EditorGUILayout.LabelField("X", GUILayout.Width(10));
		EditorGUILayout.PropertyField(
			serializedObject.FindProperty("AccelerationX"),
			GUIContent.none
		);

		EditorGUILayout.LabelField("Y", GUILayout.Width(10));
		EditorGUILayout.PropertyField(
			serializedObject.FindProperty("AccelerationY"),
			GUIContent.none
		);

		EditorGUILayout.EndHorizontal();

		serializedObject.ApplyModifiedProperties();
	}
}

#endif