using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

// -=-=-=- //

[AddComponentMenu("Vectorier/DynamicRotate")]
public class DynamicRotate : MonoBehaviour {
	[Tooltip("Transformation name of the dynamic object")]
	public string TransformationName = "Transform_name";

	public enum AnchorPoints {
		TopLeft,
		TopRight,
		Center,
		BottomLeft,
		BottomRight
	}

	[Serializable]
	public class TransformationRotate {
		[Tooltip("Rotation duration in seconds.")]
		[FormerlySerializedAs("MoveDuration")]
		public float Duration = 1.5f;

		[Tooltip("Rotation angle in degrees.")]
		public float Angle = 26.56505117707799f;

		[Tooltip("Pivot used for rotation point")]
		public AnchorPoints Anchor = AnchorPoints.TopLeft;

		public bool Linear = false;
	}

	[SerializeField]
	public TransformationRotate Rotation;

	private GameObject previewObject;
	private bool isRotating = false;

	public void SetupPreview() {
		if (previewObject != null) {
			return;
		}

		previewObject = new GameObject("RotatePreview");
		previewObject.transform.SetParent(transform);
		previewObject.transform.localPosition = Vector3.zero;
		previewObject.transform.localRotation = Quaternion.identity;

		SpriteRenderer originalRenderer = GetComponent<SpriteRenderer>();
		if (originalRenderer != null) {
			SpriteRenderer previewRenderer = previewObject.AddComponent<SpriteRenderer>();
			previewRenderer.sprite = originalRenderer.sprite;
			previewRenderer.sortingOrder = originalRenderer.sortingOrder + 1;

			Color color = previewRenderer.color;
			// Set transparency
			color.a = 120f / 255f;
			previewRenderer.color = color;
		}

		isRotating = true;
		EditorUtility.SetDirty(this);
	}

	public void UpdateRotationFromPreview() {
		if (!isRotating || previewObject == null) {
			Debug.LogError("Preview object is missing or rotation is not active");
			return;
		}

		Rotation.Angle = previewObject.transform.eulerAngles.z;

		Debug.Log("Updated Rotation from RotatePreview");

		EditorUtility.SetDirty(this);
		SceneView.RepaintAll();
	}

	public void FinishSetup() {
		if (previewObject != null) {
			DestroyImmediate(previewObject);
		}
		isRotating = false;
		EditorUtility.SetDirty(this);
	}
}

[CustomEditor(typeof(DynamicRotate))]
[CanEditMultipleObjects]
public class DynamicRotateEditor : Editor {
	public override void OnInspectorGUI() {
		if (targets.Length > 1) {
			EditorGUILayout.HelpBox("Multi-object editing is not supported for DynamicRotate.", MessageType.Warning);
			return;
		}

		DrawDefaultInspector();
		DynamicRotate dynamicRotateComponent = (DynamicRotate)target;

		if (GUILayout.Button("Setup Preview")) {
			dynamicRotateComponent.SetupPreview();
		}

		if (GUILayout.Button("Update Rotation From Preview")) {
			dynamicRotateComponent.UpdateRotationFromPreview();
		}

		if (GUILayout.Button("Finish Setup")) {
			dynamicRotateComponent.FinishSetup();
		}

		EditorUtility.SetDirty(dynamicRotateComponent);
	}
}
