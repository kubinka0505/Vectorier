using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// -=-=-=- //

public class ItemProperties : MonoBehaviour {
	public enum ItemTypes {
		Coin,
		Bonus
	};

	[Tooltip("Item type")]
	public ItemTypes Type;

	[Tooltip("Score assigned for collecting an item. N/A if Type is Coin.")]
	public int Score = 100;

	[Tooltip("Radius (game units)")]
	public float Radius = 80f;

	[Tooltip(@"Group ID of an item.

* -1 for unique
* N/A if Type is Coin")]
	public int GroupID = 1;
}
