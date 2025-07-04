using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;

[AddComponentMenu("Vectorier/Dynamic")]
public class Dynamic : MonoBehaviour {
	[Tooltip("Transformation name of the dynamic object")]
	public string TransformationName = "Transform_name";

	[Tooltip("Whether or not to add the red visual line while setting up")]
	public bool AddVisualLines = true;

	[Tooltip("Whether or not to automatically reset after the preview is finished")]
	public bool ResetAfterPreviewFinish = true;

	[Serializable]
	public class UseCheck {
		public bool UseMovement1 = true;
		public bool UseMovement2 = false;
		public bool UseMovement3 = false;
		public bool UseMovement4 = false;
		public bool UseMovement5 = false;
		public bool UseMovement6 = false;
		public bool UseMovement7 = false;
		public bool UseMovement8 = false;
		public bool UseMovement9 = false;
		public bool UseMovement10 = false;
		public bool UseMovement11 = false;
		public bool UseMovement12 = false;
		public bool UseMovement13 = false;
		public bool UseMovement14 = false;
		public bool UseMovement15 = false;
		public bool UseMovement16 = false;

		public bool UseMovement(int index) {
			switch (index) {
				case 1: return UseMovement1;
				case 2: return UseMovement2;
				case 3: return UseMovement3;
				case 4: return UseMovement4;
				case 5: return UseMovement5;
				case 6: return UseMovement6;
				case 7: return UseMovement7;
				case 8: return UseMovement8;
				case 9: return UseMovement9;
				case 10: return UseMovement10;
				case 11: return UseMovement11;
				case 12: return UseMovement12;
				case 13: return UseMovement13;
				case 14: return UseMovement14;
				case 15: return UseMovement15;
				case 16: return UseMovement16;
				// Out of range
				default: return false;
			}
		}
	}


	[Serializable]
	public class Movement {
		[Tooltip("Move Duration in Second")]
		public float MoveDuration = 1.5f;

		[Tooltip("Move Delay in Second")]
		public float Delay = 0f;

		[Tooltip("Easing Value on the X Axis (Divide by 2 for linear easing)")]
		public float SupportXAxis = 0.0f;

		[Tooltip("Easing Value on the Y Axis (Divide by 2 for linear easing)")]
		public float SupportYAxis = 0.0f;

		[Tooltip("How much to move on X Axis")]
		public float MoveXAxis = 0.0f;

		[Tooltip("How much to move on Y Axis")]
		public float MoveYAxis = 0.0f;
	}

	[SerializeField]
	public UseCheck MovementUsage;

	[SerializeField]
	public Movement MoveInterval1;

	[SerializeField]
	public Movement MoveInterval2;

	[SerializeField]
	public Movement MoveInterval3;

	[SerializeField]
	public Movement MoveInterval4;

	[SerializeField]
	public Movement MoveInterval5;

	[SerializeField]
	public Movement MoveInterval6;

	[SerializeField]
	public Movement MoveInterval7;

	[SerializeField]
	public Movement MoveInterval8;

	[SerializeField]
	public Movement MoveInterval9;

	[SerializeField]
	public Movement MoveInterval10;

	[SerializeField]
	public Movement MoveInterval11;

	[SerializeField]
	public Movement MoveInterval12;

	[SerializeField]
	public Movement MoveInterval13;

	[SerializeField]
	public Movement MoveInterval14;

	[SerializeField]
	public Movement MoveInterval15;

	[SerializeField]
	public Movement MoveInterval16;

	private Vector2 lastSpecificTopLeft;

	private const float checkInterval = 0.5f;

	private Dictionary<string, EditorApplication.CallbackFunction> monitoringActions = new Dictionary<string, EditorApplication.CallbackFunction>();
	private Dictionary<string, bool> isLoopActive = new Dictionary<string, bool>();
	private Dictionary<string, float> nextCheckTimes = new Dictionary<string, float>();

	private bool isImageDynamic = false;

