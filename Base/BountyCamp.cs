using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FullSerializer;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// controls camp and party data, since DA3 it becomes more of a global item manager since camps are now planned to be streamlined and stored as bases in a faction's instance data
/// </summary>
[CreateAssetMenu(fileName = "Camp", menuName = "SDObjects/DataBase/Camp")]
[fsObject]
public partial class BountyCamp : ScriptableObject
{
    public static readonly Dictionary<CharacterBaseState, int> statDeathTable = new Dictionary<CharacterBaseState, int>()
    {
        { CharacterBaseState.Infected, 6 },
        { CharacterBaseState.Thirsty, 6 },
        { CharacterBaseState.Hungry, 10 },
    };
    public static readonly float[] bedHealValues = new float[] { 0.5f, 0.65f, 0.8f };
    public static readonly Dictionary<string, string> replacementCharTable = new Dictionary<string, string>()
    {
        { "Zivilist", "Tom" },
        { "Zivilist_Unbewaffnet", "Jamie" },
        { "Zivilistin", "Tina" },
        { "Zivilistin_Unbewaffnet2", "Charlotte" },
    };
    public static readonly int[] generatorConsumptionValues = new int[] { 8, 6, 4 };

    [fsProperty]
    [HideInInspector]
    [SerializeField]
    public List<BountyCharacter> party; // all characters in party
    [fsProperty]
    [HideInInspector]
    [SerializeField]
    public List<BountyCharacter> inhabitants; // all characters in base excluding the partymembers
    [fsProperty]
    [HideInInspector]
    public List<BountyCharacter> dead; // all characters that died
    [fsProperty]
    [HideInInspector]
    public List<BountyCharacter> away; // all characters that are currently somewhere not available but need to be saved on disk
    [fsProperty]
    [HideInInspector]
    public List<string> recruitedReplacementChars; // list of replacement chars that have been recruited

    [fsIgnore]
    [NonReorderable]
    public CampRoomDefinition[] roomDefinitions; // not saved on disk!
    [fsIgnore]
    [NonReorderable]
    public JobDefinition[] jobDefinitions; // not saved on disk!
    [fsProperty]
    [SerializeField]
    public List<CampRoomEntry> currentRooms;
    [fsProperty]
    [HideInInspector]
    public List<BaseItem> campStorage;
    [fsProperty]
    [HideInInspector]
    public List<BaseItem> partyStorage;
    [fsProperty]
    [HideInInspector]
    public List<BaseItem> backedStorage;
    [SerializeField, HideInInspector]
    [fsProperty]
    public int[] playerResorces;
    [SerializeField, HideInInspector]
    [fsProperty]
    public int[] playerBackedResorces;
    [fsIgnore]
    [SerializeField]
    [NonReorderable]
    protected List<BaseItemDefinition> startPartyStorage; // not saved on disk!
    [fsIgnore]
    [SerializeField]
    [NonReorderable]
    public List<BaseItemDefinition> playerStartEquipPool; // not saved on disk!
    [fsIgnore]
    [SerializeField]
    protected List<BountyCharacter> startInhabitants;
    [fsIgnore]
    [SerializeField]
    protected int[] startResorces;
    [fsIgnore]
    [SerializeField]
    [NonReorderable]
    protected BountyItemLevelSetting[] itemLevelSettings;
    [fsIgnore]
    [SerializeField]
    [NonReorderable]
    protected BountyCharacterLevelSetting[] characterLevelSettings;
    [fsProperty]
    public List<JobInstance> activeJobs;
    [fsProperty]
    public List<Vector3Int> jobExecutions;
    [fsProperty]
    [HideInInspector]
    public int[] resourcePositiveQueue;
    [fsProperty]
    [HideInInspector]
    public int[] resourceNegativeQueue;
    [fsProperty]
    [HideInInspector]
    public int formationSetting;

    [fsIgnore]
    public int FormationSetting
    {
        get { return formationSetting; }
        set { formationSetting = Mathf.Clamp(value, 0, 3); }
    }

    [fsIgnore]
    public int[] ResourcePositiveQueue
    {
        get
        {
            if (resourcePositiveQueue == null)
                resourcePositiveQueue = new int[playerResorces.Length];
            return resourcePositiveQueue;
        }
        set { resourcePositiveQueue = value; }
    }
    [fsIgnore]
    public int[] ResourceNegativeQueue
    {
        get
        {
            if (resourceNegativeQueue == null)
                resourceNegativeQueue = new int[playerResorces.Length];
            return resourceNegativeQueue;
        }
        set { resourceNegativeQueue = value; }
    }

    [fsIgnore]
    [SerializeField]
    public bool debugStart;
    [fsIgnore]
    [SerializeField]
    public List<BountyCharacter> debugPeople;
    [fsIgnore]
    [SerializeField]
    public List<BountyCharacter> debugParty;
    [fsIgnore]
    [SerializeField]
    public EncounterTemplate debugEnemies;
    [fsIgnore]
    [SerializeField]
    protected List<BaseItemDefinition> debugItems;
    [fsIgnore]
    [SerializeField]
    protected int debugTalentLvl = 10;
    [fsIgnore]
    [SerializeField]
    protected int debugRoomLvl = 2;
    [fsIgnore]
    [SerializeField]
    protected int debugTalentPoints = 150;
    [fsIgnore]
    [SerializeField]
    protected int debugResources = 300;
    [fsIgnore]
    [SerializeField]
    protected List<ItemValuePatch> itemPatches;

    [fsIgnore]
    private Dictionary<string, RuntimeAnimatorController> debugAniControllerTable = new Dictionary<string, RuntimeAnimatorController>();
    [fsIgnore]
    public Dictionary<string, RuntimeAnimatorController> DebugAniControllerTable
    {
        get { return debugAniControllerTable; }
        set { debugAniControllerTable = value; }
    }
    [fsIgnore]
    public RuntimeAnimatorController debugAniController
    {
        get;
        set;
    }

    [fsIgnore]
    protected Dictionary<BaseItem.ItemType2, BountyItemLevelSetting> itemLevelTable;
    public BountyItemLevelSetting GetItemLevelSetting(BaseItem.ItemType2 itemType)
    {
        if (itemLevelTable.ContainsKey(itemType))
            return itemLevelTable[itemType];
        else
            return null;
    }
    [fsIgnore]
    protected Dictionary<BountyCharAttribute, BountyCharacterLevelSetting> characterLevelTable;
    public BountyCharacterLevelSetting GetCharacterLevelSetting(BountyCharAttribute attrib)
    {
        if (characterLevelTable == null)
        {
            characterLevelTable = new Dictionary<BountyCharAttribute, BountyCharacterLevelSetting>();
            for (int i = 0; i < characterLevelSettings.Length; i++)
            {
                characterLevelTable.Add(characterLevelSettings[i].attribute, characterLevelSettings[i]);
            }
        }
        if (characterLevelTable.ContainsKey(attrib))
            return characterLevelTable[attrib];
        else
            return null;
    }

    public void PreSetupData()
    {
        itemLevelTable = new Dictionary<BaseItem.ItemType2, BountyItemLevelSetting>();
        for (int i = 0; i < itemLevelSettings.Length; i++)
        {
            itemLevelTable.Add(itemLevelSettings[i].itemtype, itemLevelSettings[i]);
        }

        characterLevelTable = new Dictionary<BountyCharAttribute, BountyCharacterLevelSetting>();
        for (int i = 0; i < characterLevelSettings.Length; i++)
        {
            characterLevelTable.Add(characterLevelSettings[i].attribute, characterLevelSettings[i]);
        }
    }

    public void Setup()
    {
        playerResorces = new int[9];
        playerBackedResorces = new int[9];
        resourceNegativeQueue = new int[9];
        resourcePositiveQueue = new int[9];
        campStorage = new List<BaseItem>();
        partyStorage = new List<BaseItem>();
        backedStorage = new List<BaseItem>();
        activeJobs = new List<JobInstance>();
        jobExecutions = new List<Vector3Int>();
        dead = new List<BountyCharacter>();
        away = new List<BountyCharacter>();
        recruitedReplacementChars = new List<string>();

        for (int i = 0; i < inhabitants.Count; i++)
        {
            inhabitants[i] = Instantiate<BountyCharacter>(inhabitants[i]);
            inhabitants[i].Setup(0, new CharacterCreationInfo("spawned by camp script setup"));
        }
        for (int i = 0; i < party.Count; i++)
        {
            party[i] = Instantiate<BountyCharacter>(party[i]);
            party[i].Setup(0, new CharacterCreationInfo("spawned by camp script setup"));
        }
    }

    public void TutGameSetup()
    {
        //BountyCharacter bc;
        //for (int i = 0; i < startInhabitants.Count; i++)
        //{
        //	bc = Instantiate<BountyCharacter>(startInhabitants[i]);
        //	bc.Setup();
        //	inhabitants.Add(bc);
        //}
    }
    public void NewGameSetup()
    {
        startResorces.CopyTo(playerResorces, 0);
        for (int i = 0; i < startPartyStorage.Count; i++)
        {
            partyStorage.Add(startPartyStorage[i].GenerateItem());
        }
        BountyCharacter bc;
        for (int i = 0; i < startInhabitants.Count; i++)
        {

            bc = Instantiate<BountyCharacter>(startInhabitants[i]);
            bc.Setup(0, new CharacterCreationInfo("spawned by camp script setup"));
            FactionBase bb = BountyManager.instance.factionManager.PlayerFaction.bases[0];
            bb.members.Add(bc);
            AddPartyItems(bc.GenerateStartInvItems(), false, false);
        }
        SetRoomLevel(CampRoomType.Defense, 1);
    }
    public void TutEndSetup()
    {
        startResorces.CopyTo(playerResorces, 0);
        if (BountyManager.instance.Variables.GetVariable("Story_Smuggler_Paid").AsBool())
        {
            playerResorces[0] -= 40;
        }
    }

