using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FullSerializer;
using Unity.Entities.UniversalDelegates;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using System.Text;
using System.IO;
#endif

/// <summary>
/// -data object that represents a character in the game. player, NPCs and enemies. holds information about it's stats, talents, buffs, and other states
/// </summary>
[CreateAssetMenu(fileName = "Character", menuName = "SDObjects/Object/Character")]
[fsObject]
public class BountyCharacter : ScriptableObject//, ICombatant
{

	public static readonly int[] xpTable = new int[] { 150, 220, 350, 500, 1000, 1750, 2500, 3500, 5000, 7500, 10000, 13500, 15000, 18000, 22000 };
	public static readonly int levelCap = 15;
	public static readonly int injuredHealthPercent = 25;
	public static readonly Dictionary<BountyCharAttribute, int> attributeImprovementCaps = new Dictionary<BountyCharAttribute, int>()
	{
		{ BountyCharAttribute.Armor, 20 },
		{ BountyCharAttribute.Resistance, 20 },
		{ BountyCharAttribute.BlockChance, 3 },
		{ BountyCharAttribute.CritChance, 3 },
	};
	public static readonly Dictionary<CharacterClass, BaseItem.ItemType2> classItemTable = new Dictionary<CharacterClass, BaseItem.ItemType2>()
	{
		{ CharacterClass.Frontkaempfer, BaseItem.ItemType2.GearWeaponShotgun },
		{ CharacterClass.Jaeger, BaseItem.ItemType2.GearWeaponRifle },
		{ CharacterClass.Krieger, BaseItem.ItemType2.GearWeaponPunch },
		{ CharacterClass.MartialArtist, BaseItem.ItemType2.None },
		{ CharacterClass.Pistolero, BaseItem.ItemType2.GearWeaponPistol },
		{ CharacterClass.Schuetze, BaseItem.ItemType2.GearWeaponRifle },
		{ CharacterClass.Techniker, BaseItem.ItemType2.GearWeaponPistol },
		{ CharacterClass.Todeskommando, BaseItem.ItemType2.GearWeaponSmg },
		{ CharacterClass.Vollstrecker, BaseItem.ItemType2.GearWeaponBlade },
		{ CharacterClass.Waechter, BaseItem.ItemType2.GearWeaponAxe },
	};
	public static int GetAttributeMaxImprovement(BountyCharAttribute attribute)
	{
		if (attributeImprovementCaps.ContainsKey(attribute))
		{
			return attributeImprovementCaps[attribute];
		}
		return 999;
	}

	[Header("Allgemeine Info")]
	[fsProperty]
	public string characterId;
	//public int intId;
	[SerializeField]
	[fsIgnore]
	public BountyModel modelPrefab;
	[fsProperty, SerializeField]
	[HideInInspector]
	public string customName;
	[fsProperty]
	public BountyPortrait portraitData;
	// [fsIgnore]
	// public Sprite portrait;
	[Tooltip("Wird für bestimmte Charakter-Standart-Einstellungen benutzt oder animationen")]
	[fsIgnore]
	public BountyRace race;// SerializedObject.FindProperty throws an exception when it's looking for this
	[fsIgnore]
	public Faction faction;
	[fsIgnore]
	public bool female;
	[fsProperty]
	public bool darkSkin;
	[fsIgnore]
	public BountyPersistentStat killStat;
	//[fsIgnore]
	//[Tooltip("Entscheidet über die grafische darstellung der HP-Leiste im Kampf")]
	//public DefenseStyle defenseStyle;
	[fsIgnore]
	public BountyTalentData talentData;
	[fsIgnore]
	[Tooltip("Props sind objekte die auf dem feld stehen, wie fallen, türme und barrieren")]
	public bool isProp;
	[fsIgnore]
	[Tooltip("kann deckung geben")]
	public bool coverProp;
	[fsProperty]
	[Tooltip("Ein Charakter, der nicht für interaktionen zur verfügung steht, wie zB Trish")]
	public bool backgroundCharacter;
	[fsIgnore]
	[Tooltip("Ein Charakter, der irgendwann als partymitglied zur verfügung steht (für hp-ausdauer-berechenung)")]
	public bool partyCharacter;
	[fsIgnore]
	[Tooltip("Ein Charakter, der der story relevant ist und bei tot nur KO geht um dann in der base wieder zu regenerieren")]
	public bool storyCharacter;
	[fsIgnore]
	[Tooltip("Ein Charakter, der auch in da 1 vorkam (für achievements)")]
	public bool daCharacter;
	[fsIgnore]
	[Tooltip("Ein Charakter, der als ersatz spawnen kann, wenn ein story char stirbt")]
	public bool replacementCharacter; // added 21.12.2020

	// instance/runtime variables
	[Header("Variablen")]
	[fsIgnore]
	private BountyModel modelInstance;
	[SerializeField]
	[Tooltip("Kann als Startwert benutzt werden")]
	[fsProperty]
	[HideInInspector]
	public int level;
	[fsProperty]
	[HideInInspector]
	public int prevLevel;
	[SerializeField]
	[Tooltip("Für gegner ist es der Xp drop")]
	[fsProperty]
	[HideInInspector]
	public int exp;
	[fsProperty]
	[HideInInspector]
	public int prevExp;
	[fsProperty]
	[HideInInspector]
	public int stamina = 1;
	[fsIgnore]
	public int defaultStamina = 1;
	[SerializeField]
	[fsIgnore]
	private int baseHealth = 100;
	/// <summary>
	/// use the Health property for controlled HP manipulation
	/// </summary>
	[fsProperty]
	[HideInInspector]
	public int healthPoints;
	[fsProperty]
	[HideInInspector]
	public CampRoomType job = CampRoomType.None;
	[fsProperty]
	[HideInInspector]
	public CampRoomType lastJob = CampRoomType.None;
	[SerializeField]
	[fsProperty]
	public int talentPoints;
	private int tempTalentPoints;
	[SerializeField]
	[fsProperty]
	[NonReorderable]
	//[ContextMenuItem("Copy", "CopyTalents"), ContextMenuItem("Paste", "PasteTalents")]
	public List<CharTalentEntry> talents;
	private List<CharTalentEntry> tempTalents;
	[fsProperty]
	public List<BountyPerk> perks = new List<BountyPerk>();
	[SerializeField]
	[fsProperty]
	public int attributePoints;
	private int tempAttributePoints;
	[SerializeField]
	[fsProperty]
	[NonReorderable]
	//[ContextMenuItem("Copy", "CopyAttributes"), ContextMenuItem("Paste", "PasteAttributes")]
	public List<CharAttributeEntry> attributes;
	[fsProperty]
	[HideInInspector]
	public List<CharAttributeEntry> improvedAttributes; // player improved attributes
	private List<CharAttributeEntry> tempImprovedAttributes;
	[fsProperty]
	[HideInInspector]
	public List<CharacterEquipmentSlot> equipment;
	[SerializeField]
	[fsIgnore]
	[NonReorderable]
	public BaseItemDefinition[] startEquipment;
	[fsProperty]
	[HideInInspector]
	public bool isSetup;
	[fsProperty]
	[HideInInspector]
	public bool isDead;
	[fsIgnore]
	public bool mainCharacter;
	[fsProperty]
	public BaseNavNode.NodeType startNodeType;
	[fsProperty]
	public BaseNavNode.StationType startNodeStation;
	[fsProperty]
	public int startNavMode = 1;
	[fsProperty]
	public int startNodeIndex = -1;
	[fsIgnore]
	public bool goToWork; // when true baseNavAgent will ignore stations that arent built yet
	[fsIgnore]
	public bool skipAniFast; // when true the first animation transition when a day starts will be fast skipped
	[fsProperty]
	[HideInInspector]
	public List<CharacterStateEntry> baseStates;
	[fsIgnore, SerializeField]
	private List<CharacterStateEntry> debugStates;
	[fsProperty]
	[HideInInspector]
	public List<CharacterBaseState> newStates;
	[fsIgnore, SerializeField]
	public BaseIdleData specialSegments;
	[fsIgnore]
	private int specialSegment = -1;
	[fsProperty]
	[HideInInspector]
	public DamageSource deathReason;
	[fsProperty]
	[HideInInspector]
	public Faction factionOverride = Faction.None;
	

	[Header("Kampf Eigenschaften")]
	[fsIgnore]
	public bool elite; // elite version des gegners, entscheidet sich erst wenn er gespawnt wird, jedoch geringe chance
	[fsIgnore]
	public bool boss; // shows elite/boss particle but without attrib changes and gives more xp?
	[fsProperty]
	public bool controllable;
	[fsProperty]
	public bool allied;
	[fsProperty]
	public CombatSide side;
	[fsProperty]
	public bool invincible; // wont die 
	[fsIgnore]
	public bool notHealable; // cant be selected for healing
	[fsProperty]
	public bool justKOForDeath;
	[fsProperty]
	public bool temporary;
	[fsIgnore]
	public BountyBuff[] instantBuffs;
	[fsIgnore]
	public CharacterEffectData[] effectData;
	[fsIgnore]
	[HideInInspector]
	public bool simulatedChar; // used for the AI's turn calculations
	[fsIgnore]
	[SerializeField]
	private BountySkill freezeSummonerSkill;

	// kampf vars
	[fsProperty]
	[SerializeField]
	public int ammoType;
	[fsIgnore]
	//[SerializeField]
	private int resistPoints;
	[fsIgnore]
	//[SerializeField]
	private int armorPoints;
	[fsIgnore]
	//[SerializeField]
	private int heavyArmorPoints;
	[fsIgnore]
	private List<BountyBuff> activeBuffs;
	[fsProperty]
	[HideInInspector]
	public int startRow;
	[fsProperty]
	[HideInInspector]
	public int startSlot;
	[fsIgnore]
	private int row;
	[fsIgnore]
	private int slot;
	[fsIgnore]
	private int targetRow;
	[fsIgnore]
	private int targetSlot;
	[fsIgnore]
	private BountySkill lastSkill;
	[fsIgnore]
	private BountySkill chargedSkill;
	[fsIgnore]
	private BountyCombatAI.AiMove lastMove;
	[fsProperty]
	[SerializeField]
	[HideInInspector]
	public BountyTalentType lastTalent = BountyTalentType.None;
	private BountyTalentType displayTalent = BountyTalentType.None;
	[fsIgnore]
	private BountySkill currentSkill;
	[fsIgnore]
	private int skillRepeats;
	[fsIgnore]
	private BountyDamage currentIncomingKnockback; // stored until the knockback animation is at the impact frame so we can du the damage effects
	[fsIgnore]
	private List<BountyProjectile> activeProjectiles;
	[fsIgnore]
	private List<BountyCharacter> currentTargets = new List<BountyCharacter>();
	//[fsIgnore]
	//private List<BountyCharacter> targetRecord = new List<BountyCharacter>();
	[fsIgnore]
	public BountyAiRole aiRole;
	[fsIgnore]
	public bool aiPreferFarRanged;
	[fsIgnore]
	private int deathEffectIndex;
	[fsIgnore]
	public int DeathEffectIndex
	{
		set { deathEffectIndex = value; }
	}
	[fsIgnore]
	private int adrenaline;
	[fsIgnore]
	private bool adrenalineChanged;
	[fsIgnore]
	private int charge;
	[fsIgnore]
	private Dictionary<BountySkill, float> skillCooldowns;
	[fsProperty]
	[HideInInspector]
	public List<TempAttributeBuff> tempCombatAttribBuffs;
	[fsProperty]
	[HideInInspector]
	public List<string> tempCombatBuffs;
	[fsIgnore]
	private BountyDamage currentAttackResult;
	[fsProperty]
	[HideInInspector]
	public bool wasKO; //got knocked out in last combat
	[fsIgnore]
	private int medicUsed;
	[fsIgnore]
	private bool hasHeavyArmor;
	[fsIgnore]
	private int combatLogicState; // combat logic: -1 = dead, 0 = disabled, 1 = ready, 2 = moving, 3 = doing skill ani, 4 = doing hit ani, 5 = being knocked back
	[fsIgnore]
	private List<BountyCharacter> activeMinions;
	[fsIgnore]
	private BountyCharacter summoner;
	[fsIgnore]
	private bool hasMovedThisTurn;
	[fsIgnore]
	private BountyCombatant combatantData;

	// base state vars
	[fsIgnore]
	private int idleNodeIndex; // used to decide baseState ani chance
	[fsIgnore]
	private Dictionary<CharacterBaseState, List<int>> baseStateTracker = new Dictionary<CharacterBaseState, List<int>>(); // used to track baseState ani occurance
	[fsIgnore]
	private List<CharacterBaseState> notShownStates = new List<CharacterBaseState>();
	[fsIgnore]
	private static readonly List<BountyCharAttribute> difficultyAffectedAttribs = new List<BountyCharAttribute>()
	{
		BountyCharAttribute.Strength,
		BountyCharAttribute.Perception,
		BountyCharAttribute.Intelligence,
	};

	// properties
	[fsIgnore]
	public bool IsPassiveProp
	{
		get { return isProp && aiRole == BountyAiRole.Passive; }
	}
	[fsIgnore]
	public int AmmoType
	{
		get { return ammoType; }
		set { ammoType = value; }
	}
	[fsIgnore]
	public CampRoomType Job
	{
		get { return job; }
		set { job = value; }
	}
	[fsIgnore]
	public CampRoomType LastJob
	{
		get { return lastJob; }
		set { lastJob = value; }
	}
	[fsIgnore]
	public List<CharacterStateEntry> BaseStates
	{
		get
		{
			List<CharacterStateEntry> result = new List<CharacterStateEntry>();
			result.AddRange(baseStates);
			if (result.Exists(n => n.state == CharacterBaseState.Moody))
				result.RemoveAll(n => n.state == CharacterBaseState.Happy);
			return baseStates;
		}
		//set { baseStates = value; }
	}
	[fsIgnore]
	public DamageSource DeathReason
	{
		get { return deathReason; }
	}
	[fsIgnore]
	public List<CharacterBaseState> NewStates
	{
		get
		{
			if (newStates == null)
				newStates = new List<CharacterBaseState>();
			return newStates;
		}
		//set { newStates = value; }
	}
	[fsIgnore]
	public bool HappyStateShown
	{
		get;
		set;
	}
	[fsIgnore]
	public bool MoodyStateShown
	{
		get;
		set;
	}
	[fsIgnore]
	public bool InfectedStateShown
	{
		get;
		set;
	}
	[fsIgnore]
	public bool InjuredStateShown
	{
		get;
		set;
	}
	[fsIgnore]
	public int SpecialSegment
	{
		get { return specialSegment; }
		set { specialSegment = value; }
	}

	// flags
	[fsProperty]
	[HideInInspector]
	public int recentLevelUp;
	[fsProperty]
	[HideInInspector]
	public bool recentDeath;
	[fsProperty]
	[HideInInspector]
	public bool recentSpawn; // not used yet
	[fsProperty]
	[HideInInspector]
	public bool recentRecovery; // wurde der character von moody, infected befreit?
	[fsProperty]
	[HideInInspector]
	public int recentIdleSessions; // hat der character schon seit x tagen keinen job?
	[fsProperty]
	[HideInInspector]
	public bool isQuestGiver; // only true on the day this character spawned with or gives a new quest and is waiting at the bar
	[fsIgnore]
	public bool IsQuestGiver
	{
		get { return isQuestGiver; }
		set { isQuestGiver = value; }
	}
	[fsIgnore]
	public int RecentIdleSessions
	{
		get { return recentIdleSessions; }
		set { recentIdleSessions = value; }
	}
	[fsIgnore]
	public bool RecentDeath
	{
		get { return recentDeath; }
		set { recentDeath = value; }
	}
	[fsIgnore]
	public bool RecentRecovery
	{
		get { return recentRecovery; }
		set { recentRecovery = value; }
	}
	[fsIgnore]
	public int BaseHealth
	{
		get { return baseHealth; }
		set { baseHealth = value; }
	}

	[fsIgnore]
	public string CharName
	{
		set
		{
			customName = value;
		}
		get
		{
			if(proceduralCharacter)
			{
				return Localization.Get(customName);
			}
			else if (!string.IsNullOrEmpty(customName))
				return customName;

			return Localization.Get("Character_" + characterId);
		}
	}
	[fsIgnore]
	public FormatTextToken CharNameToken
	{
		get
		{
			if (!string.IsNullOrEmpty(customName))
				return new FormatTextToken(customName, proceduralCharacter);

			return new FormatTextToken("Character_" + characterId, true);
		}
	}
	[fsIgnore]
	public Faction Faction
	{
		set
		{
			factionOverride = value;
		}
		get
		{
			if (factionOverride != Faction.None)
				return factionOverride;
			else
				return faction;
		}
	}

	/// <summary>
	/// called once wehn the character is created. not when loaded!
	/// </summary>
	/// <param name="param">special initation options: 1 = elite, 2 = summoned</param>
	public void Setup(int param = 0, CharacterCreationInfo cci = null)
	{
		activeBuffs = new List<BountyBuff>();
		equipment = new List<CharacterEquipmentSlot>();
		ammoType = 1;
		adrenaline = 0;
		charge = 0;
		stamina = defaultStamina;
		baseStates = new List<CharacterStateEntry>();
		newStates = new List<CharacterBaseState>();
		tempTalents = new List<CharTalentEntry>();
		tempSkills = new List<CharSkillEntry>();
		tempImprovedAttributes = new List<CharAttributeEntry>();
		deathEffectIndex = 0;
		skillCooldowns = new Dictionary<BountySkill, float>();
		activeProjectiles = new List<BountyProjectile>();
		//talents = new List<CharTalentEntry>();
		tempCombatAttribBuffs = new List<TempAttributeBuff>();
		tempCombatBuffs = new List<string>();
		improvedAttributes = new List<CharAttributeEntry>();
		activeMinions = new List<BountyCharacter>();
		moraleState = new CharacterMoralState();
		moraleState.SetNewMealRequest();
		morale = 50;

		// save creation info
		charCreationInfo = cci;

		// save the heavy armor state
		hasHeavyArmor = attributes.Exists(n => n.attribute == BountyCharAttribute.Armor && n.value >= 30);

		// start equipment
		for (int i = 0; i < startEquipment.Length; i++)
		{
			if (startEquipment[i].IsType(BaseItem.ItemType2.Gear))
				AddEquipmentItem(startEquipment[i].GenerateItem(0, BountyManager.instance.CurrentTutorialIndex < 0 && partyCharacter));
		}
		// start skills
		for (int i = 0; i < startSkills.Count; i++)
		{
			if(startSkills[i].skill)
			{
				ChangeSkillLevel(startSkills[i].skill.skillId, startSkills[i].value, true, false);
			}
			else if(startSkills[i].skillPassive)
			{
				ChangeSkillLevel(startSkills[i].skillPassive.skillId, startSkills[i].value, true, false);
			}
		}
		LastTalent = BountyTalentType.None;
		// add base attributes if they are not existant by inspector configuration
		for (int i = 10; i <= 16; i++)
		{
			if (!attributes.Exists(n => (int)n.attribute == i))
			{
				attributes.Add(new CharAttributeEntry((BountyCharAttribute)i, 0));
			}
			if (!improvedAttributes.Exists(n => (int)n.attribute == i))
			{
				improvedAttributes.Add(new CharAttributeEntry((BountyCharAttribute)i, 0));
			}
		}


		// alle bekommen base attribut boni in höhe des levels(-1) sofern sie level 2 oder höher sind
		if (param != 2)
		{
			int tValue = 0;
			if (level > 1)
			{
				for (int i = attributes.Count - 1; i >= 0; i--)
				{
					tValue = 0;
					if ((int)attributes[i].attribute >= 10 && (int)attributes[i].attribute <= 14)
						tValue = (level - 1); // base attributes +1 per level
					else if ((int)attributes[i].attribute >= 15 && (int)attributes[i].attribute <= 16)
						tValue = (level - 1) / 2; // defense +0.5 per level
					else if ((int)attributes[i].attribute >= 20 && (int)attributes[i].attribute <= 21)
						tValue = (level - 1) / 4; // crit and block + 0.25 per level

					attributes[i].value += tValue;

				}
				if (partyCharacter)
				{
					for (int i = improvedAttributes.Count - 1; i >= 0; i--)
					{
						tValue = 0;
						if ((int)improvedAttributes[i].attribute >= 10 && (int)improvedAttributes[i].attribute <= 14)
							tValue = (level - 1); // base attributes +1 per level
						else if ((int)improvedAttributes[i].attribute >= 15 && (int)improvedAttributes[i].attribute <= 16)
							tValue = (level - 1) / 2; // defense +0.5 per level
						else if ((int)improvedAttributes[i].attribute >= 20 && (int)improvedAttributes[i].attribute <= 21)
							tValue = (level - 1) / 4; // crit and block +0.25 per level

						improvedAttributes[i].value += tValue;

					}

				}

			}
		}
		else
		{
			// summoned chars get level 1 bonus right away
			for (int i = attributes.Count - 1; i >= 0; i--)
			{
				if ((int)attributes[i].attribute >= 10 && (int)attributes[i].attribute <= 14)
					attributes[i].value += (level); // base attributes +1 per level
				else if ((int)attributes[i].attribute >= 15 && (int)attributes[i].attribute <= 16)
					attributes[i].value += (level) / 2; // defense +0.5 per level
				else if ((int)attributes[i].attribute >= 20 && (int)attributes[i].attribute <= 21)
					attributes[i].value += (level) / 4; // crit and block +0.25 per level
			}
		}

		// boss status boni
		if (boss)
		{
			elite = true;

		}
		// elite boni
		else if (!allied && param == 1 && !boss)
		{
			elite = true;
			for (int i = attributes.Count - 1; i >= 0; i--)
			{
				if ((int)attributes[i].attribute >= 10 && (int)attributes[i].attribute <= 14)
					attributes[i].value = Mathf.RoundToInt(1.5f * (float)attributes[i].value);
			}
			baseHealth = Mathf.RoundToInt(1.5f * (float)baseHealth);


		}

		// translate base hp to endurance
		if (!partyCharacter)
		{
			attributes.Find(n => n.attribute == BountyCharAttribute.Endurance).value += baseHealth / 5;
			//baseHealth = 0;
		}

		Health = GetMaxHealth();


		if (!boss && !allied && param == 1)
		{
			// elite buff
			BountyBuff.CastBuff(SDResources.Load<BountyBuff>("Buffs/Buff_EliteInfo"), this, this, false);
		}

		// debug start states
		debugStates.ForEach(n => AddState(n.state, n.duration));
		if (allied && controllable && !backgroundCharacter)
		{
			if (BountyManager.instance.camp.debugStart)
			{
				for (int i = talents.Count - 1; i >= 0; i--)
				{
					talents[i].value = 10;
				}
			}
		}

		uniqueId = BountyManager.instance.camp.RegisterCharacter(this);

		prevLevel = level;
		isSetup = true;
	}

