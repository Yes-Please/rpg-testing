using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// The Item class outlines the behavior of all Items that are placed in an Inventory
/// </summary>

[System.Serializable]
public class Item {

	public delegate void ItemEvent();
	public static event ItemEvent OnPickup;
	public static event ItemEvent OnEquip;
	public static event ItemEvent OnUse;

	public enum ItemQuality {
		Junk, //gray
		Basic, //white
		Magical, //green
		Production, //blue
		Professional, //teal
		Artifact, //purple
		Phantasmal, //pink
		Mythical // gold; At most 1 of each weapon type to start (created via long quest chains involving high-tier crafting and dungeon/raid loot)
	};

	public enum Tag { //tags that determine what inventory tab the item can and will go into
		Consumable,
		Crafting,
		Quest
	};

	public enum BindType {
		None, // The item can be freely traded
		Account, // The item is bound to a player's account. It can be moved between characters on the account
		Character // The item is bound to the character that picked it up
	};

	public enum BindTime {
		OnPickup, // The item binds when it is picked up or looted by a character
		OnEquip, // The item binds when it is equipped on a character
		OnUse // The item binds when it is 'used', such as books, scrolls, or recipes that teach skills
	};

	public int ID { get; protected set; }
	protected string baseName;

	public string SingularName {
		get {
			return Pluralize(false);
		}
	}

	public string PluralName { 
		get {
			return Pluralize(true);
		}
	}

	//public string Title { get; private set; } //alt name for "Name" in case that cannot be used
	public string Description { get; protected set; } // 'flavor text'
	public int ReqLvl { get; protected set; }

	public ItemQuality Quality { get; protected set; }
	public Sprite icon;

	public Spell useEffect;

	protected BindType bindType;
	protected BindTime bindTime;

	public bool isBound { get; private set; } // Whether or not the item has been bound

	int[] lblIDs = new int[1] { 0 }; // final version of labels?
	//public List<Tag> tags = new List<Tag>(); //DEPRECATED VERSION OF LABELS

	public int SingletonValue { get; protected set; }
	public byte MaxStack { get; protected set; }
	//public int stackQty, stackMax; // How many of this Item is currently stacked and limit of how many can stack into 1 inventory slot
	//DEPRECATED, quantity now handled on the inventory-slot level scripts

	//public int invWidth;
	//public int invHeight;

	public int[] LabelIDs {
		get {
			return lblIDs.Distinct().ToArray();
			//rather distinct once on object creation rather than on-call for performance reasons(?)
			//an item's label IDs should NEVER change during its lifetime
		}
	}

	void OnEnable() {
		Debug.Log (baseName + " Enabled");
		OnPickup += BoP;
		OnUse += BoU;
		OnEquip += BoE;
	}
	
	void OnDisable() {
		Debug.Log (baseName + " Disabled");
		OnPickup -= BoP;
		OnUse -= BoU;
		OnEquip -= BoE;
	}

	public Item(int _ID = -1, string _baseName = "<GENERIC {ITEM}>", int[] _lblIDs = null, int _SingletonValue = 0, int _count = 1, byte _MaxStack = byte.MaxValue) {
		ID = _ID;
		baseName = _baseName;
		lblIDs = _lblIDs.Distinct().ToArray();

		SingletonValue = Mathf.Max(0, _SingletonValue);

		_MaxStack = (byte)Mathf.Min(byte.MaxValue, _MaxStack);
		MaxStack = (byte)Mathf.Clamp(_MaxStack, 1, (int)byte.MaxValue);

		Print();
	}

	/*
	 * Move this to Inventory/Slot-level scripts... or remove Quantity section, and call this from inv/slot when needed
	public virtual string ToHex() {
		int qlty = (int)Quality;
		return ID.ToString("X") + stackQty.ToString("X") + qlty.ToString("X2") + ReqLvl.ToString("X2");
	}
	*/

	string Pluralize(bool isPlural) {
		//return the item's name, formatted for appropriate pluralization.
		//since count is external, this receives a bool to determine output (less than ideal but eh)
		string output = baseName;

		if (isPlural) {
			//find start of search term, csv uses paired {}
			int splitStart = baseName.IndexOf('{');
			//find end of search term, csv uses paired {}
			int splitEnd = baseName.IndexOf('}');
			//if both were found, search & replace
			if (splitStart >= 0 && splitEnd >= 0) {
				//tolower to match csv format, +1 to grab end char
				string search = baseName.Substring(splitStart, splitEnd-splitStart+1);
				string searchLower = search.ToLower();
				//error-handling here in case search term isn't found
				try {
					//string replace = Globals.plurals[searchLower]; //TODO plurals table/csv/list/etc
					string replace = "items";
					char[] a = replace.ToCharArray();
					a[0] = char.ToUpper(a[0]);
					replace = new string(a);

					output = output.Replace(search, replace);
				}
				catch {

				}
			}
		}
		//remove {} symbols and return
		//not in 'else' in case a matching pair wasn't found above, will fall-out to this block to format
		output = output.Replace("{", "").Replace("}","");
		return output;
	}

	void BoP() {
		if (!isBound && bindTime == BindTime.OnPickup) {
			// Add confirmation screen
			isBound = true;
		}
	}

	void BoU() {
		if (!isBound && bindTime == BindTime.OnUse) {
			// Add confirmation screen
			isBound = true;
		}
	}

	void BoE() {
		if (!isBound && bindTime == BindTime.OnUse) {
			// Add confirmation screen
			isBound = true;
		}
	}

	public virtual string ToHex() {
		int qlty = (int)Quality;
		return ID.ToString("X") + qlty.ToString("X2") + ReqLvl.ToString("X2");
	}

	public void Print() {
		Debug.Log("[" + ID + "] \t" + baseName);
		Debug.Log("\t" + SingularName);
		Debug.Log("\t" + PluralName);
		Debug.Log(" -- Labels (" + LabelIDs.Length + ")");
		foreach (int i in LabelIDs) {
			Debug.Log(" -- [" + i + "]");
		}
		Debug.Log(" -- SINGLEVAL: \t" + SingletonValue + " \t MAX: \t" + MaxStack);
	}
}
