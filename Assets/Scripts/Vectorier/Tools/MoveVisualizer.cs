using UnityEditor;
using UnityEngine;

using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Collections.Generic;

using Random = System.Random;

// Idea by FlipThoseTitle
//-=-=-=-//

#nullable enable
#pragma warning disable CS8602 // nullables
#pragma warning disable CS8603 // possible null returns
#pragma warning disable CS8604 // possible null returns

public class MoveVisualizer : EditorWindow {
	// Base
	static string[] NodePoints = Vectorier.Core.Components.Model.ModelHelpers.Skeleton.Nodes.Ordered;

	enum Direction {
		Left,
		Right
	}

	// Visual
	static Color colorRig = new Color(0.8f, 0.8f, 0.8f, 1f);
	static Color colorHandles = Color.red;
	static Color colorHandlesActive = Color.red;
	static Color colorPath = new Color(1f, 0, 0, 0.5f);

	static float handleThickness = 5f;

	// GUI
	DefaultAsset? file;

	string targetNodeName = string.Empty;
	int startFrame = 0;
	float speed = 100f;
	Direction direction = Direction.Right;

	float distinctionThreshold = 0f;

	bool dynamicPath = false;
	bool loop = true;

	bool drawRig = true;
	bool drawPath = true;
	bool drawNodes = true;

	// Core
	bool placementEnabled;

	Dictionary<string, Vector3> animationNodes = new Dictionary<string, Vector3>();
	Dictionary<string, Vector3> previewNodes = new Dictionary<string, Vector3>();
	Dictionary<string, Vector3> previewPose = new Dictionary<string, Vector3>();

	List<Vector3[]> animationFrames = new List<Vector3[]>();
	List<Vector3> targetNodePath = new List<Vector3>();

	double playbackTime;

	int frameIndex = 0;
	int originalFrameCount; // number of frames before resampling

	int targetNodeIndex;
	int visibleTargetNodeCount;

	double lastPlaybackTime;
	float nodeHandleSize = 4f;

	Vector3 startOffset;
	Vector3 pivotOffset;

	bool offsetInitialized;
	bool previewActive;

	//-=-=-=-//

	[MenuItem("Vectorier/Tools/Move Visualizer", false)]
	static void OpenWindow() => GetWindow<MoveVisualizer>("Move Visualizer");

	void OnEnable() {
		SceneView.duringSceneGui += OnSceneGUI;
		EditorApplication.update += UpdatePlayback;

		// Absolute filesystem path
		string startPath = Path.Combine(
			Application.dataPath,
			"XML",
			"dzip",
			"archives",
			"uncompiled",
			"animations"
		);

		// Get all files with the desired extension (e.g. "bin")
		string extension = "." + Vectorier.Core.Game.Extensions.File.Animation;

		if (!Directory.Exists(startPath)) {
			Debug.LogError($"Directory does not exist: {startPath}");
			return;
		}

		string[] files = Directory.GetFiles(startPath, "*" + extension, SearchOption.TopDirectoryOnly);

		if (files.Length == 0) {
			Debug.LogError($"No *{extension} files found in {startPath}");
			return;
		}

		string randomFile = files[UnityEngine.Random.Range(0, files.Length)];
		string assetPath = "Assets" + randomFile.Substring(Application.dataPath.Length).Replace("\\", "/");

		file = AssetDatabase.LoadAssetAtPath<DefaultAsset>(assetPath);

		if (file == null) {
			Debug.LogError($"Failed to load asset at path: {assetPath}");
			return;
		}

		InitializePreviewPose();
	}

	void OnDisable() {
		SceneView.duringSceneGui -= OnSceneGUI;
		EditorApplication.update -= UpdatePlayback;
		ClearAll();
	}

