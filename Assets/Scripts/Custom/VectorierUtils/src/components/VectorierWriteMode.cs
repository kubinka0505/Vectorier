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

⚠️ Does not work on objects having ""Untagged"" tag
⚠️ May not work in many cases, especially on nested GameObjects.")]
    public Mode Value = Mode.HunterMode;

    // method to get the string representation of the Mode value
    public string GetWriteModeValue() {
        return Value.ToString();
    }

	public void OnEnable() {}

    // create and append the <Properties>, <Static>, and <Selection> nodes
	public void AddWriteModeProperties(XmlDocument xml, XmlElement parentElement, string writeModeValue) {
		if (string.IsNullOrEmpty(writeModeValue) || writeModeValue.ToLower() == "any") {
			return;
		}

		// find or create <Properties> without relying on parent being attached
		XmlElement propertiesElement = null;
		foreach (XmlNode child in parentElement.ChildNodes) {
			if (child is XmlElement el && el.Name == "Properties") {
				propertiesElement = el;
				break;
			}
		}

		if (propertiesElement == null) {
			propertiesElement = xml.CreateElement("Properties");
			parentElement.AppendChild(propertiesElement);
		}

		// find or create <Static>
		XmlElement staticElement = null;
		foreach (XmlNode child in propertiesElement.ChildNodes) {
			if (child is XmlElement el && el.Name == "Static") {
				staticElement = el;
				break;
			}
		}

		if (staticElement == null) {
			staticElement = xml.CreateElement("Static");
			propertiesElement.AppendChild(staticElement);
		}

		// find or create <Selection>
		XmlElement selectionElement = null;
		foreach (XmlNode child in staticElement.ChildNodes) {
			if (child is XmlElement el && el.Name == "Selection") {
				selectionElement = el;
				break;
			}
		}

		if (selectionElement == null) {
			selectionElement = xml.CreateElement("Selection");
			staticElement.AppendChild(selectionElement);
		}

		// Set attributes
		selectionElement.SetAttribute("Choice", "AITriggers");
		selectionElement.SetAttribute("Variant", writeModeValue.Replace(" ", string.Empty));
	}
}