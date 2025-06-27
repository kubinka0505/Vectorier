using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

// -=-=-=- //

public class SceneViewWideScreenshot : EditorWindow {
	// Store the screen width and height
	private int screenWidth;
	private int screenHeight;
	private string sceneName = "Untitled";
	
	// Variables for the target width and height
	private int targetWidth;
	private int targetHeight;

	[MenuItem("Vectorier/⚙ Utils/📷 Take scene screenshot")]
	public static void ShowWindow() {
		var window = GetWindow<SceneViewWideScreenshot>("VectorierUtils - Scene screenshot");
		window.minSize = new Vector2(300, 72);
		window.maxSize = new Vector2(300, 72);
	}

	private void OnEnable() {
		// Scene name for file naming
		var csc = SceneManager.GetActiveScene();
		if (!string.IsNullOrEmpty(csc.path)) {
			sceneName = csc.name;
		}

		// Store the current screen dimensions
		screenWidth = Screen.currentResolution.width;
		screenHeight = Screen.currentResolution.height;

		// Set initial values for target width and height
		targetWidth = screenWidth;
		targetHeight = screenHeight;
	}

	void OnGUI() {
		int min = 2;
		int maxSize = SystemInfo.maxTextureSize;

		int snapStepWidth = ((int)screenWidth / 10);
		int snapStepHeight = ((int)screenHeight / 10);

		// Display sliders with snapping to the nearest `snapStep`
		targetWidth = SnapSlider("Width", targetWidth, (int)(screenWidth / min), maxSize, snapStepWidth);
		targetHeight = SnapSlider("Height", targetHeight, (int)(screenHeight / min), maxSize, snapStepHeight);

		// Capture button
				var csc = SceneManager.GetActiveScene();

		if (GUILayout.Button("Capture", GUILayout.Height(32))) {
			Debug.Log(sceneName);
			CaptureWideSceneViewScreenshot();
		}
	}

	// Helper function to snap slider value
	private int SnapSlider(string label, int currentValue, int minValue, int maxValue, int snapStep) {
		int sliderValue = EditorGUILayout.IntSlider(label, currentValue, minValue, maxValue);
		
		// Snap the value to the nearest step
		return Mathf.RoundToInt(sliderValue / (float)snapStep) * snapStep;
	}

	void CaptureWideSceneViewScreenshot() {
		SceneView sceneView = SceneView.lastActiveSceneView;
		if (sceneView == null) {
			Debug.LogError("No active Scene View found!");
			return;
		}

		Camera sceneCamera = sceneView.camera;
		if (sceneCamera == null) {
			Debug.LogError("Scene View Camera not found!");
			return;
		}

		// Width
		if (targetWidth > SystemInfo.maxTextureSize) {
			Debug.LogError($"Width exceeds max texture size ({SystemInfo.maxTextureSize})");
			return;
		}

		// Height
		if (targetHeight > SystemInfo.maxTextureSize) {
			Debug.LogError($"Height exceeds max texture size ({SystemInfo.maxTextureSize})");
			return;
		}

		// Create texture with the selected dimensions
		Texture2D finalScreenshot = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);

		// Capture Scene View with the adjusted resolution
		RenderTexture rt = new RenderTexture(targetWidth, targetHeight, 24);
		sceneCamera.targetTexture = rt;
		sceneCamera.Render();

		// Read the pixels from the render texture
		RenderTexture.active = rt;
		finalScreenshot.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
		finalScreenshot.Apply();

		// Clean up
		sceneCamera.targetTexture = null;
		RenderTexture.active = null;
		DestroyImmediate(rt);

		// Save the screenshot without refreshing the Asset Database
		byte[] bytes = finalScreenshot.EncodeToPNG();
		string fileName = $"._{sceneName}_{targetWidth}x{targetHeight}.png";
		string fullPath = Path.Combine("Assets", fileName);
		File.WriteAllBytes(fullPath, bytes);

		Debug.Log(
			Path.Combine(
				Application.dataPath, fileName
			).Replace(
				"/",
				Path.DirectorySeparatorChar.ToString()
			)
		);
	}
}