    public void DebugSetup()
    {
        if (debugStart)
        {

            if (debugTalentLvl > 0)
            {
                BountyManager.instance.Player.ChangeTalentLevel(BountyTalentType.Talent_Melee, debugTalentLvl, true, false);
                BountyManager.instance.Player.ChangeTalentLevel(BountyTalentType.Talent_Swords, debugTalentLvl, true, false);
                BountyManager.instance.Player.ChangeTalentLevel(BountyTalentType.Talent_Pistol, debugTalentLvl, true, false);
                BountyManager.instance.Player.ChangeTalentLevel(BountyTalentType.Talent_Rifle, debugTalentLvl, true, false);
                BountyManager.instance.Player.ChangeTalentLevel(BountyTalentType.Talent_Shotgun, debugTalentLvl, true, false);
                BountyManager.instance.Player.ChangeTalentLevel(BountyTalentType.Talent_Grenadier, debugTalentLvl, true, false);
                BountyManager.instance.Player.ChangeTalentLevel(BountyTalentType.Talent_Hunting, debugTalentLvl, true, false);
                BountyManager.instance.Player.ChangeTalentLevel(BountyTalentType.Talent_Guarding, debugTalentLvl, true, false);
                BountyManager.instance.Player.ChangeTalentLevel(BountyTalentType.Talent_Medical, debugTalentLvl, true, false);
                BountyManager.instance.Player.ChangeTalentLevel(BountyTalentType.Talent_Survival, debugTalentLvl, true, false);
                BountyManager.instance.Player.ChangeTalentLevel(BountyTalentType.Talent_Crafting, debugTalentLvl, true, false);
            }

            if (debugRoomLvl > 0)
            {
                SetRoomLevel(CampRoomType.Medical, debugRoomLvl);
                SetRoomLevel(CampRoomType.WeaponSmith, debugRoomLvl);
                SetRoomLevel(CampRoomType.Smith, debugRoomLvl);
                SetRoomLevel(CampRoomType.ArmorSmith, debugRoomLvl);
                SetRoomLevel(CampRoomType.Hunter, debugRoomLvl);
                SetRoomLevel(CampRoomType.Farm, debugRoomLvl);
                SetRoomLevel(CampRoomType.RadioStation, debugRoomLvl);
                SetRoomLevel(CampRoomType.Defense, debugRoomLvl);
                SetRoomLevel(CampRoomType.Kitchen, debugRoomLvl);
                SetRoomLevel(CampRoomType.Generator, debugRoomLvl);
                SetRoomLevel(CampRoomType.WaterTreatment, debugRoomLvl);
                SetRoomLevel(CampRoomType.Bar, debugRoomLvl);
                SetRoomLevel(CampRoomType.BedRoom, debugRoomLvl);
                SetRoomLevel(CampRoomType.Merchant, debugRoomLvl);
                SetRoomLevel(CampRoomType.Recreation, debugRoomLvl);
            }

            foreach (var item in debugItems)
            {
                //AddCampItem(item.GenerateItem());
                partyStorage.Add(item.GenerateItem(0, true));
            }

            BountyCharacter bc = null;
            for (int i = 0; i < debugPeople.Count; i++)
            {
                bc = Instantiate<BountyCharacter>(debugPeople[i]);
                bc.Setup(0, new CharacterCreationInfo("spawned by camp script debug setup"));
                if (i < 2)
                    party.Add(bc);
                else
                    inhabitants.Add(bc);
                bc.startNavMode = 2;
                bc.startNodeStation = BaseNavNode.StationType.Any;
                bc.startNodeType = BaseNavNode.NodeType.Idle;
            }
            for (int i = 0; i < playerResorces.Length; i++)
            {
                playerResorces[i] = debugResources;
            }
            List<BountyCharacter> llist = GetAllCampPeople();
            for (int i = 0; i < llist.Count; i++)
            {
                llist[i].TalentPoints = debugTalentPoints;
                llist[i].AttributePoints = debugTalentPoints;
            }
            BountyManager.instance.Variables.SetVariable("Story_TessaDogUnlocked", true);
            BountyManager.instance.Variables.SetVariable("Story_TigerUnlocked", true);

            knownRecipes.Clear();
            knownRecipes.AddRange(BountyManager.instance.craftingDatabase.GetAssetIds());

        }
    }
    public void OnLoadPrewarm()
    {
        inhabitants.Clear();
        party.Clear();
    }
    public void OnLoadStep()
    {

        List<BountyCharacter> loadedList = new List<BountyCharacter>(); // fixing the bug that away list never got emptied
        for (int i = 0; i < inhabitants.Count; i++)
        {
            loadedList.Add(inhabitants[i]);
            inhabitants[i] = Instantiate<BountyCharacter>(SDResources.Load<BountyCharacter>("Character/" + inhabitants[i].characterId));
        }
        for (int i = 0; i < party.Count; i++)
        {
            loadedList.Add(party[i]);
            party[i] = Instantiate<BountyCharacter>(SDResources.Load<BountyCharacter>("Character/" + party[i].characterId));
        }
        for (int i = 0; i < dead.Count; i++)
        {
            loadedList.Add(dead[i]);
            dead[i] = Instantiate<BountyCharacter>(SDResources.Load<BountyCharacter>("Character/" + dead[i].characterId));
        }
        if (away == null)
            away = new List<BountyCharacter>();
        for (int i = away.Count - 1; i >= 0; i--)
        {
            if (loadedList.Contains(away[i]))
            {
                away.RemoveAt(i);
            }
            else
            {
                away[i] = Instantiate<BountyCharacter>(SDResources.Load<BountyCharacter>("Character/" + away[i].characterId));
            }
        }
    }
    public void OnLoaded(int fileVersion)
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
    }
    public void ApplyItemPatches(int fileVersion)
    {

        bool needsPatching = false;
        for (int i = 0; i < itemPatches.Count; i++)
        {
            if (itemPatches[i].fileVersion > fileVersion)
            {
                needsPatching = true;
                break;
            }
        }
        if (!needsPatching)
            return;

        // apply patches to all items
        List<BaseItem> itemList = new List<BaseItem>();
        itemList.AddRange(partyStorage);
        itemList.AddRange(campStorage);
        itemList.AddRange(backedStorage);
        List<BountyCharacter> charList = GetAllCampPeople(true);
        for (int i = charList.Count - 1; i >= 0; i--)
        {
            itemList.AddRange(charList[i].GetCompleteEqupment());
        }
        //List<WayPoint> pointList = BountyManager.instance.mapList[0].GetAllBasePoints();
        //for (int i = pointList.Count - 1; i >= 0; i--)
        //{
        //	itemList.AddRange(pointList[i].activeBase.shopInventory);
        //	itemList.AddRange(pointList[i].activeBase.shopPendingInventory);
        //	// base inhabitants too?
        //}
        int resultCount = 0;
        for (int i = 0; i < itemPatches.Count; i++)
        {
            if (itemPatches[i].fileVersion > fileVersion)
            {
                for (int j = itemList.Count - 1; j >= 0; j--)
                {
                    // apply patch to item
                    if (itemList[j].itemType == itemPatches[i].itemtype)
                    {
                        for (int k = itemList[j].attributes.Count - 1; k >= 0; k--)
                        {
                            for (int l = 0; l < itemPatches[i].changes.Length; l++)
                            {
                                if (itemList[j].attributes[k].attribute == itemPatches[i].changes[l].attribute)
                                {
                                    itemList[j].attributes[k].fixedValue += itemPatches[i].changes[l].fixedValue;
                                    resultCount++;
                                }
                            }
                        }
                    }
                    if (itemPatches[i].fixMissingAttribs)
                    {
                        if (itemList[j].IsType(BaseItem.ItemType2.Gear) && (itemList[j].attributes == null || itemList[j].attributes.Count < 2))
                        {
                            itemList[j].attributes = BaseItem.GenerateAttributes(itemList[j].itemType, Mathf.Clamp(itemList[j].level - 1, 1, 3), itemList[j].level, 0);
                            resultCount++;
                        }

                    }
                }
            }
        }

        if (resultCount > 0)
            Debug.LogFormat("Applied {0} patches to items", resultCount);
    }

    private void OnDestroy()
    {
        for (int i = 0; i < party.Count; i++)
        {
            Destroy(party[i]);
        }
        for (int i = 0; i < inhabitants.Count; i++)
        {
            Destroy(inhabitants[i]);
        }
        if (dead != null)
        {
            for (int i = 0; i < dead.Count; i++)
            {
                Destroy(dead[i]);
            }
        }
        if (away != null)
        {
            for (int i = 0; i < away.Count; i++)
            {
                Destroy(away[i]);
            }
        }
        if (partyStorage != null)
        {
            for (int i = 0; i < partyStorage.Count; i++)
            {
                Destroy(partyStorage[i]);
            }
        }
        if (campStorage != null)
        {
            for (int i = 0; i < campStorage.Count; i++)
            {
                Destroy(campStorage[i]);
            }
        }
        if (backedStorage != null)
        {
            for (int i = 0; i < backedStorage.Count; i++)
            {
                Destroy(backedStorage[i]);
            }
        }

        if (globalCharacters != null)
        {

            for (int i = 0; i < globalCharacters.Count; i++)
            {
                Destroy(globalCharacters[i]);
            }
        }
        if (globalItems != null)
        {
            for (int i = 0; i < globalItems.Count; i++)
            {
                Destroy(globalItems[i]);
            }
        }
    }

    public List<string> GetRecruitedReplacementChars() // returns: char id
    {
        return new List<string>(recruitedReplacementChars);
    }
    //public void AddRecruitedReplacementChar(string id) // arg: char id
    //{
    //	if(replacementCharTable.ContainsValue(id) && !recruitedReplacementChars.Contains(id))
    //		recruitedReplacementChars.Add(id);
    //}
    public bool HasCharBeenRecruited(string id) // arg: zivilist id
    {
        if (replacementCharTable.ContainsKey(id))
        {
            return recruitedReplacementChars.Contains(replacementCharTable[id]);
        }
        else
        {
            return false;
        }
    }
    public int CountSpawnableReplacementChars()
    {
        int lostChars = dead.FindAll(n => !n.replacementCharacter).Count; // how many non-replacements have died
        int replacments = GetAllCampPeople().FindAll(n => n.replacementCharacter).Count; // how many recruited replacements are active
        int unSpawned = replacementCharTable.Count - recruitedReplacementChars.Count; // how many replacements are left
        return Mathf.Clamp(lostChars - replacments, 0, unSpawned); // return the smaller value
    }
    public void SpawnReplacementCharcter(string id) // arg: zivilist id
    {
        if (replacementCharTable.ContainsKey(id))
        {
            string charId = replacementCharTable[id];
            recruitedReplacementChars.Add(charId);

            BountyCharacter c = Instantiate<BountyCharacter>(SDResources.Load<BountyCharacter>("Character/" + charId));
            c.Setup(0, new CharacterCreationInfo("spawned as replacement char"));
            AddInhabitant(c);
            AddPartyItems(c.GenerateStartInvItems());
        }
    }
    public List<Vector2Int> GetFreeSlots(bool useCustomFormation = true)
    {
        Vector2Int combatSize = new Vector2Int(BountyManager.instance.combatManager.CombatInfo.fieldSizeX, BountyManager.instance.combatManager.CombatInfo.fieldSizeY);

        List<Vector2Int> result = new List<Vector2Int>();
        for (int x = 0; x < combatSize.x / 2; x++)
        {
            for (int y = 0; y < combatSize.y; y++)
            {
                result.Add(new Vector2Int(x, y));
            }
        }
        for (int i = 0; i < party.Count; i++)
        {
            result.Remove(new Vector2Int(party[i].StartRow, party[i].StartSlot));
        }
        return result;
    }

    #region character related

    public void AddPartyMember(BountyCharacter c)
    {
        if (!party.Contains(c))
        {
            if ((GetParty(false).Count < 3 || c.temporary) && (c.HasState(CharacterBaseState.Infected) == false && c.HasState(CharacterBaseState.Moody) == false)) //Fragwürdige Methode; Sollte überarbeitet werden
            {
                int index = 0;
                if (c.temporary)
                {
                    index = party.Count;
                }
                else if (c.mainCharacter)
                {
                    index = 0;
                }
                else
                {
                    index = GetParty(false).Count;
                }

                if (index >= party.Count)
                {
                    party.Add(c);
                }
                else
                {
                    party.Insert(index, c);
                }
                UpdatePartyFormation();
                UpdatePartyCount();
                BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.PartyMemberAdded, c.uniqueId });
                MainGuiController.instance.partyOverview.UpdateGui();
            }
            else
            {
                AddInhabitant(c);
            }
            BountyManager.instance.persistentManager.ChangeCurrentStat(BountyPersistentStat.Camp_Survivor_Num, GetAllCampPeople().Count, true);
        }
    }
    public void InsertPlayer(BountyCharacter c)
    {
        if (!party.Contains(c))
        {
            if (party.Count == 0)
            {
                party.Add(c);
            }
            else
            {
                party.Insert(0, c);
            }
        }
    }
    public List<BountyCharacter> GetParty(bool includeTemporarys, bool includeBackgroundpartyChars = false)
    {
        List<BountyCharacter> result = new List<BountyCharacter>();
        int c = party.Count;
        bool included = false;
        for (int i = 0; i < c; i++)
        {
            included = true;
            if (party[i].temporary)
                included = includeTemporarys;
            if (!includeTemporarys && party[i].backgroundCharacter && party[i].partyCharacter)
                included = includeBackgroundpartyChars;

            if (included)
                result.Add(party[i]);
        }

        return result;
    }
    public List<BountyCharacter> GetCombatSortedParty(bool includeBackgroundChars = false)
    {
        List<BountyCharacter> raw = GetParty(false, BountyManager.instance.CurrentTutorialIndex == 5 || includeBackgroundChars);
        List<BountyCharacter> result = new List<BountyCharacter>();
        result.AddRange(raw);
        //if (raw.Count == 0)
        //	return result;

        //if (raw.Count >= 2)
        //{
        //	result.Add(raw[1]);
        //}
        //result.Add(raw[0]);
        //if (raw.Count >= 3)
        //{
        //	result.Add(raw[2]);
        //}
        return result;
    }
    public List<BountyCharacter> GetParty(System.Predicate<BountyCharacter> match)
    {
        return new List<BountyCharacter>(party.FindAll(match));
    }
    public BountyCharacter GetPartyMember(System.Predicate<BountyCharacter> match)
    {
        return party.Find(match);
    }
    public bool IsInParty(BountyCharacter c)
    {
        return party.Contains(c);
    }
    public void RemovePartyMember(System.Predicate<BountyCharacter> match)
    {
        BountyCharacter c = party.Find(match);
        if (c != null)
        {
            RemovePartyMember(c);
        }
    }
    public void RemovePartyMember(BountyCharacter c)
    {
        if (party.Contains(c))
        {
            party.Remove(c);
            UpdatePartyCount();
            //MainGuiController.instance.partyOverview.UpdateOverview(party);
            MainGuiController.instance.partyOverview.UpdateGui();
        }
    }
    public void RemoveTempPartyMembers()
    {
        List<BountyCharacter> list = party.FindAll(n => n.temporary);
        list.ForEach(n => { n.DestroyModel(); Destroy(n); party.Remove(n); });
        UpdatePartyCount();
    }
    public void RemoveBackgroundPartyMembers() // not used anymore?
    {
        List<BountyCharacter> list = party.FindAll(n => !n.mainCharacter && ((n.backgroundCharacter && n.Faction == Faction.Player) || n.HasState(CharacterBaseState.Moody) || n.HasState(CharacterBaseState.Infected)));
        list.ForEach(n => { inhabitants.Add(n); party.Remove(n); });
        UpdatePartyCount();
    }
    public void SwitchPartyState(BountyCharacter c, bool force = false)
    {
        if (IsInParty(c))
        {
            //if (!c.backgroundCharacter)
            //{
            RemovePartyMember(c);
            //inhabitants.Add(c);
            BountyManager.instance.factionManager.GetCurrentPlayerBase().members.Add(c);
            BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.PartyMemberChanged, c.uniqueId, false });
            BountyManager.instance.audioManager.PlaySoundEffect("Item_");
            // }
        }
        else
        {
            List<BountyCharacter> tList = new List<BountyCharacter>(GetParty(false));
            tList.RemoveAll(n => n.mainCharacter); // we count the party without main char so we can always add him but not a 4th member
            if (tList.Count < 2 || force)
            {
                if (!c.HasWorkBlockingState())
                {
                    RemoveJob(c);
                    AddPartyMember(c);
                    //inhabitants.Remove(c);
                    BountyManager.instance.factionManager.GetCurrentPlayerBase().members.Remove(c);
                    if (!force)
                    {
                        BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.PartyMemberChanged, c.uniqueId, true });
                        BountyManager.instance.audioManager.PlaySoundEffect("gui_clothes");
                    }
                }
                else
                {
                    MainGuiController.instance.notificationPanel.ShowNotification("Info_UnableToJoinParty");
                }
            }
            else
            {
                // party full
                MainGuiController.instance.notificationPanel.ShowNotification("Info_PartyFull");
            }
        }
    }
    public void SwitchCharacterAway(BountyCharacter c, bool value)
    {
        if (away.Contains(c) && !value)
        {
            AddInhabitant(c);
            away.Remove(c);
        }
        else if (!away.Contains(c) && value)
        {
            if (party.Contains(c))
            {
                party.Remove(c);
            }
            else if (GetInhabitants().Contains(c))
            {
                RemoveInhabitant(c);
            }
            away.Add(c);
        }
    }
    public bool IsPartyFull()
    {
        return party.Count >= 3;
    }
    public void AddInhabitant(BountyCharacter c)
    {
        FactionBase bb = BountyManager.instance.factionManager.PlayerFaction.bases[0];

        if (!bb.members.Contains(c))
        {
            bb.members.Add(c);
            BountyManager.instance.persistentManager.ChangeCurrentStat(BountyPersistentStat.Camp_Survivor_Num, GetAllCampPeople().Count, true);
            BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.CampInhabitantAdded, c.uniqueId });
        }
    }
    public void RemoveInhabitant(BountyCharacter c)
    {
        FactionBase bb = BountyManager.instance.factionManager.PlayerFaction.bases[0];

        if (bb.members.Contains(c))
        {
            bb.members.Remove(c);
        }
    }
    public List<BountyCharacter> GetInhabitants()
    {
        FactionBase bb = BountyManager.instance.factionManager.PlayerFaction.bases[0];
        return new List<BountyCharacter>(bb.members);
    }
    public List<BountyCharacter> GetInhabitants(System.Predicate<BountyCharacter> match)
    {
        FactionBase bb = BountyManager.instance.factionManager.PlayerFaction.bases[0];
        return new List<BountyCharacter>(bb.members.FindAll(match));
    }
    public BountyCharacter GetInhabitant(System.Predicate<BountyCharacter> match)
    {
        FactionBase bb = BountyManager.instance.factionManager.PlayerFaction.bases[0];
        return bb.members.Find(match);
    }
    // functions for new base system
    public List<BountyCharacter> GetBaseInhabitants(int id)
    {
        FactionBase bb = BountyManager.instance.factionManager.GetBase(id);
        return new List<BountyCharacter>(bb.members);
    }
    public BountyCharacter GetBaseInhabitant(int id, System.Predicate<BountyCharacter> match)
    {
        FactionBase bb = BountyManager.instance.factionManager.GetBase(id);
        return bb.members.Find(match);
    }
    public List<BountyCharacter> GetBaseInhabitants(int id, System.Predicate<BountyCharacter> match)
    {
        FactionBase bb = BountyManager.instance.factionManager.GetBase(id);
        return new List<BountyCharacter>(bb.members.FindAll(match));
    }
    public List<BountyCharacter> GetAwayPeople()
    {
        if (away != null)
        {
            return new List<BountyCharacter>(away);
        }
        else return new List<BountyCharacter>();
    }
    public List<BountyCharacter> GetAllCampPeople(bool includeBackgroundChars = false, bool includeAwayChars = false)
    {
        List<BountyCharacter> result = new List<BountyCharacter>();
        result.AddRange(GetParty(n => !n.temporary || includeBackgroundChars));
        if (includeBackgroundChars)
            result.AddRange(GetInhabitants());
        else
        {

            result.AddRange(GetInhabitants(n => !n.backgroundCharacter));

        }
        if (includeAwayChars)
        {
            if (includeBackgroundChars)
                result.AddRange(away);
            else
                result.AddRange(away.FindAll(n => !n.backgroundCharacter));
        }
        return result;
    }
    public List<BountyCharacter> GetAllCampPartyPeople(bool includeBackgroundChars = false)
    {
        List<BountyCharacter> result = new List<BountyCharacter>();
        result.AddRange(GetParty(true, true));
        if (includeBackgroundChars)
            result.AddRange(GetInhabitants(n => n.partyCharacter || n.backgroundCharacter));
        else
            result.AddRange(GetInhabitants(n => n.partyCharacter && !n.backgroundCharacter));
        return result;
    }
    public List<BountyCharacter> GetAllPeopleWithoutJob(bool excludeParty = false)
    {
        List<BountyCharacter> result = GetAllCampPeople();
        result.RemoveAll(n => GetActiveJob(n) != null);
        if (excludeParty)
            result.RemoveAll(n => IsInParty(n));
        return result;
    }
    public List<BountyCharacter> GetAllPeopleWithState(CharacterBaseState state)
    {
        List<BountyCharacter> result = GetAllCampPeople();
        result.RemoveAll(n => !n.HasState(state));
        return result;
    }
    public List<BountyCharacter> GetAllBedriddenPeople()
    {
        List<BountyCharacter> result = GetAllCampPeople();
        result.RemoveAll(n => !n.HasBedriddenState());
        return result;
    }
    public void RegisterDeath(BountyCharacter c, bool informLootEvent)
    {
        if (informLootEvent)
        {
            int index = party.IndexOf(c);
            if (index >= 0 && index < party.Count)
            {
                int eventIndex = BountyManager.instance.Variables.GetVariable("@LootEventCharacter").AsInt();
                if (index < eventIndex)
                {
                    BountyManager.instance.Variables.SetVariable("@LootEventCharacter", eventIndex - 1);
                }
            }
        }

        party.Remove(c);
        inhabitants.Remove(c);
        away.Remove(c);
        dead.Add(c);
        UpdatePartyCount();
    }
    public void RegisterTempDeath(BountyCharacter c)
    {
        party.Remove(c);
        UpdatePartyCount();
    }
    public void RegisterKo(BountyCharacter c, bool informLootEvent)
    {
        if (informLootEvent)
        {
            int index = party.IndexOf(c);
            if (index >= 0 && index < party.Count)
            {
                int eventIndex = BountyManager.instance.Variables.GetVariable("@LootEventCharacter").AsInt();
                if (index < eventIndex)
                {
                    BountyManager.instance.Variables.SetVariable("@LootEventCharacter", eventIndex - 1);
                }
            }
        }
        c.AddState(CharacterBaseState.Injured, 3);
        if (c.faction == Faction.Player)
        {
            party.Remove(c);
            AddInhabitant(c);
        }
        else
        {
            // what happens to foreign chars in party when the go KO? the just stay in the party and wake up after combat most of the time I guess
        }
        UpdatePartyCount();
    }
    public List<BountyCharacter> GetAllDeadPeople()
    {
        List<BountyCharacter> result = new List<BountyCharacter>(dead);
        return result;
    }
    public List<BountyCharacter> GetNewDeadPeople()
    {
        List<BountyCharacter> result = new List<BountyCharacter>();
        result.AddRange(dead.FindAll(n => n.RecentDeath));
        return result;
    }
    public void ShowPartySpeechBubble(SpeechBubbleType type, BountyCharacter exclude = null, int range = 3)
    {
        BountyCharacter[] arr = GetParty(n => !n.temporary && n != exclude).ToArray();
        if (arr.Length > 0)
        {
            BountyCharacter guy = arr[SDRandom.Range(0, arr.Length)];
            string id = guy.mainCharacter ? "Player" : guy.characterId;
            if (guy.Model != null)
                SpeechBubble.CreateSpeechBubble(guy, LocaTokenHelper.ParseDialogueTags(BountyManager.instance.dialogueManager.LocalizedText("SpeechBubble_" + type.ToString() + "_" + id + "_" + SDRandom.Range(0, range))));
        }
        //return null;
    }
    public void ShowSpeechBubble(string locaKey, BountyCharacter guy, int range = -1)
    {
        string key = locaKey;
        if (range >= 0)
        {
            key += "_" + SDRandom.Range(0, range);
        }
        if (guy.Model != null)
            SpeechBubble.CreateSpeechBubble(guy, LocaTokenHelper.ParseDialogueTags(BountyManager.instance.dialogueManager.LocalizedText(key)));
        //return null;
    }

    private void UpdatePartyCount()
    {
        BountyManager.instance.Variables.SetVariable("@PartySize", BountyManager.instance.camp.GetParty(false).Count);
    }
    public void UpdatePartyFormation()
    {
        int[] slotMap = new int[] { 1, 0, 2 };
        for (int i = 0; i < party.Count; i++)
        {
            party[i].StartRow = i / 3;
            party[i].StartSlot = slotMap[i % 3];
        }
    }

    public static int CharacterCampSort(BountyCharacter a, BountyCharacter b)
    {
        if (a.mainCharacter == b.mainCharacter)
        {
            if (a.storyCharacter == b.storyCharacter)
            {
                return 0;
            }
            else
            {
                return a.storyCharacter ? -1 : 1;
            }
        }
        else
        {
            return a.mainCharacter ? -1 : 1;
        }
    }
    public static int CharacterBedCalculationSort(BountyCharacter a, BountyCharacter b)
    {
        if (a.mainCharacter == b.mainCharacter)
        {
            if (a.storyCharacter == b.storyCharacter)
            {
                if (a.survivorType == b.survivorType)
                {
                    return 0;
                }
                else
                {
                    if (a.survivorType == SurvivorType.Worker)
                        return -1;
                    else if (b.survivorType == SurvivorType.Worker)
                        return 1;
                    else if (a.survivorType == SurvivorType.Soldier)
                        return 1;
                    else if (b.survivorType == SurvivorType.Soldier)
                        return -1;
                    else
                        return 0;
                }
            }
            else
            {
                return a.storyCharacter ? -1 : 1;
            }
        }
        else
        {
            return a.mainCharacter ? -1 : 1;
        }
    }

    #endregion

    #region item related

    public int[] GetResourceArray()
    {
        List<int> temp = new List<int>(playerResorces);
        return temp.ToArray();
    }
    public int GetResource(int i)
    {
        return playerResorces[i];
    }
    public void ChangeResource(int resource, int value, bool set = false, bool showNotification = false, bool dontUpdateGui = false)
    {
        int diff = 0;

        if (set)
        {
            diff = value - playerResorces[resource];
            playerResorces[resource] = value;

        }
        else
        {
            if (value < 0 && value + playerResorces[resource] < 0) // negative clamp to 0
                value = -playerResorces[resource];

            playerResorces[resource] += value;
            diff = value;
        }

        playerResorces[resource] = Mathf.Clamp(playerResorces[resource], 0, 9999);
        // store changes
        if (diff < 0)
        {
            resourceNegativeQueue[resource] += diff;
        }
        else if (diff > 0)
        {
            resourcePositiveQueue[resource] += diff;
        }

        if (!dontUpdateGui)
        {
            MainGuiController.instance.resourceDisplay.UpdateResources(playerResorces, false, false);
        }

        if (showNotification && !set)
        {
            MainGuiController.instance.notificationPanel.ShowResourceNotification(resource, value);
        }
        if (resource == 0)
            BountyManager.instance.persistentManager.ChangeCurrentStat(BountyPersistentStat.Camp_Stored_Dollars, playerResorces[0], true);

        BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.ResourceChanged, resource, value });
    }
    // used by crafting system
    public bool CheckResources(BountyResourceEntry[] checks)
    {
        for (int i = 0; i < checks.Length; i++)
        {
            if (GetResource(checks[i].type) < checks[i].amount)
                return false;
        }
        return true;
    }
    /// <summary>
    /// used by crafting system. returns: 1 => all resource there, 0 => only missable missing, -1 => missing
    /// </summary>
    /// <param name="checks"></param>
    /// <returns></returns>
    public int CheckResourcesWithMissables(BountyResourceEntry[] checks)
    {
        int result = 1;
        for (int i = 0; i < checks.Length; i++)
        {
            if (checks[i].type == 1 || checks[i].type == 2)
            {
                if (GetResource(checks[i].type) < checks[i].amount)
                {
                    if (result > 0)
                        result = 0;
                }
            }
            else
            {
                if (GetResource(checks[i].type) < checks[i].amount)
                {
                    result = -1;
                    break;
                }
            }
        }
        return result;
    }
    // used by crafting system
    public void TakeResources(BountyResourceEntry[] checks)
    {
        for (int i = 0; i < checks.Length; i++)
        {
            ChangeResource(checks[i].type, -checks[i].amount);
        }
    }
    // used by crafting system
    public List<BountyResourceEntry> TakeResourcesWithMissables(BountyResourceEntry[] checks)
    {
        List<BountyResourceEntry> missing = new List<BountyResourceEntry>();

        for (int i = 0; i < checks.Length; i++)
        {
            if (checks[i].type == 1 || checks[i].type == 2)
            {
                if (GetResource(checks[i].type) < checks[i].amount)
                {
                    missing.Add(new BountyResourceEntry(checks[i].type, checks[i].amount - GetResource(checks[i].type)));
                }
                ChangeResource(checks[i].type, -checks[i].amount);
            }
            else
            {
                ChangeResource(checks[i].type, -checks[i].amount);
            }

        }

        return missing;
    }
    // used by crafting system
    public void GiveResources(BountyResourceEntry[] checks)
    {
        for (int i = 0; i < checks.Length; i++)
        {
            ChangeResource(checks[i].type, checks[i].amount);
        }
    }
    // used in final dungeon
    public void BackAllResources(bool alsoRemove)
    {
        for (int i = 0; i < playerResorces.Length; i++)
        {
            playerBackedResorces[i] = playerResorces[i];
            if (alsoRemove)
                ChangeResource(i, 0, true, false, true);
        }
    }
    public void ReturnAllBackedResources()
    {
        for (int i = 0; i < playerResorces.Length; i++)
        {
            ChangeResource(i, playerBackedResorces[i], false, false, true);
            playerBackedResorces[i] = 0;
        }
    }
    public void BackAllItems()
    {
        List<BaseItem> list = GetPartyItems();
        foreach (var item in list)
        {
            RemovePartyItem(item);
            if (item.itemType == BaseItem.ItemType2.MiscKey && item.Tier == 2)
            {
                Destroy(item);
            }
            else
            {
                backedStorage.Add(item);
            }
        }
    }
    public void ReturnAllBackedItems()
    {
        List<BaseItem> list = new List<BaseItem>(backedStorage);
        AddPartyItems(backedStorage);
        backedStorage.Clear();
    }

    /// <summary>
    /// get all party items
    /// </summary>
    /// <returns>list of items</returns>
    public List<BaseItem> GetPartyItems()
    {
        List<BaseItem> result = new List<BaseItem>(partyStorage);
        result.Sort(ItemSort);
        return result;
    }
    /// <summary>
    /// get all party items by match
    /// </summary>
    /// <param name="match">match predicate</param>
    /// <returns>list of items</returns>
    public List<BaseItem> GetPartyItems(System.Predicate<BaseItem> match)
    {
        List<BaseItem> result = partyStorage.FindAll(match);
        result.Sort(ItemSort);
        return result;
    }
    /// <summary>
    /// get all party items by item type and sub type
    /// </summary>
    /// <param name="mainType">main type</param>
    /// <param name="subType">sub type (optional)</param>
    /// <returns></returns>
    public List<BaseItem> GetPartyItems(BaseItem.ItemType2 mainType, BaseItem.ItemType2 subType = BaseItem.ItemType2.None, int level = -1)
    {
        List<BaseItem> result = new List<BaseItem>();
        for (int i = 0; i < partyStorage.Count; i++)
        {
            if (partyStorage[i].GetMainType() != mainType)
                continue;
            if (subType != BaseItem.ItemType2.None && partyStorage[i].GetSubType() != subType)
                continue;
            if (level != -1 && partyStorage[i].Tier != level)
                continue;
            result.Add(partyStorage[i]);
        }
        result.Sort(ItemSort);
        return result;
    }
    public List<BaseItem> GetPartyItems(BaseItem.ItemType2 exactType, int level = -1)
    {
        List<BaseItem> result = new List<BaseItem>();
        for (int i = 0; i < partyStorage.Count; i++)
        {
            if (partyStorage[i].itemType != exactType)
                continue;
            if (level != -1 && partyStorage[i].Tier != level)
                continue;
            result.Add(partyStorage[i]);
        }
        result.Sort(ItemSort);
        return result;
        //return new List<BaseItem>(partyStorage.FindAll(n => n.itemType == exactType && (level == -1 || n.level == level)));
    }
    public int CountPartyItems(BaseItem.ItemType2 exactType, int level = -1)
    {
        if (exactType == BaseItem.ItemType2.Resource)
        {
            return GetResource(level);
        }

        List<BaseItem> list = new List<BaseItem>();
        for (int i = 0; i < partyStorage.Count; i++)
        {
            if (partyStorage[i].itemType != exactType)
                continue;
            if (level != -1 && partyStorage[i].Tier != level)
                continue;
            list.Add(partyStorage[i]);
        }
        if (list.Count > 0 && BaseItem.IsStackable(exactType))
        {
            return list[0].currentStack;
        }
        else
        {
            return list.Count;
        }
    }
    public int CountPartyItemsBroad(BaseItem.ItemType2 exactType)
    {
        int result = 0;
        for (int i = 0; i < partyStorage.Count; i++)
        {
            if (!BaseItem.IsType(partyStorage[i].itemType, exactType))
                continue;
            if (BaseItem.IsStackable(partyStorage[i].itemType))
            {
                result += partyStorage[i].currentStack;
            }
            else
            {
                result += 1;
            }
        }

        return result;
    }
    public bool RemovePartyItem(BaseItem item, bool includeEquipment = false, bool showNotification = false)
    {
        BaseItemDefinition itemInfos = null;
        itemInfos = item.GenerateDefinition();

        if (partyStorage.Contains(item))
        {
            partyStorage.Remove(item);
            if (showNotification)
            {
                MainGuiController.instance.notificationPanel.ShowItemNotification(itemInfos, true, true);
            }
            return true;
        }
        if (includeEquipment)
        {
            for (int i = 0; i < party.Count; i++)
            {
                if (party[i].RemoveEquippedItem(item))
                {
                    if (showNotification)
                    {
                        MainGuiController.instance.notificationPanel.ShowItemNotification(itemInfos, true, true);
                    }
                    return true;
                }
            }
        }
        return false;
    }
    public bool RemovePartyItem(BaseItemDefinition item, bool showNotification)
    {
        BaseItemDefinition itemInfos = null;
        List<BaseItem> l = null;
        l = GetPartyItems(n => n.itemType == item.itemType && n.Tier == item.Tier && (item.variant == -1 || item.variant == n.variant));
        if (l.Count == 0)
            return false;
        if (l[0].IsStackable())
        { // stackable item
            if (item.currentStack < 1)
                item.currentStack = l[0].currentStack;

            if (l[0].currentStack < item.currentStack)
                return false;
            else if (l[0].currentStack > item.currentStack)
            {
                l[0].currentStack -= item.currentStack;
                itemInfos = item;
            }
            else
            {
                partyStorage.Remove(l[0]);
                itemInfos = l[0].GenerateDefinition();
                Destroy(l[0]);
            }
        }
        else
        {
            // gear can yet not specified precicely
            partyStorage.Remove(l[0]);
            itemInfos = l[0].GenerateDefinition();
            Destroy(l[0]);
        }
        if (showNotification)
        {
            MainGuiController.instance.notificationPanel.ShowItemNotification(itemInfos, true, true);
        }
        return true;
    }
    // used by crafting system
    public bool CheckItems(ResultItemDefinition[] checks)
    {
        for (int i = 0; i < checks.Length; i++)
        {
            if (CountPartyItems(checks[i].itemType, checks[i].tier) < checks[i].stack)
                return false;
        }
        return true;
    }
    // used by crafting system
    public bool RemovePartyItems(ResultItemDefinition[] items)
    {
        //BaseItem b = null;
        List<BaseItem> l = null;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].itemType == BaseItem.ItemType2.Resource)
            {
                if (GetResource(items[i].tier - 1) < items[i].stack)
                    return false;
                ChangeResource(items[i].tier - 1, -items[i].stack);
            }

            l = GetPartyItems(n => n.itemType == items[i].itemType && n.Tier == items[i].tier);
            if (l.Count == 0)
                return false;
            if (l[0].IsStackable())
            { // stackable item
                if (l[0].currentStack < items[i].stack)
                    return false;
                else if (l[0].currentStack > items[i].stack)
                {
                    l[0].currentStack -= items[i].stack;
                }
                else
                {
                    partyStorage.Remove(l[0]);
                    Destroy(l[0]);
                }
            }
            else
            {
                // not stackable item is weird, because we dont know which item to take?
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Searches for items, matching the definitions and removes them from the Camp's Item storage.
    /// Returns all items from storage, that matched the item definitions. Might not be as many items as asked for
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    public List<BaseItem> TakePartyItems(ResultItemDefinition[] items)
    {
        List<BaseItem> result = new List<BaseItem>();
        List<BaseItem> l = null;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].itemType == BaseItem.ItemType2.Resource)
            {
                if (GetResource(items[i].tier - 1) >= items[i].stack)
                {
                    ChangeResource(items[i].tier - 1, -items[i].stack);
                    result.Add(items[i].GenerateItem(0, 0));
                }
            }

            l = GetPartyItems(n => n.itemType == items[i].itemType && n.Tier == items[i].tier);
            if (l.Count > 0)
            {
                if (l[0].IsStackable())
                {
                    // stackable item
                    if (l[0].currentStack > items[i].stack) // when there are more items in camp storage than we need
                    {
                        result.Add(l[0].Split(items[i].stack)); // remove the number of items from the camp storage
                    }
                    else // there are equal or to few items in storage
                    {
                        partyStorage.Remove(l[0]);  // remove the entire stack
                        result.Add(l[0]);
                    }
                }
                else
                {
                    // not stackable but just take first found item
                    partyStorage.Remove(l[0]); // remove the item from storage
                    result.Add(l[0]);
                }
            }
        }
        return result; // return all items from storage, that matched the item definitions. might not be as many items as asked for
    }
    // used by crafting system

    /// <summary>
    /// Create Items to fit the Item Definitions. Uses camp resources when required in item definition 
    /// </summary>
    /// <param name="items"></param>
    public void AddPartyItems(ResultItemDefinition[] items)
    {
        List<BaseItem> l = null;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].itemType == BaseItem.ItemType2.Resource)
            {
                ChangeResource(items[i].tier - 1, items[i].stack);
                continue;
            }

            l = GetPartyItems(n => n.itemType == items[i].itemType && n.Tier == items[i].tier);
            if (!BaseItem.IsStackable(items[i].itemType) || l.Count == 0)
            {
                // create item
                AddPartyItem(items[i].GenerateItem(0, 0), false, false);
            }
            else
            {
                l[0].currentStack += items[i].stack;
            }
        }
    }
    public void ClearItems()
    {
        foreach (var item in partyStorage)
        {
            //UnregistierItem(item);
            Destroy(item);

        }
        //partyStorage.ForEach(n => Destroy(n));
        partyStorage.Clear();
    }

    public int AddPartyItem(BaseItem item, bool showNotification = false, bool updateInvGui = true)
    {
        ClearNullEntries();
        //int result = 0;


        if (item.itemType == BaseItem.ItemType2.Resource)
        {
            ChangeResource(item.level, item.currentStack, false, showNotification);
            //UnregistierItem(item);
            Destroy(item);

            return 0; // success
        }
        if (item.itemType == BaseItem.ItemType2.MiscRecipe)
        {
            AddRecipe(item.level, true);
            Destroy(item);

            return 0; // success
        }
        if (item.itemType == BaseItem.ItemType2.Resource)
        {
            ChangeResource(item.level, item.currentStack, false, showNotification);
            //UnregistierItem(item);
            Destroy(item);

            return 0; // success
        }

        // is stackable?
        if (item.IsStackable())
        {
            BaseItemDefinition bid = item.GenerateDefinition();
            //try find item
            for (int i = 0; i < partyStorage.Count; i++)
            {
                if (partyStorage[i].itemType == item.itemType && partyStorage[i].Tier == item.Tier)
                {
                    // fill up stack -> get max stack size that fits into inventory
                    int diff = partyStorage[i].GetMaxStack() - partyStorage[i].currentStack;
                    diff = Mathf.Min(diff, item.currentStack);
                    if (diff > 0)
                    {
                        partyStorage[i].currentStack += diff;
                        item.currentStack -= diff;

                    }
                    Destroy(item);
                    //// remaining stack is either 0 or will be transfered to the camp
                    //if (item.currentStack <= 0)
                    //{
                    //	//UnregistierItem(item);
                    //	Destroy(item);
                    //}
                    //else
                    //{
                    //	AddCampItem(item);
                    //	if (showNotification)
                    //	{
                    //		MainGuiController.instance.notificationPanel.ShowItemNotification(bid);
                    //	}
                    //	if (updateInvGui)
                    //	{
                    //		MainGuiController.instance.inventoryGui.InventoryUpdated();
                    //	}
                    //	return 1; // success with excess moved to camp
                    //}
                    if (showNotification)
                    {
                        MainGuiController.instance.notificationPanel.ShowItemNotification(bid);
                    }
                    if (updateInvGui)
                    {
                        MainGuiController.instance.inventoryGui.InventoryUpdated();
                    }
                    BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.ItemObtained, (int)item.itemType, item.Tier });
                    return 0; // success
                }
            }
            // add new item
            partyStorage.Add(item);
            if (showNotification)
            {
                MainGuiController.instance.notificationPanel.ShowItemNotification(item.GenerateDefinition());
            }
            if (updateInvGui)
            {
                MainGuiController.instance.inventoryGui.InventoryUpdated();
            }
            BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.ItemObtained, (int)item.itemType, item.Tier });
            return 0; // success
        }
        else
        {
            // add new item
            partyStorage.Add(item);
            if (showNotification)
            {
                MainGuiController.instance.notificationPanel.ShowItemNotification(item.GenerateDefinition());
            }
            if (updateInvGui)
            {
                MainGuiController.instance.inventoryGui.InventoryUpdated();
            }
            BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.ItemObtained, (int)item.itemType, item.Tier });
            return 0; // success
        }
        //return -1; // failure
    }
    /// <summary>
    /// adds a bunch of items to the inventory at once, returns the amount of placed items, which may be less then the total items if some got moved to the camp inv
    /// </summary>
    /// <param name="items"></param>
    /// <param name="showNotification"></param>
    /// <param name="updateInvGui"></param>
    /// <returns></returns>
    public int AddPartyItems(List<BaseItem> items, bool showNotification = false, bool updateInvGui = true)
    {
        int result = 0;
        int c = items.Count;
        for (int i = 0; i < c; i++)
        {
            if (AddPartyItem(items[i], showNotification, updateInvGui) == 0)
            {
                result += 1;
            }
        }
        return result;
    }
    private void ClearNullEntries()
    {
        partyStorage.RemoveAll(n => n == null);
        campStorage.RemoveAll(n => n == null);
    }

    public void AddCampItem(BaseItem item)
    {
        ClearNullEntries();

        // stackable
        if (item.IsStackable())
        {
            //try find item
            for (int i = 0; i < campStorage.Count; i++)
            {
                if (campStorage[i].itemType == item.itemType && campStorage[i].Tier == item.Tier)
                {
                    // merge stack
                    campStorage[i].currentStack += item.currentStack;
                    //UnregistierItem(item);
                    Destroy(item);
                    return;
                }
            }
        }
        else
        {
            campStorage.Add(item);
        }
    }
    public static int ItemSort(BaseItem a, BaseItem b)
    {
        if (a.itemType == b.itemType)
        {
            int a1 = a.SortValue1();
            int b1 = b.SortValue1();
            if (a1 == b1)
            {
                int a2 = a.SortValue2();
                int b2 = b.SortValue2();
                if (a2 == b2)
                {
                    return b.age.CompareTo(a.age);
                }
                else
                {
                    return b2.CompareTo(a2);
                }
            }
            else
            {
                return b1.CompareTo(a1);
            }
        }
        else
        {
            return BaseItem.sortTable[a.itemType].CompareTo(BaseItem.sortTable[b.itemType]);
        }
    }

    #endregion

    #region base related
    public int GetRoomLevel(CampRoomType type)
    {
        for (int i = currentRooms.Count - 1; i >= 0; i--)
        {
            if (currentRooms[i].type == type)
                return Mathf.Clamp(currentRooms[i].currentLevel, 0, 3);
        }
        return 0;
    }
    public bool GetRoomLocked(CampRoomType type)
    {
        for (int i = currentRooms.Count - 1; i >= 0; i--)
        {
            if (currentRooms[i].type == type)
                return currentRooms[i].locked;
        }
        return true;
    }
    public void SetRoomLocked(CampRoomType type, bool value)
    {
        // update ui layer
        if (BountyManager.instance.campScene.gameObject.activeInHierarchy)
        {
            BountyManager.instance.campScene.baseCam.GetAnchor(type).gameObject.SetActive(!value);
        }

        for (int i = currentRooms.Count - 1; i >= 0; i--)
        {
            if (currentRooms[i].type == type)
            {
                currentRooms[i].locked = value;
                return;
            }
        }
        // if no room was found add the new entry
        currentRooms.Add(new CampRoomEntry() { type = type, currentLevel = 0, locked = value });
    }
    public void UnlockRoom(CampRoomType type, bool showNotification)
    {
        SetRoomLocked(type, false);

        if (showNotification)
            MainGuiController.instance.notificationPanel.ShowNotification("Info_RoomUnlocked", new FormatTextToken[] { new FormatTextToken("Room_" + type.ToString(), true) }, "building_unlocked", true, new SpriteData("icon_" + type.ToString().ToLowerInvariant(), 2));
    }
    public void LockRoom(CampRoomType type, bool showNotification)
    {
        SetRoomLocked(type, true);

        if (showNotification)
            MainGuiController.instance.notificationPanel.ShowNotification("Info_RoomUnlocked", new FormatTextToken[] { new FormatTextToken("Room_" + type.ToString(), true) }, "building_unlocked", true, new SpriteData("icon_" + type.ToString().ToLowerInvariant(), 2));
    }
    /// <summary>
    /// increases room level by 1. if room does not exists it will be added and set to level 1. returns the result room level
    /// </summary>
    /// <param name="type">the room type to upgrade</param>
    public int UpgradeRoom(CampRoomType type)
    {
        for (int i = 0; i < currentRooms.Count; i++)
        {
            if (currentRooms[i].type == type)
            {
                currentRooms[i].currentLevel += 1;
                BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.RoomUpgraded, (int)type, currentRooms[i].currentLevel });
                return currentRooms[i].currentLevel;
            }
        }
        currentRooms.Add(new CampRoomEntry() { type = type, currentLevel = 1, locked = false });
        BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.RoomUpgraded, (int)type, 1 });
        return 1;
    }
    public void SetRoomLevel(CampRoomType type, int value)
    {
        for (int i = 0; i < currentRooms.Count; i++)
        {
            if (currentRooms[i].type == type)
            {
                currentRooms[i].currentLevel = value;
                //BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.RoomUpgraded, type, currentRooms[i].currentLevel });
                return;
            }
        }
        currentRooms.Add(new CampRoomEntry() { type = type, currentLevel = value, locked = false });
        //BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.RoomUpgraded, type, 1 });
    }
    public CampRoomDefinition GetRoomDefinition(CampRoomType type)
    {
        for (int i = 0; i < roomDefinitions.Length; i++)
        {
            if (roomDefinitions[i].type == type)
                return roomDefinitions[i];
        }
        return null;
    }

    public List<CampRoomEntry> CurrentRooms
    {
        get { return new List<CampRoomEntry>(currentRooms); }
    }
    #endregion

    #region job related

    public JobDefinition GetJobDefinition(CampRoomType type)
    {
        for (int i = 0; i < jobDefinitions.Length; i++)
        {
            if (jobDefinitions[i].type == type)
            {
                return jobDefinitions[i];
            }
        }
        return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="roomType"></param>
    /// <param name="cat"></param>
    /// <param name="slot"></param>
    /// <returns>0 = possible, 1 = no because room type, 2 no because resources, 3 because no talent, 4 because char is moody, 5 because too many workers, 6 because room level insufficent, 7 because recipe not learned</returns>
    public int IsJobPossible(CampRoomType roomType, int cat, int slot, BountyCharacter c, int recipeIndex)
    {
        JobDefinition jd = GetJobDefinition(roomType);
        int maxWorker = GetRoomLevel(roomType) >= 1 ? GetRoomDefinition(roomType).levels[GetRoomLevel(roomType) - 1].maxWorker : 1;
        int resourceSituation = CheckResourcesWithMissables(jd.categories[cat].actions[slot].GetMainRecipe().GetAdjustedResourceCost());

        if (GetRoomLevel(roomType) < jd.categories[cat].actions[slot].roomMinLevel)
            return 1;

        if (jd.categories[cat].isRoomUpgrade && GetRoomLevel(roomType) > jd.categories[cat].actions[slot].roomMinLevel)
            return 6;

        if (c == null || c.HasState(CharacterBaseState.Moody) || c.HasState(CharacterBaseState.Infected))
            return 4;

        if (!string.IsNullOrEmpty(jd.passiveSkill) && c != null)
        {
            if (c.GetSkillLevel(jd.passiveSkill) < jd.categories[cat].actions[slot].talentMinLevel)
                return 3;
        }
        else if (jd.talent != BountyTalentType.None && c != null)
        {
            if (c.GetTalentLevel(jd.talent) < jd.categories[cat].actions[slot].talentMinLevel)
                return 3;
        }

        if (jd.categories[cat].actions[slot].isCrafting && jd.categories[cat].actions[slot].recipeTemplates.Length > 0)
        {
            bool tCheck = false;
            foreach (var rec in jd.categories[cat].actions[slot].recipeTemplates)
            {
                if (IsRecipeKnown(rec))
                {
                    tCheck = true;
                    break;
                }
            }
            if (!tCheck)
                return 7;
        }

        if (resourceSituation == -1)
            return 2;

        BountyCraftingRecipe bcr = jd.categories[cat].actions[slot].GetMainRecipe();
        if (jd.categories[cat].actions[slot].recipeTemplates.Length > 0)
            bcr = jd.categories[cat].actions[slot].GetKnownRecipe(recipeIndex);
        if (!CheckItems(bcr.itemInput))
            return 2;


        if (GetPeopleWithJob(roomType, jd.categories[cat].isRoomUpgrade ? 1 : 0).Count >= maxWorker)
            return 5;

        if (jd.categories[cat].actions[slot].maxPerDay > 0 && GetJobExecutions(roomType, cat, slot) >= jd.categories[cat].actions[slot].maxPerDay)
            return 5;


        return 0;
    }

    public bool IsJobAssignPossible(BountyCharacter c, CampRoomType roomType, int cat, int slot, int recipeIndex)
    {
        if (c == null)
            return false;


        JobDefinition jd = GetJobDefinition(roomType);
        int maxWorker = GetRoomLevel(roomType) >= 1 ? GetRoomDefinition(roomType).levels[GetRoomLevel(roomType) - 1].maxWorker : 1;
        int resourceSituation = CheckResourcesWithMissables(jd.categories[cat].actions[slot].GetMainRecipe().GetAdjustedResourceCost());

        if (GetRoomLevel(roomType) < jd.categories[cat].actions[slot].roomMinLevel)
            return false;

        if (c.HasState(CharacterBaseState.Moody) || c.HasState(CharacterBaseState.Infected))
            return false;

        if (!string.IsNullOrEmpty(jd.passiveSkill) && c != null)
        {
            if (c.GetSkillLevel(jd.passiveSkill) < jd.categories[cat].actions[slot].talentMinLevel)
                return false;
        }
        else if (jd.talent != BountyTalentType.None && c != null)
        {
            if (c.GetTalentLevel(jd.talent) < jd.categories[cat].actions[slot].talentMinLevel)
                return false;
        }

        if (jd.categories[cat].actions[slot].isCrafting && jd.categories[cat].actions[slot].recipeTemplates.Length > 0)
        {
            bool tCheck = false;
            foreach (var rec in jd.categories[cat].actions[slot].recipeTemplates)
            {
                if (IsRecipeKnown(rec))
                {
                    tCheck = true;
                    break;
                }
            }
            if (!tCheck)
                return false;
        }

        if (resourceSituation == -1)
            return false;

        BountyCraftingRecipe bcr = jd.categories[cat].actions[slot].GetMainRecipe();
        if (jd.categories[cat].actions[slot].recipeTemplates.Length > 0)
            bcr = jd.categories[cat].actions[slot].GetKnownRecipe(recipeIndex);
        if (!CheckItems(bcr.itemInput))
            return false;

        if (GetPeopleWithJob(roomType, jd.categories[cat].isRoomUpgrade ? 1 : 0).Count >= maxWorker)
            return false;

        if (jd.categories[cat].actions[slot].maxPerDay > 0 && GetJobExecutions(roomType, cat, slot) >= jd.categories[cat].actions[slot].maxPerDay)
            return false;

        return true;
    }

    public bool AssignJob(BountyCharacter c, CampRoomType type, int cat, int slot, bool continiously = true, string specInfo = null, int recipeId = -1)
    {
        JobInstance next = activeJobs.Find(n => n.character == c);
        JobDefinition jd = GetJobDefinition(type);
        BaseNavNode.NodeType node = BaseNavNode.NodeType.Station;
        bool tConti = continiously && jd.continiously;
        int maxWorker = GetRoomLevel(type) >= 1 ? GetRoomDefinition(type).levels[GetRoomLevel(type) - 1].maxWorker : 1;
        if (jd.categories[cat].isRoomUpgrade)
        {
            maxWorker = 1;
            node = BaseNavNode.NodeType.Construction;
        }

        if (next != null)
        {
            return false; // char already has a job
        }
        else
        {
            BountyCraftingRecipe bcr = jd.categories[cat].actions[slot].GetMainRecipe();
            if (recipeId != -1)
                bcr = BountyManager.instance.craftingDatabase.GetAsset(recipeId).recipe;
            BountyResourceEntry[] tResources = bcr.GetAdjustedResourceCost();
            int resourceSituation = CheckResourcesWithMissables(tResources);
            if (GetPeopleWithJob(type, jd.categories[cat].isRoomUpgrade ? 1 : 0).Count >= maxWorker)
            {
                return false; // job count for this job already reached
            }
            else if (resourceSituation == -1)
            {
                return false; // not enough resources
            }
            else if (c.HasState(CharacterBaseState.Moody) || c.HasState(CharacterBaseState.Infected))
            {
                return false;
            }
            else
            {

                RemovePartyItems(bcr.itemInput);
                List<BountyResourceEntry> missings = TakeResourcesWithMissables(tResources);

                if (resourceSituation != 1)
                    tConti = false;
                c.Job = type;
                c.LastJob = type;
                if (BountyManager.instance.InCamp)
                {

                    activeJobs.Add(new JobInstance() { character = c, type = type, cat = cat, slot = slot, startTime = BountyManager.instance.DateTime, continiously = tConti, specialInfo = specInfo, hasMissingResources = (resourceSituation == 0), missingResources = missings, recipeId = recipeId });
                }
                else if (BountyManager.instance.InStevenCampBool)
                {
                    BountyManager.instance.NonPlayerBase.activeJobs.Add(new JobInstance() { character = c, type = type, cat = cat, slot = slot, startTime = BountyManager.instance.DateTime, continiously = tConti, specialInfo = specInfo, hasMissingResources = (resourceSituation == 0), missingResources = missings, recipeId = recipeId });
                }
                c.startNavMode = 1;
                c.goToWork = true;
                c.startNodeType = node;
                c.startNodeStation = BaseNavNode.GetStationFromRoom(type);
                if (BountyManager.instance.InCamp)
                {
                    if (BountyManager.instance.CurrentTutorialIndex < 0)
                        BountyManager.instance.campScene.UpdateCharacter(c, c.startNodeType, c.startNodeStation, c.startNavMode = 1, 2, true, 0, true);

                }
                else if (BountyManager.instance.InStevenCampBool)
                {
                    BountyManager.instance.CBase.GetComponent<BountyBase>().UpdateCharacter(c, c.startNodeType, c.startNodeStation, c.startNavMode = 1, 2, true, 0, true);

                }
                if (jd.categories[cat].isRoomUpgrade)
                {
                    if (BountyManager.instance.InCamp)
                    {

                        BountyManager.instance.campScene.UpdateRoomUpgradeModel(type, true, c.female ? 1.14f : 1.1f);
                    }
                    else
                    {
                        BountyManager.instance.CBase.GetComponent<BountyBase>().UpdateRoomUpgradeModel(type, true, c.female ? 1.14f : 1.1f);

                    }
                }

                // update merchant active state
                BountyManager.instance.Variables.SetVariable("@HomeBaseMerchantActive", GetPeopleWithJob(CampRoomType.Merchant, 0).Count > 0);

                BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.JobAssigned, c.uniqueId, (int)type, cat, slot });
                return true;
            }
        }
    }

    /// <summary>
    /// job removed
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public bool RemoveJob(BountyCharacter c, bool moodyAbort = false)
    {
        JobInstance next = activeJobs.Find(n => n.character == c);

        if (next != null)
        {
            JobDefinition jd = GetJobDefinition(next.type);
            c.Job = CampRoomType.None;
            c.LastJob = CampRoomType.None;
            activeJobs.Remove(next);
            c.startNavMode = 2;
            c.goToWork = false;
            c.startNodeType = BaseNavNode.NodeType.Idle;
            c.startNodeStation = BaseNavNode.StationType.Any;

            if (c.Model != null && !BountyManager.instance.IsFightActive())
            {
                c.Model.myNavAgent.masterState = c.startNavMode;
                c.Model.myNavAgent.targetType = c.startNodeType;
                c.Model.myNavAgent.targetStation = c.startNodeStation;
                c.Model.myNavAgent.state = 1;
                c.Model.RemoveAllAniStates();
                c.Model.myAudio.StopLoop(null, 1);
                c.Model.CurrentIdleData = new BaseIdleData[0];
            }

            if (jd.categories[next.cat].actions[next.slot].specialFunction == SpecialJobFunction.Training)
            {
                StopTraining(c, next);
            }

            BountyCraftingRecipe bcr = jd.categories[next.cat].actions[next.slot].GetMainRecipe();
            if (next.recipeId != -1)
                bcr = BountyManager.instance.craftingDatabase.GetAsset(next.recipeId).recipe;

            AddPartyItems(bcr.itemInput);

            BountyResourceEntry[] tResources = bcr.GetAdjustedResourceCost();

            if (next.hasMissingResources)
            {
                BountyResourceEntry[] actualChange = new BountyResourceEntry[tResources.Length];
                // copy values
                for (int i = 0; i < actualChange.Length; i++)
                {
                    actualChange[i] = new BountyResourceEntry(tResources[i].type, tResources[i].amount);
                }
                // subtract missings
                for (int i = 0; i < actualChange.Length; i++)
                {
                    BountyResourceEntry found = next.missingResources.Find(n => n.type == actualChange[i].type);
                    if (found != null)
                    {
                        actualChange[i].amount -= found.amount;
                    }
                }
                // apply resource change
                GiveResources(actualChange);
            }
            else
            {
                GiveResources(tResources);
            }

            if (moodyAbort)
            {
                MainGuiController.instance.notificationPanel.ShowNotification("Info_RoomJob_Canceled_Mood", new FormatTextToken[] { next.character.CharNameToken, new FormatTextToken("Room_" + jd.type, true) }, null, false);
            }

            if (jd.categories[next.cat].isRoomUpgrade)
                BountyManager.instance.campScene.UpdateRoomUpgradeModel(next.type, false, 1f);

            // update merchant active state
            BountyManager.instance.Variables.SetVariable("@HomeBaseMerchantActive", GetPeopleWithJob(CampRoomType.Merchant, 0).Count > 0);

            return true;
        }
        else
        {
            return false;
        }
    }
    /// <summary>
    /// called when a day session is over. updates all active jobs and yields their results when time is up
    /// </summary>
    public void CheckJobsProgress()
    {
        BountyManager.instance.Variables.SetVariable("@ReportWaterProd", 0);
        BountyManager.instance.Variables.SetVariable("@ReportFoodProd", 0);
        BountyManager.instance.Variables.SetVariable("@ReportCraftedNum", 0);
        JobDefinition jd;
        //List<JobInstance> done = new List<JobInstance>();
        for (int i = 0; i < activeJobs.Count; i++)
        {
            jd = GetJobDefinition(activeJobs[i].type);
            if (activeJobs[i].startTime + jd.categories[activeJobs[i].cat].actions[activeJobs[i].slot].duration <= BountyManager.instance.DateTime)
            {
                // time is over
                ExecuteJob(activeJobs[i]);
            }
        }
        jobExecutions.Clear();

    }
    /// <summary>
    /// called after the session calculation is completed, clears done jobs and reassings the continuing jobs
    /// </summary>
    public void CheckJobExpiration()
    {
        JobDefinition jd;
        List<JobInstance> done = new List<JobInstance>();
        for (int i = 0; i < activeJobs.Count; i++)
        {
            jd = GetJobDefinition(activeJobs[i].type);
            if (activeJobs[i].startTime + jd.categories[activeJobs[i].cat].actions[activeJobs[i].slot].duration <= BountyManager.instance.DateTime)
            {
                // time is over
                if (!activeJobs[i].continiously || jd.categories[activeJobs[i].cat].isRoomUpgrade) // check for continuing jobs
                {
                    done.Add(activeJobs[i]);
                    activeJobs[i].stopingReason = 1;

                    // missing resource malus
                    if (activeJobs[i].hasMissingResources && !activeJobs[i].character.mainCharacter)
                    {
                        activeJobs[i].character.AddState(CharacterBaseState.Moody, 1);
                    }
                }
                else
                {
                    BountyCraftingRecipe bcr = jd.categories[activeJobs[i].cat].actions[activeJobs[i].slot].GetMainRecipe();
                    if (activeJobs[i].recipeId != -1)
                        bcr = BountyManager.instance.craftingDatabase.GetAsset(activeJobs[i].recipeId).recipe;
                    BountyResourceEntry[] tResources = bcr.GetAdjustedResourceCost();
                    // check resources
                    if (CheckResources(tResources) && CheckItems(bcr.itemInput))
                    {
                        RemovePartyItems(bcr.itemInput);
                        TakeResources(tResources);
                        activeJobs[i].startTime = BountyManager.instance.DateTime;
                    }
                    else
                    {
                        done.Add(activeJobs[i]);
                        activeJobs[i].stopingReason = 2;
                    }
                }
            }
        }
        // removed finished jobs
        for (int i = 0; i < done.Count; i++)
        {
            jd = GetJobDefinition(done[i].type);
            done[i].character.Job = CampRoomType.None;
            activeJobs.Remove(done[i]);
            if (done[i].stopingReason == 2)
            {
                MainGuiController.instance.notificationPanel.ShowNotification("Info_RoomJob_Canceled", new FormatTextToken[] { done[i].character.CharNameToken, new FormatTextToken("Room_" + jd.type, true) }, null, false);
            }

            BountyManager.instance.StartCoroutine(DelayedJobEnd(done[i].character, jd.categories[done[i].cat].isRoomUpgrade ? done[i].type : CampRoomType.None));

            // re add main char to party
            if (done[i].character.mainCharacter && !IsInParty(done[i].character))
            {
                SwitchPartyState(done[i].character, true);
            }
        }
    }

    private IEnumerator DelayedJobEnd(BountyCharacter bc, CampRoomType constuction)
    {
        yield return new WaitForSeconds(5f);

        if (bc && bc.Model && bc.Model.myNavAgent.state == 0)
        {
            bc.startNodeIndex = -1;
            bc.startNavMode = 2;
            bc.goToWork = false;
            bc.startNodeType = BaseNavNode.NodeType.Idle;
            bc.startNodeStation = BaseNavNode.StationType.Any;
            bc.Model.myNavAgent.masterState = bc.startNavMode;
            bc.Model.myNavAgent.targetType = bc.startNodeType;
            bc.Model.myNavAgent.targetStation = bc.startNodeStation;
            bc.Model.myNavAgent.state = 1;
            //bc.Model.StopIdleData();
            bc.Model.RemoveAllAniStates();
            bc.Model.myAudio.StopLoop(null, 1);
            bc.Model.ClearBaseIdleObject(); // added 16.9.19
            bc.Model.CurrentIdleData = new BaseIdleData[0];
        }
        // update deco
        if (constuction != CampRoomType.None)
            BountyManager.instance.campScene.UpdateRoomUpgradeModel(constuction, false, 1f);
    }

    private void ExecuteJob(JobInstance ji)
    {
        JobDefinition jd = GetJobDefinition(ji.type);

        if (jd.categories.Count > 0 && jd.categories[ji.cat].actions.Length > 0)
        {
            JobActionDefinition jad = jd.categories[ji.cat].actions[ji.slot];

            if (jad.specialFunction != SpecialJobFunction.None)
            {
                ExecuteSpecialJob(jad.specialFunction, ji);
            }
            else if (jd.categories[ji.cat].isRoomUpgrade)
            {
                int rl = UpgradeRoom(jd.type);
                ji.character.AddXp((int)(50 * ji.GetTier() * ji.character.PerkBoostFactor(PerkBoostType.ExperienceInJobs)));
                // play room sound?
                //MainGuiController.instance.notificationPanel.ShowNotification("Info_RoomUpgraded_" + rl, new FormatTextToken[] { new FormatTextToken("Room_" + jd.type, true), ji.character.CharNameToken, new FormatTextToken((jd.xpGain * ji.GetTier()).ToString(), false) }, null, false);
            }
            else if (jad.isCrafting)
            {
                BountyCraftingRecipe bcr = BountyManager.instance.craftingDatabase.GetAsset(ji.recipeId).recipe;
                CraftItem(bcr, ji);
            }

            if (jad.specialFunction != SpecialJobFunction.SleepHeal)
                MainGuiController.instance.notificationPanel.ShowJobNotification(ji, false);

            // apply resource yield
            BountyResourceRangeEntry[] yields = jad.resourceYield;
            int tAmount = 0;
            for (int i = 0; i < yields.Length; i++)
            {
                tAmount = SDRandom.Range(yields[i].amountMin, yields[i].amountMax + 1);
                if (jad.isMerchant)
                {
                    tAmount += 5 * (ji.GetTier() - 1);
                    if (BaseInhabitants.Count > 10)
                    {
                        if (ji.GetTier() < 2)
                            tAmount += 3;
                        else
                            tAmount += 5;
                    }
                }
                ChangeResource(yields[i].type, tAmount, false, false, true);
            }
            // apply xp yield
            if (!jd.categories[ji.cat].isRoomUpgrade)
                ji.character.AddXp((int)(jd.xpGain * ji.GetTier() * ji.character.PerkBoostFactor(PerkBoostType.ExperienceInJobs)));
            BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.JobExecuted, (int)ji.type, ji.cat, ji.slot });
        }


    }

    private void ExecuteSpecialJob(SpecialJobFunction f, JobInstance job)
    {
        if (f == SpecialJobFunction.Scouting)
        {
            int level = 1;
            int cat = 0;
            if (!string.IsNullOrEmpty(job.specialInfo))
            {
                Dictionary<string, string> compounds = BountyExtensions.ParseCompoundString(job.specialInfo);
                if (compounds != null && compounds.ContainsKey("Level"))
                {
                    string r = compounds["Level"];
                    level = int.Parse(r, System.Globalization.CultureInfo.InvariantCulture);
                    r = compounds["LocationType"];
                    cat = int.Parse(r, System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            BountyManager.instance.Map.Scout((LootLocationCategory)cat, level);
        }
        else if (f == SpecialJobFunction.SleepHeal)
        {
            job.character.HealthPercent = 100;
        }
        else if (f == SpecialJobFunction.BarDrink)
        {

            int tier = GetRoomLevel(CampRoomType.Bar);
            Vector2Int drinkBonus = GetBarDrinkBonus(tier, job.slot);
            //Debug.Log("Character "+job.character.name+" is Drinking at the Bar. Morale Set to "+ drinkBonus.y+" with perk: "+ (int)(drinkBonus.y * job.character.PerkRegenerationBoostFactor(PerkRegenerationType.BarDrinkingMoral)));
            job.character.Morale += (int)(drinkBonus.y * job.character.PerkBoostFactor(PerkBoostType.BarDrinkingMoral)); // boost Morale if character has a Drinking Perk
            job.character.Stress += (int)(drinkBonus.x * job.character.PerkBoostFactor(PerkBoostType.BarDrinkingStress)); // boost Stress reduction if character has a Stress reduction Perk
                                                                                                                          //Stress is only reduced when drinking. (12.05.2023)
        }
        else if (f == SpecialJobFunction.Recreation)
        {
            int tier = GetRoomLevel(CampRoomType.Recreation);
            Vector2Int drinkBonus = GetRecreationBonus(tier, job.slot);
            job.character.Morale += drinkBonus.y;
            job.character.Stress += drinkBonus.x;
        }
        else if (f == SpecialJobFunction.Training)
        {
            Dictionary<string, string> table = BountyExtensions.ParseCompoundString(job.specialInfo);
            int rank = int.Parse(table["rank"]);
            CharacterClass pClass = (CharacterClass)int.Parse(table["class"]);
            job.character.charClass = pClass;
            job.character.rank = rank + 1;
            List<CharSkillEntry> skills = GetTrainingSkillRewards(pClass, rank);
            foreach (var item in skills)
            {
                job.character.ChangeSkillLevel(item.skill, item.value, true, false);
            }
            List<CharAttributeEntry> stats = GetTrainingAttributeRewards(pClass, rank);
            foreach (var item in stats)
            {
                job.character.ChangeAttributeRaw(item.attribute, item.value);
            }
            foreach (var item in job.storedItems)
            {
                if (item.itemType == BaseItem.ItemType2.Resource) // destroy resource storage
                {
                    Destroy(item);
                }
                else if (item.IsType(BaseItem.ItemType2.Gear)) // equip stored items
                {
                    BaseItem bi = job.character.RemoveEquippedItem(item.GetSubType());//If I get this right, a character could have an inferior version of an item already equipped. Like a not good sword. 
                    if (bi != null) // the not good sword is taken away from the character and added to the player camp's item storage.
                        AddPartyItem(bi, false);
                    job.character.AddEquipmentItem(item); // give the character the (improved) item that matches their new rank and class. 
                                                          //which items a character revieces through training is determined in BountyCamp.Extended.GetTrainingItemRequirements
                }
            }
            job.storedItems.Clear();
        }
    }
    private void CraftItem(BountyCraftingRecipe recipe, JobInstance job)
    {
        int[] mods = null;
        if (!string.IsNullOrEmpty(job.specialInfo))
        {
            Dictionary<string, string> compounds = BountyExtensions.ParseCompoundString(job.specialInfo);
            if (compounds != null && compounds.ContainsKey("CraftingMods"))
            {
                string r = compounds["CraftingMods"];
                r = r.Replace(":", ",");
                mods = BountyExtensions.ParseIntArray(r);
            }
        }

        int itemLvl = 1 + job.character.GetPassiveItemBonus(PassiveSkillEffect.IncreaseCraftingLevel, recipe.result.itemType, recipe.result.tier);
        BaseItem bi = recipe.result.GenerateItem(itemLvl, itemLvl, 0, mods);
        bi.currentStack += Mathf.RoundToInt((float)bi.currentStack * (float)job.character.GetPassiveItemBonus(PassiveSkillEffect.IncreaseCraftingYield, recipe.result.itemType, recipe.result.tier) / 100f);

        AddPartyItem(bi);
        BountyManager.instance.Variables.SetVariable("@ReportCraftedNum", BountyManager.instance.Variables.GetVariable("@ReportCraftedNum").AsInt() + 1);
        BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.ItemCrafted, (int)bi.itemType, bi.Tier, bi.currentStack });
    }
    /// <summary>
    /// returns a list of characters that are assigned to jobs in the given room
    /// </summary>
    /// <param name="type">job type</param>
    /// <param name="contructionjob">0 = no constructions, 1 = only constructions, 2 = both</param>
    /// <returns>list of characters</returns>
    public List<BountyCharacter> GetPeopleWithJob(CampRoomType type, int contructionjob)
    {
        List<BountyCharacter> result = new List<BountyCharacter>();
        JobDefinition jd;
        for (int i = 0; i < activeJobs.Count; i++)
        {
            jd = GetJobDefinition(activeJobs[i].type);
            if (activeJobs[i].type == type && (contructionjob == 2 || (contructionjob == 1) == jd.categories[activeJobs[i].cat].isRoomUpgrade) && activeJobs[i].character != null && activeJobs[i].character.Health > 0)
            {

                result.Add(activeJobs[i].character);
            }

        }
        return result;
    }
    public int GetJobExecutions(CampRoomType type, int cat, int slot)
    {
        int result = 0;
        for (int i = 0; i < jobExecutions.Count; i++)
        {
            if (jobExecutions[i].x == (int)type && jobExecutions[i].y == cat && jobExecutions[i].z == slot)
                result++;
        }
        return result;
    }
    /// <summary>
    /// returns the respective JobInstance object if the given character is assigned to job or null otherwise
    /// </summary>
    /// <param name="c">character to check for</param>
    /// <returns>JobInstance or null</returns>
    public JobInstance GetActiveJob(BountyCharacter c)
    {
        for (int i = 0; i < activeJobs.Count; i++)
        {
            if (activeJobs[i].character == c)
                return activeJobs[i];
        }
        return null;
    }
    public List<JobInstance> GetActiveJobs()
    {
        return new List<JobInstance>(activeJobs);
    }
    public List<JobInstance> GetActiveJobs(CampRoomType type)
    {
        return new List<JobInstance>(activeJobs.FindAll(n => n.type == type));
    }
    public void ClearActiveJobs()
    {
        activeJobs.Clear();
    }
    /// <summary>
    /// returns the characters job tier / level based on the job specific talent OR the room level if talent is defined as "None" value ranges from 1-3 or 0 if not learned
    /// </summary>
    /// <param name="type"></param>
    /// <param name="character"></param>
    /// <returns></returns>
    public int GetJobTier(CampRoomType type, BountyCharacter character)
    {
        JobDefinition jd = GetJobDefinition(type);
        if ((jd.talent == BountyTalentType.None && string.IsNullOrEmpty(jd.passiveSkill)) || type == CampRoomType.RadioStation)
        {
            return GetRoomLevel(type);
        }

        if (character == null)
            return 1;

        for (int i = jd.jobTiers.Length - 1; i >= 0; i--)
        {
            if (character.GetSkillLevel(jd.passiveSkill) >= jd.jobTiers[i])
            {
                // return i + 1;
                return Mathf.Min(i + 1, GetRoomLevel(type));
            }
        }

        return 1;
    }


    public bool RefillMissingJobResources()
    {
        // refill algorithm:
        // 1 search active jobs with missing resources
        // 2 sort by missing one or two types of resource
        // 3 then sort by amount of resource missing
        // 4 fill up missing amounts and reset missing state
        List<string> refilledJobbers = new List<string>();
        List<JobInstance> preList = activeJobs.FindAll(n => n.hasMissingResources);
        List<JobInstance> sortList = new List<JobInstance>();
        int c = preList.Count;
        int j;
        for (int i = 0; i < c; i++) // insertion sort into new list
        {
            j = i;
            while (j > 0 && (preList[i].missingResources.Count != preList[j - 1].missingResources.Count ? preList[i].missingResources.Count < preList[j - 1].missingResources.Count : preList[i].missingResources.SumAmount() < preList[j - 1].missingResources.SumAmount()))
            {
                j--;
            }
            if (j == i)
                sortList.Add(preList[i]);
            else
                sortList.Insert(j, preList[i]);
        }
        int exchange;
        foreach (var job in sortList)
        {
            for (int i = 0; i < job.missingResources.Count; i++)
            {
                exchange = Mathf.Min(job.missingResources[i].amount, GetResource(job.missingResources[i].type));
                if (exchange > 0)
                {
                    job.missingResources[i].amount -= exchange;
                    ChangeResource(job.missingResources[i].type, -exchange, false, false, false);
                }
            }
            if (job.missingResources.SumAmount() == 0)
            {
                job.hasMissingResources = false;
                refilledJobbers.Add(job.character.characterId);
            }
        }
        if (refilledJobbers.Count > 0)
        {
            BountyManager.instance.StartCoroutine(JobberRefillRoutine(refilledJobbers));
            return true;
        }
        else
        {
            return false;
        }
    }

    private IEnumerator JobberRefillRoutine(List<string> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            BountyManager.instance.Variables.SetVariable("@JobChar", list[i]);
            BountyManager.instance.dialogueManager.StartDialogue("Trish_JobResourceRefill");
            yield return new WaitWhile(() => BountyManager.instance.dialogueManager.DialogueActive);
        }
    }

    public bool CanUpgradeItem(BaseItem item)
    {
        BountyCraftingRecipe bcr = item.GetUpgradeCost();
        if (CheckResources(bcr.resourceCost) && CheckItems(bcr.itemInput))
        {
            if (item.level < 5)
            {
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// tries to upgrade an item return code for varius results: 1=success, -1=insufficient resources, -2=insufficient kits, -3 max level
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public int DoItemUpgrade(BaseItem item)
    {
        BountyCraftingRecipe bcr = item.GetUpgradeCost();
        if (CheckResources(bcr.resourceCost))
        {
            if (CheckItems(bcr.itemInput))
            {
                if (item.UpgradeLevel(1, item.level >= BaseItem.upgradeMaxLevels[0] ? 2 : 1))
                {
                    TakeResources(bcr.resourceCost);
                    RemovePartyItems(bcr.itemInput);
                    return 1;
                }
                else
                {
                    return -3;
                }
            }
            else
            {
                return -2;
            }
        }
        else
        {
            return -1;
        }

    }

    #endregion


    /// <summary>
    /// execute daily resource consumption in the camp
    /// </summary>
    public void CheckResourceConsumption()
    {
        FactionBase bb = BountyManager.instance.factionManager.PlayerFaction.bases[0];
        List<BountyCharacter> campCharacters = GetAllCampPeople(false);
        // check water - manage water consumption for the camp
        int campCharacterCount = campCharacters.Count;
        // how many chars can we supply with the given consumption per char
        int numberOfCharsThatCouldBeSupplied = Mathf.RoundToInt((float)GetResource(1) / BountyManager.instance.persistentManager.GetSurvivalOption(SurvivalOption.WaterCostFactor)); // added survival option 28.4.21
                                                                                                                                                                                     // how much resource is going to be consumed
        int numberOfCharsThatConsumedResource = 0;
        for (int i = campCharacterCount - 1; i >= 0; i--)
        {
            campCharacters[i].NewStates.Remove(CharacterBaseState.Thirsty);
            if (campCharacters[i].PerkChance(BountyPerkChanceTypes.NoWaterConsumtionPerDay))//checks if the Character does not drink water today, due to their perks
            {
                campCharacters[i].RemoveState(CharacterBaseState.Thirsty);
                campCharacters[i].moraleState.moraleEntries.RemoveAll(n => n.id == "Thirsty");
                continue; //not added to numberOfPeopleThatConsumedResource because this Character does not need to drink today -> not consuming water
            }

            // Characters that need to consume water:
            if (numberOfCharsThatConsumedResource < numberOfCharsThatCouldBeSupplied) // still enough water to drink
            {
                campCharacters[i].RemoveState(CharacterBaseState.Thirsty); // person drinks
                campCharacters[i].moraleState.moraleEntries.RemoveAll(n => n.id == "Thirsty");
                numberOfCharsThatConsumedResource++;
            }
            else // not enough water for someone else to drink
            {
                campCharacters[i].AddState(CharacterBaseState.Thirsty, -1); // person is now thirsty
                campCharacters[i].moraleState.moraleEntries.Add(new MoralStateEntry("Thirsty", -1, -30));
            }
        }
        int resourceAmountConsumed = Mathf.RoundToInt((float)numberOfCharsThatConsumedResource * BountyManager.instance.persistentManager.GetSurvivalOption(SurvivalOption.WaterCostFactor));
        BountyManager.instance.Variables.SetVariable("@ReportWaterConsum", Mathf.Min(GetResource(1), resourceAmountConsumed));
        ChangeResource(1, -Mathf.Min(GetResource(1), resourceAmountConsumed), false, false, true);


        // check food - manage food consumption for the camp
        campCharacterCount = campCharacters.Count;
        numberOfCharsThatCouldBeSupplied = Mathf.RoundToInt((float)GetResource(2) / BountyManager.instance.persistentManager.GetSurvivalOption(SurvivalOption.FoodCostFactor)); // added survival option 28.4.21
        numberOfCharsThatConsumedResource = 0;
        for (int i = campCharacterCount - 1; i >= 0; i--)
        {
            campCharacters[i].NewStates.Remove(CharacterBaseState.Hungry);
            if (campCharacters[i].PerkChance(BountyPerkChanceTypes.NoFoodConsumtionPerDay))//checks if the Character does not eat food today, due to their perks
            {
                campCharacters[i].RemoveState(CharacterBaseState.Hungry);
                campCharacters[i].moraleState.moraleEntries.RemoveAll(n => n.id == "Hungry");
                continue; //not added to numberOfPeopleThatConsumedResource because this Character does not need to eat today -> not consuming food
            }


            if (numberOfCharsThatConsumedResource < numberOfCharsThatCouldBeSupplied) // still enough food
            {
                campCharacters[i].RemoveState(CharacterBaseState.Hungry);
                campCharacters[i].moraleState.moraleEntries.RemoveAll(n => n.id == "Hungry");
                numberOfCharsThatConsumedResource++;
            }
            else // not enough food for someone else to eat
            {
                campCharacters[i].AddState(CharacterBaseState.Hungry, -1);
                campCharacters[i].moraleState.moraleEntries.Add(new MoralStateEntry("Hungry", -1, -30));
            }
        }
        resourceAmountConsumed = Mathf.RoundToInt((float)numberOfCharsThatConsumedResource * BountyManager.instance.persistentManager.GetSurvivalOption(SurvivalOption.FoodCostFactor)); // diff is negative if resource is to low
        BountyManager.instance.Variables.SetVariable("@ReportFoodConsum", Mathf.Min(GetResource(2), resourceAmountConsumed));
        ChangeResource(2, -Mathf.Min(GetResource(2), resourceAmountConsumed), false, false, true);


        // generator fuel consumption
        int genLvl = GetRoomLevel(CampRoomType.Generator);
        if (genLvl > 0 && bb.generatorActive)
        {
            ResultItemDefinition rid = new ResultItemDefinition();
            rid.itemType = BaseItem.ItemType2.CraftingBuildingParts;
            rid.tier = 1;
            rid.stack = generatorConsumptionValues[genLvl - 1];
            List<BaseItem> tFuel = TakePartyItems(new ResultItemDefinition[] { rid });
            if (tFuel[0].currentStack <= rid.stack)
                bb.generatorActive = false;
            Destroy(tFuel[0]);
        }
    }

    public int GetGeneratorConsumption()
    {
        FactionBase bb = BountyManager.instance.factionManager.PlayerFaction.bases[0];
        int genLvl = GetRoomLevel(CampRoomType.Generator);
        if (genLvl > 0 && bb.generatorActive)
        {
            return generatorConsumptionValues[genLvl - 1];
        }
        else
            return 0;
    }
    public bool ToggleGenerator(bool pValue)
    {
        FactionBase bb = BountyManager.instance.factionManager.PlayerFaction.bases[0];
        bool newValue = false;
        int genLvl = GetRoomLevel(CampRoomType.Generator);
        if (genLvl > 0)
        {
            int tAmount = CountPartyItems(BaseItem.ItemType2.CraftingBuildingParts, 1);

            if (pValue && generatorConsumptionValues[genLvl - 1] < tAmount)
            {
                newValue = pValue;
                bb.generatorActive = newValue;
            }

        }

        return newValue;
    }

    /// <summary>
    /// execute daily healing and state evaluation
    /// </summary>
    public void CheckCharacterStates()
    {
        List<BountyCharacter> list = GetAllCampPeople();
        List<BountyCharacter> injured = list.FindAll(n => n.HasState(CharacterBaseState.Injured));

        list.Sort(CharacterBedCalculationSort);
        int levelCiv = GetRoomLevel(CampRoomType.BedRoom);
        int amountCiv = 2 + (levelCiv - 1) * 2; // the amount beds for civilians (Worker, Slave, Normal)
        int levelTroop = GetRoomLevel(CampRoomType.TroopBedRoom);
        int amountTroop = 2 + (levelTroop - 1) * 4; // the amount of beds for soldiers
        int levelMedic = GetRoomLevel(CampRoomType.Medical);
        int amountMedic = levelMedic; // the amount of beds in the medical room


        int counterCiv = 0;
        int counterTroop = 0;
        int stressValue = 0;

        //injured characters in the camp are assigned to a bed in the medical room
        for (int i = 0; i < injured.Count; i++)
        {
            if (i < amountMedic)
            {
                list.Remove(injured[i]); // don't need another bed. this character sleeps in the medical room
                                         // heal injured
                injured[i].RemoveState(CharacterBaseState.Injured);
                // regen hp
                injured[i].Health += Mathf.RoundToInt((float)list[i].GetMaxHealth() * bedHealValues[levelMedic - 1] * injured[i].PerkBoostFactor(PerkBoostType.HealthInSleep));
                // checks if character has a HP Regeneration Boost and applies it.
            }
        }

        // non-injured characters and injured characters that don't fit into the medical room
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].survivorType == SurvivorType.Soldier)
            {
                if (++counterTroop > amountTroop + Mathf.Max(0, amountCiv - counterCiv))
                { //no bed for solider. They are stressed :(
                  // add stress
                    stressValue = list[i].AddStress(20);
                    MainGuiController.instance.notificationPanel.ShowNotification("Info_MissingBed", new FormatTextToken[] { list[i].CharNameToken, new FormatTextToken(stressValue.ToString(), false) }, null, false);
                }
                else // soldier sleeps in Troop Bedroom
                {
                    // regen hp
                    list[i].Health += Mathf.RoundToInt((float)list[i].GetMaxHealth() * bedHealValues[levelTroop - 1] * list[i].PerkBoostFactor(PerkBoostType.HealthInSleep));
                }
            }
            else // survivorType Normal, Worker or Slave (civilians)
            {
                if (++counterCiv > amountCiv)
                { // no bed for civilian
                  // add stress
                    stressValue = list[i].AddStress(20);
                    MainGuiController.instance.notificationPanel.ShowNotification("Info_MissingBed", new FormatTextToken[] { list[i].CharNameToken, new FormatTextToken(stressValue.ToString(), false) }, null, false);
                }
                else // civilian sleeps in bed room
                {
                    // regen hp
                    string name = list[i].name;
                    int number_of_perks = list[i].perks.Count;
                    list[i].Health += Mathf.RoundToInt((float)list[i].GetMaxHealth() * bedHealValues[levelCiv - 1] * list[i].PerkBoostFactor(PerkBoostType.HealthInSleep));
                }
            }

            list = GetAllCampPeople();
            // count up idle sessions
            if (list[i].Job == CampRoomType.None && !IsInParty(list[i]))
            {
                list[i].RecentIdleSessions += 1;
            }
            else
            {
                list[i].RecentIdleSessions = 0;
            }

            // tick temp attrib buff
            list[i].TickTempAttribBuffSessions();

            // apply negative state damage
            for (int j = list[i].BaseStates.Count - 1; j >= 0; j--)
            {
                BountyDamage bd = new BountyDamage();
                bool killing = false;
                bd.ignoresDefence = true;
                bd.inputValue = Mathf.RoundToInt(list[i].GetMaxHealth() * 0.2f); // normal damage
                if (statDeathTable.ContainsKey(list[i].BaseStates[j].state) && list[i].BaseStates[j].startTime + statDeathTable[list[i].BaseStates[j].state] <= BountyManager.instance.DateTime)
                    killing = true;

                if (killing)
                    bd.inputValue = Mathf.RoundToInt(list[i].GetMaxHealth() * 10f); // lethal damage

                if (list[i].BaseStates[j].state == CharacterBaseState.Hungry && !list[i].NewStates.Contains(CharacterBaseState.Hungry))
                {
                    if (!list[i].HasState(CharacterBaseState.Injured)) //Take damage if character is hungry
                    {
                        bd.source = DamageSource.Hunger;
                        list[i].ApplyDamage(ref bd, false, true, true); // damage from hungry
                    }
                    else
                    {
                        list[i].BaseStates[j].startTime += 1;
                    }
                }
                if (list[i].BaseStates[j].state == CharacterBaseState.Thirsty && !list[i].NewStates.Contains(CharacterBaseState.Thirsty))
                {
                    if (!list[i].HasState(CharacterBaseState.Injured)) //Take damage if character is thirsty
                    {
                        bd.source = DamageSource.Thirst;
                        list[i].ApplyDamage(ref bd, false, true, true); // damage from thristy
                    }
                    else
                    {
                        list[i].BaseStates[j].startTime += 1;
                    }
                }
                if (list[i].BaseStates[j].state == CharacterBaseState.Infected && !list[i].HasState(CharacterBaseState.Injured))
                {
                    if (!list[i].HasState(CharacterBaseState.Injured)) // Take damage if character is infected
                    {
                        bd.source = DamageSource.Infection;
                        list[i].ApplyDamage(ref bd, false, true, true); // damage from infection
                    }
                    else
                    {
                        list[i].BaseStates[j].startTime += 1;
                    }
                }

                if (list[i].BaseStates[j].duration > 0 && list[i].BaseStates[j].startTime + list[i].BaseStates[j].duration < BountyManager.instance.DateTime)
                {
                    list[i].RemoveState(list[i].BaseStates[j].state);
                }

                // kill character, if they took lethal damage from negative state
                if (bd.killingBlow && !list[i].storyCharacter && !list[i].mainCharacter)
                {
                    BountyManager.instance.AddPendingDead(list[i].uniqueId);
                    BountyManager.instance.campScene.RemovePeople(new BountyCharacter[] { list[i] });
                }
            }
        }

        // update morale states and meal requests
        foreach (var guy in list)
        {
            for (int i = guy.moraleState.moraleEntries.Count - 1; i >= 0; i--) // go through stat entries and remove expiring ones
            {
                if (guy.moraleState.moraleEntries[i].dateTime >= 0 && guy.moraleState.moraleEntries[i].dateTime <= BountyManager.instance.DateTime)
                {
                    guy.moraleState.moraleEntries.RemoveAt(i);
                }
            }
            if (BountyManager.instance.DateTime >= guy.moraleState.mealDateTime + 15)
            {
                guy.moraleState.SetNewMealRequest();
                guy.moraleState.moraleEntries.RemoveAll(n => n.id == "MealRequestSucess");
            }
            else if (BountyManager.instance.DateTime == guy.moraleState.mealDateTime + 11)
            {
                if (!guy.moraleState.moraleEntries.Exists(n => n.id == "MealRequestSucess" || n.id == "MealRequestFailed"))
                {
                    guy.moraleState.moraleEntries.Add(new MoralStateEntry("MealRequestFailed", guy.moraleState.mealDateTime + 15, -20)); // add request fail state
                }
            }
            if (guy.moraleState.wasInAction || (guy.Job != CampRoomType.BedRoom && guy.Job != CampRoomType.Recreation && guy.Job != CampRoomType.Bar && guy.Job != CampRoomType.None))
            {
                guy.Stress += 10; // stress buildup in normal jobs
            }
            guy.moraleState.wasInAction = false;
        }


        // heal background chars
        list = GetAllCampPeople(true);
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].backgroundCharacter)
                list[i].HealthPercent = 100;
        }
    }

    public void CheckCharacterStatesDangerHint()
    {
        List<BountyCharacter> list = GetAllCampPeople();
        List<BountyCharacter> inDanger = new List<BountyCharacter>();
        for (int i = 0; i < list.Count; i++)
        {
            // store chars in danger
            for (int j = list[i].BaseStates.Count - 1; j >= 0; j--)
            {
                if (statDeathTable.ContainsKey(list[i].BaseStates[j].state) && list[i].BaseStates[j].startTime + statDeathTable[list[i].BaseStates[j].state] <= BountyManager.instance.DateTime + 1)
                {
                    if (!inDanger.Contains(list[i]))
                        inDanger.Add(list[i]);
                }
            }
        }

        // show warnings by trish
        if (inDanger.Contains(BountyManager.instance.Player))
        {
            inDanger.Remove(BountyManager.instance.Player);
            BountyManager.instance.dialogueManager.StartDialogue(596);
        }
        if (inDanger.Count > 1)
        {
            BountyManager.instance.dialogueManager.StartDialogue(595);
        }
        else if (inDanger.Count > 0)
        {
            BountyManager.instance.Variables.SetVariable("@StateSurvivor", inDanger[0].CharName);
            BountyManager.instance.dialogueManager.StartDialogue(594);
        }
    }

    public List<JobDefinition> GetJobListForItem(BaseItem.ItemType2 type, int tier)
    {
        List<JobDefinition> result = new List<JobDefinition>();
        BountyCraftingRecipe bcr = null;
        for (int i = jobDefinitions.Length - 1; i >= 0; i--)
        {
            for (int j = 0; j < jobDefinitions[i].categories.Count; j++)
            {
                for (int k = 0; k < jobDefinitions[i].categories[j].actions.Length; k++)
                {
                    bcr = jobDefinitions[i].categories[j].actions[k].GetMainRecipe();
                    for (int l = 0; l < bcr.itemInput.Length; l++)
                    {
                        if (bcr.itemInput[l].itemType == type && bcr.itemInput[l].tier == tier)
                        {
                            if (!result.Contains(jobDefinitions[i]))
                            {
                                result.Add(jobDefinitions[i]);
                            }
                        }
                    }
                }
            }
        }
        return result;
    }
    /// <summary>
    /// gives the stress and morale bonus as a vector2int (stress,morale)
    /// </summary>
    /// <param name="tier"></param>
    /// <param name="slot"></param>
    /// <returns></returns>
    public Vector2Int GetBarDrinkBonus(int tier, int slot)
    {
        int morale = 0;
        int stress = 0;
        if (tier == 1)
        {
            morale = 15;
            stress = -2;
        }
        else if (tier == 2)
        {
            morale = 30;
            stress = -4;
        }
        else if (tier == 3)
        {
            morale = 50;
            stress = -6;
        }
        if (slot == 1)
            morale += 10;
        return new Vector2Int(stress, morale);
    }
    /// <summary>
    /// gives the stress and morale bonus as a vector2int (stress,morale)
    /// </summary>
    /// <param name="tier"></param>
    /// <param name="slot"></param>
    /// <returns></returns>
    public Vector2Int GetRecreationBonus(int tier, int slot)
    {
        int morale = 0;
        int stress = 0;
        if (tier == 1)
        {
            morale = 2;
            stress = -15;
        }
        else if (tier == 2)
        {
            morale = 4;
            stress = -30;
        }
        else if (tier == 3)
        {
            morale = 6;
            stress = -50;
        }
        return new Vector2Int(stress, morale);
    }

    public void SkipDateComplex(int value)
    {
        List<BountyCharacter> cList = GetAllCampPeople(true);
        for (int i = cList.Count - 1; i >= 0; i--)
        {
            cList[i].SkipStatesTime(value);
        }
        for (int i = activeJobs.Count - 1; i >= 0; i--)
        {
            activeJobs[i].startTime += value;
        }
    }

    #region inspector helper methods

