using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Entity.
/// The Entity class is the root class for anything that has collision, can move, and be damaged or destroyed
/// </summary>

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
//[RequireComponent(typeof(CombatTracker))]
public class Entity : MonoBehaviour {

	protected int entityLayerMask;
	protected int nonEntityLayerMask;

	protected Rigidbody rb;
	protected NavMeshAgent nma;

	public bool isAlive;
	public bool isInCombat;
	public bool isInAFormation;

	[SerializeField]
	protected string fullName;
	public float currHP;

	// The Actor's current level
	[SerializeField]
	protected int lvl;

	public int xp = 0; // XP awarded during combat with the Entity (most Entities award 0 xp)

	// Arrays containing overall incoming/outgoing damage and healing mods
	protected float[,] incomingDamageMods;
	protected float[,] outgoingDamageMods;
	protected float[,] incomingHealingMods;
	protected float[,] outgoingHealingMods;

	// The Faction the Entity belongs to, determines its allies and enemies
	public Faction faction;
	public Dictionary<Faction, int> standings = new Dictionary<Faction, int>();

	// The maximum distance before a target can be locked and beyond which locked targets are lost (draw distance, basically)
	public int maxTargetDistance {get; set;}

	// Targeted Entities
	public Entity enemyLockTarget;
	public Entity allyLockTarget;

	public GameObject tempTarget, alignTarget;

	public float moveSpeed = 3f;

	// Separately-altered movement speed factor
	public float movementSpeedMultiplier = 1f;

	protected void Awake() {
		entityLayerMask = 1<<LayerMask.NameToLayer("Entity");
		nonEntityLayerMask = ~entityLayerMask;

		int categories = System.Enum.GetNames(typeof(Spell.Category)).Length;
		int schools = System.Enum.GetNames(typeof(Spell.School)).Length;

		incomingDamageMods = new float[categories, schools];
		outgoingDamageMods = new float[categories, schools];
		incomingHealingMods = new float[categories, schools];
		outgoingHealingMods = new float[categories, schools];
	}

	protected void OnEnable() {
		rb = GetComponent<Rigidbody>();
		nma = GetComponent<NavMeshAgent>();
		isAlive = true;
	}

	public virtual string FullName {
		get {
			return fullName;
		}
	}

	public int Level {
		get {
			return Mathf.Max(1, lvl);
		}
	}

	public bool IsAlive {
		get {
			return isAlive;
		}
	}

	public bool IsInCombat {
		get {
			return isInCombat;
		}
	}

	public bool IsInAFormation {
		get {
			return isInAFormation;
		}
		set {
			isInAFormation = value;
		}
	}

	protected void AwardXP() {

	}

	public void Launch(Vector3 point, float force) {
		nma.enabled = false;
		rb.isKinematic = false;
		//-Vector3.forward + Vector3.up
		rb.AddForce((point + Vector3.up).normalized * force, ForceMode.Impulse);
	}

	void Update() {

		Debug.DrawRay(transform.position, transform.forward * 3f, Color.red);

		if (!nma.enabled) {
			rb.isKinematic = false;
		}
		else {
			rb.isKinematic = true;
		}

		nma.speed = moveSpeed * movementSpeedMultiplier;

		if (tempTarget != null) {
			nma.destination = tempTarget.transform.position;
		}

		if (alignTarget != null && nma.remainingDistance != Mathf.Infinity && nma.remainingDistance < nma.stoppingDistance && nma.velocity.magnitude < 0.1f) {
			float step = Time.deltaTime * nma.angularSpeed / 100f;
			Vector3 facing = Vector3.RotateTowards(transform.forward, alignTarget.transform.forward, step, 0.0f);
			transform.rotation = Quaternion.LookRotation(facing);
		}
	}
}