	void OnGUI() {
		EditorGUI.BeginChangeCheck();
		
		GUILayout.Space(5);

		// File
		var newFile = (DefaultAsset)EditorGUILayout.ObjectField(
			new GUIContent("File", "Animation file"),
			file,
			typeof(DefaultAsset),
			false
		);

		if (newFile != file) {
			file = newFile;

			UpdateStartFrameFromFile(Vectorier.Files.Moves);

			if (placementEnabled) {
				OnSettingsChanged();
			}
		}

		// Target node
		if (string.IsNullOrEmpty(targetNodeName)) {
			if (Array.IndexOf(NodePoints, "COM") >= 0) {
				targetNodeName = "COM";
			} else {
				targetNodeName = NodePoints[0];
			}
		}

		int nodeIndex = Array.IndexOf(NodePoints, targetNodeName);
		if (nodeIndex < 0) {
			nodeIndex = 0;
		}

		GUILayout.Space(12);

		nodeIndex = EditorGUILayout.Popup(
			new GUIContent("Node", "Target placement node"),
			nodeIndex,
			NodePoints
		);
		targetNodeName = NodePoints[nodeIndex];

		startFrame = EditorGUILayout.IntField(
			new GUIContent("Start Frame", "Animation start frame index"),
			startFrame
		);

		speed = (float)EditorGUILayout.Slider(
			new GUIContent("Speed", "Target animation speed"),
			speed,
			1f, 150f
		);

		direction = (Direction)EditorGUILayout.EnumPopup(
			new GUIContent("Direction", "Animation direction"),
			direction
		);

		GUILayout.Space(12);

		distinctionThreshold = EditorGUILayout.Slider(
			new GUIContent("Distinction threshold", "The similarity value, that all nodes must meet, in order to remove previous frame"),
			distinctionThreshold,
			0, 100
		);

		GUILayout.Space(12);

		loop = EditorGUILayout.Toggle(
			new GUIContent("Loop", "Loops animation indefinitely"),
			loop
		);

		dynamicPath = EditorGUILayout.Toggle(
			new GUIContent("Dynamic path", "Write path dynamically instead of immediately"),
			dynamicPath
		);

		GUILayout.Space(12);

		GUILayout.Label("Draw");

		drawRig = EditorGUILayout.Toggle(
			new GUIContent("Rig", "Draw pseudo-model"),
			drawRig
		);

		drawNodes = EditorGUILayout.Toggle(
			new GUIContent("Nodes", "Draw pseudo-model"),
			drawNodes
		);

		drawPath = EditorGUILayout.Toggle(
			new GUIContent("Path", "Draw target node path"),
			drawPath
		);

		// Buttons
		GUILayout.Space(12);

		string placementButtonLabel = placementEnabled ? "Stop" : "Start";
		if (GUILayout.Button(
			new GUIContent(placementButtonLabel, "Start or stop placing the animation"),
			GUILayout.Height(40)
		)) {
			placementEnabled = !placementEnabled;
		}

		if (GUILayout.Button(
			new GUIContent("Clear", "Clear all nodes and preview"),
			GUILayout.Height(40)
		)) {
			ClearAll();
		}

		/*
		if (GUILayout.Button(
			new GUIContent("Details", "Get target node non-interpolated frame number and local coordinates."),
			GUILayout.Height(40)
		)) {
			GetNodeDetails();
		}
		*/

		if (placementEnabled) {
			InitializePreviewPose(direction == Direction.Left ? true : false);
		}

		if (EditorGUI.EndChangeCheck()) {
			OnSettingsChanged();
		}
	}

	void OnSceneGUI(SceneView sceneView) {
		if (Event.current.type == EventType.Repaint) {
			SceneView.RepaintAll();
		}

		Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

		if (placementEnabled) {
			Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
			Vector3 worldPosition = ray.origin + ray.direction * 10f;
			worldPosition.z = 0f;

			UpdatePreview(worldPosition);
			previewActive = true;

			if (Event.current.type == EventType.MouseDown && Event.current.button == 0) {
				PlaceAt(
					file: AssetDatabase.GetAssetPath(file),

					pos: worldPosition,
					node: targetNodeName,
					startFrame: startFrame,
					speed: speed,
					left: direction == Direction.Left,

					distinctionThreshold: distinctionThreshold
				);

				previewActive = false;
				Event.current.Use();
			}

			SceneView.RepaintAll();
		}

		if (!drawNodes && !drawRig && !drawPath) {
			Debug.LogWarning("Nothing selected to draw, enabling drawRig by default.");
			drawRig = true;
		}

		if (drawNodes) {
			DrawNodes();
		}

		if (drawRig) {
			DrawRig();
		}

		if (drawPath) {
			DrawPath();
		}
	}

