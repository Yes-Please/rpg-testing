using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Weapon : Equipment {

	public enum WieldType {
		none,
		fist,
		whip,
		dagger,
		sword,
		axe,
		mace,
		//spear,
		wand,
		tome,
		greatsword,
		battleax,
		warhammer,
		polearm,
		staff,
		smallInstrument,
		largeInstrument,
		buckler,
		greatshield,
		bow,
		crossbow,
		throwing
	};

	/*
	public enum Grip {
		oneHanded = 1,
		twoHanded = 2
	};
	*/
	
	public GameObject model;

	[SerializeField]
	protected WieldType wieldType;
	[SerializeField]
	//protected Grip grip;
	protected int grip = 1; //one-handed or two-handed

	int maxPower;
	int minPower;

	public float pAffinity;
	public float mAffinity;
	
	public int minEffectsCount;
	public int maxEffectsCount;

	public List<Item> upgrades = new List<Item>();

	public int durability;
	public int maxDurability;

	void OnEnable() {
		Debug.Log (SingularName + " Enabled");
		Equipment.OnQualityChange += CalculatePowerRange;
	}

	void OnDisable() {
		Debug.Log (SingularName + "Disabled");
		Equipment.OnQualityChange -= CalculatePowerRange;
	}

	public Weapon() {
	}

	public Weapon(string _baseName, string _Description, int _reqLvl, ItemQuality _Quality, WieldType _wieldType, float _pAffinity, float _mAffinity, int _maxDurability) {
		baseName = _baseName;
		Description = _Description;
		ReqLvl = Mathf.Max (_reqLvl, 0);
		Quality= _Quality;
		wieldType = _wieldType;

		pAffinity = _pAffinity;
		mAffinity = _mAffinity;

		maxDurability = _maxDurability;
		durability = maxDurability;

		MaxStack = 1;
		DetermineAttributes();
		CalculatePowerRange();

		DetermineEffectsCount();

		Debug.Log(SingularName + " - " + wieldType.ToString() + " - " + slot.ToString() + " - " + weight);// + " - " + invWidth + "x" + invHeight);
	}

	public override int Weight {
		get { return weight; }
	}

	public int GetHighestPower() {
		return Mathf.Max(GetPhysicalPowerRange()[0], GetMagicalPowerRange()[0]); //return whichever damage value is higher - physical or magical
	}

	public int[] GetPhysicalPowerRange() {
		return new int[2] { Mathf.RoundToInt(pAffinity * GetMaxPower()), Mathf.RoundToInt(pAffinity * GetMinPower()) };
	}

	public int[] GetMagicalPowerRange() {
		return new int[2] { Mathf.RoundToInt(mAffinity * GetMaxPower()), Mathf.RoundToInt(mAffinity * GetMinPower()) };
	}

	public int GetMaxPower() {
		CalculatePowerRange();
		return maxPower;
	}
	
	public int GetMinPower() {
		CalculatePowerRange();
		return minPower;
	}
	
	public WieldType GetWieldType() {
		return wieldType;
	}
	/*
	public Grip GetGrip() {
		return grip;
	}
	*/

	public int Grip {
		get { return grip; }
	}

	void ModDurability (int value) {

	}

	public override string ToHex() {

		string upgradesAsHex = "";

		if (upgrades.Count > 0) {
			
		}

		int qlty = (int)Quality;

		return ID.ToString("X") + qlty.ToString("X2") + ReqLvl.ToString("X2");
	}

	protected void CalculatePowerRange() { //Recalculates the power range of the weapon
		int qlty = (int)Quality;
		float tempMax = Mathf.Pow((ReqLvl + qlty + weight), ((1 + (weight+(float)grip)/30f) * (1+(float)qlty/40f)));
		float tempMin = (ReqLvl + qlty + weight) * ((1 + (weight+(float)grip)/30f) * (1+(float)qlty/40f));

		maxPower = Mathf.RoundToInt(tempMax * durability / maxDurability);
		minPower = Mathf.RoundToInt(tempMin * durability / maxDurability);
	}

	protected void DetermineEffectsCount() {
		switch(Quality) {
		case ItemQuality.Junk:
			minEffectsCount = 0;
			maxEffectsCount = 0;
			break;

		case ItemQuality.Basic:
			minEffectsCount = 0;
			maxEffectsCount = 1;
			break;

		case ItemQuality.Magical:
			minEffectsCount = 1;
			maxEffectsCount = 2;
			break;

		case ItemQuality.Production:
			minEffectsCount = 3;
			maxEffectsCount = 5;
			break;

		case ItemQuality.Professional:
			minEffectsCount = 3;
			maxEffectsCount = 5;
			break;

		case ItemQuality.Artifact:
			minEffectsCount = 5;
			maxEffectsCount = 7;
			break;

		case ItemQuality.Phantasmal:
			minEffectsCount = 7;
			maxEffectsCount = 9;
			break;
		}
		minEffectsCount *= grip;
		maxEffectsCount *= grip;
	}

	protected virtual void DetermineAttributes() {
		switch(wieldType) {

		case WieldType.fist:
			grip = 1;//Grip.oneHanded;
			//invWidth = 2;
			//invHeight = 2;
			weight = 1;
			break;
			
		case WieldType.whip:
			grip = 1;//Grip.oneHanded;
			//invWidth = 2;
			//invHeight = 2;
			weight = 1;
			break;

		case WieldType.dagger:
			grip = 1;//Grip.oneHanded;
			//invWidth = 2;
			//invHeight = 2;
			weight = 1;
			break;

		case WieldType.sword:
			grip = 1;//Grip.oneHanded;
			//invWidth = 2;
			//invHeight = 3;
			weight = 2;
			break;

		case WieldType.axe:
			grip = 1;//Grip.oneHanded;
			//invWidth = 2;
			//invHeight = 3;
			weight = 2;
			break;

		case WieldType.mace:
			grip = 1;//Grip.oneHanded;
			//invWidth = 2;
			//invHeight = 3;
			weight = 2;
			break;

		case WieldType.wand:
			grip = 1;//Grip.oneHanded;
			//invWidth = 2;
			//invHeight = 3;
			weight = 1;
			break;

		case WieldType.tome:
			grip = 1;//Grip.oneHanded;
			//invWidth = 2;
			//invHeight = 2;
			weight = 1;
			break;

		case WieldType.greatsword:
			grip = 2;//Grip.twoHanded;
			//invWidth = 2;
			//invHeight = 4;
			weight = 6;
			break;

		case WieldType.battleax:
			grip = 2;//Grip.twoHanded;
			//invWidth = 2;
			//invHeight = 4;
			weight = 6;
			break;

		case WieldType.warhammer:
			grip = 2;//Grip.twoHanded;
			//invWidth = 2;
			//invHeight = 4;
			weight = 6;
			break;

		case WieldType.polearm:
			grip = 2;//Grip.twoHanded;
			//invWidth = 2;
			//invHeight = 4;
			weight = 4;
			break;

		case WieldType.staff:
			grip = 2;//Grip.twoHanded;
			//invWidth = 2;
			//invHeight = 4;
			weight = 3;
			break;

		case WieldType.smallInstrument:
			grip = 2;//Grip.twoHanded;
			//invWidth = 1;
			//invHeight = 1;
			weight = 1;
			break;

		case WieldType.largeInstrument:
			grip = 2;//Grip.twoHanded;
			//invWidth = 2;
			//invHeight = 4;
			weight = 3;
			break;

		case WieldType.buckler:
			grip = 1;//Grip.oneHanded;
			//invWidth = 2;
			//invHeight = 2;
			weight = 1;
			break;

		case WieldType.greatshield:
			grip = 1;//Grip.oneHanded;
			//invWidth = 2;
			//invHeight = 3;
			weight = 4;
			break;

		case WieldType.bow:
			grip = 2;//Grip.twoHanded;
			//invWidth = 2;
			//invHeight = 4;
			weight = 3;
			break;

		case WieldType.crossbow:
			grip = 2;//Grip.twoHanded;
			//invWidth = 2;
			//invHeight = 3;
			weight = 2;
			break;

		case WieldType.throwing:
			grip = 1;//Grip.oneHanded;
			//invWidth = 1;
			//invHeight = 1;
			weight = 1;
			break;

		default:
			grip = 1;//Grip.oneHanded;
			//invWidth = 2;
			//invHeight = 2;
			weight = 0;
			break;
		}
	}
}
