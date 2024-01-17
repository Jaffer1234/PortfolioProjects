using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FullSerializer;

/// <summary>
/// controlls a base scene like the player base or other outposts. manages the nav agents and the room interaction
/// </summary>
public class BountyBase : MonoBehaviour
{
    public int ScenarioIndex;
    public BaseNavController baseNav;
    public BaseCam baseCam;
    public List<BaseRoomModel> roomModels;
    public GameObject roomUpgradeModel;
    public Transform staircasePos;
    public Transform camBasePos;

    [Header("Fremd-Basis-Bereich")]
    public Faction defaultFaction;
    public BountyScenario defenseScenario;
    [ListSelection("EventBlock", true, true)]
    public int eventBlockOnEnter = -1;
    public AudioClip baseMusic;
    public AudioClip baseMusicNight;
    public BaseIdleData[] combatViewerIdleData;
    public BaseIdleData[] defaultIdleData;
    [Header("SpielerCamp-Ist-Gefangen-Modul (Endgame)")]
    public bool prisonedCampInhabitantModule;
    public BaseNavNode.NodeType pcim_nodeType;
    public BaseNavNode.StationType pcim_StationType;

    private List<BountyModel> activeBaseModels = new List<BountyModel>(); // all models that belong to the base background living
    private List<BountyCharacter> activeBaseChars = new List<BountyCharacter>();
    private BountyBaseData baseData;
    private FactionBase baseInstance;
    private Faction currentOwner;
    private CampRoomType currentRoom;
    private int currentRoomIndex;
    //private int eventBlockInstance;

    private Dictionary<Faction, BaseDataStructure> baseStructureTable;
    private Dictionary<CampRoomType, BaseRoomModel> roomModelTable;

    public bool EntranceFight
    {
        get;
        set;
    }
    public List<BountyCharacter> ActiveInhabitants
    {
        get { return activeBaseChars; }
    }

    private readonly int[] arenaCosts = new int[] { 25, 35, 50, 50 };
    private readonly int[] arenaCostsHQ = new int[] { 40, 60, 80, 100 };

    private int rescueCheckIndex = 0;

    private void FixedUpdate()
    {
        if (activeBaseModels.Count > 0)
        {
            if (rescueCheckIndex >= activeBaseModels.Count)
                rescueCheckIndex = 0;
            var item = activeBaseModels[rescueCheckIndex];
            if (item != null && item.transform.position.y < transform.position.y - 150f)
            {
                if (item.myNavAgent.targetNode != null)
                {
                    if (IsInsideView(item.myNavAgent.targetNode.transform.position))
                    {
                        TeleportModelToViewBorder(item, item.myNavAgent.targetNode.transform.position);
                    }
                    else
                    {
                        TeleportModel(item, item.myNavAgent.targetNode.transform.position);
                    }
                }
                else
                {
                    TeleportModelToViewBorder(item, baseNav.Nodes[0].transform.position);
                }
            }
            rescueCheckIndex++;
        }
    }

    /// <summary>
    /// helper method
    /// </summary>
    /// <returns></returns>
    public BaseDataStructure GetBaseStructure()
    {
        if (baseStructureTable == null)
        {
            baseStructureTable = new Dictionary<Faction, BaseDataStructure>();
            for (int i = 0; i < baseData.structures.Length; i++)
            {
                baseStructureTable.Add(baseData.structures[i].faction, baseData.structures[i]);
            }
        }

        return baseStructureTable[currentOwner];
    }

    public void AddBaseModel(BountyModel bm)
    {
        if (!activeBaseModels.Contains(bm))
        {
            activeBaseModels.Add(bm);
        }
    }

    public List<BountyCharacter> SetupInhabitants(List<BountyCharacter> additionals)
    {
        List<BountyCharacter> result = new List<BountyCharacter>();
        BountyCharacter cInst;
        foreach (var c in additionals)
        {
            cInst = Instantiate<BountyCharacter>(c);
            cInst.Setup(0, new CharacterCreationInfo("spawned as additional base inhabitant in: " + gameObject.name));
            activeBaseChars.Add(cInst);
            result.Add(cInst);
        }
        return result;
    }
    public void AddInhabitant(BountyCharacter c, bool addPermanently)
    {

        //if (baseInstance.characterReplacements.Exists(n => n.key == c.characterId && n.value == "<none>"))
        //	baseInstance.characterReplacements.RemoveAll(n => n.key == c.characterId && n.value == "<none>");
        //else
        //	baseInstance.characterReplacements.Add(new StringPair("<none>", c.characterId));

        BountyCharacter cInst = null;
        if (addPermanently)
        {
            cInst = c;
            baseInstance.members.Add(cInst);
        }
        else
        {
            cInst = Instantiate<BountyCharacter>(c);
            cInst.Setup(0, new CharacterCreationInfo("spawned by AddInhabitant in: " + gameObject.name));
        }
        // update model
        if (!activeBaseChars.Exists(n => n.characterId == c.characterId) && defenseScenario.gameObject.activeInHierarchy)
        {
            activeBaseChars.Add(cInst);
            UpdateCharacter(cInst);
        }


    }
    public void RemoveInhabitant(BountyCharacter c, bool permanently)
    {
        //if(baseInstance.characterReplacements.Exists(n => n.value == c.characterId))
        //	baseInstance.characterReplacements.RemoveAll(n => n.value == c.characterId);
        //else
        //{
        //	baseInstance.characterReplacements.Add(new StringPair(c.characterId,"<none>"));
        //}

        // remove char reference
        BountyCharacter cInst = null;
        if (permanently)
        {
            cInst = c;
            baseInstance.members.Remove(c);
        }
        else
        {
            cInst = activeBaseChars.Find(n => n.characterId == c.characterId);
        }

        // remove model
        if (cInst != null && defenseScenario.gameObject.activeInHierarchy)
        {
            RemovePeople(new BountyCharacter[] { cInst });
            cInst.DestroyModel();
        }

        // destroy char data when temporary
        if (!permanently)
        {
            Destroy(cInst);
        }
    }