	void OnSettingsChanged() {
		if (!placementEnabled) {
			SceneView.RepaintAll();
			return;
		}

		// Reload animation frames
		if (file != null) {
			UpdateStartFrameFromFile(Vectorier.Files.Moves);

			LoadBin(
				file: AssetDatabase.GetAssetPath(file),
				startFrame: startFrame,
				speed: speed,
				left: direction == Direction.Left,
				distinctionThreshold: distinctionThreshold
			);

			lastPlaybackTime = EditorApplication.timeSinceStartup;

			if (animationFrames.Count > 0 && targetNodeIndex >= 0) {
				PrecomputeTargetNodePath();
				visibleTargetNodeCount = targetNodePath.Count;
			}
		}

		InitializePreviewPose(direction == Direction.Left);

		SceneView.RepaintAll();
		Repaint();
	}

	//-=-=-=-//
	// Core

	void PlaceAt(
		string file,

		Vector3 pos,
		string node,
		int startFrame,
		float speed,
		bool left,

		float distinctionThreshold
	) {
		ClearAll();
		LoadBin(
			file: file,

			startFrame: startFrame,
			speed: speed,
			left: left,

			distinctionThreshold: distinctionThreshold
		);
		InitializeNodes();

		targetNodeIndex = Array.IndexOf(NodePoints, node); // COM
		if (targetNodeIndex < 0) {
			throw new Exception("COM node not found");
		}

		int pivotIndex = Array.IndexOf(NodePoints, node); // NPivot
		Vector3 pivotFrameZero = ConvertAxis(animationFrames[0][targetNodeIndex]);

		startOffset = pos - pivotFrameZero;
		offsetInitialized = true;

		lastPlaybackTime = EditorApplication.timeSinceStartup;
	}

	void PrecomputeTargetNodePath() {
		targetNodePath.Clear();

		// Build the raw path first
		foreach (var frame in animationFrames) {
			Vector3 localTP = ConvertAxis(frame[targetNodeIndex]) + startOffset;
			targetNodePath.Add(localTP);
		}

		// Apply smoothing if desired
		targetNodePath = SmoothPath(targetNodePath, subdivisions: 10);
	}

	void GetNodeDetails() {
		if (animationFrames.Count == 0) {
			Debug.LogWarning("No animation frames loaded.");
			return;
		}

		int nodeIndex = Array.IndexOf(NodePoints, targetNodeName);
		if (nodeIndex < 0) {
			Debug.LogError($"Target node '{targetNodeName}' not found in NodePoints.");
			return;
		}

		// current interpolated frame
		Vector3 interpPos = ConvertAxis(animationFrames[frameIndex][nodeIndex]);
		
		// map to original frame number
		int originalFrameIndex = Mathf.Clamp(
			Mathf.FloorToInt(frameIndex / (speed / 100f)), 
			0, 
			originalFrameCount - 1
		);

		Debug.Log($"{targetNodeName}, {originalFrameIndex / 3} (interp {frameIndex}), {interpPos} (+offset: {interpPos + startOffset})");
	}

	List<Vector3[]> ResampleFrames(
		List<Vector3[]> sourceFrames,

		float sourceFPS,
		float targetFPS,

		float speed = 1f
	) {
		if (sourceFrames.Count < 2) {
			return sourceFrames;
		}

		speed = Mathf.Max(float.Epsilon, speed);
		targetFPS /= speed;

		if (Mathf.Approximately(sourceFPS, targetFPS)) {
			return sourceFrames;
		}

		float sourceDuration = sourceFrames.Count / sourceFPS;
		int targetFrameCount = Mathf.CeilToInt(sourceDuration * targetFPS);

		var result = new List<Vector3[]>(targetFrameCount);

		for (int i = 0; i < targetFrameCount; i++) {
			float t = i / targetFPS;        // seconds
			float srcFrame = t * sourceFPS; // source-frame time

			int a = Mathf.FloorToInt(srcFrame);
			int b = Mathf.Min(a + 1, sourceFrames.Count - 1);
			float lerp = srcFrame - a;

			var frame = new Vector3[sourceFrames[0].Length];
			for (int n = 0; n < frame.Length; n++) {
				frame[n] = Vector3.Lerp(
					sourceFrames[a][n],
					sourceFrames[b][n],
					lerp
				);
			}

			result.Add(frame);
		}

		return result;
	}

