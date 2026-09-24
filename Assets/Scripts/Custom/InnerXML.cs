using UnityEngine;
using UnityEditor;

using System;
using System.Xml;
using System.Linq;
using System.Text;
using System.Xml.Linq;

// -=-=-=- //

public class LiveXmlPreviewWindow : EditorWindow {
	private GameObject targetObject;
	private string innerXml = "";

	private Vector2 scrollPos;

	// SETTINGS
	private int fontSize = 10;

	private bool wrapText = true;
	private bool freeze = false;

	private bool lockSelection = false;
	private GameObject lockedObject;

	private enum IndentMode { Tabs, Spaces, None }
	private IndentMode indentMode = IndentMode.Spaces;

	private double lastUpdateTime;
	private const double updateInterval = 0.2;

	// cached stats
	private int nodeCount;

	[MenuItem("Vectorier/Inner XML #O")]
	private static void OpenWindow() {
		var window = GetWindow<LiveXmlPreviewWindow>();
		window.titleContent = new GUIContent("Live XML Preview");
		window.Show();
	}

	private void OnEnable() {
		EditorApplication.update += OnEditorUpdate;
		Selection.selectionChanged += OnSelectionChanged;

		targetObject = Selection.activeGameObject;
		UpdateXml();
	}

	private void OnDisable() {
		EditorApplication.update -= OnEditorUpdate;
		Selection.selectionChanged -= OnSelectionChanged;
	}

	private void OnSelectionChanged() {
		if (lockSelection) {
			return;
		}

		targetObject = Selection.activeGameObject;
		UpdateXml();
		Repaint();
	}

	// ---------------- GUI ----------------
	private void OnGUI() {
		DrawTopBar();
		DrawSettings();
		DrawScrollView();
	}

	// ---------------- TOP BAR ----------------
	private void DrawTopBar() {
		EditorGUILayout.BeginHorizontal();

		// GameObject field (always shows current target)
		targetObject = (GameObject)EditorGUILayout.ObjectField(
			targetObject, typeof(GameObject), true
		);

		// LOCK BUTTON
		Color prev = GUI.backgroundColor;
		GUI.backgroundColor = lockSelection ? Color.red : Color.white;

		if (GUILayout.Button(lockSelection ? "🔒 Locked" : "🔓 Lock", GUILayout.Width(90))) {
			lockSelection = !lockSelection;

			if (lockSelection) {
				lockedObject = targetObject; // freeze current selection
			}
		}

		GUI.backgroundColor = prev;

		GUILayout.FlexibleSpace();

		DrawInfoPanel();

		EditorGUILayout.EndHorizontal();
	}

	private void DrawInfoPanel() {
		GUIStyle small = new GUIStyle(EditorStyles.miniLabel) {
			alignment = TextAnchor.UpperRight
		};

		EditorGUILayout.BeginVertical(GUILayout.Width(200));

		EditorGUILayout.LabelField($"Nodes: {nodeCount}", small);
		EditorGUILayout.LabelField($"Chars: {innerXml?.Length ?? 0}", small);

		EditorGUILayout.EndVertical();
	}

	// ---------------- SETTINGS ----------------
	private void DrawSettings() {
		EditorGUILayout.BeginHorizontal();

		fontSize = EditorGUILayout.IntSlider("Font Size", fontSize, 8, 32);
		freeze = GUILayout.Toggle(freeze, "Freeze", "Button", GUILayout.Width(80));

		if (GUILayout.Button("Copy", GUILayout.Width(80))) {
			GUIUtility.systemCopyBuffer = innerXml;
		}

		EditorGUILayout.EndHorizontal();

		wrapText = EditorGUILayout.Toggle("Wrap", wrapText, GUILayout.Width(60));
		indentMode = (IndentMode)EditorGUILayout.EnumPopup("Indentation", indentMode);
	}

	// ---------------- SCROLL ----------------
	private void DrawScrollView() {
		GUIStyle style = new GUIStyle(EditorStyles.textArea){
			fontSize = fontSize,
			wordWrap = wrapText
		};

		float height = style.CalcHeight(
			new GUIContent(innerXml),
			position.width - 20
		);

		scrollPos = EditorGUILayout.BeginScrollView(
			scrollPos,
			true,
			true,
			GUILayout.Height(position.height - 80)
		);

		EditorGUILayout.SelectableLabel(innerXml, style, GUILayout.MinHeight(height));

		EditorGUILayout.EndScrollView();
	}

	// ---------------- UPDATE LOOP ----------------
	private void OnEditorUpdate() {
		if (freeze) {
			return;
		}

		if (targetObject == null) {
			return;
		}

		// If not locked, follow selection
		if (!lockSelection && Selection.activeGameObject != targetObject) {
			targetObject = Selection.activeGameObject;
			UpdateXml();
			Repaint();

			return;
		}

		if (EditorApplication.timeSinceStartup - lastUpdateTime > updateInterval) {
			UpdateXml();
			Repaint();
			lastUpdateTime = EditorApplication.timeSinceStartup;
		}
	}

	// ---------------- XML BUILD ----------------
	private void UpdateXml() {
		try {
			if (targetObject == null) {
				innerXml = "";
				return;
			}

			XmlDocument xml = new XmlDocument();

			XmlElement root = xml.CreateElement("Root");
			xml.AppendChild(root);

			XmlElement track = xml.CreateElement("Track");
			root.AppendChild(track);

			Vectorier.Core.Components.ObjectDynamicComponent.Convert(
				targetObject,
				track,
				legacy: true
			);

			Vectorier.Core.XML.Utils.Optimize.Objects(
				track,
				Vectorier.Settings.Core.GetPrecision(
					"VectorierSettings.Elements.Properties.Object.Precision"
				)
			);

			var node = track.SelectSingleNode(".//Object");

			innerXml = FormatXml(node);

			UpdateStats(track);
		} catch (System.Exception ex) {
			innerXml = $"ERROR: {ex.Message}";
		}
	}

	// ---------------- FORMATTING ----------------
	private string FormatXml(XmlNode node) {
		if (node == null) {
			return "No Object found";
		}

		// Parse the node into XDocument
		XDocument doc = XDocument.Parse(node.OuterXml);

		string result;

		switch (indentMode) {
			case IndentMode.None:
				result = Vectorier.Core.XML.Utils.Operations.Linearize(doc);
				break;

			case IndentMode.Tabs:
				result = Vectorier.Core.XML.Utils.Operations.Standard(doc);
				break;

			default:
				// Standard formatted with tabs, then converted to spaces
				result = Vectorier.Core.XML.Utils.Operations.Standard(doc);
				result = result.Replace("\t", "    ");
				break;
		}

		if (result.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)) {
			int endIdx = result.IndexOf("?>");
			if (endIdx != -1) {
				result = result.Substring(endIdx + 2).TrimStart();
			}
		}

		return result.Trim();
	}

	// ---------------- STATS ----------------
	private void UpdateStats(XmlElement track) {
		nodeCount = track.SelectNodes(".//*")?.Count ?? 0;
	}
}