    public void UpdateRoomModels(List<CampRoomEntry> list)
    {



        BaseRoomModel check;
        for (int i = 0; i < list.Count; i++)
        {
            check = roomModels.Find(n => list[i].type == n.roomType);
            if (check != null)
            {

                for (int j = 0; j < check.tiers.Length; j++)
                {
                    if (check.tiers[j] != null)
                    {
                        check.tiers[j].SetActive(j == list[i].currentLevel - 1);

                        //Debug.Log(list[i].currentLevel - 1 +"CurrentLevel" + list[i].type  + "UpdateRoomModelsTrueOrfalse" + "j= " + j + check.roomType);
                    }
                    else
                    {
                        Debug.Log("hmm null bei: " + list[i].type.ToString());

                    }
                }
            }
        }
        if (roomModelTable == null)
        {
            roomModelTable = new Dictionary<CampRoomType, BaseRoomModel>();
        }
        if (roomModelTable.Count != roomModels.Count)
        {
            for (int i = 0; i < roomModels.Count; i++)
            {
                if (!roomModelTable.ContainsKey(roomModels[i].roomType))
                {
                    roomModelTable.Add(roomModels[i].roomType, roomModels[i]);
                }
            }
        }




    }
    /// <summary>
    /// zeigt und versteckt den arbeitsbock im raum
    /// </summary>
    /// <param name="room"></param>
    /// <param name="enable"></param>
    /// <param name="scale"></param>
    public void UpdateRoomUpgradeModel(CampRoomType room, bool enable, float scale)
    {
        Debug.Log("UpdateRoomUpgradeModel");
        if (roomUpgradeModel == null)
        {
            Debug.Log("roomUpgradeModel == null");
            return;
        }
        BaseRoomModel brm = GetRoomModel(room);
        if (enable)
        {
            if (brm.upgradeModel == null)
            {
                brm.upgradeModel = Instantiate<GameObject>(roomUpgradeModel, transform);
                BaseNavNode tNode = baseNav.Nodes.Find(n => n.roomType == room && n.nodeType == BaseNavNode.NodeType.Construction);
                brm.upgradeModel.transform.position = tNode.transform.position;
                brm.upgradeModel.transform.rotation = tNode.transform.rotation;
                brm.upgradeModel.transform.localScale = Vector3.one * scale;
            }
        }
        else
        {

            Destroy(brm.upgradeModel);
        }
        //foreach (var item in roomModelTable)
        //{
        //          Debug.Log($"Key: {item.Key}, Value: {item.Value}");
        //      }
    }
    public BaseRoomModel GetRoomModel(CampRoomType room)
    {
        if (roomModelTable == null)
        {
            roomModelTable = new Dictionary<CampRoomType, BaseRoomModel>();
        }
        if (roomModelTable.Count != roomModels.Count)
        {
            for (int i = 0; i < roomModels.Count; i++)
            {
                if (!roomModelTable.ContainsKey(roomModels[i].roomType))
                {
                    roomModelTable.Add(roomModels[i].roomType, roomModels[i]);
                }
            }
        }

        return roomModelTable[room];
    }


    /// <summary>
    /// update a list of people that have their own nav instructions
    /// </summary>
    /// <param name="characters"></param>
    public void UpdatePeople(List<BountyCharacter> characters)
    {
        //Debug.LogFormat("updating camp people");
        for (int i = 0; i < characters.Count; i++)
        {
            UpdateCharacter(characters[i], characters[i].startNodeType, characters[i].startNodeStation, characters[i].startNavMode, characters[i].mainCharacter ? 2 : 1, false, 0, false);
        }
    }

    /// <summary>
    /// use to update a list of people and give all the same nav instructions
    /// </summary>
    /// <param name="characters"></param>
    /// <param name="st"></param>
    /// <param name="nt"></param>
    /// <param name="navMode"></param>
    public void UpdatePeople(List<BountyCharacter> characters, BaseNavNode.StationType st, BaseNavNode.NodeType nt, int navMode)
    {
        //Debug.LogFormat("updating camp people");
        for (int i = 0; i < characters.Count; i++)
        {
            UpdateCharacter(characters[i], nt, st, navMode, 1, false, 0, false);
        }
    }