	public void DuplicateChildrenPreview(string newParentName) {
		// Ignore List
		string[] excludedNames = {
			"MovePreview1",
			"MovePreview2",
			"MovePreview3",
			"MovePreview4",
			"MovePreview5",
			"MovePreview6",
			"MovePreview7",
			"MovePreview8",
			"MovePreview9",
			"MovePreview10",
			"MovePreview11",
			"MovePreview12",
			"MovePreview13",
			"MovePreview14",
			"MovePreview15",
			"MovePreview16",
			"VisualPointParent",
			"VisualLineParent"
		};
		string triggerTag = "Trigger";

		// In case of Image
		if (transform.CompareTag("Image")) {
			if (transform.childCount == 0) {
				GameObject duplicateParent = Instantiate(gameObject, transform);
				duplicateParent.name = name;
				duplicateParent.tag = "Unused";
				duplicateParent.transform.position = transform.position;
				duplicateParent.transform.rotation = transform.rotation;

				SpriteRenderer renderer = duplicateParent.GetComponent<SpriteRenderer>();
				if (renderer != null) {
					Color color = renderer.color;
					// Set alpha transparency to 120
					color.a = 120 / 255f;
					renderer.color = color;
				}

				// Clear components except Transform and SpriteRenderer
				var components = duplicateParent.GetComponents<UnityEngine.Component>();
				foreach (var component in components) {
					if (!(component is Transform || component is SpriteRenderer)) {
						DestroyImmediate(component);
					}
				}
				isImageDynamic = true;
			}
		}

		// Create new empty object
		GameObject newParent = new GameObject(newParentName);
		newParent.tag = "Unused";
		newParent.transform.SetParent(transform);
		newParent.transform.position = transform.position;


		// Run through all child objects
		foreach (UnityEngine.Transform child in transform) {
			// Skip ignored object.
			if (child.CompareTag(triggerTag) && child.GetComponent<DynamicTrigger>()) {
				continue;
			}

			if (Array.Exists(excludedNames, name => name == child.name)) {
				continue;
			}

			// Duplication
			GameObject duplicate = null;
			duplicate = Instantiate(child.gameObject, newParent.transform);

			if (duplicate != null) {
				duplicate.name = child.name + "_Preview";
				duplicate.tag = "Unused";

				// Adjust SpriteRenderer transparency
				SpriteRenderer renderer = duplicate.GetComponent<SpriteRenderer>();
				if (renderer != null) {
					Color color = renderer.color;
					// Set alpha transparency to 120
					color.a = 120 / 255f;
					renderer.color = color;
				}
			}
		}
	}

	private void UpdateMoveInterval(string movePreview, Vector2 positionDifference) {
		Movement targetInterval = null;

		// Map MovePreview to the their MoveInterval
		switch (movePreview) {
			case "MovePreview1":
				targetInterval = MoveInterval1;
				break;
			case "MovePreview2":
				targetInterval = MoveInterval2;
				break;
			case "MovePreview3":
				targetInterval = MoveInterval3;
				break;
			case "MovePreview4":
				targetInterval = MoveInterval4;
				break;
			case "MovePreview5":
				targetInterval = MoveInterval5;
				break;
			case "MovePreview6":
				targetInterval = MoveInterval6;
				break;
			case "MovePreview7":
				targetInterval = MoveInterval7;
				break;
			case "MovePreview8":
				targetInterval = MoveInterval8;
				break;
			case "MovePreview9":
				targetInterval = MoveInterval9;
				break;
			case "MovePreview10":
				targetInterval = MoveInterval10;
				break;
			case "MovePreview11":
				targetInterval = MoveInterval11;
				break;
			case "MovePreview12":
				targetInterval = MoveInterval12;
				break;
			case "MovePreview13":
				targetInterval = MoveInterval13;
				break;
			case "MovePreview14":
				targetInterval = MoveInterval14;
				break;
			case "MovePreview15":
				targetInterval = MoveInterval15;
				break;
			case "MovePreview16":
				targetInterval = MoveInterval16;
				break;
			default:
				Debug.LogWarning($"No matching MoveInterval found for {movePreview}.");
				return;
		}

		if (targetInterval != null) {
			// Update the MoveInterval's X and Y values
			targetInterval.MoveXAxis = positionDifference.x;
			targetInterval.MoveYAxis = positionDifference.y;

		}
	}

	// Looping method to check for changes
	public void StartPositionMonitoring(string MovePreview) {
		if (isLoopActive.ContainsKey(MovePreview) && isLoopActive[MovePreview]) {
			Debug.LogWarning($"Position monitoring is already running for {MovePreview}.");
			return;
		}

		// Run loop state and next check time
		isLoopActive[MovePreview] = true;
		nextCheckTimes[MovePreview] = 0f;

		// Define monitoring action
		EditorApplication.CallbackFunction monitoringAction = () => {
			if (this == null || transform == null) {
				StopPositionMonitoring(MovePreview);
				Debug.LogError("Dynamic Parent removed! Cancelling position monitoring.");
				return;
			}

			if (EditorApplication.timeSinceStartup >= nextCheckTimes[MovePreview]) {
				nextCheckTimes[MovePreview] = (float)EditorApplication.timeSinceStartup + checkInterval;
				CheckPositionChanges(MovePreview);
			}
		};

		// Store and add to update loop
		monitoringActions[MovePreview] = monitoringAction;
		EditorApplication.update += monitoringActions[MovePreview];
		Debug.Log($"Starting set-up for {MovePreview}...");
	}

