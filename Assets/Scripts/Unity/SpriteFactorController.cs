using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;

// -=-=-=- //
// Idea by elrealsincoma

[ExecuteAlways]
// [RequireComponent(typeof(Camera))]
public class SpriteFactorController : MonoBehaviour {
	private Camera _camera;

	// parallax objects tracked at runtime
	private class ParallaxObject {
		public Transform transform;
		public float factor;
		public Vector3 originalPosition;
		public Vector3 originalScale;
	}

	private List<ParallaxObject> parallaxObjects = new List<ParallaxObject>();

	private bool isRunning = false;
	private Vector3 initialCameraPosition;

	// value used in calculations
	private static readonly float v_factor = 0.5f;

	// allowed tags (case-insensitive)
	private static readonly HashSet<string> AllowedTags = new HashSet<string> {
		"image", "images", "backdrop"
	};

	private void Awake() {
		_camera = GetComponent<Camera>();

#if UNITY_EDITOR
		if (_camera == null) {
			EditorUtility.DisplayDialog(
				"Missing Camera Component",
				"This component requires a Camera component on this GameObject. The script will be disabled.", // bugged
				"OK"
			);
			enabled = false;
			return;
		}
#else
		if (_camera == null) {
			Debug.LogError("This component requires a Camera component on this GameObject.");
			enabled = false;
			return;
		}
#endif
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(SpriteFactorController))]
	public class SpriteFactorControllerEditor : Editor {
		public override void OnInspectorGUI() {
			DrawDefaultInspector();
			SpriteFactorController controller = (SpriteFactorController)target;

			GUILayout.Space(6);
			if (GUILayout.Button(controller.isRunning ? "Stop Parallax" : "Start Parallax")) {
				if (controller.isRunning) {
					controller.StopParallax();
				} else {
					controller.StartParallax();
				}
			}
		}
	}
#endif

	public void StartParallax() {
		parallaxObjects.Clear();
		initialCameraPosition = _camera.transform.position;

		// find all SpriteRenderer objects in the scene (including inactive)
		SpriteRenderer[] renderers = FindObjectsOfType<SpriteRenderer>(true);

		foreach (var renderer in renderers) {
			if (renderer == null || renderer.gameObject == null) {
				continue;
			}

			// only process allowed tags
			string tagLower = renderer.gameObject.tag.ToLowerInvariant();
			if (!AllowedTags.Contains(tagLower)) {
				continue;
			}

			// pass tag to parser to handle "Backdrop + Default"
			float? factor = ParseFactorFromSortingLayer(renderer.sortingLayerName, tagLower);
			if (factor == null) {
				continue;
			}

			parallaxObjects.Add(new ParallaxObject {
				transform = renderer.transform,
				factor = factor.Value,
				originalPosition = renderer.transform.position,
				originalScale = renderer.transform.localScale
			});
		}

		if (parallaxObjects.Count == 0) {
			Debug.LogWarning("No parallax objects found with allowed tags and Factor_* sorting layers.");
		} else {
			Debug.Log($"Registered {parallaxObjects.Count} parallax objects.");
		}

		isRunning = true;
	}

	// restore camera and all registered objects to their original position + scale
	public void StopParallax() {
		if (!isRunning) {
			return;
		}

		if (_camera != null) {
			_camera.transform.position = initialCameraPosition;
		}

		foreach (var obj in parallaxObjects) {
			if (obj.transform == null) {
				continue;
			}

			obj.transform.position = obj.originalPosition;
			obj.transform.localScale = obj.originalScale;
		}

		parallaxObjects.Clear();
		isRunning = false;

		Debug.Log("Parallax stopped and objects restored.");
	}

	// -=-=-=- //
	// Main

	private void Update() {
		if (!isRunning) {
			return;
		}

		if (_camera == null) {
			return;
		}

		// p_pos?
		Vector3 cameraDelta = _camera.transform.position - initialCameraPosition;

		foreach (var obj in parallaxObjects) {
			if (obj.transform == null) {
				continue;
			}

			// VisualContainer style scale calculation
			// rounded to 1 decimal
			float _Scale = Round(1f / ((1f / v_factor - 1f) * obj.factor + 1f), v_factor, 10f);
			float FrameScale = _Scale * 2f;

			// scale relative to original scale
			obj.transform.localScale = obj.originalScale * FrameScale;

			// parallax move
			// opposite to cameraDelta, using factor * FrameScale
			Vector3 pos = obj.originalPosition;
			pos.x += -(cameraDelta.x * obj.factor * FrameScale);
			pos.y += -(cameraDelta.y * obj.factor * FrameScale);
			obj.transform.position = pos;
		}
	}

	// -=-=-=- //
	// Helpers

	// parse numeric factor from sorting layer name, e.g. "Factor_1.125" -> 1.125
	// fallback: Default -> 0.5
	private static float? ParseFactorFromSortingLayer(string sortingLayerName, string tagLower) {
		if (string.IsNullOrEmpty(sortingLayerName)) {
			return null;
		}

		string nameLower = sortingLayerName.ToLowerInvariant();

		// backdrop on Default layer -> factor 0.5
		if (tagLower == "backdrop" && nameLower == "default") {
			return v_factor;
		}

		// only process layers that start with "factor_"
		if (!nameLower.StartsWith("factor_")) {
			return null;
		}

		// extract number after "factor_"
		string[] numberParts = nameLower.Split('_');
		string numberPart = numberParts[numberParts.Length - 1];

		if (float.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)) {
			return parsed;
		}

		return null;
	}

	// same algo as original round
	private static float Round(float value, float factor, float pow) {
		return Mathf.Floor(value * pow + factor) / pow;
	}
}