using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Timers;

/// <summary>
/// Actor.
/// The Actor class is the root class for every 'living thing'
/// Actors have HP, MP, and AP (can be 0) as well as Lists for Items and Equipment
/// </summary>

public abstract class Actor: Entity {
	// Actor-class events
	public delegate void ActorEvent();
	public event ActorEvent OnLoseHP;
	public event ActorEvent OnLoseMP;
	public event ActorEvent OnLoseAP;
	public event ActorEvent OnStruck;

	// The Actor's current Title and which
	public Title currentTitle;
	bool titleAffix;

	// Actors have three name-parts that combine to the full name + title
	public string firstName, lastName, nickname;

	// An Actor's gender
	protected byte gender = 0;

	protected const float HP_REGEN_DELAY = 5000f;
	protected const float MP_REGEN_DELAY = 3000f;
	protected const float AP_REGEN_DELAY = 1000f;

	protected float HPRegenTimer = 0f;
	protected float MPRegenTimer = 0f;
	protected float APRegenTimer = 0f;

	CombatTracker combatTracker;

	public List<Item> inventory = new List<Item>();
	protected List<Equipment> equipment = new List<Equipment>();

	// The lists of buff and debuff Auras currently affecting the Actor
	[SerializeField]
	public List<AuraStack> buffs = new List<AuraStack>();
	[SerializeField]
	public List<AuraStack> debuffs = new List<AuraStack>();

	// The list of the Actor's usable spells, for Players, this is their Main Class Skills
	public List<Spell> mainSpellbook = new List<Spell>();

	/*
	[SerializeField]
	protected float[] baseDerivedStats; //result of calculations on baseRawStats; set manually via DB for actors
	[SerializeField]
	protected float[] derivedStatMods; //sum of values that affect DERIVED STATS due to equipment and auras/passives
	[SerializeField]
	protected float[] totalDerivedStats; //sum of derived(totalRawStats) + derivedStatMods for players, baseDerived + derivedStatMods for Actors
	*/

	[SerializeField]
	protected float[,] derivedStats; //multi-dimensional array to replace all derivedStat arrays

	public float currMP, currAP;

	// Lists containing immunities to status types
	//public List<Spell.Status> statusImmunities = new List<Spell.Status>();

	protected bool isCasting = false;
	protected float castCounter = 0;

	public List<SpellEffect.HardControl> hardControlEffects; //actor.checkauras method for control effects

	Timer HPRegenDelayTimer, MPRegenDelayTimer, APRegenDelayTimer;
	Timer HPRegenTickTimer, MPRegenTickTimer, APRegenTickTimer;

	protected new void Awake() {
		entityLayerMask = 1<<LayerMask.NameToLayer("Entity");
		nonEntityLayerMask = ~entityLayerMask;

		//baseDerivedStats = new float[System.Enum.GetNames(typeof(Globals.DerivedStat)).Length];
		//totalDerivedStats = derivedStatMods = baseDerivedStats;

		derivedStats = new float[System.Enum.GetNames(typeof(Globals.DerivedStat)).Length, 3]; //0 - derivedOfBase, 1 - derivedOfTotal, 2 - mods, 3 - stored sum of 1 + 2

		int categories = System.Enum.GetNames(typeof(Spell.Category)).Length;
		int schools = System.Enum.GetNames(typeof(Spell.School)).Length;

		incomingDamageMods = new float[categories, schools];
		outgoingDamageMods = new float[categories, schools];
		incomingHealingMods = new float[categories, schools];
		outgoingHealingMods = new float[categories, schools];

		SetRegenTimers();
	}

	new void OnEnable() {
		OnLoseHP += SetHPRegenTimer;
		OnLoseMP += SetMPRegenTimer;
		OnLoseAP += SetAPRegenTimer;

		rb = GetComponent<Rigidbody>();
		nma = GetComponent<NavMeshAgent>();
	}

	void OnDisable() {
		OnLoseHP -= SetHPRegenTimer;
		OnLoseMP -= SetMPRegenTimer;
		OnLoseAP -= SetAPRegenTimer;
	}