	public void StopPositionMonitoring(string MovePreview) {
		if (!isLoopActive.ContainsKey(MovePreview) || !isLoopActive[MovePreview]) {
			Debug.LogWarning($"Position monitoring is not active for {MovePreview}.");
			return;
		}

		// Remove the monitoring action
		EditorApplication.update -= monitoringActions[MovePreview];
		monitoringActions.Remove(MovePreview);
		isLoopActive[MovePreview] = false;

		Debug.Log($"Stopping set-up for {MovePreview}.");
	}

	private void CheckPositionChanges(string movePreview) {
		UnityEngine.Transform specificObject = transform.Find(movePreview);

		// Check if parent is null or removed
		if (this == null || transform == null) {
			StopPositionMonitoring(movePreview);
			Debug.LogError("DynamicParent is null or removed! Cancelling position monitoring.");
			return;
		}

		// Stop monitoring when MovePreview is removed and clear all previews.
		if (specificObject == null) {
			StopPositionMonitoring(movePreview);
			Debug.Log($"{movePreview} has been removed. Clearing all MovePreviews.");
			ClearMovePreview();
			return;
		}

		// Calculate top-left positions
		Vector2 topLeftSpecific = CalculateTopLeftPosition(false, movePreview);

		// reference top-left position
		Vector2 referenceTopLeft;

		if (movePreview == "MovePreview1") {
			// Use the dynamic parent's top-left position for MovePreview1
			referenceTopLeft = CalculateTopLeftPosition(true);
		} else {
			// Use the previous MovePreview's top-left position
			int previewNumber = int.Parse(movePreview.Replace("MovePreview", ""));
			string previousMovePreview = $"MovePreview{previewNumber - 1}";
			UnityEngine.Transform previousObject = transform.Find(previousMovePreview);

			if (previousObject != null) {
				referenceTopLeft = CalculateTopLeftPosition(false, previousMovePreview);
			} else {
				Debug.LogError($"Previous MovePreview ({previousMovePreview}) not found for {movePreview}.");
				return;
			}
		}

		// Check if the top-left position has changed
		if (topLeftSpecific != lastSpecificTopLeft) {
			// Update stored top-left position
			lastSpecificTopLeft = topLeftSpecific;

			// Difference result
			Vector2 positionDifference = topLeftSpecific - referenceTopLeft;

			// Update the MoveInterval
			UpdateMoveInterval(movePreview, positionDifference);
		}
	}

	// Calculation Method
	public Vector2 CalculateTopLeftPosition(bool calculateFromParent, string specificObjectName = "") {
		Bounds combinedBounds = new Bounds();
		bool hasBounds = false;

		// Ignore List
		string[] excludedNames = {
			"MovePreview1",
			"MovePreview2",
			"MovePreview3",
			"MovePreview4",
			"MovePreview5",
			"MovePreview6",
			"MovePreview7",
			"MovePreview8",
			"MovePreview9",
			"MovePreview10",
			"MovePreview11",
			"MovePreview12",
			"MovePreview13",
			"MovePreview14",
			"MovePreview15",
			"MovePreview16",
			"VisualPointParent",
			"VisualPoint1",
			"VisualPoint2",
			"VisualPoint3",
			"VisualPoint4",
			"VisualPoint5",
			"VisualPoint6",
			"VisualPoint7",
			"VisualPoint8",
			"VisualPoint9",
			"VisualPoint10",
			"VisualPoint11",
			"VisualPoint12",
			"VisualPoint13",
			"VisualPoint14",
			"VisualPoint15",
			"VisualPoint16"
		};
		string triggerTag = "Trigger";

		void AccumulateBounds(UnityEngine.Transform parent) {
			foreach (Transform child in parent) {
				if (child.CompareTag(triggerTag) && child.GetComponent<DynamicTrigger>()) {
					continue;
				};

				if (Array.Exists(excludedNames, name => name == child.name)) {
					continue;
				}

				SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
				if (renderer != null) {
					if (!hasBounds) {
						combinedBounds = renderer.bounds;
						hasBounds = true;
					} else {
						// Combine bounds
						combinedBounds.Encapsulate(renderer.bounds);
					}
				}

				// Recurse into child objects
				AccumulateBounds(child);
			}
		}

		if (calculateFromParent) {
			AccumulateBounds(transform);
		} else {
			// Calculate from specific object's child objects
			UnityEngine.Transform specificObject = transform.Find(specificObjectName);
			if (specificObject == null) {
				Debug.LogWarning($"Object with name '{specificObjectName}' not found under the Dynamic Parent.");
				return Vector2.zero;
			}

			AccumulateBounds(specificObject);
		}

		if (!hasBounds) {
			Debug.LogWarning("No bounds found for the given objects.");
			return Vector2.zero;
		}

		// Calculate the top-left position in world coordinates

		// Leftmost X in world space
		float topLeftX = RoundToThreeDecimals(combinedBounds.min.x);

		// Topmost Y in world space
		float topLeftY = RoundToThreeDecimals(combinedBounds.max.y);

		return new Vector2(topLeftX, topLeftY);
	}