	public void OnLoaded(int fileVersion)
	{
		tempTalents = new List<CharTalentEntry>();
		tempSkills = new List<CharSkillEntry>();
		tempImprovedAttributes = new List<CharAttributeEntry>();
		activeBuffs = new List<BountyBuff>();
		skillCooldowns = new Dictionary<BountySkill, float>();
		activeProjectiles = new List<BountyProjectile>();
		activeMinions = new List<BountyCharacter>();
		if (tempCombatBuffs == null)
			tempCombatBuffs = new List<string>();
		if (tempCombatAttribBuffs == null)
			tempCombatAttribBuffs = new List<TempAttributeBuff>();
		for (int i = 0; i < baseStates.Count; i++)
		{
			if (!baseStateTracker.ContainsKey(baseStates[i].state))
				baseStateTracker.Add(baseStates[i].state, new List<int>());
			if (baseStates[i].state != CharacterBaseState.Happy && baseStates[i].state != CharacterBaseState.Moody && baseStates[i].state != CharacterBaseState.Infected)
				notShownStates.Add(baseStates[i].state);
		}

	}
	// creates a shallow copy of the character with all combat relevant information carried over //only used in editor testing
	public BountyCharacter CopyClone()
	{
		BountyCharacter result = Instantiate<BountyCharacter>(this);
		result.name = name + " (CodeCopyClone)";

		result.equipment = new List<CharacterEquipmentSlot>();
		if (equipment != null)
			equipment.ForEach(n => result.equipment.Add(new CharacterEquipmentSlot(n.type, n.item, n.blocked)));
		if (activeBuffs != null)
			result.activeBuffs = new List<BountyBuff>(activeBuffs);
		result.isSetup = true;
		result.row = row;
		result.slot = slot;
		result.healthPoints = healthPoints;
		result.armorPoints = armorPoints;
		result.resistPoints = resistPoints;
		result.heavyArmorPoints = heavyArmorPoints;
		result.ammoType = ammoType;
		result.lastSkill = lastSkill;
		result.lastMove = lastMove;
		result.lastTalent = lastTalent;
		result.level = level;
		result.stamina = stamina;
		result.charge = charge;
		result.adrenaline = adrenaline;
		result.attributes = new List<CharAttributeEntry>();
		attributes.ForEach(n => result.attributes.Add(new CharAttributeEntry(n.attribute, n.value)));
		result.improvedAttributes = new List<CharAttributeEntry>();
		improvedAttributes.ForEach(n => result.improvedAttributes.Add(new CharAttributeEntry(n.attribute, n.value)));
		//result.skillCooldowns
		return result;
	}
	/// <summary>
	/// generates all the items the character brings with him that are not equipped on him like ammo or so
	/// </summary>
	/// <returns></returns>
	public List<BaseItem> GenerateStartInvItems()
	{
		List<BaseItem> result = new List<BaseItem>();
		for (int i = 0; i < startEquipment.Length; i++)
		{
			if (!startEquipment[i].IsType(BaseItem.ItemType2.Gear))
			{
				result.Add(startEquipment[i].GenerateItem(0, /*BountyManager.instance.CurrentTutorialIndex < 0 &&*/ partyCharacter)); // changed to rebuild attribs because there are wrong values in game 1.7.20 // removed tut check 25.8.20
			}
		}
		return result;
	}
	/// <summary>
	/// generates inherant start buffs and stored temp buffs
	/// </summary>
	public void SetupStartBuffs()
	{
		// start insta buffs
		for (int i = 0; i < instantBuffs.Length; i++)
		{
			if(instantBuffs[i] != null)
				BountyBuff.CastBuff(instantBuffs[i], this, this, false, true); // oncast is postponed to StartCombat function 23.4.20
		}
		// start temp buffs
		int c = tempCombatBuffs.Count;
		for (int i = 0; i < c; i++)
		{
			BountyBuff.CastBuff(SDResources.Load<BountyBuff>("Buffs/" + tempCombatBuffs[i]), this, this, false); // oncast is postponed to StartCombat function 23.4.20
		}
		tempCombatBuffs.Clear();

		// start temp attrib buff
		ApplyTempAttribBuff(false);

	}

	public void FireStartBuffsCastEffect()
	{
		BountyBuff[] buffs = GetActiveBuffs();
		BountyDamage dmg = new BountyDamage();
		for (int i = 0; i < buffs.Length; i++)
		{
			buffs[i].Evaluate(BuffModule.BuffModuleTrigger.OnCast, ref dmg, null);
		}
	}

	private void OnDestroy()
	{
		List<BaseItem> eq = GetCompleteEqupment();
		for (int i = 0; i < eq.Count; i++)
		{
			if (eq[i])
			{
				if (Application.isPlaying)
					Destroy(eq[i]);
				else
					DestroyImmediate(eq[i]);
			}
		}
		if (activeBuffs != null)
		{
			for (int i = 0; i < activeBuffs.Count; i++)
			{
				if (activeBuffs[i])
					Destroy(activeBuffs[i]);
			}
		}
		DestroyModel();
		if(BountyManager.instance && BountyManager.instance.camp)
		{
			BountyManager.instance.camp.UnregisterCharacter(this);
		}
	}

	#region model related
	[fsIgnore]
	public BountyModel Model
	{
		get { return modelInstance; }
	}
	public BountyModel SpawnModel(Transform parent = null, bool respawn = true)
	{
		if(proceduralCharacter && proceduralModel >= 0)
		{
			modelPrefab = BountyManager.instance.modelManager.GetProceduralModel(proceduralModel);
		}

		//Debug.Log("Spawning model: "+modelPrefab.name);
		deathEffectIndex = 0;
		if (modelInstance == null)
		{
			if (respawn)
			{
				if (modelPrefab == null)
					Debug.LogErrorFormat("The Model of {0} doesnt exist",name);
                modelInstance = Instantiate<BountyModel>(modelPrefab);
			}
			else
				modelInstance = modelPrefab;

			modelInstance.myCharacter = this;
			modelInstance.SetupIKs(allied);

		}
		else
		{
			modelInstance.gameObject.SetActive(true);
		}
		if (parent != null)
		{
			modelInstance.transform.SetParent(parent, true);
		}
		modelInstance.SetupPathfinding();

		if ((elite || boss) && BountyManager.instance.IsFightActive())
			Model.AddParticleState("Elite", SDResources.Load<GameObject>("Particle/Elite"));


		// used in realtime combat system as automatik attack frame callback
		//Model.onHitFrame += AttackFrame;

		return modelInstance;
	}
	public void DestroyModel()
	{
		if (modelInstance != null)
		{
			Destroy(modelInstance.gameObject);
			modelInstance = null;
		}
	}
	public void DisposeModel()
	{
		modelInstance.DisposeBody(2f, DestroyModel);
	}

	#endregion

	#region equipment related

