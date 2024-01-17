using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// a collection of talents that can be assigned to a character
/// </summary>
[CreateAssetMenu(fileName = "TalentData", menuName = "SDObjects/Object/Talent Data")]
public class BountyTalentData : ScriptableObject {

	public List<BountyTalentStructure> talents;
}
