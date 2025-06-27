using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// -=-=-=- //

[AddComponentMenu("Vectorier/Dynamic Trigger")]
public class DynamicTrigger : MonoBehaviour {
	// -=-=-=- //
	[Tooltip("Which transformation to trigger")]
	public string TriggerTransformName = "Transform_name";

	[Tooltip("Which AI is allowed to trigger")]
	public int AIAllowed = -1;

	[Tooltip(@"Which node activates the trigger. Default is ""COM"".")]
	public string modelNode = "COM";

	[Tooltip(@"Pipe-separated animation names that will activate the trigger. Leave empty for any.")]
	public string Animations = "";

	[Tooltip(@"Determines whether trigger is activated if:

- Enter: Model node enters the trigger bounds
- Exit: Model node leaves the trigger bounds
- OnShow: When trigger enters the screen
- OnHide: When trigger leaves the screen")]
	public EventTypes EventType = EventTypes.Enter;

	// -=-=-=- //
	[Header("Sound settings")]
	public bool PlaySound = false;

	[Tooltip(@"File inside ""sound.dz"" archive to be played.")]
	public string Sound = "";

	[Tooltip(@"Delay in seconds after which ""Sound"" will be played.")]
	public float Latency = 0f;

	// -=-=-=- //
	[Header("Miscellaneous")]
	[Tooltip("Allows trigger to be used multiple times.")]
	public bool Reusable = false;

	[Tooltip("Use multiple transformations")]
	public bool MultipleTransformation = false;

	[Tooltip("List of transformation names to use if using multiple transformation")]
	public List<string> TransformationNames = new List<string>();

	[Tooltip("Order of transformations, Random will choose a random transformation from the list, Sync will play at the same time")]
	public OrderType Order = OrderType.Sync;

	[Tooltip("Set value, This indicates how much transformations to choose from the list if order is random. Set this to 0 for Sync order")]
	public int Set = 1;

	// -=-=-=- //

	public enum EventTypes {
		Enter,
		Exit,
		OnShow,
		OnHide,
	}

	public enum OrderType {
		Random,
		Sync,
		Straight
	}
}