    /// <summary>
    /// teleport a list of people to their desired nav locations
    /// </summary>
    /// <param name="characters"></param>
    public void RePlacePeople(List<BountyCharacter> characters)
    {
        //Debug.LogFormat("updating camp people");

        for (int i = 0; i < characters.Count; i++)
        {
            if (characters[i].Model == null)
                continue;


            characters[i].Model.StopIdleData();
            characters[i].Model.myNavAgent.ProcessNavigation();
            //Debug.LogFormat("job sets target node for {0} to {1}, tries to teleport", character.characterId, character.Model.myNavAgent.targetNode.name);
            if (characters[i].Model.myNavAgent.targetNode)
            {

                characters[i].Model.Stop();
                characters[i].Model.transform.position = characters[i].Model.myNavAgent.targetNode.transform.position;
                characters[i].Model.transform.rotation = characters[i].Model.myNavAgent.targetNode.transform.rotation;
                characters[i].Model.myAnimator.rootPosition = characters[i].Model.transform.position;
                characters[i].Model.myAnimator.rootRotation = characters[i].Model.transform.rotation;
            }
        }
    }
    public void SetPeopleActive(BountyCharacter[] characters, bool value)
    {

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i].Model == null)
            {
                continue;
            }
            if (!value)
            {
                characters[i].Model.Stop();
                characters[i].Model.myNavAgent.enabled = false;

            }
            characters[i].Model.gameObject.SetActive(value);
        }
    }
    public void RemovePeople(BountyCharacter[] characters)
    {
        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i].Model != null)
            {
                activeBaseModels.Remove(characters[i].Model);
                baseNav.RemoveModel(characters[i].Model);
                characters[i].DestroyModel();
            }
        }

    }
    /// <summary>
    /// updates the navigation state of the character by adjusting the values on his model and nav agent component
    /// </summary>
    /// <param name="character"></param>
    public void UpdateCharacter(BountyCharacter character, bool clearOldAni = false)
    {
        //if(clearOldAni)
        //{
        //	character.Model.ClearBaseIdleObject();
        //}
        UpdateCharacter(character, character.startNodeType, character.startNodeStation, character.startNavMode, character.mainCharacter ? 2 : 1, false, 0, false);
    }
    /// <summary>
    /// updates the navigation state of the  character by adjusting the values on their model and nav agent component
    /// </summary>
    /// <param name="character"></param>
    /// <param name="nt"></param>
    /// <param name="st"></param>
    /// <param name="masterState"></param>
    /// <param name="walkMode"></param>
    /// <param name="allowTeleport"></param>
    /// <param name="prioMode"></param>
    public void UpdateCharacter(BountyCharacter character, BaseNavNode.NodeType nt, BaseNavNode.StationType st, int masterState, int walkMode, bool allowTeleport, int prioMode, bool stop)
    {

        BountyModel bm;
        //bm = baseNav.GetModelByCharacter(character);
        bm = character.Model;

        if (bm == null) // model needs to spawn
        {
            bm = character.SpawnModel();
            //bm.SetupPathfinding();
            AddBaseModel(bm);
            //bm.SetBodyHitLayerWeight(false);
            //bm.DefaultIdleData = defaultIdleData;
            //bm.CurrentIdleData = defaultIdleData;

            // search a start node and place it there, except when in working mode? changed 4.12.19 by M.E.
            List<BaseNavNode> list = new List<BaseNavNode>();
            if (!character.goToWork)
            {
                list = baseNav.Nodes.FindAll(n => n.nodeType == nt && (st == BaseNavNode.StationType.Any || n.stationType == st) && !n.occupied && n.IsUnlocked() && !n.dontSpawnModelsRandomly); // added occupied check 14.12.20
            }
            else
            {
                list = baseNav.Nodes.FindAll(n => n.nodeType == nt && (st == BaseNavNode.StationType.Any || n.stationType == st) && !n.occupied && n.IsUnlocked() && !n.dontSpawnModelsRandomly);
            }

            if (list.Count == 0)
            {
                character.goToWork = false;
                st = BaseNavNode.StationType.Any;
                nt = BaseNavNode.NodeType.Idle;
                list = baseNav.Nodes.FindAll(n => n.nodeType == nt && (st == BaseNavNode.StationType.Any || n.stationType == st) && !n.dontSpawnModelsRandomly);
            }


            if (character.startNodeIndex >= 0 && character.startNodeIndex < list.Count)
                bm.myNavAgent.currentNode = list[character.startNodeIndex];
            else
            {
                int rnd = bm.AgentRng.GetRange(0, list.Count);
                bm.myNavAgent.currentNode = list[rnd];
                if (character.goToWork)
                    character.startNodeIndex = rnd;
            }



            bm.myNavAgent.PlaceOnNode();
            bm.myNavAgent.currentNode.occupied = true; // added  14.12.20

            bm.transform.SetParent(transform, true);
            baseNav.AddModel(bm);
            //if(BaseNavAgent.choice.Contains(st))
            //{
            //	bm.myNavAgent.LastRandomStation = st;
            //}
            bm.myNavAgent.targetType = nt;
            bm.myNavAgent.targetStation = st;
            bm.myNavAgent.masterState = masterState;
            bm.myNavAgent.walkMode = walkMode;
            bm.myNavAgent.prioMode = prioMode;
            bm.myNavAgent.goToWork = character.goToWork; // added 4.9.19 by M.E.

            bm.onFinishedSpawning = (m) => StartNavAgent(m, allowTeleport);
        }
        else
        {
            bm.transform.SetParent(transform, true);
            baseNav.AddModel(bm);



            if (bm.myNavAgent.currentNode && bm.myNavAgent.currentNode.stationType == st && bm.myNavAgent.currentNode.nodeType == nt && masterState == bm.myNavAgent.masterState)
            {

            }
            else
            {
                bm.myNavAgent.enabled = true;
                bm.myNavAgent.StopAllCoroutines();
                if (stop)
                    bm.Stop(false);
                if (bm.myNavAgent.currentNode)
                    bm.myNavAgent.currentNode.occupied = false;
                if (bm.myNavAgent.targetNode)
                    bm.myNavAgent.targetNode = null;
            }



            bm.myNavAgent.targetType = nt;
            bm.myNavAgent.targetStation = st;
            bm.myNavAgent.masterState = masterState;
            bm.myNavAgent.walkMode = walkMode;
            bm.myNavAgent.prioMode = prioMode;
            bm.myNavAgent.goToWork = character.goToWork; // added 4.9.19 by M.E.
           baseNav.models.RemoveAll(model => model == null || model.gameObject == null);
            if (bm.FinishedBuild)
                StartNavAgent(bm, allowTeleport);
            else
                bm.onFinishedSpawning = (m) => StartNavAgent(m, allowTeleport);
        }

    }

    private void StartNavAgent(BountyModel bm, bool allowTeleport)
    {
        bm.onFinishedSpawning -= (m) => StartNavAgent(bm, allowTeleport);

        bm.myNavAgent.enabled = true;

        if (allowTeleport)
            bm.myNavAgent.teleportView = true;
        //TeleportModelToViewBorder(bm, baseCam.worldCam, baseNav.Nodes, bm.myNavAgent.targetNode.transform.position);

        bm.myNavAgent.currentNode = bm.myNavAgent.GetNearestNode();

        bm.myNavAgent.fastSkip = bm.myCharacter.skipAniFast;
        if (bm.myCharacter.skipAniFast)
        {
            bm.myCharacter.skipAniFast = false;
        }

        bm.myNavAgent.state = 1;


    }


    /// <summary>
    /// called when the scene got instantiated
    /// </summary>
    public void PreCheckBase(bool lowSpawnRate = false)
    {
        Debug.Log("PreCheckBase");
        if (BountyManager.instance.Map.CurrentPoint.virtualPoint)
            currentOwner = BountyManager.instance.Map.CurrentPoint.ownerOverride;
        else
            currentOwner = BountyManager.instance.Map.CurrentArea.owner;

        baseData = Instantiate<BountyBaseData>(BountyManager.instance.factionManager.GetBaseDataTemplate(BountyManager.instance.Map.CurrentPoint.activeBaseRefId)); // needs to be a copy so we can apply modifications during gameplay
        baseInstance = BountyManager.instance.Map.CurrentPoint.BaseRef;

        // replace dialogues
        for (int i = 0; i < baseInstance.dialogueReplacements.Count; i++)
        {
            for (int j = 0; j < GetBaseStructure().baseRooms.Length; j++)
            {
                if (GetBaseStructure().baseRooms[j].enterDialogue == baseInstance.dialogueReplacements[i].key)
                {
                    GetBaseStructure().baseRooms[j].enterDialogue = baseInstance.dialogueReplacements[i].value;
                    break;
                }
            }
        }

        // replace charcters and models
        //BountyCharacter cOld = null;
        ModelPlaceHolder[] placers = GetComponentsInChildren<ModelPlaceHolder>();



        // add members
        foreach (var tCh in baseInstance.members)
        {
            activeBaseChars.Add(tCh);
        }

        // spawn other models
        int tC = 0;
        while (tC < placers.Length)
        {
            if (!lowSpawnRate || placers[tC].GetComponent<BaseNavNode>() == null || (placers[tC].GetComponent<BaseNavNode>() != null && placers[tC].GetComponent<BaseNavNode>().nodeType == BaseNavNode.NodeType.Station) || tC % 4 == 0)
                placers[tC].SendMessage("SpawnModel", null, SendMessageOptions.DontRequireReceiver);
            tC++;
        }
        // spawn deco models
        List<BountyModel> modelList = new List<BountyModel>(GetBaseStructure().inhabitantModels);
        BountyModel modelInstance;
        BaseNavAgent bna;
        List<BaseNavNode> nodes = new List<BaseNavNode>(baseNav.Nodes);
        int theNode;
        nodes.RemoveAll(n => n.occupied || !n.nodeEnabled || n.nodeType == BaseNavNode.NodeType.Station || n.dontSpawnModelsRandomly);
        foreach (var bm in modelList)
        {
            if (nodes.Count == 0)
                break;
            theNode = SDRandom.Range(0, nodes.Count);
            modelInstance = Instantiate<BountyModel>(bm, nodes[theNode].transform.position, nodes[theNode].transform.rotation, transform);
            modelInstance.SetupPathfinding();
            bna = modelInstance.GetComponent<BaseNavAgent>();
            AddBaseModel(modelInstance);
            //bna.currentNode = nodes[theNode];

            modelInstance.onFinishedSpawning = (m) =>
            {
                m.myNavAgent.currentNode = m.myNavAgent.GetNearestNode();
                m.myNavAgent.enabled = true;
                m.myNavAgent.state = 1;
                m.myNavAgent.masterState = 2;
                m.myNavAgent.targetStation = BaseNavNode.StationType.Any;
                m.myNavAgent.targetType = BaseNavNode.NodeType.Idle;
                m.myNavAgent.currentNode.occupied = true;
            };
            nodes.RemoveAt(theNode);
        }
    }

    /// <summary>
    /// called when arriving at an outpost
    /// </summary>
    public void CheckBase(bool skipEntrance = false)
    {

        int tOwner = (int)BountyManager.instance.Map.CurrentArea.owner;
        BountyManager.instance.Variables.SetVariable("@LocalVisited", baseInstance.GetProperty("Visited").AsBool());
        BountyManager.instance.Variables.SetVariable("@LocalConflict", 0);
        BountyManager.instance.Variables.SetVariable("@LocalQuestActive", false); // unused
        BountyManager.instance.Variables.SetVariable("@LocalPlayerRank", BountyManager.instance.factionManager.GetRank(currentOwner));
        BountyManager.instance.Variables.SetVariable("@LocalFactionName", "FactionName_" + tOwner);
        BountyManager.instance.Variables.SetVariable("@LocalFactionPos", "FactionPossessive_" + tOwner);
        BountyManager.instance.Variables.SetVariable("@LocalFactionRef", "FactionReference_" + tOwner);
        BountyManager.instance.Variables.SetVariable("@LocalFactionAcc", "FactionAccusative_" + tOwner);
        BountyManager.instance.Variables.SetVariable("@LocalBaseName", "MapLocation_" + BountyManager.instance.factionManager.GetBaseDataTemplate(BountyManager.instance.Map.CurrentPoint.activeBaseRefId).scenario.locationName);

        baseInstance.SetProperty("Visited", true);
        currentRoom = CampRoomType.Defense;
        for (int i = 0; i < GetBaseStructure().baseRooms.Length; i++)
        {
            if (GetBaseStructure().baseRooms[i].roomType == currentRoom)
            {
                currentRoomIndex = i;
                break;
            }
        }


        // disable context buttons
        MainGuiController.instance.controlInfo.SetupButtons(null);

        BountyManager.instance.factionManager.UpdateAutoVariables();
        BountyManager.instance.factionManager.CalcBribeValue();

        if (!skipEntrance)
        {
            baseCam.SetCamMode(1);
            baseCam.gameObject.SetActive(false);
            if (BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.ArrivedBase, BountyManager.instance.Map.CurrentPoint.coords.ToString() }))
            {
                return;
            }

            activeBaseModels.ForEach(n => { n.DefaultIdleData = n.CurrentIdleData; });
            BountyManager.instance.dialogueManager.StartDialogue("Op_Entrance_Start");
        }
        else
        {
            BountyManager.instance.ResolveCombatIntoBaseScene();

            BountyManager.instance.Variables.SetVariable("SideQuest_1_Here", false);
            BountyManager.instance.Variables.SetVariable("SideQuest_2_Here", false);
            BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.EnteredBase, BountyManager.instance.Map.CurrentPoint.coords.ToString() });
        }
    }



    public void Attack()
    {
        BountyManager.instance.factionManager.ChangeRelationship(currentOwner, Faction.Player, -10, true);
        BountyManager.instance.combatManager.Mode = 1;
        BountyManager.instance.combatManager.EndTurn();
        BountyManager.instance.combatManager.EnemySurrender = true;

        EntranceFight = true;

        activeBaseModels.ForEach(n => { n.DefaultIdleData = n.CurrentIdleData; n.StopIdleData(); n.CurrentIdleData = combatViewerIdleData; n.RotateToCombat = defenseScenario.combatPositions[0]; });
    }

    public void Leave()
    {
        BountyManager.instance.combatManager.LeaveToLeftSide = true;
        BountyManager.instance.OnCombatFinished(-1);
        BountyManager.instance.combatManager.EnemySurrender = false;
        EntranceFight = false;

    }
    public void Enter()
    {
        baseCam.gameObject.SetActive(true);
        baseCam.SetCamMode(0);
        //BountyManager.instance.ShiftTimeScale(1f, 0.5f);

        bool wasFighting = EntranceFight;
        EntranceFight = false;

        BountyManager.instance.ResolveCombatIntoBaseScene();
        BountyManager.instance.combatManager.EnemySurrender = false;

        if (wasFighting)
            activeBaseModels.ForEach(n => { n.CurrentIdleData = n.DefaultIdleData; n.DefaultIdleData = new BaseIdleData[0]; if (n.DefaultAniStates != null) n.AddAniState(n.DefaultAniStates); n.RotateBack = true; if (n.myNavAgent != null && n.myNavAgent.enabled) n.myNavAgent.EngageNextNode(); });

        BountyManager.instance.Variables.SetVariable("SideQuest_1_Here", false);
        BountyManager.instance.Variables.SetVariable("SideQuest_2_Here", false);
        BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.EnteredBase, BountyManager.instance.Map.CurrentPoint.coords.ToString() });
        BountyManager.instance.persistentManager.TrackBase(BountyManager.instance.Map.CurrentPoint.coords);

        BountyManager.instance.LastRoomType = CampRoomType.None; // added 15.2.21
    }

    /// <summary>
    /// called by clicking the room in base scene
    /// </summary>
    /// <param name="room"></param>
    public void CheckRoom(CampRoomType room, bool quick = false)
    {
        int arenaLevel = baseInstance.GetProperty("ArenaLevel").AsInt();
        BountyManager.instance.Variables.SetVariable("@ArenaLevel", arenaLevel);

        currentRoom = room;
        for (int i = 0; i < GetBaseStructure().baseRooms.Length; i++)
        {
            if (GetBaseStructure().baseRooms[i].roomType == room)
            {
                currentRoomIndex = i;
                break;
            }
        }

        int rank = BountyManager.instance.factionManager.GetRank(currentOwner);
        BountyManager.instance.Variables.SetVariable("@LocalPlayerRank", rank);

        bool refillShop = false;

        // generate new shop inventory
        // first clear old inventory
        if (BountyManager.instance.Map.CurrentPoint.BaseRef.GetProperty("LastShopRefill").AsInt() == -1 || (BountyManager.instance.DateTime - 8) / 10 > (BountyManager.instance.Map.CurrentPoint.BaseRef.GetProperty("lastShopRefill").AsInt() - 8) / 10)
        {
            foreach (var item in BountyManager.instance.Map.CurrentPoint.BaseRef.inventory)
            {
                //BountyManager.instance.camp.UnregistierItem(item);
                Destroy(item);
            }
            BountyManager.instance.Map.CurrentPoint.BaseRef.inventory.Clear();
            refillShop = true;
            BountyManager.instance.Map.CurrentPoint.BaseRef.SetProperty("LastShopRefill", BountyManager.instance.DateTime);
        }
        BountyManager.instance.Variables.SetVariable("@ShopInvAge", BountyManager.instance.Day - BountyManager.instance.Map.CurrentPoint.BaseRef.GetProperty("LastShopRefill").AsInt());



        // check some stuff and set some variables
        if (currentRoom == CampRoomType.Merchant)
        {
            int roughRank = BountyManager.instance.factionManager.GetRoughRank(currentOwner);
            BountyManager.instance.Variables.SetVariable("@NewRank", 0);
            if (BountyManager.instance.Map.CurrentPoint.BaseRef.GetProperty("playerReputationUpFlag").AsInt() < roughRank /*&& !baseInstance.shopPoolBlockage.Contains(roughRank-1)*/)
            {
                BountyManager.instance.Variables.SetVariable("@NewRank", roughRank);
                BountyManager.instance.Map.CurrentPoint.BaseRef.SetProperty("playerReputationUpFlag", roughRank);
            }



            // add tier 1 items
            RandomItemDefinition[] pool = GetBaseStructure().shopTemplate.GetShopPool(ShopPool.PoolType.LowTier).fixedItems;
            if ((refillShop /*|| baseInstance.refillForce.Contains(0)*/) /*&& !baseInstance.shopPoolBlockage.Contains(0)*/)
            {
                //baseInstance.refillForce.Remove(0);
                for (int i = 0; i < pool.Length; i++)
                {
                    BountyManager.instance.Map.CurrentPoint.BaseRef.inventory.Add(pool[i].GenerateItem(1));
                }
            }
            // tier 2 items
            if ((roughRank >= 2 /*|| baseInstance.shopPoolForce.Contains(1)*/) && (refillShop || BountyManager.instance.Variables.GetVariable("@NewRank").AsInt() >= 2 /*|| baseInstance.refillForce.Contains(1)*/) /*&& !baseInstance.shopPoolBlockage.Contains(1)*/)
            {
                //baseInstance.refillForce.Remove(1);
                pool = GetBaseStructure().shopTemplate.GetShopPool(ShopPool.PoolType.MidTier).fixedItems;
                for (int i = 0; i < pool.Length; i++)
                {
                    BountyManager.instance.Map.CurrentPoint.BaseRef.inventory.Add(pool[i].GenerateItem(1));
                }
            }
            // and tier 3 items
            if ((roughRank >= 3 /*|| baseInstance.shopPoolForce.Contains(2)*/) && (refillShop || BountyManager.instance.Variables.GetVariable("@NewRank").AsInt() >= 3 /*|| baseInstance.refillForce.Contains(2)*/) /*&& !baseInstance.shopPoolBlockage.Contains(2)*/)
            {
                //baseInstance.refillForce.Remove(2);
                pool = GetBaseStructure().shopTemplate.GetShopPool(ShopPool.PoolType.HighTier).fixedItems;
                for (int i = 0; i < pool.Length; i++)
                {
                    BountyManager.instance.Map.CurrentPoint.BaseRef.inventory.Add(pool[i].GenerateItem(1));
                }
            }
            // and random items
            if (refillShop)
            {
                LootPool pool2 = GetBaseStructure().shopTemplate.GetShopPool(ShopPool.PoolType.RandomItems).randomItems;
                BaseItemDefinition bdi = pool2.RollItem();
                if (bdi != null && bdi.itemType != BaseItem.ItemType2.None)
                {
                    BountyManager.instance.Map.CurrentPoint.BaseRef.inventory.Add(bdi.GenerateItem(1));
                }
            }

            // new mechanic
            if ((refillShop || BountyManager.instance.Map.CurrentPoint.BaseRef.GetProperty("refillAdditionalItems").AsBool()) && BountyManager.instance.Map.CurrentPoint.BaseRef.shopAdditionalInventory != null && BountyManager.instance.Map.CurrentPoint.BaseRef.shopAdditionalInventory.Count > 0)
            {
                BountyManager.instance.Map.CurrentPoint.BaseRef.SetProperty("refillAdditionalItems", false);
                List<BaseItemDefinition> list = BountyManager.instance.Map.CurrentPoint.BaseRef.shopAdditionalInventory;
                for (int i = 0; i < list.Count; i++)
                {
                    BountyManager.instance.Map.CurrentPoint.BaseRef.inventory.Add(list[i].GenerateItem(1));
                }
            }
        }
        else if (currentRoom == CampRoomType.Prison)
        {
            Debug.Log("Enter Room Prison. Should show Soldier and Worker Survivors to purchase");
            if (refillShop)
            {
                foreach (var item in BountyManager.instance.Map.CurrentPoint.BaseRef.slaves)
                {
                    Destroy(item);
                }
                BountyManager.instance.Map.CurrentPoint.BaseRef.slaves.Clear();

                foreach (var item in GetBaseStructure().survivorTradeEntries)
                {
                    for (int i = 0; i < item.amount; i++)
                    {
                        BountyCharacter bc = BountyManager.instance.camp.CreateRandomCharacter(item.type);
                        BountyManager.instance.Map.CurrentPoint.BaseRef.slaves.Add(bc);
                    }
                }
            }
        }
        else if (currentRoom == CampRoomType.MerchantAnimals)
        {
            if (refillShop)
            {
                foreach (var item in BountyManager.instance.Map.CurrentPoint.BaseRef.animals)
                {
                    Destroy(item);
                }
                BountyManager.instance.Map.CurrentPoint.BaseRef.animals.Clear();

                foreach (var item in GetBaseStructure().animalTrades)
                {
                    for (int i = 0; i < item.amount; i++)
                    {
                        BountyAnimal ba = Instantiate<BountyAnimal>(item.template);
                        BountyManager.instance.Map.CurrentPoint.BaseRef.animals.Add(ba);
                    }
                }
            }
        }
        else if (currentRoom == CampRoomType.Arena)
        {
            if (arenaLevel < arenaCosts.Length)
            {
                if (BountyManager.instance.Map.CurrentPoint.coords == new Vector2Int(20, 18))
                    BountyManager.instance.Variables.SetVariable("@ArenaCost", arenaCostsHQ[arenaLevel]);
                else
                    BountyManager.instance.Variables.SetVariable("@ArenaCost", arenaCosts[arenaLevel]);

            }

        }
        else if (currentRoom == CampRoomType.Medical)
        {
            int price = 20;
            int reduction = 0;
            if (rank > 3) // verbündet
            {
                reduction = 20;
                //price = 16;
            }
            if (rank > 4) // held
            {
                reduction = 30;
                //price = 14;
            }
            if (BountyManager.instance.Variables.HasVariable("PersonalDiscount_" + GetBaseStructure().baseRooms[currentRoomIndex].character.characterId))
            {
                reduction += BountyManager.instance.Variables.GetVariable("PersonalDiscount_" + GetBaseStructure().baseRooms[currentRoomIndex].character.characterId).AsInt();
            }

            price = Mathf.RoundToInt((float)price * (100f - (float)reduction) / 100f);

            BountyManager.instance.Variables.SetVariable("@MedicPrice", price);
            BountyManager.instance.Variables.SetVariable("@MedicReduction", reduction);
            BountyManager.instance.Variables.SetVariable("@RoomResult", false);

            if (BountyManager.instance.camp.GetResource(0) >= price)
            {
                BountyManager.instance.Variables.SetVariable("@RoomResult", true);
            }
        }


        // actual enter command

        BountyManager.instance.LastRoomType = currentRoom; // moved here from enter room function 28.10.20
        if (quick && GetBaseStructure().baseRooms[currentRoomIndex].enterDialogue >= 0)
        {
            MainGuiController.instance.dialogueGui.SetScreenBlock(false);
            MainGuiController.instance.mainTabBar.SetToggleSwitchesBlocked(false);
            BountyManager.instance.dialogueManager.StartDialogue(GetBaseStructure().baseRooms[currentRoomIndex].enterDialogue);
        }
        else
        {

            // enter room or trigger dialogue
            if (BountyManager.instance.CurrentTutorialIndex < 0)
            {
                baseCam.Mode = 3;
                BountyManager.instance.Player.Model.myNavAgent.onTargetReached = (agent) =>
                {
                    if (GetBaseStructure().baseRooms[currentRoomIndex].enterDialogue >= 0)
                    {
                        MainGuiController.instance.dialogueGui.SetScreenBlock(false);
                        MainGuiController.instance.mainTabBar.SetToggleSwitchesBlocked(false);
                        BountyManager.instance.dialogueManager.StartDialogue(GetBaseStructure().baseRooms[currentRoomIndex].enterDialogue);

                    }
                    else
                    {
                        MainGuiController.instance.dialogueGui.SetScreenBlock(false);
                        MainGuiController.instance.mainTabBar.SetToggleSwitchesBlocked(false);
                        EnterRoom();
                    }
                    baseCam.Mode = 0;
                    agent.onTargetReached = null;
                };

                UpdateCharacter(BountyManager.instance.Player, BaseNavNode.NodeType.Station, BaseNavNode.GetStationFromRoom(room), 1, 2, BountyManager.instance.CurrentTutorialIndex < 0, 0, true);
            }
        }
    }


    /// <summary>
    /// called by quest scripts eg after a cutscene
    /// </summary>
    public void TriggerEnterRoom(CampRoomType roomType)
    {
        baseCam.GetAnchor(roomType).CmdClicked();
    }

    public void UpdateRoomVariables()
    {
        int rank = BountyManager.instance.factionManager.GetRank(currentOwner);

        if (currentRoom == CampRoomType.Medical)
        {
            int price = 20;
            int reduction = 0;
            if (rank > 3) // verbündet
            {
                reduction = 20;
                //price = 16;
            }
            if (rank > 4) // held
            {
                reduction = 30;
                //price = 14;
            }
            if (BountyManager.instance.Variables.HasVariable("PersonalDiscount_" + GetBaseStructure().baseRooms[currentRoomIndex].character.characterId))
            {
                reduction += BountyManager.instance.Variables.GetVariable("PersonalDiscount_" + GetBaseStructure().baseRooms[currentRoomIndex].character.characterId).AsInt();
            }

            price = Mathf.RoundToInt((float)price * (100f - (float)reduction) / 100f);

            BountyManager.instance.Variables.SetVariable("@MedicPrice", price);
            BountyManager.instance.Variables.SetVariable("@MedicReduction", reduction);
            BountyManager.instance.Variables.SetVariable("@RoomResult", false);

            if (BountyManager.instance.camp.GetResource(0) >= price)
            {
                BountyManager.instance.Variables.SetVariable("@RoomResult", true);
            }
        }
    }

    /// <summary>
    /// called when the room pre check is complete maybe directly 
    /// </summary>
    /// <param name="partyMember"></param>
    public void EnterRoom(int partyMember = -1)
    {

        if (currentRoom == CampRoomType.Merchant)
        {
            MainGuiController.instance.inventoryGui.Open(1);
            MainGuiController.instance.mainTabBar.OpenTab(MainTabButton.MainTabType.Inventory, true);
            MainGuiController.instance.RegisterGuiSwitch(false);

            //MainGuiController.instance.mainTabBar.tab
        }
        else if (currentRoom == CampRoomType.Prison)
        {
            MainGuiController.instance.survivorGui.Open(baseInstance.slaves, DoSlavePurchase, baseInstance.GetProperty(BountyBaseProperty.ItemPriceOff.ToString()).AsInt());
        }
        else if (currentRoom == CampRoomType.MerchantAnimals)
        {
            MainGuiController.instance.animalTradeGui.Open(baseInstance.animals, DoAnimalPurchase);
        }
        else if (currentRoom == CampRoomType.Bar)
        {
            MainGuiController.instance.campGui.ShowRoomGui(currentRoom);
        }
        else if (currentRoom == CampRoomType.Arena)
        {
            if (BountyManager.instance.Variables.GetVariable("@ArenaLevel").AsInt() == 1)
            {
                BountyManager.instance.combatManager.IgnoreFormationOverride = true;
                BountyCharacter res = SDResources.Load<BountyCharacter>("Character/Barricade_Player");
                for (int i = 0; i < 3; i++)
                {
                    BountyManager.instance.ExtraSpawn.Add(Instantiate<BountyCharacter>(res));
                    BountyManager.instance.ExtraSpawn[i].ChangeTalentLevel(BountyTalentType.Talent_Guarding, 2, true, false);
                    BountyManager.instance.ExtraSpawn[i].Setup(0, new CharacterCreationInfo("spawned arena barricade"));
                    BountyManager.instance.ExtraSpawn[i].Row = 1;
                    BountyManager.instance.ExtraSpawn[i].Slot = i;
                }
            }

            BountyManager.instance.IgnoreTempPartyMember = true;

            BountyManager.instance.StartLootLocation(31);
            BountyManager.instance.combatManager.NonLethal = true;
            BountyManager.instance.combatManager.ArenaCombat = true;
            BountyManager.instance.camp.ChangeResource(0, -BountyManager.instance.Variables.GetVariable("@ArenaCost").AsInt(), false, true);
            //BountyManager.instance.combatManager.SetLootEventOverrides(0, BountyManager.instance.PartyPosition == new Vector2Int(20, 18) ? 1 : 0, -1);
        }
        else if (currentRoom == CampRoomType.Medical)
        {
            int price = BountyManager.instance.Variables.GetVariable("@MedicPrice").AsInt();
            BountyCharacter bc = BountyManager.instance.camp.GetCombatSortedParty()[partyMember];
            if (bc.HealthPercent == 100)
            {
                MainGuiController.instance.notificationPanel.ShowNotification("Info_AlreadyHealed");
            }
            else if (BountyManager.instance.camp.GetResource(0) >= price)
            {
                BountyManager.instance.camp.ChangeResource(0, -price, false, true);
                bc.HealthPercent = 100;
                MainGuiController.instance.Broadcast("UpdateGui");
            }
        }
    }

    public void TeleportModelToNode(BountyCharacter character, BaseNavNode.NodeType nt, BaseNavNode.StationType st, bool chooseRandom)
    {
        List<BaseNavNode> list = new List<BaseNavNode>();
        list = baseNav.Nodes.FindAll(n => n.nodeType == nt && (st == BaseNavNode.StationType.Any || n.stationType == st) && !n.occupied && n.IsUnlocked() && !n.dontSpawnModelsRandomly);
        if (list.Count == 0)
            return;
        BaseNavNode target = null;
        if (chooseRandom)
        {
            target = list[SDRandom.Range(0, list.Count)];
        }
        else
        {
            target = list[0];
        }
        character.Model.CurrentIdleData = new BaseIdleData[0];
        character.Model.StopIdleData();

        character.Model.pathfinding.Teleport(target.transform.position, true);
        character.Model.pathfinding.rotation = target.transform.rotation;
        character.Model.myAnimator.rootPosition = character.Model.transform.position;
        character.Model.myAnimator.rootRotation = character.Model.transform.rotation;


    }


    public bool DoSlavePurchase(BountyCharacter pChar)
    {
        if (pChar != null)
        {
            int tPrice = Mathf.RoundToInt(pChar.price * (1f - baseInstance.GetProperty(BountyBaseProperty.ItemPriceOff.ToString()).AsInt() / 100f));
            if (BountyManager.instance.camp.GetResource(0) >= tPrice)
            {
                BountyManager.instance.camp.ChangeResource(0, -tPrice);
                BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.SurvivorBought, pChar.uniqueId, pChar.survivorType.ToString() });
                BountyManager.instance.camp.pendingJoiningSurvivors.Add(pChar);
                baseInstance.slaves.Remove(pChar);
                return true;
            }
            else
                return false;
        }
        else
        {
            return true;
        }
    }
    public bool DoAnimalPurchase(BountyAnimal pChar)
    {
        if (pChar != null)
        {
            if (BountyManager.instance.camp.GetResource(0) >= pChar.price)
            {
                BountyManager.instance.camp.ChangeResource(0, -pChar.price);
                BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.AnimalBought, pChar.type.ToString() });
                BountyManager.instance.factionManager.PlayerFaction.bases[0].animals.Add(pChar);
                baseInstance.animals.Remove(pChar);
                return true;
            }
            else
                return false;
        }
        else
        {
            return true;
        }

    }

    public bool IsInsideView(Vector3 bm)
    {
        Vector3 pointOnScreen = baseCam.worldCam.WorldToScreenPoint(bm);
        return (pointOnScreen.x > 0f && pointOnScreen.x < Screen.width);
    }
    public bool TeleportModelToViewBorder(BountyModel bm, Vector3 target)
    {
        Vector3 tStaircasePos = Vector3.one * 9999f;
        if (staircasePos != null)
        {
            tStaircasePos = staircasePos.position;
        }
        return TeleportModelToViewBorder(bm, baseCam.worldCam, new List<BaseNavNode>(baseNav.Nodes), target, tStaircasePos);
    }
    public static bool TeleportModelToViewBorder(BountyModel bm, Camera cam, List<BaseNavNode> nodes, Vector3 target, Vector3 staircasePos)
    {
        // teleport character to border of cam frustrum?
        Vector3 pointOnScreen = cam.WorldToScreenPoint(bm.transform.position);
        Vector3 staircasePosOnScreen = cam.WorldToScreenPoint(staircasePos);
        bool staircaseOnScreen = !(staircasePosOnScreen.x < 0f || staircasePosOnScreen.x > Screen.width);

        if (pointOnScreen.x < 0f || pointOnScreen.x > Screen.width)
        {
            int index = pointOnScreen.x < 0f ? 0 : 1;
            Plane plane = GeometryUtility.CalculateFrustumPlanes(cam)[index];
            Vector3 sweepDir = pointOnScreen.x < 0f ? -cam.transform.right : cam.transform.right;
            plane.Translate(-sweepDir * 3f);
            float bestY = staircaseOnScreen ? bm.transform.position.y : target.y;
            // teleport to border waypoint 
            nodes.RemoveAll(n => n.occupied);
            nodes.RemoveAll(n => n.dontTeleportTo);
            List<Transform> positions = nodes.ConvertAll<Transform>(n => n.transform);
            positions.RemoveAll(n => plane.GetSide(n.position));
            positions.Sort((a, b) =>
            {
                // compare by y diff
                if (Mathf.Abs(Mathf.Abs(a.position.y - bestY) - Mathf.Abs(b.position.y - bestY)) > 3f)
                {
                    return Mathf.Abs(a.position.y - bestY).CompareTo(Mathf.Abs(b.position.y - bestY));
                }
                else
                {
                    return a.position.x.CompareTo(b.position.x) * (int)Mathf.Sign(sweepDir.x);
                }
            });
            bm.CurrentIdleData = new BaseIdleData[0];
            bm.StopIdleData();
            if (positions.Count == 0)
                return false;
            //Debug.Log(string.Join(",", positions.ConvertAll<string>(n => n.ToString()).ToArray(), 0, 6));

            //Debug.Log("Teleported "+bm.name+" to pos:"+positions[0].position.ToString());
            bm.pathfinding.Teleport(positions[0].position, true);
            bm.pathfinding.rotation = positions[0].rotation;

            //bm.transform.position = positions[0].position;
            bm.myAnimator.rootPosition = bm.transform.position;

            //bm.transform.rotation = positions[0].rotation;
            bm.myAnimator.rootRotation = bm.transform.rotation;

            return true;
        }
        else
        {
            return false;
        }
    }

    protected void TeleportModel(BountyModel bm, Vector3 position)
    {
        bm.CurrentIdleData = new BaseIdleData[0];
        bm.StopIdleData();

        //Debug.Log("Teleported "+bm.name+" to pos:"+positions[0].position.ToString());
        bm.pathfinding.Teleport(position, true);
        bm.myAnimator.rootPosition = bm.transform.position;

    }

    /// <summary>
    /// returns the display name of a local person in the base UNUSED
    /// </summary>
    /// <returns></returns>
    public string GetLocalName()
    {
        return GetBaseStructure().baseRooms[currentRoomIndex].character.CharName;
        // string s = "Job_"+currentRoom.ToString();
        // if (GetBaseStructure().baseRooms[currentRoomIndex].character.female)
        // 	s += "_f";
        // else
        // 	s += "_m";

        // s = Localization.Get(s);
        // if (GetBaseStructure().baseRooms[currentRoomIndex].showCharName)
        // {
        // 	s += " "+ GetBaseStructure().baseRooms[currentRoomIndex].character.CharName;
        // }

        // return s;
    }
    public bool IsLocalFemale()
    {
        return GetBaseStructure().baseRooms[currentRoomIndex].character.female;
    }
    public BountyPortrait GetLocalPortrait()
    {
        return GetBaseStructure().baseRooms[currentRoomIndex].character.portraitData;
    }
    public BountyCharacter GetLocalChar()
    {
        return GetBaseStructure().baseRooms[currentRoomIndex].character;
    }

    public void AddCharacterReplacement(string a, string b)
    {
        // b ersetzt a
        //baseInstance.characterReplacements.Add(new StringPair(a,b));
        if (gameObject.activeInHierarchy)
        {
            BountyModel[] models = GetComponentsInChildren<BountyModel>();
            for (int j = 0; j < GetBaseStructure().baseRooms.Length; j++)
            {
                if (GetBaseStructure().baseRooms[j].character.characterId == a)
                {
                    BountyCharacter cOld = GetBaseStructure().baseRooms[j].character;
                    GetBaseStructure().baseRooms[j].character = BountyManager.instance.characterDatabase.LoadCharacterResource(b);

                    for (int k = 0; k < models.Length; k++)
                    {
                        if (models[k].name.Remove(models[k].name.Length - ("(Clone)").Length) == cOld.modelPrefab.name)
                        {
                            Destroy(models[k].gameObject);
                            break;
                        }
                    }
                    break;
                }
            }
        }

    }
    public void AddDialogueReplacement(int a, int b)
    {
        // b ersetzt a
        baseInstance.AddDialogueReplacement(a, b);

        // replace dialogues
        for (int i = 0; i < baseInstance.dialogueReplacements.Count; i++)
        {
            for (int j = 0; j < GetBaseStructure().baseRooms.Length; j++)
            {
                if (GetBaseStructure().baseRooms[j].enterDialogue == baseInstance.dialogueReplacements[i].key)
                {
                    GetBaseStructure().baseRooms[j].enterDialogue = baseInstance.dialogueReplacements[i].value;
                    break;
                }
            }
        }
    }
}

