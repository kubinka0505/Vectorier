using UnityEditor;
using UnityEngine;

using System.Linq;
using System.Globalization;
using System.Collections.Generic;

// -=-=-=- //

public class PathPhaseShifter : EditorWindow {
	private enum PathOrderMode {
		HierarchySiblingIndex,
		AutoNearestNeighborChain
	}

	private PathOrderMode orderMode = PathOrderMode.HierarchySiblingIndex;
	private string lockedIndexes = "0,-1";
	private bool closedLoop = false;
	private bool alignRotationToTangent = false;

	// Curve settings
	private float curveTension = 0.5f;
	private bool showPathGizmo = true;
	private int gizmoResolution = 500;
	private Color gizmoColor = Color.yellow;

	private float phase = 0f; // 0..1, displayed as 0..360 degrees

	private List<Transform> targets = new List<Transform>();
	private List<Vector3> originalPositions = new List<Vector3>();
	private List<float> originalT = new List<float>();

	private static Vector2 minSizeInitial = new Vector2(420, 300);

	[MenuItem("Tools/Path Phase Shifter #&D")]
	public static void ShowWindow() {
		var window = GetWindow<PathPhaseShifter>("Path Phase Shifter");
		window.minSize = minSizeInitial;
	}

	private void OnEnable() {
		minSize = minSizeInitial;
		SceneView.duringSceneGui += OnSceneGUI;
	}

	private void OnDisable() {
		SceneView.duringSceneGui -= OnSceneGUI;
	}

	private void OnGUI() {
		EditorGUI.BeginChangeCheck();

		EditorGUILayout.HelpBox(
			Vectorier.Settings.GUILayoutHelpers.ParseHelpBoxString(
				GetOrderModeDescription(orderMode)
			),
			MessageType.Info
		);

		EditorGUILayout.Space();

		orderMode = (PathOrderMode)EditorGUILayout.EnumPopup("Mode", orderMode);
		//lockedIndexes = Vectorier.Settings.GUILayoutHelpers.TextField("Locked indexes", null, lockedIndexes);

		EditorGUILayout.Space();

		if (EditorGUI.EndChangeCheck()) {
			AutoCaptureSelection();
		}

		closedLoop = EditorGUILayout.Toggle(
			new GUIContent(
				"Closed",
				"On: path wraps seamlessly (good for loops/rings). Off: path is a line with two ends."
			), closedLoop
		);

		/*
		alignRotationToTangent = EditorGUILayout.Toggle(
			new GUIContent(
				"Align Rotation To Path",
				"Optionally rotate each object to face the direction of travel along the curve."
			), alignRotationToTangent
		);
		*/

		curveTension = EditorGUILayout.Slider(
		new GUIContent(
			"Tension",
			"0.5 = standard Catmull-Rom. Lower = straighter/tighter path, higher = more bulge/overshoot near each point."),
			curveTension,
			0f, 1f
		);

		showPathGizmo = EditorGUILayout.Toggle("Show", showPathGizmo);

		using (new EditorGUI.DisabledScope(!showPathGizmo)) {
			gizmoResolution = EditorGUILayout.IntSlider("Resolution", gizmoResolution, 4, 500);
			//gizmoColor = EditorGUILayout.ColorField("Gizmo Color", gizmoColor);
		}

		EditorGUILayout.Space();
		using (new EditorGUILayout.HorizontalScope()) {
			if (GUILayout.Button("Clear", GUILayout.Height(50))) {
				ClearCaptured();
			}
		}

		if (EditorGUI.EndChangeCheck()) {
			SceneView.RepaintAll();
		}

		if (targets == null || targets.Count == 0) {
			return;
		}

		EditorGUI.BeginChangeCheck();
		phase = EditorGUILayout.Slider(
			new GUIContent("φ(" + targets.Count.ToString(CultureInfo.InvariantCulture) + ") = " + (phase * 360f).ToString("F1", CultureInfo.InvariantCulture) + "°"),
			phase,
			0f, 1f
		);

		if (EditorGUI.EndChangeCheck()) {
			ApplyPhase();
			SceneView.RepaintAll();
		}

		EditorGUILayout.Space();
		using (new EditorGUILayout.HorizontalScope()) {
			if (GUILayout.Button("Reset (Phase 0)", GUILayout.Height(50))) {
				phase = 0f;
				ApplyPhase();
				SceneView.RepaintAll();
			}

			if (GUILayout.Button("Re-Capture (use current positions as new base)", GUILayout.Height(50))) {
				AutoCaptureSelection();
				SceneView.RepaintAll();
			}
		}
	}