	List<Vector3> SmoothPath(List<Vector3> points, int? subdivisions = 5) {
		if (subdivisions == 0 || subdivisions == null || points.Count < 2) {
			return new List<Vector3>(points);
		}

		var smoothed = new List<Vector3>();

		for (int i = 0; i < points.Count - 1; i++) {
			Vector3 p0 = i > 0 ? points[i - 1] : points[i];
			Vector3 p1 = points[i];
			Vector3 p2 = points[i + 1];
			Vector3 p3 = i < points.Count - 2 ? points[i + 2] : points[i + 1];

			for (int j = 0; j <= subdivisions; j++) {
				float t = j / (float)subdivisions;
				float t2 = t * t;
				float t3 = t2 * t;

				// Catmull-Rom formula
				Vector3 pos = 0.5f * (
					2f * p1 +
					(-p0 + p2) * t +
					(2f*p0 - 5f*p1 + 4f*p2 - p3) * t2 +
					(-p0 + 3f*p1 - 3f*p2 + p3) * t3
				);
				smoothed.Add(pos);
			}
		}

		smoothed.Add(points[points.Count - 1]);
		return smoothed;
	}

	List<Vector3[]> DistinctFrames(
		List<Vector3[]> sourceFrames,
		float distinctionThreshold
	) {
		if (sourceFrames == null || sourceFrames.Count == 0) {
			return sourceFrames;
		}

		// no filtering
		if (distinctionThreshold <= 0) {
			return sourceFrames;
		}

		float thresholdSq = distinctionThreshold * distinctionThreshold;

		var result = new List<Vector3[]>(sourceFrames.Count);
		// always keep first frame
		result.Add(sourceFrames[0]);

		Vector3[] lastKept = sourceFrames[0];

		for (int i = 1; i < sourceFrames.Count; i++) {
			Vector3[] current = sourceFrames[i];

			bool isDistinct = false;

			for (int n = 0; n < current.Length; n++) {
				// squared distance is cheaper than magnitude
				if ((current[n] - lastKept[n]).sqrMagnitude > thresholdSq) {
					isDistinct = true;
					break;
				}
			}

			if (isDistinct) {
				result.Add(current);
				lastKept = current;
			}
		}

		return result;
	}

	void LoadBin(
		string file,

		int startFrame = 0,
		float speed = 100f,
		bool left = false,

		float distinctionThreshold = float.Epsilon
	) {
		animationFrames.Clear();

		using var binaryReader = new BinaryReader(File.OpenRead(file));

		int frameCount = binaryReader.ReadInt32();
		originalFrameCount = frameCount;

		for (int frameIndex = 0; frameIndex < frameCount; frameIndex++) {
			binaryReader.ReadByte();
			int nodeCount = binaryReader.ReadInt32();

			if (nodeCount != NodePoints.Length) {
				Debug.LogError($"Expected {NodePoints.Length} nodes, got {nodeCount}");
			}

			Vector3[] frame = new Vector3[nodeCount];

			for (int nodeIndex = 0; nodeIndex < nodeCount; nodeIndex++) {
				float x = binaryReader.ReadSingle();
				float y = binaryReader.ReadSingle();
				float z = binaryReader.ReadSingle();

				frame[nodeIndex] = new Vector3(left ? -x : x, -z, -y);
			}

			animationFrames.Add(frame);
		}

		if (startFrame < 0 || startFrame >= animationFrames.Count) {
			Debug.LogError(
				$"Start frame {startFrame} is out of range (0–{animationFrames.Count - 1})"
			);
			return;
		}

		animationFrames = animationFrames.GetRange(
			startFrame,
			animationFrames.Count - startFrame
		);

		animationFrames = DistinctFrames(
			animationFrames,
			distinctionThreshold
		);

		animationFrames = ResampleFrames(
			animationFrames,
			sourceFPS: Vectorier.Core.Game.Animation.FrameRate,
			targetFPS: Vectorier.Core.Game.FrameRate,
			speed: speed / 100f
		);
	}

