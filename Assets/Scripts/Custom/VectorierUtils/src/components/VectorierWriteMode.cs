using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

[System.Serializable] // Makes the class serializable
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
⚠️ Does not work in many cases, especially on nested GameObjects.")]
    public Mode Value = Mode.HunterMode;

    // Helper method to get the string representation of the Mode value
    public string GetWriteModeValue() {
        return Value.ToString();
    }

    // Helper function to create and append the <Properties>, <Static>, and <Selection> nodes
    public void AddWriteModeProperties(XmlDocument xml, XmlElement parentElement, string writeModeValue) {
        if (!string.IsNullOrEmpty(writeModeValue) && writeModeValue.ToLower() != "any") {
            // Check if <Properties> node exists
            XmlElement propertiesElement = parentElement.SelectSingleNode("Properties") as XmlElement;
            if (propertiesElement == null) {
                // If not, create the <Properties> node
                propertiesElement = xml.CreateElement("Properties");
                parentElement.AppendChild(propertiesElement);
            }

            // Check if <Static> node exists inside <Properties>
            XmlElement staticElement = propertiesElement.SelectSingleNode("Static") as XmlElement;
            if (staticElement == null) {
                // If not, create the <Static> node
                staticElement = xml.CreateElement("Static");
                propertiesElement.AppendChild(staticElement);
            }

            // Check if <Selection> node exists inside <Static>
            XmlElement selectionElement = staticElement.SelectSingleNode("Selection") as XmlElement;
            if (selectionElement == null) {
                // If not, create the <Selection> node
                selectionElement = xml.CreateElement("Selection");
                staticElement.AppendChild(selectionElement);
            }

            // Ensure the "Choice" attribute exists and set it to "AITriggers"
            if (selectionElement.GetAttribute("Choice") == string.Empty) {
                selectionElement.SetAttribute("Choice", "AITriggers");
            }

            selectionElement.SetAttribute("Variant", writeModeValue.Replace(" ", string.Empty));
        }
    }
}
