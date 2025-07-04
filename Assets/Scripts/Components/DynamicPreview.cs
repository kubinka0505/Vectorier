using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// -=-=-=- //

[AddComponentMenu("Vectorier/DynamicPreview")]
public class DynamicPreview : MonoBehaviour {
	public List<string> DynamicTransformationNames = new List<string>();
	private bool isPreviewRunning = false;

	// isPreviewRunning read-only property
	public bool IsPreviewRunning => isPreviewRunning;

	public void StartPreview() {
		if (isPreviewRunning) {
			Debug.LogWarning("Preview is already running!");
			return;
		}

		// Find all game objects with tag "Dynamic"
		GameObject[] dynamicObjects = GameObject.FindGameObjectsWithTag("Dynamic");
		List<Dynamic> matchedDynamics = new List<Dynamic>();

		foreach (GameObject dynamicObject in dynamicObjects) {
			Dynamic dynamicComponent = dynamicObject.GetComponent<Dynamic>();
			if (dynamicComponent != null && DynamicTransformationNames.Contains(dynamicComponent.TransformationName)) {
				// Set ResetAfterPreviewFinish to false
				dynamicComponent.ResetAfterPreviewFinish = false;
				matchedDynamics.Add(dynamicComponent);
			}
		}

		// Play preview on matched dynamic
		foreach (Dynamic dynamic in matchedDynamics) {
			dynamic.PlayPreview();
		}

		// Disable GUI buttons and wait for preview to finish
		isPreviewRunning = true;
		StartCoroutine(WaitForPreviewToEnd(matchedDynamics));
	}

	public void ResetPreview() {
		if (isPreviewRunning) {
			Debug.LogWarning("Cannot reset preview while it is running!");
			return;
		}

		GameObject[] dynamicObjects = GameObject.FindGameObjectsWithTag("Dynamic");

		foreach (GameObject dynamicObject in dynamicObjects) {
			Dynamic dynamicComponent = dynamicObject.GetComponent<Dynamic>();
			if (dynamicComponent != null && DynamicTransformationNames.Contains(dynamicComponent.TransformationName)) {
				// Reset position
				dynamicComponent.ResetPreviewPosition();
			}
		}
	}

	private System.Collections.IEnumerator WaitForPreviewToEnd(List<Dynamic> dynamics) {
		bool allPreviewsFinished = false;

		while (!allPreviewsFinished) {
			allPreviewsFinished = true;

			foreach (Dynamic dynamic in dynamics) {
				if (dynamic.IsPreviewDisabled) {
					allPreviewsFinished = false;
					break;
				}
			}

			// Wait for the next frame
			yield return null;
		}

		// Re-enable ResetAfterPreviewFinish and GUI buttons
		foreach (Dynamic dynamic in dynamics) {
			dynamic.ResetAfterPreviewFinish = true;
		}

		isPreviewRunning = false;
		Debug.Log("All previews finished.");
	}
}


[CustomEditor(typeof(DynamicPreview))]
[CanEditMultipleObjects]
public class DynamicPreviewEditor : Editor {
	public override void OnInspectorGUI() {
		if (serializedObject.isEditingMultipleObjects) {
			EditorGUILayout.HelpBox("Multi-object editing is not supported for DynamicPreview.", MessageType.Warning);
			return;
		}

		DrawDefaultInspector();
		DynamicPreview dynamicPreview = (DynamicPreview)target;

		// Disable GUI buttons if a preview is running
		GUI.enabled = !dynamicPreview.IsPreviewRunning;

		if (GUILayout.Button("Start Preview")) {
			dynamicPreview.StartPreview();
		}

		if (GUILayout.Button("Reset Preview")) {
			dynamicPreview.ResetPreview();
		}

		// Re-enable GUI for other controls
		GUI.enabled = true;
	}
}