	private float RoundToThreeDecimals(float value) {
		return Mathf.Round(value * 1000f) / 1000f;
	}


	// Red Visualization Point
	public void AddRedSprite(bool placeInDynamicParent, string objectName, string specificObjectName) {
		Sprite redSprite = Resources.Load<Sprite>("Textures/__red");
		if (redSprite == null) {
			Debug.LogError("Red.png not found in Resources/Textures.");
			return;
		}

		GameObject redObject = new GameObject(objectName);
		SpriteRenderer spriteRenderer = redObject.AddComponent<SpriteRenderer>();
		spriteRenderer.sprite = redSprite;
		redObject.tag = "Unused";
		spriteRenderer.sortingOrder = 999;
		redObject.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

		// Determine parent and position based on boolean parameter
		if (placeInDynamicParent) {
			redObject.transform.SetParent(transform);
			Vector2 topLeftPosition = CalculateTopLeftPosition(true);
			redObject.transform.position = new Vector3(topLeftPosition.x, topLeftPosition.y, 0);
		} else {
			// Find the specific game object by name
			UnityEngine.Transform specificObject = transform.Find(specificObjectName);
			if (specificObject == null) {
				Debug.LogError($"Object with name '{specificObjectName}' not found.");
				DestroyImmediate(redObject);
				return;
			}
			redObject.transform.SetParent(specificObject);
			Vector2 topLeftPosition = CalculateTopLeftPosition(false, specificObjectName);
			redObject.transform.position = new Vector3(topLeftPosition.x, topLeftPosition.y, 0);
		}

	}

	private void ConnectVisualLine(string lineName, string startPointName, string endPointName, string parentName) {
		// Check if line exists
		UnityEngine.Transform parent = transform.Find(parentName);
		if (parent == null) {
			Debug.LogError($"Parent '{parentName}' not found.");
			return;
		}

		if (parent.Find(lineName) != null) {
			Debug.LogWarning($"{lineName} already exists under {parentName}.");
			return;
		}

		GameObject lineObject = new GameObject(lineName);
		lineObject.transform.SetParent(parent);
		lineObject.tag = "Unused";

		// Config
		LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
		lineRenderer.startWidth = 0.05f;
		lineRenderer.endWidth = 0.05f;
		lineRenderer.positionCount = 2;
		lineRenderer.material = new Material(Shader.Find("Sprites/Default")) {
			color = Color.red
		};
		lineRenderer.sortingOrder = 9999;

		// Store references to the start and end points
		UnityEngine.Transform startPoint = transform.Find(startPointName);
		UnityEngine.Transform endPoint = transform.Find(endPointName);

		if (startPoint == null || endPoint == null) {
			Debug.LogError($"One or both points '{startPointName}' or '{endPointName}' not found.");
			DestroyImmediate(lineObject);
			return;
		}

		// Update the line's position
		EditorApplication.CallbackFunction updateLine = () => {
			if (lineRenderer != null && startPoint != null && endPoint != null) {
				lineRenderer.SetPosition(0, startPoint.position);
				lineRenderer.SetPosition(1, endPoint.position);
			}
		};

		// Add the update function to the editor update loop
		EditorApplication.update += updateLine;

		// Remove the update function if line is destroyed
		lineObject.AddComponent<DestroyCallback>().OnDestroyAction = () => {
			EditorApplication.update -= updateLine;
		};
	}


	public void SetUpMove(int number)
	{
		int originalNumber = number;

		// Check if all previous previews exist
		for (int i = 1; i < number; i++)
		{
			if (transform.Find($"MovePreview{i}") == null)
			{
				Debug.LogError($"Please set up MovePreview{i} before setting up MovePreview{number}!");
				return;
			}
		}

		// Check if the current preview already exists
		UnityEngine.Transform existingPreview = transform.Find($"MovePreview{number}");
		if (existingPreview != null)
		{
			Debug.LogWarning($"MovePreview{number} already exists. Skipping setup...");
			return;
		}

		// Duplicate preview and set up visuals
		DuplicateChildrenPreview($"MovePreview{number}");
		if (AddVisualLines)
		{
			AddRedSprite(false, $"VisualPoint{number}", $"MovePreview{number}");
			ConnectVisualLine(
				$"VisualLine{number}",
				$"MovePreview{number - 1}/VisualPoint{number - 1}",
				$"MovePreview{number}/VisualPoint{number}",
				$"MovePreview{number}"
			);
		}

		// Start monitoring the position
		StartPositionMonitoring($"MovePreview{number}");
	}