	//-=-=-=-//
	// Utils

	void InitializeNodes() {
		animationNodes.Clear();

		foreach (var nodeName in NodePoints) {
			animationNodes[nodeName] = pivotOffset;
		}
	}

	void UpdatePlayback() {
		if (!offsetInitialized || animationFrames.Count == 0) {
			return;
		}

		double now = EditorApplication.timeSinceStartup;
		double deltaTime = now - lastPlaybackTime;
		lastPlaybackTime = now;

		playbackTime += deltaTime;

		// duration of the interpolated animation
		float duration = animationFrames.Count / Vectorier.Core.Game.FrameRate;

		if (loop) {
			playbackTime %= duration;
		} else {
			playbackTime = Math.Min(playbackTime, duration);
		}

		// interpolated frame index (resampled)
		float frameFloat = (float)(playbackTime * Vectorier.Core.Game.FrameRate);
		frameIndex = Mathf.Clamp(Mathf.FloorToInt(frameFloat), 0, animationFrames.Count - 1);

		// compute corresponding original frame number using time
		float originalDuration = originalFrameCount / Vectorier.Core.Game.Animation.FrameRate; // original FPS
		double originalTime = playbackTime * (Vectorier.Core.Game.FrameRate / Vectorier.Core.Game.Animation.FrameRate) * (100f / speed);
		
		int originalFrameIndex = Mathf.Clamp(Mathf.FloorToInt((float)originalTime), 0, originalFrameCount - 1);

		// update nodes
		Vector3[] frame = animationFrames[frameIndex];
		for (int i = 0; i < NodePoints.Length; i++) {
			animationNodes[NodePoints[i]] = ConvertAxis(frame[i]) + startOffset;
		}

		// target node path
		targetNodePath.Clear();
		if (dynamicPath) {
			for (int i = 0; i <= frameIndex; i++) {
				targetNodePath.Add(ConvertAxis(animationFrames[i][targetNodeIndex]) + startOffset);
			}
		} else {
			foreach (var f in animationFrames) {
				targetNodePath.Add(ConvertAxis(f[targetNodeIndex]) + startOffset);
			}
		}

		visibleTargetNodeCount = targetNodePath.Count;

		SceneView.RepaintAll();

		// Debug.Log($"Interpolated: {frameIndex}, Original: {originalFrameIndex}");
	}

	void UpdatePreview(Vector3 cursorWorldPosition) {
		if (!previewPose.ContainsKey("NPivot")) {
			return;
		}

		Vector3 pivotLocal = previewPose["NPivot"];
		Vector3 offset = cursorWorldPosition - pivotLocal;

		previewNodes.Clear();

		foreach (var entry in previewPose) {
			previewNodes[entry.Key] = entry.Value + offset;
		}
	}

	Vector3 ConvertAxis(Vector3 value) => new Vector3(
		value.x / Vectorier.Core.Game.UnitScale,
		-value.z / Vectorier.Core.Game.UnitScale,
		value.y / Vectorier.Core.Game.UnitScale
	);

	void ClearAll() {
		animationNodes.Clear();
		previewNodes.Clear();
		animationFrames.Clear();
		targetNodePath.Clear();

		playbackTime = 0;
		visibleTargetNodeCount = 0;
		offsetInitialized = false;
		previewActive = false;
	}

	// Drawing

	void DrawNodes() {
		if (previewActive) {
			Handles.color = colorHandles;

			foreach (var position in previewNodes.Values) {
				Handles.DotHandleCap(0, position, Quaternion.identity, nodeHandleSize / Vectorier.Core.Game.UnitScale, EventType.Repaint);
			}
		}

		Handles.color = colorHandlesActive;

		foreach (var position in animationNodes.Values) {
			Handles.DotHandleCap(0, position, Quaternion.identity, nodeHandleSize / Vectorier.Core.Game.UnitScale, EventType.Repaint);
		}
	}