#if UNITY_EDITOR

    public void FixJobs()
    {
        for (int i = 0; i < jobDefinitions.Length; i++)
        {
            for (int j = 0; j < jobDefinitions[i].categories.Count; j++)
            {
                for (int k = 0; k < jobDefinitions[i].categories[j].actions.Length; k++)
                {
                    if (jobDefinitions[i].categories[j].actions[k].isCrafting)
                    {
                        for (int l = 0; l < jobDefinitions[i].categories[j].actions[k].recipe.resourceCost.Length; l++)
                        {
                            if (jobDefinitions[i].categories[j].actions[k].recipe.resourceCost[l].type == 4)
                            {
                                jobDefinitions[i].categories[j].actions[k].recipe.resourceCost[l].amount = (k + 1) * 2;
                            }
                        }
                    }
                }
            }
        }
        EditorUtility.SetDirty(this);
    }
    //[ContextMenu("Fix List")]
    public void FixJobs2()
    {
        //for (int i = 0; i < jobDefinitions.Length; i++)
        //{
        //	jobDefinitions[i].categories = new List<JobActionCategory>();
        //	for (int j = 0; j < jobDefinitions[i].categories2.Count; j++)
        //	{
        //		jobDefinitions[i].categories.Add(jobDefinitions[i].categories2[j]);
        //	}
        //}
        //EditorUtility.SetDirty(this);
    }



