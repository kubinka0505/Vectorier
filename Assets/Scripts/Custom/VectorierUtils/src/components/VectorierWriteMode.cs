using UnityEngine;
using System.Xml;
using System.Collections;
using System.Collections.Generic;

// -=-=-=- //

[System.Serializable]
[AddComponentMenu("Vectorier/Utils/Vectorier Write Mode")]
public class VectorierWriteMode : MonoBehaviour {
    public enum Mode {
        CommonMode,
        HunterMode,
        Any,
    }

    [SerializeField]
    [Tooltip(@"Writes object only on certain mode.

Default is ""HunterMode"".

⚠️ Does not work on unwriteable GameObjects.
⚠️ May not work in many cases, especially on nested GameObjects.")]
    public Mode Value = Mode.HunterMode;

    // method to get the string representation of the Mode value
    public string GetWriteModeValue() {
        return Value.ToString();
    }

	public void OnEnable() {}

	public void AddWriteModeProperties(
		XmlDocument xml,
		XmlElement parentElement,

		string writeModeValue
	) {
		if (
			string.IsNullOrEmpty(writeModeValue) ||
			writeModeValue.ToLower() == "any"
		) {
			return;
		}

		XmlElement propertiesElement =
			Vectorier.Core.XML.Utils.General.GetOrCreateElement(
				"Properties",
				parentElement,
				xml
			);

		XmlElement staticElement =
			Vectorier.Core.XML.Utils.General.GetOrCreateElement(
				"Static",
				propertiesElement,
				xml
			);

		XmlElement selectionElement =
			Vectorier.Core.XML.Utils.General.GetOrCreateElement(
				"Selection",
				staticElement,
				xml
			);

		selectionElement.SetAttribute(
			"Choice",
			"AITriggers"
		);

		selectionElement.SetAttribute(
			"Variant",
			writeModeValue.Replace(" ", string.Empty)
		);
	}
}