	public List<BaseItem> GetCompleteEqupment()
	{
		List<BaseItem> result = new List<BaseItem>();
		if (equipment != null)
		{
			for (int i = 0; i < equipment.Count; i++)
			{
				if (equipment[i].item != null)
				{
					result.Add(equipment[i].item);
				}
			}
		}

		return result;
	}
	public void RemoveCompleteEqupment(bool destroyObjects = true)
	{
		//List<BaseItem> result = new List<BaseItem>();
		if (equipment != null)
		{
			for (int i = 0; i < equipment.Count; i++)
			{
				if (equipment[i].item != null)
				{
					if (destroyObjects)
					{
						//BountyManager.instance.camp.UnregistierItem(equipment[i].item);
						Destroy(equipment[i].item);

					}
					equipment[i].item = null;
				}
			}
		}

		//return result;
	}
	/// <summary>
	/// Gives the item in the given equipslot if there is one
	/// </summary>
	/// <param name="type">the gear type to look for</param>
	/// <returns>the item or null of there is no</returns>
	public BaseItem GetEquippedItem(BaseItem.ItemType2 type)
	{
		for (int i = 0; i < equipment.Count; i++)
		{
			if (equipment[i].type == BaseItem.GetSubType(type))
			{
				return equipment[i].item;
			}
		}
		return null;
	}
	/// <summary>
	/// Removes an item from character equipment
	/// </summary>
	/// <param name="type">The gear type (slot) to remove</param>
	/// <returns>the item if an item was removed or null if no item matched</returns>
	public BaseItem RemoveEquippedItem(BaseItem.ItemType2 type)
	{
		for (int i = 0; i < equipment.Count; i++)
		{
			if (equipment[i].item != null && equipment[i].type == BaseItem.GetSubType(type))
			{
				BaseItem temp = equipment[i].item;
				equipment[i].item = null;
				BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.ItemUnequiped, uniqueId, temp.GetCompoundStringDefinition() });
				if (temp.GetSubType() != BaseItem.ItemType2.GearWeaponMelee && temp.GetSubType() != BaseItem.ItemType2.GearWeaponRanged && Model)
					Model.UpdateClothes();
				if (temp.IsType(BaseItem.ItemType2.GearRandomWeapon) && Model && BountyManager.instance.IsFightActive())
				{
					Model.UpdateWeaponModel(BountyTalentType.None);
					Model.UpdateWeaponIdle(BountyTalentType.Talent_Melee);
				}
				if (CombatGui.GetTalentFromWeapon(temp.itemType) == LastTalent)
					LastTalent = BountyTalentType.None;
				return temp;
			}
		}
		return null;
	}
	/// <summary>
	/// Removes an item from character equipment
	/// </summary>
	/// <param name="item">The item to remove</param>
	/// <returns>true if an item was removed</returns>
	public bool RemoveEquippedItem(BaseItem item)
	{
		for (int i = 0; i < equipment.Count; i++)
		{
			if (equipment[i].item == item)
			{
				BaseItem temp = equipment[i].item;
				equipment[i].item = null;
				BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.ItemUnequiped, uniqueId, temp.GetCompoundStringDefinition() });
				if (temp.GetSubType() != BaseItem.ItemType2.GearWeaponMelee && temp.GetSubType() != BaseItem.ItemType2.GearWeaponRanged && Model && Model.gameObject.activeInHierarchy && Health > 0)
					Model.UpdateClothes();
				if (item.IsType(BaseItem.ItemType2.GearRandomWeapon) && Model && BountyManager.instance.IsFightActive() && Health > 0)
				{
					Model.UpdateWeaponModel(BountyTalentType.None);
					Model.UpdateWeaponIdle(BountyTalentType.Talent_Melee);
				}
				if (CombatGui.GetTalentFromWeapon(temp.itemType) == LastTalent)
					LastTalent = BountyTalentType.None;
				return true;
			}
		}
		return false;
	}
	/// <summary>
	/// Adds an item to character eqip
	/// </summary>
	/// <param name="item">the item to equip</param>
	/// <returns>true if item was equipped, false if item was not gear or slot is already occupied</returns>
	public bool AddEquipmentItem(BaseItem item)
	{
		if (item.GetMainType() != BaseItem.ItemType2.Gear)
			return false;

		for (int i = 0; i < equipment.Count; i++)
		{
			if (equipment[i].type == item.GetSubType())
			{
				if (equipment[i].item != null)
				{
					return false;
				}
				else
				{
					equipment[i].item = item;
					BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.ItemEquiped, uniqueId, item.GetCompoundStringDefinition() });
					if (equipment[i].item.GetSubType() != BaseItem.ItemType2.GearWeaponMelee && equipment[i].item.GetSubType() != BaseItem.ItemType2.GearWeaponRanged && Model && Model.gameObject.activeInHierarchy && !isProp)
						Model.UpdateClothes();
					if (item.IsType(BaseItem.ItemType2.GearRandomWeapon) && Model && BountyManager.instance.IsFightActive() && !isProp)
					{
						Model.UpdateWeaponModel(CombatGui.GetTalentFromWeapon(item.itemType));
						Model.UpdateWeaponIdle(CombatGui.GetTalentFromWeapon(item.itemType));
					}
					if (item.IsType(BaseItem.ItemType2.GearRandomWeapon))
						LastTalent = CombatGui.GetTalentFromWeapon(item.itemType);
					return true;
				}
			}
		}
		equipment.Add(new CharacterEquipmentSlot(item.GetSubType(), item, false));
		BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.ItemEquiped, uniqueId, item.GetCompoundStringDefinition() });
		if (item.GetSubType() != BaseItem.ItemType2.GearWeaponMelee && item.GetSubType() != BaseItem.ItemType2.GearWeaponRanged && Model && Model.gameObject.activeInHierarchy && !isProp)
			Model.UpdateClothes();
		if (item.IsType(BaseItem.ItemType2.GearRandomWeapon) && Model && BountyManager.instance.IsFightActive() && !isProp)
		{
			Model.UpdateWeaponModel(CombatGui.GetTalentFromWeapon(item.itemType));
			Model.UpdateWeaponIdle(CombatGui.GetTalentFromWeapon(item.itemType));
		}
		if (item.IsType(BaseItem.ItemType2.GearRandomWeapon))
			LastTalent = CombatGui.GetTalentFromWeapon(item.itemType);
		return true;
	}

	public int CanEquipItem(BaseItem item)
	{
		// check weapon
		if (item.IsType(BaseItem.ItemType2.GearRandomWeapon))
		{
			if (!mainCharacter && !storyCharacter && rank > 0)
			{
				if (classItemTable[charClass] == item.itemType || BaseItem.GetSubType(classItemTable[charClass]) != BaseItem.GetSubType(item.itemType)) // class restriction for weapon type
					return 1;
				else
					return -2;
			}

			BountyTalentType btt = CombatGui.GetTalentFromWeapon(item.itemType);
			if (GetTalentLevel(btt) >= 1)
			{
				return 1;
			}
			else
			{
				return -1;
			}
		}
		return 1;
	}
	public int CanUnEquipItem(BaseItem.ItemType2 type)
	{
		// check weapon
		if (BaseItem.IsType(type, BaseItem.ItemType2.Gear))
		{
			if (!mainCharacter && !storyCharacter && rank > 0)
			{
				if(classItemTable[charClass] == type)
					return -2;
			}
		}

		return 1;
	}
	#endregion

	#region stats related

	// some getters
	[fsIgnore]
	public int Level
	{
		get { return level; }
		set { level = value; }
	}
	[fsIgnore]
	public int PrevLevel
	{
		get { return prevLevel; }
		set { prevLevel = value; }
	}
	[fsIgnore]
	public int Exp
	{
		get { return exp; }
	}
	[fsIgnore]
	public int PrevExp
	{
		get { return prevExp; }
		set { prevExp = value; }
	}
	[fsIgnore]
	public int Stamina
	{
		get { return stamina; }
		set
		{
			stamina = Mathf.Clamp(value, 0, allied ? 4 : defaultStamina);
		}
	}
	[fsIgnore]
	public int Adrenaline
	{
		get { return adrenaline; }
		set
		{
			adrenaline = Mathf.Clamp(value, 0, 3);
		}
	}
	[fsIgnore]
	public bool AdrenalineChanged
	{
		get { return adrenalineChanged; }
		set { adrenalineChanged = value; }
	}
	[fsIgnore]
	public bool WasKO
	{
		get { return wasKO; }
		set { wasKO = value; }
	}
	[fsIgnore]
	public int MedicUsed
	{
		get { return medicUsed; }
		set { medicUsed = value; }
	}
	[fsIgnore]
	public int CombatLogicState
	{
		get { return combatLogicState; }
		set
		{
			combatLogicState = value;
			//Debug.LogFormat("CombatState of {0} set to {1}",characterId, value);
		}
	}
	[fsIgnore]
	public int Charge
	{
		get { return charge; }
		set
		{
			charge = Mathf.Clamp(value, 0, 3);
		}
	}
	[fsIgnore]
	public int TalentPoints
	{
		get { return talentPoints; }
		set { talentPoints = value; }
	}
	[fsIgnore]
	public int AttributePoints
	{
		get { return attributePoints; }
		set { attributePoints = value; }
	}
	
	/// <summary>
	/// gets the health points, or sets it, setting will trigger neccessary procedures
	/// </summary>
	[fsIgnore]
	public int Health
	{
		get { return healthPoints; }
		set
		{
			healthPoints = value;
			if ((BountyManager.instance.CurrentTutorialIndex >= 0 && (characterId == "Steven" || characterId == "Jack")) || invincible)
				healthPoints = Mathf.Max(healthPoints, 1);
			if (healthPoints <= 0 && !isDead)
			{
				// killed
				if (Model)
				{
					Model.UpdateDead(true);
					if (mainCharacter) // maincharacter always knocked out instead of dying
					{
						Model.myAnimator.SetBool("KnockOut", true);
						Model.LeaveIdle();
						healthPoints = 0;
					}
					else if (BountyManager.instance.IsFightActive() && race == BountyRace.Human && !isProp && ((allied && BountyManager.instance.combatManager.ArenaCombat) 
						         || (!allied && BountyManager.instance.combatManager.NonLethal)) || justKOForDeath || storyCharacter 
					         || PerkChance(BountyPerkChanceTypes.KoInsteadOfDeath)) // character is knocked out instead of dying
					{
						Model.myAnimator.SetBool("KnockOut", true);
						Model.LeaveIdle();
						healthPoints = 0;
						wasKO = true;
					}
					else //character dies
					{

						isDead = true;

						Model.myAnimator.SetTrigger("Dead");
					}
					if (Model.myAudio)
						Model.myAudio.StopLoop(null, 50);
					Model.RemoveAllParticleStates();
					if (isProp)
						Model.HideVisually();
				}
				if(allied && storyCharacter && !mainCharacter)
				{
					if(BountyManager.instance.camp.IsInParty(this))
						BountyManager.instance.camp.RegisterKo(this, BountyManager.instance.combatManager.CurrentLootEvent != null);
				}
				else if (!mainCharacter && allied && !temporary && !BountyManager.instance.combatManager.ArenaCombat && !justKOForDeath && !simulatedChar && Faction == Faction.Player)
				{
					BountyManager.instance.camp.RegisterDeath(this, BountyManager.instance.combatManager.CurrentLootEvent != null);
					recentDeath = true;

					List<BaseItem> items = GetCompleteEqupment();
					for (int i = items.Count - 1; i >= 0; i--) // put all items back to party
					{
						BountyManager.instance.camp.AddPartyItem(items[i]);
						RemoveEquippedItem(items[i]);
					}
					if(!proceduralCharacter)
						MainGuiController.instance.notificationPanel.ShowDeathCollectNotification(this, BountyManager.instance.IsFightActive());
				}
				if (allied && temporary && !justKOForDeath)
				{
					BountyManager.instance.camp.RegisterTempDeath(this);
				}
				if (Summoner != null)
				{
					Summoner.RemoveMinion(this);
				}
				if (Combatant != null)
					Combatant.ResetState(-1);
				BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.CharacterDiedImmediate, uniqueId });

			} // none deadly damage
			else if (healthPoints > GetMaxHealth())
			{
				healthPoints = GetMaxHealth();
			}
			else if (race == BountyRace.Human && !isProp && BountyManager.instance.IsFightActive() && BountyManager.instance.combatManager.combatants.Contains(this) && !simulatedChar && !BountyManager.instance.AllFightsDone)
			{
				if (HealthPercent < injuredHealthPercent && !HasCombatState(CharacterCombatState.Injured) && !backgroundCharacter)
				{
					BountyDamage dmg = new BountyDamage();
					BountyBuff next = Instantiate<BountyBuff>(SDResources.Load<BountyBuff>("Buffs/Buff_Angeschlagen"));
					next.Caster = this;
					next.Target = this;
					if (AddBuff(next, false, true) >= 0)
						next.Evaluate(BuffModule.BuffModuleTrigger.OnCast, ref dmg, null);
				}
				else if (HealthPercent >= injuredHealthPercent && HasCombatState(CharacterCombatState.Injured))
				{
					BountyBuff bb = activeBuffs.Find(n => n.buffId == "Buff_Angeschlagen");
					RemoveBuff(bb);
				}
			}
			if (BountyManager.instance.IsFightActive())
			{
				if(BountyManager.instance.combatManager.combatants.Contains(this) && Model && Model.combatSelection)
					Model.combatSelection.UpdateInfoDisplay();
			}
			if (Model)
			{
				Model.UpdateHealthState(healthPoints, (float)healthPoints / (float)GetMaxHealth());
			}

		}
	}
	public void ReCheckHealth(bool triggerHealth)
	{
		if (triggerHealth)
			Health = Mathf.Min(Health, GetMaxHealth());
		else
			healthPoints = Mathf.Min(Health, GetMaxHealth());
	}
	public int GetMaxHealth()
	{
		int result = 0;
		if (partyCharacter)
		{
			int bonus = 0;
			// bonus health
			//if (BountyManager.instance.CurrentTutorialIndex < 0)
			//{
			//	bonus = 100;
			//}

			result += baseHealth + bonus;
		}
		result += 5 * GetAttribute(BountyCharAttribute.Endurance);

		return result;
	}
	public int HealthPercent
	{
		get { return Mathf.RoundToInt((float)Health / (float)GetMaxHealth() * 100f); }
		set { Health = Mathf.RoundToInt((float)Mathf.Clamp(value, 0, 100) / 100f * (float)GetMaxHealth()); }
	}
	public void AddXp(int value) // character experience
	{
		int oldXp = exp;

		int oldLevel = level;


		int maxLevel = levelCap;
		if (level < maxLevel && !temporary && !backgroundCharacter)
		{
			exp += value;
		}

		if (storyCharacter || mainCharacter) // only story chars aka heros can level up the normal way
		{
			// alte xp und level speichern, aber nur wenn es den wahren letzen wert hat, also nicht beireits ein level up vorliegt
			if (recentLevelUp == 0)
			{
				prevLevel = oldLevel;
				prevExp = oldXp;
			}


			while (exp >= GetXpForNextLevel())
			{
				exp -= GetXpForNextLevel();
				level += 1;
				recentLevelUp += 1;
				if (level >= maxLevel)
				{
					exp = 0;
				}
			}
		}
	}
	public int GetXpForNextLevel()
	{
		return GetXpForLevel(level);
	}
	/// <summary>
	/// returns amount of xp to get to next level based on the current level
	/// </summary>
	/// <param name="pLevel">current level</param>
	/// <returns></returns>
	public static int GetXpForLevel(int pLevel)
	{
		//int[] factorTable = new int[] { 50, 60, 80, 100, 180, 280, 400, 620, 880, 1180, 1520, 1900, 2060, 2220, 2380 }; // factors

		int index = Mathf.Clamp(pLevel - 1, 0, xpTable.Length - 1);
		//int factor = factorTable[index]; // default formular
		//int factor = pLevel * 30 - 20;
		//if (pLevel < 4)
		//	factor = factorTable[pLevel - 1];
		//else if (pLevel >= 9)
		//	factor = pLevel * 60 - 260; // late game formular
		//return factor * pLevel + 100;
		return xpTable[index];
	}
	public bool CheckLevelUp(bool hide = false, int levelOverride = 0)
	{
		recentLevelUp += levelOverride;
		if (recentLevelUp == 0)
		{
			return false;
		}
		else
		{
			TalentPoints += 10 * recentLevelUp;

			for (int i = 0; i < recentLevelUp; i++)
			{
				int amount = 5;
				//if character has state: happy
				if ((baseStates != null && HasState(CharacterBaseState.Happy)))
				{
					amount = 6;
				}
				AttributePoints += amount;
			}
			recentLevelUp = 0;
			BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.OnCharacterLevelUp, uniqueId, level });
			return !hide;
		}
	}
	/// <summary>
	/// returns the attribute point cost for an attribute increase
	/// </summary>
	/// <param name="currentLevel">the current level</param>
	/// <param name="increaseType">true if it's the special increase curve</param>
	/// <returns></returns>
	public static int GetAttributCost(int currentLevel, bool increaseType)
	{
		if (increaseType)
			return currentLevel + 1;
		else
			return currentLevel / 10 + 1;
	}

	private List<CharAttributeEntry> RollLevelUp(int levels) // unused belongs to old system
	{
		List<BountyCharAttribute> rawPool = new List<BountyCharAttribute>{
			BountyCharAttribute.Endurance,
			BountyCharAttribute.Strength,
			BountyCharAttribute.Perception,
			BountyCharAttribute.Intelligence,
			//BountyCharAttribute.Reflexes,
			BountyCharAttribute.Armor,
			BountyCharAttribute.Resistance,
		};

		List<CharAttributeEntry> incrAttribute = new List<CharAttributeEntry>(); // result of increases
		List<CharAttributeEntry> pool = new List<CharAttributeEntry>(); // weights for the roll
																		//float[] ratioIdeal = new float[rawPool.Count];
																		//float[] ratioActual = new float[rawPool.Count];
		float ratioIdeal = 0f;
		float ratioActual = 0f;
		int preTotal = 0;
		int attribTotal = 0;
		int bonusOffset = 0;
		// for every new level (will be 1 most of the time)
		for (int i = 0; i < levels; i++)
		{
			pool.Clear();
			pool.AddRange(rawPool.ConvertAll<CharAttributeEntry>(n => new CharAttributeEntry(n, 100)));
			preTotal = 0;
			// add up actual attributes
			attribTotal = 0;
			for (int k = attributes.Count - 1; k >= 0; k--)
			{
				bonusOffset = attributes[k].attribute == BountyCharAttribute.Armor || attributes[k].attribute == BountyCharAttribute.Resistance ? 40 : 0;
				attribTotal += attributes[k].value + bonusOffset;
			}
			// apply talent boni to the weights to get ideal wieghts
			for (int j = 0; j < talentData.talents.Count; j++)
			{
				for (int k = pool.Count - 1; k >= 0; k--)
				{
					if (pool[k].attribute == talentData.talents[j].increasingAttribute)
						pool[k].value += talentData.talents[j].attributePerLevel * GetTalentLevel(talentData.talents[j].type);
				}
			}
			// calc pre total
			for (int k = pool.Count - 1; k >= 0; k--)
			{
				preTotal += pool[k].value;
			}

			// get ideal ratios for the weights and ratios for the actual attributes and use that to counter correct unbalanced values more strongly
			for (int k = pool.Count - 1; k >= 0; k--)
			{
				bonusOffset = pool[k].attribute == BountyCharAttribute.Armor || pool[k].attribute == BountyCharAttribute.Resistance ? 40 : 0;
				// get ratios
				ratioIdeal = (float)pool[k].value / (float)preTotal;
				ratioActual = (float)(GetAttributeRaw(pool[k].attribute) + bonusOffset) / (float)attribTotal;
				//ratioActual = Mathf.Max(ratioActual, 0.1f); // minimum value
				// adjust weights to be more balanced
				pool[k].value = Mathf.RoundToInt((float)pool[k].value * ratioIdeal / ratioActual);
			}

			int amount = 4;
			// 50% chance for 5th attribute or 100% if character has state: happy
			if ((baseStates != null && HasState(CharacterBaseState.Happy)) || SDRandom.Range(0, 100) < 50)
			{
				amount = 5;
			}

			// check if chances are within limits
			//for (int k = pool.Count - 1; k >= 0; k--)
			//{
			//	for (int j = pool.Count - 1; j >= 0; j--)
			//	{
			//		float max = BountyManager.instance.camp.GetCharacterLevelSetting(pool[j].attribute).maxAttributeIncreaseWeight;
			//		float expection = (float)pool[j].value / (float)preTotal * (float)amount;
			//		if (expection > max)
			//		{
			//			pool[j].value = Mathf.RoundToInt(expection / max * (float)pool[j].value);
			//		}
			//	}
			//	preTotal = 0;
			//	for (int u = pool.Count - 1; u >= 0; u--)
			//	{
			//		preTotal += pool[u].value;
			//	}
			//}

			// debugPrint
			//string str = "Chances:";
			//for (int k = pool.Count - 1; k >= 0; k--)
			//{
			//	str += "\n" + pool[k].attribute.ToString() + ": " + ((float)pool[k].value / (float)preTotal * (float)amount);
			//}
			//Debug.Log(str);

			// choose attrbutes by weighted chances and increase them by 1, a chance slot can be rolled more than once
			int total = 0;
			List<CharAttributeEntry> list = new List<CharAttributeEntry>();
			for (int u = pool.Count - 1; u >= 0; u--)
			{
				total += pool[u].value;
				list.Add(pool[u]);
			}
			for (int j = 0; j < amount; j++)
			{
				int point = SDRandom.Range(1, total);
				int sum = 1;
				CharAttributeEntry choice = null;
				for (int u = list.Count - 1; u >= 0; u--)
				{
					sum += list[u].value;
					if (sum > point)
					{
						choice = list[u];
						break;
					}
				}
				if (choice != null)
				{
					if (incrAttribute.Exists(n => n.attribute == choice.attribute))
					{
						incrAttribute.Find(n => n.attribute == choice.attribute).value += 1;
					}
					else
					{
						incrAttribute.Add(new CharAttributeEntry(choice.attribute, 1));
					}
					if (choice.attribute == BountyCharAttribute.Armor || choice.attribute == BountyCharAttribute.Resistance)
					{
						if (GetAttributeRaw(choice.attribute) + incrAttribute.Find(n => n.attribute == choice.attribute).value >= level)
						{
							pool.Remove(choice); // removing it makes it impossible to increase more than 1 time per level, for armor at limit value that's ok
						}
					}
				}
			}
		}
		return incrAttribute;
	}
	/// <summary>
	/// returns percent chance of event result based on talent and attribute checks
	/// </summary>
	/// <param name="talent"></param>
	/// <returns>an integer between 0 and 100 that can be used a percentage input in a RNG roll</returns>
	public int GetSkillCheckValue(BountyTalentType talent)
	{
		bool isPassive = true;
        foreach (var tal in talentData.talents)
		{
			if (tal.type == talent)
			{
				if (!tal.passive)
				{
					isPassive = false;	
				}
                break;
            }
		}
        if (isPassive)
		{
			return GetPassiveEventBonus(PassiveSkillEffect.IncreaseTalentEventChance, talent) + 45;
		}
		else 
		{
			return GetTalentLevel(talent)*3+45;
		}
		//return GetTalentLevel(talent) * 5 + 40;
	}
	/// <summary>
	/// returns percent chance of event result based on talent and attribute checks
	/// </summary>
	/// <param name="talent"></param>
	/// <returns>an integer between 0 and 100 that can be used a percentage input in a RNG roll</returns>
	public int GetSkillCheckValue(BountyCharAttribute attrib)
	{
		return GetAttribute(attrib) * 1 + 5;
	}
	/// <summary>
	/// returns percent chance of event result based on talent and attribute checks
	/// </summary>
	/// <param name="talent"></param>
	/// <returns>an integer between 0 and 100 that can be used a percentage input in a RNG roll</returns>
	public int GetLockPickValue(int lockTier)
	{
		return GetPassiveLockPickBonus(PassiveSkillEffect.IncreaseLockPickChance, lockTier) + 45;
		//return GetTalentLevel(talent) * 5 + 40;
	}
	/// <summary>
	/// adds the sums of all skill levels in the given talent
	/// </summary>
	/// <param name="talent"></param>
	/// <returns></returns>
	public int GetTalentLevel(BountyTalentType talent)
	{
		foreach(var tal in talentData.talents)
		{
			if(tal.type == talent)
			{
				int result = 0;
				foreach (var item in tal.tiers)
				{
					string tId = "";
					if (item.grantSkill)
						tId = item.grantSkill.skillId;
					else if (item.grantPassiveSkill)
						tId = item.grantPassiveSkill.skillId;

					result += GetSkillLevel(tId);
				}
				return result;
			}
		}
		return 0;
		// old version
		//int c = talents.Count;
		//for (int i = 0; i < c; i++)
		//{
		//	if (talents[i].talent == talent)
		//	{
		//		return talents[i].value;
		//	}
		//}
		//return 0;
	}
	/// <summary>
	/// changes the level of a talent. can be temporarly for use in the talent gui. talent leveling is currently not used. skills are leveled indiviually now
	/// </summary>
	/// <param name="talent"></param>
	/// <param name="value"></param>
	/// <param name="fixedValue"></param>
	/// <param name="tempMode"></param>
	public void ChangeTalentLevel(BountyTalentType talent, int value = 1, bool fixedValue = false, bool tempMode = true)
	{
		foreach (var tal in talentData.talents)
		{
			if (tal.type == talent)
			{
				int tIndex = 0;
				int tValue = 0;
				int tMax = 0;
				Dictionary<string, Vector2Int> skillTable = new Dictionary<string, Vector2Int>();
				foreach (var item in tal.tiers)
				{
					string tId = "";
					if (item.grantSkill)
					{
						tId = item.grantSkill.skillId;
						tMax = GetActiveSkill(tId).maxLvl;
					}
					else if (item.grantPassiveSkill)
					{
						tId = item.grantPassiveSkill.skillId;
						tMax = GetPassiveSkill(tId).maxLvl;
					}
					tValue = GetSkillLevel(tId);
					if(tValue < tMax)
						skillTable.Add(tId, new Vector2Int(tValue, tMax));
				}
				List<string> tKeys = new List<string>();
				while(value > 0 && skillTable.Count > 0) // loop while we have points to spent
				{
					tKeys.Clear();
					tKeys.AddRange(skillTable.Keys);
					ChangeSkillLevel(tKeys[tIndex], 1, false, false); // change skill level
					value -= 1;
					if (skillTable[tKeys[tIndex]].x >= skillTable[tKeys[tIndex]].y) // if skill is at max level, remove it from pool
						skillTable.Remove(tKeys[tIndex]);
					tIndex += 1;
					if (tIndex >= skillTable.Count) // if index is flown over, reset it
						tIndex = 0;
				}
			}
		}

		// old version
		/*
		List<CharTalentEntry> list = talents;
		int c = list.Count;
		bool found = false;
		int resultLevel = 0;
		for (int i = 0; i < c; i++)
		{
			if (list[i].talent == talent)
			{
				if (fixedValue)
					list[i].value = value;
				else
					list[i].value += value;
				found = true;
				resultLevel = list[i].value;
				break;
			}

		}
		if (!found && talentData.talents.Exists(n => n.type == talent))
		{
			list.Add(new CharTalentEntry(talent, value));
			resultLevel = value;
		}

		if (!tempMode)
			return;

		talentPoints -= resultLevel;
		tempTalentPoints += resultLevel;

		list = tempTalents;
		c = list.Count;
		found = false;
		for (int i = 0; i < c; i++)
		{
			if (list[i].talent == talent)
			{
				if (fixedValue)
					list[i].value = value;
				else
					list[i].value += value;
				found = true;
				break;
			}
		}
		if (!found && talentData.talents.Exists(n => n.type == talent))
		{
			list.Add(new CharTalentEntry(talent, value));
		}
		*/
	}
	/// <summary>
	/// applies the temporary talent changes and makes them permanent
	/// </summary>
	public void ApplyTalents()
	{
		tempTalentPoints = 0;
		tempTalents.Clear();

		// count achievemnt progress
		if (BountyManager.instance.CurrentTutorialIndex < 0)
		{
			int c = talentData.talents.Count;
			int level = 0;
			int result1 = 0;
			int result2 = 0;
			for (int i = 0; i < c; i++)
			{
				if (!talentData.talents[i].passive)
				{
					result1 = 0;
					result2 = 0;
					level = GetTalentLevel(talentData.talents[i].type);
					for (int j = 0; j < talentData.talents[i].tiers.Length; j++)
					{
						if (talentData.talents[i].tiers[j].level <= level)
						{
							if (talentData.talents[i].tiers[j].grantSkill.staminaNeeded <= 2)
								result1 += 1;
							else if (talentData.talents[i].tiers[j].grantSkill.staminaNeeded == 3)
								result2 += 1;
						}
					}
					BountyManager.instance.persistentManager.ChangeCurrentStat((BountyPersistentStat)(int)talentData.talents[i].type + 8000, result1, true);
					BountyManager.instance.persistentManager.ChangeCurrentStat((BountyPersistentStat)(int)talentData.talents[i].type + 9000, result2, true);
				}
			}
		}
	}
	/// <summary>
	/// reverts temporary talent changes
	/// </summary>
	public void RevertTalents()
	{
		talentPoints += tempTalentPoints;
		tempTalentPoints = 0;
		for (int i = tempTalents.Count - 1; i >= 0; i--)
		{
			ChangeTalentLevel(tempTalents[i].talent, -tempTalents[i].value, false, false);
		}
		tempTalents.Clear();
	}
	/// <summary>
	/// returns the list of this char's combat relevant talents
	/// </summary>
	/// <param name="limit"></param>
	/// <param name="minLevel"></param>
	/// <returns></returns>
	public List<BountyTalentType> GetCombatTalents(int limit = 4, int minLevel = 1)
	{
		List<BountyTalentType> result = new List<BountyTalentType>();

		for (int i = 0; i < talentData.talents.Count; i++)
		{
			if (!talentData.talents[i].passive && talentData.talents[i].tiers[0].grantSkill != null /*&& GetTalentLevel(talentData.talents[i].type) >= minLevel*/)
			{
				result.Add(talentData.talents[i].type);
				if (result.Count >= limit)
					break;
			}
		}


		return result;
	}
	/// <summary>
	/// returns weither the given talent is part of this char's talent set
	/// </summary>
	/// <param name="talent"></param>
	/// <returns></returns>
	public bool HasTalent(BountyTalentType talent)
	{
		int c = talentData.talents.Count;
		for (int i = 0; i < c; i++)
		{
			if (talentData.talents[i].type == talent)
				return true;
		}
		return false;
	}

	/// <summary>
	/// gives the skill ui relevant skills for a given talent, used by scill screen
	/// </summary>
	/// <param name="talent"></param>
	/// <returns></returns>
	public string[] GetTalentSkillIds(BountyTalentType talent)
	{
		List<string> result = new List<string>();
		int c = talentData.talents.Count;
		for (int i = 0; i < c; i++)
		{
			if (talentData.talents[i].type == talent)
			{
				for (int j = 0; j < talentData.talents[i].tiers.Length; j++)
				{
					if (talentData.talents[i].passive && talentData.talents[i].tiers[j].grantPassiveSkill)
						result.Add(talentData.talents[i].tiers[j].grantPassiveSkill.skillId);
					else if(talentData.talents[i].tiers[j].grantSkill && !talentData.talents[i].tiers[j].grantSkill.hiddenSkill)
						result.Add(talentData.talents[i].tiers[j].grantSkill.skillId);
				}
				break;
			}
		}
		return result.ToArray();
	}
	/// <summary>
	/// gives the skill ui relevant skill sprites for a given talent, used by skill screen
	/// </summary>
	/// <param name="talent"></param>
	/// <returns></returns>
	public Sprite[] GetTalentSkillSprites(BountyTalentType talent)
	{
		List<Sprite> result = new List<Sprite>();
		int c = talentData.talents.Count;
		for (int i = 0; i < c; i++)
		{
			if (talentData.talents[i].type == talent)
			{
				for (int j = 0; j < talentData.talents[i].tiers.Length; j++)
				{
					if (talentData.talents[i].passive && talentData.talents[i].tiers[j].grantPassiveSkill)
						result.Add(talentData.talents[i].tiers[j].grantPassiveSkill.icon);
					else if (talentData.talents[i].tiers[j].grantSkill && !talentData.talents[i].tiers[j].grantSkill.hiddenSkill)
						result.Add(talentData.talents[i].tiers[j].grantSkill.icon);
				}
				break;
			}
		}
		return result.ToArray();
	}

	/// <summary>
	/// returns all skills that belong to a given talent, used by combat
	/// </summary>
	/// <param name="talent">the talent to check for skills</param>
	/// <param name="unlockedOnly">only return unlocked skills? default: true</param>
	/// <returns>an array of skills</returns>
	public BountySkill[] GetTalentSkills(BountyTalentType talent, bool unlockedOnly = true)
	{
		List<BountySkill> result = new List<BountySkill>();
		int c = talentData.talents.Count;
		for (int i = 0; i < c; i++)
		{
			if (talentData.talents[i].type == talent)
			{
				for (int j = 0; j < talentData.talents[i].tiers.Length; j++)
				{
					if (talentData.talents[i].tiers[j].grantSkill != null && (!unlockedOnly || GetSkillLevel(talentData.talents[i].tiers[j].grantSkill.skillId) > 0))
					{
						result.Add(talentData.talents[i].tiers[j].grantSkill);
					}
				}
				break;
			}

		}
		return result.ToArray();
	}

	public BountyPassiveSkill[] GetTalentPassiveSkills(BountyTalentType talent, bool unlockedOnly = true)
	{
		List<BountyPassiveSkill> result = new();
		int c = talentData.talents.Count;
		for (int i = 0; i < c; i++)
		{
			if (talentData.talents[i].type == talent)
			{
				for (int j = 0; j < talentData.talents[i].tiers.Length; j++)
				{
					if (talentData.talents[i].tiers[j].grantPassiveSkill != null && (!unlockedOnly || GetSkillLevel(talentData.talents[i].tiers[j].grantPassiveSkill.skillId) > 0))
					{
						result.Add(talentData.talents[i].tiers[j].grantPassiveSkill);
					}
				}
			}
		}
		return result.ToArray();
	}

	/// <summary>
	/// Returns the accumulated Skill Level for a certain Talent. For example returns the Melee_Angriff Level + Melee_Runderhumgschlag + Melee_idk_Fresse_polieren_oder_so
	/// This includes the "default" Talent skill, that the player is unable to level up with skillpoints.
	/// </summary>
	/// <param name="talent"></param>
	/// <param name="unlockedOnly"></param>
	/// <returns></returns>
	public int GetTotalSkillLevelForTalentSkills(BountyTalentType talent, bool unlockedOnly = true)
	{
		var skills = GetTalentSkills(talent, unlockedOnly);
		int totalLevel = 0;
		foreach (var skill in skills)
		{
			totalLevel += GetSkillLevel(skill.skillId);
		}

		var passiveSkills = GetTalentPassiveSkills(talent, unlockedOnly);
		foreach (var passiveSkill in passiveSkills)
		{
			totalLevel += GetSkillLevel(passiveSkill.skillId);
		}
		return totalLevel;
	}

	public BountySkill[] GetAllSkills(bool unlockedOnly = true, List<BountyTalentType> excludedTalents = null)
	{
		List<BountySkill> result = new List<BountySkill>();
		int c = talentData.talents.Count;
		for (int i = 0; i < c; i++)
		{
			if (excludedTalents == null || !excludedTalents.Contains(talentData.talents[i].type))
			{
				for (int j = 0; j < talentData.talents[i].tiers.Length; j++)
				{
					if (talentData.talents[i].tiers[j].grantSkill != null && (talentData.talents[i].type == BountyTalentType.Always || !unlockedOnly || GetSkillLevel(talentData.talents[i].tiers[j].grantSkill.skillId) > 0))
					{
						result.Add(talentData.talents[i].tiers[j].grantSkill);
					}
				}
			}
		}
		return result.ToArray();
	}
	public int GetAttributePlayerImproved(BountyCharAttribute attribute)
	{
		int baseValue = 0;
		int max = improvedAttributes.Count;
		for (int i = 0; i < max; i++)
		{
			if (improvedAttributes[i].attribute == attribute)
			{
				baseValue = improvedAttributes[i].value;
				break;
			}
		}
		return baseValue;
	}
	public void ImproveAttribute(BountyCharAttribute attribute, int value, bool tempMode = true)
	{
		int foundLevel = 0;
		int cost = 0;
		bool found = false;
		int max = improvedAttributes.Count;
		for (int i = 0; i < max; i++)
		{
			if (improvedAttributes[i].attribute == attribute)
			{
				foundLevel = improvedAttributes[i].value;
				improvedAttributes[i].value += value;
				found = true;
				break;
			}
		}
		if (!found)
		{
			foundLevel = 0;
			improvedAttributes.Add(new CharAttributeEntry(attribute, value));
		}

		ChangeAttributeRaw(attribute, value);

		if (!tempMode)
			return;

		cost = GetAttributCost(foundLevel, attribute == BountyCharAttribute.BlockChance || attribute == BountyCharAttribute.CritChance);
		attributePoints -= cost;
		tempAttributePoints += cost;

		found = false;
		max = tempImprovedAttributes.Count;
		for (int i = 0; i < max; i++)
		{
			if (tempImprovedAttributes[i].attribute == attribute)
			{
				tempImprovedAttributes[i].value += value;
				found = true;
				break;
			}
		}
		if (!found)
			tempImprovedAttributes.Add(new CharAttributeEntry(attribute, value));
	}
	public void ApplyAttributes()
	{
		tempAttributePoints = 0;
		tempImprovedAttributes.Clear();
	}
	public void RevertAttributes()
	{
		attributePoints += tempAttributePoints;
		tempAttributePoints = 0;
		for (int i = tempImprovedAttributes.Count - 1; i >= 0; i--)
		{
			ImproveAttribute(tempImprovedAttributes[i].attribute, -tempImprovedAttributes[i].value, false);
		}
		tempImprovedAttributes.Clear();
	}

	/// <summary>
	/// fetches a detaileld info about the given attrbibute value, returns whether the buffed value is increased or decreased by buffs
	/// </summary>
	/// <param name="attribute">the attribute to fetch</param>
	/// <param name="rawResult">gets set to the raw value</param>
	/// <param name="buffedResult">gets set to the buffed value</param>
	/// <param name="context">BountyTalent that could increase damage</param>
	/// <param name="targetContext">a BountyCharacter that is targeted in a combat situation. The target character is used to determine if CombatPerks should be applied to increase Attributes</param>
	/// <returns>1 if buffed value is increased, -1 if decresed and 0 if not changed</returns>
	public int GetAttributeDetailed(BountyCharAttribute attribute, out int rawResult, out int buffedResult, BountyTalentType context = BountyTalentType.None, BountyCharacter targetContext = null)
	{
		//float value = GetAttributeEquipped(attribute);
		rawResult = GetAttributeEquipped(attribute, context);
		// int c = activeBuffs.Count;
		// for (int i = 0; i < c; i++)
		// {
		// 	for (int j = 0; j < activeBuffs[i].modules.Length; j++)
		// 	{
		// 		if(activeBuffs[i].modules[j].type == BuffModule.BuffModuleType.AttributeChange && activeBuffs[i].modules[j].attribute == attribute)
		// 		{
		// 			value = value * ((100f + activeBuffs[i].modules[j].effectValues[0]) / 100f);
		// 		}
		// 	}
		// }
		buffedResult = GetAttribute(attribute, context, targetContext);
		if (rawResult == buffedResult)
			return 0;
		else
			return rawResult > buffedResult ? -1 : 1;
	}
	
	/// <summary>
	/// calculates the final value of a given attribute including buffs
	/// </summary>
	/// <returns>absolute int-value of the attribute</returns>
	public int GetAttribute(BountyCharAttribute attribute, BountyTalentType context = BountyTalentType.None, BountyCharacter targetContext = null)
	{
		float value = 0;
		if (attribute == BountyCharAttribute.MeleeDamage || attribute == BountyCharAttribute.RangedDamage || attribute == BountyCharAttribute.SpecialDamage || attribute == BountyCharAttribute.SpecialRangedDamage)
		{
			// fetch buffed base attributes
			if (attribute == BountyCharAttribute.MeleeDamage)
			{
				value = GetAttribute(BountyCharAttribute.Strength, context);
			}
			else if (attribute == BountyCharAttribute.RangedDamage)
			{
				value = GetAttribute(BountyCharAttribute.Perception, context);
				if (ammoType < 3 && controllable) // only player chars should get bonus stats
					value += GetEquippedAmmoBonus();
			}
			else if (attribute == BountyCharAttribute.SpecialDamage)
			{
				value = GetAttribute(BountyCharAttribute.Intelligence, context);
			}
			else if (attribute == BountyCharAttribute.SpecialRangedDamage) // special case for armor piercing damage calculation
			{
				value = GetAttribute(BountyCharAttribute.Intelligence, context);
				attribute = BountyCharAttribute.RangedDamage;
				if (ammoType == 3 && controllable)
					value += GetEquippedAmmoBonus();
			}
			// add gear attribute modifier
			if (controllable || partyCharacter) // only player chars should get bonus stats
			{
				List<BaseItem> list = GetCompleteEqupment();
				int c0 = list.Count;
				AttributeModifier am;
				for (int i = 0; i < c0; i++)
				{
					am = list[i].attributes.Find(n => n.attribute == attribute);
					if (am != null)
					{
						if (!allied && difficultyAffectedAttribs.Contains(am.attribute) && BountyManager.instance.CurrentTutorialIndex < 0)
						{
							value += Mathf.Round((float)am.fixedValue * BountyManager.instance.DifficultyFactor);
						}
						else
							value += (float)am.fixedValue;
					}
				}
			}
		}
		else
		{
			value = GetAttributeEquipped(attribute, context);
        }
		
		// apply perks. This is done before buffs or defbuffs, perks behave as is the raw attribute value was changed. 
		foreach (var perk in perks)
		{
			if(perk is BountyAttributePerk attributePerk)
			{
				value += attributePerk.GetPerkBonus(attribute);
				//Debug.Log("Increased " + name + "'s Attribute " + attribute + " by " +
				          //attributPerk.GetPerkBonus(attribute) + " due to Perk " + attributPerk.perkID);
			}else if (perk is BountyCombatAttributePerk combatPerk)
			{
				value += combatPerk.GetPerkBonus(attribute, targetContext);
				//Debug.Log("Increased "+name+"'s Attribute "+attribute+" by "+combatPerk.GetPerkBonus(attribute,targetContext)+" during Combat due to Perk "+combatPerk.perkID);
			}
		}
		

		// decrease the attributes by 20% when hungry/thirsty
		if(attribute == BountyCharAttribute.Strength && HasState(CharacterBaseState.Hungry))
		{
			value = (value * 0.8f);
        }
        if (attribute == BountyCharAttribute.Perception && HasState(CharacterBaseState.Thirsty))
        {
            value = (value * 0.8f);
        }

        // add buff values
        int c = activeBuffs.Count;
		int add = 0;
		float factor = 1f;
		BountyDamage dmg = new BountyDamage();
		for (int i = 0; i < c; i++)
		{
			for (int j = 0; j < activeBuffs[i].modules.Length; j++)
			{
				if (activeBuffs[i].modules[j].type == BuffModule.BuffModuleType.AttributeChange && activeBuffs[i].modules[j].attribute == attribute && activeBuffs[i].CheckConditions(activeBuffs[i].modules[j], ref dmg))
				{
					int steps = 1;
					if (activeBuffs[i].modules[j].accumulatedEffect)
						steps = activeBuffs[i].Accumulation;
					//for(int k = 0; k < steps; k++)
					{
						factor *= (100f + (float)activeBuffs[i].modules[j].effectValues[0] * steps) / 100f;
						if (activeBuffs[i].modules[j].effectValues.Length > 1)
						{
							add += activeBuffs[i].modules[j].effectValues[1] * steps;
						}
					}
					// special hack for hit chance malus and the one pistol chance bonus
					if (activeBuffs[i].modules[j].attribute == BountyCharAttribute.RangedHitChance && context != BountyTalentType.Talent_Pistol && add >= 0)
					{
						add = 0;
					}
				}
			}
		}
		value *= factor;
		value += add;
		return Mathf.RoundToInt(value);
	}
	/// <summary>
	/// calculates the value of a given attribute including the gear's modifiers
	/// </summary>
	/// <returns>absolute int-value of the attribute</returns>
	public int GetAttributeEquipped(BountyCharAttribute attribute, BountyTalentType context = BountyTalentType.None)
	{
		int value = 0;
		if (attribute == BountyCharAttribute.MeleeDamage)
		{
			value = GetAttributeEquipped(BountyCharAttribute.Strength, context);
		}
		else if (attribute == BountyCharAttribute.RangedDamage)
		{
			value = GetAttributeEquipped(BountyCharAttribute.Perception, context);
			// add ammo boni
			if (ammoType < 3 && controllable)
				value += GetEquippedAmmoBonus();
		}
		else if (attribute == BountyCharAttribute.SpecialDamage)
		{
			value = GetAttributeEquipped(BountyCharAttribute.Intelligence, context);
		}
		else if (attribute == BountyCharAttribute.SpecialRangedDamage) // special case for armor piercing damage calculation
		{
			value = GetAttributeEquipped(BountyCharAttribute.Intelligence, context);
			attribute = BountyCharAttribute.RangedDamage;
			if (ammoType == 3 && controllable)
				value += GetEquippedAmmoBonus();
		}
		else
		{
			value = GetAttributeRaw(attribute);
		}
		// add gear attribute modifier
		if (controllable || partyCharacter) // only player chars should get bonus stats
		{
			List<BaseItem> list = GetCompleteEqupment();
			int c = list.Count;
			AttributeModifier am;
			for (int i = 0; i < c; i++)
			{
				am = list[i].attributes.Find(n => n.attribute == attribute);
				if (am != null)
				{
					if (!allied && difficultyAffectedAttribs.Contains(am.attribute) && BountyManager.instance.CurrentTutorialIndex < 0)
					{
						value += Mathf.RoundToInt((float)am.fixedValue * BountyManager.instance.DifficultyFactor);
					}
					else
						value += am.fixedValue;
				}
			}

			if (attribute == BountyCharAttribute.Strength && context == BountyTalentType.Talent_Swords)
				value /= 2;
			else if (context == BountyTalentType.Talent_Rifle)
				value /= 3;
		}



		return value;
	}
	private int GetEquippedAmmoBonus()
	{
		BaseItem weapon = GetEquippedItem(BaseItem.ItemType2.GearWeaponRanged);
		if (weapon == null)
			return 0;
		return BaseItem.GetAmmoBonus(weapon.itemType, ammoType);
	}

	
	/// <summary>
	/// called to check if Character may learn a Skill.
	/// </summary>
	/// <param name="attribute"></param>
	/// <returns></returns>
	public int GetAttributeRawWithPerk(BountyCharAttribute attribute)
	{
		int value = GetAttributeRaw(attribute);
		// apply perks. This is done before buffs or defbuffs, perks behave as is the raw attribute value was changed. 
		foreach (var perk in perks)
		{
			if(perk is BountyAttributePerk attributePerk)
			{
				value += attributePerk.GetPerkBonus(attribute);
			} // Combat Perks should not apply when learning weapon skills. They only happen in some fights.
		}

		return value;
	}
	/// <summary>
	/// calculates the base value of a characters attribute WITHOUT equip and buffs
	/// CAUTION: fetching the damage-type-attibutes with this method will probably return 0
	/// </summary>
	/// <returns>absolute int-value of the attribute</returns>
	public int GetAttributeRaw(BountyCharAttribute attribute)
	{
		int baseValue = 0;
		//int talentValue = 0;
		int max = attributes.Count;
		for (int i = 0; i < max; i++)
		{
			if (attributes[i].attribute == attribute)
			{
				baseValue = attributes[i].value;
				break;
			}
		}
		if (!allied && difficultyAffectedAttribs.Contains(attribute) && BountyManager.instance.CurrentTutorialIndex < 0)
		{
			baseValue = Mathf.RoundToInt((float)baseValue * BountyManager.instance.DifficultyFactor);
		}
		return baseValue;
	}
	public void ChangeAttributeRaw(BountyCharAttribute attribute, int value)
	{
		float tPercent = (float)Health / (float)GetMaxHealth();
		int max = attributes.Count;
		for (int i = 0; i < max; i++)
		{
			if (attributes[i].attribute == attribute)
			{
				attributes[i].value += value;
				return;
			}
		}
		attributes.Add(new CharAttributeEntry(attribute, value));
		Health = Mathf.RoundToInt((float)tPercent * (float)GetMaxHealth());
	}
	public bool HasBuff(string id)
	{
		return activeBuffs.Exists(n => n.buffId == id);
	}
	public List<BountyBuff> ActiveBuffs
	{
		get { return activeBuffs; }
		private set { activeBuffs = value; }
	}
	public BountyBuff[] GetActiveBuffs(bool skipInvisible = false)
	{
		List<BountyBuff> result = new List<BountyBuff>();
		result.AddRange(activeBuffs);
		if (skipInvisible)
			result.RemoveAll(n => n.dontShow);
		return result.ToArray();
	}
	/// <summary>
	/// Adds a Buff to this character. If the Character has Immunity agaist this buff, returns -1.
	/// If the character already has the buff, remove the previous buff instance an replace it with the new one.
	/// Essentially refreshing the duration. If replacing a Buff return 0. When adding a new Buff return 1.
	/// </summary>
	/// <param name="b"></param>
	/// <param name="simulated"></param>
	/// <param name="fromHit"></param>
	/// <param name="fastSkip"></param>
	/// <returns></returns>
	public int AddBuff(BountyBuff b, bool simulated = false, bool fromHit = false, bool fastSkip = false)
	{
		if (HasBuffImmunity(b))
		{
			Destroy(b);
			return -1;
		}

		// remember health percent
		float tPercent = (float)Health / (float)GetMaxHealth();

		BountyBuff preBuff = activeBuffs.Find(n => n.buffId == b.buffId);
		bool replacing = false;
		if (preBuff != null && !b.allowMultiple)
		{
			replacing = true;
			activeBuffs.Remove(preBuff);
			Destroy(preBuff);
		}
		activeBuffs.Add(b);
		// remove charge when its KO or Stun, added 31.8.20
		if (b.addCombatState == CharacterCombatState.KnockedOut || b.addCombatState == CharacterCombatState.Stunned)
		{
			charge = 0;
			chargedSkill = null;
		}

		// apply old health percent
		Health = Mathf.RoundToInt((float)tPercent * (float)GetMaxHealth());

		if (!simulated && Model)
		{
			if (b.addParticleState.particleInstance != null && !replacing && Health > 0)
			{
				if (b.staticParticlePosition == StaticParticlePosition.None)
					Model.AddParticleState(b.addParticleState.id, b.addParticleState.particleInstance, b.addParticleState.bone);
				else
					Model.AddStaticParticleState(b.addParticleState.id, b.addParticleState.particleInstance, GetStaticParticlePosition(b.staticParticlePosition));
			}
			if (b.addCombatState == CharacterCombatState.KnockedOut && !fastSkip)
			{
				TriggerEffectEvent(CharacterEffectEventType.KO_In, 4);
			}
			CheckBuffParticleState(fromHit, fastSkip);
		}
		return replacing ? 0 : 1;
	}
	public void RemoveBuff(BountyBuff b, bool simulated = false)
	{
		if (!simulated && Model)
		{
			if (b.addParticleState.particleInstance != null)
			{
				Model.RemoveParticleState(b.addParticleState.id, false, 4f);
			}
			if (b.addCombatState == CharacterCombatState.KnockedOut)
			{
				TriggerEffectEvent(CharacterEffectEventType.KO_Out, 4);
			}
		}
		if (activeBuffs.Contains(b))
		{
			Destroy(b);
			activeBuffs.Remove(b);
		}
		if (!simulated)
			CheckBuffParticleState();// moved this line here below the removal from list because otherwise the visual state would not reflect the removed buffs 25.9.20
		ReCheckHealth(true);
	}
	public void RemoveBuff(string id)
	{
		BountyBuff b = activeBuffs.Find(n => n.buffId == id);
		if (b != null)
			RemoveBuff(b);
	}
	public void ClearBuffs()
	{
		for (int i = 0; i < activeBuffs.Count; i++)
		{
			BountyManager.instance.combatManager.RemoveTrap(activeBuffs[i]);
			if (activeBuffs[i].addParticleState.particleInstance != null && Model != null)
			{
				Model.RemoveParticleState(activeBuffs[i].addParticleState.id, false, 4f);
			}
			Destroy(activeBuffs[i]);
		}
		activeBuffs.Clear();
		CheckBuffParticleState();// added 24.1.20
		ReCheckHealth(true);
	}

	private Vector3 GetStaticParticlePosition(StaticParticlePosition pos)
	{
		if (pos == StaticParticlePosition.Self)
		{
			return BountyManager.instance.combatManager.combatTransform.GetPosition(row, slot);
		}
		else if (pos == StaticParticlePosition.AlliedBack)
		{
			return BountyManager.instance.combatManager.combatTransform.GetPosition(allied ? 0 : 3, 0);
		}
		else if (pos == StaticParticlePosition.AlliedFront)
		{
			return BountyManager.instance.combatManager.combatTransform.GetPosition(allied ? 1 : 2, 0);
		}
		else if (pos == StaticParticlePosition.EnemyBack)
		{
			return BountyManager.instance.combatManager.combatTransform.GetPosition(!allied ? 0 : 3, 0);
		}
		else if (pos == StaticParticlePosition.EnemyFront)
		{
			return BountyManager.instance.combatManager.combatTransform.GetPosition(!allied ? 1 : 2, 0);
		}
		return Model.transform.position;
	}

	public void CheckBuffParticleState(bool fromHitCheck = false, bool fastSkip = false)
	{
		if (Model == null)
			return;

		bool aniChanged = false;
		CharacterCombatState state;

		BountyBoneTransform t = null;
		state = CharacterCombatState.KnockedOut;
		if (HasCombatState(state) && Health > 0)
		{
			t = Model.GetBone(BountyBoneType.HeadTop);
			BountyParticleState st = Model.GetParticleState(state.ToString());
			if (st == null && t != null)
			{
				Model.AddParticleState(state.ToString(), SDResources.Load<GameObject>("Particle/" + state.ToString()), BountyBoneType.HeadTop);
			}
			if (!Model.HasAniState(BountyAniState.ParameterType.Bool, "KnockOut", "True"))
				Model.AddAniState(new BountyAniState(BountyAniState.ParameterType.Bool, "KnockOut", "True"), false);
			aniChanged = true;
		}
		else
		{
			Model.RemoveParticleState(state.ToString());
			if (Model.HasAniState(BountyAniState.ParameterType.Bool, "KnockOut", "True"))
				Model.RemoveAniState(new BountyAniState(BountyAniState.ParameterType.Bool, "KnockOut", "True"));
		}

		state = CharacterCombatState.Stunned;
		if (HasCombatState(state) && Health > 0)
		{
			t = Model.GetBone(BountyBoneType.HeadTop);
			BountyParticleState st = Model.GetParticleState(state.ToString());
			if (st == null && t != null)
			{
				Model.AddParticleState(state.ToString(), SDResources.Load<GameObject>("Particle/" + state.ToString()), BountyBoneType.HeadTop);
			}
			if (!Model.HasAniState(BountyAniState.ParameterType.Bool, "Stunned", "True"))
				Model.AddAniState(new BountyAniState(BountyAniState.ParameterType.Bool, "Stunned", "True"), fromHitCheck);
			aniChanged = true;
		}
		else
		{
			if (Model.HasAniState(BountyAniState.ParameterType.Bool, "Stunned", "True"))
				Model.RemoveAniState(new BountyAniState(BountyAniState.ParameterType.Bool, "Stunned", "True"));
			Model.RemoveParticleState(state.ToString());
		}

		state = CharacterCombatState.Scared;
		if (HasCombatState(state) && Health > 0)
		{
			if (!Model.HasAniState(BountyAniState.ParameterType.Bool, "Scared", "True"))
				Model.AddAniState(new BountyAniState(BountyAniState.ParameterType.Bool, "Scared", "True"), fromHitCheck);
			aniChanged = true;
		}
		else
		{
			if (Model.HasAniState(BountyAniState.ParameterType.Bool, "Scared", "True"))
				Model.RemoveAniState(new BountyAniState(BountyAniState.ParameterType.Bool, "Scared", "True"));
		}

		state = CharacterCombatState.Guarding;
		if (HasCombatState(state) && Health > 0)
		{
			if (!Model.HasAniState(BountyAniState.ParameterType.Bool, "Guard", "True"))
				Model.AddAniState(new BountyAniState(BountyAniState.ParameterType.Bool, "Guard", "True"), fromHitCheck);
			aniChanged = true;
		}
		else
		{
			if (Model.HasAniState(BountyAniState.ParameterType.Bool, "Guard", "True"))
				Model.RemoveAniState(new BountyAniState(BountyAniState.ParameterType.Bool, "Guard", "True"));
		}

		state = CharacterCombatState.InCover;
		if (HasCombatState(state) && Health > 0)
		{
			if (!Model.HasAniState(BountyAniState.ParameterType.Bool, "Guard", "True"))
				Model.AddAniState(new BountyAniState(BountyAniState.ParameterType.Bool, "Guard", "True"), fromHitCheck);
			aniChanged = true;
		}
		else
		{
			if (Model.HasAniState(BountyAniState.ParameterType.Bool, "Guard", "True"))
				Model.RemoveAniState(new BountyAniState(BountyAniState.ParameterType.Bool, "Guard", "True"));
		}

		state = CharacterCombatState.Injured;
		if (HasCombatState(state) && Health > 0)
		{
			BountyParticleState st = Model.GetParticleState(state.ToString());
			if (st == null)
			{
				Model.AddParticleState(state.ToString(), SDResources.Load<GameObject>("Particle/" + state.ToString()));
			}
			if (!aniChanged && !Model.HasAniState(BountyAniState.ParameterType.Bool, "Injured", "True"))
				Model.AddAniState(new BountyAniState(BountyAniState.ParameterType.Bool, "Injured", "True"), fromHitCheck);
		}
		else
		{
			Model.RemoveParticleState(state.ToString());
			if (Model.HasAniState(BountyAniState.ParameterType.Bool, "Injured", "True"))
				Model.RemoveAniState(new BountyAniState(BountyAniState.ParameterType.Bool, "Injured", "True"));
		}

		if (fastSkip)
			Model.myAnimator.SetTrigger("SkipFast");
	}

	public void AddState(CharacterBaseState state, int dur = -1, bool restart = false)
	{
		CharacterStateEntry entry = baseStates.Find(n => n.state == state);
		if (entry != null)
		{
			if (restart)
				entry.startTime = BountyManager.instance.DateTime;
			entry.duration = dur;

		}
		else
		{
			baseStates.Add(new CharacterStateEntry(state, BountyManager.instance.DateTime, dur));

			if ((state == CharacterBaseState.Moody || state == CharacterBaseState.Infected) && BountyManager.instance.InCamp && BountyManager.instance.camp.IsInParty(this) && !mainCharacter)
			{
				BountyManager.instance.camp.SwitchPartyState(this);
			}
			if (state == CharacterBaseState.Moody && !mainCharacter && job != CampRoomType.None)
			{
				BountyManager.instance.camp.RemoveJob(this, true);
			}

			if (!newStates.Contains(state))
				newStates.Add(state);

			baseStateTracker.Add(state, new List<int>());
			if (state != CharacterBaseState.Happy && state != CharacterBaseState.Moody && state != CharacterBaseState.Infected)
				notShownStates.Add(state);

			BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.CharacterStateChange, uniqueId, (int)state, true });
			if (BountyManager.instance.IsFightActive() && state == CharacterBaseState.Infected && !IsTurnBlocked())
			{
				if (!BountyManager.instance.dialogueManager.DialogueActive)
				{
					BountyManager.instance.combatManager.ShowInfectionSpeechBubble(this);
				}
			}
		}
	}
	public void RemoveState(CharacterBaseState state)
	{
		CharacterStateEntry entry = baseStates.Find(n => n.state == state);
		if (entry != null)
		{
			baseStates.Remove(entry);
			baseStateTracker.Remove(state);
			notShownStates.Remove(state);
			if (newStates.Contains(state))
				newStates.Remove(state);
			if ((entry.state == CharacterBaseState.Moody || entry.state == CharacterBaseState.Infected) && !mainCharacter)
			{
				recentRecovery = true;
			}
			if (entry.state == CharacterBaseState.Infected && !BountyManager.instance.IsFightActive())
			{
				//startNodeStation = BaseNavNode.StationType.Any;
				//startNavMode = 2;
				//startNodeType = BaseNavNode.NodeType.Idle;
				RemoveNavState("BedRidden");
				if (Model && BountyManager.instance.InCamp)
				{
					BountyManager.instance.campScene.UpdateCharacter(this);
				}
			}
			BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.CharacterStateChange, uniqueId, (int)state, false });
		}
	}
	public bool HasState(CharacterBaseState state)
	{
		return baseStates.Exists(n => n.state == state);
	}
	public bool HasBedriddenState()
	{
		return baseStates.Exists(n => n.state == CharacterBaseState.Infected || n.state == CharacterBaseState.Injured || n.state == CharacterBaseState.Sick);
	}
	public bool HasWorkBlockingState()
	{
		return baseStates.Exists(n => n.state == CharacterBaseState.Infected || n.state == CharacterBaseState.Injured || n.state == CharacterBaseState.Sick || n.state == CharacterBaseState.Moody);
	}
	public void SkipStatesTime(int value)
	{
		for (int i = baseStates.Count - 1; i >= 0; i--)
		{
			baseStates[i].startTime += value;
		}
	}
	/// <summary>
	/// called in player camp when idle nodes are entered. tries to diplay base states
	/// </summary>
	public void ShowState()
	{
		// infected has highest priority
		CharacterBaseState state = CharacterBaseState.Infected;
		int choice = 0;
		if (HasState(state) && !InfectedStateShown)
		{

			if (baseStateTracker[state].Count == 0)
				baseStateTracker[state].AddRange(new int[] { 0, 1, 2 }); // refresh indezes
			Model.myAnimator.SetTrigger("BaseState_" + state.ToString());
			choice = Model.AgentRng.GetRange(0, baseStateTracker[state].Count); // choose an index of an index

			SpeechBubble.CreateSpeechBubble(this, LocaTokenHelper.ParseDialogueTags(BountyManager.instance.dialogueManager.LocalizedText(string.Format("{0}_{1}_{2}", "SpeechBubble", state.ToString(), baseStateTracker[state][choice]))));

			baseStateTracker[state].RemoveAt(choice); // remove choosen index

			Model.myNavAgent.masterState = 1;
			Model.myNavAgent.targetStation = BaseNavNode.StationType.Bed;
			Model.myNavAgent.targetType = BaseNavNode.NodeType.Station;
			Model.myNavAgent.state = 1;

			InfectedStateShown = true;
		}
		else if (HasState(CharacterBaseState.Injured) && !InjuredStateShown)
		{
			state = CharacterBaseState.Injured;
			if (baseStateTracker[state].Count == 0)
				baseStateTracker[state].AddRange(new int[] { 0, 1}); // refresh indezes
			Model.myAnimator.SetTrigger("BaseState_" + state.ToString());
			choice = Model.AgentRng.GetRange(0, baseStateTracker[state].Count); // choose an index of an index

			SpeechBubble.CreateSpeechBubble(this, LocaTokenHelper.ParseDialogueTags(BountyManager.instance.dialogueManager.LocalizedText(string.Format("{0}_{1}_{2}", "SpeechBubble", state.ToString(), baseStateTracker[state][choice]))));

			baseStateTracker[state].RemoveAt(choice); // remove choosen index

			Model.myNavAgent.masterState = 1;
			Model.myNavAgent.targetStation = BaseNavNode.StationType.Bed;
			Model.myNavAgent.targetType = BaseNavNode.NodeType.Station;
			Model.myNavAgent.state = 1;

			InjuredStateShown = true;
		}
		else
		{
			idleNodeIndex++;

			if ((HasState(CharacterBaseState.Hungry) || HasState(CharacterBaseState.Thirsty)) && (BaseStates.Count > 1 || idleNodeIndex % 2 == 0))
			{

				if (notShownStates.Count == 0)
				{
					notShownStates.AddRange(BaseStates.ConvertAll<CharacterBaseState>(n => n.state)); // refresh indezes
					notShownStates.Remove(CharacterBaseState.Happy);
					notShownStates.Remove(CharacterBaseState.Moody);
					notShownStates.Remove(CharacterBaseState.Infected);
				}
				choice = Model.AgentRng.GetRange(0, notShownStates.Count); // choose an index
				state = notShownStates[choice];
				notShownStates.Remove(state); // remove choosen index


				if (baseStateTracker[state].Count == 0)
					baseStateTracker[state].AddRange(new int[] { 0, 1, 2 }); // refresh indezes
				choice = Model.AgentRng.GetRange(0, baseStateTracker[state].Count); // choose an index of an index

				SpeechBubble.CreateSpeechBubble(this, LocaTokenHelper.ParseDialogueTags(BountyManager.instance.dialogueManager.LocalizedText(string.Format("{0}_{1}_{2}", "SpeechBubble", state.ToString(), baseStateTracker[state][choice]))));
				baseStateTracker[state].RemoveAt(choice); // remove choosen index

				if (state == CharacterBaseState.Thirsty)
				{
					SpecialSegment = 0;
					Model.AniEntry = true;
					Model.AniLeave = true;
				}
				Model.myAnimator.SetTrigger("BaseState_" + state.ToString());

			}
			else if (HasState(CharacterBaseState.Happy) && !HappyStateShown && Model.AgentRng.GetRange(0, 100) < 10)
			{
				state = CharacterBaseState.Happy;
				HappyStateShown = true;
				choice = Model.AgentRng.GetRange(0, 3);
				SpeechBubble.CreateSpeechBubble(this, LocaTokenHelper.ParseDialogueTags(BountyManager.instance.dialogueManager.LocalizedText(string.Format("{0}_{1}_{2}", "SpeechBubble", state.ToString(), choice))));
				Model.myAnimator.SetTrigger("BaseState_" + state.ToString());
			}
			else if (HasState(CharacterBaseState.Moody) && (Model.AgentRng.GetRange(0, 100) < 34 || !MoodyStateShown))
			{
				state = CharacterBaseState.Moody;
				MoodyStateShown = true;

				//if (baseStateTracker[state].Count == 0)
				//	baseStateTracker[state].AddRange(new int[] { 0, 1, 2 }); // refresh indezes
				choice = Model.AgentRng.GetRange(0, 3 /*baseStateTracker[state].Count*/); // choose an index of an index

				Model.myAnimator.SetTrigger("BaseState_" + state.ToString());
				SpeechBubble.CreateSpeechBubble(this, LocaTokenHelper.ParseDialogueTags(BountyManager.instance.dialogueManager.LocalizedText(string.Format("{0}_{1}_{2}", "SpeechBubble", state.ToString(), choice))));

				//baseStateTracker[state].RemoveAt(choice); // remove choosen index
			}
			else if (recentRecovery)
			{
				choice = Model.AgentRng.GetRange(0, 2); // choose an index

				Model.myAnimator.SetTrigger("BaseState_Recovered");
				SpeechBubble.CreateSpeechBubble(this, LocaTokenHelper.ParseDialogueTags(BountyManager.instance.dialogueManager.LocalizedText(string.Format("{0}_{1}_{2}", "SpeechBubble", "Recovered", choice))));

				recentRecovery = false;
			}
			else if (recentIdleSessions > 1 && Model.AgentRng.GetRange(0, 100) < 34 && !HasState(CharacterBaseState.Moody) && !HasState(CharacterBaseState.Infected))
			{
				choice = Model.AgentRng.GetRange(0, 2); // choose an index

				Model.myAnimator.SetTrigger("BaseState_Idling");
				SpeechBubble.CreateSpeechBubble(this, LocaTokenHelper.ParseDialogueTags(BountyManager.instance.dialogueManager.LocalizedText(string.Format("{0}_{1}_{2}", "SpeechBubble", "Idling", choice))));

			}

		}
	}


	public int GetPassiveItemBonus(PassiveSkillEffect effect, BaseItem.ItemType2 type, int tier)
	{
		int result = 0;
		int lvl = 0;
		for (int i = talentData.talents.Count - 1; i >= 0; i--)
		{
			if (talentData.talents[i].passive)
			{
				for (int j = 0; j < talentData.talents[i].tiers.Length; j++)
				{
					if (talentData.talents[i].tiers[j].grantPassiveSkill != null)
					{
						lvl = GetSkillLevel(talentData.talents[i].tiers[j].grantPassiveSkill.skillId);
						for (int k = 0; k < talentData.talents[i].tiers[j].grantPassiveSkill.levels[0].modules.Length; k++)
						{
							if (talentData.talents[i].tiers[j].grantPassiveSkill.levels[0].modules[k].effectType == effect)
							{
								if (BaseItem.IsType(type, talentData.talents[i].tiers[j].grantPassiveSkill.levels[0].modules[k].itemType) && (talentData.talents[i].tiers[j].grantPassiveSkill.levels[0].modules[k].itemLevel == -1 || talentData.talents[i].tiers[j].grantPassiveSkill.levels[0].modules[k].itemLevel == tier))
								{
									result += talentData.talents[i].tiers[j].grantPassiveSkill.levels[0].modules[k].effectValue * lvl;
								}
							}
						}
					}
				}
			}
		}
		return result;
	}
	public int GetPassiveEventBonus(PassiveSkillEffect effect, BountyTalentType talent)
	{
		int result = 0;
		int lvl = 0;
		for (int i = talentData.talents.Count - 1; i >= 0; i--)
		{
			if (talentData.talents[i].passive)
			{
				for (int j = 0; j < talentData.talents[i].tiers.Length; j++)
				{
					if (talentData.talents[i].tiers[j].grantPassiveSkill != null)
					{
						lvl = GetSkillLevel(talentData.talents[i].tiers[j].grantPassiveSkill.skillId)-1;
						if (lvl >= 0)
						{
                            for (int k = 0; k < talentData.talents[i].tiers[j].grantPassiveSkill.levels[lvl].modules.Length; k++)
                            {
                                if (talentData.talents[i].tiers[j].grantPassiveSkill.levels[lvl].modules[k].effectType == effect)
                                {
                                    if (talent == talentData.talents[i].tiers[j].grantPassiveSkill.levels[lvl].modules[k].talent)
                                    {
                                        result += talentData.talents[i].tiers[j].grantPassiveSkill.levels[lvl].modules[k].effectValue;
                                    }
                                }
                            }
                        }
					}
				}
			}
		}
		return result;
	}
	public int GetPassiveLockPickBonus(PassiveSkillEffect effect, int lockTier)
	{
		int result = 0;
		int lvl = 0;
		for (int i = talentData.talents.Count - 1; i >= 0; i--)
		{
			if (talentData.talents[i].passive)
			{
				for (int j = 0; j < talentData.talents[i].tiers.Length; j++)
				{
					if (talentData.talents[i].tiers[j].grantPassiveSkill != null)
					{
						lvl = GetSkillLevel(talentData.talents[i].tiers[j].grantPassiveSkill.skillId)-1;
						if (lvl >= 0)
						{
                            for (int k = 0; k < talentData.talents[i].tiers[j].grantPassiveSkill.levels[lvl].modules.Length; k++)
                            {
                                if (talentData.talents[i].tiers[j].grantPassiveSkill.levels[lvl].modules[k].effectType == effect)
                                {
                                    if (lockTier == talentData.talents[i].tiers[j].grantPassiveSkill.levels[lvl].modules[k].lockTier)
                                    {
                                        result += talentData.talents[i].tiers[j].grantPassiveSkill.levels[lvl].modules[k].effectValue;
                                    }
                                }
                            }
                        }
					}
				}
			}
		}
		if (result == 0 && lockTier == 1) result = -20;
		return result;
	}

	/// <summary>
	/// returns true if the character has any talent that can be upgraded with the current amount of talent points
	/// </summary>
	/// <returns></returns>
	public bool CanSpendTalentPoints()
	{
		int lvl;
		for (int i = talentData.talents.Count - 1; i >= 0; i--)
		{
			lvl = GetTalentLevel(talentData.talents[i].type);
			if (talentPoints > lvl + 1)
			{
				return true;
			}
		}
		return false;
	}

	// called by skill gui
	public string GetSkillDescription(BountyTalentType talent, string skillId)
	{
		foreach (var tal in talentData.talents)
		{
			if(tal.type == talent)
			{
				foreach (var tie in tal.tiers)
				{
					if (tal.passive && tie.grantPassiveSkill.skillId == skillId)
					{
						int tLvl = GetSkillLevel(skillId);
						int maxLevel = tie.grantPassiveSkill.maxLvl;
						string s = "";
						if (tal.levelInfo) // crafting job version
						{
							s = Localization.Get("Skill_" + skillId + "_" + Mathf.Clamp(tLvl, 1, maxLevel) + "_desc"); // basic info
							if (tLvl > 0 && tLvl < maxLevel) // add next level extra info
							{
								s += "\n\n<b>" + Localization.Get("Label_NextLevel")+"</b>";
								s += "\n" + Localization.Get("Skill_" + skillId + "_" + Mathf.Clamp(tLvl + 1, 1, maxLevel) + "_desc");
							}
						}
						else // diplomacy
						{
							s = Localization.Get("Skill_" + skillId + "_desc"); // basic info
							s += "\n" + Localization.Get("Skill_Skillsteigerung_" + Mathf.Clamp(tLvl, 1, maxLevel)+"_desc");  // current level extra info
							if (tLvl > 0 && tLvl < maxLevel) // add next level extra info
							{
								s += "\n\n<b>" + Localization.Get("Label_NextLevel") + "</b>";
								s += "\n" + Localization.Get("Skill_Skillsteigerung_" + (tLvl+1) + "_desc");
							}
						}

						return s;
					}
					else if(!tal.passive && tie.grantSkill.skillId == skillId)
					{
						int tLvl = GetSkillLevel(skillId);
						int maxLevel = tie.grantSkill.maxLvl;
						string s = LocaTokenHelper.ParseSkillDescTags(Localization.Get("Skill_" + skillId + "_desc"), tie.grantSkill, this); // basic info
						if (tLvl > 1) // current level extra info
						{
							s += "\n"+Localization.Get("Skill_Levelsteigerung_"+(tLvl-1) + "_desc");
						}
						if(tLvl > 0 && tLvl < maxLevel) // add next level extra info
						{
							s += "\n\n<b>" + Localization.Get("Label_NextLevel") + "</b>";
							s += "\n" + Localization.Get("Skill_Levelsteigerung_" + (tLvl) + "_desc");
						}
						return s;
					}
				}
				
			}
		}
		return "<skill not found>";
	}

	public bool CheckRevivalOptions(bool execute, bool inFight)
	{
		List<BaseItem> bi = BountyManager.instance.camp.GetPartyItems(BaseItem.ItemType2.ConsumableRevival, -1);
		if (bi.Count > 0 && bi[0].currentStack > 0)
		{
			if(execute)
			{
				ItemUseAction iua = new ItemUseAction();
				iua.user = this;
				iua.inFight = inFight;
				iua.stackAmount = 1;
				bi[0].Use(ref iua);
				BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.PlayerRevived });
			}
			return true;
		}
		return false;
	}
	public bool CheckTroopMedipack(bool execute) // Gefechtsmedizin
	{
		if (PerkChance(BountyPerkChanceTypes.DontUseTroopMedicPack))// check if character has a perk that allows them to not use troop medic packs.
		{
			return false;
		}
		List<BaseItem> bi = BountyManager.instance.camp.GetPartyItems(BaseItem.ItemType2.ConsumableTroopMedipack, -1);
		if (bi.Count > 0 && bi[0].currentStack > 0 && HealthPercent < 20)
		{
			if (execute)
			{
				ItemUseAction iua = new ItemUseAction();
				iua.user = this;
				iua.inFight = true;
				iua.stackAmount = 1;
				bi[0].Use(ref iua);
			}
			return true;
		}
		return false;
	}
	public bool CheckTroopAmmo(bool execute)// Gefechtsmunition
	{
		if (PerkChance(BountyPerkChanceTypes.DontUseTroopAmmo))// check if character has a perk that allows them to not use troop ammo.
		{
			return false;
		}
		List<BaseItem> bi = BountyManager.instance.camp.GetPartyItems(BaseItem.ItemType2.AmmoTroop, -1);
		if (bi.Count > 0 && bi[0].currentStack > 0)
		{
			if (execute)
			{
				ItemUseAction iua = new ItemUseAction();
				iua.user = this;
				iua.inFight = true;
				iua.stackAmount = 1;
				bi[0].Use(ref iua);
			}
			return true;
		}
		return false;
	}

	#endregion

	#region combat related

	public void PlayCustomEffectData(ParticleSystem particle, AudioClip sound, BountyBoneType bone = BountyBoneType.None)
	{
		if (!Model)
			return;

		if (sound != null)
		{
			Model.myAudio.PlayAudio(sound, false, false, 1f, ModelAudioSystem.AudioPlayMode.Omit);
		}
		if (particle != null)
		{
			Transform trans = Model.transform;
			if (bone != BountyBoneType.None && Model.HasBone(bone))
			{
				trans = Model.GetBone(bone, true).trans;
			}
			int value = (int)bone / 10;
			Quaternion rot = allied ^ value == 2 ? Quaternion.Euler(0f, -90f, 0f) : Quaternion.Euler(0f, 90f, 0f);
			Instantiate<ParticleSystem>(particle, trans.position, rot);
		}
	}
	public void PlayCustomEffectData(string particle, string sound, BountyBoneType bone = BountyBoneType.None)
	{
		if (!Model)
			return;

		ParticleSystem ps = null;
		AudioClip ac = null;
		if (!string.IsNullOrEmpty(particle))
		{
			ps = SDResources.Load<ParticleSystem>(particle);
		}
		if (!string.IsNullOrEmpty(sound))
		{
			ac = SDResources.Load<AudioClip>(sound);
		}
		PlayCustomEffectData(ps, ac, bone);
	}

	public void TriggerEffectEvent(CharacterEffectEventType trigger, int channel, BountyBoneType bone = BountyBoneType.None, int forceIndex = -1)
	{
		if (!Model || !Model.myAudio || effectData == null || effectData.Length == 0)
			return;
		List<CharacterEffectEvent> list = effectData.GetAllEffects();
		int c = list.Count;
		bool indexMode = forceIndex >= 0; // (int)trigger >= 34 && (int)trigger <= 48;
										  //int channel = -1;
		ModelAudioSystem.AudioPlayMode mode = ModelAudioSystem.AudioPlayMode.Restart;
		if (trigger == CharacterEffectEventType.VoiceHit || trigger == CharacterEffectEventType.VoiceDie)
		{
			mode = ModelAudioSystem.AudioPlayMode.Omit;
			//channel = 5;
		}
		for (int i = 0; i < c; i++)
		{
			if (list[i].type == trigger)
			{
				if (list[i].sound.Length > 0)
				{
					if (indexMode && forceIndex >= 0 && list[i].sound.Length > forceIndex)
					{
						Model.myAudio.PlayAudio(list[i].sound[forceIndex], false, true, 1f, ModelAudioSystem.AudioPlayMode.Restart);
					}
					else
					{
						Model.myAudio.PlayAudio(list[i].sound[Model.AgentRng.GetRange(0, list[i].sound.Length)], false, true, 1f, mode, channel);
					}
				}
				if (list[i].visual.Length > 0)
				{
					Transform trans = Model.transform;
					if (bone != BountyBoneType.None && Model.HasBone(bone))
					{
						trans = Model.GetBone(bone, true).trans;
					}
					int value = (int)bone / 10;
					Quaternion rot = allied ^ value == 2 ? Quaternion.Euler(0f, -90f, 0f) : Quaternion.Euler(0f, 90f, 0f);
					int index = indexMode ? deathEffectIndex : Model.AgentRng.GetRange(0, list[i].visual.Length);
					if(list[i].visual[index] != null)
						Instantiate<GameObject>(list[i].visual[index], trans.position, rot);
					if (indexMode)
					{
						deathEffectIndex = (deathEffectIndex + 1) % list[i].visual.Length;
					}
				}

			}
		}
	}

	public void SetEffectLoop(CharacterEffectEventType trigger, bool value, int soundChannel, BountyBoneType bone = BountyBoneType.None, int forceIndex = -1, bool extraLoop = false)
	{
		if (!Model || !Model.myAudio || effectData == null || effectData.Length == 0)
			return;
		List<CharacterEffectEvent> list = effectData.GetAllEffects();
		int c = list.Count;

		for (int i = 0; i < c; i++)
		{
			if (list[i].type == trigger)
			{
				if (list[i].sound.Length > 0)
				{
					if (forceIndex >= 0 && list[i].sound.Length > forceIndex)
					{
						if (value)
							Model.myAudio.PlayAudio(list[i].sound[forceIndex], true, true, 1f, ModelAudioSystem.AudioPlayMode.Omit, soundChannel, extraLoop ? ModelAudioSystem.ExtraMode.AlternatingPitch : ModelAudioSystem.ExtraMode.None);
						else
							Model.myAudio.StopLoop(null, soundChannel);
					}
					else
					{
						if (value)
							Model.myAudio.PlayAudio(list[i].sound[Model.AgentRng.GetRange(0, list[i].sound.Length)], true, true, 1f, ModelAudioSystem.AudioPlayMode.Omit, soundChannel, extraLoop ? ModelAudioSystem.ExtraMode.AlternatingPitch : ModelAudioSystem.ExtraMode.None);
						else
							Model.myAudio.StopLoop(null, soundChannel);
					}
				}
				/*
				if (list[i].visual.Length > 0)
				{
					Transform trans = Model.transform;
					if (bone != BountyBoneType.None && Model.HasBone(bone))
					{
						trans = Model.GetBone(bone, true).trans;
					}
					int value = (int)bone / 10;
					Quaternion rot = allied ^ value == 2 ? Quaternion.Euler(0f, -90f, 0f) : Quaternion.Euler(0f, 90f, 0f);
					int index = indexMode ? deathEffectIndex : Model.AgentRng.GetRange(0, list[i].visual.Length);
					Instantiate<GameObject>(list[i].visual[index], trans.position, rot);
					if (indexMode)
					{
						deathEffectIndex = (deathEffectIndex + 1) % list[i].visual.Length;
					}
				}
				*/
			}
		}
	}
	public void ApplyDamage(ref BountyDamage damage, bool simulate = false)
	{
		ApplyDamage(ref damage, false, false, false);
	}

	public void ApplyDamage(ref BountyDamage damage, bool simulate = false, bool dontShowFloater = false, bool dontPlayReaction = false)
	{
		int def = 0;
		damage.result = 1;

		// get defensive values and block chances
		if (damage.type == BountyDamageType.Physical)
		{
			def = GetAttribute(BountyCharAttribute.Armor, BountyTalentType.None, damage.attacker);//considers our combatperks vs this enemy
		}
		else if (damage.type == BountyDamageType.Psi)
		{
			def = GetAttribute(BountyCharAttribute.Resistance, BountyTalentType.None, damage.attacker);//considers our combatperks vs this enemy
		}

		damage.actualValue = damage.inputValue;


		if (damage.usedSkill != null && damage.usedSkill.psychicDamage && isProp)
		{
			damage.actualValue = 0;
			damage.result = 5;
		}

		// crit macht 50% mehr schaden
		if (damage.crit)
			damage.actualValue = damage.actualValue + damage.actualValue / 2;

		if (damage.usedSkill != null)
		{
			if ((damage.usedSkill.needsWeapon || (damage.attacker && damage.attacker.isProp)) && ((damage.usedSkill.weaponType == BaseItem.ItemType2.GearWeaponRifle) || damage.usedSkill.weaponType == BaseItem.ItemType2.GearWeaponBlade))
			{
				if (HasHeavyArmor())
				{
					int malusValue = BountyCombatManager.pierceDamageArmorMalus;
					if (damage.usedSkill.weaponType == BaseItem.ItemType2.GearWeaponRifle && damage.ammoLevel < 3)
						malusValue = BountyCombatManager.pierceDamageArmorMalusPierced;
					damage.actualValue = Mathf.RoundToInt((float)damage.actualValue * (100f - (float)malusValue) / 100f);

				}
				//else
				//	damage.actualValue = Mathf.RoundToInt((float)damage.actualValue * (100f - (float)BountyCombatManager.pierceDamageGeneralMalus) / 100);
			}
		}

		if (!damage.ignoresDefence && damage.result == 1)
		{
			if (damage.armorPiercing) // armor piercing skill ignores 50% of armor value
				def = def / 2;
			int preDefDamage = damage.actualValue;

			// reduced by armor or resistance attrib
			damage.actualValue = Mathf.RoundToInt((float)damage.actualValue * (100f - def) / 100f);

			// clamp
			damage.actualValue = Mathf.Max(damage.actualValue, 1);

			if (HeavyArmorPoints > 0 && !damage.friendlyFire)
			{
				//int def2 = damage.actualValue / 2;
				damage.armorState = 2;
				int def2 = Mathf.Min(HeavyArmorPoints, damage.armorPiercing ? preDefDamage : damage.actualValue); // armor piercing skill can hit armor with full power
				HeavyArmorPoints -= def2;
				damage.result = 2;
				damage.actualValue = Mathf.RoundToInt(0.25f * (float)damage.actualValue);
				if (HeavyArmorPoints == 0)
				{
					damage.armorBroken = true;
					// zerstöre rüstungs model
					if (Model && Model.ArmorObject)
					{
						dontShowFloater = true;
						Model.ArmorObject.SetActive(false);
						// effekte
						Instantiate<ParticleSystem>(SDResources.Load<ParticleSystem>("Particle/ArmorDestroy"), Model.transform.position, Quaternion.identity);
						//Model.myAudio.PlayAudio(Resources.Load<AudioClip>("Sound/ArmorDestroy"));
						BountyFloater.CreateFloater(Localization.Get("Label_ArmorBroken"), BountyFloater.MovementType.Float, this, new Vector3(0f, 1.1f, 0f));
					}
				}
			}
			else if (damage.type == BountyDamageType.Physical && ArmorPoints > 0 && !damage.friendlyFire)
			{
				damage.armorState = 1;
				int def2 = Mathf.Min(ArmorPoints, damage.actualValue);
				ArmorPoints -= def2;
				damage.actualValue = damage.actualValue - def2;
				//if(damage.actualValue == 0) // line disabled which means -> even slight blocking will prevent buffs from being casted
				damage.result = 2;
			}
			else if (damage.type == BountyDamageType.Psi && ResistPoints > 0 && !damage.friendlyFire)
			{
				damage.armorState = 1;
				int def2 = Mathf.Min(ResistPoints, damage.actualValue);
				ResistPoints -= def2;
				damage.actualValue = damage.actualValue - def2;
				//if (damage.actualValue == 0) // line disabled which means -> even slight blocking will prevent buffs from being casted
				damage.result = 2;
			}

		}

		int chance = 0;
		if (damage.usedSkill != null && !damage.friendlyFire)
		{
			if (damage.usedSkill.isMelee)
			{
				if (!HasCombatState(CharacterCombatState.Stunned) && !HasCombatState(CharacterCombatState.KnockedOut) && !damage.ignoresDefence && !damage.crit && !damage.usedSkill.unblockable)
				{
					// apply block chance for melee
					chance = GetAttribute(BountyCharAttribute.BlockChance, BountyTalentType.None, damage.attacker); //considers our combatperks vs this enemy
					int overBlock = BountyManager.instance.combatManager.GetBlockOverrideTo(characterId);
					if (overBlock == 1)
						chance = 100;
					else if (overBlock == -1)
						chance = 0;
					if (SDRandom.Range(0, 100) < chance)
					{
						damage.result = 2;
						damage.actualValue /= 2;
						damage.actualValue = Mathf.Max(damage.actualValue, 1);
						damage.crit = false;
						damage.knockBack = false;
					}
				}
				if (!damage.crit)
				{
					// apply graze hit chance for melee
					chance = 100;
					//chance += GetAttribute(BountyCharAttribute.DodgeChance);
					chance -= damage.usedSkill.hitChanceMalus;
					chance += damage.attacker.GetAttribute(BountyCharAttribute.RangedHitChance, CombatGui.GetTalentFromWeapon(damage.usedSkill.weaponType), this); // attackers hitchance, considers attackers combat perks
					chance = Mathf.Max(chance, 0);
					int overBlock = BountyManager.instance.combatManager.GetBlockOverrideTo(characterId);
					if (overBlock == 1)
						chance = 0;
					else if (overBlock == -1)
						chance = 100;
					if (SDRandom.Range(0, 100) >= chance)
					{
						damage.result = 4;
						damage.actualValue /= 2;
						damage.actualValue = Mathf.Max(damage.actualValue, 1);
						damage.crit = false;
						damage.knockBack = false;
					}
				}
			}
			else
			{
				if (!damage.crit && !damage.usedSkill.undodgable)
				{
					// apply graze shot chance for ranged
					chance = BountyCombatManager.baseHitChance;
					chance -= GetAttribute(BountyCharAttribute.DodgeChance, BountyTalentType.None, damage.attacker); // my dodge chance, considers combat perks that might increase my dodge chance
					if (HasCombatState(CharacterCombatState.InCover) && Vector3.Dot(Model.transform.forward, (Model.transform.position - damage.attacker.Model.transform.position).normalized) >= 0) // check cover effect: only covers if the angle of attack is not from behind or perpendicular to the cover direction
						chance -= 30;
					chance += damage.attacker.GetAttribute(BountyCharAttribute.RangedHitChance, CombatGui.GetTalentFromWeapon(damage.usedSkill.weaponType), this); // attackers hitchance, considers combat perks that might increase their hit chance
					chance -= damage.usedSkill.hitChanceMalus;
					chance = Mathf.Clamp(chance, 0, 100);
					int overBlock = BountyManager.instance.combatManager.GetBlockOverrideTo(characterId);
					if (overBlock == 1)
						chance = 0;
					else if (overBlock == -1)
						chance = 100;
					if (SDRandom.Range(0, 100) >= chance)
					{
						damage.result = 3;
						damage.actualValue /= 2;
						damage.actualValue = Mathf.Max(damage.actualValue, 1);
						damage.crit = false;
						damage.knockBack = false;
					}
				}

			}
		}

		// block knockback damage due to immunity
		if (damage.knockBack && HasKnockBackImmunity())
		{
			damage.knockBack = false;
		}
		// half damage when this is knockbackresult damage
		if (damage.knockbackSecondaryHit)
		{
			damage.actualValue /= 2;
			damage.actualValue = Mathf.Max(damage.actualValue, 1);
		}



		damage.shownValue = damage.actualValue;
		//int adrenalineDamage = 0;

		if (damage.usedSkill != null)
		{
			if (damage.usedSkill.weaponType == BaseItem.ItemType2.GearWeaponRifle)
				damage.adrenalineDamage = Mathf.RoundToInt((float)damage.actualValue * (BountyCombatManager.adrenalineBoni2[damage.adrenalineValue] - 1f));
			else
				damage.adrenalineDamage = Mathf.RoundToInt((float)damage.actualValue * (BountyCombatManager.adrenalineBoni[damage.adrenalineValue] - 1f));

			damage.actualValue += damage.adrenalineDamage;
		}

		if (!damage.peaceful && Health > 0 && Health - damage.actualValue <= 0 && !invincible && !(BountyManager.instance.CurrentTutorialIndex >= 0 && characterId == "Steven"))
		{
			damage.killingBlow = true;
		}

		//Debug.Log(characterId+ " looses "+ damage.actualValue+" HP");
		if (!damage.peaceful && !simulate)
		{
			Health -= damage.actualValue;
			BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.DamageReceived, uniqueId, damage.actualValue });

			if (damage.source == DamageSource.Buff || damage.source == DamageSource.DefenseCombat || damage.source == DamageSource.Event || damage.source == DamageSource.Ambush)
				deathReason = DamageSource.Combat;
			else
				deathReason = damage.source;
		}

		// removed die ani selection from here


		List<string> infocode = new List<string>();
		string colorcode = "#FF8000";

		if (damage.type == BountyDamageType.Psi)
			colorcode = "#44AAFF";

		if (damage.crit)
			colorcode = "#FF0000";
		else if (damage.result > 1)
			colorcode = "#FFFF00";


		//if (damage.adrenalineValue > 0 && damage.adrenalineDamage > 0)
		//{
		//	infocode.Add(string.Format("<color={0}>{1}: {2}</color>", "#FF5000", Localization.Get("Label_AdrenalineHit"), damage.adrenalineDamage));
		//}

		if (damage.result == 1)
		{
			if (damage.bigHit)
				damage.reactResult += 3;
		}
		else if (damage.armorState == 1)
		{
			infocode.Add(string.Format("<color={0}>{1}</color>", colorcode, Localization.Get("Label_ArmorHit")));
			damage.reactResult += 1;
		}
		else if (damage.armorState == 2)
		{
			infocode.Add(string.Format("<color={0}>{1}</color>", colorcode, Localization.Get("Label_HeavyArmorHit")));
			damage.reactResult += 1;
		}
		else if (damage.result == 2)
		{
			//infocode.Add(Localization.Get("Label_BlockHit"));
			infocode.Add(string.Format("<color={0}>{1}</color>", colorcode, Localization.Get("Label_BlockHit")));
			damage.reactResult += 1;
		}
		else if (damage.result == 3)
		{
			//infocode.Add(Localization.Get("Label_GrazeHit"));
			infocode.Add(string.Format("<color={0}>{1}</color>", colorcode, Localization.Get("Label_GrazeHit")));
			damage.reactResult += 1;
		}
		else if (damage.result == 4)
		{
			infocode.Add(string.Format("<color={0}>{1}</color>", colorcode, Localization.Get("Label_GrazeHit")));
			//infocode.Add(string.Format("<color={0}>{1}</color>", colorcode, Localization.Get("Label_BlockHit")));
			damage.reactResult += 1;
		}
		else if (damage.result == 5)
		{
			infocode.Add(string.Format("<color={0}>{1}</color>", colorcode, Localization.Get("Label_Immune")));
		}
		if (damage.crit)
		{
			//infocode.Add(Localization.Get("Label_CritHit"));
			infocode.Add(string.Format("<color={0}>{1}</color>", colorcode, Localization.Get("Label_CritHit")));
			if (damage.bigHit)
				damage.reactResult += 1;
			else
				damage.reactResult += 2;
		}
		if (!dontShowFloater && Model && (Health > 0 || damage.killingBlow))
		{

			string floaterText = "";
			for (int i = 0; i < infocode.Count; i++)
			{
				floaterText += "\n" + infocode[i];
			}
			floaterText = string.Format("<color={0}>{1}</color>{2}", colorcode, damage.shownValue, floaterText);
			BountyFloater.CreateFloater(floaterText, BountyFloater.MovementType.Drop, this, new Vector3(0f, 1f, 0f));
		}

		// removed hit reaction ani from here

		//if (BountyManager.instance.IsFightActive() && BountyManager.instance.combatManager.combatants.Contains(this) && !simulate)
		//{
		//	BountyManager.instance.combatManager.combatTransform.GetSlot(Row, Slot).CharInfo.SetHealth(Health, GetMaxHealth());
		//}

		if (!dontPlayReaction)
		{
			ExecuteHitReaction(damage);
		}

	}

	public void ExecuteHitReaction(BountyDamage damage)
	{
		if (!Model)
			return;


		if (damage.killingBlow)
		{
			// play death ani
			int deathIndex = 0;
			if (HasCombatState(CharacterCombatState.KnockedOut))
			{
				deathIndex = 8;
			}
			else if (currentIncomingKnockback == null && damage.knockBack && damage.result == 1)
			{
				deathIndex = 3;
				int front = allied ? 1 : 2;
				if (Row == front && BountyManager.instance.combatManager.combatTransform.GetModel(allied ? 0 : 3, Slot) != null)
				{
					CurrentIncomingKnockback = damage;
				}
			}
			else if (damage.specialAniType != 0)
			{
				deathIndex = damage.specialAniType;
			}
			else if (damage.usedSkill && damage.usedSkill.weaponType == BaseItem.ItemType2.GearWeaponShotgun)
			{
				deathIndex = 3;
			}
			else if (damage.source == DamageSource.Buff || (damage.usedSkill && damage.usedSkill.weaponType == BaseItem.ItemType2.GearWeaponPistol) || (damage.usedSkill && damage.usedSkill.weaponType == BaseItem.ItemType2.GearWeaponRifle))
			{
				deathIndex = 2;
			}

			Model.myAnimator.SetInteger("DeathIndex", deathIndex);
			TriggerEffectEvent(CharacterEffectEventType.Die, 3, BountyBoneType.None, deathIndex);
			TriggerEffectEvent(CharacterEffectEventType.VoiceDie, 5);

		}
		//else
		{
			Vector3 attackDir = Vector3.zero;
			float attackVectorDot = 1f;
			if(damage.target != null && damage.attacker != null)
			{
				attackDir = (damage.target.Model.transform.position - damage.target.Model.transform.position).normalized;
				attackVectorDot = Vector3.Dot(attackDir, damage.target.Model.transform.forward);
			}

			// hit effects
			if (damage.melee || (damage.attacker != null && damage.attacker.allied == this.allied))
			{
				damage.reactResult += 16;
			}
			else if (damage.usedSkill != null && damage.usedSkill.needsWeapon && damage.usedSkill.weaponType != BaseItem.ItemType2.None) // shot sounds should only occur when weapons are involved
			{
				damage.reactResult += 11;
			}

			if (damage.armorState == 2)
				damage.reactResult = 21;

			if (damage.result == 1 && damage.armorState == 0 && !damage.killingBlow)
				TriggerEffectEvent(CharacterEffectEventType.VoiceHit, 5);

			if (damage.result == 1)
				TriggerEffectEvent((CharacterEffectEventType)damage.reactResult, 2, damage.effectBones);

			// play hit anis
			if (!damage.killingBlow /*&& !IsHitBlocked()*/ && currentIncomingKnockback == null && damage.attacker != this)
			{
				int hitIndex = 0;
				if (damage.knockBack && damage.result == 1 && !HasCombatState(CharacterCombatState.KnockedOut) && Combatant.CanExecuteHitAnimation(damage))
				{
					// knocked back anis
					//int front = allied ? 1 : 2;
					//if(Row == front && BountyManager.instance.combatManager.combatTransform.GetModel(allied ? 0 : 3, Slot) != null)
					//{
					//	// wenn charackter kollison dann nur frontal ani
					//	hitIndex = 10;
					//}
					//else
					{
						hitIndex = SDRandom.Range(11, 13);
					}

					TriggerEffectEvent(CharacterEffectEventType.Hit_Knockback, 2, BountyBoneType.None, hitIndex - 10);
					CurrentIncomingKnockback = damage;
				}
				else if (damage.specialAniType != 0 && damage.specialAniType != 5)
				{
					// unterscheide zwischen gift, wunde und feuer hits
					hitIndex = damage.specialAniType;
				}
				else if (damage.knockbackSecondaryHit)
				{
					hitIndex = 1;
				}
				else if (damage.usedSkill != null && damage.usedSkill.psychicDamage)
				{
					hitIndex = 8;
				}
				else if (damage.usedSkill != null && damage.martialArts)
				{
					// martial art hits
					if(damage.specialAniType == 13)
					{
						if (attackVectorDot >= 0f) // hit from behind
							hitIndex = 42;
						else
							hitIndex = 41; // hit from front
					}
					else
					{
						hitIndex = SDRandom.Range(30, 32);

					}
				}
				else
				{
					// normale hits
					if (race == BountyRace.Zombie)
					{
						if (damage.usedSkill != null && BaseItem.IsType(damage.usedSkill.weaponType, BaseItem.ItemType2.GearWeaponRanged) && damage.usedSkill.needsWeapon && damage.usedSkill.weaponType != BaseItem.ItemType2.None)
						{
							hitIndex = SDRandom.Range(20, 22);
						}
						else
						{
							hitIndex = SDRandom.Range(0, 3);
						}
					}
					else
					{
						hitIndex = SDRandom.Range(0, 3);
					}

				}

				if (!isProp)
				{
					Model.myAnimator.SetInteger("HitIndex", hitIndex);
					// dont play hit if an attack is going to play or already playing because it looks wrong and can mess up the leave idle trigger state
					if (
						!(Model.myAnimator.GetBool("Attack_Melee") || (Model.HumanAnimator && Model.myAnimator.GetBool("Attack_Blade")) || (Model.HumanAnimator && Model.myAnimator.GetBool("Attack_Gun")) ||
						(Model.HumanAnimator && Model.myAnimator.GetBool("Attack_Shotgun")) || (Model.HumanAnimator && Model.myAnimator.GetBool("Attack_Rifle")) || Model.myAnimator.GetBool("Attack_Special") ||
						(Model.HumanAnimator && Model.myAnimator.GetBool("Attack_Guard")) || Model.LastAnimationArea == BountyAnimationArea.Skill)
					)
					{
						Model.myAnimator.SetTrigger("Hit");
						//if (Combatant.CanExecuteHitAnimation(damage))
						//{
						//	Model.myAnimator.SetTrigger("Hit");
						//	Combatant.ExecuteHitAnimation(damage);
						//}
						//else
						//{
						//	Model.myAnimator.SetTrigger("SmallHit");
						//}
					}

				}
			}
		}
		if (!isProp && !damage.killingBlow)
		{
			if (damage.knockBack && damage.result == 1 && !HasCombatState(CharacterCombatState.KnockedOut))
				CombatLogicState = 5;
			else if (CombatLogicState != 5 && !HasCombatState(CharacterCombatState.KnockedOut))
				CombatLogicState = 4;
		}
	}

	/// <summary>
	/// returns true if this character has a buff on it that skips its turn, eg KO or stun
	/// </summary>
	/// <returns></returns>
	public bool IsTurnBlocked()
	{
		int c = activeBuffs.Count;
		for (int i = 0; i < c; i++)
		{
			if (activeBuffs[i].addCombatState == CharacterCombatState.KnockedOut || activeBuffs[i].addCombatState == CharacterCombatState.Stunned || activeBuffs[i].addCombatState == CharacterCombatState.Scared)
				return true;
		}
		return false;
	}
	/// <summary>
	/// returns true if the character is KO anf cant show hit and death reaction anis properly
	/// </summary>
	/// <returns></returns>
	public bool IsHitBlocked()
	{
		int c = activeBuffs.Count;
		for (int i = 0; i < c; i++)
		{
			if (activeBuffs[i].addCombatState == CharacterCombatState.KnockedOut)
				return true;
		}
		return false;
	}
	public bool IsMoveBlocked()
	{
		int c = activeBuffs.Count;
		for (int i = 0; i < c; i++)
		{
			if (activeBuffs[i].addCombatState == CharacterCombatState.KnockedOut || activeBuffs[i].addCombatState == CharacterCombatState.Stunned)
				return true;
		}
		return false;
	}
	public bool HasCombatState(CharacterCombatState state)
	{
		int c = activeBuffs.Count;
		for (int i = 0; i < c; i++)
		{
			if (activeBuffs[i].addCombatState == state)
				return true;
		}
		return false;
	}
	/// <summary>
	/// returns 1 (specific immunmity) or 0 (general immunity) if the combatant is immune to the given buff and the buff was rejected or -1 when the buff is applicable
	/// </summary>
	/// <param name="b"></param>
	/// <returns></returns>
	public bool HasBuffImmunity(BountyBuff b, bool showFloater = true)
	{
		if (isProp) // props are immune against most buffs
		{
			if (!b.applicableToProps)
			{
				if (showFloater)
					BountyFloater.CreateFloater(string.Format("<color={0}>{1}</color>", "#70AAFF", Localization.Get("Label_Immune")), BountyFloater.MovementType.Pop, this, new Vector3(0f, 2f, 0f));
				return true;
			}
		}

		int c = activeBuffs.Count;
		for (int i = 0; i < c; i++)
		{
			for (int j = 0; j < activeBuffs[i].addBuffImmunityChance.Length; j++)
			{
				if (activeBuffs[i].addBuffImmunityChance[j].buff.buffId == b.buffId)
				{
					if (SDRandom.Range(0, 100) < activeBuffs[i].addBuffImmunityChance[j].chance)
					{
						if (showFloater)
							BountyFloater.CreateFloater(string.Format("<color={0}>{1}</color>", "#70AAFF", Localization.Get(activeBuffs[i].buffId)), BountyFloater.MovementType.Pop, this, new Vector3(0f, 2f, 0f));
						return true;
					}
				}
				//if (activeBuffs[i].addBuffImmunities[j].buffId == b.buffId)
				//{
				//	return true;
				//}
			}

		}
		return false;
	}
	public bool HasKnockBackImmunity()
	{
		if (isProp)
			return true;

		int c = activeBuffs.Count;
		for (int i = 0; i < c; i++)
		{
			if (activeBuffs[i].grantsKnockBackImmunity)
				return true;
		}
		return false;
	}
	/// <summary>
	/// updates the cooldown list for the the given skill and enters it's respective cooldown value
	/// </summary>
	/// <param name="skill"></param>
	public void SetSkillUsage(BountySkill skill)
	{
		if (skillCooldowns.ContainsKey(skill))
		{
			skillCooldowns[skill] = skill.cooldown;
		}
		else
		{
			skillCooldowns.Add(skill, skill.cooldown);
		}
	}
	/// <summary>
	/// reduces all skillcooldown greater than 0 by 1
	/// </summary>
	public void UpdateSkillCooldowns()
	{
		List<BountySkill> list = new List<BountySkill>(skillCooldowns.Keys);
		for (int i = 0; i < list.Count; i++)
		{
			if (skillCooldowns[list[i]] > 0 && !activeMinions.Exists(n => n.freezeSummonerSkill == list[i])) // added minion check for cooldowns 1.12.20
			{
				skillCooldowns[list[i]] -= 1f;
			}
		}
	}
	public void ClearSkillCooldowns(bool keepSummoningLocks = false)
	{
		//skillCooldowns.Clear();
		List<BountySkill> list = new List<BountySkill>(skillCooldowns.Keys);
		for (int i = 0; i < list.Count; i++)
		{
			if(!keepSummoningLocks || !activeMinions.Exists(n => n.freezeSummonerSkill == list[i]))
			{
				skillCooldowns.Remove(list[i]);
			}
		}
	}
	/// <summary>
	/// fetches the skill cooldown for a given skills in rounds
	/// </summary>
	/// <param name="skill"></param>
	/// <returns></returns>
	public int GetSkillCooldown(BountySkill skill)
	{
		if (skillCooldowns.ContainsKey(skill))
		{
			return (int)Mathf.Min(skillCooldowns[skill], skill.cooldown);
		}
		else
		{
			return 0;
		}
	}

	public void AddMinion(BountyCharacter c)
	{
		if (!activeMinions.Contains(c))
		{
			activeMinions.Add(c);
		}
	}
	public void RemoveMinion(BountyCharacter c)
	{
		if (activeMinions.Contains(c))
			activeMinions.Remove(c);
	}
	public void ClearMinions()
	{
		activeMinions.Clear();
	}
	[fsIgnore]
	public int StartRow
	{
		get { return startRow; }
		set { startRow = value; }
	}
	[fsIgnore]
	public int StartSlot
	{
		get { return startSlot; }
		set { startSlot = value; }
	}
	[fsIgnore]
	public int Row
	{
		get { return row; }
		set { row = value; }
	}
	[fsIgnore]
	public int Slot
	{
		get { return slot; }
		set { slot = value; }
	}
	[fsIgnore]
	public BountySkill LastSkill
	{
		get { return lastSkill; }
		set { lastSkill = value; }
	}
	[fsIgnore]
	public BountyCombatAI.AiMove LastMove
	{
		get { return lastMove; }
		set { lastMove = value; }
	}
	[fsIgnore]
	public List<BountyProjectile> ActiveProjectiles
	{
		get { return activeProjectiles; }
	}
	[fsIgnore]
	public BountyCharacter Summoner
	{
		get { return summoner; }
		set { summoner = value; }
	}
	[fsIgnore]
	public BountyTalentType LastTalent
	{
		get
		{
			if (lastTalent == BountyTalentType.None || lastTalent == BountyTalentType.Talent_Grenadier || lastTalent == BountyTalentType.Talent_Hunting)
			{
				if (GetEquippedItem(BaseItem.ItemType2.GearWeaponRanged) != null) // changed order of weapon checks on 30.3.20 by M.E.
				{
					lastTalent = CombatGui.GetTalentFromWeapon(GetEquippedItem(BaseItem.ItemType2.GearWeaponRanged).itemType);
				}
				else if (GetEquippedItem(BaseItem.ItemType2.GearWeaponMelee) != null)
				{
					lastTalent = CombatGui.GetTalentFromWeapon(GetEquippedItem(BaseItem.ItemType2.GearWeaponMelee).itemType);
				}
				else
				{
					if (allied && controllable)
						lastTalent = BountyTalentType.Talent_Melee;
					else
					{
						lastTalent = talentData.talents[0].type;
					}

				}
			}
			return lastTalent;
		}
		set
		{
			//if(value != BountyTalentType.Talent_Grenadier && value != BountyTalentType.Talent_Hunting)
			lastTalent = value;
		}
	}
	[fsIgnore]
	public BountyTalentType LastWeaponTalent
	{
		get
		{
			if (allied && controllable)
			{
				if (GetEquippedItem(BaseItem.ItemType2.GearWeaponRanged) != null)
				{
					return CombatGui.GetTalentFromWeapon(GetEquippedItem(BaseItem.ItemType2.GearWeaponRanged).itemType);
				}
				else if (GetEquippedItem(BaseItem.ItemType2.GearWeaponMelee) != null)
				{
					return CombatGui.GetTalentFromWeapon(GetEquippedItem(BaseItem.ItemType2.GearWeaponMelee).itemType);
				}
				else
				{
					return BountyTalentType.Talent_Melee;
				}
			}
			else
				return talentData.talents[0].type;
		}
	}
	[fsIgnore]
	public BountyTalentType EquippedMeleeTalent
	{
		get
		{
			if (GetEquippedItem(BaseItem.ItemType2.GearWeaponMelee) != null)
			{
				return CombatGui.GetTalentFromWeapon(GetEquippedItem(BaseItem.ItemType2.GearWeaponMelee).itemType);
			}
			else
			{
				return BountyTalentType.Talent_Fists;
			}
		}
	}
	[fsIgnore]
	public BountyTalentType EquippedRangedTalent
	{
		get
		{
			if (GetEquippedItem(BaseItem.ItemType2.GearWeaponRanged) != null)
			{
				return CombatGui.GetTalentFromWeapon(GetEquippedItem(BaseItem.ItemType2.GearWeaponRanged).itemType);
			}
			else
			{
				return BountyTalentType.None;
			}
		}
	}
	[fsIgnore]
	public BountyTalentType DisplayTalent
	{
		get
		{
			if (displayTalent == BountyTalentType.None)
				displayTalent = LastTalent;
			return displayTalent;
		}
		set { displayTalent = value; }
	}
	[fsIgnore]
	public BountyDamage CurrentIncomingKnockback
	{
		get { return currentIncomingKnockback; }
		set { currentIncomingKnockback = value; }
	}
	[fsIgnore]
	public int SkillRepeats
	{
		get { return skillRepeats; }
		set
		{
			skillRepeats = Mathf.Clamp(value, 0, 100);
		}
	}
	[fsIgnore]
	public BountySkill CurrentSkill
	{
		get { return currentSkill; }
		set { currentSkill = value; }
	}
	public void ClearCurrentSkill()
	{
		currentSkill = null;
	}
	[fsIgnore]
	public BountySkill ChargedSkill
	{
		get { return chargedSkill; }
		set { chargedSkill = value; }
	}
	[fsIgnore]
	public List<BountyCharacter> CurrentTargets
	{
		get { return currentTargets; }
		set { currentTargets = value; }
	}
	//[fsIgnore]
	//public List<BountyCharacter> TargetRecord
	//{
	//	get { return targetRecord; }
	//	set { targetRecord = value; }
	//}
	[fsIgnore]
	public int TargetRow
	{
		get { return targetRow; }
		set { targetRow = value; }
	}
	[fsIgnore]
	public int TargetSlot
	{
		get { return targetSlot; }
		set { targetSlot = value; }
	}
	[fsIgnore]
	public int ArmorPoints
	{
		get { return armorPoints; }
		set { armorPoints = value; /*if (BountyManager.instance.IsFightActive()) BountyManager.instance.combatManager.combatTransform.GetSlot(Row, Slot).InfoActive = true;*/ }
	}
	[fsIgnore]
	public int ResistPoints
	{
		get { return resistPoints; }
		set { resistPoints = value; /*if (BountyManager.instance.IsFightActive()) BountyManager.instance.combatManager.combatTransform.GetSlot(Row, Slot).InfoActive = true;*/ }
	}
	[fsIgnore]
	public int HeavyArmorPoints
	{
		get { return heavyArmorPoints; }
		set { heavyArmorPoints = value; /*if (BountyManager.instance.IsFightActive()) BountyManager.instance.combatManager.combatTransform.GetSlot(Row, Slot).InfoActive = true;*/ }
	}
	[fsIgnore]
	public List<string> TempCombatBuffs
	{
		get { return tempCombatBuffs; }
	}
	[fsIgnore]
	public List<TempAttributeBuff> TempCombatAttribBuffs
	{
		get { return tempCombatAttribBuffs; }
	}
	[fsIgnore]
	public BountyDamage CurrentAttackResult
	{
		get { return currentAttackResult; }
		set { currentAttackResult = value; }
	}
	public void AddTempCombatAttrib(TempAttributeBuff buff, bool updateActiveBuff)
	{
		TempAttributeBuff old = tempCombatAttribBuffs.Find(n => n.id == buff.id);
		if(old != null)
		{
			old.combats = buff.combats;
			old.duration = buff.duration;
		}
		else
		{
			tempCombatAttribBuffs.Add(buff);
		}

		//AttributeModifier am = tempCombatAttribBuffs.Find(n => n.attribute == attr.attribute);
		//if (am != null)
		//	am.fixedValue = Mathf.Max(attr.fixedValue, am.fixedValue);
		//else
		//{
		//	tempCombatAttribBuffs.Add(new AttributeModifier(attr.attribute, attr.fixedValue));
		//}

		if (updateActiveBuff)
		{
			RemoveBuff("Buff_Snacks");
			ApplyTempAttribBuff(true);
		}
	}
	public void ApplyTempAttribBuff(bool addImmediatly)
	{
		if (tempCombatAttribBuffs.Count > 0)
		{
			BountyDamage dmg = new BountyDamage();
			BountyBuff next = Instantiate<BountyBuff>(SDResources.Load<BountyBuff>("Buffs/Buff_Snacks"));
			BuffModule[] tModules = new BuffModule[tempCombatAttribBuffs.Count];
			List<AttributeModifier> modi = new List<AttributeModifier>();
			// dont stack attrib boni but search for the highest granted bonus
			foreach (var item in tempCombatAttribBuffs)
			{
				foreach (var attr in item.attribs)
				{
					AttributeModifier am = modi.Find(n => n.attribute == attr.attribute);
					if (am != null)
						am.fixedValue = Mathf.Max(attr.fixedValue, am.fixedValue);
					else
					{
						modi.Add(new AttributeModifier(attr.attribute, attr.fixedValue));
					}
				}
			}
			// apply found attributes to buff
			for (int i = 0; i < modi.Count; i++)
			{
				tModules[i] = next.modules[0].CopyClone();
				tModules[i].attribute = modi[i].attribute;
				tModules[i].effectValues[1] = modi[i].fixedValue;
			}
			next.modules = tModules;
			next.Caster = this;
			next.Target = this;
			if(addImmediatly)
			{
				if (AddBuff(next) >= 0)
					next.Evaluate(BuffModule.BuffModuleTrigger.OnCast, ref dmg, null);
			}
		}
	}
	public void TickTempAttribBuffCombats()
	{
		for (int i = tempCombatAttribBuffs.Count - 1; i >= 0; i--)
		{
			if(tempCombatAttribBuffs[i].combats > 0)
				tempCombatAttribBuffs[i].combats -= 1;
			if(tempCombatAttribBuffs[i].combats == 0)
			{
				tempCombatAttribBuffs.RemoveAt(i);
			}
		}
	}
	public void TickTempAttribBuffSessions()
	{
		for (int i = tempCombatAttribBuffs.Count - 1; i >= 0; i--)
		{
			if (tempCombatAttribBuffs[i].duration > 0)
				tempCombatAttribBuffs[i].duration -= 1;
			if (tempCombatAttribBuffs[i].duration == 0)
			{
				tempCombatAttribBuffs.RemoveAt(i);
			}
		}
	}

	public bool HasHeavyArmor()
	{
		if (allied && controllable) // for player side
		{
			BaseItem bi = GetEquippedItem(BaseItem.ItemType2.GearChest);
			return bi != null && bi.Tier == 3; // only heavy armor from faction counts as heavy armor
		}
		else
		{
			return GetAttribute(BountyCharAttribute.Armor) >= 30; //hasHeavyArmor; // changed to dynamic check 8.4.21
		}
	}

	public bool HasMovedThisTurn
	{
		get { return hasMovedThisTurn; }
		set { hasMovedThisTurn = value; }
	}



	#endregion

	#region perk related

	/// <summary>
	/// Returns True if this Character succeeds at conserving a Resource or at any other event in BountyPerkChanceTypes through a Perk
	/// For example Character has a 25% chance to not consume Water at the end of a day.
	/// Or Character has a 50% to be KO instead of dying.
	/// </summary>
	/// <param name="resourcetype"></param>
	/// <returns></returns>
	public bool PerkChance(BountyPerkChanceTypes resourcetype)
	{
		foreach (var perk in perks)
		{
			if (perk is BountyChancePerk chancePerk)
			{
				if (chancePerk.CheckIfSuccessful(resourcetype))
				{
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Returns a Characters Boost Perk value for a specific Boost type.
	/// A Perk may for example boost experience gained in jobs, hp regenerated in sleep or morale increased through drinking at a bar
	/// </summary>
	/// <param name="perkBoostType"></param>
	/// <returns>Returns 1.0, if the character does not have an applicable Perk. Returns a value larger than 1, if the character does have a Boost. E.g.: Returns 1.25 if the Character has a 25% Boost.</returns>
	public float PerkBoostFactor(PerkBoostType perkBoostType)
	{
		foreach (var perk in perks)
		{
			if (perk is BountyBoostFactorPerk regenerationPerk)
			{
				return regenerationPerk.GetBoostFactor(perkBoostType); // example values: 1.25 or 1.50
			}
		}
		return 1.0f; // no boost
	}

	#endregion
	
	/// <summary>
	/// fetches a summarized description string about an enemy char that contains all aktive skills it can use
	/// </summary>
	/// <returns></returns>
	public string CreateSkillListDescription(int max = 3)
	{
		string result = "";
		int c = 0;
		for (int i = 0; i < talentData.talents.Count && c < max; i++)
		{
			for (int j = 0; j < talentData.talents[i].tiers.Length && c < max; j++)
			{
				if (talentData.talents[i].tiers[j].grantSkill != null)
				{
					if (!talentData.talents[i].tiers[j].grantSkill.hiddenSkill && !talentData.talents[i].tiers[j].grantSkill.defaultAttack)
					{
						c++;
						if (!talentData.talents[i].tiers[j].grantSkill.altDescForEnemies)
							result += string.Format("-[li]\"{0}\": {1}[/li][br]", Localization.Get("Skill_" + talentData.talents[i].tiers[j].grantSkill.skillId), LocaTokenHelper.ParseSkillDescTags(Localization.Get("Skill_" + talentData.talents[i].tiers[j].grantSkill.skillId + "_desc"), talentData.talents[i].tiers[j].grantSkill, this));
						else
							result += string.Format("-[li]\"{0}\": {1}[/li][br]", Localization.Get("Skill_" + talentData.talents[i].tiers[j].grantSkill.skillId), LocaTokenHelper.ParseSkillDescTags(Localization.Get("Skill_" + talentData.talents[i].tiers[j].grantSkill.skillId + "_alt_desc"), talentData.talents[i].tiers[j].grantSkill, this));
					}
				}

			}
		}
		return result;
	}
	public List<BountySkill> CreateSkillList(int max = 5)
	{
		List<BountySkill> result = new List<BountySkill>();
		int c = 0;
		for (int i = 0; i < talentData.talents.Count && c < max; i++)
		{
			for (int j = 0; j < talentData.talents[i].tiers.Length && c < max; j++)
			{
				if (talentData.talents[i].tiers[j].grantSkill != null)
				{
					if (!talentData.talents[i].tiers[j].grantSkill.hiddenSkill && !talentData.talents[i].tiers[j].grantSkill.defaultAttack)
					{
						result.Add(talentData.talents[i].tiers[j].grantSkill);
						c++;
					}
				}

			}
		}
		return result;
	}

	// debug
	public void ResetAttributes()
	{
		//int points = 0;
		for (int i = 0; i < improvedAttributes.Count; i++)
		{
			//for (int j = improvedAttributes[i].value; j > 0; j--)
			//{
			//	points += GetAttributCost(j - 1, improvedAttributes[i].attribute == BountyCharAttribute.BlockChance || improvedAttributes[i].attribute == BountyCharAttribute.CritChance);
			//}
			ChangeAttributeRaw(improvedAttributes[i].attribute, -improvedAttributes[i].value);
			improvedAttributes[i].value = 0;
		}
		attributePoints = (level - 1) * 5;
	}

	#region inspector helper methods
#if UNITY_EDITOR


	//[ContextMenu("TestLevelUps")]
	private void TestRollLevelUps()
	{
		BountyCharacter bc = CopyClone();
		bc.healthPoints = 100;
		int levels = 30;
		string[] rowTitle = new string[] { "Level", "HP", "Endurance", "Strength", "Perception", "Might", "Armor", "Resistance" };
		BountyCharAttribute[] attribs = new BountyCharAttribute[] { BountyCharAttribute.Endurance, BountyCharAttribute.Strength, BountyCharAttribute.Perception, BountyCharAttribute.Intelligence, BountyCharAttribute.Armor, BountyCharAttribute.Resistance };
		//List<CharAttributeEntry> results = new List<CharAttributeEntry>();
		int[,] table = new int[levels, 8];

		for (int i = 0; i < levels; i++)
		{
			table[i, 0] = bc.Level;
			table[i, 1] = bc.GetMaxHealth();
			for (int j = 0; j < 6; j++)
			{
				table[i, 2 + j] = bc.GetAttributeRaw(attribs[j]);
			}
			bc.recentLevelUp = 1;
			bc.level += 1;
			bc.CheckLevelUp(true);
		}

		// print table
		StringBuilder sb = new StringBuilder();
		// sb.Append("-");
		// for(int i = 0; i < levels; i++)
		// {
		// 	sb.AppendFormat(",{0}", i+1);
		// }
		for (int j = 0; j < 8; j++)
		{
			sb.Append(rowTitle[j]);
			for (int i = 0; i < levels; i++)
			{
				sb.AppendFormat(",{0}", table[i, j]);
			}
			sb.AppendLine();
		}
		StreamWriter sw = new StreamWriter("levelUpTests.csv", false, System.Text.UnicodeEncoding.UTF8);
		sw.Write(sb.ToString());
		sw.Close();

		DestroyImmediate(bc);
	}

#endif
	#endregion

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	/// 
	/// 
	/// 
	/// 
	/// 
	/////////////////////////////////////////////////////// DA3 ////////////////////////////////////////////////////////////////

	private static Dictionary<CharacterBaseState, int> stateStressTable = new Dictionary<CharacterBaseState, int>()
	{
		{ CharacterBaseState.Happy, 60 },
		{ CharacterBaseState.Hungry, -20 },
		{ CharacterBaseState.Thirsty, -20 },
		{ CharacterBaseState.Moody, -20 },
		{ CharacterBaseState.Sick, -20 },
		{ CharacterBaseState.Injured, -20 },
	};

	[fsProperty]
	public int uniqueId;
	[fsProperty]
	public bool proceduralCharacter;
	[fsProperty]
	public int proceduralModel = -1;
	[fsProperty]
	public bool commander; // troop commander
	[fsIgnore]
	public CharacterCombatType combatType = 0;
	[fsProperty][HideInInspector]
	public int morale; // 0-100 for morale value
	[fsProperty][HideInInspector]
	public int stress; // 0-100 for stress value
	[SerializeField][fsProperty]
	public List<CharSkillEntry> skills;
	[fsIgnore]
	private List<CharSkillEntry> tempSkills;
	[fsIgnore]
	public List<StartCharSkillEntry> startSkills;
	[fsProperty]
	public int price;
	[fsProperty]
	public SurvivorType survivorType; //None, Normal, Worker, Soldier, Slave
	[fsProperty]
	public List<CharacterNavState> navStates = new List<CharacterNavState>();
	[fsProperty]
	public CharacterCreationInfo charCreationInfo; // maybe only debug info, saves the creation context for the char instance
	[fsProperty]
	public CharacterClass charClass;
	[fsProperty]
	public int rank;
	[fsProperty]
	public CharacterMoralState moraleState;


	[fsIgnore]
	public int RawMorale // -1, 0, 1 for unhappy neutral or happy
	{
		get {
			if (morale >= 70)
				return 1;
			if (morale < 30)
				return -1;
			return 0;
		}
		//set { morale = Mathf.Clamp(value, -1, 1); }
	}
	[fsIgnore]
	public int Morale
	{
		get { return morale + moraleState.GetMoralSum(); }
		set { morale = Mathf.Clamp(value, 0, 100); }
	}
	[fsIgnore]
	public int Stress
	{
		get { return stress; }
		set { stress = Mathf.Clamp(value, 0, 100); }
	}
	public int AddStress(int amount)
	{
		Stress += amount; // modifikator durch perk später
		return amount;
	}

	[fsIgnore]
	[SerializeField]
	protected BountyCombatant combatant;
	public BountyCombatant Combatant
	{
		get { return combatant; }
		private set { combatant = value; }
	}
	/// <summary>
	/// sets up the realtime combat logic for this char
	/// </summary>
	/// <param name="manager"></param>
	/// <returns></returns>
	public bool SetupCombatant(BountyCombatManager manager)
	{

		if (Combatant == null)
			Combatant = new BountyCombatant();


		if (Combatant.initialized)
			return false;


		Combatant.Start(this, manager);
		return true;
	}


	public BountySkill[] GetCombatSkills()
	{
		List<BountySkill> result = new List<BountySkill>();
		BountyTalentType talent = LastTalent;
		int c = talentData.talents.Count;
		for (int i = 0; i < c; i++)
		{
			if (talentData.talents[i].type == talent)
			{
				result.Add(talentData.talents[i].tiers[0].grantSkill);
				//result.Add(talentData.talents[i].troopCombatSkill);
				break;
			}

		}
		
		return result.ToArray();
	}
	/// <summary>
	/// triggers an attack frame command to the combat functionality
	/// </summary>
	public void AttackFrame()
	{
		if(Combatant != null)
			Combatant.AttackFrame();
	}

	public float GetBuffSpeedModifier()
	{
		int factor = 100;
		foreach (var buff in activeBuffs)
		{
			if (buff.addSpeedModifiier != 0)
				factor += buff.addSpeedModifiier;
		}

		return ((float)factor / 100f);
	}

	public float GetRealtimeSkillCooldown(BountySkill skill)
	{
		if (skillCooldowns.ContainsKey(skill))
		{
			return Mathf.Min(skillCooldowns[skill], (float)skill.cooldown);
		}
		else
		{
			return 0;
		}
	}

	public float GetDisplayedRealtimeSkillCooldown(BountySkill skill)
	{
		if (skillCooldowns.ContainsKey(skill))
		{
			return Mathf.Min(skillCooldowns[skill], (float)skill.cooldown);
		}
		else
		{
			return 0;
		}
	}

	public void UpdateRealtimeSkillCooldowns(float deltaTime)
	{
		List<BountySkill> list = new List<BountySkill>(skillCooldowns.Keys);
		for (int i = 0; i < list.Count; i++)
		{
			if (skillCooldowns[list[i]] > 0f && !activeMinions.Exists(n => n.freezeSummonerSkill == list[i]))
			{
				skillCooldowns[list[i]] -= deltaTime;
			}
		}
	}
	public BountySkill GetActiveSkill(string pSkill)
	{
		int c = talentData.talents.Count;
		for (int i = 0; i < c; i++)
		{
			for (int j = 0; j < talentData.talents[i].tiers.Length; j++)
			{
				if (talentData.talents[i].tiers[j].grantSkill != null && talentData.talents[i].tiers[j].grantSkill.skillId == pSkill)
				{
					return talentData.talents[i].tiers[j].grantSkill;
				}
			}
		}
		return null;
	}
	public BountyPassiveSkill GetPassiveSkill(string pSkill)
	{
		int c = talentData.talents.Count;
		for (int i = 0; i < c; i++)
		{
			for (int j = 0; j < talentData.talents[i].tiers.Length; j++)
			{
				if (talentData.talents[i].tiers[j].grantPassiveSkill != null && talentData.talents[i].tiers[j].grantPassiveSkill.skillId == pSkill)
				{
					return talentData.talents[i].tiers[j].grantPassiveSkill;
				}
			}
		}
		return null;
	}
	
	public int GetSkillLevel(string skill, bool useSkillModifier = false)
	{
		int c = skills.Count;
		int result = 0;
		for (int i = 0; i < c; i++)
		{
			if (skills[i].skill == skill)
			{
				result = skills[i].value;
				break;
			}
		}
		if(useSkillModifier)
		{
			List<BaseItem> tList = GetCompleteEqupment();
			foreach (var item in tList)
			{
				if(item.skillMods.Count > 0)
				{
					for (int i = 0; i < item.skillMods.Count; i++)
					{
						if (item.skillMods[i].skill == skill)
							result += item.skillMods[i].fixedValue;
					}
				}
			}
		}
		return result;
	}
	public void ChangeSkillLevel(string pSkill, int value = 1, bool fixedValue = false, bool tempMode = true)
	{
		List<CharSkillEntry> list = skills;
		int c = list.Count;
		bool found = false;
		int resultLevel = 0;
		bool exists = false;
		foreach (var tTal in talentData.talents)
		{
			foreach (var tTier in tTal.tiers)
			{
				if((tTier.grantSkill != null && tTier.grantSkill.skillId == pSkill) || (tTier.grantPassiveSkill != null && tTier.grantPassiveSkill.skillId == pSkill))
				{
					exists = true;
					break;
				}
				if (exists)
					break;
			}
		}
		for (int i = 0; i < c; i++)
		{
			if (list[i].skill == pSkill)
			{
				if (fixedValue)
					list[i].value = value;
				else
					list[i].value += value;
				found = true;
				resultLevel = list[i].value;
                
                break;
			}

		}
		if (!found && exists)
		{
			list.Add(new CharSkillEntry(pSkill, value));
			resultLevel = value;
		}

		if (!tempMode)
			return;

		talentPoints -= resultLevel;
		tempTalentPoints += resultLevel;

		list = tempSkills;
		c = list.Count;
		found = false;
		for (int i = 0; i < c; i++)
		{
			if (list[i].skill == pSkill)
			{
				if (fixedValue)
					list[i].value = value;
				else
					list[i].value += value;
				found = true;
				break;
			}
		}
		if (!found && exists)
		{
			list.Add(new CharSkillEntry(pSkill, value));
		}
	}
	public void ApplySkills()
	{
		tempTalentPoints = 0;
		tempSkills.Clear();

		// count achievemnt progress
		//if (BountyManager.instance.CurrentTutorialIndex < 0)
		//{
		//	int c = talentData.talents.Count;
		//	int level = 0;
		//	int result1 = 0;
		//	int result2 = 0;
		//	for (int i = 0; i < c; i++)
		//	{
		//		if (!talentData.talents[i].passive)
		//		{
		//			result1 = 0;
		//			result2 = 0;
		//			level = GetTalentLevel(talentData.talents[i].type);
		//			for (int j = 0; j < talentData.talents[i].tiers.Length; j++)
		//			{
		//				if (talentData.talents[i].tiers[j].level <= level)
		//				{
		//					if (talentData.talents[i].tiers[j].grantSkill.staminaNeeded <= 2)
		//						result1 += 1;
		//					else if (talentData.talents[i].tiers[j].grantSkill.staminaNeeded == 3)
		//						result2 += 1;
		//				}
		//			}
		//			BountyManager.instance.persistentManager.ChangeCurrentStat((BountyPersistentStat)(int)talentData.talents[i].type + 8000, result1, true);
		//			BountyManager.instance.persistentManager.ChangeCurrentStat((BountyPersistentStat)(int)talentData.talents[i].type + 9000, result2, true);
		//		}
		//	}
		//}
	}
	public void RevertSkills()
	{
		talentPoints += tempTalentPoints;
		tempTalentPoints = 0;
		for (int i = tempSkills.Count - 1; i >= 0; i--)
		{
			ChangeSkillLevel(tempSkills[i].skill, -tempSkills[i].value, false, false);
		}
		tempSkills.Clear();
	}
	/// <summary>
	/// checks if a given skill can be leveled up
	/// </summary>
	/// <param name="pSkill">the skill id</param>
	/// <returns>true if it can be leveled up</returns>
	public bool CanLevelUpSkill(string pSkill) //check skill requirement
	{
		int maxLvl = 0;
		SkillLevelRequirement tReq = null;
		int tLvl = GetSkillLevel(pSkill);
		int c = talentData.talents.Count;
		for (int i = 0; i < c; i++)
		{
			for (int j = 0; j < talentData.talents[i].tiers.Length; j++)
			{
				if (talentData.talents[i].tiers[j].grantSkill != null && talentData.talents[i].tiers[j].grantSkill.skillId == pSkill)
				{
					maxLvl = talentData.talents[i].tiers[j].grantSkill.maxLvl;
					if(talentData.talents[i].tiers[j].grantSkill.levelRequirements.Length > tLvl)
						tReq = talentData.talents[i].tiers[j].grantSkill.levelRequirements[tLvl];
				}
				else if(talentData.talents[i].tiers[j].grantPassiveSkill != null && talentData.talents[i].tiers[j].grantPassiveSkill.skillId == pSkill)
				{
					maxLvl = talentData.talents[i].tiers[j].grantPassiveSkill.maxLvl;
					if (talentData.talents[i].tiers[j].grantPassiveSkill.levels.Length > tLvl)
						tReq = talentData.talents[i].tiers[j].grantPassiveSkill.levels[tLvl].requirements;
				}
			}
		}
		if (tLvl >= maxLvl)
			return false;

		if (tReq != null)
		{
			// check attrbiutes
			foreach (var item in tReq.attributes)
			{
				if (GetAttributeRawWithPerk(item.attribute) < item.value)
					return false;
			}
		}
		return true;
	}

	/// <summary>
	/// returns true if a nav state with given name has been saved
	/// </summary>
	/// <param name="pId"></param>
	/// <returns></returns>
	public bool HasNavState(string pId)
	{
		foreach (var st in navStates)
		{
			if (st.name == pId)
				return true;
		}
		return false;
	}
	/// <summary>
	/// returns a saved nav state with given name or null if not found
	/// </summary>
	/// <param name="pId"></param>
	/// <returns></returns>
	public CharacterNavState GetNavState(string pId)
	{
		foreach (var st in navStates)
		{
			if (st.name == pId)
				return st;
		}
		return null;
	}
	/// <summary>
	/// adds a nav state to the saved list
	/// </summary>
	/// <param name="pState"></param>
	public void AddNavState(CharacterNavState pState)
	{
		navStates.Add(pState);
	}
	/// <summary>
	/// removes a nav state from the saved list
	/// </summary>
	/// <param name="pId"></param>
	public void RemoveNavState(string pId)
	{
		for (int i = navStates.Count - 1; i >= 0; i--)
		{
			if (navStates[i].name == pId)
				navStates.RemoveAt(i);
		}
	}

	public bool IsNaked()
	{
		if(BountyManager.instance == null || BountyManager.instance.camp == null)
			return false;
		JobInstance ji = BountyManager.instance.camp.GetActiveJob(this);
		if (ji != null)
			return ji.type == CampRoomType.Recreation && ji.cat != 0;
		else
			return false;
	}

	public void FedMeal(BaseItem.ItemType2 type, int level)
	{
		if(type == moraleState.desiredMeal.itemType && level == moraleState.desiredMeal.level)
		{
			if(!moraleState.moraleEntries.Exists(n => n.id == "MealRequestSucess"))
				moraleState.moraleEntries.Add(new MoralStateEntry("MealRequestSucess", moraleState.mealDateTime + 15, 20)); // add request success state
		}
	}
}

// important for animations, but other stuff too
public enum BountyRace
{
	Human,
	Zombie,
	Wolf,
	Turret,
	FarmAnimal,
}

// bestimmt evtl nur grafische darstellung der HP leiste, nicht benutzt bisher
public enum DefenseStyle
{
	Normal,
	Armored,
	Resistant,
}

public enum BountyCharAttribute
{
	// system
	None = 0,
	Health = 1,
	Exp = 2,
	Level = 3,
	// base - these are stored
	Strength = 10,
	Intelligence = 11, // called Power ingame
	Endurance = 12,
	Reflexes = 13, // used for movement speed multiplacator 
	Perception = 14,
	Armor = 15,
	Resistance = 16,
	// special attributes - these are either calculated, aquired by equip, temporal effect or just graphical
	CritChance = 20,
	BlockChance = 21,
	DodgeChance = 22, // used for changing ranged hit chance modifier for a target
	RangedHitChance = 24, // used for changing ranged hit chance modifier for an attacker
	CritVulnurability = 25, // used for changing crit chance modifier for target
							// damage - these are calculated
	MeleeDamage = 30,
	RangedDamage = 31,
	SpecialDamage = 32, // power damage
	SpecialRangedDamage = 33, // power damage for ranged attacks with piercing ammo
}

[System.Serializable]
[fsObject]
public class CharacterStateEntry
{
	public CharacterStateEntry(CharacterBaseState pState, int pStart, int pDur)
	{
		state = pState;
		duration = pDur;
		startTime = pStart;
	}

	[fsProperty]
	public CharacterBaseState state;
	[fsProperty]
	public int duration = -1; // format 2 * day + session
	[fsProperty]
	public int startTime; // format 2 * day + session

	public CharacterStateEntry() { }

	public int GetLethalDate()
	{
		if (BountyCamp.statDeathTable.ContainsKey(state))
			return startTime + BountyCamp.statDeathTable[state];
		else
			return 0;
	}
}

// bestimmt eine staus ursache eines characters für seine moral
public enum CharacterBaseState
{
	None = 0,
	Hungry = 1,
	Thirsty = 2,
	Infected = 3,
	Moody = 4,
	Happy = 5,
	Sick = 6,
	Injured = 7,
	Imprisoned = 8, // only used by story purpose?
}
// kampfstatus eines kämpfers
public enum CharacterCombatState
{
	None = 0,
	Stunned = 1, // benommen/betäubt
	KnockedOut = 2, // ko / bewusstlos
	Injured = 3, // angeschlagen
	Scared = 4, // ängstlich
	Vulnerable = 5, // anfällig, aber keine ani changes
	Guarding = 6, // veränderte waffen idle
	InCover = 7,
}

[System.Serializable]
[fsObject]
public class CharAttributeEntry
{
	public CharAttributeEntry(BountyCharAttribute pAtt, int pValue)
	{
		attribute = pAtt;
		value = pValue;
	}

	public CharAttributeEntry() { }

	[fsProperty]
	public BountyCharAttribute attribute;
	[fsProperty]
	public int value;
}

[System.Serializable]
[fsObject]
public class CharTalentEntry
{
	public CharTalentEntry(BountyTalentType pTa, int pValue)
	{
		talent = pTa;
		value = pValue;
	}

	public CharTalentEntry() { }

	[fsProperty]
	public BountyTalentType talent;
	[fsProperty]
	public int value;
}

[System.Serializable]
[fsObject]
public class CharSkillEntry
{
	public CharSkillEntry(string pSkill, int pValue)
	{
		skill = pSkill;
		value = pValue;
	}

	public CharSkillEntry() { }

	[fsProperty]
	public string skill;
	[fsProperty]
	public int value;
}

[System.Serializable]
public class StartCharSkillEntry
{

	public BountySkill skill;
	public BountyPassiveSkill skillPassive;
	public int value;
}

/// <summary>
/// stores information about a talent a character has
/// </summary>
[System.Serializable]
public class BountyTalentStructure
{
	public BountyTalentType type;
	public bool passive;
	public bool levelInfo; // has different description for every level
	[HideInInspector]
	public int maxLvl = 11; // not used for DAS
	[HideInInspector]
	public BountyCharAttribute increasingAttribute; // not used for DAS
	[HideInInspector]
	public int attributePerLevel = 1; // not used for DAS
	public BountySkill troopCombatSkill; // the skill provided as a second skill in troop combat
	public BountyTalentTier[] tiers;
}

public enum BountyTalentType
{
	// general
	None = 0,
	Ignored = 1,
	Always = 2,
	Commander = 3, // commander skills

	// passive talents
	Talent_Hunting = 101, // active talent for hunter skills
	Talent_Guarding = 102, // NO actual talent and not used
	Talent_Medical = 103, // NO actual talent and not used
	Talent_Crafting = 104, // camp jobs skills
	Talent_Farming = 105, // NO actual talent and not used
	Talent_Scouting = 106, // NO actual talent and not used
	Talent_Survival = 107, //  NO actual talent and not used
	Talent_Threaten = 108, // dialog skill drohen, just used as identifier
	Talent_Bargain = 109, // dialog skill verhandeln, just used as identifier
	Talent_Deceive = 110, // dialog skill lügen/überzeugen, just used as identifier
	Talent_Diplomacy = 111, // passive talent for the 3 dialogue skills
	Talent_Industry = 112, // Tool jobs skills
	//active talents
	Talent_Melee = 201, // stumpfe waffen
	Talent_Pistol = 202,
	Talent_Shotgun = 203,
	Talent_Rifle = 204,
	Talent_Grenadier = 205, // techniker
	Talent_MachinePistol = 206,
	Talent_Swords = 207,
	Talent_Fists = 208, // martial arts
	Talent_ZombieMelee = 209,
	Talent_ZombieRanged = 210,
	Talent_Axe = 211,
	// save slot upgrades
	Upgrade_CloseCombat = 301,
	Upgrade_RangedCombat = 302,
	Upgrade_Engineer = 303,
	Upgrade_Medical = 304,
}

[System.Serializable]
public class BountyTalentTier
{
	public int level; // not used for DAS
	public BountySkill grantSkill;
	public BountyPassiveSkill grantPassiveSkill;
}


/// <summary>
/// character effects are played when something in combat happens, like: being hit
/// </summary>
[System.Serializable]
public class CharacterEffectEvent
{
	public CharacterEffectEventType type;
	public AudioClip[] sound;
	public GameObject[] visual;
	//public bool loop;
}

public enum CharacterEffectEventType
{
	Idle = 0,
	Hit = 1,
	Block = 2,
	Crit = 3,
	Die = 4,
	VoiceHit = 5,
	VoiceBlock = 6,
	VoiceCrit = 7,
	VoiceCry = 8,
	VoiceThink = 9,
	AttackRoar = 10,

	Hit_Shot = 11,
	Hit_Shot_Graze = 12,
	Hit_Shot_Crit = 13,
	Hit_Shot_Big = 14,
	Hit_Shot_Big_Crit = 15,
	Hit_Slash = 16,
	Hit_Slash_Block = 17,
	Hit_Slash_Crit = 18,
	Hit_Slash_Big = 19,
	Hit_Slash_Big_Crit = 20,
	Hit_Armor = 21,

	// not used actually?
	DeathEffect_Fire = 34,
	DeathEffect_Explosion = 35,
	DeathEffect_Wound = 36,
	DeathEffect_Poison = 37,
	DeathEffect_KO = 38,

	Hit_Knockback = 40,
	Hit_Fire = 44,
	Hit_Wound = 46,
	Hit_Poison = 47,
	Hit_KO = 48,

	Walking = 51,
	Running = 52,
	Crouching = 53,

	AttackSound = 60,
	AttackSpecial = 61,
	BuffSelf = 62,
	KO_In = 63,
	KO_Out = 64,
	VoiceDie = 65,
	SpecialEffect = 70, // used for eg. zombie eating ani in loot events
}

public enum SpecialDamageAni
{
	None = 0,
	Fire = 4,
	Explosion = 5,
	Wound = 6,
	Poison = 7,
}

public enum CombatSide
{
	None = 0,
	Player = 1,
	Allied = 2,
	Enemy = 3,
}

[System.Flags]
public enum CharacterCommandRank
{
	None = 0,
	PartyMember = 1,
	FactionLeader = 2,
	TroopLeader = 4,
	BaseLeader = 8,
}

[System.Flags]
public enum CharacterCombatType
{
	None = 0,
	Melee = 1,
	Ranged = 2,
	Elite = 4,
	Nimble = 8,
	Heavy = 16,
}

/// <summary>
/// character ausrüstungs slot
/// </summary>
[System.Serializable]
[fsObject]
public class CharacterEquipmentSlot
{
	[fsProperty]
	public BaseItem.ItemType2 type; // stores the subtype (aka gear type)
	[fsProperty]
	public BaseItem item;
	[fsProperty]
	public bool blocked = false;

	public CharacterEquipmentSlot() { }

	public CharacterEquipmentSlot(BaseItem.ItemType2 pType, BaseItem pItem, bool pBlocked)
	{
		type = pType;
		item = pItem;
		blocked = pBlocked;
	}
}

[System.Serializable]
public class SDFXEntry
{
	public GameObject particle;
	public AudioClip sound;
	public AudioClip soundAlternative; // used for playing a female version of a sound
									   //public BountyBoneType bonePoint;

	//public SDFXEntry(GameObject pParticle, AudioClip pSound)
	//{
	//	particle = pParticle;
	//	sound = pSound;
	//}
	public SDFXEntry GetCopy()
	{
		return new SDFXEntry() { particle = particle, sound = sound, soundAlternative = soundAlternative };
	}
}

[System.Serializable]
[fsObject]
public class BountyPortrait
{
	[fsIgnore]
	public Sprite simple;
	[fsIgnore]
	public bool animated;
	[fsProperty]
	public string[] layers;
	[fsProperty]
	public string simpleReference;
	[fsIgnore]
	public bool hasClothingLayer;
	[fsIgnore]
	public bool isZoomable;

	public BountyPortrait() { }

	public BountyPortrait CopyClone()
	{
		BountyPortrait result = new BountyPortrait();
		result.simple = simple;
		result.animated = animated;
		result.layers = new string[layers.Length];
		System.Array.Copy(layers, result.layers, layers.Length);
		result.simpleReference = simpleReference;
		result.hasClothingLayer = hasClothingLayer;
		result.isZoomable = isZoomable;
		return result;
	}
}

[System.Serializable]
[fsObject]
public class CharacterNavState
{
	[fsProperty]
	public string name;
	[fsProperty]
	public BaseNavNode.NodeType startNodeType;
	[fsProperty]
	public BaseNavNode.StationType startNodeStation;
	[fsProperty]
	public int startNavMode;
	[fsProperty]
	public int startNodeIndex;

	public CharacterNavState() { }
}

[System.Serializable]
[fsObject]
public class CharacterCreationInfo
{
	[fsProperty]
	public string info; // textual info about the creation context
	[fsProperty]
	public int instanceId; // quest/events instance, if given
	[fsProperty]
	public int date; // ingame date

	public CharacterCreationInfo() { }

	public CharacterCreationInfo(string pInfo, int pInstance = -1)
	{
		info = pInfo;
		instanceId = pInstance;
		date = BountyManager.instance.DateTime;
	}
}

[System.Serializable]
[fsObject]
public class TempAttributeBuff
{
	[fsProperty]
	public string id; // identifies the source of the buff
	[fsProperty]
	public List<AttributeModifier> attribs; // attributes to increase
	[fsProperty]
	public int combats; // remaining combat counts
	[fsProperty]
	public int duration; // remaining sessions

	public TempAttributeBuff() { }

	public TempAttributeBuff(string pId, List<AttributeModifier> pAttribs, int pCombats, int pDuration)
	{
		id = pId;
		attribs = pAttribs;
		combats = pCombats;
		duration = pDuration;
	}
}

[System.Serializable]
[fsObject]
public class CharacterMoralState // sammelobjekt für den gesamten moral-zustand eines chars
{

	[fsProperty]
	public BaseItemDefinition desiredMeal; // food item typ that is requested
	[fsProperty]
	public int mealDateTime; // time the last meal was requested
	[fsProperty]
	public List<MoralStateEntry> moraleEntries;
	//[fsProperty]
	//public List<MoralStateEntry> stressEntries;
	[fsProperty]
	public bool wasInAction;

	public CharacterMoralState() {
		moraleEntries = new List<MoralStateEntry>();
		//stressEntries = new List<MoralStateEntry>();
	}

	public int GetMoralSum()
	{
		int result = 0;
		foreach (var item in moraleEntries)
		{
			result += item.value;
		}
		return result;
	}

	public int GetMoralSum(string id)
	{
		int result = 0;
		foreach (var item in moraleEntries)
		{
			if (item.id == id)
				result += item.value;
		}
		return result;
	}

	public void SetNewMealRequest()
	{
		mealDateTime = BountyManager.instance.DateTime;
		int set = SDRandom.Range(0, 2);
		if(set == 0)
		{
			List<int> choices = new List<int>();
			for (int i = 1; i < 13; i++)
			{
				if(desiredMeal == null || desiredMeal.level != i || desiredMeal.itemType == BaseItem.ItemType2.ConsumableDrink)
					choices.Add(i);
			}
			desiredMeal = new BaseItemDefinition() { itemType = BaseItem.ItemType2.ConsumableFood, level = choices[SDRandom.Range(0, choices.Count)] };
		}
		else
		{
			List<int> choices = new List<int>();
			for (int i = 1; i < 5; i++)
			{
				if (desiredMeal == null || desiredMeal.level != i || desiredMeal.itemType == BaseItem.ItemType2.ConsumableFood)
					choices.Add(i);
			}
			desiredMeal = new BaseItemDefinition() { itemType = BaseItem.ItemType2.ConsumableDrink, level = choices[SDRandom.Range(0, choices.Count)] };
		}
	}

}

[System.Serializable]
[fsObject]
public class MoralStateEntry
{

	[fsProperty]
	public string id; // id / name of moral relevant status event
	[fsProperty]
	public int dateTime; // date time it will expire OR -1 when endless
	[fsProperty]
	public int value; // morale state change value

	public MoralStateEntry() { }

	public MoralStateEntry(string id, int dateTime, int value)
	{
		this.id = id;
		this.dateTime = dateTime;
		this.value = value;
	}
}

public enum CharacterClass
{
	None = 0,
	Krieger = 1, // stumpfe waffe
	Waechter = 2, // axt und schild
	Vollstrecker = 3, // klingen waffe
	MartialArtist = 4, // fists

	Frontkaempfer = 10, // shotgun
	Todeskommando = 11, // smg
	Schuetze = 12, // rifle
	Pistolero = 13, // pistol

	Jaeger = 20, // jäger
	Techniker = 21, // techniker

	Survival = 30,
	Handwerk = 31,
	Forschung = 32,
	Industrie = 33,
}

public enum CharacterPerk // do we use an enum? or will we use assets like with skills and buffs?
{ // we use assets
	None = 0,
	Athletisch = 1,
	Super_Athletisch = 2,
	Nahkaempfer = 3,
	Super_Nahkaempfer = 4,
	Fernkaempfer = 5,
	Super_Fernkaempfer = 6,
	Macht = 7,
	Super_Macht = 8,
	Stahl = 9,
	Super_Stahl = 10,
	Resistent = 11,
	Super_Resistent = 12,
	Assassine = 13,
	Super_Assassine = 14,
	Blockmeister = 15,
	Super_Blockmeister = 16,
	Tagmensch = 17,
	Super_Tagmensch = 18,
	Nachtmensch = 19,
	Super_Nachtmensch = 20,
	Zombiehasser = 21,
	Super_Zombiehasser = 22,
	Frauenhasser = 23,
	Super_Frauenhasser = 24,
	Männerhasser = 25,
	Super_Männerhasser = 26,
	SchnellerLerner = 27,
	Super_SchnellerLerner = 28,
	GuterSchlaf = 29,
	Super_GuterSchlaf = 30,
	GuterTrinker = 31,
	Super_GuterTrinker = 32,
	EntspanntePerson = 33,
	Super_entspanntePerson = 34,
	Schwerzutoeten = 35,
	Super_Schwerzutoeten = 36,
	GeringerHunger = 37,
	Super_GeringerHunger = 38,
	GeringerDurst = 39,
	Super_GeringerDurst = 40,
	Munitionsparend = 41,
	Super_Munitionsparend = 42,
	Medizinsparend = 43,
	Super_Medizinsparend = 44,
}