#endif
    #endregion


    public void GiveDebugItems(int pCategory)
    {
        if (pCategory == 0)
        {
            BaseItem i;
            i = ScriptableObject.CreateInstance<BaseItem>();
            i.Setup(BaseItem.ItemType2.MiscKey, 1, 5);
            AddPartyItem(i);

            i = ScriptableObject.CreateInstance<BaseItem>();
            i.Setup(BaseItem.ItemType2.MiscKey, 2, 5);
            AddPartyItem(i);
        }
        else if (pCategory == 1)
        {
            BaseItem i;
            RandomItemDefinition rid = new RandomItemDefinition();
            rid.itemType = BaseItem.ItemType2.GearRandomClothing;
            rid.tierMin = 1;
            rid.tierMax = 2;
            rid.levelMin = 1;
            rid.levelMax = 2;
            for (int j = 0; j < 11; j++)
            {
                i = rid.GenerateItem();
                AddPartyItem(i);
            }
        }
    }

}

/// <summary>
/// data structure for the rooms that the player has build in the base
/// </summary>
[System.Serializable]
[fsObject]
public class CampRoomEntry
{
    [fsProperty]
    public CampRoomType type;
    [fsProperty]
    public int currentLevel;
    [fsProperty]
    public bool locked;

    public CampRoomEntry() { }

