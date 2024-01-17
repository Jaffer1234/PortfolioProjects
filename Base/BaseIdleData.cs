using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// container asset that stores a collection of animation configs, that can be randomly choosen by a character
/// </summary>
[CreateAssetMenu(fileName = "BaseIdleData", menuName = "SDObjects/Object/Base Idle Data")]
public class BaseIdleData : ScriptableObject
{

	public BaseIdleSegment[] segments;

	private BaseIdleData[] subData;
	private BaseIdleSegment[] AllSegments
	{
		get 
		{
			//List<BaseIdleData> recursion = new List<BaseIdleData>();
			List<BaseIdleSegment> result = new List<BaseIdleSegment>();
			int index = 0;
			//AllSegmentsInternal(ref result, ref recursion, ref index);
			for(int i = 0; i < segments.Length; i++) // add all (copied) segments
			{
				result.Add(segments[i].CopyClone());
			}
			index += segments.Length;
			for(int i = 0; i < subData.Length; i++) // go through subDatas
			{
				for(int j = 0; j < subData[i].segments.Length; j++) // add all (copied) segments
				{
					result.Add(subData[i].segments[j].CopyClone());
					for(int k = 0; k < subData[i].segments[j].allowedFollowSegments.Length; k++) // correkt all index numbers
					{
						result[index+j].allowedFollowSegments[k] += index;
					}
				}
				index += subData[i].segments.Length;
			}
			return result.ToArray();
		}
	}

	private void AllSegmentsInternal(ref List<BaseIdleSegment> result, ref List<BaseIdleData> recursion, ref int index)
	{
		BaseIdleSegment tSeg;
		if(recursion.Contains(this))
		{
			return;
		}
		else
		{
			recursion.Add(this);
			for(int i = 0; i < segments.Length; i++)
			{
				tSeg = segments[i].CopyClone();
				for(int k = 0; k < tSeg.allowedFollowSegments.Length; k++)
				{
					tSeg.allowedFollowSegments[k] += index;
				}
				result.Add(tSeg);
			}
			index += segments.Length;

			for(int i = 0; i < subData.Length; i++)
			{
				subData[i].AllSegmentsInternal(ref result, ref recursion, ref index);
			}
		}
	}

	[ContextMenu("TestIt")]
	private void DebugTest()
	{
		Debug.Log(AllSegments.Length);
	}
	
}

/// <summary>
/// a single animation and the corresponding information, like sound, tool held in hand, particle effects and a selection of which segments can follow next
/// </summary>
[System.Serializable]
public class BaseIdleSegment
{
	public BountyEquipModel equipment;
	public BountyBoneType equipBone;
	public BountyEquipModel equipment2;
	public BountyBoneType equipBone2;
	[ListSelection("Resource", true)]
	public int resourceType;
	public bool resourceRequired;
	[Tooltip("Spawnt die Items für die Ani erst wenn ein spezieller Ani-Frame auslöst")]
	public bool waitForAniFrame;
	public bool hasOutroAni;
	public ParticleSystem effectParticle;
	[Tooltip("Der Particle ist  im Equip angegeben und muss hier nicht mehr definiert werden, er wird außerdem nur bei effect frames getriggert")]
	public bool isEquipParticle;
	public BountyBoneType particleBone;
	[Tooltip("spawnt den partikel bei definierten frames statt einmal beim starten des ani state")]
	public bool particleHasFrame;
	public bool attachParticle;
	public AudioClip effectSound;
	public AudioClip effectSound2;
	[Tooltip("spielt den sound erst bei einem definierten frame ab statt direkt beim starten des ani state")]
	public bool soundHasFrame;
	public bool femaleUseSound2;
	[Tooltip("wird nur benutzt wenn frauen sound2 nutzen, sound2 kann aber identisch mit sound1 sein, 1.0 ist normalWert")]
	public float femalePitchFactor;
	public bool isEquipSound;
	public float customHearDistance;

	public float durationMin;  // if 0 the next idle will be queued immediately
	public float durationMax;

	public string aniSwitch1;
	public string aniTrigger; // ignore if empty
	public string aniTrigger2; // ignore if empty
	public bool useWeaponIdle;
	public BountyAniState aniState; // ignore if empty
	[Tooltip("wenn angehakt, wird die positionskorrektur angewendet ohne auf die grund-idle zu warten")]
	public bool dontReassert;

	public bool notRandomly;
	public bool onlyOnce;
	public bool leaveNodeAfterThis;
	public int[] allowedFollowSegments; // ignore if empty

	public BaseIdleSegment CopyClone()
	{
		BaseIdleSegment result = new BaseIdleSegment();
		result.equipment = equipment;
		result.equipBone = equipBone;
		result.equipment2 = equipment2;
		result.equipBone2 = equipBone2;
		result.resourceType = resourceType;
		result.resourceRequired = resourceRequired;
		result.waitForAniFrame = waitForAniFrame;
		result.effectParticle = effectParticle;
		result.particleHasFrame = particleHasFrame;
		result.particleBone = particleBone;
		result.isEquipParticle = isEquipParticle;
		result.customHearDistance = customHearDistance;
		result.attachParticle = attachParticle;
		result.leaveNodeAfterThis = leaveNodeAfterThis;
		result.onlyOnce = onlyOnce;
		result.notRandomly = notRandomly;
		result.hasOutroAni = hasOutroAni;
		result.effectSound = effectSound;
		result.effectSound2 = effectSound2;
		result.soundHasFrame = soundHasFrame;
		result.femaleUseSound2 = femaleUseSound2;
		result.femalePitchFactor = femalePitchFactor;
		result.isEquipSound = isEquipSound;
		result.durationMin = durationMin;
		result.durationMax = durationMax;
		result.aniSwitch1 = aniSwitch1;
		result.aniTrigger = aniTrigger;
		result.aniTrigger2 = aniTrigger2;
		result.useWeaponIdle = useWeaponIdle;
		result.aniState = aniState.CopyClone();
		result.dontReassert = dontReassert;
		result.allowedFollowSegments = new int[allowedFollowSegments.Length];
		allowedFollowSegments.CopyTo(result.allowedFollowSegments, 0);
		return result;
	}

}