	public void ClearMovePreview() {
		// List of MovePreview names
		string[] movePreviewNames = {
			"MovePreview1",
			"MovePreview2",
			"MovePreview3",
			"MovePreview4",
			"MovePreview5",
			"MovePreview6",
			"MovePreview7",
			"MovePreview8",
			"MovePreview9",
			"MovePreview10",
			"MovePreview11",
			"MovePreview12",
			"MovePreview13",
			"MovePreview14",
			"MovePreview15",
			"MovePreview16",
			"VisualPointParent"
		};

		foreach (string previewName in movePreviewNames) {
			// Find the MovePreview object under the Dynamic Parent
			UnityEngine.Transform previewObject = transform.Find(previewName);

			// If it exists, destroy it
			if (previewObject != null) {
				// Immediate destroy in editor mode
				DestroyImmediate(previewObject.gameObject);
			}
		}

		if (isImageDynamic) {
			foreach (Transform child in transform) {
				if (child.name == transform.name) {
					DestroyImmediate(child.gameObject);
				}
			}
			isImageDynamic = false;
		}
	}

	public void FinishSetUp() {
		UnityEngine.Transform previewObject = transform.Find("MovePreview1");

		if (previewObject != null) {
			DestroyImmediate(previewObject.gameObject);
			Debug.Log("MovePreview1 has been cleared.");
		} else {
			Debug.LogWarning("MovePreview1 does not exist.");
		}
	}

	public void ResetMoveIntervals() {
		// Reset all MoveIntervals
		MoveInterval1.MoveXAxis = 0f;
		MoveInterval1.MoveYAxis = 0f;
		MoveInterval1.SupportXAxis = 0f;
		MoveInterval1.SupportYAxis = 0f;

		MoveInterval2.MoveXAxis = 0f;
		MoveInterval2.MoveYAxis = 0f;
		MoveInterval2.SupportXAxis = 0f;
		MoveInterval2.SupportYAxis = 0f;

		MoveInterval3.MoveXAxis = 0f;
		MoveInterval3.MoveYAxis = 0f;
		MoveInterval3.SupportXAxis = 0f;
		MoveInterval3.SupportYAxis = 0f;

		MoveInterval4.MoveXAxis = 0f;
		MoveInterval4.MoveYAxis = 0f;
		MoveInterval4.SupportXAxis = 0f;
		MoveInterval4.SupportYAxis = 0f;

		MoveInterval5.MoveXAxis = 0f;
		MoveInterval5.MoveYAxis = 0f;
		MoveInterval5.SupportXAxis = 0f;
		MoveInterval5.SupportYAxis = 0f;

		MoveInterval6.MoveXAxis = 0f;
		MoveInterval6.MoveYAxis = 0f;
		MoveInterval6.SupportXAxis = 0f;
		MoveInterval6.SupportYAxis = 0f;

		MoveInterval7.MoveXAxis = 0f;
		MoveInterval7.MoveYAxis = 0f;
		MoveInterval7.SupportXAxis = 0f;
		MoveInterval7.SupportYAxis = 0f;

		MoveInterval8.MoveXAxis = 0f;
		MoveInterval8.MoveYAxis = 0f;
		MoveInterval8.SupportXAxis = 0f;
		MoveInterval8.SupportYAxis = 0f;

		MoveInterval9.MoveXAxis = 0f;
		MoveInterval9.MoveYAxis = 0f;
		MoveInterval9.SupportXAxis = 0f;
		MoveInterval9.SupportYAxis = 0f;

		MoveInterval10.MoveXAxis = 0f;
		MoveInterval10.MoveYAxis = 0f;
		MoveInterval10.SupportXAxis = 0f;
		MoveInterval10.SupportYAxis = 0f;

		MoveInterval11.MoveXAxis = 0f;
		MoveInterval11.MoveYAxis = 0f;
		MoveInterval11.SupportXAxis = 0f;
		MoveInterval11.SupportYAxis = 0f;

		MoveInterval12.MoveXAxis = 0f;
		MoveInterval12.MoveYAxis = 0f;
		MoveInterval12.SupportXAxis = 0f;
		MoveInterval12.SupportYAxis = 0f;

		MoveInterval13.MoveXAxis = 0f;
		MoveInterval13.MoveYAxis = 0f;
		MoveInterval13.SupportXAxis = 0f;
		MoveInterval13.SupportYAxis = 0f;

		MoveInterval14.MoveXAxis = 0f;
		MoveInterval14.MoveYAxis = 0f;
		MoveInterval14.SupportXAxis = 0f;
		MoveInterval14.SupportYAxis = 0f;

		MoveInterval15.MoveXAxis = 0f;
		MoveInterval15.MoveYAxis = 0f;
		MoveInterval15.SupportXAxis = 0f;
		MoveInterval15.SupportYAxis = 0f;

		MoveInterval16.MoveXAxis = 0f;
		MoveInterval16.MoveYAxis = 0f;
		MoveInterval16.SupportXAxis = 0f;
		MoveInterval16.SupportYAxis = 0f;

		Debug.Log("MoveIntervals Resetted.");
	}

