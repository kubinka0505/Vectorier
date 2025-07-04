using UnityEditor;
using UnityEngine;

// -=-=-=- //

public class BackgroundImageWindow : EditorWindow {
	private Texture2D backgroundImage;
	private string imagePath;
	private bool fitToScene = true;
	private bool showBackground = false;

	// Add menu item to show the window
	[MenuItem("Window/Background Image")]
	public static void ShowWindow() {
		GetWindow<BackgroundImageWindow>("Background Image");
	}

	private void OnGUI() {
		GUILayout.Label("Background Image Settings", EditorStyles.boldLabel);

		// Field to input image path
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Image Path", GUILayout.Width(70));
		imagePath = GUILayout.TextField(imagePath, GUILayout.Width(250));
		if (GUILayout.Button("Browse", GUILayout.Width(70))) {
			string path = EditorUtility.OpenFilePanel("Select Background Image", "", "png,jpg,jpeg");

			if (!string.IsNullOrEmpty(path)) {
				imagePath = path;
				LoadImage();
			}
		}
		EditorGUILayout.EndHorizontal();

		// Checkbox to fit image to scene size
		fitToScene = EditorGUILayout.Toggle("Fit to Scene", fitToScene);

		// Load image button
		if (GUILayout.Button("Load Image")) {
			LoadImage();

			// Enable background display
			showBackground = true;
		}

		// Display the image preview if loaded
		if (backgroundImage != null) {
			GUILayout.Label("Image Preview:");
			GUILayout.Label(backgroundImage, GUILayout.Width(300), GUILayout.Height(150));
		}

		// Button to remove the background
		if (GUILayout.Button("Undo Background")) {
			// Disable background display
			showBackground = false;

			// Refresh scene view
			SceneView.RepaintAll(); 
		}
	}

	private void LoadImage() {
		if (!string.IsNullOrEmpty(imagePath)) {
			byte[] fileData = System.IO.File.ReadAllBytes(imagePath);
			backgroundImage = new Texture2D(2, 2);

			// Automatically resize
			backgroundImage.LoadImage(fileData);
		} else {
			Debug.LogWarning("Image path is empty. Please select an image.");
		}
	}

	// Draw the image as a background in the Scene view
	private void OnSceneGUI(SceneView sceneView) {
		if (!showBackground || backgroundImage == null) {
			return;
		}

		Handles.BeginGUI();

		// Calculate dimensions to fit the scene view if enabled
		float width = fitToScene ? sceneView.position.width : backgroundImage.width;
		float height = fitToScene ? sceneView.position.height : backgroundImage.height;

		// Position to center the image in the scene view
		Rect rect = new Rect(0, 0, width, height);
		GUIStyle backgroundStyle = new GUIStyle();
		backgroundStyle.normal.background = backgroundImage;

		// Use DrawTexture to render the background behind everything
		// Draw behind all GUI elements
		GUI.depth = -10000000;
		GUI.Box(rect, GUIContent.none, backgroundStyle);

		Handles.EndGUI();
	}

	// Enable the scene GUI rendering when window is opened
	private void OnEnable() {
		SceneView.duringSceneGui += OnSceneGUI;
	}

	// Disable the scene GUI rendering when window is closed
	private void OnDisable() {
		SceneView.duringSceneGui -= OnSceneGUI;
	}
}
