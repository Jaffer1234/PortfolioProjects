using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// data structure that holds information of a base or outpost. structures were planned to be folder like so that a seperate base setup could be used depending
/// on the current owner faction of that base (eg when bases are conquered by someone else and the chars and dialogues chage completely)
/// </summary>
[CreateAssetMenu(fileName = "BaseData", menuName = "SDObjects/Object/Base Data")]
public class BountyBaseData : ScriptableObject {

	public BountyScenarioEntry scenario;
	public BountyScenarioEntry defenseScenario;
	[NonReorderable]
	public BaseDataStructure[] structures;

	public BaseDataStructure GetBaseStructure(Faction faction)
	{
		for (int i = 0; i < structures.Length; i++)
		{
			if(structures[i].faction == faction)
				return structures[i];
		}
		return null;
	}


}
