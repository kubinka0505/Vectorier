using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Vectorier;

using Debug = UnityEngine.Debug;

// -=-=-=- //

public class ConvertXmlObjectsGUI : EditorWindow {
	// === GUI Settings =====
	private float windowMinWidth = 655;
	private float windowMinHeight = 228;
	private float windowSpacing = 5;

	private int maxfilePathLegnth = 75;

	private string loc_filePath = "File Path";
	private string loc_fileReset = "↺";
	private string loc_fileSelect = "⋯";
	private string loc_fileSelectXML = "Select XML File";

	private string loc_caseSensitive = "Case-sensitive.";
	private string loc_separatable = "Separatable by:\n -{0}";
	private Dictionary<string, string> loc_sepCharsDisplayMappings = new Dictionary<string, string> {
		{ "|", "pipe" },
		{ "\n", "newline" }
	};

	private string loc_textArea = "Objects to convert";
	private string loc_occurence = "Find n-th occurence";
	private string loc_autoTag = "Apply correct tags";
	private string loc_debugObjectFound = "Debug object found";

	private string loc_filePath_ToolTip = "Target file path.";
	private string loc_textArea_ToolTip = "Textarea for objects to convert.";
	private string loc_debugObjectFound_ToolTip = "Logs information regarding unspecified object with content.";
	private string loc_autoTag_ToolTip = @"Applies ""Unused"" tag to converted objects, otherwise ""Object"".";
	private string loc_occurence_ToolTip = @"
	Amount of iterations that function will perform in order to find the last occurence of object in the file.

	ⓘ Returns the number of maximum iterations when an object is not found but exists.
	";

	private int occurence_min = 1;
	private int occurence_max = 75;

	private string loc_convert = "Convert";

	// -=-=-=- //

	private int orderInLayer = 0;

	private GameObject lastContent;
	private GameObject actualObject;
	private GameObject dummyObject;

	// File paths
	private string objects_path;
	private string filePathParent;
	private string filePathExt;
	private string filePath;

	// Other
	private string objectsToConvert = "";
	private string[] sepChars = { "\n" }; //, "|" };
	private string[] sepCharsDisplay;
	private string emSpace = " ";

	private Vector2 windowMinSize;
	private Vector2 windowMaxSize;

	private int occurence_n = 1;

	private int buttonSmallSize = 25;
	private int buttonBigSize = 70;
	protected string filePathReverted;

	// Checkboxes
	private bool autoTag = true;
	private bool debugObjectFound = false;

	[MenuItem("Vectorier/Convert XML objects")]
	public static void ShowWindow() {
		// Create window instance
		ConvertXmlObjectsGUI window = GetWindow<ConvertXmlObjectsGUI>("Convert XML objects");

		window.windowMinSize = new Vector2(window.windowMinWidth, window.windowMinHeight);
		window.windowMaxSize = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);

		// Center
		window.position = new Rect(
			(Screen.currentResolution.width - window.windowMinSize.x) / 3,
			(Screen.currentResolution.height - window.windowMinSize.y) / 3,
			window.windowMinSize.x,
			window.windowMinSize.y
		);

