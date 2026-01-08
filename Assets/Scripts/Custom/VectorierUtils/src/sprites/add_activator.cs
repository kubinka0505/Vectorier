using UnityEngine;
using UnityEditor;

using Vectorier;

// -=-=-=- //

public class AddActivator : MonoBehaviour {
	[MenuItem("Vectorier/⚙ Utils/Create Activator #A", false, 9997)]
	private static void CreateActivator() {
		GameObject parent = Selection.activeGameObject;
		if (parent == null) {
			return;
		}

		Sprite triggerSprite = Resources.Load<Sprite>("Textures/trigger");
		if (triggerSprite == null) {
			Debug.LogError("Trigger sprite not found at Assets/Resources/Textures/trigger.png");
			return;
		}

		Dynamic dynamicComp = parent.GetComponent<Dynamic>();
		if (dynamicComp == null) {
			return;
		}

		// Setup
		string newName = "activator";

		Vector3 spawnOffset = new Vector3(1f, 0f, 0f);
		Vector3 spawnScale = new Vector3(100f * Vectorier.Core.Game.UnitValue, 150f * Vectorier.Core.Game.UnitValue * 2, 0f);
		int aiAllowed = -1;

		bool setupWriteMode = false;
		VectorierWriteMode.Mode mode = VectorierWriteMode.Mode.Any;

		// Find
		Transform cmChild = parent.transform.Find("activator CM");
		Transform hmChild = parent.transform.Find("activator HM");

		if (cmChild == null) {
			newName += " CM";

			aiAllowed = 0;

			setupWriteMode = true;
			mode = VectorierWriteMode.Mode.CommonMode;
		} else if (hmChild == null) {
			newName += " HM";

			spawnOffset = new Vector3(-1f, 0f, 0f);
			aiAllowed = 5;

			setupWriteMode = true;
			mode = VectorierWriteMode.Mode.HunterMode;
		}
		// else keep "activator" and normal flow

		// Create
		GameObject activator = new GameObject(newName);
		activator.transform.SetParent(parent.transform);

		spawnOffset = new Vector3(spawnOffset.x, spawnOffset.y + (spawnScale.y * 1f / (100 * Vectorier.Core.Game.UnitValue)), spawnOffset.z);
		activator.transform.position = parent.transform.position + spawnOffset;
		activator.transform.localScale = spawnScale;

		// SpriteRenderer
		SpriteRenderer sr = activator.AddComponent<SpriteRenderer>();
		sr.sprite = triggerSprite;
		sr.sortingOrder = 256;

		// DynamicTrigger
		DynamicTrigger dynTrigger = activator.AddComponent<DynamicTrigger>();
		dynTrigger.TriggerTransformName = dynamicComp.TransformationName;

		// Special cases for CM / HM
		if (setupWriteMode) {
			dynTrigger.AIAllowed = aiAllowed;

			VectorierWriteMode wm = activator.AddComponent<VectorierWriteMode>();
			wm.Value = mode;
		}

		// Undo + reselect parent
		Undo.RegisterCreatedObjectUndo(activator, "Create Activator");
		Selection.activeGameObject = activator;
	}
}