    public CampRoomEntry(CampRoomType type, int currentLevel, bool locked)
    {
        this.type = type;
        this.currentLevel = currentLevel;
        this.locked = locked;
    }
}

/// <summary>
/// data structure for the possible rooms and their upgrades // freshly loaded on game start
/// </summary>
[System.Serializable]
public class CampRoomDefinition
{
    public CampRoomType type;
    public int maxLevel;
    public int maxAmount;
    public CampRoomLevelDefinition[] levels;
}

/// <summary>
/// data structure for a certain room level that grants bonus stats
/// </summary>
[System.Serializable]
public class CampRoomLevelDefinition
{
    public int jobBonus;
    public int maxWorker;
}

public enum CampRoomType
{
    // original camp rooms
    BedRoom,
    Bar,
    Hunter,
    Farm,
    Smith,
    ArmorSmith,
    WeaponSmith,
    Defense,
    Medical,
    // additional
    Floor,
    Graveyard,
    RadioStation,
    None,
    // outpost rooms
    Leader,
    Merchant,
    Arena,
    Prison,
    // new rooms DA3
    Generator,
    WaterTreatment,
    Recreation,
    WorkerTraining,
    TroopTraining,
    Kennel,
    Kitchen,
    MerchantSurvivors, // not used
    MerchantAnimals,
    TroopBedRoom,
    Quests,
    OreMining,
}