	public void EditMoveSetup() {
		// Check and set up movements
		if (MovementUsage.UseMovement1) SetUpMoveWithOffset(1);
		if (MovementUsage.UseMovement2) SetUpMoveWithOffset(2);
		if (MovementUsage.UseMovement3) SetUpMoveWithOffset(3);
		if (MovementUsage.UseMovement4) SetUpMoveWithOffset(4);
		if (MovementUsage.UseMovement5) SetUpMoveWithOffset(5);
		if (MovementUsage.UseMovement6) SetUpMoveWithOffset(6);
		if (MovementUsage.UseMovement7) SetUpMoveWithOffset(7);
		if (MovementUsage.UseMovement8) SetUpMoveWithOffset(8);
		if (MovementUsage.UseMovement9) SetUpMoveWithOffset(9);
		if (MovementUsage.UseMovement10) SetUpMoveWithOffset(10);
		if (MovementUsage.UseMovement11) SetUpMoveWithOffset(11);
		if (MovementUsage.UseMovement12) SetUpMoveWithOffset(12);
		if (MovementUsage.UseMovement13) SetUpMoveWithOffset(13);
		if (MovementUsage.UseMovement14) SetUpMoveWithOffset(14);
		if (MovementUsage.UseMovement15) SetUpMoveWithOffset(15);
		if (MovementUsage.UseMovement16) SetUpMoveWithOffset(16);
	}

	private void SetUpMoveWithOffset(int moveIndex) {
		switch (moveIndex) {
			case 1:
				SetUpMove(1);
				break;
			case 2:
				SetUpMove(2);
				break;
			case 3:
				SetUpMove(3);
				break;
			case 4:
				SetUpMove(4);
				break;
			case 5:
				SetUpMove(5);
				break;
			case 6:
				SetUpMove(6);
				break;
			case 7:
				SetUpMove(7);
				break;
			case 8:
				SetUpMove(8);
				break;
			case 9:
				SetUpMove(9);
				break;
			case 10:
				SetUpMove(10);
				break;
			case 11:
				SetUpMove(11);
				break;
			case 12:
				SetUpMove(12);
				break;
			case 13:
				SetUpMove(13);
				break;
			case 14:
				SetUpMove(14);
				break;
			case 15:
				SetUpMove(15);
				break;
			case 16:
				SetUpMove(16);
				break;
			default:
				Debug.LogError($"Invalid move index: {moveIndex}");
				return;
		}

		// Calculate cumulative offset for the MovePreview
		Vector3 cumulativeOffset = Vector3.zero;
		for (int i = 1; i <= moveIndex; i++) {
			var interval = GetMoveInterval(i);
			cumulativeOffset += new Vector3(interval.MoveXAxis, interval.MoveYAxis, 0f);
		}

		// Apply offset to the MovePreview position
		string movePreviewName = $"MovePreview{moveIndex}";
		Transform movePreview = transform.Find(movePreviewName);
		if (movePreview != null) {
			movePreview.position += cumulativeOffset;
		} else {
			Debug.LogWarning($"{movePreviewName} not found.");
		}
	}

	private Movement GetMoveInterval(int index) {
		return index switch {
			1 => MoveInterval1,
			2 => MoveInterval2,
			3 => MoveInterval3,
			4 => MoveInterval4,
			5 => MoveInterval5,
			6 => MoveInterval6,
			7 => MoveInterval7,
			8 => MoveInterval8,
			9 => MoveInterval9,
			10 => MoveInterval10,
			11 => MoveInterval11,
			12 => MoveInterval12,
			13 => MoveInterval13,
			14 => MoveInterval14,
			15 => MoveInterval15,
			16 => MoveInterval16,
			_ => null,
		};
	}

	// Previewing
	private bool isPlayingPreview = false;
	private bool _isPreviewDisabled = false;
	public bool IsPreviewDisabled {
		get => _isPreviewDisabled;
		private set => _isPreviewDisabled = value;
	}

	private float postPreviewResetDelay = 0.5f; // Delay before reset
	private bool isWaitingForDelay = false; // Track the delay state

	private Vector3 originalPosition;
	private List<Movement> activeMovements;
	private int currentMovementIndex = 0;
	private float elapsedTime = 0f;


