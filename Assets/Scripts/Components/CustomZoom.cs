using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// -=-=-=- //

[AddComponentMenu("Vectorier/Custom Zoom")]
public class CustomZoom : MonoBehaviour {
    [Tooltip(@"0.65 = TriggerZoomMin
0.8  = TriggerZoom80
1.0  = TriggerZoomNormal
1.1  = TriggerZoomMax")]
	[Range(0.3f, 1.5f)]
	public float ZoomAmount;

    [Tooltip(@"
0.6  = WallJumpCamera")]
	[Range(0.3f, 2f)]
	public float Smoothness;
}
