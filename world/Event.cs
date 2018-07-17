using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class Event {
	//Request/'Quest' class? Should Events and Quests be separate?

	public enum EventType {
		Delivery,
		Bounty,
		Hunting
	};

	public enum Scope {
		personal, // given to only a specific Player
		local, // given to Players in a small area (less than sight distance, usually just those near the starting NPC/object)
		regional, // given to all Players in a region (mapwide)
		global // given to all Players when they log-in (game-wide)
	};

	public enum Type {
		escort, // bringing a specific NPC or NPCs to a certain location
		puzzle, // solving an environmental challenge to reach a location (jumping, moving objects, talking to NPCs)
		combat, // fighting/killing certain NPCs in an Ecosystem until the population is a certain amount
		delivery // bringing a certain item to a specific NPC or location
	};

	public enum Repeatable {
		not,
		hourly,
		daily,
		weekly
	};

	Scope scope;
	Type type;
	Repeatable repeatable;

	EventType[] types;

	int repeatableTime = -1; // > 0 if the quest can be repeated, in milliseconds

	//values needed to determine how rewards are distributed

	string title; //event name
	Actor owner; //source of the request... optional?

	int xp = 0; //xp rewarded for completion
	int participationTime; //in seconds, how long a participant can be inactive before losing participation
	float scaleFactor; //how much quantities scale per participant
	static List<Participant> participants = new List<Participant>(); //players that contributed to the event completion and their score (0-100)
	static Dictionary<Item, int> rewards = new Dictionary<Item, int>(); //items rewarded to participants upon completion, int is qty for 100% participation

	bool isComplete = false;
	int timeLimit = 0; // in seconds, how long the event lasts; <= 0 for infinite
	static int lingerTime = 0; //in seconds, how long the event remains active after being 'complete' before distributing rewards
	DateTime startDate, endDate, dueDate; //startDate is UtcNow when Event starts, endDate = startDate.AddSeconds(timeLimit), dueDate is a fixed expriation date that overrides endDate

	List<DeliveryRequest> deliveryList = new List<DeliveryRequest>();
	List<BountyRequest> bountyList = new List<BountyRequest>();
	List<HuntingRequest> huntingList = new List<HuntingRequest>();

	public void StartEvent () {
		startDate = DateTime.UtcNow; //set when the Event begins
		endDate = GetEndTime(); //calculate when the Event expires
	}

	void Rescale() {
		foreach (DeliveryRequest dr in deliveryList) {
			if (!dr.isComplete) { 
				dr.adjTotal = (int)Math.Round(Math.Min(1f, participants.Count) * (float)dr.baseTotal * scaleFactor);
			}
		}
		foreach (BountyRequest br in bountyList) {
			if (!br.isComplete) {
				br.adjTotal = (int)Math.Round(Math.Min(1f, participants.Count) * (float)br.baseTotal * scaleFactor);
			}
		}
		//Hunting Requests do not scale
		// -- if desired, the Herd.spawnTime value can be reduced by a similar method as above (so monsters spawn more quickly and thus more kills are needed to reach target density)
	}

	DateTime GetEndTime () {
		DateTime end;

		if (timeLimit > 0) { //if there is a valid timeLimit
			end = startDate.AddSeconds(timeLimit); //set the end equal to the startDate + timeLimit
			if (DateTime.Compare(startDate, dueDate) > 0 && TimeSpan.Compare(end - startDate, dueDate - startDate) >= 0) { // if the dueDate is after the startDate and the endDate is later than the dueDate
				end = dueDate; //set the end to the dueDate (the Event cannot run longer than the dueDate)
			}
			return end;
		}
		else {
			return DateTime.MinValue;
		}
	}

	void Update () {

		if (endDate != DateTime.MinValue) {
			Console.WriteLine((endDate - startDate).Seconds);
			float secondsRemaining = (endDate - startDate).Seconds; //-Time.deltaTime
			if (secondsRemaining < 0 && !isComplete) { //if there is no time remaining and the Event has not been completed
				//event fails/removes, whatever
			}
		}

		if (isComplete) {
			CompleteCountdown();
			//linger until lingerTime is reached, then distribute rewards
		}
	}

	static IEnumerable CompleteCountdown() {
		float countdown = 0f;
		while (countdown < lingerTime) {
			countdown += 0.1f;
			//yield return WaitForSeconds(0.1f);
		}
		DistributeAwards();

		yield return null;
	}

	//need a way to 

	static void DistributeAwards() {
		foreach (Participant p in participants) {
			//check to make sure the actor is currently in the same area/within X distance/can receive awards, if true:
			foreach (KeyValuePair<Item, int> r in rewards) {
				int rewardQty = (int)Math.Round((float)r.Value * (p.score / 100f)); //award qty of item based on participation
				//p.player.AddItem(r.Key, rewardQty);
			}
		}
	}

	void CompletionCheck() {
		foreach (DeliveryRequest dr in deliveryList) {
			if (dr.recipient.inventory.Contains(dr.product) && !dr.isComplete) { //if the recipient has at least 1 occurence of the Item and the request is not complete
				List<Slot> entries = new List<Slot>();//dr.recipient.inventory.GetEveryEntryOfItem(dr.product); //get a List of all the Item entries in the recipient's inventory
				int total = 0;
				foreach(Slot s in entries) {
					total += s.Quantity;
					if (total > dr.adjTotal) {
						dr.isComplete = true; //Complete() method that
						break;
					}
				}
			}
			else {
				//???
			}
		}
		foreach (BountyRequest br in bountyList) {
			if (br.count >= br.adjTotal && !br.isComplete) {
				br.isComplete = true;
			}
		}
		foreach (HuntingRequest hr in huntingList) {
			if (!hr.isComplete) { //Herd.density >= hr.targetDensity &&
				hr.isComplete = true;
			}
		}
		if (deliveryList.Any(i => !i.isComplete) || bountyList.Any(i => !i.isComplete)) { //if there are any incomplete requests
			isComplete = true;
		}
	}


	/*
	Event Types:
	Delivery -- Dictionary<Actor, Item, int> shoppingList; -- bring each Actor the specified Item until they have the specified total
	Bounty -- Dictionary<Actor, int> bountyList; -- kill each Actor the specified number of times (1 for special NPCs, X for others, etc)
	Hunting -- Dictionary<Herd, float> -- reduce each Herd's density to below the threshold
	
	Herds (groups of Monsters) have a density variable -- calculate Bounds of each spawned/alive Creatures, and total Bounds containing all Creatures
	Vector3 totalCreatureSize += c.bounds.size
	 */
}

class Participant {
	public Actor player;
	public float score;
	public float time;
}

class DeliveryRequest {
	public bool isComplete = false;
	public Actor recipient;
	public Item product;
	public int baseTotal, adjTotal;
}

class BountyRequest {
	public bool isComplete = false;
	public Actor target;
	public int count, baseTotal, adjTotal;
}

class HuntingRequest {
	public bool isComplete = false;
	//public Herd herd;
	public float targetDensity;
}