	public void Kill() {
		isAlive = false;
	}

	public void Revive(float healthPercent) {
		isAlive = true;
		SetPercentHP(healthPercent);
		//TODO: Be mindful of using these triggers here, if spells/equipment relies on it
		OnLoseHP();
		OnLoseMP();
		OnLoseAP();
	}

	public float GetBaseDerivedStat(Globals.DerivedStat stat) {
		return derivedStats[(int)stat, 0];
		//return baseDerivedStats[(int)stat];
	}

	// method to retrieve a particular Derived Stat
	public float GetTotalDerivedStat(Globals.DerivedStat stat) {
		return derivedStats[(int)stat, 0] + derivedStats[(int)stat, 1];
		//return totalDerivedStats[(int)stat];
	}

	// method to combine and return the components of an Actor's name in one string
	public virtual string FullName {
		get { return firstName + " \"" + nickname + "\" " + lastName; }
	}

	public List<Weapon> AllWeapons {
		get { return equipment.OfType<Weapon>() as List<Weapon>; }
	}

	public bool CheckInventory(Item item) {
		return inventory.Contains(item);
	}

	public float GetIncomingDamageMod(Spell.Category category, Spell.School school) {
		return incomingDamageMods[(int)category, (int)school];
	}

	public void SetGender (Globals.Gender set) {
		//gender = (byte)Mathf.Clamp (set, 0, System.Enum.GetNames(typeof(Globals.Gender)).Length);
		gender = (byte)set;
	}

	// method to set an Actor's level
	public virtual void SetLevel(int set) {
		// The Actor cannot have a 0-or-less level
		lvl = Mathf.Clamp(set, 1, 255);
		xp = lvl;
		//OnLevelChange();
	}

	// method to increment an Actor's level by 1; bypasses the Global MAX_LEVEL limit
	public virtual void LevelUp() {
		SetLevel(lvl++);
	}

	/*
	 * Calculate TOTAL DERIVED STAT should:
	 * sum derivedStatMods and baseDerivedStats
	 * AFTER calculating derivedStats from totalRawStats
	 */

	void RecalculateDerivedStatMods() {
		//TODO: A full recalculation of sources that mod DERIVED STATS -- might not be necessary and probably shouldn't be used often
		if (buffs.Count > 0) {
			//if buffs has an an AuraStack with an Aura with a ModDerivedStat SpellEffect
			//get the output and affected stat of that effect and add the result to the derivedStatMods array
		}
		if (debuffs.Count > 0) {
			//do the same for debuffs
		}
	}

	void ModDerivedStat(Globals.DerivedStat stat, float value, bool isAdditive) {
		int index = (int)stat;
		Debug.Log("ModDerivedStat(" + stat.ToString() + ", " + value + ", " + isAdditive + ")");
		//Debug.Log("From: " + derivedStatMods[index]);
		Debug.Log("From: " + derivedStats[index, 1]);
		if (isAdditive) {
			//derivedStatMods[index] += value;
			derivedStats[index, 1] += value;
		}
		else {
			//derivedStatMods[index] += baseDerivedStats[index] * (1 + value/100f);
			derivedStats[index, 1] += derivedStats[index, 0] * (1 + value/100f);
		}
		//Debug.Log("To: " + derivedStatMods[index]);
		Debug.Log("From: " + derivedStats[index, 1]);
		//totalDerivedStats[index] = baseDerivedStats[index] + derivedStatMods[index];
		//Total calculation no longer needed, values in rows 0 and 1 are summed as-needed
	}

	void CalculateTotalDerivedStats() {
		Debug.Log ("Calculating Total Derived Stats.");

		for (int i = 0; i < derivedStats.GetLength(0); i++) {
			derivedStats[i, 2] = derivedStats[i, 0] + derivedStats[i, 1];
		}

		/*
		for (int i=0; i<totalDerivedStats.Length; i++) {
			totalDerivedStats[i] = baseDerivedStats[i] + derivedStatMods[i];
		}
		*/
	}