	void DrawPath() {
		if (visibleTargetNodeCount < 2) {
			return;
		}

		Handles.color = colorPath;
		for (int i = 1; i <= 5; i++) {
			Handles.DrawAAPolyLine(nodeHandleSize * 1.25f, targetNodePath.GetRange(0, visibleTargetNodeCount).ToArray());
		}
	}

	void DrawRig() {
		Handles.color = colorRig;

		foreach (var (a, b) in Vectorier.Core.Components.Model.ModelHelpers.Skeleton.Nodes.Connections) {
			if (animationNodes.TryGetValue(a, out var aPosAnim) &&
				animationNodes.TryGetValue(b, out var bPosAnim)) 
			{
				DrawBone(aPosAnim, bPosAnim, handleThickness);
			}

			if (previewActive &&
				previewNodes.TryGetValue(a, out var aPosPrev) &&
				previewNodes.TryGetValue(b, out var bPosPrev)) 
			{
				DrawBone(aPosPrev, bPosPrev, handleThickness);
			}
		}
	}

	void DrawBone(Vector3 start, Vector3 end, float thickness) {
		/*
		float unitScale = Vectorier.Core.Game.UnitScale;

		Handles.SphereHandleCap(0, start, Quaternion.identity, thickness / unitScale, EventType.Repaint);

		Vector3 direction = end - start;
		float length = direction.magnitude;
		if (length > 0f) {
			Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
			Handles.CylinderHandleCap(0, start + direction * 0.5f, rotation, thickness / unitScale, EventType.Repaint);
		}

		Handles.SphereHandleCap(0, end, Quaternion.identity, thickness / unitScale, EventType.Repaint);
		*/

		Handles.DrawLine(start, end, thickness * 1.25f);
	}

	void InitializePreviewPose(bool left = true) {
		previewPose.Clear();

		void SetPose(string name, float x, float y, float z)
			=> previewPose[name] = ConvertAxis(new Vector3(left ? -x : x, -y, -z));

		SetPose("NHip_1", -19.577221f, -8.585417f, 84.134026f);
		SetPose("NHip_2", -14.560858f, 8.122724f, 82.953896f);
		SetPose("NStomach", -9.392555f, -1.314231f, 99.322510f);
		SetPose("NChest", -0.985765f, -2.248583f, 114.576309f);
		SetPose("NNeck", 8.144234f, 0.799165f, 129.391342f);
		SetPose("NShoulder_1", 12.551178f, -15.790263f, 128.772690f);
		SetPose("NShoulder_2", 6.237663f, 17.609047f, 134.210663f);
		SetPose("NKnee_1", -50.975418f, -5.785246f, 45.456955f);
		SetPose("NKnee_2", 25.844582f, 9.903176f, 58.044540f);
		SetPose("NAnkle_1", -88.699814f, -3.378675f, 25.798462f);
		SetPose("NAnkle_2", -13.250669f, 6.528876f, 50.287231f);
		SetPose("NToe_1", -92.157410f, -0.610153f, 8.867973f);
		SetPose("NHeel_1", -98.535316f, -3.505732f, 27.600554f);
		SetPose("NToeTip_1", -87.669655f, 0.365667f, 2.316364f);
		SetPose("NToeS_1", -92.283051f, -8.508365f, 7.602760f);
		SetPose("NHeel_2", -22.572092f, 4.031524f, 52.909119f);
		SetPose("NToe_2", -18.491583f, 7.329743f, 33.609646f);
		SetPose("NToeTip_2", -16.405792f, 8.739633f, 26.016127f);
		SetPose("NToeS_2", -20.299704f, 15.065866f, 34.549374f);
		SetPose("NElbow_1", 23.573296f, -29.794552f, 106.561279f);
		SetPose("NElbow_2", -22.879360f, 22.530788f, 132.434006f);
		SetPose("NWrist_1", 47.289154f, -12.875849f, 114.187531f);
		SetPose("NWrist_2", -24.266720f, 36.565292f, 107.673439f);
		SetPose("NKnuckles_1", 56.037056f, -9.906665f, 115.244850f);
		SetPose("NFingertips_1", 55.178741f, -1.261982f, 119.921249f);
		SetPose("NKnucklesS_1", 50.209671f, -10.927604f, 120.081985f);
		SetPose("NKnuckles_2", -27.603422f, 39.348595f, 98.328270f);
		SetPose("NFingertips_2", -29.340508f, 31.007551f, 102.793465f);
		SetPose("NKnucklesS_2", -21.749163f, 35.052471f, 98.640076f);
		SetPose("NHead", 17.986877f, 0.118644f, 143.801025f);
		SetPose("NTop", 22.405685f, -0.554002f, 160.715347f);
		SetPose("NChestS_1", -0.897280f, -8.979884f, 116.031631f);
		SetPose("NChestS_2", -2.560478f, 8.153231f, 115.891487f);
		SetPose("NStomachS_1", -9.784924f, -9.954099f, 99.871407f);
		SetPose("NStomachS_2", -9.076771f, 7.395645f, 99.385635f);
		SetPose("NChestF", 4.352207f, 0.990542f, 110.784447f);
		SetPose("NStomachF", -2.270447f, -1.816215f, 95.587852f);
		SetPose("NPelvisF", -9.550920f, -2.755259f, 79.839340f);
		SetPose("NHeadS_1", 17.150383f, -8.589858f, 143.676453f);
		SetPose("NHeadS_2", 18.824371f, 8.826989f, 143.926865f);
		SetPose("NHeadF", 26.410934f, -0.664772f, 141.568832f);
		SetPose("NPivot", -17.069702f, -0.231332f, 83.542564f);
		SetPose("DetectorH", -12f, 0f, 0f);
		SetPose("DetectorV", 56f, 0f, 100f);
		SetPose("COM", -8.691902f, 0.692974f, 91.093567f);
	}