	public void PlayPreview() {
		if (isPlayingPreview) {
			Debug.LogWarning("Preview is already running!");
			return;
		}

		// Null check
		if (this == null || transform == null) {
			Debug.LogError("DynamicParent is null or removed! Cancelling preview.");
			return;
		}

		// Check for "MovePreview1"
		UnityEngine.Transform movePreview1 = transform.Find("MovePreview1");
		if (movePreview1 != null) {
			Debug.LogWarning("Cannot play preview while setting up move!");
			return;
		}

		// Disable buttons during preview
		IsPreviewDisabled = true;

		// Original position
		originalPosition = transform.position;

		// Reset elapsed time
		elapsedTime = 0f;

		// Start from the first movement
		currentMovementIndex = 0;

		// active movements in sequence
		activeMovements = new List<Movement>();
		if (MovementUsage.UseMovement1) activeMovements.Add(MoveInterval1);
		if (MovementUsage.UseMovement2) activeMovements.Add(MoveInterval2);
		if (MovementUsage.UseMovement3) activeMovements.Add(MoveInterval3);
		if (MovementUsage.UseMovement4) activeMovements.Add(MoveInterval4);
		if (MovementUsage.UseMovement5) activeMovements.Add(MoveInterval5);
		if (MovementUsage.UseMovement6) activeMovements.Add(MoveInterval6);
		if (MovementUsage.UseMovement7) activeMovements.Add(MoveInterval7);
		if (MovementUsage.UseMovement8) activeMovements.Add(MoveInterval8);
		if (MovementUsage.UseMovement9) activeMovements.Add(MoveInterval9);
		if (MovementUsage.UseMovement10) activeMovements.Add(MoveInterval10);
		if (MovementUsage.UseMovement11) activeMovements.Add(MoveInterval11);
		if (MovementUsage.UseMovement12) activeMovements.Add(MoveInterval12);
		if (MovementUsage.UseMovement13) activeMovements.Add(MoveInterval13);
		if (MovementUsage.UseMovement14) activeMovements.Add(MoveInterval14);
		if (MovementUsage.UseMovement15) activeMovements.Add(MoveInterval15);
		if (MovementUsage.UseMovement16) activeMovements.Add(MoveInterval16);

		if (activeMovements.Count == 0) {
			Debug.LogWarning("No movements are active for preview!");

			// Re-enable buttons if no movements
			IsPreviewDisabled = false;

			return;
		}

		// preview start
		isPlayingPreview = true;
		isWaitingForDelay = activeMovements[0].Delay > 0f;
		EditorApplication.update += UpdatePreviewMovement;
	}

	private void UpdatePreviewMovement() {
		if (this == null || transform == null) {
			Debug.LogError("Dynamic Parent is removed during preview! Stopping preview.");
			StopPreviewOnNull();
			return;
		}

		Movement currentMovement = activeMovements[currentMovementIndex];

		// Delay 
		if (elapsedTime < currentMovement.Delay) {
			// Delay is active
			isWaitingForDelay = true;
			elapsedTime += Time.deltaTime;
			// Pause here until the delay is over
			return;
		}

		// Delay is over
		isWaitingForDelay = false;

		float adjustedTime = elapsedTime - currentMovement.Delay;
		elapsedTime += Time.deltaTime;

		// Calculate start, target, and support positions
		Vector3 startPosition = currentMovementIndex == 0 ? originalPosition :
			originalPosition + CalculateCumulativeOffset(currentMovementIndex - 1);

		Vector3 targetPosition = startPosition + new Vector3(
			currentMovement.MoveXAxis,
			currentMovement.MoveYAxis,
			0f
		);

		Vector3 supportPosition = startPosition + new Vector3(
			currentMovement.SupportXAxis,
			currentMovement.SupportYAxis,
			0f
		);

		// Smooth movement using easing
		float t = Mathf.Clamp01(adjustedTime / currentMovement.MoveDuration);
		transform.position = CalculateEasedPosition(t, startPosition, supportPosition, targetPosition);

		// Check if movement is complete
		if (adjustedTime >= currentMovement.MoveDuration) {
			// Snap to the final position
			transform.position = targetPosition;
			currentMovementIndex++;
			// Reset elapsed time for the next movement
			elapsedTime = 0f;

			// Handle next movement or finishing
			if (currentMovementIndex < activeMovements.Count) {
				// Prepare for the next interval
				elapsedTime = 0f;

				// Update delay status
				isWaitingForDelay = activeMovements[currentMovementIndex].Delay > 0f;
			} else {
				FinishPreview();
			}
		}
	}

	private void FinishPreview() {
		isPlayingPreview = false;
		EditorApplication.update -= UpdatePreviewMovement;

		if (ResetAfterPreviewFinish) {
			// Reset elapsed time for potential delay handling
			elapsedTime = 0f;
			EditorApplication.update += HandleResetDelay;
		} else {
			// Reenable buttons
			IsPreviewDisabled = false;
		}
	}