		window.minSize = window.windowMinSize;
		window.maxSize = window.windowMaxSize;
	}

	private void OnEnable() {
		// Initialize paths in OnEnable to avoid errors
		objects_path = Path.Combine(Application.dataPath, "XML", "dzip", "level_xml", "objects.xml");

		// Filepaths manipulation
		filePathParent = Directory.GetParent(Application.dataPath).ToString();
		filePathExt = Path.GetExtension(objects_path);

		filePath = objects_path.Substring(filePathParent.Length + 1);
		filePath = filePath.Replace(Path.DirectorySeparatorChar, '/');
		filePathReverted = filePath;

		// Apply replacements
		loc_caseSensitive = loc_caseSensitive.Trim();
		loc_separatable = loc_separatable.Trim();

		loc_occurence_ToolTip = loc_occurence_ToolTip.Trim();

		sepChars = sepChars.OrderBy(o => o).ToArray();
		sepCharsDisplay = sepChars
			.Select(s => loc_sepCharsDisplayMappings.Aggregate(s, (current, kvp) => current.Replace(kvp.Key, kvp.Value)))
			.ToArray();
	}

	private void OnGUI() {
		GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
		boxStyle.padding = new RectOffset(5, 5, 5, 5);
		boxStyle.normal.background = null;

		GUILayout.BeginVertical("box");

		// Start Horizontal (2 columns)
		GUILayout.BeginHorizontal();

		// ======== COLUMN 1 (Left) ======== //
		GUILayout.BeginVertical(
			GUILayout.MinWidth(windowMinWidth / 2f),
			GUILayout.MaxWidth(windowMinWidth / 2f)
		);

		// File path input
		GUIContent lc_filePath = new GUIContent(loc_filePath, loc_filePath_ToolTip);
		GUILayout.Label(lc_filePath);

		GUILayout.BeginHorizontal();

		filePath = GUILayout.TextField(
			filePath,
			maxfilePathLegnth
		);

		// Reset path button
		if (GUILayout.Button(
			loc_fileReset,
			GUILayout.Width(buttonSmallSize)
		)) {
			filePath = filePathReverted;
		}

		// Browse file button
		if (GUILayout.Button(
			loc_fileSelect,
			GUILayout.Width(buttonSmallSize)
		)) {
			string selectedPath = EditorUtility.OpenFilePanel(loc_fileSelectXML, "Assets/XML/dzip/level_xml", "xml");

			if (!string.IsNullOrEmpty(selectedPath)) {
				filePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
			}
		}
		GUILayout.EndHorizontal();

		// Warnings
		GUILayout.Space(windowSpacing);

		EditorGUILayout.HelpBox(emSpace + loc_caseSensitive, MessageType.Warning);
		EditorGUILayout.HelpBox(
			"\n" + emSpace + (
				string.Format(
					loc_separatable, 
					string.Join("\n -", sepCharsDisplay)
				) + "\n"
			).Replace("\n", emSpace + "\n"),
			MessageType.Info
		);

		// Checkboxes (... [])
		GUILayout.Space(windowSpacing);

		// Occurence slider
		GUILayout.BeginHorizontal();
		GUIContent lc_occurence = new GUIContent(loc_occurence, loc_occurence_ToolTip);
		GUILayout.Label(lc_occurence);
		GUILayout.BeginHorizontal();
		GUILayout.Space(windowSpacing * 10); 
		occurence_n = (int)EditorGUILayout.Slider(
			(float)occurence_n,
			occurence_min,
			occurence_max
		);
		GUILayout.EndHorizontal();
		GUILayout.EndHorizontal();

		// Auto tag
		GUILayout.Space(windowSpacing);

		GUILayout.BeginHorizontal();
		GUIContent lc_autoTag = new GUIContent(loc_autoTag, loc_autoTag_ToolTip);
		GUILayout.Label(lc_autoTag);
		GUILayout.FlexibleSpace();
		autoTag = EditorGUILayout.Toggle(
			autoTag,
			GUILayout.Width(15)
		);
		GUILayout.EndHorizontal();

		// Debug object found
		GUILayout.Space(windowSpacing);

		GUILayout.BeginHorizontal();
		GUIContent lc_debugObjectFound = new GUIContent(loc_debugObjectFound, loc_debugObjectFound_ToolTip);
		GUILayout.Label(lc_debugObjectFound);
		GUILayout.FlexibleSpace();
		debugObjectFound = EditorGUILayout.Toggle(
			debugObjectFound,
			GUILayout.Width(15)
		);
		GUILayout.EndHorizontal();

		// End 1st column
		GUILayout.EndVertical();

		// ======== COLUMN 2 (Right) ======== //
		GUILayout.Space(windowSpacing);

		GUILayout.BeginVertical(GUILayout.ExpandWidth(true));

		// Objects text area
		GUIContent lc_textArea = new GUIContent(loc_textArea, loc_textArea_ToolTip);
		GUILayout.Label(lc_textArea);
		objectsToConvert = EditorGUILayout.TextArea(
			objectsToConvert,
			GUILayout.MaxHeight(windowMaxSize.y - 50),
			GUILayout.ExpandWidth(true),
			GUILayout.ExpandHeight(true)
		);

		// Convert button
		GUILayout.Space(windowSpacing);

		GUILayout.BeginHorizontal();
		if (GUILayout.Button(
			loc_convert,
			GUILayout.Height(buttonBigSize),
			GUILayout.ExpandWidth(true)
		)) {
			string[] objectsToConvertArr = objectsToConvert
				.Split(sepChars, StringSplitOptions.RemoveEmptyEntries)
				.Select(o => o.Trim())
				.Distinct()
				.ToArray();

			if (string.IsNullOrEmpty(filePath) || objectsToConvertArr.Length == 0) {
				Debug.LogError("Please provide both the file path and objects to convert");
				return;
			}

			if (objectsToConvertArr.Length > 1 && occurence_n > 1) {
				Debug.LogWarning("Multiple objects combined with multiple ccurences can result in objects lack");
			}

			foreach (string objectToConvert in objectsToConvertArr) {
				if (!string.IsNullOrEmpty(objectToConvert)) {
					ConvertXmlToObject(filePath, objectToConvert, occurence_n);
				}
			}
		}
		GUILayout.EndHorizontal();

		// End 2nd Column
		GUILayout.EndVertical();

		// End main horizontal layout
		GUILayout.EndHorizontal();

		GUILayout.EndVertical();
	}

	// -=-=-=- //
	// Main

	private void ConvertXmlToObject(string path, string objectToConvert, int occurence) {
		if (string.IsNullOrEmpty(path)) {
			Debug.LogError("No XML file selected!");
			return;
		}

		if (!File.Exists(path)) {
			Debug.LogError($@"Provided XML file (""{path}"") doesn't exist");
			return;
		}

		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();

		XmlDocument obj = new XmlDocument();
		try {
			obj.Load(path);
		} catch (Exception e) {
			Debug.LogError($@"Failed to load XML file: ""{e.Message}""");
			return;
		}

		// Keep track of the total occurrences found
		int occurence_placeholder = 0;
		int totalOccurrences = 0;

		// Store the last found object
		bool objectFound = false;
		bool objectExists = false;
		XmlNode lastFoundObject = null;

		int docNum = 0;

		while (!objectFound && docNum < 3) {
			foreach (XmlNode node in obj.DocumentElement.SelectSingleNode("/Root/Objects")) {
				if (node.Name == "Object" && node.Attributes["Name"].Value == objectToConvert) {
					objectExists = true;

					occurence_placeholder++;
					totalOccurrences++;

					if (occurence == occurence_placeholder) {
						// Store the last found object
						lastFoundObject = node;
						objectFound = true;
						Debug.Log($@"""{objectToConvert}"" found");

						// Reset orderInLayer
						orderInLayer = 0;

						// Extract variables and expressions from the "Properties" section
						Dictionary<string, float> variables = ExtractVariables(node);

						foreach (XmlNode content in node["Content"]) {
							StoreExtraArguments(orderInLayer, autoTag, debugObjectFound);
							InstantiateObject(content, obj, variables, false, null, objectToConvert);
							break;
						}
					}
				}
			}

			docNum++;
		}		

		// If we found the object, instantiate the last found object
		if (objectFound && lastFoundObject != null) {
			// After processing n occurrences, instantiate the last found object
			Dictionary<string, float> variables = ExtractVariables(lastFoundObject);

			foreach (XmlNode content in lastFoundObject["Content"]) {
				StoreExtraArguments(orderInLayer, autoTag, debugObjectFound);
				InstantiateObject(content, obj, variables, false, null, objectToConvert);
			}
		}

		stopwatch.Stop();
		TimeSpan ts = stopwatch.Elapsed;

		string formattedTime = ts.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture);

		if (objectFound) {
			Debug.Log($@"""{objectToConvert}"" processed ({formattedTime} seconds)");
			actualObject = null;
		} else {
			Debug.LogError($@"""{objectToConvert}"" not found in the XML file");

			if (objectExists) {
				Debug.LogWarning($@"""{objectToConvert}"" total occurrences available in this file: {totalOccurrences - 1}");
			}
		}
	}

	private HashSet<string> processedNodes = new HashSet<string>(); // This will track processed nodes

	private void InstantiateObject(XmlNode content, XmlDocument xmlDoc, Dictionary<string, float> variables, bool useParent, GameObject parent, string objectToConvert) {
		// Use class fields directly without redeclarations
		orderInLayer = RetrieveStoredOrderInLayer();
		autoTag = RetrieveStoredAutoTag();
		debugObjectFound = RetrieveStoredDebugObjectFound();

		// Replace variable placeholders with actual values
		ReplacePlaceholders(content, variables);

		// Generate a random parent name for tempParentObject
		int rngint = (int)1e8f;
		int randint = UnityEngine.Random.Range(rngint, rngint - 1);

		if (actualObject == null) {
			GameObject newgo = new GameObject($"{objectToConvert}_{randint}");

			actualObject = Instantiate(newgo, Vector3.zero, Quaternion.identity);
			DestroyImmediate(newgo);
			actualObject.name = objectToConvert;
		}

		if (!autoTag) {
			actualObject.tag = "Object";
		}

		// Check if the object was already processed to prevent duplicate instantiation
		string nodeKey = content.Name + "_" + content.Attributes["Name"]?.Value + "_" + content.Attributes["Position"]?.Value;
		
		// Skip this node if it was already processed
		if (processedNodes.Contains(nodeKey)) {
			return;
		}

		processedNodes.Add(nodeKey);

		// Object
		if (content.Name == "Object") {
			XmlAttribute referencedObjectName = content.Attributes["Name"];

			if (referencedObjectName != null) {
				if (debugObjectFound) {
					Debug.Log($"Found {content.Name}: {referencedObjectName.Value}");
				}

				XmlNode referencedObjectNode = FindObjectNodeByName(referencedObjectName.Value, xmlDoc, objects_path);

				// Check if the object has child nodes (nested Content)
				if (content.SelectSingleNode("Content") != null) {

					Vector3 position = Engine.Math.GameUnits.DivideVector(content);

					GameObject nestedObject = new GameObject(referencedObjectName.Value);
					nestedObject.transform.position = position;
					if (useParent) {
						nestedObject.transform.SetParent(parent.transform, false);
					} else {
						nestedObject.transform.parent = actualObject.transform;
					}

					if (referencedObjectNode != null) {
						// Recursively instantiate nested objects
						foreach (XmlNode nestedContent in referencedObjectNode.SelectNodes("Content/*")) {
							InstantiateObject(nestedContent, xmlDoc, variables, true, nestedObject, objectToConvert);
						}
					}

					// Recursively instantiate nested objects
					foreach (XmlNode nestedContent in content.SelectNodes("Content/*")) {
						InstantiateObject(nestedContent, xmlDoc, variables, true, nestedObject, objectToConvert);
					}
				} else {
					// This is a simple one-line object, find its definition in the XML and instantiate
					if (referencedObjectNode != null) {
						Vector3 position = Engine.Math.GameUnits.DivideVector(content);

						// Temporarily set the position of the actualObject to match the reference position
						GameObject nestedObject = new GameObject(referencedObjectName.Value);
						nestedObject.transform.position = position;
						if (useParent) {
							nestedObject.transform.SetParent(parent.transform, false);
						} else {
							nestedObject.transform.parent = actualObject.transform;
						}

						// Recursively instantiate nested objects
						foreach (XmlNode nestedContent in referencedObjectNode.SelectNodes("Content/*")) {
							InstantiateObject(nestedContent, xmlDoc, variables, true, nestedObject, objectToConvert);
						}
					} else {
						Debug.LogError($@"Referenced object ""{referencedObjectName.Value}"" not found in the XML file");
					}
				}
			} else {
				if (debugObjectFound) {
					Debug.Log($"Found {content.Name}");
				}

				Vector3 position = Engine.Math.GameUnits.DivideVector(content);

				GameObject nestedObject = new GameObject(string.Empty);
				nestedObject.transform.position = position;
				if (useParent) {
					nestedObject.transform.SetParent(parent.transform, false);
				} else {
					nestedObject.transform.parent = actualObject.transform;
				}

				// Recursively instantiate nested objects
				foreach (XmlNode nestedContent in content.SelectNodes("Content/*")) {
					InstantiateObject(nestedContent, xmlDoc, variables, true, nestedObject, objectToConvert);
				}
			}

			return;
		}

		// Check and handle the content based on its name
		switch (content.Name) {
			case "Image":
				HandleImageNode(content, variables);
				lastContent.tag = autoTag ? "Image" : "Unused";
				break;

			case "Platform":
				HandlePlatformNode(content);
				lastContent.tag = autoTag ? "Platform" : "Unused";
				break;

			case "Trapezoid":
				HandleTrapezoidNode(content);
				lastContent.tag = autoTag ? "Trapezoid" : "Unused";
				break;

			case "Trigger":
				HandleTriggerNode(content);
				lastContent.tag = autoTag ? "Trigger" : "Unused";
				break;

			case "Area":
				HandleAreaNode(content);
				lastContent.tag = autoTag ? "Area" : "Unused";
				break;

			default:
				return;
		}

		// Log debug message if enabled
		if (debugObjectFound) {
			string message = (content.Name == "Trigger" || content.Name == "Area")
				? $"Found {content.Name}: {content.Attributes["Name"].Value}"
				: $"Found {content.Name}";
			Debug.Log(message);
		}

		// Remove last "(Clone)" from object's name
		string suffixClone = "(Clone)";

		string[] splitResult = lastContent.name.Split(new string[] { suffixClone }, StringSplitOptions.None);
		lastContent.name = string.Join(suffixClone, splitResult.Take(splitResult.Length - 1));

		if (useParent) {
			lastContent.transform.SetParent(parent.transform, false);
		} else {
			lastContent.transform.parent = actualObject.transform;
		}
		DestroyImmediate(dummyObject);
	}

	// -=-=-=- //
	// Nodes

	// Images
	void HandleImageNode(XmlNode content, Dictionary<string, float> variables) {
		ReplacePlaceholders(content, variables);

		string className = content.Attributes.GetNamedItem("ClassName").Value.ToLower();

		Vector3 position = Engine.Math.GameUnits.DivideVector(content);

		lastContent = Instantiate(
			dummyObject = new GameObject(className),
			position,
			Quaternion.identity
		);

		SpriteRenderer spriteRenderer = lastContent.AddComponent<SpriteRenderer>();
		spriteRenderer.sprite = LoadSpriteFromResources(className);

		// Set order in layer for the image
		spriteRenderer.sortingOrder = orderInLayer;
		orderInLayer++;

		float width = float.Parse(content.Attributes.GetNamedItem("Width").Value);
		float height = float.Parse(content.Attributes.GetNamedItem("Height").Value);

		float originalWidth = spriteRenderer.sprite.texture.width;
		float originalHeight = spriteRenderer.sprite.texture.height;

		Vector3 scale = new Vector3(width / originalWidth, height / originalHeight, 1);
		lastContent.transform.localScale = scale;

		// Check if there are Matrix transformations
		if (content.HasChildNodes) {
			// Check for either Matrix or MatrixTransform node
			XmlNode matrixNode = content.SelectSingleNode("Properties/Static/Matrix") ?? content.SelectSingleNode("Properties/Static/MatrixTransform");

			if (matrixNode != null) {
				Debug.Log("Matrix node found. Attempting to apply transformation");
				ConvertFromMarmaladeMatrix(matrixNode, lastContent.transform, spriteRenderer, width, height, lastContent, className);
			}
		}
	}

	// Trapezoid
	void HandleTrapezoidNode(XmlNode content) {
		// Parse position
		Vector3 position = Engine.Math.GameUnits.DivideVector(content);

		// Parse trapezoid type
		int t_type = int.Parse(content.Attributes.GetNamedItem("Type").Value);

		// Create the trapezoid object
		lastContent = Instantiate(
			dummyObject = new GameObject("Trapezoid"),
			position,
			Quaternion.identity
		);

		// Add the sprite renderer and load the sprite
		SpriteRenderer spriteRenderer = lastContent.AddComponent<SpriteRenderer>();
		spriteRenderer.sprite = Resources.Load<Sprite>("Textures/trapezoid_type" + t_type);

		// Set order in layer
		spriteRenderer.sortingOrder = 1000;

		// Compute and apply size
		float width = float.Parse(content.Attributes.GetNamedItem("Width").Value);
		float height = float.Parse(content.Attributes.GetNamedItem("Height").Value);
		float height1 = float.Parse(content.Attributes.GetNamedItem("Height1").Value);

		float originalWidth = spriteRenderer.sprite.texture.width;
		float originalHeight = spriteRenderer.sprite.texture.height;

		float type1height = height1 - height;
		float type2height = height - height1;

		float heightFactor = (t_type == 1) ? type1height : (t_type == 2) ? type2height : 1;
		Vector3 scale = new Vector3(width / originalWidth, heightFactor / originalHeight);
	}

	// Platforms
	void HandlePlatformNode(XmlNode content) {
		Vector3 position = Engine.Math.GameUnits.DivideVector(content);

		lastContent = Instantiate(
			dummyObject = new GameObject("Platform"),
			position,
			Quaternion.identity
		);

		SpriteRenderer spriteRenderer = lastContent.AddComponent<SpriteRenderer>();
		spriteRenderer.sprite = Resources.Load<Sprite>("Textures/collision");

		// Set order in layer to 253 for the platform
		spriteRenderer.sortingOrder = 253;

		float width = float.Parse(content.Attributes.GetNamedItem("Width").Value);
		float height = float.Parse(content.Attributes.GetNamedItem("Height").Value);

		float originalWidth = spriteRenderer.sprite.texture.width;
		float originalHeight = spriteRenderer.sprite.texture.height;

		Vector3 scale = new Vector3(width / originalWidth, height / originalHeight, 1);
		lastContent.transform.localScale = scale;
	}

	// Trigger
	void HandleTriggerNode(XmlNode content) {
		Vector3 position = Engine.Math.GameUnits.DivideVector(content);

		XmlAttribute name = content.Attributes["Name"];

		lastContent = Instantiate(
			dummyObject = new GameObject(),
			position,
			Quaternion.identity
		);
		
		lastContent.name = name?.Value ?? string.Empty;

		SpriteRenderer spriteRenderer = lastContent.AddComponent<SpriteRenderer>();
		try {
			spriteRenderer.sprite = Resources.Load<Sprite>("Textures/trigger");
		} catch {
			spriteRenderer.sprite = Resources.Load<Sprite>("Textures/trick");
		}

		// Set order in layer to 255 for the trigger
		spriteRenderer.sortingOrder = 255;

		float width = float.Parse(content.Attributes.GetNamedItem("Width").Value);
		float height = float.Parse(content.Attributes.GetNamedItem("Height").Value);

		float originalWidth = spriteRenderer.sprite.texture.width;
		float originalHeight = spriteRenderer.sprite.texture.height;

		Vector3 scale = new Vector3(width / originalWidth, height / originalHeight, 1);
		lastContent.transform.localScale = scale;

		// Add the TriggerSettings component
		TriggerSettings triggerSettings = lastContent.AddComponent<TriggerSettings>();

		// Copy the content of the <Trigger> node to the TriggerSettings component
		XmlNode contentNode = content.SelectSingleNode("Content");
		triggerSettings.Content = contentNode?.InnerXml ?? string.Empty;
	}

	// Area
	void HandleAreaNode(XmlNode content) {
		Vector3 position = Engine.Math.GameUnits.DivideVector(content);

		lastContent = Instantiate(
			dummyObject = new GameObject(content.Attributes.GetNamedItem("Name").Value),
			position,
			Quaternion.identity
		);

		SpriteRenderer spriteRenderer = lastContent.AddComponent<SpriteRenderer>();
		spriteRenderer.sprite = Resources.Load<Sprite>("Textures/trick");

		// Set order in layer to 254 for the Area
		spriteRenderer.sortingOrder = 254;

		float width = float.Parse(content.Attributes.GetNamedItem("Width").Value);
		float height = float.Parse(content.Attributes.GetNamedItem("Height").Value);

		float originalWidth = spriteRenderer.sprite.texture.width;
		float originalHeight = spriteRenderer.sprite.texture.height;

		Vector3 scale = new Vector3(width / originalWidth, height / originalHeight, 1);
		lastContent.transform.localScale = scale;
	}

	// -=-=-= //
	// Meta

	private int storedOrderInLayer;
	private bool storedAutoTag;
	private bool storedDebugObjectFound;

	private Dictionary<string, float> ExtractVariables(XmlNode objectNode) {
		Dictionary<string, float> variables = new Dictionary<string, float>();
		XmlNode propertiesNode = objectNode.SelectSingleNode("Properties/Static/ContentVariable");

		if (propertiesNode != null) {
			foreach (XmlNode variableNode in propertiesNode.SelectNodes("Variable")) {
				string name = variableNode.Attributes["Name"].Value;
				float value = float.Parse(variableNode.Attributes["Default"].Value);
				variables.Add(name, value);
			}
		}

		return variables;
	}

	private void StoreExtraArguments(int orderInLayer, bool autoTag, bool debugObjectFound) {
		storedOrderInLayer = orderInLayer;
		storedAutoTag = autoTag;
		storedDebugObjectFound = debugObjectFound;
	}

	private int RetrieveStoredOrderInLayer() {
		return storedOrderInLayer;
	}

	private bool RetrieveStoredAutoTag() {
		return storedAutoTag;
	}

	private bool RetrieveStoredDebugObjectFound() {
		return storedDebugObjectFound;
	}

	static float EvaluateExpression(string expression, Dictionary<string, float> variables) {
		// Remove the curly braces if they exist
		if (expression.StartsWith("{") && expression.EndsWith("}")) {
			expression = expression.Substring(1, expression.Length - 2);
		}

		// Replace variable placeholders in the expression with actual values
		string evaluatedExpression = Regex.Replace(expression, @"~(\w+)", match => {
			string varName = match.Groups[1].Value;
			return variables.ContainsKey(varName) ? variables[varName].ToString() : "0";
		});

		// Evaluate and return as a float
		return EvaluateSimpleMath(evaluatedExpression);
	}

	static float EvaluateSimpleMath(string expression) {
		try {
			var dataTable = new System.Data.DataTable();
			var result = dataTable.Compute(expression, "");
			return Convert.ToSingle(result);
		} catch (Exception ex) {
			Debug.LogError($"Failed to evaluate expression: {expression}. Error: {ex.Message}");
			return 0;
		}
	}

	void ReplacePlaceholders(XmlNode node, Dictionary<string, float> variables) {
		foreach (XmlAttribute attribute in node.Attributes) {
			if (attribute.Value.StartsWith("~")) {
				string variableName = attribute.Value.Substring(1);

				if (variables.ContainsKey(variableName)) {
					attribute.Value = variables[variableName].ToString();
				} else {
					string errorMessage = $@"variable ""{variableName}"" not found in the variables dictionary";
					string prefix = node.Name == "Matrix" ? "matrix" : "";

					errorMessage = prefix + " " + errorMessage;
					errorMessage = char.ToUpper(errorMessage[0]) + errorMessage.Substring(1);

					Debug.LogError(errorMessage);
				}
			}
		}
	}

	XmlNode FindObjectNodeByName(string objectName, XmlDocument xmlDoc, string xmlDoc2_pth) {
		// Load xmlDoc2 once
		XmlDocument xmlDoc2 = new XmlDocument();
		xmlDoc2.Load(xmlDoc2_pth);

		// Search in both XML documents using a helper method
		XmlNode node = FindObjectNode(xmlDoc2, objectName);
		if (node == null) {
			node = FindObjectNode(xmlDoc, objectName);
		}

		return node;
	}

	XmlNode FindObjectNode(XmlDocument xmlDoc, string objectName) {
		foreach (XmlNode node in xmlDoc.DocumentElement.SelectSingleNode("/Root/Objects")) {
			if (node.Name == "Object" && node.Attributes.GetNamedItem("Name").Value == objectName) {
				return node;
			}
		}

		return null;
	}

	Sprite LoadSpriteFromResources(string spriteName) {
		string lowerCaseSpriteName = spriteName.ToLower();
		Sprite[] allSprites = Resources.LoadAll<Sprite>("Textures");

		return allSprites.FirstOrDefault(sprite => sprite.name.ToLower() == lowerCaseSpriteName);
	}

	// Calculation
	void ConvertFromMarmaladeMatrix(XmlNode matrixNode, UnityEngine.Transform transform, SpriteRenderer spriteRenderer, float xmlWidth, float xmlHeight, GameObject image, string className) {
		_DefaultPosition = transform.localPosition;

		_Transformation[0, 0] = float.Parse(matrixNode.Attributes["A"].Value);
		_Transformation[0, 1] = -float.Parse(matrixNode.Attributes["B"].Value);
		_Transformation[1, 0] = -float.Parse(matrixNode.Attributes["C"].Value);
		_Transformation[1, 1] = float.Parse(matrixNode.Attributes["D"].Value);
		_Transformation[2, 2] = 1f;
		_Transformation[3, 3] = 1f;
		_DefaultPosition.X += float.Parse(matrixNode.Attributes["Tx"].Value) / 100;
		_DefaultPosition.Y += -float.Parse(matrixNode.Attributes["Ty"].Value) / 100;

		Matrix4x4 transpose = this._Transformation.transpose;
		QRDecomposition qrDecomposition = new QRDecomposition(transpose);
		if (qrDecomposition.ContainsSkew()) {
			int orderinLayer = spriteRenderer.sortingOrder;
			DestroyImmediate(lastContent.GetComponent<SpriteRenderer>());

			_SupportUnityObject = new GameObject("Support");
			_SupportUnityObject.transform.SetParent(transform, false);
			_SupportUnityObject.tag = "Unused";

			SpriteRenderer sprite = _SupportUnityObject.AddComponent<SpriteRenderer>();
			sprite.sprite = LoadSpriteFromResources(className);
			sprite.sortingOrder = orderinLayer;

			AffineDecomposition affineDecomposition = new AffineDecomposition(this._Transformation);
			_SupportUnityObject.transform.localScale = new Vector3(
				affineDecomposition.ScaleX1 / sprite.sprite.bounds.size.x / 100,
				affineDecomposition.ScaleY1 / sprite.sprite.bounds.size.x / 100,
				1f
			);

			_SupportUnityObject.transform.Rotate(0f, 0f, affineDecomposition.Angle1);
			transform.localScale = new Vector3(
				affineDecomposition.ScaleX2,
				affineDecomposition.ScaleY2,
				1f
			);

			transform.Rotate(0f, 0f, affineDecomposition.Angle2);
			transform.localPosition = _DefaultPosition;
		} else {
			Matrix4x4 rotation = qrDecomposition.Rotation;
			Quaternion localRotation = default(Quaternion);

			int num = (rotation[0, 0] * rotation[1, 1] - rotation[0, 1] * rotation[1, 0] <= 0f) ? -1 : 1;
			localRotation = Quaternion.LookRotation(num < 0 ? -rotation.GetColumn(2) : rotation.GetColumn(2), rotation.GetColumn(1));

			transform.localRotation = localRotation;
			transform.localScale = new Vector3(
				qrDecomposition.ScaleX / spriteRenderer.sprite.bounds.size.x / 100,
				qrDecomposition.ScaleY / spriteRenderer.sprite.bounds.size.y / 100,
				1f
			);
			transform.localPosition = _DefaultPosition;
		}
	}

	protected Vector3f _DefaultPosition = new Vector3f(0f, 0f, -800f);
	protected Matrix4x4 _Transformation = Matrix4x4.identity;
	protected GameObject _SupportUnityObject;
}
