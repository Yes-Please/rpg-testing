using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//likely a useless endeavor ... consider deleting this class

/*
	 public enum RawStat {
		Vitality, // 0
		Willpower, // 1
		Stamina, // 2
		Vigor, // 3
		Focus,  // 4
		Endurance, // 5
		Strength, // 6
		Intellect, // 7
		Dexterity, // 8
		Clarity, // 9
		Agility, // 10
		Defense, // 11
		Resistance // 12
	};
	
	public enum DerivedStat {
		MaxHP, // 0
		MaxMP, // 1
		MaxAP, // 2
		RegenHP, // 3
		RegenMP, // 4
		RegenAP, // 5
		PhysicalPower, // 6
		MagicalPower, // 7
		PhysicalAccuracy, // 8
		MagicalAccuracy, // 9
		Haste, // 10
		PhysicalReduction, // 11
		PhysicalMitigation, // 12
		MagicalReduction, // 13
		MagicalMitigation, // 14
		CriticalBonus, // 15
		CriticalThreshold, // 16
		CriticalAngle, // 17
		WalkSpeed, // 18
		RunSpeed, // 19
		SprintSpeed, // 20
		JumpForce // 21
	};
	
	public enum OtherStat {
		WeaponDamage, // 0
		AverageWeight, // 1
		Level, // 2
		IncomingDamage, // 3
		IncomingHealing, // 4
		OutgoingDamage, // 5
		OutgoingHealing // 6
	};
*/

public abstract class Stat {

	public enum StatType {
		Resource,
		Regen,
		Attack,
		Accuracy,
		CastSpeed,
		MoveSpeed,
		Critical,
		Defense,
		Other
	}

	protected StatType type;
	protected float baseValue;
	public virtual float BaseValue { 
		get {
			return baseValue;
		}
		protected set {
			baseValue = value;
		}
	}

	protected float modValue = 0f; //net value of all +/- mods (both additive/multiplicative)
	public abstract float ModValue { get; protected set; }

	public float Value {
		get {
			return BaseValue + modValue;
		}
	}

	public Stat(StatType _type = StatType.Other, float _baseValue = 0f) {
		type = _type;
		BaseValue = Mathf.Min(0f, _baseValue);
		modValue = 0f;
	}

	/*
    Have to be a bit careful, multiplicative mods on a baseValue of 0 will have no effect
    (even if the Value ends up being >0 due to other mods)
    So basically:
    bV = 0; mV = 0;
    ModValue(5, true);
    mV += 5, V = 5 (0 + 5)
    ModValue (2, false);
    mV = 5 + 0 * 2 >> 5 (unchanged)
    */

	public virtual void Modify(float value, bool isAdditive) {
		if (isAdditive) {
			modValue += value;
		}
		else {
			modValue += (BaseValue * value);
		}
	}

	public void Print() {
		Debug.Log("B: " + BaseValue);
		Debug.Log("m: " + modValue);
		Debug.Log("V: " + Value);
	}
}

public class RawStat : Stat {

	public override float ModValue {
		get {
			return modValue;
		}
		protected set {
			ModValue = value;
		}
	}

	public override void Modify(float value, bool isAdditive) {
		if (isAdditive) {
			ModValue += value;
		}
		else {
			ModValue += (BaseValue * value);
		}
	}
}

public class DerivedStat : Stat {

	float[] inputs; //factors used to calculate the result of this stat
	List<float> mods = new List<float>(); //List of values that modify the resulting output; List needed since the Base can change (due to mods on the Raw stat)

	public override float ModValue {
		get {
			float sum = 0f;
			foreach (float f in mods) {
				sum += f;
			}
			return sum;
		}
		protected set {
			ModValue = value;
		}
	}

	public DerivedStat(StatType _type = StatType.Other, float _baseValue = 0f, float[] _inputs = null) : base(_type, _baseValue) {
		inputs = _inputs;
	}