[System.Serializable]
[fsObject]
public class BountyResourceEntry
{
    [fsProperty]
    [ListSelection("Resource", true)]
    public int type;
    [fsProperty]
    public int amount;

    public BountyResourceEntry() { }

    public BountyResourceEntry(int pType, int pAmount)
    {
        type = pType;
        amount = pAmount;
    }
}
[System.Serializable]
public class BountyResourceRangeEntry
{
    [ListSelection("Resource", true)]
    public int type;
    public int amountMin;
    public int amountMax;

    public BountyResourceRangeEntry(int pType, int pMin, int pMax)
    {
        type = pType;
        amountMin = pMin;
        amountMax = pMax;
    }
}


/// <summary>
/// data structure for the possible jobs and their behaviours
/// </summary>
[System.Serializable]

public class JobDefinition
{
    public CampRoomType type;
    [Tooltip("associated talent to check level requirements")]
    public BountyTalentType talent; // requirement
    [Tooltip("associated pasive skill to check level requirements")]
    public string passiveSkill; // requirement
    public int xpGain;
    [Tooltip("can this job reassign itself after every session automaticly?")]
    public bool continiously;
    public List<JobActionCategory> categories;
    [Tooltip("at which talent/skill levels the job tier increases (max 2) => level 0-2 or 1-3 respectively")]
    public int[] jobTiers;

}