	void SetRegenTimers() {
		HPRegenDelayTimer = new Timer(HP_REGEN_DELAY);
		HPRegenDelayTimer.Elapsed += StartHPRegen;
		//HPRegenDelayTimer.AutoReset = false;
		HPRegenDelayTimer.Enabled = true;

		HPRegenTickTimer = new Timer(250);
		HPRegenTickTimer.Elapsed += HPRegenTick;

		MPRegenDelayTimer = new Timer(MP_REGEN_DELAY);
		MPRegenDelayTimer.Elapsed += StartMPRegen;
		//MPRegenDelayTimer.AutoReset = false;
		MPRegenDelayTimer.Enabled = true;

		MPRegenTickTimer = new Timer(250);
		MPRegenTickTimer.Elapsed += MPRegenTick;

		APRegenDelayTimer = new Timer(AP_REGEN_DELAY);
		APRegenDelayTimer.Elapsed += StartAPRegen;
		//APRegenDelayTimer.AutoReset = false;
		APRegenDelayTimer.Enabled = true;

		APRegenTickTimer = new Timer(250);
		APRegenTickTimer.Elapsed += APRegenTick;

		Debug.Log(this.name + " -- Regen Timers set");
	}

	protected void SetHPRegenTimer() {
		HPRegenDelayTimer.Interval = HP_REGEN_DELAY;
		HPRegenDelayTimer.Enabled = true;
		HPRegenTickTimer.Enabled = false;
	}

	protected void SetMPRegenTimer() {
		MPRegenDelayTimer.Interval = MP_REGEN_DELAY;
		MPRegenDelayTimer.Enabled = true;
		MPRegenTickTimer.Enabled = false;
	}

	protected void SetAPRegenTimer() {
		APRegenDelayTimer.Interval = AP_REGEN_DELAY;
		APRegenDelayTimer.Enabled = true;
		APRegenTickTimer.Enabled = false;
	}
		
	void StartHPRegen (object source, ElapsedEventArgs e) {
		if (IsAlive) {
			HPRegenTickTimer.Enabled = true;
		}
	}

	void StartMPRegen (object source, ElapsedEventArgs e) {
		if (IsAlive) {
			MPRegenTickTimer.Enabled = true;
		}
	}

	void StartAPRegen (object source, ElapsedEventArgs e) {
		if (IsAlive) {
			APRegenTickTimer.Enabled = true;
		}
	}

	void HPRegenTick (object source, ElapsedEventArgs e) {
		//float maxHP = totalDerivedStats[(int)Globals.DerivedStat.MaxHP];
		//currHP = Mathf.Min(currHP + maxHP*totalDerivedStats[(int)Globals.DerivedStat.RegenHP]/4f, Mathf.RoundToInt(maxHP));
		float maxHP = derivedStats[(int)Globals.DerivedStat.MaxHP, 2];
		currHP = Mathf.Min(currHP + maxHP * derivedStats[(int)Globals.DerivedStat.RegenHP, 2] / 4f, Mathf.RoundToInt(maxHP));
		if (currHP >= maxHP) {
			currHP = maxHP;
			HPRegenTickTimer.Enabled = false;
		}
	}

	void MPRegenTick (object source, ElapsedEventArgs e) {
		//float maxMP = totalDerivedStats[(int)Globals.DerivedStat.MaxMP];
		//currMP = Mathf.Min(currMP + maxMP*totalDerivedStats[(int)Globals.DerivedStat.RegenMP]/4f, Mathf.RoundToInt(maxMP));
		float maxMP = derivedStats[(int)Globals.DerivedStat.MaxMP, 2];
		currMP = Mathf.Min(currMP + maxMP * derivedStats[(int)Globals.DerivedStat.RegenMP, 2] / 4f, Mathf.RoundToInt(maxMP));
		if (currMP >= maxMP) {
			currMP = maxMP;
			MPRegenTickTimer.Enabled = false;
		}
	}

