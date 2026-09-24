using UnityEngine;

// -=-=-=- //

[System.Serializable]
[AddComponentMenu("Vectorier/Utils/Game Write Mode")]
public class GameWriteMode : MonoBehaviour {
    public enum Mode {
        Legacy,
        Modern,
        Any,
    }

	public void OnEnable() {}

    [SerializeField]
    [Tooltip(@"Writes object only on certain game version.

Default is ""Any"".

⚠️ Does not work on unwriteable GameObjects.
⚠️ May not work in many cases, especially on nested GameObjects.")]
    public Mode Value = Mode.Any;
}