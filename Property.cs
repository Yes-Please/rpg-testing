using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Timers;

public class Property {
	
	public string Name { get; private set; } // The name of the Property
	public int BaseValue { get; private set; } // The base/intitial value of the Property
	Actor owner; // The current owner (name on the deed) of the Property -- switch to Account later?
	Bounds boundary; // Collider instead?
	public int GlobalDecoLimit { get; private set; } //global limit on the number of objects that can be placed on the Property
	public int LocalDecoLimit { get; private set; } //limit on how many objects can be placed within a certain proximity of each other
	public int LocalDecoRange { get; private set; } //range within which the localLimit is checked
	public List<Decoration> decorations = new List<Decoration>(); // All of the decorations currently placed within the boundaries of the Property

	public Property (string _Name = "<#N/A>", int _BaseValue = 0, int _GlobalDecoLimit = 100, int _LocalDecoLimit = 10, int _LocalDecoRange = 1) {
		Name = _Name;
		BaseValue = _BaseValue;
		GlobalDecoLimit = _GlobalDecoLimit;
		LocalDecoLimit = _LocalDecoLimit;
		LocalDecoRange = _LocalDecoRange;
	}

	public bool CanAddDecorations {
		get {
			return decorations.Count < GlobalDecoLimit;
		}
	}

	public bool CanAddDecoration (Vector3 location) {
		//checks to see if there is too many 'cluttered' Decorations around the given location
		int localDecoCount = 0;
		Collider[] colls =  Physics.OverlapSphere(location, LocalDecoLimit);
		foreach (Collider c in colls) {
			if (c.gameObject.GetComponent<Decoration>()) {
				localDecoCount++;
				if (localDecoCount > LocalDecoLimit) {
					return false;
				}
			}
		}
		return true;
	}

	public void AddObject(Decoration d) {
		decorations.Add(d);
	}

	public int DecorationCount {
		get { return decorations.Count; }
	}

	public void Evaluate() {
		int sumActualValue = 0;
		int sumMaxValue = 0;
		foreach (Decoration d in decorations) {
			sumActualValue += d.ActualValue;
			sumMaxValue += d.MaxValue;
		}

		int avgActualValue = (int)Mathf.RoundToInt(sumActualValue / (float)decorations.Count);
		int avgMaxValue = Mathf.RoundToInt(sumMaxValue / (float)decorations.Count);
		float sumActualValuePercent = (float)Mathf.Round(sumActualValue / (float)sumMaxValue * 100f);
		//float avgActualValuePercent = (float)Math.Round(avgActualValue / (float)avgMaxValue * 100f, 2);
		int actualPropertyValue = Mathf.RoundToInt((float)BaseValue * sumActualValuePercent / 100f + avgActualValue);

		Debug.Log(
			"|PROPERTY: " + Name +
			"\n|Value: " + BaseValue +
			"\n|D: Sum of Actual Values: " + sumActualValue +
			"\n|D: Sum of Max Values: " + sumMaxValue +
			"\n|D: Average of Actual Values: " + avgActualValue +
			"\n|D: Average of Max Values: " + avgMaxValue +
			"\n|D: sumCurrent/sumMax as %: " + sumActualValuePercent +
			"\n|=========" +
			"\n|Actual Property Value = " + BaseValue + " * " + sumActualValuePercent + "% + " + avgActualValue + " = " + actualPropertyValue
		);
	}
}