	void APRegenTick (object source, ElapsedEventArgs e) {
		//float maxAP = totalDerivedStats[(int)Globals.DerivedStat.MaxAP];
		//currAP = Mathf.Min(currAP + maxAP*totalDerivedStats[(int)Globals.DerivedStat.RegenAP]/4f, Mathf.RoundToInt(maxAP));
		float maxAP = derivedStats[(int)Globals.DerivedStat.MaxAP, 2];
		currAP = Mathf.Min(currAP + maxAP * derivedStats[(int)Globals.DerivedStat.RegenAP, 2] / 4f, Mathf.RoundToInt(maxAP));
		if (currAP >= maxAP) {
			currAP = maxAP;
			APRegenTickTimer.Enabled = false;
		}
	}

	// method to determine if the Actor has an empty hand
	public bool HasEmptyHand() {
		if (equipment.OfType<Weapon>().Any()) { //if the Actor has any Weapons equipped
			List<Weapon> equippedWeapons = AllWeapons; //get all equipped Weapons
			if (equippedWeapons.Count > 1) { //if more than 1 weapon is equipped, false
				return false;
			}
			else if (equippedWeapons[0].Grip == 1) { //if 1H, true
				return true;
			}
			else {
				return false;
			}
		}
		// otherwise return false
		else {
			return true;
		}
	}

	// method to check if the Actor has a Weapon equipped with a given WieldType
	public bool HasEquippedWeaponWieldType (Weapon.WieldType wieldType) {
		// if the Actor has any Weapons equipped
		if (equipment.OfType<Weapon>().Any()) {
			List<Weapon> equippedWeapons = AllWeapons; // get all equipped Weapons
			// then compare the passed WieldType against the equipped Weapon WieldTypes
			foreach (Weapon equippedWeapon in equippedWeapons) {
				// return true if there is a match
				if (equippedWeapon.GetWieldType() == wieldType) {
					return true;
				}
			}
		}
		else {
			return false;
		}
		return false;
	}

	public Weapon.WieldType GetFirstWeaponWieldType() {
		Weapon.WieldType result = Weapon.WieldType.none;
		// if the Actor has any Weapons equipped
		if (equipment.OfType<Weapon>().Any()) {
			// get first equipped weapon's type
			Weapon equippedWeapon = equipment.OfType<Weapon>().First();
			result = equippedWeapon.GetWieldType();
		}
		return result;
	}

	/// <summary>
	/// Gets the highest weapon damage.
	/// </summary>
	/// <returns>The highest weapon damage.</returns>
	public float GetHighestWeaponDamage () {
		if (equipment.OfType<Weapon>().Any()) { //if the Actor has any Weapons equipped
			int max = 0;
			List<Weapon> equippedWeapons = AllWeapons; //get all equipped Weapons
			foreach(Weapon w in equippedWeapons) { //iterate through each of the Actor's equipped weapons
				max = Mathf.Max(max, w.GetHighestPower());
			}
		return max;
		}
		else {
			return 0;
		}
	}

	/// <summary>
	/// Gets the highest weapon damage value among equipped weapons within a range of Wield Types
	/// </summary>
	/// <returns>The highest weapon damage.</returns>
	/// <param name="wieldTypes">Array of Wield types.</param>
	public int GetHighestWeaponDamage (Weapon.WieldType[] wieldTypes) {
		if (equipment.OfType<Weapon>().Any()) { //if the Actor has any Weapons equipped
			int max = 0;
			List<Weapon> equippedWeapons = AllWeapons; //get all equipped Weapons
			foreach(Weapon.WieldType wt in wieldTypes) { //iterate through each of the WieldType parameters
				foreach(Weapon w in equippedWeapons) { //iterate through each of the Actor's equipped weapons
					if (w.GetWieldType() == wt) {
						max = Mathf.Max(max, w.GetHighestPower());
					}
				}
			}
			return max;
		}
		else {
			return 0;
		}
	}

