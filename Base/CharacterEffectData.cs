using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// a collection of effect data used for sounds and particles in combat for different kinds of characters
/// </summary>
[CreateAssetMenu(fileName = "EffectData", menuName = "SDObjects/Object/Effect Data")]
public class CharacterEffectData : ScriptableObject {

	public CharacterEffectEvent[] effectEvents;

	public int GetAllEffectEntries(ref List<CharacterEffectEvent> list)
	{
		list.AddRange(effectEvents);
		return effectEvents.Length;
	}
}