/// <summary>
/// used to determine how the base behaves under control of a certain faction
/// </summary>
[System.Serializable]
public class BaseDataStructure
{
    public Faction faction;

    [NonReorderable]
    public BaseRoomDefinition[] baseRooms;
    public ShopTemplate shopTemplate;
    [Tooltip("Charactere die in der basis leben und darum mitgespawnt werden")]
    public BountyCharacter[] inhabitants;
    [Tooltip("models die deko mäßig mitgespawnt werden")]
    public BountyModel[] inhabitantModels;
    public EncounterTemplate[] defenders;
    public AnimalTradeEntry[] animalTrades;
    public SurvivorTradeEntry[] survivorTradeEntries;


    public BaseRoomDefinition GetRoomDefinition(CampRoomType room)
    {
        for (int i = 0; i < baseRooms.Length; i++)
        {
            if (baseRooms[i].roomType == room)
            {
                return baseRooms[i];
            }

        }
        return null;
    }

}

/// <summary>
/// used to determine which rooms exist in an outpost or base (except player camp) and what to do there
/// </summary>
[System.Serializable]
public class BaseRoomDefinition
{
    public CampRoomType roomType;
    [ListSelection("Dialogue", true, true)]
    public int enterDialogue;
    //public bool showCharName;
    public BountyCharacter character;
}