	// XML

	int? ResolveStartFrameFromXml(
		string inputXml,
		DefaultAsset animationFile
	) {
		if (animationFile == null || string.IsNullOrEmpty(inputXml)) {
			return null;
		}

		string binName = Path.GetFileName(AssetDatabase.GetAssetPath(animationFile));

		var doc = new XmlDocument();
		doc.Load(inputXml);

		string animationNodeName = FindNodeNameByAttribute(doc, binName, "FileName");
		if (animationNodeName == null) {
			return null;
		}

		return FindFirstNodeInSubNode(doc, animationNodeName, "/root/ReactionGroups");
	}

	string FindNodeNameByAttribute(
		XmlDocument doc,

		string attribute,
		string value
	) {
		var nodes = doc.SelectNodes($"//*[@{value}]");

		foreach (XmlNode node in nodes) {
			var attr = node.Attributes?[value];
			if (attr == null) continue;

			if (string.Equals(
				attr.Value,
				attribute,
				StringComparison.OrdinalIgnoreCase
			)) {
				return node.Name;
			}
		}

		return null;
	}

	int? FindFirstNodeInSubNode(
		XmlDocument doc,

		string nodeName,
		string nodePath
	) {
		var subNode = doc.SelectSingleNode(nodePath);

		if (subNode == null) {
			return null;
		}

		return FindFirstNodeByNameRecursive(subNode, nodeName, "FirstFrame");
	}

	int? FindFirstNodeByNameRecursive(
		XmlNode node,

		string targetNodeName,
		string value
	) {
		foreach (XmlNode child in node.ChildNodes) {
			// match node name
			if (child.Name == targetNodeName) {
				var attr = child.Attributes?[value];

				if (attr != null &&
					int.TryParse(attr.Value, out int firstFrame)) {
					return firstFrame;
				}
			}

			// recurse
			var result = FindFirstNodeByNameRecursive(child, targetNodeName, value);
			if (result.HasValue) {
				return result;
			}
		}

		return null;
	}

	void UpdateStartFrameFromFile(
		string? inputFile = null
	) {
		if (string.IsNullOrEmpty(inputFile) || file == null) {
			return;
		}

		int? frame = ResolveStartFrameFromXml(inputFile, file);

		if (frame.HasValue) {
			startFrame = frame.Value;

			Repaint();              // EditorWindow repaint
			SceneView.RepaintAll(); // Scene redraw
		}
	}
}