	public int GetHighestWeaponDamage (Spell.Category category) {
		if (equipment.OfType<Weapon>().Any()) { //if the Actor has any Weapons equipped
			int max = 0;
			List<Weapon> equippedWeapons = AllWeapons; //get all equipped Weapons
			foreach (Weapon w in equippedWeapons) { //iterate through each of the Actor's equipped weapons
				switch (category) {
				case Spell.Category.Physical:
					max = Mathf.Max(max, w.GetPhysicalPowerRange()[0]);
					break;
				case Spell.Category.Magical:
					max = Mathf.Max(max, w.GetMagicalPowerRange()[0]);
					break;
				default:
					max = Mathf.Max(max, w.GetHighestPower());
					break;
				}
			}
			return max;
		}
		else {
			return 0;
		}
	}

	public int GetHighestWeaponDamage (Spell.Category category, Weapon.WieldType[] wieldTypes) {
		if (equipment.OfType<Weapon>().Any()) { //if the Actor has any Weapons equipped
			int max = 0;
			List<Weapon> equippedWeapons = AllWeapons; //get all equipped Weapons
			foreach(Weapon.WieldType wt in wieldTypes) { //iterate through each of the WieldType parameters
				foreach(Weapon w in equippedWeapons) { //iterate through each of the Actor's equipped weapons
					if (w.GetWieldType() == wt) {
						switch (category) {
						case Spell.Category.Physical:
							max = Mathf.Max(max, w.GetPhysicalPowerRange()[0]);
							break;
						case Spell.Category.Magical:
							max = Mathf.Max(max, w.GetMagicalPowerRange()[0]);
							break;
						default:
							max = Mathf.Max(max, w.GetHighestPower());
							break;
						}
					}
				}
			}
			return max;
		}
		else {
			return 0;
		}
	}

	public float AvgWeight {
		get {
			float total = 0f;
			float count = 0f;
			foreach (Equipment equip in equipment) {
				if (equip.Weight > 0) {
					total += equip.Weight;
					count++;
				}
			}
			if (count > 0) {
				return total/count;
			}
			else {
				Debug.Log("No equipment with weight found, return default");
				return 1;
			}
		}
	}

	public virtual float RunSpeed {
		get {
			return moveSpeed;
		}
	}

	public virtual float SprintSpeed {
		get {
			int statID = (int)Globals.DerivedStat.SprintSpeed;
			//return totalDerivedStats[statID];
			return derivedStats[statID, 2];
		}
	}

	public virtual float JumpForce {
		get {
			int statID = (int)Globals.DerivedStat.JumpForce;
			//return totalDerivedStats[statID];
			return derivedStats[statID, 2];
		}
	}

	// Functions to edit incoming/outgoing damage/healing dictionaries
	public void EditIncomingDamageMods(Spell.Category category, Spell.School school, float percent) {
		incomingDamageMods[(int)category, (int)school] += percent;
	}
	public void EditOutgoingDamageMods(Spell.Category category, Spell.School school, float percent) {
		outgoingDamageMods[(int)category, (int)school] += percent;
	}
	public void EditIncomingHealingMods(Spell.Category category, Spell.School school, float percent) {
		incomingHealingMods[(int)category, (int)school] += percent;
	}
	public void EditOutgoingHealingMods(Spell.Category category, Spell.School school, float percent) {
		outgoingHealingMods[(int)category, (int)school] += percent;
	}

	public int GetAuraStackCount (Aura aura) {

		int stackIndex = BuffIndex(aura);

		if (stackIndex >= 0) {
			//int stackIndex = BuffIndex(aura);
			return buffs[stackIndex].StackCount;
		}
		else if (DebuffIndex(aura) >= 0) {
			stackIndex = DebuffIndex(aura);
			return debuffs[stackIndex].StackCount;
		}
		else {
			return stackIndex;
		}
	}

	int BuffIndex (Aura aura) {
		return buffs.FindIndex(a => a is Aura);
	}

	int DebuffIndex (Aura aura) {
		return debuffs.FindIndex(a => a is Aura);
	}