[System.Serializable]
public class JobActionCategory
{
    public string title;
    public int tierOverride; // used to return a specifi tier number when asking the job instance for it's tier (guard function used it?)
    public JobActionDefinition[] actions;
    public bool isRoomUpgrade;
}

/// <summary>
/// data structure for a job action like scouting or farming or crafting
/// </summary>
[System.Serializable]
public class JobActionDefinition
{
    public int duration; // in sessions (2 sessions = 1 day)
    public int talentMinLevel; // might be a passive skill level rather than a talent level
    public int roomMinLevel;
    public SpecialJobFunction specialFunction;
    public bool isCrafting;
    public bool isMerchant;
    public int maxPerDay; // max executions per semi day
    [ListSelection("Recipe", true, true)]
    public int[] recipeTemplates;
    public BountyCraftingRecipe recipe;
    public BountyResourceRangeEntry[] resourceYield;

    public BountyCraftingRecipe GetMainRecipe()
    {
        if (recipeTemplates.Length > 0)
        {
            return BountyManager.instance.craftingDatabase.GetAsset(recipeTemplates[0]).recipe;
        }
        else
        {
            return recipe;
        }
    }
    public BountyCraftingRecipe GetKnownRecipe(int index)
    {
        List<BountyCraftingRecipe> list = GetKnwonRecipes();
        if (list.Count == 0)
        {
            return null;
        }
        else
        {
            index = Mathf.Clamp(index, 0, list.Count - 1);
            return list[index];
        }
    }
    public int GetKnownRecipeId(int index)
    {
        List<int> result = new List<int>();
        foreach (var item in recipeTemplates)
        {
            if (BountyManager.instance.camp.IsRecipeKnown(item))
            {
                result.Add(BountyManager.instance.craftingDatabase.GetAsset(item).intId);
            }
        }
        if (result.Count == 0)
        {
            return -1;
        }
        else
        {
            index = Mathf.Clamp(index, 0, result.Count - 1);
            return result[index];
        }
    }
    public List<BountyCraftingRecipe> GetKnwonRecipes()
    {
        List<BountyCraftingRecipe> result = new List<BountyCraftingRecipe>();
        foreach (var item in recipeTemplates)
        {
            if (BountyManager.instance.camp.IsRecipeKnown(item))
            {
                result.Add(BountyManager.instance.craftingDatabase.GetAsset(item).recipe);
            }
        }
        return result;
    }
}

