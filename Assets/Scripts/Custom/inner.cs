using UnityEngine;
using UnityEditor;

using System.Text;
using System.Xml;
using System.Linq;

// -=-=-=- //

public class LiveXmlPreviewWindow : EditorWindow {
    private GameObject targetObject;
    private string innerXml = "";
    private bool isRecording = false;
    private Vector2 scrollPos;

    [MenuItem("Vectorier/Inner XML #O")]
    private static void OpenWindow() {
        var window = GetWindow<LiveXmlPreviewWindow>();
        window.titleContent = new GUIContent("Live XML Preview");
        window.Show();
    }

    // -----------------------
    // GUI
    // -----------------------

    private void OnGUI() {
		using (new EditorGUI.DisabledScope(isRecording)) {
			GameObject newTarget = (GameObject)EditorGUILayout.ObjectField(
				targetObject,
				typeof(GameObject),
				true
			);
			
			if (newTarget != targetObject) {
				targetObject = newTarget;
				if (isRecording && targetObject != null) {
					UpdateInnerXml(); // immediate update
					Repaint();
				}
			}
		}

        GUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        if (!isRecording) {
            if (GUILayout.Button("Record", GUILayout.Width(80), GUILayout.Height(30))) {
                if (targetObject == null) {
                    EditorUtility.DisplayDialog("Error", "Assign a GameObject first!", "OK");
                } else {
                    isRecording = true;
                }
            }
        } else {
            if (GUILayout.Button("Stop", GUILayout.Width(80), GUILayout.Height(30))) {
                isRecording = false;
            }
        }

        if (GUILayout.Button("Copy", GUILayout.Width(100), GUILayout.Height(30))) {
            GUIUtility.systemCopyBuffer = innerXml;
        }
        EditorGUILayout.EndHorizontal();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
        var style = new GUIStyle(EditorStyles.textArea) { wordWrap = false };

        EditorGUILayout.SelectableLabel(innerXml, style, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    // Live Update
	private double lastUpdateTime = 0;
	private const double updateInterval = 0.2; // seconds

	private void OnEnable() {
		EditorApplication.update += OnEditorUpdate;
	}

	private void OnDisable() {
		EditorApplication.update -= OnEditorUpdate;
	}

	private void OnEditorUpdate() {
		if (!isRecording || targetObject == null) return;

		if (EditorApplication.timeSinceStartup - lastUpdateTime > updateInterval) {
			UpdateInnerXml();
			Repaint();
			lastUpdateTime = EditorApplication.timeSinceStartup;
		}
	}

	private void UpdateInnerXml() {
		try {
			var buildMap = FindObjectOfType<BuildMap>();
			if (buildMap == null) {
				return;
			}

			XmlDocument xml = new XmlDocument();

			XmlElement root = xml.CreateElement("Root");
			xml.AppendChild(root);

			XmlElement track = xml.CreateElement("Track");
			root.AppendChild(track);

			buildMap.ConvertToDynamic(targetObject, track, xml);

			int precision = Vectorier.Settings.GetPrecision(
				"VectorierSettings.Elements.Properties.Object.Precision"
			);
			Vectorier.Core.XML.Utils.Optimize.Objects(track, precision);

			// Force X/Y = 0 on parent object
			foreach (XmlElement obj in track.SelectNodes(".//Object").Cast<XmlElement>()) {
				obj.RemoveAttribute("X");
				obj.RemoveAttribute("Y");
			}

			innerXml = PrettyPrintXmlNode(track.SelectSingleNode(".//Object"));
		} catch (System.Exception ex) {
			innerXml = $"<!-- ERROR: {ex.Message} -->";
		}
	}

    private static string PrettyPrintXmlNode(XmlNode node) {
        if (node == null) return "<!-- No <Object> found -->";

        var settings = new XmlWriterSettings {
            Indent = true,
            IndentChars = "\t",
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Replace,
            OmitXmlDeclaration = true
        };

        using (var sw = new System.IO.StringWriter())
        using (var xw = XmlWriter.Create(sw, settings)) {
            node.WriteTo(xw);
            xw.Flush();
            return sw.ToString().Replace("/ >", "/>").TrimEnd();
        }
    }
}