	public void ApplyAura (Aura aura, int count) {

		if (aura.HasHelpfulEffects) {
			if (BuffIndex(aura) >= 0) {
				buffs[BuffIndex(aura)].AddStacks(aura, count);
			}
			else {
				AuraStack newStack = new AuraStack();
				newStack.AddStacks(aura, count);
				buffs.Add(newStack);
			}
		}
		else {
			if (DebuffIndex(aura) >= 0) { //if there is a stack of this debuff
				debuffs[DebuffIndex(aura)].AddStacks(aura, count); //call the stack's add function (adds stacks up to the limit)
			}
			else { //otherwise, create a new stack
				AuraStack newStack = new AuraStack();
				newStack.AddStacks(aura, count);
				debuffs.Add(newStack);
			}
		}
	}

	public void Interrupt() {
		//stops current spellcast, if successful, fires an OnInterrupt event
	}

	public void SetPercentHP(float percent) {
		percent = Mathf.Clamp(percent, 0.1f, 100.0f);
		//currHP = totalDerivedStats[(int)Globals.DerivedStat.MaxHP] * percent / 100f;
		currHP = derivedStats[(int)Globals.DerivedStat.MaxHP, 2] * percent / 100f;
	}

	// Overloaded function for taking non-type (unmitigated) damage of a set value
	public void TakeDamage(float rawValue) {
		currHP -= Mathf.RoundToInt(rawValue);
		OnLoseHP();
	}

	public void TakeDamage(Globals.DerivedStat stat, float rawValue) {
		rawValue = Mathf.RoundToInt(rawValue);

		switch(stat) {
		case Globals.DerivedStat.MaxHP:
			currHP -= rawValue;
			OnLoseHP();
			break;
		case Globals.DerivedStat.MaxMP:
			currMP -= rawValue;
			OnLoseMP();
			break;
		case Globals.DerivedStat.MaxAP:
			currAP -= rawValue;
			OnLoseAP();
			break;
		default:
			Debug.Log("Damage was not dealt to HP, MP, or AP");
			break;
		}
	}

	public void TakeDamage(Spell.Category category, List<Spell.School> schools, float rawValue) {
		int damageTaken = CalculateMitigatedDamage(category, schools, rawValue);
		currHP -= damageTaken;
		OnLoseHP();
	}

	public void TakeDamage(Globals.DerivedStat stat, Spell.Category category, List<Spell.School> schools, float rawValue) {
		int damageTaken = CalculateMitigatedDamage(category, schools, rawValue);

		switch(stat) {
		case Globals.DerivedStat.MaxHP:
			currHP -= damageTaken;
			OnLoseHP();
			break;
		case Globals.DerivedStat.MaxMP:
			currMP -= damageTaken;
			OnLoseMP();
			break;
		case Globals.DerivedStat.MaxAP:
			currAP -= damageTaken;
			OnLoseAP();
			break;
		default:
			Debug.Log("Invalid stat. Damage was not dealt to HP, MP, or AP");
			break;
		}
	}
		