/// <summary>
/// data structure for crafting recipe
/// </summary>
[System.Serializable]
public class BountyCraftingRecipe
{
    public BountyResourceEntry[] resourceCost;
    public ResultItemDefinition[] itemInput;
    public ResultItemDefinition result;

    public BountyResourceEntry[] GetAdjustedResourceCost()
    {
        BountyResourceEntry[] bre = new BountyResourceEntry[resourceCost.Length];
        int tAmount;
        for (int i = 0; i < resourceCost.Length; i++)
        {
            tAmount = resourceCost[i].amount;
            if (resourceCost[i].type == 1)
            {
                tAmount = Mathf.RoundToInt((float)resourceCost[i].amount * BountyManager.instance.persistentManager.GetSurvivalOption(SurvivalOption.WaterCostFactor));
            }
            else if (resourceCost[i].type == 2)
            {
                tAmount = Mathf.RoundToInt((float)resourceCost[i].amount * BountyManager.instance.persistentManager.GetSurvivalOption(SurvivalOption.FoodCostFactor));
            }
            bre[i] = new BountyResourceEntry(resourceCost[i].type, tAmount);
        }
        return bre;
    }
}

/// <summary>
/// data structure for a job assignment instance connecting a camp person and a job, has to be saved to on disk
/// </summary>
[System.Serializable]
[fsObject]
public class JobInstance
{
    [fsProperty]
    public CampRoomType type;
    [fsIgnore]
    public BountyCharacter character
    {
        get { return BountyManager.instance.camp.GetUniqueCharacter(characterRef); }
        set
        {
            if (value.uniqueId == -1)
            {
                Debug.LogError("A character added to a JobInstance has the illegal uniqueId -1. This will lead to crashes");
            }
            characterRef = value.uniqueId;
        }
    }
    [fsProperty]
    public int characterRef;
    [fsProperty]
    public int cat;
    [fsProperty]
    public int slot;
    [fsProperty]
    public int startTime; // format: d * 2 + session
    [fsProperty]
    public bool continiously;
    [fsProperty]
    public string specialInfo;
    [fsProperty]
    public int stopingReason;
    [fsProperty]
    public bool hasMissingResources;
    [fsProperty]
    public List<BountyResourceEntry> missingResources = new List<BountyResourceEntry>();
    [fsProperty]
    public List<BaseItem> storedItems = new List<BaseItem>();
    [fsProperty]
    public int recipeId;

    public JobInstance() { }

    public int GetTier()
    {
        JobDefinition jd = BountyManager.instance.camp.GetJobDefinition(type);
        if (jd.categories[cat].isRoomUpgrade)
            return slot + 1;
        else if (jd.type == CampRoomType.Defense || jd.type == CampRoomType.Merchant)
            return BountyManager.instance.camp.GetJobTier(type, character);
        else
        {
            if (jd.categories[cat].tierOverride > 0)
                return jd.categories[cat].tierOverride; // added 30.11.20
            else
                return slot + 1; // changed xp reward to be based on slot for most jobs? 30.11.20
        }
    }

    public void UpdateCharacterAnis()
    {
        BaseNavNode.NodeType node = BaseNavNode.NodeType.Station;
        JobDefinition jd = BountyManager.instance.camp.GetJobDefinition(type);
        if (jd.categories[cat].isRoomUpgrade)
        {
            node = BaseNavNode.NodeType.Construction;
        }

        character.startNavMode = 1;
        character.goToWork = true;
        //character.startNodeIndex = -1;
        character.skipAniFast = true;
        character.startNodeType = node;
        character.startNodeStation = BaseNavNode.GetStationFromRoom(type);


        // update deco
        if (jd.categories[cat].isRoomUpgrade)
            BountyManager.instance.campScene.UpdateRoomUpgradeModel(type, true, character.female ? 1.14f : 1.1f);

        if (character.Model == null || BountyManager.instance.CurrentTutorialIndex >= 0)
            return;

        //character.Model.myNavAgent.masterState = character.startNavMode;
        //character.Model.myNavAgent.targetType = character.startNodeType;
        //character.Model.myNavAgent.targetStation = character.startNodeStation;
        //character.Model.myNavAgent.state = character.startNavMode;
        //character.Model.myNavAgent.goToWork = character.goToWork;
        character.Model.StopIdleData();
        //character.Model.myNavAgent.StartNavigationManually();
        BountyManager.instance.campScene.UpdateCharacter(character);
        //Debug.LogFormat("job sets target node for {0} to {1}, tries to teleport", character.characterId, character.Model.myNavAgent.targetNode.name);
        if (character.Model.myNavAgent.targetNode)
        {
            character.Model.myNavAgent.currentNode = character.Model.myNavAgent.targetNode;
            character.Model.Stop();
            character.Model.transform.position = character.Model.myNavAgent.targetNode.transform.position;
            character.Model.transform.rotation = character.Model.myNavAgent.targetNode.transform.rotation;
            character.Model.myAnimator.rootPosition = character.Model.transform.position;
            character.Model.myAnimator.rootRotation = character.Model.transform.rotation;
            //Debug.LogFormat("teleport done");
            //Debug.Break();
        }

    }

}

public enum SpecialJobFunction
{
    None,
    Scouting,
    ItemUpgrade,
    CampGuard,
    SleepHeal,
    BuySurvivors,
    Training,
    BarDrink,
    Recreation,
}

public enum SpeechBubbleType
{
    TrapFail,
    Enemies,
    LockFail,

}

[System.Serializable]
public class BountyItemLevelSetting
{
    public BaseItem.ItemType2 itemtype;
    public int baseMainAttribValue;
    public float attributeMainPerLevel;
    public float attributeModPerLevel;
    public float attributeMainPerTier;
    //public float attributeModPerTier;
}

[System.Serializable]
public class BountyCharacterLevelSetting
{
    public BountyCharAttribute attribute;
    public float maxAttributeIncreaseWeight;
}

[System.Serializable]
public class ItemValuePatch
{
    public int fileVersion;
    public BaseItem.ItemType2 itemtype;
    public AttributeModifier[] changes;
    public bool fixMissingAttribs;
}