	private HashSet<int> GetLockedIndexes() {
		HashSet<int> result = new HashSet<int>();

		if (string.IsNullOrWhiteSpace(lockedIndexes)) {
			return result;
		}

		string[] parts = lockedIndexes.Split(',');

		int count = targets.Count;

		foreach (string part in parts) {
			if (!int.TryParse(part.Trim(), out int index)) {
				continue;
			}

			if (index < 0) {
				index = count + index;
			}

			if (index >= 0 && index < count) {
				result.Add(index);
			}
		}

		return result;
	}

	private string GetOrderModeDescription(PathOrderMode mode) {
		switch (mode) {
			case PathOrderMode.HierarchySiblingIndex:
				return "Orders objects by their sibling index in the Hierarchy.";
			case PathOrderMode.AutoNearestNeighborChain:
				return "Infers an order automatically by starting from one end of the shape and repeatedly proceeding to the nearest unvisited object.";
			default:
				return string.Empty;
		}
	}

	// Capture
	private void ClearCaptured() {
		targets.Clear();
		originalPositions.Clear();
		originalT.Clear();
		phase = 0f;
	}

	private void AutoCaptureSelection() {
		var selected = Selection.transforms;

		if (selected == null || selected.Length < 2) {
			ClearCaptured();
			return;
		}

		targets = selected.ToList();

		if (orderMode == PathOrderMode.HierarchySiblingIndex) {
			targets = targets
				.OrderBy(t => t.parent ? t.parent.GetInstanceID() : 0)
				.ThenBy(t => t.GetSiblingIndex())
				.ToList();
		} else {
			var order = BuildNearestNeighborOrder(
				targets.Select(t => t.position).ToList()
			);

			targets = order.Select(i => targets[i]).ToList();
		}

		originalPositions = targets
			.Select(t => t.position)
			.ToList();

		originalT.Clear();

		int n = targets.Count;

		for (int i = 0; i < n; i++) {
			// never place an object exactly at t=1
			originalT.Add((float)i / n);
		}

		phase = 0f;
	}

	// Greedy nearest-neighbor chain: finds the two farthest-apart points to use
	// as a likely "end" of the path, then walks to the closest unvisited point
	// each step. Not guaranteed optimal, but works well for coin-like arrangements.
	private List<int> BuildNearestNeighborOrder(List<Vector3> pts) {
		int n = pts.Count;
		var order = new List<int>();

		if (n == 0) {
			return order;
		}

		if (n <= 2) {
			for (int i = 0; i < n; i++) order.Add(i);
			return order;
		}

		int startIdx = 0;
		float maxDist = -1f;
		for (int i = 0; i < n; i++) {
			for (int j = i + 1; j < n; j++) {
				float d = Vector3.Distance(pts[i], pts[j]);

				if (d > maxDist) {
					maxDist = d;
					startIdx = i;
				}
			}
		}

		bool[] visited = new bool[n];
		order.Add(startIdx);
		visited[startIdx] = true;

		for (int step = 1; step < n; step++) {
			int last = order[order.Count - 1];
			int best = -1;
			float bestDist = float.MaxValue;

			for (int i = 0; i < n; i++) {
				if (visited[i]) {
					continue;
				}

				float d = Vector3.Distance(pts[last], pts[i]);

				if (d < bestDist) {
					bestDist = d;
					best = i;
				}
			}

			order.Add(best);
			visited[best] = true;
		}

		return order;
	}

