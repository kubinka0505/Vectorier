using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// -=-=-=- //

public class MoveToCenterWindow : EditorWindow {
	private GameObject objectTarget;
	private GameObject objectReference;

	private bool awaitingReference = true;

	public static readonly int height = 60;

	// Axis selection
	private bool moveX = true;
	private bool moveY = true;
	private bool moveZ = false;

	// Lock states
	private bool lockReference = false;
	private bool lockTarget = false;

	public static readonly int lockWidth = 25;
	public static readonly string lockString = "⃠";

	private GUIStyle centeredButtonStyle;

	[MenuItem("Vectorier/⚙ Utils/✥ Objects positioning/▦ Centerer &Z")]
	public static void ShowWindow() {
		MoveToCenterWindow window = GetWindow<MoveToCenterWindow>();
		window.titleContent = new GUIContent("VectorierUtils - Centerer");
		window.minSize = new Vector2(340, 201);
		window.maxSize = new Vector2(400, 201 + (height * 3));
		window.SubscribeSelection();
	}

	// Cached keys array
	private Dictionary<string, Vector2> presetDict;
	private string[] presetKeys;

	// Pivot and Placement control
	private float pivotPointX = 0.5f;
	private float pivotPointY = 0.5f;
	private float placePointX = 0.5f;
	private float placePointY = 0.5f;

	private int pivotPresetIndex = -1;
	private int placePresetIndex = -1;

	private void OnEnable() {
		// Initialize preset dictionary and keys
		presetDict = new Dictionary<string, Vector2>() {
			{"Top Left", new Vector2(0, 1)},
			{"Top Center", new Vector2(0.5f, 1)},
			{"Top Right", new Vector2(1, 1)},
			{"Center Right", new Vector2(1, 0.5f)},
			{"Center", new Vector2(0.5f, 0.5f)},
			{"Bottom Left", new Vector2(0, 0)},
			{"Bottom Center", new Vector2(0.5f, 0)},
			{"Bottom Right", new Vector2(1, 0)}
		};
		presetKeys = new List<string>(presetDict.Keys).ToArray();

		// Default presets (set to 1 = "Top Center")
		pivotPresetIndex = 1;
		placePresetIndex = 1;

		// Apply preset values immediately
		string pivotKey = presetKeys[pivotPresetIndex];
		Vector2 pivotVal = presetDict[pivotKey];
		pivotPointX = pivotVal.x;
		pivotPointY = pivotVal.y;

		string placeKey = presetKeys[placePresetIndex];
		Vector2 placeVal = presetDict[placeKey];
		placePointX = placeVal.x;
		placePointY = placeVal.y;
	}

	private void OnDisable() => UnsubscribeSelection();

	private void SubscribeSelection() {
		Selection.selectionChanged -= OnSelectionChanged;
		Selection.selectionChanged += OnSelectionChanged;
	}

	private void UnsubscribeSelection() {
		Selection.selectionChanged -= OnSelectionChanged;
	}

	private void OnSelectionChanged() {
		GameObject selected = Selection.activeGameObject;

		if (lockReference && lockTarget) return;

		if (selected == null) {
			if (!lockReference) {
				objectReference = null;
			}

			if (!lockTarget) {
				objectTarget = null;
			}

			awaitingReference = true;
			Repaint();
			return;
		}

		if (!lockReference && lockTarget) {
			objectReference = selected;
		} else if (lockReference && !lockTarget) {
			objectTarget = selected;
		} else if (!lockReference && !lockTarget) {
			if (awaitingReference) {
				objectReference = selected;
				awaitingReference = false;
			} else {
				if (selected == objectReference) {
					return;
				}
				objectTarget = selected;
				awaitingReference = true;
			}
		}

		Repaint();
	}

