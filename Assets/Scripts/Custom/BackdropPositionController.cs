using UnityEditor;
using UnityEngine;

// -=-=-=- //

[ExecuteAlways]
public class BackdropPositionController : MonoBehaviour {
	private Transform backdropTransform;
	private Vector3 initialBackdropPosition;
	private Vector3 initialBackdropScale; // Store the initial scale
	private Renderer objectRenderer;
	private float divisionFactor = 2f; // Default factor
	private bool isDragging = false;
	private Vector3 dragStartPosition;
	private Vector3 initialDragPosition;

	private void OnEnable() {
		EditorApplication.update += OnEditorUpdate;
		SceneView.duringSceneGui += OnSceneGUI;
		ValidateBackdrop();
	}

	private void OnDisable() {
		EditorApplication.update -= OnEditorUpdate;
		SceneView.duringSceneGui -= OnSceneGUI;

		// Restore the initial position and scale when the script is disabled
		RestoreInitialTransform();
	}

	private void OnEditorUpdate() {
		if (backdropTransform == null || !backdropTransform.CompareTag("Backdrop")) {
			RestoreInitialTransform();
			backdropTransform = null;
			return;
		}

		// Only update position when not dragging
		if (!isDragging) {
			UpdatePositionAndScaleBasedOnCamera();
		}
	}

	private void UpdatePositionAndScaleBasedOnCamera() {
		UpdateDivisionFactor();

		// Get the Scene View camera position
		SceneView sceneView = SceneView.lastActiveSceneView;
		if (sceneView != null && sceneView.camera != null) {
			Vector3 sceneViewCameraPosition = sceneView.camera.transform.position;

			// Calculate the offset based on the initial position and division factor
			Vector3 offset = sceneViewCameraPosition / divisionFactor;

			// Preserve the original Z position
			backdropTransform.position = new Vector3(
				initialBackdropPosition.x + offset.x,
				initialBackdropPosition.y + offset.y,
				initialBackdropPosition.z
			);
		}

		// Adjust the scale based on the division factor
		if (backdropTransform != null) {
			// Smaller divisionFactor -> Larger scale
			float scaleFactor = (1 / divisionFactor) + 1;
			backdropTransform.localScale = initialBackdropScale * scaleFactor;
		}
	}

	private void UpdateDivisionFactor()
	{
		if (objectRenderer != null) {
			string sortingLayerName = objectRenderer.sortingLayerName;

			switch (sortingLayerName) {
				case "Factor_0.5":
					divisionFactor = 2f;
					break;
				case "Factor_0.25":
					divisionFactor = 1.5f;
					break;
				case "Factor_0.05":
					divisionFactor = 1.05f;
					break;
				default:
					divisionFactor = 2f;
					break;
			}
		}
	}

	private void ValidateBackdrop() {
		if (!CompareTag("Backdrop")) {
			Debug.LogError("This script must be attached to a GameObject with the 'Backdrop' tag.");
			enabled = false;
		} else {
			backdropTransform = transform;

			// Save the original position and scale of the backdrop
			initialBackdropPosition = backdropTransform.position;
			initialBackdropScale = backdropTransform.localScale;

			// Renderer component check
			objectRenderer = GetComponent<Renderer>();
			if (objectRenderer == null)
			{
				Debug.LogError("This GameObject must have a Renderer component to check the sorting layer.");
				enabled = false;
			}
		}
	}

	private void RestoreInitialTransform() {
		if (backdropTransform != null) {
			backdropTransform.position = initialBackdropPosition;
			backdropTransform.localScale = initialBackdropScale;
		}
	}

	private void OnSceneGUI(SceneView sceneView) {
		if (backdropTransform == null) {
			return;
		}

		// Handle mouse interaction for dragging
		Event e = Event.current;

		if (e.type == EventType.MouseDown && e.button == 0 && HandleUtility.nearestControl == 0) {
			// Start dragging
			isDragging = true;
			dragStartPosition = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
			initialDragPosition = backdropTransform.position;
			e.Use();
		} else if (e.type == EventType.MouseDrag && isDragging) {
			// During dragging
			Vector3 currentMousePosition = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
			Vector3 delta = currentMousePosition - dragStartPosition;

			backdropTransform.position = initialDragPosition + new Vector3(delta.x, delta.y, 0); // Lock Z-axis
			e.Use();
		} else if (e.type == EventType.MouseUp && e.button == 0 && isDragging) {
			// Stop dragging
			isDragging = false;

			// Update the initial position after dragging
			initialBackdropPosition = backdropTransform.position;
			e.Use();
		}

		// Draw a draggable handle
		EditorGUI.BeginChangeCheck();
		Vector3 newPosition = Handles.PositionHandle(backdropTransform.position, Quaternion.identity);
		if (EditorGUI.EndChangeCheck()) {
			Undo.RecordObject(backdropTransform, "Move Backdrop");
			backdropTransform.position = newPosition;

			// Update the initial position when moved manually
			initialBackdropPosition = backdropTransform.position;
		}
	}
}
