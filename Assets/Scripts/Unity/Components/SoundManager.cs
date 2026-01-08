using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

using System;
using System.Linq;
using System.Collections.Generic;

using Vectorier;

// -=-=-=- //

[AddComponentMenu("Vectorier/Sound Manager")]
public class SoundManager : MonoBehaviour {
	public enum ChannelTypes {
		MonoLeft,
		MonoRight,
		MonoCombined,
		Stereo,
		Original
	}

	public int SampleRate = 22050;
	public ChannelTypes Channel = ChannelTypes.MonoCombined;

	public bool SwapChannels = false;
	public bool InvertPolarity = false;

	public List<string> _ExcludedNames = new List<string>();
	public List<string> _SilentNames = new List<string> { "ui_click.wav" };

	public bool Rebuild = false;

	public void OnEnable() {}
}

// -=-=-=- //

#if UNITY_EDITOR
[CustomEditor(typeof(SoundManager))]
public class SoundManagerEditor : Editor {
	private readonly int[] sampleRates = {
		8000,
		11025,
		16000,
		22050,
		24000,
		32000,
		44100,
		48000
	};

	private ReorderableList excludedList;
	private ReorderableList silentList;

	private void OnEnable() {
		// find serialized property
		excludedList = new ReorderableList(
			serializedObject,
			serializedObject.FindProperty("_ExcludedNames"),
			true, true, true, true
		);

		excludedList.drawHeaderCallback = (Rect rect) => {
			EditorGUI.LabelField(
				rect,
				new GUIContent("Excluded", "Name of files that will not be written into archive.")
			);
		};

		excludedList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
			var element = excludedList.serializedProperty.GetArrayElementAtIndex(index);
			rect.y += 2;
			element.stringValue = EditorGUI.TextField(
				new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
				element.stringValue
			);
		};

		excludedList.elementHeight = EditorGUIUtility.singleLineHeight + 6;

		// -=-=-=- //

		silentList = new ReorderableList(
			serializedObject,
			serializedObject.FindProperty("_SilentNames"),
			true, true, true, true
		);

		silentList.drawHeaderCallback = (Rect rect) => {
			EditorGUI.LabelField(
				rect,
				new GUIContent("Silenced", "Name of files that will be replaced with silence.")
			);
		};

		silentList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
			var element = silentList.serializedProperty.GetArrayElementAtIndex(index);
			rect.y += 2;
			element.stringValue = EditorGUI.TextField(
				new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
				element.stringValue
			);
		};

		silentList.elementHeight = EditorGUIUtility.singleLineHeight + 6;
	}

	public override void OnInspectorGUI() {
		SoundManager sm = (SoundManager)target;

		serializedObject.Update();

		// --------------------
		// Sample rate
		// --------------------
		string[] sampleRateLabels = sampleRates.Select(r => r.ToString() + " Hz").ToArray();

		int currentRateIndex = Array.IndexOf(sampleRates, sm.SampleRate);
		if (currentRateIndex < 0) {
			// default to 22050 if stored value not found
			currentRateIndex = Array.IndexOf(sampleRates, 22050);
			if (currentRateIndex < 0) currentRateIndex = 0;
		}

		GUIContent rateLabel = new GUIContent(
			"Sample Rate",
			"Audio sample rate in Hz."
		);
		int selectedRateIndex = EditorGUILayout.Popup(rateLabel, currentRateIndex, sampleRateLabels);
		sm.SampleRate = sampleRates[selectedRateIndex];

		// --------------------
		// Channel type
		// --------------------
		var channelValues = (SoundManager.ChannelTypes[])Enum.GetValues(typeof(SoundManager.ChannelTypes));
		string[] channelLabels = channelValues.Select(c => c.ToString()).ToArray();

		int currentChannelIndex = Array.IndexOf(channelValues, sm.Channel);
		if (currentChannelIndex < 0) currentChannelIndex = 0;

		GUIContent channelLabel = new GUIContent(
			"Channel Mode",
			"Select which channel configuration to use."
		);
		int selectedChannelIndex = EditorGUILayout.Popup(channelLabel, currentChannelIndex, channelLabels);
		sm.Channel = channelValues[selectedChannelIndex];

		// -=-=-=- //

		GUILayout.Space(8);

		// --------------------
		// Swap channels
		// --------------------
		GUIContent swapChannelsLabel = new GUIContent(
			"Swap Channels",
			"Swaps left and right audio channels."
		);
		sm.SwapChannels = EditorGUILayout.Toggle(swapChannelsLabel, sm.SwapChannels);

		// --------------------
		// Invert polarity
		// --------------------
		GUIContent polarityLabel = new GUIContent(
			"Invert Polarity",
			"Inverts the audio waveform (flips polarity)."
		);
		sm.InvertPolarity = EditorGUILayout.Toggle(polarityLabel, sm.InvertPolarity);

		// -=-=-=- //

		GUILayout.Space(8);

		// -=-=-=- //
		// Lists
		excludedList.DoLayoutList();
		GUILayout.Space(10);
		silentList.DoLayoutList();

		// --------------------
		// Rebuild
		// --------------------
		GUIContent rebuildLabel = new GUIContent(
			"Rebuild",
			"If enabled, existing sounds will be re-added when recompiling.\n\nOtherwise they will just be added with following settings."
		);
		sm.Rebuild = EditorGUILayout.Toggle(rebuildLabel, sm.Rebuild);

		// --------------------
		// Warnings
		// --------------------
		if (sm.SampleRate != Vectorier.Core.Game.Audio.Sound.SampleRate) {
			GUILayout.Space(10);

			EditorGUILayout.HelpBox(SettingsHelpers.ParseHelpBoxString(
				$"Setting sample rate to something other than {Vectorier.Core.Game.Audio.Sound.SampleRate} Hz will result in files playing at a different speed."),
				MessageType.Error
			);

			EditorGUILayout.HelpBox(SettingsHelpers.ParseHelpBoxString(
				"Using trainers can modify the structure of the game's bytecode so it can handle such values."),
				MessageType.Info
			);
		}

		if (!sm.Rebuild) {
			var activeMods = new List<string>();

			if (sm.SwapChannels) {
				activeMods.Add("Channel Swap");
			}

			if (sm.InvertPolarity) {
				activeMods.Add("Polarity Inversion");
			}

			if (activeMods.Count > 0) {
				string modsList = string.Join("\n- ", activeMods);
				string message = $"Rebuild is not checked, but the following modificators are:\n\n- {modsList}";

				EditorGUILayout.HelpBox(
					SettingsHelpers.ParseHelpBoxString(message),
					MessageType.Warning
				);
			}
		}

		// -=-=-=- //

		if (GUI.changed) {
			EditorUtility.SetDirty(sm);
		}

		serializedObject.ApplyModifiedProperties();
	}
}
#endif