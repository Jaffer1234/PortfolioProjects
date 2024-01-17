using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// passive skill class that defines properties of passive skill. these are not used in combat but apply different passive effects to a char in questions of base/working, crafting abilities, event skill check outcome boni
/// </summary>
[CreateAssetMenu(fileName = "PassiveSkill", menuName = "SDObjects/Object/PassiveSkill")]
public class BountyPassiveSkill : ScriptableObject
{

	public string skillId;
	public Sprite icon;
	public int maxLvl = 6;

	[NonReorderable]
	public PassiveSkillLevel[] levels;

}

[System.Serializable]
public class PassiveSkillModule
{
	public PassiveSkillEffect effectType;
	[Header("Item Values")]
	public BaseItem.ItemType2 itemType;
	public int itemLevel;
	[Header("Talent Test")]
	public BountyTalentType talent;
	public int lockTier;
	[Space]
	public int effectValue;
	
}
[System.Serializable]
public class PassiveSkillLevel
{
	[NonReorderable]
	public SkillLevelRequirement requirements;
	public PassiveSkillModule[] modules;
}

public enum PassiveSkillEffect
{
	None = 0,
	IncreaseItemEffect = 1,
	IncreaseCraftingYield = 2,
	IncreaseCraftingLevel = 3,
	IncreaseTalentEventChance = 4,
	IncreaseLockPickChance = 5,
}