/// <summary>
/// used to store persistent base data on disk
/// </summary>
[fsObject]
[System.Serializable]
public class BountyBaseInstance
{
    [fsProperty]
    public List<BaseItem> shopInventory = new List<BaseItem>();
    [fsProperty]
    public List<BaseItem> shopPendingInventory = new List<BaseItem>(); // probably unused in the future?
    [fsProperty]
    public List<BaseItemDefinition> shopAdditionalInventory = new List<BaseItemDefinition>();
    [fsProperty]
    public int lastShopRefill = -1; // datum des tages
    [fsProperty]
    public int playerReputationUpFlag = 0; // auf welches ansehen der spieler zuletzt gesprungen ist 0 = leer 1-3 sind die groben ansehen-ränge
    [fsProperty]
    public int state = 0; // 0 = normal, 1 = destroyed, 2 = blocked by faction war, 3 = blocked by event
    [fsProperty]
    public bool visited = false;
    [fsProperty]
    public int arenaLevel = 0;
    [fsProperty]
    public List<StringPair> characterReplacements = new List<StringPair>(); // key gets replaced by value
    [fsProperty]
    public List<IntPair> dialogueReplacements = new List<IntPair>(); // key gets replaced by value
    [fsProperty]
    public List<int> shopPoolBlockage = new List<int>(); // indezes der shoop pools (0-2) die grade nicht gefüllt werden dürfen
    [fsProperty]
    public List<int> shopPoolForce = new List<int>(); // indezes der shoop pools (0-2) die gefüllt werden dürfen obwohl der ruf eig nicht ausreicht
    [fsProperty]
    public bool refillAdditionalItems = false;
    [fsProperty]
    public List<int> refillForce = new List<int>(); // indezes der shoop pools (0-2) die gefüllt werden