	// RawStat.Base + RawStat.Mod = RawStat.Value
	// DerivedStat.Base = DerivedStat.Derive(inputs)
	// DerivedStat.Base + DerivedStat.Mod = DerivedStat.Value

	public override void Modify(float value, bool isAdditive) {
		if (isAdditive) {
			mods.Add(BaseValue + value);
		}
		else {
			mods.Add(BaseValue * value);
		}
	}

	public override float BaseValue {
		get {
			//if there are inputs (raw stat values) provided to process
			if (inputs == null) {
				//process them
				float val1 = inputs[0];
				float lvl;
				float MIN;
				float MAX;
				float wgt;
				switch (type) {
				case StatType.Resource:
					//Max.HP
					//Max.MP
					//Max.AP

					//0 -- Value
					//1 -- level
					lvl = inputs[1];

					return val1 * 10f * lvl / 100f;
				case StatType.Regen:
					//HP.Regen
					//MP.Regen
					//AP.Regen

					//0 -- Value
					//1 -- level
					//2 -- MIN
					//3 -- MAX
					lvl = inputs[1];
					float cap = 100f + (float)Mathf.Pow(Mathf.Sqrt(inputs[1]), 3);
					MIN = inputs[2];
					MAX = inputs[3];

					return MIN + (MAX - MIN) * val1 / cap;
				case StatType.Attack:
					//P.Power -- STR * (1 + lvl / 100)
					//P.Power -- INT * (1 + lvl / 100)

					//0 -- Value
					//1 -- level

					lvl = inputs[1];

					return val1 * (1 + lvl/100f);
				case StatType.Accuracy:
					//P.Finesse -- (DEX / lvl) ^ (1 + SQRT(DEX / lvl))
					//M.Finesse -- (CLA / lvl) ^ (1 + SQRT(DEX / lvl))

					//0 -- Value
					//1 -- level
					//2 -- MIN
					//3 -- MAX

					lvl = inputs[1];
					MIN = inputs[2];
					MAX = inputs[3];

					return (float)Mathf.Pow(val1 / lvl, 1 + Mathf.Sqrt(val1 / lvl));
				case StatType.CastSpeed:
					//Haste -- AGI * 0.375/lvl/wgt

					//0 -- Value
					//1 -- level
					//2 -- weight
					//3 -- MIN
					//4 -- MAX

					return 0f;
				case StatType.MoveSpeed:
					//Speed.Run -- MIN ^ (1.1 + AGI * 0.14 / lvl) - SQRT(wgt)
					//Speed.Sprint -- MIN ^ (1.0 + AGI * 0.14 / lvl) - SQRT(wgt)
					//Speed.Jump -- MIN ^ (1.3 + AGI * 0.14 / lvl) - SQRT(wgt)

					//0 -- Value
					//1 -- level
					//2 -- weight
					//3 -- MIN
					//4 -- MAX

					return 0f;
				case StatType.Critical:
					//Crit.Bonus -- MIN + (MAX - MIN) * (DEX + CLA) * 1f / lvl
					//Crit.Threshold -- MIN + (MAX - MIN) * (DEX + CLA) * 1f / lvl
					//Crit.Angle -- MIN + (MAX - MIN) (AGI + 0f) * 0.625f / lvl

					//0 -- Value1
					//1 -- Value2
					//2 -- coeff
					//3 -- level
					//4 -- MIN
					//5 -- MAX
					return inputs[4] + (inputs[5] - inputs[4]) * (inputs[0] + inputs[1]) * inputs[2] / inputs[3];
				case StatType.Defense:
					//
					return 0f;
				default:
					return 0f;
				}
			}
			else {
				//otherwise just return the backing field
				return baseValue;
			}
		}
		protected set {
			//setting the Base only modifies the backing field (used for NPCs, while Players have their Derived Stat BaseValues calcualted)
			baseValue = value;
		}
	}
}