	private void StopPreviewOnNull() {
		isPlayingPreview = false;
		EditorApplication.update -= UpdatePreviewMovement;

		if (ResetAfterPreviewFinish) {
			elapsedTime = 0f;
			EditorApplication.update -= HandleResetDelay;
		}

		IsPreviewDisabled = false;
	}

	private void HandleResetDelay() {
		if ((float)EditorApplication.timeSinceStartup - elapsedTime >= postPreviewResetDelay) {
			ResetPreviewPosition();
			EditorApplication.update -= HandleResetDelay;

			// Reenable the buttons
			IsPreviewDisabled = false;
		}
	}

	public void ResetPreviewPosition() {
		if (isPlayingPreview) {
			Debug.LogWarning("Cannot reset position during preview!");
			return;
		}
		transform.position = originalPosition;
	}

	private Vector3 CalculateEasedPosition(float t, Vector3 start, Vector3 support, Vector3 end) {
		// Linear case: When support is exactly midpoint of start and end
		if (support == (start + end) / 2f) {
			// Purely linear
			return Vector3.Lerp(start, end, t);
		}

		// Quadratic Bézier easing for custom control

		// Complement of t
		float u = 1 - t;
		Vector3 point = u * u * start;		// (1-t)^2 * P0
		point += 2 * u * t * support;		// 2(1-t)t * P1
		point += t * t * end;				// t^2 * P2
		return point;
	}

	private Vector3 CalculateCumulativeOffset(int lastIndex) {
		// Calculate the cumulative offset from all previous movements
		Vector3 cumulativeOffset = Vector3.zero;
		for (int i = 0; i <= lastIndex; i++) {
			cumulativeOffset += new Vector3(
				activeMovements[i].MoveXAxis,
				activeMovements[i].MoveYAxis,
				0f
			);
		}

		return cumulativeOffset;
	}
}

[CustomEditor(typeof(Dynamic))]
public class DynamicButton : Editor {
	public override void OnInspectorGUI() {
		DrawDefaultInspector();
		Dynamic dynamicComponent = (Dynamic)target;

		// Disable buttons during preview
		GUI.enabled = !dynamicComponent.IsPreviewDisabled;

		if (GUILayout.Button("Play Preview")) { dynamicComponent.PlayPreview(); }
		if (GUILayout.Button("Reset Preview")) { dynamicComponent.ResetPreviewPosition(); }

		if (GUILayout.Button("Set-Up Move 1")) { dynamicComponent.SetUpMove(1); }
		if (GUILayout.Button("Set-Up Move 2")) { dynamicComponent.SetUpMove(2); }
		if (GUILayout.Button("Set-Up Move 3")) { dynamicComponent.SetUpMove(3); }
		if (GUILayout.Button("Set-Up Move 4")) { dynamicComponent.SetUpMove(4); }
		if (GUILayout.Button("Set-Up Move 5")) { dynamicComponent.SetUpMove(5); }
		if (GUILayout.Button("Set-Up Move 6")) { dynamicComponent.SetUpMove(6); }
		if (GUILayout.Button("Set-Up Move 7")) { dynamicComponent.SetUpMove(7); }
		if (GUILayout.Button("Set-Up Move 8")) { dynamicComponent.SetUpMove(8); }
		if (GUILayout.Button("Set-Up Move 9")) { dynamicComponent.SetUpMove(9); }
		if (GUILayout.Button("Set-Up Move 10")) { dynamicComponent.SetUpMove(10); }
		if (GUILayout.Button("Set-Up Move 11")) { dynamicComponent.SetUpMove(11); }
		if (GUILayout.Button("Set-Up Move 12")) { dynamicComponent.SetUpMove(12); }
		if (GUILayout.Button("Set-Up Move 13")) { dynamicComponent.SetUpMove(13); }
		if (GUILayout.Button("Set-Up Move 14")) { dynamicComponent.SetUpMove(14); }
		if (GUILayout.Button("Set-Up Move 15")) { dynamicComponent.SetUpMove(15); }
		if (GUILayout.Button("Set-Up Move 16")) { dynamicComponent.SetUpMove(16); }
		if (GUILayout.Button("Edit Movement")) { dynamicComponent.EditMoveSetup(); }

		if (GUILayout.Button("Finish All Set-Up")) { dynamicComponent.FinishSetUp(); }
		if (GUILayout.Button("Reset MoveIntervals")) { dynamicComponent.ResetMoveIntervals(); }

		// Re-enable GUI for other actions
		GUI.enabled = true;
	}

}

public class DestroyCallback : MonoBehaviour {
	public Action OnDestroyAction;

	private void OnDestroy() {
		OnDestroyAction?.Invoke();
	}
}