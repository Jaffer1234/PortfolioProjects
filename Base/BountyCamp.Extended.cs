using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FullSerializer;
using System;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class BountyCamp
{

	[fsProperty]
	[SerializeField]
	[HideInInspector]
	public int idCountItems = 0;
	[fsProperty]
	[SerializeField]
	[HideInInspector]
	public List<BaseItem> globalItems = new List<BaseItem>();

	[fsProperty]
	[SerializeField]
	[HideInInspector]
	public int idCountChars = 0;
	[fsProperty]
	[SerializeField]
	[HideInInspector]
	public List<BountyCharacter> globalCharacters = new List<BountyCharacter>();

	[fsProperty]
	[SerializeField]
	[HideInInspector]
	public int idCountInventories = 0;
	[fsProperty]
	[SerializeField]
	[HideInInspector]
	public List<LocalInventory> globalInventories = new List<LocalInventory>();

	[fsProperty]
	private int femaleRandomNameCounter;
	[fsProperty]
	private int maleRandomNameCounter;
	[fsProperty]
	[SerializeField]
	[HideInInspector]
	public List<BountyCharacter> pendingJoiningSurvivors = new List<BountyCharacter>();
	[fsIgnore]
	private List<BountyCharacter> tempSurvivors = new List<BountyCharacter>();
	[SerializeField]
	[fsIgnore]
	private BountyCharacter[] survivorTemplates; // used for spawning new random survivors
	[fsIgnore] public int survivorSpawnWithPerkChance = 5;
	[fsIgnore] public List<BountyPerk> spawnablePerks;
	[fsIgnore]
	[ListSelection("Dialogue", true, true)]
	public int prisonDialogue;
	[fsIgnore]
	[SerializeField]
	private List<TrainingClassEntry> trainingEntries;


	//[fsIgnore]
	//[SerializeField]
	//private bool debugUnlockAllRecipes;
	[fsProperty]
	[SerializeField]
	[ListSelection("Recipe",true,true)]
	private List<int> knownRecipes;

	[fsIgnore]
	protected List<BountyCharacter> BaseInhabitants
	{
		get { return BountyManager.instance.factionManager.GetFactionData(Faction.Player).bases[0].members; }
		set { BountyManager.instance.factionManager.GetFactionData(Faction.Player).bases[0].members = value; }
	}



	public void OnLoadStepExtended(Dictionary<BountyCharacter, BountyCharacter> loadedList)
	{
		//Debug.LogError("OnLoadSetupExtended + ForCharacterIns");
		//Dictionary<BountyCharacter, BountyCharacter> loadedList = new Dictionary<BountyCharacter, BountyCharacter>(); // tracking who is already loaded so we can re-map the instances
		BountyCharacter tBc;
		for (int i = 0; i < inhabitants.Count; i++)
		{
			tBc = inhabitants[i];
			inhabitants[i] = Instantiate<BountyCharacter>(SDResources.Load<BountyCharacter>("Character/" + inhabitants[i].characterId));
			loadedList.Add(tBc, inhabitants[i]);
		}
		for (int i = 0; i < party.Count; i++)
		{
			tBc = party[i];
			party[i] = Instantiate<BountyCharacter>(SDResources.Load<BountyCharacter>("Character/" + party[i].characterId));
			loadedList.Add(tBc, party[i]);
		}
		for (int i = 0; i < dead.Count; i++)
		{
			tBc = dead[i];
			dead[i] = Instantiate<BountyCharacter>(SDResources.Load<BountyCharacter>("Character/" + dead[i].characterId));
			loadedList.Add(tBc, dead[i]);
		}
		for (int i = 0; i < away.Count; i++)
		{
			tBc = away[i];
			away[i] = Instantiate<BountyCharacter>(SDResources.Load<BountyCharacter>("Character/" + away[i].characterId));
			loadedList.Add(tBc, away[i]);
		}
		for (int i = 0; i < pendingJoiningSurvivors.Count; i++)
		{
			tBc = pendingJoiningSurvivors[i];
			pendingJoiningSurvivors[i] = Instantiate<BountyCharacter>(SDResources.Load<BountyCharacter>("Character/" + pendingJoiningSurvivors[i].characterId));
			loadedList.Add(tBc, pendingJoiningSurvivors[i]);
		}

		
	}
	public void OnLoadedExtended(int fileVersion, Dictionary<BountyCharacter, BountyCharacter> loadedList)
	{
		for (int i = 0; i < inhabitants.Count; i++)
		{
			inhabitants[i].OnLoaded(fileVersion);
		}
		for (int i = 0; i < party.Count; i++)
		{
			party[i].OnLoaded(fileVersion);
		}

		if (resourceNegativeQueue == null)
			resourceNegativeQueue = new int[9];
		if (resourcePositiveQueue == null)
			resourcePositiveQueue = new int[9];
		if (playerBackedResorces == null || playerBackedResorces.Length != 9)
			playerBackedResorces = new int[9];

		if (recruitedReplacementChars == null)
			recruitedReplacementChars = new List<string>();

		if (backedStorage == null)
			backedStorage = new List<BaseItem>();

		for (int i = 0; i < away.Count; i++)
		{
			away[i].OnLoaded(fileVersion);
		}
		// DA3 stuff
		for (int i = 0; i < pendingJoiningSurvivors.Count; i++)
		{
			pendingJoiningSurvivors[i].OnLoaded(fileVersion);
		}
		for (int i = 0; i < globalCharacters.Count; i++)
		{
			if (!loadedList.ContainsKey(globalCharacters[i]))
				globalCharacters[i].OnLoaded(fileVersion);
		}
	}
	public void RemapGlobalCharListAfterLoad(Dictionary<BountyCharacter, BountyCharacter> loadedList)
	{
		// now remap the pointers in registry
		for (int i = 0; i < globalCharacters.Count; i++)
		{
			if (loadedList.ContainsKey(globalCharacters[i]))
			{
				var tPoint = globalCharacters[i];
				globalCharacters[i] = loadedList[globalCharacters[i]];
				Destroy(tPoint);
			}
			else
			{
				globalCharacters[i] = Instantiate<BountyCharacter>(SDResources.Load<BountyCharacter>("Character/" + globalCharacters[i].characterId));
			}
		}
		// now remap the pointers in registry
		//for (int i = 0; i < activeJobs.Count; i++)
		//{
		//	if (loadedList.ContainsKey(activeJobs[i].character))
		//	{
		//		var tPoint = activeJobs[i].character;
		//		activeJobs[i].character = loadedList[activeJobs[i].character];
		//		Destroy(tPoint);
		//	}
		//	else
		//	{
		//		activeJobs[i].character = Instantiate<BountyCharacter>(SDResources.Load<BountyCharacter>("Character/" + activeJobs[i].character.characterId));
		//	}
		//}
	}


	public int RegisterItem(BaseItem bi)
	{
		if(globalItems.Contains(bi))
		{
			return -1;
		}

		globalItems.Add(bi);
		bi.uniqueId = idCountItems++;
		return bi.uniqueId;
	}
	public void UnregistierItem(BaseItem bi)
	{
		globalItems.Remove(bi);
	}
	public BaseItem GetUniqueItem(int pId)
	{
		foreach (var item in globalItems)
		{
			if (item.uniqueId == pId)
				return item;
		}
		return null;
	}


	public int RegisterCharacter(BountyCharacter bi)
	{
		if (globalCharacters.Contains(bi))
		{
			return bi.uniqueId; // should not destroy the character's id if they get registered twice.
			//return -1; 
		}
		globalCharacters.Add(bi);
		bi.uniqueId = idCountChars++;
		return bi.uniqueId;
	}
	public void UnregisterCharacter(BountyCharacter bi)
	{
		if (globalCharacters.Contains(bi))
			globalCharacters.Remove(bi);
	}
	
	/// <summary>
	/// Returns BountyCharacter associated with uniqueId. Returns null for pId = -1. 
	/// </summary>
	/// <param name="pId"></param>
	/// <returns></returns>
	public BountyCharacter GetUniqueCharacter(int pId)
	{
		if (pId >= 0)
		{
			foreach (var item in globalCharacters)
			{
				if (item.uniqueId == pId)
					return item;
			}
		}
		return null;
	}
	/// <summary>
	/// finds a unique char in the database base on the given template / intId. it can auto create the char if neccessary
	/// </summary>
	/// <param name="templateId"></param>
	/// <param name="createMissing"></param>
	/// <returns></returns>
	public BountyCharacter FindUniqueCharacter(int templateId, bool createMissing)
	{
		string tCharName = BountyManager.instance.characterDatabase.GetCharacterName(templateId, true);
		foreach (var item in globalCharacters)
		{
			if (item.characterId == tCharName)
				return item;
		}
		if(createMissing)
		{
			BountyCharacter bc = Instantiate<BountyCharacter>(BountyManager.instance.characterDatabase.LoadCharacterResource(templateId));
			bc.Setup(0, new CharacterCreationInfo("spawned by FindUniqueChar"));
			return bc;
		}
		return null;
	}
	public BountyCharacter CreateRandomCharacter(SurvivorType type = SurvivorType.Normal, string spawnReason = null)
	{
		return CreateRandomCharacter(survivorTemplates[SDRandom.Range(0, survivorTemplates.Length)], type, spawnReason);
	}
	public BountyCharacter CreateRandomCharacter(SurvivorType type, bool female, string spawnReason = null)
	{
		List<BountyCharacter> preChoice = new List<BountyCharacter>(survivorTemplates);
		preChoice.RemoveAll(n => n.female != female);
		return CreateRandomCharacter(preChoice[SDRandom.Range(0, preChoice.Count)], type, spawnReason);
	}
	public BountyCharacter CreateRandomCharacter(BountyCharacter prefab, SurvivorType type, string spawnReason = null)
	{
		BountyCharacter result = Instantiate<BountyCharacter>(prefab);

		result.proceduralCharacter = true;
		ProceduralModelEntry pme = BountyManager.instance.modelManager.GetRandomModel(result.female);
		result.proceduralModel = pme.id;
		int nameId = 0;
		if (result.female)
		{
			nameId = femaleRandomNameCounter + 1;
			femaleRandomNameCounter += 1;
		}
		else
		{
			nameId = maleRandomNameCounter + 1;
			maleRandomNameCounter += 1;
		}
		result.CharName = (result.female ? "RandomSurvivor_Female_" : "RandomSurvivor_Male_") + nameId.ToString();
		//result.portraitData.simpleReference = pme.portraitReference;
		result.portraitData = pme.portraitData.CopyClone();
		result.price = 30;
		result.rank = 0;
		result.survivorType = type;
		// random clothing type to choose one from
		BaseItem.ItemType2[] clothingTypes = new BaseItem.ItemType2[]
		{
			BaseItem.ItemType2.GearHead,
			BaseItem.ItemType2.GearFeet,
			BaseItem.ItemType2.GearHands,
			BaseItem.ItemType2.GearLegs
		};
		if (type == SurvivorType.Worker)
		{
			result.price = 40;
			result.level = SDRandom.Range(2, 4);
			result.rank = 0;
			List<BaseItemDefinition> tEquip = new List<BaseItemDefinition>();
			BaseItemDefinition bid = new BaseItemDefinition();
			// worker has a GerarChest Item that provies Attributes: Armor and Strength
			bid.Setup(BaseItem.ItemType2.GearChest, 2, 1, new List<AttributeModifier>() { new AttributeModifier(BountyCharAttribute.Armor, 1), new AttributeModifier(BountyCharAttribute.Strength, 1) });
			tEquip.Add(bid);
			bid = new BaseItemDefinition();
			bid.Setup(clothingTypes[SDRandom.Range(0, clothingTypes.Length)], 2, 1, new List<AttributeModifier>() { new AttributeModifier(BountyCharAttribute.Armor, 1), new AttributeModifier(BountyCharAttribute.Strength, 1) });
			tEquip.Add(bid);
			result.startEquipment = tEquip.ToArray();
		}
		else if (type == SurvivorType.Soldier)
		{
			result.price = 50;
			result.level = SDRandom.Range(2, 4);
			result.rank = 0;
			List<BaseItemDefinition> tEquip = new List<BaseItemDefinition>();
			BaseItemDefinition bid = new BaseItemDefinition();
			// worker has a GerarChest Item that provies Attributes: Armor, Strength and Endurance
			bid.Setup(BaseItem.ItemType2.GearChest, 2, 1, new List<AttributeModifier>() { new AttributeModifier(BountyCharAttribute.Armor, 1), new AttributeModifier(BountyCharAttribute.Strength, 1), new AttributeModifier(BountyCharAttribute.Endurance, 1) });
			tEquip.Add(bid);
			bid = new BaseItemDefinition();
			bid.Setup(clothingTypes[SDRandom.Range(0,clothingTypes.Length)], 2, 1, new List<AttributeModifier>() { new AttributeModifier(BountyCharAttribute.Armor, 1), new AttributeModifier(BountyCharAttribute.Strength, 1), new AttributeModifier(BountyCharAttribute.Endurance, 1) });
			tEquip.Add(bid);
			result.startEquipment = tEquip.ToArray();
		}
		if (Random.value < (survivorSpawnWithPerkChance / 100)) //add perk to new random character generated to buy through radio room
		{
			BountyPerk p = spawnablePerks[SDRandom.Range(0, spawnablePerks.Count)];
			if (p != null)
			{
				result.perks = new List<BountyPerk> { p };
				//Debug.Log("Gave the new procedural character "+result.CharName+ "the perk "+p.perkID);
			}
			else
			{
				Debug.LogWarning("When Spawning a random procedural character: could not find the BountyPerk the character should have recieved. Continuing without Perk", this);
			}
		}
		
		string tSpawnText = "spawned as random procedural unique char";
		if (!string.IsNullOrEmpty(spawnReason))
			tSpawnText = spawnReason;
		result.Setup(0, new CharacterCreationInfo(tSpawnText));

		//training for some character types.
		if (type == SurvivorType.Soldier)
		{
			//Soldier needs to train to unlock combat skill and get weapons
			BountyManager.instance.camp.TrainingToEducateSoldierForPrisonRoom(result);
		}else if (type == SurvivorType.Worker)
		{
			BountyManager.instance.camp.TrainingToEducateWorkerForPrisonRoom(result);
		}
		return result;
	}

	public LocalInventory CreateLocalInventory(string pName, int pTemplate, int pState)
	{
		LocalInventory result = new LocalInventory();
		result.uniqueId = idCountInventories++;
		result.state = pState;
		result.name = pName;
		result.shopTemplate = pTemplate;
		globalInventories.Add(result);
		return result;
	}
	public LocalInventory GetLocalInventory(int pId)
	{
		foreach (var item in globalInventories)
		{
			if (item.uniqueId == pId)
				return item;
		}
		return null;
	}
	public List<LocalInventory> GetLocalInventories(List<int> idList, string nameFilter = "")
	{
		List<LocalInventory> result = new List<LocalInventory>();
		foreach (var item in globalInventories)
		{
			if (idList.Contains(item.uniqueId) && (string.IsNullOrEmpty(nameFilter) || item.name == nameFilter))
				result.Add(item);
		}
		return result;
	}
	public void DestroyLocalInventory(int pId)
	{
		for (int i = globalInventories.Count - 1; i >= 0; i--)
		{
			if(globalInventories[i].uniqueId == pId)
			{
				foreach (var item in globalInventories[i].inventory)
				{
					//UnregistierItem(item);
					Destroy(item);
				}
				globalInventories[i].inventory.Clear();
				globalInventories.RemoveAt(i);
				return;
			}
		}
	}

	/// <summary>
	/// called by the radio job room, creates 3 survivors and opens the survivor purchase gui
	/// </summary>
	/// <param name="pLevel"></param>
	public void StartSurvivorPurchase(int pLevel)
	{
		if (tempSurvivors == null || tempSurvivors.Count == 0)
			RefreshCallableSurvivors();
		MainGuiController.instance.survivorGui.Open(tempSurvivors, ConfirmSurvivorPurchase);
	}

	/// <summary>
	/// callback from the survivor purchase gui. takes the money and prepares the survivor to join next session
	/// </summary>
	/// <param name="pChar"></param>
	public bool ConfirmSurvivorPurchase(BountyCharacter pChar)
	{
		if(pChar != null)
		{
			if(GetResource(0) > pChar.price)
			{
				ChangeResource(0, -pChar.price);
				BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.SurvivorBought, pChar.uniqueId, SurvivorType.Normal });
				pendingJoiningSurvivors.Add(pChar);
				
				MainGuiController.instance.campGui.CurrentRoomGUI.AssignCharacterPurchase();

				return true;
			}
			else
			{
				return false;
			}
		}
		else
		{
			return true;
		}
	}

	/// <summary>
	/// called during session start routine. checks if survivors joined and needs to be assigned to camps, returns true if that is the case
	/// </summary>
	public bool CheckPendingJoiningSurvivors()
	{
        if (pendingJoiningSurvivors.Count > 0)
		{
			foreach (var item in pendingJoiningSurvivors)
			{
				if(tempSurvivors.Contains(item))
					tempSurvivors.Remove(item);
			}
			BountyManager.instance.mapList[0].ProceduralMap.AssignCharacters.AddRange(pendingJoiningSurvivors);
			pendingJoiningSurvivors.Clear();
			return BountyManager.instance.mapList[0].ProceduralMap.StartAssignSurvivors(false);
		}
		else
		{
			return false;
		}
	}

	public void RefreshCallableSurvivors()
	{
		// clear surivors
		foreach (var item in tempSurvivors)
		{
			Destroy(item);
		}
		tempSurvivors.Clear();
		for (int i = 0; i < 3; i++)
		{
			tempSurvivors.Add(CreateRandomCharacter());
		}
	}

	/// <summary>
	/// gibt die bewohner liste einer bestimmten base zurück
	/// </summary>
	/// <param name="baseId"></param>
	/// <returns></returns>
	public List<BountyCharacter> GetAllBaseMembers(int baseId)
	{
		FactionBase bb = BountyManager.instance.factionManager.PlayerFaction.bases[0];
		if (baseId != -1)
			bb = BountyManager.instance.factionManager.GetBase(baseId);
		List<BountyCharacter> result = new List<BountyCharacter>();
		result.AddRange(bb.members);
		
		return result;
	}
	/// <summary>
	/// gibt für eine bestimmte base die party und die potientellen party chars zurück
	/// </summary>
	/// <param name="baseId"></param>
	/// <returns></returns>
	public List<BountyCharacter> GetAllBasePartyChars(int baseId)
	{
		FactionBase bb = BountyManager.instance.factionManager.PlayerFaction.bases[0];
		if(baseId != -1)
			bb = BountyManager.instance.factionManager.GetBase(baseId);
		List<BountyCharacter> result = new List<BountyCharacter>();
		result.AddRange(GetParty(true, true));
		foreach (var cchar in bb.members)
		{
			if (cchar.partyCharacter)
				result.Add(cchar);
		}
		return result;
	}

	public void AddRecipe(int id, bool showNotification)
	{
		if (!knownRecipes.Contains(id))
		{
			knownRecipes.Add(id);
			if(showNotification)
			{
				CraftingRecipeTemplate crt = BountyManager.instance.craftingDatabase.GetAsset(id);
				MainGuiController.instance.notificationPanel.ShowRecipeNotification(crt);
			}
		}
	}
	public bool IsRecipeKnown(int id)
	{
		return knownRecipes.Contains(id);
	}
	public List<int> GetKnownRecipes()
	{
		return new List<int>(knownRecipes);
	}

	#region training related

	/// <summary>
	/// returns a list of items a character should have when they have a CharacterClass or rank.
	/// Is also used to give characters new Items when they level up their rank for a CharacterClass.
	/// </summary>
	/// <param name="pClass"></param>
	/// <param name="rank"></param>
	/// <returns></returns>
	public List<ResultItemDefinition> GetTrainingItemRequirements(CharacterClass pClass, int rank)
	{
		List<ResultItemDefinition> result = new List<ResultItemDefinition>();
		foreach (var entry in trainingEntries)
		{
			if(entry.charClass == pClass)
			{
				for (int i = 0; i < entry.ranks.Length; i++)
				{
					if(i == rank)
					{
						result.AddRange(entry.ranks[i].requirements);
						break;
					}
				}
				break;
			}
		}

		return result;
	}
	public List<CharSkillEntry> GetTrainingSkillRewards(CharacterClass pClass, int rank)
	{
		List<CharSkillEntry> result = new List<CharSkillEntry>();
		foreach (var entry in trainingEntries)
		{
			if (entry.charClass == pClass)
			{
				for (int i = 0; i < entry.ranks.Length; i++)
				{
					if (i == rank)
					{
						result.AddRange(entry.ranks[i].skillRewards);
						//result.AddRange(entry.ranks[i].skillPassiveRewards);
						break;
					}
				}
				break;
			}
		}

		return result;
	}
	public List<CharAttributeEntry> GetTrainingAttributeRewards(CharacterClass pClass, int rank)
	{
		List<CharAttributeEntry> result = new List<CharAttributeEntry>();
		foreach (var entry in trainingEntries)
		{
			if (entry.charClass == pClass)
			{
				for (int i = 0; i < entry.ranks.Length; i++)
				{
					if (i == rank)
					{
						result.AddRange(entry.ranks[i].attribRewards);
						break;
					}
				}
				break;
			}
		}

		return result;
	}

	public bool CanStartTraining(CampRoomType type, BountyCharacter pChar, CharacterClass pClass, int rank, ref string missing)
	{
		int c = 0;
		bool result = true;
		ResultItemDefinition[] items = GetTrainingItemRequirements(pClass, rank).ToArray();
		for (int i = 0; i < items.Length; i++)
		{
			if (CountPartyItems(items[i].itemType, items[i].tier) < items[i].stack)
			{
				result &= false;
				if (c == 0)
					missing += items[i].GetLocalizedName();
				else
					missing += ", "+ items[i].GetLocalizedName();
				c++;
			}
				
		}
		if (pChar.mainCharacter || pChar.storyCharacter || pChar.Job != CampRoomType.None)
			result &= false;

		
		int maxWorker = GetRoomLevel(type) >= 1 ? GetRoomDefinition(type).levels[GetRoomLevel(type) - 1].maxWorker : 1;

		if (GetPeopleWithJob(type, 0).Count >= maxWorker)
		{
			result &= false; // job count for this job already reached
		}

		return result;
	}
	public void StartTraining(CampRoomType type, BountyCharacter c, CharacterClass pClass, int rank)
	{
		string miss = "";
		if (!CanStartTraining(type, c, pClass, rank, ref miss))
			return;

		BaseNavNode.NodeType node = BaseNavNode.NodeType.Station;

		List<BaseItem> tItems = TakePartyItems(GetTrainingItemRequirements(pClass, rank).ToArray());

		c.Job = type;
		c.LastJob = type;
		string info = string.Format("($JobInfo,rank={0},class={1})", rank, (int)pClass);
		activeJobs.Add(new JobInstance() { character = c, type = type, cat = 1, slot = 0, startTime = BountyManager.instance.DateTime, continiously = false, specialInfo = info, hasMissingResources = false, missingResources = null, storedItems = tItems });
		c.startNavMode = 1;
		c.goToWork = true;
		c.startNodeType = node;
		c.startNodeStation = BaseNavNode.GetStationFromRoom(type);

		if (BountyManager.instance.CurrentTutorialIndex < 0)
			BountyManager.instance.campScene.UpdateCharacter(c, c.startNodeType, c.startNodeStation, c.startNavMode = 1, 2, true, 0, true);

		BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.TrainingAssigned, c.uniqueId, (int)type, rank, (int)pClass });
	}
	public void StopTraining(BountyCharacter c, JobInstance ji)
	{
		Dictionary<string, string> table = BountyExtensions.ParseCompoundString(ji.specialInfo);
		int rank = int.Parse(table["rank"]);
		CharacterClass pClass = (CharacterClass)int.Parse(table["class"]);
		AddPartyItems(ji.storedItems);

	}

	/// <summary>
	/// Character receives a Soldiers training in a randomly choosen class.
	/// Procedually generated Soldiers are sold at the rich people's Prison Room.
	/// This means the character receives skills, attributes and items matching the CharacterClass and rank.
	/// The character saves their new charClass and rank.
	/// Call this *after* BountyCharacter.Setup()
	/// </summary>
	/// <param name="procedualCharacter">character that revieces training</param>
	/// <param name="rank">new rank. At the moment there is only rank 0. It's tied to the BountyChamps trainingEntries</param>
	public void TrainingToEducateSoldierForPrisonRoom(BountyCharacter procedualCharacter, int rank =0)
	{
		//possible classes for soldiers:
		CharacterClass[] possible_classes_solider = {
			CharacterClass.Frontkaempfer, CharacterClass.Krieger, CharacterClass.Pistolero, CharacterClass.Jaeger,
			CharacterClass.Todeskommando, CharacterClass.Waechter, CharacterClass.MartialArtist,
			CharacterClass.Vollstrecker, CharacterClass.Survival, CharacterClass.Schuetze, CharacterClass.Techniker
		};

		var random_solider_class = possible_classes_solider[Random.Range(0, possible_classes_solider.Length)];
		
		TrainingToEducateCharsForPrisonRoom(procedualCharacter, random_solider_class,rank);
	}
	
	/// <summary>
	/// Character receives a Worker training in a randomly choosen class.
	/// Procedually generated Soldiers are sold at the rich people's Prison Room.
	/// This means the character receives skills, attributes and items matching the CharacterClass and rank.
	/// The character saves their new charClass and rank.
	/// Call this *after* BountyCharacter.Setup()
	/// </summary>
	/// <param name="procedualCharacter">character that revieces training</param>
	/// <param name="rank">new rank. At the moment there is only rank 0. It's tied to the BountyChamps trainingEntries</param>
	public void TrainingToEducateWorkerForPrisonRoom(BountyCharacter procedualCharacter, int rank =0)
	{
		//possible classes for soldiers:
		CharacterClass[] possible_classes_worker = {
			CharacterClass.Handwerk, CharacterClass.Industrie, CharacterClass.Forschung
		};

		var random_worker_class = possible_classes_worker[Random.Range(0, possible_classes_worker.Length)];
		
		TrainingToEducateCharsForPrisonRoom(procedualCharacter, random_worker_class,rank);
	}

	/// <summary>
	/// characters sold at the rich people's Prison Room are either Soldiers or Workers. Both receive training upon spawn.
	/// This means the character receives skills, attributes and items matching the CharacterClass and rank.
	/// The character saves their new charClass and rank.
	/// Call this *after* BountyCharacter.Setup()
	/// </summary>
	/// <param name="procedualChar">character that revieces training</param>
	/// <param name="pClass">class that character is trained in. Krieger, Nahkämpfer...</param>
	/// <param name="rank">new rank. At the moment there is only rank 0. It's tied to the BountyChamps trainingEntries </param>
	public void TrainingToEducateCharsForPrisonRoom(BountyCharacter procedualChar, CharacterClass pClass, int rank = 0)
	{
		//training for soliders:
		var skills = GetTrainingSkillRewards(pClass, rank);
		foreach (var skillEntry in skills)
		{
			procedualChar.ChangeSkillLevel(skillEntry.skill, skillEntry.value, true, false);
		}
		var attributes = GetTrainingAttributeRewards(pClass, rank);
		foreach (var charAttributeEntry in attributes)
		{
			procedualChar.ChangeAttributeRaw(charAttributeEntry.attribute, charAttributeEntry.value);
		}
		var tItems = GetTrainingItemRequirements(pClass, rank).ToArray();
		foreach (var itemDefinition in tItems)
		{
			// a slave's items should not get created by the players base. As in, it doesn't require resources for the player.
			// we also don't want to straight up take the player camp's items and give them to the slave. 
			// I would assume the slave just comes with their equipment.
			var item = itemDefinition.GenerateItem(0, 1, 0, null);

			if (item.itemType == BaseItem.ItemType2.Resource)
			{ // this is a resource that would need to be used while training
				continue;
			}
			if(item.IsType(BaseItem.ItemType2.Gear))
			{
				procedualChar.RemoveEquippedItem(item.GetSubType()); // remove previous item version if character has one
				procedualChar.AddEquipmentItem(item); // add new gear item, like weapons or a chest.
			}
		}
		procedualChar.charClass = pClass;
		procedualChar.rank = rank;
	}

	#endregion

}

[fsObject]
[System.Serializable]
public class LocalInventory
{
	public int uniqueId;
	public string name;
	public int state;
	public int lastVisited;
	public int shopTemplate; // ref id
	//public Vector2Int coords; // is saved on the map?
	public List<BaseItem> inventory;

	public LocalInventory()
	{
		shopTemplate = -1;
		lastVisited = -1;
		inventory = new List<BaseItem>();
	}
}


[System.Serializable]
public class TrainingClassRank
{
	public ResultItemDefinition[] requirements;
	public CharAttributeEntry[] attribRewards;
	public CharSkillEntry[] skillRewards;
	//public CharSkillEntry[] skillPassiveRewards;
}

[System.Serializable]
public class TrainingClassEntry
{
	public CharacterClass charClass;
	public TrainingClassRank[] ranks;
}
