using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEditor.SceneManagement;
using VectorierUtils;

public class AlignmentWindow : EditorWindow {
	[MenuItem("Vectorier/⚙ Utils/✥ Objects positioning/Alignment window", false, 200)]
	public static void ShowWindow() {
		AlignmentWindow window = GetWindow<AlignmentWindow>("VectorierUtils - Alignment tool");
		window.minSize = new Vector2(400, 350);
		window.maxSize = window.minSize;
	}

	private enum RelativeTo {
		SelectionSize,
		FirstSelected,
		LastSelected,
		SmallestObject,
		LargestObject
	}

	private enum Alignment {
		Left,
		Right,
		Top,
		Bottom,
		HorizontalCenter,
		VerticalCenter
	}

	private enum Distribute {
		Horizontal,
		Vertical
	}

	private enum ExpansionDirectionH {
		LeftToRight,
		RightToLeft
	}

	private enum ExpansionDirectionV {
		TopToBottom,
		BottomToTop
	}

	private enum ExpansionMode {
		Absolute,
		Percentage
	}

	private RelativeTo relativeTo = RelativeTo.SelectionSize;
	private ExpansionDirectionH expansionH = ExpansionDirectionH.LeftToRight;
	private ExpansionDirectionV expansionV = ExpansionDirectionV.TopToBottom;
	private ExpansionMode expansionMode = ExpansionMode.Absolute;

	private float percentageStep = 0.05f; // 5%

	// -=-=-=- //

	private void OnGUI() {
		GUILayout.Label("Align >= 2 selected GameObjects", EditorStyles.boldLabel);
		EditorGUIUtility.labelWidth = 140;

		relativeTo = (RelativeTo)EditorGUILayout.EnumPopup("Relative to:", relativeTo);
		expansionH = (ExpansionDirectionH)EditorGUILayout.EnumPopup("Horizontal expansion:", expansionH);
		expansionV = (ExpansionDirectionV)EditorGUILayout.EnumPopup("Vertical expansion:", expansionV);
		expansionMode = (ExpansionMode)EditorGUILayout.EnumPopup("Expansion mode:", expansionMode);

		EditorGUI.BeginDisabledGroup(expansionMode == ExpansionMode.Absolute);
		percentageStep = EditorGUILayout.Slider("Percentage Step", percentageStep, 0.01f, 0.5f);
		EditorGUI.EndDisabledGroup();

		GUILayout.Space(10);

		GUIStyle buttonStyleMC = new GUIStyle(GUI.skin.button);
		buttonStyleMC.alignment = TextAnchor.MiddleCenter;

		if (GUILayout.Button("⤒ Top", buttonStyleMC)) { AlignSelectedObjects(Alignment.Top); }
		if (GUILayout.Button("⇤ Left", buttonStyleMC)) { AlignSelectedObjects(Alignment.Left); }
		if (GUILayout.Button("⇥ Right", buttonStyleMC)) { AlignSelectedObjects(Alignment.Right); }
		if (GUILayout.Button("⤓ Bottom", buttonStyleMC)) { AlignSelectedObjects(Alignment.Bottom); }
		if (GUILayout.Button("Horizontal Center", buttonStyleMC)) { AlignSelectedObjects(Alignment.HorizontalCenter); }
		if (GUILayout.Button("Vertical Center", buttonStyleMC)) { AlignSelectedObjects(Alignment.VerticalCenter); }

		GUILayout.Space(10);

		// Divider
		Rect rect = EditorGUILayout.GetControlRect(false, 2);
		EditorGUI.DrawRect(rect, Color.gray);
		GUILayout.Space(10);

		GUILayout.Label("Distribute >= 2 selected GameObjects", EditorStyles.boldLabel);
		if (GUILayout.Button("↔ Horizontally", buttonStyleMC)) { DistributeSelectedObjects(Distribute.Horizontal); }
		if (GUILayout.Button("↕ Vertically", buttonStyleMC)) { DistributeSelectedObjects(Distribute.Vertical); }
	}