	// Overloaded function for taking typed (school-based) damage
	int CalculateMitigatedDamage(Spell.Category category, List<Spell.School> schools, float rawValue) {

		float result = rawValue;

		// Check for absorption effects first, pass the rawValue since the damage is against the shield, and thus not affected by the Actor's reduction factors
		if (buffs.OfType<AbsorbDamage>().Any()) { //TODO rewrite to check AuraStack for AbsorbAuras, since buffs is List<AuraStack>
			// Get all damage-absorbing Auras and iterate through them
			// TODO: Find the effect with the highest absorption amount and simply work off of that
			foreach (Spell.School s in schools) {
					result = Mathf.Min(result, CheckAbsorbAuras(category, s, rawValue));
			}
		}

		// Next, check for overall incoming damage reduction mods
		// if the incoming Damage is of multiple Categories or Schools, take the average

		float incModSum = 0f;

		foreach (Spell.School s in schools) {
			incModSum += incomingDamageMods[(int)category, (int)s];
		}

		float incModAvg = incModSum / schools.Count;


		if (incModAvg <= -1) { // <= -1 is equal to 100% reduction
			Debug.Log(FullName + " is immune to " + schools.ToString() + " damage");
			return 0;
		}
		else {
			result *= Mathf.Max(0f, 1f + incModAvg);
		}

		// Now, reduce the initially mitigated damage by the Actor's Direct and Scaling Fortitudes
		switch (category) {
		case Spell.Category.Physical:
			//result -= totalDerivedStats[(int)Globals.DerivedStat.PhysicalReduction];
			//result *= totalDerivedStats[(int)Globals.DerivedStat.PhysicalMitigation];
			result -= derivedStats[(int)Globals.DerivedStat.PhysicalReduction, 2];
			result *= derivedStats[(int)Globals.DerivedStat.PhysicalMitigation, 2];
			break;
		case Spell.Category.Magical:
			//result -= totalDerivedStats[(int)Globals.DerivedStat.MagicalReduction];
			//result *= totalDerivedStats[(int)Globals.DerivedStat.MagicalMitigation];
			result -= derivedStats[(int)Globals.DerivedStat.MagicalReduction, 2];
			result *= derivedStats[(int)Globals.DerivedStat.MagicalMitigation, 2];
			break;
		default:
			Debug.Log (result + " damage had no Category; bypassed Reduction & Mitigation");
			break;
		}
		//TODO: possibly roll Mitigation into the incomingDamageMods array (using it as a base value)
		// Correct for negative damage amounts, round, and return
		return Mathf.Max(0, Mathf.RoundToInt(result));
	}

	float CheckAbsorbAuras(Spell.Category category, Spell.School school, float rawValue) {
		// TODO: Perhaps shift this calculation to the Aura script
		float result = rawValue;

		List<AbsorbDamage> absAuras = buffs.OfType<AbsorbDamage>().ToList(); //TODO: Re-evaluate this, since buffs is now List<AuraStack>
		for (int i=0; i<absAuras.Count; i++) {
			// Check the categories and schools the Aura absorbs
			foreach (Spell.Category testCategory in absAuras[i].affectedCategories) {
				foreach (Spell.School testSchool in absAuras[i].affectedSchools) {
					// if they match the incoming damages types exactly, reduce the damage by the amount and vice versa
					if (testCategory == category && testSchool == school) {
						result = Mathf.Clamp(result, 0, result -= absAuras[i].GetAbsorbAmount());
						absAuras[i].DepleteOutput(rawValue);
						//Debug.Log (absAuras[i].GetName()+ " matches, returning: " + result);
						return result;
					}
				}
			}
		}
		Debug.Log ("No matches, returning: " + result);
		return result;
	}

	// Function to receive healing from outside sources
	public void ReceiveHealing (Spell.Category category, List<Spell.School> schools, float rawHealing) {

		float totalHealingFactor = 0f;

		foreach (Spell.School s in schools) {
			totalHealingFactor += incomingHealingMods[(int)category, (int)s];
		}

		float healingReceived = rawHealing * (1 + totalHealingFactor);

		// Combat log checks for overhealing
		//if (currHP + healingReceived > totalDerivedStats[(int)Globals.DerivedStat.MaxHP]) {
		if (currHP + healingReceived > derivedStats[(int)Globals.DerivedStat.MaxHP, 2]) {
			//float overhealing = currHP + healingReceived - totalDerivedStats[(int)Globals.DerivedStat.MaxHP];
			float overhealing = currHP + healingReceived - derivedStats[(int)Globals.DerivedStat.MaxHP, 2];
			Debug.Log (FullName + " received " + healingReceived + " healing (excess: " + overhealing + ")");
		}
		else {
			Debug.Log (FullName + " received " + healingReceived + " healing");
		}

		// Add the received healing value to current HP, capped at max HP
		//currHP = Mathf.RoundToInt(Mathf.Min(currHP + healingReceived, totalDerivedStats[(int)Globals.DerivedStat.MaxHP]));
		currHP = Mathf.RoundToInt(Mathf.Min(currHP + healingReceived, derivedStats[(int)Globals.DerivedStat.MaxHP, 2]));
	}

	public void DepleteAura(Aura aura) {
		//used to remove auras from self (spelleffects that require X stacks of an aura for example)
	}
}      