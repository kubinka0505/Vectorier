using UnityEngine;
using System.Collections.Generic;

// -=-=-=- //

[System.Serializable]
[AddComponentMenu("Vectorier/Utils/Modern Game Version Flags")]
public class ModernVersionFlags : MonoBehaviour {
	public enum Modes {
		CommonMode,
		HunterMode
	}

	[SerializeField]
	public Modes Mode = Modes.CommonMode;

	[Header("Show:")]
	public bool UI;
	public bool Stats;

	[Header("")]

	public bool Detectors = true;
	public bool Platforms = true;
	public bool Areas = true;
	public bool Triggers = true;

	public string ParseArgs(string levelName) {
		List<string> args = new List<string> {
			"level" + " " + levelName
		};

		if (Mode == Modes.HunterMode) {
			args.Add("huntermode");
		}

		// -=-=-=- //

		if (!UI) {
			args.Add("noui");
		}

		if (!Stats) {
			args.Add("nostats");
		}

		// -=-=-=- //

		if (Detectors) {
			args.Add("showdetectors");
		}

		if (Platforms) {
			args.Add("showplatforms");
		}

		if (Areas) {
			args.Add("showareas");
		}

		if (Triggers) {
			args.Add("showtriggers");
		}

		return "-" + string.Join(" -", args);
	}
}