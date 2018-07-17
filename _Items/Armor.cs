using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Armor : Equipment {

	public enum WeightCategory {
		none,
		light,
		medium,
		heavy,
		massive
	};

	[SerializeField]
	private WeightCategory wgtCat;

	private int slotMult; //Determines total EP pool and adjusted weight value

	/* Array containing how much each stat costs in EP
	 * VIT, WIL, STA, VIG, FOC, END, STR, INT, DEX, CLA, AGI, DEF, RES
	 * private int[] SPRatios = new int[] {1,1,1,2,2,2,1,1,3,3,5,1,1};
	 * DEPRECATED, all stats scale in a 1:1 ratio with EP cost
	 */

	public GameObject model;
	
	public int totalEP, maxEP, spentEP;

	[SerializeField]
	private List<Globals.RawStat> distributableStats;
	[SerializeField]
	private float[] stats = new float[System.Enum.GetNames(typeof(Globals.RawStat)).Length];

	public int durability, maxDurability;

	void OnEnable() {
		Debug.Log (SingularName + " -- Armor Enabled");
		Equipment.OnQualityChange += ResetStats;
		Equipment.OnQualityChange += CalculateEP;
		Equipment.OnQualityChange += CalculateDamageReductions;
	}
	
	void OnDisable() {
		Debug.Log (SingularName + " -- Armor Disabled");
		Equipment.OnQualityChange -= ResetStats;
		Equipment.OnQualityChange -= CalculateEP;
		Equipment.OnQualityChange -= CalculateDamageReductions;
	}

	public void Init(string _baseName, string _Description, int _reqLvl, ItemQuality _Quality, WeightCategory _wgtCat, Slot _slot) {
		baseName = _baseName;
		Description = _Description;
		ReqLvl = Mathf.Max (_reqLvl, 0);
		Quality = _Quality;
		wgtCat = _wgtCat;
		slot = _slot;

		MaxStack = 1;
		
		Setup();
	}

	void Setup() {
		distributableStats = new List<Globals.RawStat>();

		DetermineAttributes();
		CalculateEP();

		// Assign Defense and Resistance values only if the item does not allow manual distribution of such stats
		if (!(distributableStats.Contains(Globals.RawStat.Defense) || distributableStats.Contains(Globals.RawStat.Resistance))) {
			CalculateDamageReductions();
		}

		Debug.Log(SingularName + " - " + wgtCat.ToString() + " - " + slot.ToString() + " - " + weight); //invWidth + "x" + //invHeight);
	}

	public override int Weight {
		get { return weight * slotMult; }
	}

	public WeightCategory WeightClass {
		get { return wgtCat; }
	}

	public float[] AssignedStatValues {
		get { return stats; }
	}
	
	public void ResetStats() {
		spentEP = 0;
		stats = new float[System.Enum.GetNames(typeof(Globals.RawStat)).Length];
	}

	public void AssignStat(Globals.RawStat stat, int value) {
		int statIndex = (int)stat;
		// if the stat to be added can be added to the item
		if (distributableStats.Contains(stat)) {
			// determine how much EP will have been spent after the change
			int convertedValue = spentEP + value;
			// if the value is above 0 or below the item's base EP
			if (convertedValue < totalEP && convertedValue > 0) {
				//Add the stat value to the item's stat array, and it's converted cost towards the EP total
				Debug.Log (SingularName + "'s " + stat.ToString() + " value was increased from " + stats[statIndex] + " to " + (stats[statIndex] + value));
				stats[statIndex] += value;
				spentEP +=  value;
				Debug.Log (SingularName + " has " + (totalEP - spentEP) + " SP remaining");
			}
			else {
				Debug.Log (convertedValue + " exceeds available SP or is negative.");
			}
		}
		else {
			Debug.Log ("This Item cannot have " + (Globals.RawStat)statIndex + " added to it");
		}
	}

	void DetermineAttributes() {
		distributableStats.Clear();
		switch(slot) {
		case Slot.Head:
			//invWidth = 2;
			//invHeight = 2;
			slotMult = 1;
			distributableStats.Add(Globals.RawStat.Vigor);
			distributableStats.Add(Globals.RawStat.Focus);
			distributableStats.Add(Globals.RawStat.Endurance);
			distributableStats.Add(Globals.RawStat.Intellect);
			distributableStats.Add(Globals.RawStat.Clarity);
			break;

		case Slot.Torso:
			//invWidth = 2;
			//invHeight = 3;
			slotMult = 3;
			break;

		case Slot.Chest:
			//invWidth = 2;
			//invHeight = 3;
			slotMult = 2;
			distributableStats.Add(Globals.RawStat.Vitality);
			distributableStats.Add(Globals.RawStat.Willpower);
			distributableStats.Add(Globals.RawStat.Stamina);
			distributableStats.Add(Globals.RawStat.Strength);
			distributableStats.Add(Globals.RawStat.Intellect);
			break;

		case Slot.Hands:
			//invWidth = 2;
			//invHeight = 2;
			slotMult = 1;
			distributableStats.Add(Globals.RawStat.Vigor);
			distributableStats.Add(Globals.RawStat.Focus);
			distributableStats.Add(Globals.RawStat.Endurance);
			distributableStats.Add(Globals.RawStat.Strength);
			distributableStats.Add(Globals.RawStat.Dexterity);
			break;

		case Slot.Legs:
			//invWidth = 2;
			//invHeight = 3;
			slotMult = 2;
			distributableStats.Add(Globals.RawStat.Vitality);
			distributableStats.Add(Globals.RawStat.Willpower);
			distributableStats.Add(Globals.RawStat.Stamina);
			distributableStats.Add(Globals.RawStat.Agility);
			break;

		case Slot.Feet:
			//invWidth = 2;
			//invHeight = 2;
			slotMult = 1;
			distributableStats.Add(Globals.RawStat.Vigor);
			distributableStats.Add(Globals.RawStat.Focus);
			distributableStats.Add(Globals.RawStat.Endurance);
			distributableStats.Add(Globals.RawStat.Agility);
			break;

		case Slot.Back:
			//invWidth = 2;
			//invHeight = 1;
			slotMult = 0;
			distributableStats.Add(Globals.RawStat.Defense);
			distributableStats.Add(Globals.RawStat.Resistance);
			break;

		default:
			//invWidth = 1;
			//invHeight = 1;
			slotMult = 0;
			break;
		}
	}

	void CalculateEP() {
		// if the armor piece does not have a Material Type
		if (wgtCat == WeightCategory.none) {
			// its EP pool is 0
			totalEP = 0;
			maxEP = 0;
		}
		else {
			// otherwise, its EP pool is calculated based on its other attributes
			float levelRatio = (float)ReqLvl/100f; //decoupled from using Globals.MAX_LEVEL in case level cap changes
			float qualityRatio = (float)Quality/System.Enum.GetValues(typeof(ItemQuality)).Length;
			float statWeighting = 1 + slotMult/3f;
			
			//Debug.Log("LEVEL_RATIO: " + levelRatio + " || QUALITY_RATIO: " + qualityRatio + " || STAT_WEIGHTING: " + statWeighting);

			totalEP = Mathf.RoundToInt(100 * (levelRatio + qualityRatio) * statWeighting);
			maxEP = Mathf.RoundToInt(100 * (levelRatio + 1) * statWeighting);
		}
		Mathf.Clamp(spentEP, 0, totalEP);
	}

	void CalculateDamageReductions() {

		// if the armor piece does not have a Material Type
		if (wgtCat == WeightCategory.none) {
			// it gets no DEF and RES values
			stats[(int)Globals.RawStat.Defense] = 0;
			stats[(int)Globals.RawStat.Resistance] = 0;
		}
		else {
			// otherwise, it gains DEF at higher weights and RES at lower weights
			float baseValue =  (float)ReqLvl/2f * (1f + (slotMult + (float)Quality) / 10f);
			stats[(int)Globals.RawStat.Defense] = Mathf.RoundToInt(baseValue * (int)wgtCat);
			stats[(int)Globals.RawStat.Resistance] = Mathf.RoundToInt(baseValue * (System.Enum.GetNames(typeof(WeightCategory)).Length - (int)wgtCat));
		}
	}
}
