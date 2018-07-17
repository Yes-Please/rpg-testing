using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// The Equipment class outlines behavior for all equipable Items
/// </summary>

[System.Serializable]
public class Equipment : Item {
	
	public static event ItemEvent OnQualityChange; // Event to fire methods when the Equipment changes in Quality

	public enum Slot { //can only have 1 of each piece equipped
		None,
		Head,
		Torso,
		Chest,
		Hands,
		Legs,
		Feet,
		Back,
		Vanity,
		Ring,
		Accessory,
		Ammunition,
		Bag
	};
	
	public Slot slot;

	[SerializeField]
	protected int weight = 0;

	public void ModQuality(int value) {
		Quality = (ItemQuality)Mathf.Clamp((int)Quality + value, 0, System.Enum.GetNames(typeof(ItemQuality)).Length);
		OnQualityChange();
		Debug.Log ("Item Quality modded to " + Quality.ToString());
	}

	public void SetQuality (ItemQuality modQuality) {
		Quality = modQuality;
		OnQualityChange();
		Debug.Log ("Item Quality set to " + Quality.ToString());
	}

	public virtual int Weight {
		get { return weight; }
	}
}