	private void OnGUI() {
		centeredButtonStyle = new GUIStyle(GUI.skin.button) {
			alignment = TextAnchor.MiddleCenter
		};

		float step = 0.01f; // slider step

		// --------------------------
		// Reference field + lock
		// --------------------------
		EditorGUILayout.BeginHorizontal();
		objectReference = (GameObject)EditorGUILayout.ObjectField("Reference", objectReference, typeof(GameObject), true);
		bool newLockRef = GUILayout.Toggle(lockReference, lockString, centeredButtonStyle, GUILayout.Width(lockWidth));

		if (newLockRef != lockReference) {
			lockReference = newLockRef;

			if (lockReference && lockTarget) {
				lockReference = false;
				Debug.LogWarning("Both reference and target cannot be locked at once.");
			}
		}
		EditorGUILayout.EndHorizontal();

		// --------------------------
		// Target field + lock
		// --------------------------
		EditorGUILayout.BeginHorizontal();
		objectTarget = (GameObject)EditorGUILayout.ObjectField("Target", objectTarget, typeof(GameObject), true);
		bool newLockTarget = GUILayout.Toggle(lockTarget, lockString, centeredButtonStyle, GUILayout.Width(lockWidth));

		if (newLockTarget != lockTarget) {
			lockTarget = newLockTarget;

			if (lockTarget && lockReference) {
				lockTarget = false;
				Debug.LogWarning("Both target and reference cannot be locked at once.");
			}
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();

		// --------------------------
		// Placement section
		// --------------------------
		EditorGUILayout.LabelField("Placement (Target Point)", EditorStyles.boldLabel);

		placePointX = EditorGUILayout.Slider("Place X", placePointX, 0f, 1f);
		placePointX = Mathf.Round(placePointX / step) * step;

		placePointY = EditorGUILayout.Slider("Place Y", placePointY, 0f, 1f);
		placePointY = Mathf.Round(placePointY / step) * step;

		// Placement dropdown
		int newPlaceIndex = EditorGUILayout.Popup("Preset", placePresetIndex, presetKeys);
		if (newPlaceIndex != placePresetIndex) {
			placePresetIndex = newPlaceIndex;
			string key = presetKeys[placePresetIndex];

			Vector2 val = presetDict[key];
			placePointX = val.x;
			placePointY = val.y;
		}

		EditorGUILayout.Space();

		// --------------------------
		// Pivot section
		// --------------------------
		EditorGUILayout.LabelField("Pivot (Reference Point)", EditorStyles.boldLabel);

		pivotPointX = EditorGUILayout.Slider("Pivot X (flipped)", pivotPointX, 0f, 1f);
		pivotPointX = Mathf.Round(pivotPointX / step) * step;

		pivotPointY = EditorGUILayout.Slider("Pivot Y (flipped)", pivotPointY, 0f, 1f);
		pivotPointY = Mathf.Round(pivotPointY / step) * step;

		// Pivot dropdown
		int newPivotIndex = EditorGUILayout.Popup("Preset", pivotPresetIndex, presetKeys);
		if (newPivotIndex != pivotPresetIndex) {
			pivotPresetIndex = newPivotIndex;
			string key = presetKeys[pivotPresetIndex];
			Vector2 val = presetDict[key];

			pivotPointX = 1f - val.x; // flip X
			pivotPointY = 1f - val.y; // flip Y
		}

		EditorGUILayout.Space();

		// --------------------------
		// Axes selection
		// --------------------------
		EditorGUILayout.LabelField("Axes", EditorStyles.boldLabel);
		EditorGUILayout.BeginHorizontal();

		moveX = GUILayout.Toggle(moveX, "X", "Button", GUILayout.Height(height / 2));
		moveY = GUILayout.Toggle(moveY, "Y", "Button", GUILayout.Height(height / 2));
		moveZ = GUILayout.Toggle(moveZ, "Z", "Button", GUILayout.Height(height / 2));

		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();

		// --------------------------
		// Buttons: Move, Swap, Reset
		// --------------------------
		if (GUILayout.Button("▣ Move", GUILayout.Height(height / 2))) {
			if (!moveX && !moveY && !moveZ) {
				Debug.LogError("No axes selected!");
			} else {
				MoveObjectToCenter();
			}
		}

		if (GUILayout.Button("⇄ Swap", GUILayout.Height(height / 2))) {
			SwapObjects();
		}

		if (GUILayout.Button("↺ Reset", GUILayout.Height(height / 2))) {
			if (!lockReference) {
				objectReference = null;
			}

			if (!lockTarget) {
				objectTarget = null;
			}

			awaitingReference = true;
		}
	}

	private void MoveObjectToCenter() {
		if (objectTarget == null || objectReference == null) {
			Debug.LogWarning("Both objects must be assigned.");
			return;
		}

		Bounds targetBounds = CalculateBounds(objectTarget);
		Bounds referenceBounds = CalculateBounds(objectReference);

		float pivotXFlipped = 1f - pivotPointX; // flip for internal calculation
		float pivotYFlipped = 1f - pivotPointY; // flip for internal calculation

		Vector3 targetPos = new Vector3(
			Mathf.Lerp(targetBounds.min.x, targetBounds.max.x, placePointX),
			Mathf.Lerp(targetBounds.min.y, targetBounds.max.y, placePointY),
			targetBounds.center.z
		);

		Vector3 referencePivotPos = new Vector3(
			Mathf.Lerp(referenceBounds.min.x, referenceBounds.max.x, pivotXFlipped),
			Mathf.Lerp(referenceBounds.min.y, referenceBounds.max.y, pivotYFlipped),
			referenceBounds.center.z
		);

		Vector3 offset = targetPos - referencePivotPos;
		Vector3 newPos = objectReference.transform.position + offset;

		if (!moveX) {
			newPos.x = objectReference.transform.position.x;
		}

		if (!moveY) {
			newPos.y = objectReference.transform.position.y;
		}

		if (!moveZ) {
			newPos.z = objectReference.transform.position.z;
		}

		Undo.RecordObject(objectReference.transform, "Move Object To Center");
		objectReference.transform.position = newPos;
	}

	private void SwapObjects() {
		GameObject temp = objectTarget;
		objectTarget = objectReference;
		objectReference = temp;

		Repaint();
	}

	private Bounds CalculateBounds(GameObject go) {
		Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
		if (renderers.Length == 0) {
			return new Bounds(go.transform.position, Vector3.zero);
		}

		Bounds bounds = renderers[0].bounds;

		for (int i = 1; i < renderers.Length; i++) {
			bounds.Encapsulate(renderers[i].bounds);
		}

		return bounds;
	}
}