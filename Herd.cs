using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Timers;

//[RequireComponent (typeof(GameObject))]
public class Herd {

	Vector3 spawnLocation;
	public List<GameObject> roster = new List<GameObject>(); // creatures to potentially spawn
	public Dictionary<GameObject, float> rareRoster = new Dictionary<GameObject, float>(); //<Rare creature to spawn, chance to spawn (0.0 - 1.0)>
	public List<GameObject> spawns; // currently spawned creatures

	Timer spawnTimer;

	public float lvlVariance; //% as decimal of how much newly-spawned creatures can vary in level (50 avg, 0.5 variance:  50 +/- 25 = 25 - 75

	Vector3 CalculateAveragePosition() {
		Vector3 avgPos = Vector3.zero;

		if (spawns.Count > 0) {
			foreach (GameObject creature in spawns) {
				avgPos += creature.transform.position;
			}
			avgPos /= spawns.Count;
		}
		return avgPos;
	}

	float CalculateAverageLevel() {
		if (spawns.Count > 0) {
			float sum = 0f, count = 0f;
			foreach(GameObject c in spawns) { //iterate through each creature in the herd
				if (c.GetComponent<Actor>() != null) { //get their level if they have an Actor script (and they should)
					sum += c.GetComponent<Actor>().Level;
					count++;
				}
				else { //if for whatever reason the creature does not have a script, remove and delete it.
					spawns.Remove(c);
					//Destroy(c);
				}
			}
			return sum/count;
		}
		return -1f;
	}

	void ClampRareSpawnChances() {
		foreach(KeyValuePair<GameObject, float> kvp in rareRoster) {
			rareRoster[kvp.Key] = Mathf.Clamp(rareRoster[kvp.Key], 0f, 1f);
		}
	}

	public float CalculateDensity() {
		if (spawns.Count > 0) {
			Bounds b = new Bounds();
			float avgCreatureVol = 0f, herdVol = 0f;
			foreach(GameObject c in spawns) {
				if (c.GetComponent<Collider>() != null) {
					Collider coll = c.GetComponent<Collider>();
					b.Encapsulate(coll.bounds); //Encapsulate to find the bounding volume of all current spawns
					avgCreatureVol += coll.bounds.size.x * coll.bounds.size.y * coll.bounds.size.z; //Add the creature's volume to the total
				}
			}
			herdVol = b.size.x * b.size.y * b.size.z; //calculate the herd's volume by multiplying the Bounds' dimensions;
			avgCreatureVol /= (float)spawns.Count; //calculate the average creature volume (by dividing by number of spawns)
			return avgCreatureVol / herdVol;
		}
		return -1f;
	}

	public void Spawn() {
		// Default NPC spawn is a random one from the default list
		GameObject newSpawn = roster[Random.Range(0, roster.Count)];;

		// Roll for the chance to spawn a rare creature
		float rareChance = Random.Range(0f, 1f);
		Debug.Log ("rC: " + rareChance);

		// Check each rare Creature
		foreach (KeyValuePair<GameObject, float> pair in rareRoster) {
			// if the rolled chance is less than or equal to their spawn chance, AND they are not currently spawned
			if (rareChance <= Mathf.Clamp(pair.Value, 0f, 1f) && !spawns.Contains(pair.Key)) {
				// the new Creature spawned will be that rare Creature, and end the loop
				newSpawn = pair.Key;
				Debug.Log ("Rare Creature: " + pair.Key.name + " spawning!");
				break;
			}
		}

		newSpawn = GameObject.Instantiate(newSpawn);

		Vector3 origin = CalculateAveragePosition();
		float variance = Random.Range(0.9f, 1.1f);
		//TODO make sure new spawn is placed on the navmesh.
		newSpawn.transform.position =  new Vector3 (origin.x * variance, origin.y, origin.z * variance); // Add in variance to place creatures near the origin but not directly on it
		spawns.Add(newSpawn);

		NPC newSpawnScript;
		if (newSpawn.GetComponent<NPC>()) {
			newSpawnScript = newSpawn.GetComponent<NPC>();
		}
		else {
			newSpawnScript = newSpawn.AddComponent<NPC>();
		}
		float avgLvl = CalculateAverageLevel();
		newSpawnScript.SetLevel(Mathf.RoundToInt(Random.Range(avgLvl * (1 - lvlVariance), avgLvl * (1 + lvlVariance))));
	}
}
