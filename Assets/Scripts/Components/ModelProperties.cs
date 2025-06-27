using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// -=-=-=- //

[AddComponentMenu("Vectorier/Model Properties")]
public class ModelProperties : MonoBehaviour {
	[Range(0, 2)]
	public int Type = 2;
	public bool UseLifeTime = false;
	public string LifeTime = "0";
}