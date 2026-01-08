using System;
using UnityEditor;
using UnityEngine;

// -=-=-=- //

[AddComponentMenu("Vectorier/Utils/Append Repeater")]
public class AppendRepeater : MonoBehaviour {
	[Tooltip("Amount of times current object will be written.")]
	[Range(1, 25)]
	public int Multiplier = 2;

	public void OnEnable() {}
}