	// Apply
	private void ApplyPhase() {
		if (targets == null || targets.Count == 0) {
			return;
		}

		var locked = GetLockedIndexes();

		var validTargets = targets
			.Where(t => t != null)
			.ToArray();

		if (validTargets.Length == 0) {
			return;
		}

		Undo.RecordObjects(validTargets, "Shift Path Phase");

		int n = targets.Count;

		for (int i = 0; i < n; i++)  {
			if (targets[i] == null) {
				continue;
			}

			// Circular movement around the path
			float newT = Mathf.Repeat(
				originalT[i] + phase,
				1f
			);

			Vector3 newPos = EvaluateCurve(newT);

			targets[i].position = newPos;

			if (alignRotationToTangent) {
				Vector3 a = EvaluateCurve(
					Mathf.Repeat(newT - Mathf.Epsilon, 1f)
				);

				Vector3 b = EvaluateCurve(
					Mathf.Repeat(newT + Mathf.Epsilon, 1f)
				);

				Vector3 tangent = b - a;

				if (tangent.sqrMagnitude > Mathf.Epsilon) {
					targets[i].rotation =
						Quaternion.LookRotation(
							tangent.normalized,
							Vector3.up
						);
				}
			}

			EditorUtility.SetDirty(targets[i]);
		}
	}

	// Curve (Hermite / Catmull-Rom through the captured points, with tension)
	private Vector3 EvaluateCurve(float t) {
		int n = originalPositions.Count;

		if (n == 0) {
			return Vector3.zero;
		}

		if (n == 1) {
			return originalPositions[0];
		}

		if (closedLoop) {
			t = Mathf.Repeat(t, 1f);
		} else {
			t = Mathf.Clamp01(t);
		}

		int segments = closedLoop ? n : n - 1;

		float scaledT = t * segments;

		int seg = Mathf.Clamp(
			Mathf.FloorToInt(scaledT),
			0,
			segments - 1
		);

		float local = scaledT - seg;

		Vector3 p0 = GetPoint(seg - 1);
		Vector3 p1 = GetPoint(seg);
		Vector3 p2 = GetPoint(seg + 1);
		Vector3 p3 = GetPoint(seg + 2);

		return HermiteCatmullRom(
			p0,
			p1,
			p2,
			p3,
			local,
			curveTension
		);
	}

	private Vector3 GetPoint(int i) {
		int n = originalPositions.Count;

		if (closedLoop) { 
			i = ((i % n) + n) % n;
			return originalPositions[i];
		} else {
			i = Mathf.Clamp(i, 0, n - 1);
			return originalPositions[i];
		}
	}

	private Vector3 HermiteCatmullRom(
		Vector3 p0,
		Vector3 p1,
		Vector3 p2,
		Vector3 p3,

		float s,
		float tension
	) {
		Vector3 m1 = tension * (p2 - p0);
		Vector3 m2 = tension * (p3 - p1);

		float s2 = s * s;
		float s3 = s2 * s;

		float h00 = 2f * s3 - 3f * s2 + 1f;
		float h10 = s3 - 2f * s2 + s;
		float h01 = -2f * s3 + 3f * s2;
		float h11 = s3 - s2;

		return h00 * p1 + h10 * m1 + h01 * p2 + h11 * m2;
	}

	private void OnSelectionChange() {
		AutoCaptureSelection();
		Repaint();
		SceneView.RepaintAll();
	}

	// Scene view preview
	private void OnSceneGUI(SceneView view) {
		if (!showPathGizmo || originalPositions == null || originalPositions.Count < 2) {
			return;
		}

		Handles.color = gizmoColor;
		int steps = Mathf.Max(4, gizmoResolution);

		Vector3 prev = EvaluateCurve(0f);
		for (int i = 1; i <= steps; i++) {
			float t = (float)i / steps;

			Vector3 cur = EvaluateCurve(t);
			Handles.DrawLine(prev, cur);
			prev = cur;
		}

		Handles.color = Color.white;
		foreach (var p in originalPositions) {
			float size = HandleUtility.GetHandleSize(p) * 0.05f;
			Handles.SphereHandleCap(0, p, Quaternion.identity, size, EventType.Repaint);
		}
	}
}