	private void AlignSelectedObjects(Alignment alignment) {
		GameObject[] selectedObjects = Selection.gameObjects;
		if (selectedObjects.Length == 0) {
			Utils.AdvancedLog("Error", "No objects have been selected for alignment.");
			return;
		}

		Bounds targetBounds = GetTargetBounds(selectedObjects);

		foreach (var obj in selectedObjects) {
			if (obj == null) continue;

			// Record undo BEFORE changing anything
			Undo.RecordObject(obj.transform, "Align Object");

			Vector3 objCenter = GetObjectBounds(obj).center;
			Vector3 objPos = obj.transform.position;

			switch (alignment) {
				case Alignment.Left:
					objPos.x = targetBounds.min.x;
					break;
				case Alignment.Right:
					objPos.x = targetBounds.max.x;
					break;
				case Alignment.Top:
					objPos.y = targetBounds.max.y;
					break;
				case Alignment.Bottom:
					objPos.y = targetBounds.min.y;
					break;
				case Alignment.HorizontalCenter:
					objPos.x += targetBounds.center.x - objCenter.x;
					break;
				case Alignment.VerticalCenter:
					objPos.y += targetBounds.center.y - objCenter.y;
					break;
			}

			// Apply transform change
			obj.transform.position = objPos;

			// Record prefab instance modification (AFTER the change)
			PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);

			// Mark as dirty so Editor updates scene
			EditorUtility.SetDirty(obj);
		}

		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
	}

	private void DistributeSelectedObjects(Distribute distribute) {
		GameObject[] selectedObjects = Selection.gameObjects;
		if (selectedObjects.Length < 2) {
			Utils.AdvancedLog("Error", "At least two objects must be selected for distribution.");
			return;
		}

		if (distribute == Distribute.Horizontal) {
			System.Array.Sort(selectedObjects, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
			if (expansionH == ExpansionDirectionH.RightToLeft)
				System.Array.Reverse(selectedObjects);
		} else {
			System.Array.Sort(selectedObjects, (a, b) => a.transform.position.y.CompareTo(b.transform.position.y));
			if (expansionV == ExpansionDirectionV.BottomToTop)
				System.Array.Reverse(selectedObjects);
		}

		Bounds bounds = GetSelectionBounds(selectedObjects);
		float totalDistance = (distribute == Distribute.Horizontal) ? bounds.size.x : bounds.size.y;
		float gap = expansionMode == ExpansionMode.Absolute ? totalDistance / (selectedObjects.Length - 1) : percentageStep * totalDistance;

		for (int i = 0; i < selectedObjects.Length; i++) {
			RecordUndo(selectedObjects[i]);
			var objPos = selectedObjects[i].transform.position;

			if (distribute == Distribute.Horizontal)
				objPos.x = bounds.min.x + i * gap;
			else
				objPos.y = bounds.min.y + i * gap;

			selectedObjects[i].transform.position = objPos;
			EditorUtility.SetDirty(selectedObjects[i]);
		}

		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
	}

	private void RecordUndo(GameObject obj) {
		if (PrefabUtility.IsPartOfPrefabInstance(obj)) {
			Undo.RecordObject(obj.transform, "Modify Prefab Instance");
			PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
		} else {
			Undo.RecordObject(obj.transform, "Modify Object");
		}
	}

	private Bounds GetTargetBounds(GameObject[] selectedObjects) {
		switch (relativeTo) {
			case RelativeTo.FirstSelected: return GetObjectBounds(selectedObjects[0]);
			case RelativeTo.LastSelected: return GetObjectBounds(selectedObjects[selectedObjects.Length - 1]);
			case RelativeTo.SmallestObject:
				return GetObjectBounds(selectedObjects.OrderBy(o => GetObjectBounds(o).size.magnitude).First());
			case RelativeTo.LargestObject:
				return GetObjectBounds(selectedObjects.OrderByDescending(o => GetObjectBounds(o).size.magnitude).First());
			default: return GetSelectionBounds(selectedObjects);
		}
	}

	private Bounds GetObjectBounds(GameObject obj) {
		// Gather all renderers in this object’s hierarchy (including children)
		Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

		if (renderers.Length > 0) {
			// Start from the first renderer’s bounds
			Bounds combinedBounds = renderers[0].bounds;
			for (int i = 1; i < renderers.Length; i++)
				combinedBounds.Encapsulate(renderers[i].bounds);
			return combinedBounds;
		}

		// If there are no renderers at all, fall back to transform position
		return new Bounds(obj.transform.position, Vector3.zero);
	}

	private Bounds GetSelectionBounds(GameObject[] selectedObjects) {
		if (selectedObjects == null || selectedObjects.Length == 0)
			return new Bounds(Vector3.zero, Vector3.zero);

		// Initialize with the first object's bounds
		Bounds bounds = GetObjectBounds(selectedObjects[0]);

		// Expand the selection bounds to include all objects
		for (int i = 1; i < selectedObjects.Length; i++) {
			if (selectedObjects[i] == null) continue;
			bounds.Encapsulate(GetObjectBounds(selectedObjects[i]));
		}

		return bounds;
	}
}