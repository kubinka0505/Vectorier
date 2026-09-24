using System.Collections;
using System.Collections.Generic;

using UnityEngine;

// -=-=-=- //

[AddComponentMenu("Vectorier/Dynamic Trigger")]
public class DynamicTrigger : MonoBehaviour {
	public enum EventTypes {
		Enter,
		Exit,
		OnShow,
		OnHide,
		OnCollision,
		// ShowOrHide,
	}

	public enum KeyTypes {
		Up,
		Left,
		Right,
		Down,
		Any,
	}

	public enum OrderType {
		Random,
		Sync,
		Straight
	}

	public enum Directions {
		FromLeft,
		FromRight,
		Any
	}

	public void OnEnable() {}

	// -=-=-=- //
	[Tooltip("Which transformation to trigger")]
	public string TriggerTransformName = "Transform_name";

	[Tooltip(@"Which AIs are allowed to activate the trigger.

* Separatable.
* Defaults to ""0"" if null or empty.
* Can negate the value by starting it with ""!"".")]
	public string AIAllowed = "-1";

	[Tooltip(@"Which node activates the trigger.

* Defaults to ""COM"" if null or empty.")]
	public string modelNode = "COM";

	[Tooltip(@"Activates transformation with this key.

Requires aforementioned Event Type.")]
	public KeyTypes KeyActivating = KeyTypes.Any;

	// -=-=-=- //
	[Header("Advanced")]

	[Tooltip(@"Pipe-separated animation names that will activate the trigger.

Leave empty for any.
Place > before entry name to negate.")]
	public string Animations = "";

	[Tooltip(@"Determines whether trigger is activated if:

- Enter: Model node enters the trigger bounds
- Exit: Model node leaves the trigger bounds
- OnShow: When trigger enters the screen
- OnHide: When trigger leaves the screen")]
	public EventTypes EventType = EventTypes.Enter;

	[Tooltip(@"Direction of which trigger will be activated from.")]
	public Directions Direction = Directions.Any;

	// -=-=-=- //
	[Header("Sound Settings")]

	public bool PlaySound = false;

	[Tooltip(@"Files inside ""sound.dz"" archive to be played.

Separatable by attribute separator.")]
	public string Sound = "";

	[Tooltip(@"Delay in seconds after which ""Sound"" will be played.

If value is negative, it acts as a multiplier of the total duration
of all active movement intervals.

Examples:
-1   = after full duration
-0.5 = halfway through
-2   = double duration")]
	public float Latency = 0f;

	// -=-=-=- //
	[Header("Miscellaneous")]

	[Tooltip("Allows trigger to be used multiple times.")]
	public bool Reusable = false;

	[Tooltip("Use multiple transformations")]
	public bool MultipleTransformation = false;

	[Tooltip(@"Trigger execution probability in 100% divided by this value.

Example: On 3, the trigger has chances of executing once per 3 times.")]
	public int OncePer = 1;

	[Tooltip(@"List of transformation names to use, if using multiple transformation.

Names starting with > are unused.")]
	public List<string> TransformationNames = new List<string>();

	[Tooltip(@"Order of transformations.

* Random - will activate any transformation
* Sync - will activate all transformations simultaneously
* Straight - will activate all transformations iteratively")]
	public OrderType Order = OrderType.Sync;

	[Tooltip(@"Set value, This indicates how much transformations to choose from the list if order is random.

Set this to 0 for Sync order")]
	public int Set = 1;
}