    public BountyBaseInstance() { }

    public List<BaseItem> ShopInventory
    {
        get
        {
            List<BaseItem> result = new List<BaseItem>(shopInventory);
            result.Sort(BountyCamp.ItemSort);
            return result;
        }
    }

    public void SetReplacementEntry(StringPair entry, bool removeEntry)
    {
        if (removeEntry)
        {
            characterReplacements.RemoveAll(n => n.key == entry.key);
        }
        else
        {
            if (characterReplacements.Exists(n => n.key == entry.key))
            {
                characterReplacements.Find(n => n.key == entry.key).value = entry.value;
            }
            else
            {
                characterReplacements.Add(entry);
            }
        }
    }

    public void AddDialogueReplacement(int a, int b)
    {
        if (dialogueReplacements.Exists(n => n.key == a))
        {
            dialogueReplacements.Find(n => n.key == a).value = b; // replace
        }
        else
        {
            dialogueReplacements.Add(new IntPair(a, b)); // add
        }
    }
}

[System.Serializable]
public class BaseRoomModel
{
    public CampRoomType roomType;
    public GameObject[] tiers;
    public GameObject upgradeModel;
}

[System.Serializable]
[fsObject]
public class StringPair
{
    [fsProperty]
    public string key;
    [fsProperty]
    public string value;

    public StringPair() { }

    public StringPair(string a, string b)
    {
        key = a;
        value = b;
    }
}

[System.Serializable]
[fsObject]
public class IntPair
{
    [fsProperty]
    public int key;
    [fsProperty]
    public int value;

    public IntPair() { }

    public IntPair(int a, int b)
    {
        key = a;
        value = b;
    }
}

[System.Serializable]
public class AnimalTradeEntry
{
    public BountyAnimal template;
    //public BountyAnimal.AnimalType type;
    public int amount;
}

public enum SurvivorType
{
    None = -1,
    Normal = 0,
    Worker = 1,
    Soldier = 2,
    Slave = 3,
}

[System.Serializable]
public class SurvivorTradeEntry
{

    public SurvivorType type;
    public int amount;
}