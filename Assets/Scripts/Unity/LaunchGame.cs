using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;

using Debug = UnityEngine.Debug;

// -=-=-=- //

public class LaunchGame : MonoBehaviour {
	private const string SteamRunGamePath = "steam://rungameid/248970";
	private static bool shouldLaunchAfterBuild;

	static LaunchGame() {
		BuildMap.MapBuilt += OnMapBuilt;
	}

	private static void OnMapBuilt() {
		if (shouldLaunchAfterBuild) {
			RunGameLegacy();
			shouldLaunchAfterBuild = false;
		}

		// Unsubscribe from the event after running the game
		BuildMap.MapBuilt -= OnMapBuilt;
	}

	[MenuItem("Vectorier/Launch/Run Game (Legacy) %#R")]
	private static void RunGameLegacy() {
		string gameExecutablePath;
		Debug.Log(Vectorier.Settings.Core.UseShortcutLaunch);

		if (Vectorier.Settings.Core.UseShortcutLaunch) {
			gameExecutablePath = Vectorier.Settings.Core.GameShortcutPath ?? SteamRunGamePath;
		} else {
			gameExecutablePath = Path.Combine(Vectorier.Settings.Core.GameDirectoryLegacy, "Vector.exe") ?? SteamRunGamePath;
		}

		if (!gameExecutablePath.Equals(SteamRunGamePath)) {
			if (!File.Exists(gameExecutablePath)) {
				Debug.LogError($"Game was not found at \"{gameExecutablePath}\"");
				return;
			}
		}

		if (string.IsNullOrEmpty(gameExecutablePath)) {
			Debug.LogWarning("Game System.Executable path is not set! Please set it in the Project setting.");
			return;
		}

		try {
			var gameProcess = new Process {
				StartInfo = {
					FileName = gameExecutablePath,
					WorkingDirectory = Vectorier.Settings.Core.GameDirectoryLegacy
				},
				EnableRaisingEvents = true
			};

			gameProcess.Exited += (sender, args) => {
				Debug.Log("Game exited.");
			};

			gameProcess.Start();
			gameProcess.WaitForExit();
		} catch (Win32Exception) {
			Debug.LogError($"Cannot run the game from path: \"{gameExecutablePath}\"!");
		}
	}

	// -=-=-=- //

	[MenuItem("Vectorier/Launch/Build and Run Game (Copy) (Legacy) %#&R")]
	public static void BuildAndRun() {
		// Set the flag before building
		shouldLaunchAfterBuild = true;
		BuildMap.buildForRunGame = true;
		BuildMap.Build(
			compressionAlgorithm: "copy",
			moveAssets: true
		);
	}
}