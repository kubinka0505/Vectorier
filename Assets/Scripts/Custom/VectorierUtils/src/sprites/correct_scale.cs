using UnityEditor;
using UnityEngine;

// -=-=-=- //

public static class SpriteRendererScaleCorrector {
	[MenuItem("Vectorier/⚙ Utils/🖼️ Sprites/Scale corrector &Q")]
	private static void CorrectScale() {
		int counter = 0;
		GameObject[] selection = Selection.gameObjects;
		int amount = selection.Length;

		foreach (GameObject go in selection) {
			SpriteRenderer sr = go.GetComponent<SpriteRenderer>();

			if (sr == null || sr.sprite == null) {
				continue;
			}

			Vector2 dim = sr.sprite.rect.size;
			Transform transform = go.transform;

			Vector3 position = transform.localPosition;
			position.x = (float)System.Math.Round(position.x, 3);
			position.y = (float)System.Math.Round(position.y, 3);
			position.z = (float)System.Math.Round(position.z, 3);

			bool positionChanged = transform.localPosition != position;

			Vector3 scale = transform.localScale;
			scale.x = Mathf.Round(scale.x * dim.x) / dim.x;
			scale.y = Mathf.Round(scale.y * dim.y) / dim.y;
			scale.z = 0f;

			bool scaleChanged = transform.localScale != scale;

			if (!positionChanged && !scaleChanged) {
				continue;
			}

			Undo.RecordObject(transform, "Correct Sprite Scale");
			
			if (positionChanged) {
				transform.localPosition = position;
			}
			
			if (scaleChanged) {
				transform.localScale = scale;
			}

			// Mark the scene object as modified.
			EditorUtility.SetDirty(transform);

			// If this is a prefab instance, record the Transform changes
			// as prefab overrides.
			if (PrefabUtility.IsPartOfPrefabInstance(go)) {
				PrefabUtility.RecordPrefabInstancePropertyModifications(transform);
			}

			if (positionChanged || scaleChanged) {
				counter++;
			}
		}

		if (counter > 0) {
			Debug.Log(
				$"Corrected scales of {counter} from {amount} selected GameObjects."
			);
		}
	}
}