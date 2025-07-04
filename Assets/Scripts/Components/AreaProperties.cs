using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// -=-=-=- //

[AddComponentMenu("Vectorier/Area Properties")]
public class AreaProperties : MonoBehaviour {
	public enum EnumAreaType {
		None,
		Help,
		Arrest
	};

	public enum EnumHelpKeyType {
		None,
		Up,
		Down,
		Left,
		Right
	};

	[Tooltip("Area type")]
	public EnumAreaType Type;

	[Tooltip(@"

⚠️ Applicable for ""Arrest"" area type only")]
	public int ArrestDistance = 300;

	[Tooltip(@"String from the ""common_xml.dz/localization_all.xml"" language table displayed while help.

⚠️ Applicable for ""Help"" area type only")]
	public string HelpDescriptionReference = "";

	[Tooltip(@"Key which if pressed finishes the area event

⚠️ Applicable for ""Help"" area type only")]
	public EnumHelpKeyType HelpKey;
}