using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using FullSerializer;
using System.IO;
using System.IO.Compression;
using Pathfinding;
using Pathfinding.RVO;
using UnityEngine.TextCore.Text;
using static NewBases;

/// <summary>
/// -central control script for the game flow. holds important data about the current state of the game eg: in camp, fight, traveling
/// -is a semi-static singleton and always active. holds all the database objects and references. manages saving and loading
/// -also manages pre-game stage
/// </summary>
//[fsObject(MemberSerialization = fsMemberSerialization.OptOut)]
public partial class BountyManager : MonoBehaviour, IGameManager
{
	public int saveFileVersion = 1;
	public int depricatedFileVersion = 1;
	public int settingsFileVersion = 1;
	public int newsVersion = 1;

	public const int Manager_Dialogue = 1;
	public const int Manager_Faction = 2;
	public const int Manager_TravelEvent = 3;
	public const int Manager_Story = 4;
	public const int Manager_Scenario = 5;
	public const int Manager_Camp = 6;
	public const int Manager_Combat = 7;
	public const int Manager_Ai = 8;
	public const int Manager_Quest = 9;
	public const int Manager_LootLocation = 10;
	public const int Manager_LootEvent = 11;
	public const int Manager_Character = 12;
	public const int Manager_MapEvent = 13;
	public const int Manager_Persistent = 14;
	public const int Manager_Achievement = 15;

	
	public static bool openUpgradesInMainMenu;
	public static bool openLastSaveInMainMenu;
	public static bool openCreditsBeforeHand;
	public static int lastSaveSlot;

	public static int exceptionState = 0;
	public static int menuStarts = 0;
	public static bool debugMode;
    private static BountyManager _instance;
    private static bool _isDestroyed;

	private bool isTutorial=false;//Determining whether or not a player is in the middle of a tutorial process
	private bool shortcutLocked = false;//Locking Shortcuts

    public bool IsTurorial
	{
		get {  return isTutorial; }
		set {  isTutorial = value; }
	}

	public bool ShortcutLocked
	{
		get { return shortcutLocked; }
		set { shortcutLocked = value; }
	}

    public static int maxKapitel = 0;
	public int currentKapitel = 0;
    public static BountyManager instance // sometimes instantiated by BountyManagerLoader.ResourceLoadCallback
    {
        get
        {
			if (Application.isPlaying && _isDestroyed)
			{
				return null;
			}
            if (_instance == null)
            {
                _instance = FindObjectOfType<BountyManager>();
                if (_instance != null)
                {
	                _instance.SetInstance();
                }
            }
            return _instance;
        }
    }
	public static IGameManager InterfaceInstance
	{
		get
		{
			if (Application.isPlaying && _isDestroyed)
			{
				return null;
			}
			if (_instance == null)
			{
				_instance = FindObjectOfType<BountyManager>();
				if (_instance != null)
					_instance.SetInstance();
			}
			return _instance;
		}
	}

	public static void TestException()
	{
		throw new SDGeneralException("TestException");
	}
	public static void HandleEventException(SDEventException e)
	{
		string log = string.Format("Cought SDEventException: {0} with parameters: eventblock={1} state={2} functionIndex={3} function={4}\n{5}",e.Message, e.Eventblock, e.StateId, e.FunctionIndex, e.FunctionId, e.StackTrace);
		if (e.InnerException != null)
			log += string.Format("\n------>Inner Exception: {0}\n{1}", e.InnerException.Message, e.InnerException.StackTrace);

		Debug.LogError(log);

		if (exceptionState == 0 && instance != null)
		{
			exceptionState = 1;
			instance.GenerateCrashReport(log); // temporarly disabled
			Debug.Break();
		}
	}
	public static void HandleDialogueException(SDEventException e)
	{
		string log = string.Format("Cought SDEventException: {0} with parameters: dialogue={1} node={2} functionIndex={3} function={4}\n{5}", e.Message, e.Eventblock, e.StateId, e.FunctionIndex, e.FunctionId, e.StackTrace);
		if (e.InnerException != null)
			log += string.Format("\n------>Inner Exception: {0}\n{1}", e.InnerException.Message, e.InnerException.StackTrace);

		Debug.LogError(log);

		if (exceptionState == 0 && instance != null)
		{
			exceptionState = 1;
			instance.GenerateCrashReport(log); // temporarly disabled
			Debug.Break();
		}
	}
	public static void HandleGeneralException(SDGeneralException e)
	{
		string log = string.Format("Cought SDGeneralException: {0} with information: \n{1}", e.Message, e.extraInfo);

		if (e.InnerException != null)
			log += string.Format("\n------>Inner Exception: {0}\n{1}", e.InnerException.Message, e.InnerException.StackTrace);

		Debug.LogError(log);
		
		if(exceptionState == 0 && instance != null)
		{
			exceptionState = 1;
			instance.GenerateCrashReport(log); // temporarly disabled
			Debug.Break();
		}
	}
	public static void HandleCombatError(string message)
	{
		Debug.LogError(message);

		if (exceptionState == 0 && instance != null)
		{
			exceptionState = 1;
			instance.GenerateCrashReport(message); // temporarly disabled
			Debug.Break();
			// here is a bug that I found that can this:
			// if a character attempts to move to a target (BountyModel.LinearRun) but they do not need to move since they are already at the target location,
			// the character is stuck in the Weapons Idle Animation Layer.
			
			// This is the reason: The Character transitions out of the Weapon's Idle Animation Layer by walking, which they would do if they had any distance to travel
			// or, through code explicitly triggering the LeaveIdle animation-Trigger. This Trigger is used when CombatManager.UseSkill detects that the character
			// does not need to move to the target. However, the Combat Manager determines the distance to the target through the charactes' slots and rows, and not their positions. 
			// When a character's slot and row is not updated to reflect their Ingame position after a previous Movement, the CombatManager mistakenly tells the character
			// to move instead of triggering LeaveIdle. Since the character doesn't need to move they don't transition out of the Idle Animation through walking either,
			// making the character stuck in a Weapon's Idle Animation Layer.
			// This is what happens on a correct run: MeleeAttackLayer --(walking > 0)-> Walking Layer --> BaseLayer --(Attack_Melee)-> MeleeAttackLayer.
			// or not walking: MeleeAttackLayer --(LeaveIdle)-> BaseLayer --(Attack_Melee)-> MeleeAttackLayer
			
			// The next calls to Perform a skill are done through Animations, this is why combat is stuck when a character is suck in an Idle animation.
			// I am telling you this, so you know to check the animations and their transitions, for clues on why combat is stuck.
		}
	}

	private void UnHandledException(string condition, string stackTrace, LogType type)
	{
		if (type == LogType.Exception)
		{
			string log = "Uncought Exception";
			log += string.Format("\n------>Condition: {0}\n{1}", condition, stackTrace);

			if (exceptionState == 0 && instance != null)
			{
				exceptionState = 2;
				instance.GenerateCrashReport(log); // temporarly disabled
				Debug.Break();
			}
		}
	}

	private void GenerateCrashReport(string exceptionText)
	{
		// create crash report data


#if UNITY_EDITOR
		string file = Application.dataPath;
		// trim the "Assets/" part but only in editor mode right?!
		file = file.Substring(0, file.Length - 6);

#else
		string file = Application.persistentDataPath;

#endif

#if UNITY_EDITOR
		if(UnityEditor.EditorPrefs.GetBool("DontSaveCrashReports", false))
		{
			return;
		}
#endif
		if (!Directory.Exists(file + "/Crash reports"))
			Directory.CreateDirectory(file + "/Crash reports");

		string crashFile = file + "/Crash reports/" + GetDateString();
		Directory.CreateDirectory(crashFile);
		SaveScreenShot("Crash_Image", "Crash reports/" + GetDateString() + "/");
		bool result = SaveGameInformation("Crash reports/" + GetDateString() + "/", exceptionText);


		if (result)
			StartCoroutine(CrashReportRoutine(crashFile));
		
	}


	private IEnumerator CrashReportRoutine(string crashFile)
	{
		yield return null;
#if !UNITY_STANDALONE_OSX
		//ZipFile.CreateFromDirectory(crashFile, crashFile + ".zip");
		yield return null;
#endif
		MainGuiController.instance.infoPopup.Open("Label_DA_EarlyAccess","Info_CrashReport", "Button_OpenFolder", CrashCallBack, true, "Button_OK");
	}

	public void CrashCallBack(bool value)
	{
#if UNITY_EDITOR
		string file = Application.dataPath;
		// trim the "Assets/" part but only in editor mode right?!
		file = file.Substring(0, file.Length - 6);

#else
		string file = Application.persistentDataPath;

#endif
		if (value)
		{
			Application.OpenURL("file://" + file + "/Crash reports");

		}
		if (exceptionState == 1)
			BackToMainMenu(false);
		if (exceptionState == 2)
			Application.Quit(1);
	}


#region editor_related
	public delegate string[] stringDel();
    public delegate int[] intDel();

    [fsIgnore]
    public Dictionary<string, stringDel> stringLists;
    [fsIgnore]
    public Dictionary<string, intDel> intLists;

#if UNITY_EDITOR
	[HideInInspector]
	public List<System.Type> aotTypeList;

	private void PopulateAOTTypeList()
	{
		aotTypeList = new List<System.Type>()
		{
			typeof(BountyBuff),
			typeof(BountyCamp),
			typeof(BountyResourceEntry),
			typeof(JobInstance),
			typeof(BountyCharacter),
			typeof(CharacterStateEntry),
			typeof(CharAttributeEntry),
			typeof(CharTalentEntry),
			typeof(CharacterEquipmentSlot),
			typeof(BountyPortrait),
			typeof(BountyDamage),
			typeof(BountySaveData),
			typeof(BountySaveHeader),
			typeof(CampDefenceData),
			typeof(SettingsData),
			typeof(MetaInfo),
			typeof(BountyQuestManager),
			typeof(QuestDataInstance),
			typeof(SubQuestInstance),
			typeof(FactionManager),
			typeof(FactionEntry),
			typeof(NotificationData),
			typeof(FormatTextToken),
			typeof(SpriteData),
			typeof(BaseItem),
			typeof(AttributeModifier),
			typeof(MapEventDatabase),
			typeof(MapEventInstance),
			typeof(WayPointAnimation),
			typeof(MapData),
			typeof(WayPointData),
			typeof(AreaData),
			typeof(AreaSpecialOverride),
			typeof(PersistentManager),
			typeof(BountyUpgradeInstance),
			typeof(BountyPersistentStatInstance),
			typeof(SurvivalOptionData),
			typeof(BountyBaseInstance),
			typeof(StringPair),
			typeof(IntPair),
			typeof(SDDialogueManager),
			typeof(SDDialogue),
			typeof(SDDialogueNode),
			typeof(EventManager),
			typeof(EventManagerData),
			typeof(EventBlockInstance),
			typeof(VariableDatabase),
			typeof(VariableEntry),
			typeof(StoryDatabase),
			typeof(StoryArchieveEntry),
		};
	}

    [UnityEditor.Callbacks.DidReloadScripts]
    public static void CheckInstance()
    {
        if (instance != null)
            instance.ToString();

		FixCulture();

	}
	[ContextMenu("Toggle Crash Reporting")]
	private void ToggleCrashReporting()
	{
		bool tValue = !UnityEditor.EditorPrefs.GetBool("DontSaveCrashReports", false);
		UnityEditor.EditorPrefs.SetBool("DontSaveCrashReports",tValue);
		Debug.LogFormat("Toggled: >DontSaveCrashReports< to: {0}", tValue);
	}
#endif

	[ContextMenu("SetInstance")]
    public void SetInstance()
    {
		//Debug.Log("setting instance of manager");
		_instance = this;
        
//#if UNITY_EDITOR
        stringLists = new Dictionary<string, stringDel>()
        {
            {"TravelEvent", travelEventDatabase.GetEventNames},
            {"Scenario", scenarioManager.GetScenarioNames},
			{"ScenarioPart", scenarioPartDatabase.GetAssetNames},
			{"Resource", GetResourceNames},
            {"Character", characterDatabase.GetCharacterNames},
            {"EventBlock", eventManager.GetBlockNames},
            {"GameEvent", GetEnumNames<BountyGameEvent>},
            {"Quest", questManager.GetQuestNames},
            {"NavNodeType", GetEnumNames<BaseNavNode.NodeType>},
            {"NavStationType", GetEnumNames<BaseNavNode.StationType>},
            {"LootLocation", lootLocationDatabase.GetEntryNames},
            {"Dialogue", dialogueManager.GetDialogueStrings},
            {"LootEvent", lootEventDatabase.GetEventNames},
            {"Talent", GetEnumNames<BountyTalentType>},
            {"Attribute", GetEnumNames<BountyCharAttribute>},
            {"MapEvent", mapEventDatabase.GetEventNames},
            {"Faction", GetEnumNames<Faction>},
            {"Achievement", achievementDatabase.GetAchievementNames},
			{"Stat", GetEnumNames<BountyPersistentStat>},
			{"DialogueAutoEvent", GetEnumNames<DialogueAutoEvent>},
            {"ButtonTag", GetEnumNames<ButtonAPI.ButtonTag>},
            {"ItemType", GetEnumNames<BaseItem.ItemType2>},
            {"EncounterType", GetEnumNames<EnemyEncounterEntry.EnemyEncounterType>},
            {"MainTab", GetEnumNames<MainTabButton.MainTabType>},
            {"RoomType", GetEnumNames<CampRoomType>},
            {"AIRuleType", GetEnumNames<AIRuleType>},
            {"LootEventMessage", GetEnumNames<LootEventBuiltinMessage>},
			{"SpeechBubble", GetEnumNames<SpeechBubbleType>},
			{"BaseState", GetEnumNames<CharacterBaseState>},
			{"AnimationEvent", GetEnumNames<BountyAnimationEvent>},
			{"Labyrinth", labyrinthDatabase.GetAssetNames},
			{"Biome", GetEnumNames<Biome>},
			{"BaseProperty", GetEnumNames<BountyBaseProperty>},
			{"ShopTemplate", shopDatabase.GetAssetNames},
			{"BaseIdle", baseIdleDatabse.GetAssetNames},
			{"AnimalType", GetEnumNames<BountyAnimal.AnimalType>},
			{"Recipe", craftingDatabase.GetAssetNames},
			{"SurvivorType", GetEnumNames<SurvivorType>},
		};
        intLists = new Dictionary<string, intDel>()
        {
            {"TravelEvent", travelEventDatabase.GetEventIds},
            {"Scenario", scenarioManager.GetScenarioIds},
			{"ScenarioPart", scenarioPartDatabase.GetAssetIds},
			{"Character", characterDatabase.GetCharacterIds},
            {"EventBlock", eventManager.GetBlockIds},
			{"GameEvent", GetEnumInts<BountyGameEvent>},
			{"Quest", questManager.GetQuestIds},
            {"LootLocation", lootLocationDatabase.GetEntryIds},
            {"Dialogue", dialogueManager.GetDialogueIds},
            {"LootEvent", lootEventDatabase.GetEventIds},
            {"Talent", GetEnumInts<BountyTalentType>},
            {"Attribute", GetEnumInts<BountyCharAttribute>},
            {"MapEvent", mapEventDatabase.GetEventIds},
			{"Faction", GetEnumInts<Faction>},
			{"Achievement", achievementDatabase.GetAchievementIds},
			{"Stat", GetEnumInts<BountyPersistentStat>},
			{"ButtonTag", GetEnumInts<ButtonAPI.ButtonTag>},
            {"ItemType", GetEnumInts<BaseItem.ItemType2>},
			{"BaseState", GetEnumInts<CharacterBaseState>},
			{"AnimationEvent", GetEnumInts<BountyAnimationEvent>},
			{"Labyrinth", labyrinthDatabase.GetAssetIds},
			{"Biome", GetEnumInts<Biome>},
			{"BaseProperty", GetEnumInts<BountyBaseProperty>},
			{"ShopTemplate", shopDatabase.GetAssetIds},
			{"BaseIdle", baseIdleDatabse.GetAssetIds},
			{"AnimalType", GetEnumInts<BountyAnimal.AnimalType>},
			{"Recipe", craftingDatabase.GetAssetIds},
			{"SurvivorType", GetEnumInts<SurvivorType>},
		};
//#endif
        //dialogueManager.CacheEntries();

        eventManager.SetInstance();
    }

    public string[] GetEnumNames<T>()
    {
        return System.Enum.GetNames(typeof(T));
    }
    public int[] GetEnumInts<T>()
    {
        return (int[])System.Enum.GetValues(typeof(T));
    }
    public string[] GetListNames()
    {
        List<string> result = new List<string>();
        result.Add("<NONE>");
        result.AddRange(stringLists.Keys);
        return result.ToArray();
    }

    public string[] GetResourceNames()
    {
        return new string[] { "Dollar", "Wasser", "Nahrung", "Material", "Schrott", "Medizin", "Munition", "Eisen", "Wolfram" };
    }

    public List<KeyValuePair<string, object>> GetVariableList(string filter)
    {
        List<KeyValuePair<string, object>> result = new List<KeyValuePair<string, object>>();

        result.Add(new KeyValuePair<string, object>("<NONE>", string.Empty));

        string[] keys = eventManager.GetVariableDatabase().GetVariableNames();
        for(int i = 0; i < keys.Length; i++)
        {
            if(string.IsNullOrEmpty(filter) || keys[i].ToLower().Contains(filter.ToLower()))
            {
                result.Add(new KeyValuePair<string, object>(keys[i], eventManager.GetVariableDatabase().GetVariable(keys[i]).AsString()));
            }
        }
        return result;
    }

#endregion

    [Header("Datenobjekte")]
    [fsIgnore]
    [SerializeField]
    private ManagerCollection managerPrefabs;
    [fsProperty]
    public FactionManager factionManager;
    [fsIgnore]
    public SDDialogueManager dialogueManager;
    [fsIgnore]
    public TravelEventDatabase travelEventDatabase;
    [fsIgnore]
    public StoryDatabase storyDatabase;
    [fsIgnore]
    public BountyScenarioManager scenarioManager;
	[fsIgnore]
	public SDAssetDatabase scenarioPartDatabase;
	[fsIgnore]
	public SDAssetDatabase scenarioSceneDatabase;
	[fsIgnore]
    public EventManager eventManager;
    [fsProperty]
    public BountyCamp camp;
    [fsIgnore]
    public BountyCombatManager combatManager;
    [fsProperty]
    public BountyCombatAI combatAi;
    [fsIgnore]
    public AudioManager audioManager;
    [fsIgnore]
    public BountyInputManager inputManager;
    [fsProperty]
    public BountyQuestManager questManager;
    [fsIgnore]
    public LootLocationDatabase lootLocationDatabase;
    [fsIgnore]
    public LootEventDatabase lootEventDatabase;
    [fsIgnore]
    public CharacterDatabase characterDatabase;
    [fsProperty]
    public MapEventDatabase mapEventDatabase;
    [fsProperty]
    public PersistentManager persistentManager;
    [fsIgnore]
    public ModelManager modelManager;
	[fsIgnore]
	public SpriteAtlasManager spriteAtlasManager;
	[fsIgnore]
    public BountyAchievementDatabase achievementDatabase;
	[fsIgnore]
	public SDAssetDatabase labyrinthDatabase;
	[fsIgnore]
	public SDAssetDatabase shopDatabase;
	[fsIgnore]
	public SDAssetDatabase baseIdleDatabse;
	[fsIgnore]
	public CraftingDatabase craftingDatabase;
    [fsProperty]
    public NewBases NonPlayerBase;
    [fsProperty]
    public BountyCharacter[] NonPlayerBaseCharacters;
	public GameObject ButtonSpawner;

    // stores all the locations ordered: HQ, OP1 then OP2 grouped by the 3 factions: indipendents, military, smugglers
    [fsIgnore]
    [HideInInspector]
    public static readonly Vector2Int[,] baseCoords = new Vector2Int[,]{
        { new Vector2Int(7,21), new Vector2Int(10,10), new Vector2Int(17,2) },
        { new Vector2Int(2,19), new Vector2Int(3,11), new Vector2Int(4,2) },
        { new Vector2Int(20,18), new Vector2Int(19,9), new Vector2Int(9,4) },
    };

    
    [fsIgnore]
    [SerializeField]
    private int debugStartDay;
    [fsIgnore]
    private bool slowMode = false; // debug lowmode
	[SerializeField]
	private bool debugLoadRNG;

	[fsIgnore]
	[SerializeField]
	private AudioClip gameOverMusic;

    [Header("Variablen")]
	[fsIgnore]
	private int saveSlot = 1;
	[fsIgnore]
	private int startFaction = 0;
	[fsIgnore]
	[SerializeField]
	private bool loadGame;
	[fsIgnore]
    public bool earlyAccessMode;
    [fsIgnore]
    public BountyBase campScene;
	//[fsIgnore]
 //   public BountyBase StevenCampScene;
	[fsIgnore][ListSelection("Scenario", true, true)]
	public int campSceneResource;
	[fsIgnore]
    private bool ingame;
	[fsIgnore]
	public bool Ingame
	{
		get { return ingame; }
		private set { ingame = value; }
	}
	//[fsIgnore]
	//private fsSerializer serializer;
	[fsIgnore]
    private bool loading;
    [fsIgnore]
    private bool startup; // active when the game scene opens
    [fsIgnore]
    private bool shutDown; // active when the game scene closes
    [fsIgnore]
    public List<MapGenerator> mapList;
    [fsProperty]
    private VariableDatabase variables; // stores all runtime variables and quest states (may become a bit big during the game eg 100+ entries)
    [fsProperty]
    private bool inCamp; // true if inside player base
    [fsProperty]
    private bool inBase; // true if inside an outpost etc
    [fsProperty]
    private bool inDungeon; // true if inside a cave etc
    [fsProperty]
    private int currentMap; // the current map index, 0 = overworld?
    [fsProperty]
    private int currentFight; // index of fight in a sequence of fights eg 3
    [fsProperty]
    private int fightCount; // number fights to fight in this scenario
	[fsProperty]
	private int startFight; // the fight index this scenario started with
	[fsProperty]
    private int fightType; // distignuish between normal 3 encounter fight, 1 encounter fight or defense fight
    [fsProperty]
    private bool fightActive;
    [fsProperty]
    private bool fightEndPending;
	[fsProperty]
	private bool allFightsDone; // added 23.3.21 when the last combat is done and chars are about to leeve the scene
	[fsProperty]
    private bool eventActive;
    [fsProperty]
    private int day; // survived days
    [fsProperty]
    private bool night; // night or day session
    [fsProperty]
    private int time; // remaining time till end of session
	[fsProperty]
	private int itemCounter; // counts items spawned in this session
    [fsProperty]
    private int session; // is it the first or second run this day
    [fsProperty]
    private bool timeFreeze; // dont porgress time on session switch (tutorial)
    [fsProperty]
    private BountyScenarioEntry currentCombatScenario;
    [fsProperty]
    private List<BaseItem> currentLoot;
    [fsProperty]
    private Faction threadOverride;
    [fsProperty]
    private List<BountyCharacter> levelUpList;
    [fsProperty]
    private int fights; // number of fights in this scene
    [fsProperty]
    private int startFightOverride; // override value for start point in scenarios
	[fsProperty]
	private int onScenarioLeftOverride; // override value for what happens when the scenio is left
	[fsProperty]
    private TravelEventInstance currentTravelEvent;
    [fsProperty]
    private BountyCharacter playerCharacter; // refernce to the players character
    [fsProperty]
    private BountyCharacter currentEventChoiceCharacter; // refernce to the selected victim of ean event (e.g. travel event)
	[fsProperty]
	private BountyCharacter defenceVictim; // a pre selection for the next character to die when camp defence fails
	[fsProperty]
    private List<MapData> mapData; // persistent map data
    [fsProperty]
    private LootLocationDefinition currentLootLocation;
	[fsIgnore]
	private Faction currentCombatFaction;
	[fsProperty]
	private EnemyEncounterEntry lastRandomEncounter;
	[fsIgnore]
	private int[] lootEventOverride = new int[10]; // overrides what loot event spawn in this szenario
	[fsIgnore]
	private List<BountyCharacter> extraSpawn; // used to manually insert etxra combatants to player spawn e.g. barricades in arena
	[fsIgnore]
	private bool ignoreTempPartyMember; // used to manually disable spawn of temp members e.g. in arena fight
	[fsProperty]
    private Dictionary<int, int> sessionsLootEventLog;
	[fsProperty]
	private Dictionary<int, int> sessionsTravelEventLog;
	[fsProperty]
    private List<CampDefenceData> pendingDefenceData;
	[fsProperty]
	private int lastRandomDefenceEvent;
    [fsProperty]
    private SettingsData currentSettings;
	[fsProperty]
	private MetaInfo metaInfos;
	[fsProperty]
    private bool skipIntro = true;
    [fsProperty]
    private GameDifficulty difficulty;
    [SerializeField]
    private BountyCharacter playerPrefab;
    [SerializeField, ListSelection("EventBlock", true, true)]
    private int[] tutorialEventBlocks;
    [SerializeField, ListSelection("EventBlock", true, true)]
    private int[] tutorialDebugBlocks;
    [fsProperty] private int currentTutIndex;
    [fsProperty] private int currentTutEventContext;
	[fsProperty] private int eventCheckModifier;
	[fsProperty] private List<int> pendingDeadEvents; // died outside of combat but not yet checked by event system
	[fsProperty]
	private CampRoomType lastRoomType = CampRoomType.None;
	[fsIgnore]
	private List<int> waveCombatTracker = new List<int>(); // keeps track of the indezes of spawned waves in a combat so we can avoid repeatings
	[fsIgnore]
	private int enteredDungeonId;
	[fsProperty]
	private int backupIndex = 0; // added second backup 30.11.20
	[fsIgnore]
	private Coroutine campToggleRoutine;
	[fsIgnore]
	private bool enterBaseNoFadeOverride;
	[fsIgnore]
	private int loadedSaveFileVersion;
	[fsIgnore]
	private Coroutine timeScaleRoutine;
	[fsProperty]
	private Dictionary<int, List<int>> lootEventTracker; // trackt die gespawnten loot events pro loot location
	[fsIgnore,HideInInspector]
	public bool metaInfoChanged = false;

	// debug
	[SerializeField, fsIgnore] private bool debugBlockDayEvents;
    [SerializeField, fsIgnore] private bool debugStart;
	[SerializeField, fsIgnore] public bool debugAllowScenarioTesting;
	[SerializeField, fsIgnore] private bool debugStartOutpost;
    [SerializeField, fsIgnore] private bool debugStartNight;
	[SerializeField, fsIgnore] private bool debugDontSaveMetaInfo;
	[SerializeField, fsIgnore] private GameDifficulty debugDifficulty = GameDifficulty.Normal;
	[SerializeField, fsIgnore][ListSelection("Const_Location", false, false, true)] private string debugOutpostCoord;
	//[SerializeField, fsIgnore][ListSelection("Scenario", true, true)] private string debugOutpostScenario;
	[fsIgnore] public bool blockDebugCommands;
    [SerializeField, fsIgnore] private int debugTutorialStart = -1;
    [fsIgnore] private string codeWord = "";
    [fsIgnore] private bool variablesDebug = false;
    [fsIgnore] private Vector2 variablesScrollViewVector = Vector2.zero;
	[fsIgnore] private System.Action onQuitGameSession;  // deleate fired when the user quits the game or goes back to menu, the manager reference will become null after that

	[fsIgnore]
	public bool ShutDown
	{
		get { return shutDown; }
	}
	[fsIgnore]
	public int SaveSlot
	{
		get { return saveSlot; }
		set { saveSlot = value; }
	}
	[fsIgnore]
	public int StartFaction
	{
		get { return startFaction; }
		set { startFaction = value; }
	}
	[fsIgnore]
	public bool LoadGame
	{
		get { return loadGame; }
		set { loadGame = value; }
	}
	[fsIgnore]
    public SettingsData Settings
    {
        get { return currentSettings; }
		set { currentSettings = value; }
    }
	[fsIgnore]
	public MetaInfo MetaInfos
	{
		get { return metaInfos; }
	}
	[fsIgnore]
    public MapGenerator Map
    {
        get { return mapList[currentMap]; }
    }
    [fsIgnore]
    public int Day
    {
        get { return day; }
    }
    [fsIgnore]
    public int DateTime
    {
        get { return day * 2 + session; }
    }
    [fsIgnore]
    public int CurrentTime
    {
        get { return time; }
    }
    [fsIgnore]
    public bool Night
    {
        get { return night; }
    }
    [fsIgnore]
    public int Session
    {
        get { return session; }
    }
    [fsIgnore]
    public int FightType
    {
        get { return fightType; }
    }
	[fsIgnore]
	public int CurrentFight
	{
		get { return currentFight; }
	}
	[fsIgnore]
    public bool TimeFreeze
    {
        get { return timeFreeze; }
        set { timeFreeze = value; }
    }
    [fsIgnore]
    public bool InCamp
    {
        get { return inCamp; }
    }
    [fsIgnore]
    public bool InBase
    {
        get { return inBase; }
    }
    [fsIgnore]
    public bool InDungeon
    {
        get { return inDungeon; }
    }
    [fsIgnore]
    public BountyCharacter Player
    {
        get { return playerCharacter; }
        set { playerCharacter = value; }
    }
    [fsIgnore]
    public BountyCharacter EventVictim
    {
        get { return currentEventChoiceCharacter; }
        set { currentEventChoiceCharacter = value; }
    }
    [fsIgnore]
    public Vector2Int PartyPosition
    {
        get { return Map.PartyPosition; }
        set { Map.PartyPosition = value; }
    }
    [fsIgnore]
    public VariableDatabase Variables
    {
        get { return variables; }
    }
    [fsIgnore]
    public List<MapData> MapData
    {
        get { return mapData; }
    }
	[fsIgnore]
	public Faction CurrentCombatFaction
	{
		get { return currentCombatFaction; }
		set { currentCombatFaction = value; }
	}
	[fsIgnore]
    public LootLocationDefinition CurrentLootLocation
    {
        get { return currentLootLocation; }
    }
    [fsIgnore]
    public TravelEventInstance CurrentTravelEvent
    {
        get { return currentTravelEvent; }
    }
	[fsIgnore]
	public BountyScenarioEntry CurrentCombatScenario
	{
		get { return currentCombatScenario; }
	}
	[fsIgnore]
    public Faction ThreadOverride
    {
        get { return threadOverride; }
        set { threadOverride = value; }
    }
    [fsIgnore]
    public bool SkipIntro
    {
        get { return skipIntro; }
        set { skipIntro = value; }
    }
    [fsIgnore]
    public int CurrentTutorialIndex
    {
        get { return currentTutIndex; }
        set { currentTutIndex = value; }
    }
    [fsIgnore]
    public GameDifficulty Difficulty
    {
        get { return difficulty; }
        set { difficulty = value; }
    }
	[fsIgnore]
	public float DifficultyFactor
	{
		get {
			if (difficulty == GameDifficulty.Casual)
				return 0.7f;
			else if (difficulty == GameDifficulty.Hardcore)
				return 1.2f;
			else
				return 1f;
		}
	}
    [fsIgnore]
    public int StartFightOverride
    {
        get { return startFightOverride; }
        set { startFightOverride = value; }
    }
	[fsIgnore]
	public CampRoomType LastRoomType
	{
		get { return lastRoomType; }
		set { lastRoomType = value; }
	}
	[fsIgnore]
	public int OnScneaioLeftOverride
	{
		get { return onScenarioLeftOverride; }
		set { onScenarioLeftOverride = value; }
	}
	[fsIgnore]
	public List<BountyCharacter> ExtraSpawn
	{
		get { return extraSpawn; }
	}
	[fsIgnore]
	public bool IgnoreTempPartyMember
	{
		get { return ignoreTempPartyMember; }
		set { ignoreTempPartyMember = value; }
	}
	[fsIgnore]
	public int EventCheckModifier
	{
		get { return eventCheckModifier; }
		set { eventCheckModifier = value; }
	}
	[fsIgnore]
	public List<CampDefenceData> PendingDefenceData
	{
		get { return pendingDefenceData; }
	}
	[fsIgnore]
	public List<int> WaveCombatTracker
	{
		get { return waveCombatTracker; }
		set { waveCombatTracker = value; }
	}
	[fsIgnore]
	public bool EnterBaseNoFadeOverride
	{
		get { return enterBaseNoFadeOverride; }
		set { enterBaseNoFadeOverride = value; }
	}
	[fsIgnore]
	public Dictionary<int, List<int>> LootEventTracker
	{
		get { return lootEventTracker; }
		set { lootEventTracker = value; }
	}


	//[ContextMenu("Fix Culture")]
	private static void FixCulture()
	{
		// used to fix unity inspector confusing point and comma signs in float numbers because of german number format on german OS
		System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
	}
	/// <summary>
	/// returns true if the game is shutting down
	/// </summary>
	/// <returns></returns>
	public bool IsShutDown()
	{
		return shutDown;
	}

	public T GetSubManager<T>(int managerId) where T : Object
	{
		switch (managerId)
		{
			case 1:
				return dialogueManager as T;
			case 2:
				return factionManager as T;
			case 3:
				return travelEventDatabase as T;
			case 4:
				return storyDatabase as T;
			case 5:
				return scenarioManager as T;
			case 6:
				return camp as T;
			case 7:
				return combatManager as T;
			case 8:
				return combatAi as T;
			case 9:
				return questManager as T;
			case 10:
				return lootLocationDatabase as T;
			case 11:
				return lootEventDatabase as T;
			case 12:
				return characterDatabase as T;
			case 13:
				return mapEventDatabase as T;
			case 14:
				return persistentManager as T;
			case 15:
				return achievementDatabase as T;
			default:
				return null;
		}

	}

	/// <summary>
	/// starts when the game starts
	/// </summary>
	public void Start()
    {
        if (ingame)
            return;

        ingame = true;
		_isDestroyed = false;

		Application.logMessageReceived += UnHandledException;
		SetInstance();

		if(!SDSaveLoad.InitVirtualFiles(StartInternal))
		{
			StartInternal();
		}
		Instantiate(ButtonSpawner);

    }
	/// <summary>
	/// actuall call to start the start procedure
	/// </summary>
	private void StartInternal()
	{
		
		InitSettings();
		audioManager.UpdateMusicVolume();
		audioManager.UpdateEffectVolume();
		if (scenarioManager.CurrentScenario)
		{
			scenarioManager.CurrentScenario.UpdateVolumes();
		}
		else if (campScene)
		{
			campScene.defenseScenario.UpdateVolumes();
		}

		inputManager.Setup();
		if (debugStart)
		{
			SDResources.OnInitialized += InitGame;
			SDResources.Init(this);

			difficulty = debugDifficulty;
			PreparePersistentData();
		}

#if UNITY_EDITOR
		debugMode = true;
		PopulateAOTTypeList();
#endif
	}
	/// <summary>
	/// tries to evaluate the correct ingame language based on the language code given by the operating system
	/// </summary>
	/// <returns></returns>
	public static BountyLanguage GetDefaultLanguage()
	{
		return BountyLanguage.German; // debug
		/*
		BountyLanguage sl = BountyLanguage.English;
		string cultureString = System.Globalization.CultureInfo.CurrentCulture.Name;
		Debug.Log("Language code: "+cultureString);
		string[] cultureString2 = cultureString.Split(new string[] { "-" }, System.StringSplitOptions.RemoveEmptyEntries);
		if (cultureString2.Length == 0)
		{
			// fallback for linux invariant mode?
			if(SettingsData.languageUpdateTable.ContainsKey(Application.systemLanguage))
			{
				sl = SettingsData.languageUpdateTable[Application.systemLanguage];
			}
		}
		else
		{
			// choose by normal lang code
			if (cultureString2.Length > 1 && SettingsData.languageTableFine.ContainsKey(cultureString))
			{
				sl = SettingsData.languageTableFine[cultureString];
			}
			else if (SettingsData.languageTableRough.ContainsKey(cultureString2[0]))
			{
				sl = SettingsData.languageTableRough[cultureString2[0]];
			}
		}
		return sl;
		*/
	}
	/// <summary>
	/// usable before the manager or any data has been loaded. used to lookup pre saved language or assert the system language for the first start
	/// </summary>
	public static void PreCheckLangauge()
	{
		SettingsData sd = null;
		BountyLanguage lang = GetDefaultLanguage();

		if (LoadSettings(ref sd))
			lang = sd.gameLanguage;

		if (lang == BountyLanguage.None /*&& SettingsData.languageUpdateTable.ContainsKey(sd.language)*/) // new language field fix
		{
			//lang = SettingsData.languageUpdateTable[sd.language];
			lang = GetDefaultLanguage();
		}
		Localization.language = lang.ToString();
	}
	/// <summary>
	/// usable before managers load up. gets the best matching screen resolution that we support
	/// </summary>
	/// <returns></returns>
	public static Vector2Int GetStartResolution()
	{
		Vector2Int result = new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
		float ratio = result.x / result.y;
		if (ratio >= 1.7f && result.y > 1080)
			result = new Vector2Int(1920,1080);
		else if (ratio < 1.7f && result.y > 1200)
			result = new Vector2Int(1920, 1200);
		return result;
	}

	/// <summary>
	/// loads settings from file or creates new default settings if neccesary
	/// </summary>
	public static SettingsData GetSettings()
	{
		SettingsData resultSettings = new SettingsData();

		if (!LoadSettings(ref resultSettings))
		{
			// set default settings
			resultSettings = SettingsData.GetDefaultSettings();

		}
		return resultSettings;
	}

	/// <summary>
	/// prepares settings in game
	/// </summary>
	public void InitSettings()
	{
		if (!LoadMetaInfo())
		{
			metaInfos = new MetaInfo()
			{
				runs = 0,
				tutorialPlayed = false,
				seenStorySegments = new List<int>(),
				newsRead = 0,
			};
			SaveMetaInfo();
		}
		if (!LoadSettings(ref currentSettings))
		{
			// set default settings
			currentSettings = SettingsData.GetDefaultSettings();
			currentSettings.settingsVersion = settingsFileVersion;
			//Debug.Log("Settings set to default");
			SaveSettings();
		}


		// apply current settings
		QualitySettings.vSyncCount = currentSettings.vSync ? 1 : 0;

		FullScreenMode sfm = FullScreenMode.Windowed;
		if (currentSettings.fullScreen)
			sfm = FullScreenMode.ExclusiveFullScreen;
		if (currentSettings.borderedWindow)
			sfm = FullScreenMode.FullScreenWindow;

		Screen.SetResolution(currentSettings.resolution.x, currentSettings.resolution.y, sfm, Screen.currentResolution.refreshRate);
		Cursor.lockState = sfm == FullScreenMode.ExclusiveFullScreen ? CursorLockMode.Confined : CursorLockMode.None;
		//MainMenuGui.SetScreenMode(currentSettings.fullScreen, currentSettings.borderedWindow);

		QualitySettings.SetQualityLevel(currentSettings.quality);
		//QualitySettings.softParticles = currentSettings.enableSoftParticles;
		//QualitySettings.shadows = (ShadowQuality)currentSettings.shadowQuality;
		//QualitySettings.shadowResolution = (ShadowResolution)currentSettings.shadowResolution;
		// new language field fix
		if (currentSettings.gameLanguage == BountyLanguage.None/* && SettingsData.languageUpdateTable.ContainsKey(currentSettings.language)*/) 
		{
			//currentSettings.gameLanguage = SettingsData.languageUpdateTable[currentSettings.language];
			currentSettings.gameLanguage = GetDefaultLanguage();
		}
		Localization.language = currentSettings.gameLanguage.ToString();
		// typeWriter setting fix 23.12.20
		if (currentSettings.typeWriterSpeed == 0)
			currentSettings.typeWriterSpeed = 1;

		currentSettings.settingsVersion = settingsFileVersion;
	}
	/// <summary>
	/// applies new quality setting dependend sub-settings
	/// </summary>
	public void UpdateGraphicQuality()
	{
		int index = Settings.quality;
		Settings.enableSoftParticles = index < 2;
		Settings.enableSSAO = index < 2;
		Settings.shadowQuality = 2 - index;
		Settings.shadowResolution = 2 - index;
	}

    /// <summary>
    /// called when we enter the game phase eg from the main menu
    /// </summary>
    public void InitGame()
    {
		//Debug.Log("init game called");
		
		startup = true;

        variables = eventManager.GetVariableDatabase().Duplicate();
        dialogueManager = Instantiate<SDDialogueManager>(managerPrefabs.dialogueManager);
        dialogueManager.Setup();
        dialogueManager.eventManager = eventManager;
        dialogueManager.varResolver = (s) => variables.GetVariable(s);
        MainGuiController.instance.dialogueGui.Setup();

		eventManager.Setup();
		modelManager.Setup();

		camp = Instantiate<BountyCamp>(managerPrefabs.camp);
		camp.PreSetupData();
        NonPlayerBase = Instantiate<NewBases>(NonPlayerBase);
        mapData = new List<MapData>();
		currentLoot = new List<BaseItem>();
		pendingDefenceData = new List<CampDefenceData>();
		extraSpawn = new List<BountyCharacter>();
		pendingDeadEvents = new List<int>();

		// physics fix due to grabit interference
		Physics.simulationMode = SimulationMode.FixedUpdate;
		Physics.gravity = Vector3.down * 9.81f;

		// spawn camp base
		scenarioManager.SpawnScenario(campSceneResource);

		

    }
	/// <summary>
	/// callback function to proceed the init procedure. after in between actions were performed
	/// </summary>
	private void OnInitResumed()
	{
		//campScene.defenseScenario.Setup();
		campScene.baseCam.SetCamMode(0, true);

		if (campScene != null)
			campScene.gameObject.SetActive(false);

		sessionsLootEventLog = new Dictionary<int, int>();
		sessionsTravelEventLog = new Dictionary<int, int>();
		lootEventTracker = new Dictionary<int, List<int>>();

		ClearLootEventOverride();


		if (!debugStartOutpost && loadGame && SDSaveLoad.FileExists("Save1"))
		{
			BountySaveHeader tHead = null;
			SDSaveLoad.LoadHeader("Save1", ref tHead);
			if(tHead.saveFileVersion <= depricatedFileVersion)
			{
				Debug.Log("Old Save was deprecated, started a new game instead");
				NewGame();
			}	
			else
				StartCoroutine(Load());

		}
		else
			NewGame();
	}
	/// <summary>
	/// creates the persistent manager and sets it up
	/// </summary>
	public void PreparePersistentData()
	{
		persistentManager = Instantiate<PersistentManager>(managerPrefabs.persistentManager);
		persistentManager.Setup();
	}

    /// <summary>
    /// sets up all the managers. should only be called once when a new save game gets created and not when it gets loaded
    /// </summary>
    public void NewGame()
    {
		loading = false;

		factionManager = Instantiate<FactionManager>(managerPrefabs.factionManager);
        travelEventDatabase = Instantiate<TravelEventDatabase>(managerPrefabs.travelEventDatabase);
        storyDatabase = Instantiate<StoryDatabase>(managerPrefabs.storyDatabase);
        scenarioManager = Instantiate<BountyScenarioManager>(managerPrefabs.scenarioManager);
        //camp = Instantiate<BountyCamp>(managerPrefabs.camp); // moved up so that global item Data can be fetched right away
        combatManager = Instantiate<BountyCombatManager>(managerPrefabs.combatManager);
        combatAi = Instantiate<BountyCombatAI>(managerPrefabs.combatAi);
        questManager = Instantiate<BountyQuestManager>(managerPrefabs.questManager);
        lootLocationDatabase = Instantiate<LootLocationDatabase>(managerPrefabs.lootLocationDatabase);
        lootEventDatabase = Instantiate<LootEventDatabase>(managerPrefabs.lootEventDatabase);
        characterDatabase = Instantiate<CharacterDatabase>(managerPrefabs.characterDatabase);
        mapEventDatabase = Instantiate<MapEventDatabase>(managerPrefabs.mapEventDatabase);
		achievementDatabase = Instantiate<BountyAchievementDatabase>(managerPrefabs.achievementDatabase);
        NonPlayerBase = Instantiate<NewBases>(NonPlayerBase);

        currentMap = 0;
		Map.gameObject.SetActive(true);
		Map.Setup(false, mapTemplate);


		day = 1;
        time = 480;
        fightActive = false;
        fightEndPending = false;
        eventActive = false;
        inCamp = false;
        
        startFightOverride = -1;
		currentTutIndex = -1;

		// setup databases
		factionManager.Setup();
		combatManager.Setup();
		camp.Setup();
        questManager.Setup();
        mapEventDatabase.Setup();
		storyDatabase.Setup();

		lootEventDatabase.CacheEntries();
		characterDatabase.CacheEntries();
		persistentManager.ResetCurrentStats();
		persistentManager.Runs++;
		MetaInfos.runs++;
		metaInfoChanged = true;

		// insert player
		if (playerCharacter == null)
        {
            playerCharacter = Instantiate<BountyCharacter>(playerPrefab);
        }
        playerCharacter.Setup(0, new CharacterCreationInfo("spawned for new game by BountyManager"));

		// additional faction setup
		if (mapList[0].GetComponent<ProceduralMapGenerator>())
			factionManager.SetupExtended(Map.ProceduralMap.templates[mapTemplate]);


		if (debugTutorialStart >= 0)
			skipIntro = false;
        variables.SetVariable("SkipIntro", skipIntro);

		// tutorial start
		if (!skipIntro || debugTutorialStart >= 0)
        {
            session = 0;
            night = false;
			//questManager.CreateAreaQuests(); // da2 threat quests
			MainGuiController.instance.mainTabBar.SetTabs(1);

			//camp.TutGameSetup();
			camp.ChangeResource(1, 6);

            if(debugTutorialStart >= 0)
            {
                playerCharacter.ChangeTalentLevel(BountyTalentType.Talent_Melee, 5, true, false);
                playerCharacter.ChangeTalentLevel(BountyTalentType.Talent_Pistol, 5, true, false);
                playerCharacter.ChangeTalentLevel(BountyTalentType.Talent_Hunting, 3, true, false);
                playerCharacter.ChangeTalentLevel(BountyTalentType.Talent_Crafting, 3, true, false);


            }


			GivePlayerEquip(true);
			//ApplyUpgrades(true); // dont apply the upgrades for the tut
			List<BaseItem> items = playerCharacter.GetCompleteEqupment();
			for (int i = items.Count - 1; i >= 0; i--) // put all items back to party
			{
				camp.AddPartyItem(items[i]);
				playerCharacter.RemoveEquippedItem(items[i]);
			}

			currentTutIndex = debugTutorialStart >= 0 ? debugTutorialStart : 0;
            if(debugTutorialStart >= 0)
            {
				if(debugTutorialStart > 0 && debugTutorialStart < 5)
				{
					MainGuiController.instance.timeDisplay.gameObject.SetActive(true);
					MainGuiController.instance.resourceDisplay.gameObject.SetActive(true);
					MainGuiController.instance.timeDisplay.UpdateTime(day, time, night);
					MainGuiController.instance.resourceDisplay.UpdateResources(camp.GetResourceArray());
					MainGuiController.instance.partyOverview.Show();
					MainGuiController.instance.pinnedQuestInfo.Show();
					MainGuiController.instance.mainTabBar.gameObject.SetActive(true);
				}


                if(tutorialDebugBlocks.Length > currentTutIndex && tutorialDebugBlocks[currentTutIndex] != -1)
                {
                    int temp = eventManager.InstantiateEventBlock(tutorialDebugBlocks[currentTutIndex]);
                    eventManager.StartEventBlock(temp);
                    eventManager.DestroyEventBlockInstance(temp);
                }
            }
            
            startup = false;
            currentTutEventContext = eventManager.InstantiateEventBlock(tutorialEventBlocks[currentTutIndex]);

            if(debugTutorialStart >= 0)
            {
                eventManager.StartEventBlock(currentTutEventContext);
            }
            else
            {
                MainGuiController.instance.transitionPanel.FadeToBlack("", ()=>{ MainGuiController.instance.transitionPanel.HideLoading(); eventManager.StartEventBlock(currentTutEventContext); });
            }

        }
        else
        {
			// normal start
			day = 0;
			if (debugStartDay > 1)
				day = debugStartDay;
            session = 1;
            night = false;
			//if (campScene != null)
			//	campScene.gameObject.SetActive(true);
			night = true;

			camp.InsertPlayer(playerCharacter);
			persistentManager.TutPlayed = true;
			playerCharacter.TalentPoints = 3; // talent points for jason at level 1
			playerCharacter.AttributePoints = 3;


			camp.NewGameSetup();
            questManager.CreateSideQuests();
			questManager.CreateAllDASAreaQuests();


			if (debugStart)
			{
				playerCharacter.ChangeTalentLevel(BountyTalentType.Talent_Melee, 5, true, false);
				playerCharacter.ChangeTalentLevel(BountyTalentType.Talent_Pistol, 5, true, false);
				playerCharacter.ChangeTalentLevel(BountyTalentType.Talent_Hunting, 3, true, false);
				playerCharacter.ChangeTalentLevel(BountyTalentType.Talent_Survival, 3, true, false);

				playerCharacter.ChangeTalentLevel(BountyTalentType.Talent_Bargain, 1, true, false);
				playerCharacter.ChangeTalentLevel(BountyTalentType.Talent_Crafting, 1, true, false);
				playerCharacter.ChangeTalentLevel(BountyTalentType.Talent_Deceive, 1, true, false);
				playerCharacter.ChangeTalentLevel(BountyTalentType.Talent_Farming, 1, true, false);
				playerCharacter.ChangeTalentLevel(BountyTalentType.Talent_Fists, 1, true, false);
				playerCharacter.ChangeTalentLevel(BountyTalentType.Talent_Grenadier, 1, true, false);
				playerCharacter.ChangeTalentLevel(BountyTalentType.Talent_Guarding, 1, true, false);
				playerCharacter.ChangeTalentLevel(BountyTalentType.Talent_MachinePistol, 1, true, false);
				playerCharacter.ChangeTalentLevel(BountyTalentType.Talent_Medical, 1, true, false);
				playerCharacter.ChangeTalentLevel(BountyTalentType.Talent_Rifle, 1, true, false);
				playerCharacter.ChangeTalentLevel(BountyTalentType.Talent_Scouting, 1, true, false);
				playerCharacter.ChangeTalentLevel(BountyTalentType.Talent_Shotgun, 1, true, false);
				playerCharacter.ChangeTalentLevel(BountyTalentType.Talent_Swords, 1, true, false);
				playerCharacter.ChangeTalentLevel(BountyTalentType.Talent_Threaten, 1, true, false);
			}

			// give items to player
			GivePlayerEquip(false);
			ApplyUpgrades(false);

			camp.DebugSetup();


			// setup camp movement targets
			if (!debugStartOutpost)
            {
                BountyCharacter bc;
                bc = camp.GetPartyMember(n => n.mainCharacter);
                bc.startNodeIndex = 1;
                bc.startNodeStation = BaseNavNode.StationType.Bed;
                bc.startNodeType = BaseNavNode.NodeType.Station;

                bc = camp.GetInhabitant(n => n.characterId == "Trish");
                bc.startNodeIndex = 0;
                bc.startNodeStation = BaseNavNode.StationType.Bar;
                bc.startNodeType = BaseNavNode.NodeType.Station;
				bc.goToWork = true;
				bc.startNavMode = 1;

				bc = camp.GetInhabitant(n => n.characterId == "Jack");
                bc.startNodeStation = BaseNavNode.StationType.Standing;
                bc.startNodeType = BaseNavNode.NodeType.Idle;
                bc.startNavMode = 2;

				MainGuiController.instance.resourceDisplay.UpdateResources(camp.GetResourceArray(), true);
				EnterCamp();
            }
            else
            {
				startup = false;
				night = debugStartNight;
                session = night ? 1 : 0;
				if(campScene != null)
	               campScene.gameObject.SetActive(false);
				Map.TeleportTo(BountyExtensions.ParseVector2I(debugOutpostCoord));
                BountyManager.instance.StartFight(BountyManager.instance.factionManager.GetBaseDataTemplate(BountyManager.instance.Map.CurrentPoint.activeBaseRefId).scenario, 10);
            }

            
        }
    }

	/// <summary>
	/// gives the player their start equip
	/// </summary>
	/// <param name="tutorial"></param>
	private void GivePlayerEquip(bool tutorial)
	{
		BaseItem i;
		if (playerCharacter.GetTalentLevel(BountyTalentType.Talent_Melee) > 0)
		{
			//i = ScriptableObject.CreateInstance<BaseItem>();
			//i.Setup(camp.playerStartEquipPool.Find(n => n.itemType == BaseItem.ItemType2.GearWeaponPunch));
			i = camp.playerStartEquipPool.Find(n => n.itemType == BaseItem.ItemType2.GearWeaponPunch).GenerateItem(0, !tutorial);
			//if (!tutorial)
			playerCharacter.AddEquipmentItem(i);
			//else
				//camp.AddPartyItem(i);
		}
		else if (playerCharacter.GetTalentLevel(BountyTalentType.Talent_Swords) > 0)
		{
			//i = ScriptableObject.CreateInstance<BaseItem>();
			//i.Setup(camp.playerStartEquipPool.Find(n => n.itemType == BaseItem.ItemType2.GearWeaponBlade));
			i = camp.playerStartEquipPool.Find(n => n.itemType == BaseItem.ItemType2.GearWeaponBlade).GenerateItem(0, !tutorial);
			//if (!tutorial)
			playerCharacter.AddEquipmentItem(i);
			//else
				//camp.AddPartyItem(i);
		}

		if (playerCharacter.GetTalentLevel(BountyTalentType.Talent_Pistol) > 0)
		{
			//i = ScriptableObject.CreateInstance<BaseItem>();
			//i.Setup(camp.playerStartEquipPool.Find(n => n.itemType == BaseItem.ItemType2.GearWeaponPistol));
			i = camp.playerStartEquipPool.Find(n => n.itemType == BaseItem.ItemType2.GearWeaponPistol).GenerateItem(0, !tutorial);
			//if (!tutorial)
			playerCharacter.AddEquipmentItem(i);
			//else
			//	camp.AddPartyItem(i);
			BaseItemDefinition bid = camp.playerStartEquipPool.Find(n => n.itemType == BaseItem.ItemType2.AmmoPistol);
			i = ScriptableObject.CreateInstance<BaseItem>();
			i.Setup(BaseItem.ItemType2.AmmoPistol, 1, tutorial ? 3 : bid.currentStack);
			camp.AddPartyItem(i);
		}
		else if (playerCharacter.GetTalentLevel(BountyTalentType.Talent_Shotgun) > 0)
		{
			//i = ScriptableObject.CreateInstance<BaseItem>();
			//i.Setup(camp.playerStartEquipPool.Find(n => n.itemType == BaseItem.ItemType2.GearWeaponShotgun));
			i = camp.playerStartEquipPool.Find(n => n.itemType == BaseItem.ItemType2.GearWeaponShotgun).GenerateItem(0, !tutorial);
			//if (!tutorial)
			playerCharacter.AddEquipmentItem(i);
			//else
				//camp.AddPartyItem(i);
			BaseItemDefinition bid = camp.playerStartEquipPool.Find(n => n.itemType == BaseItem.ItemType2.AmmoShotgun);
			i = ScriptableObject.CreateInstance<BaseItem>();
			i.Setup(BaseItem.ItemType2.AmmoShotgun, 1, tutorial ? 3 : bid.currentStack);
			camp.AddPartyItem(i);
		}
		else if (playerCharacter.GetTalentLevel(BountyTalentType.Talent_Rifle) > 0)
		{
			//i = ScriptableObject.CreateInstance<BaseItem>();
			//i.Setup(camp.playerStartEquipPool.Find(n => n.itemType == BaseItem.ItemType2.GearWeaponRifle));
			i = camp.playerStartEquipPool.Find(n => n.itemType == BaseItem.ItemType2.GearWeaponRifle).GenerateItem(0, !tutorial);
			//if (!tutorial)
			playerCharacter.AddEquipmentItem(i);
			//else
				//camp.AddPartyItem(i);
			BaseItemDefinition bid = camp.playerStartEquipPool.Find(n => n.itemType == BaseItem.ItemType2.AmmoRifle);
			i = ScriptableObject.CreateInstance<BaseItem>();
			i.Setup(BaseItem.ItemType2.AmmoRifle, 1, tutorial ? 9 : bid.currentStack);
			camp.AddPartyItem(i);
		}

		if(playerCharacter.GetTalentLevel(BountyTalentType.Talent_Hunting) > 0)
		{
			i = ScriptableObject.CreateInstance<BaseItem>();
			i.Setup(camp.playerStartEquipPool.Find(n => n.itemType == BaseItem.ItemType2.AmmoHunter));
			camp.AddPartyItem(i);
		}
		else if (playerCharacter.GetTalentLevel(BountyTalentType.Talent_Grenadier) > 0)
		{
			i = ScriptableObject.CreateInstance<BaseItem>();
			i.Setup(camp.playerStartEquipPool.Find(n => n.itemType == BaseItem.ItemType2.AmmoGrenadier));
			camp.AddPartyItem(i);
		}

		if(!tutorial)
		{
			// handbandagen
			i = ScriptableObject.CreateInstance<BaseItem>();
			i.Setup(BaseItem.ItemType2.GearHands, 3, 1, new List<AttributeModifier>() { new AttributeModifier(BountyCharAttribute.Armor, 3), new AttributeModifier(BountyCharAttribute.Resistance, 3) });
			playerCharacter.AddEquipmentItem(i);
			// ring
			i = ScriptableObject.CreateInstance<BaseItem>();
			i.Setup(BaseItem.ItemType2.MiscValuables, 2, 1);
			camp.AddPartyItem(i);
			// medi craft bandagen
			i = ScriptableObject.CreateInstance<BaseItem>();
			i.Setup(BaseItem.ItemType2.CraftingMedicalParts, 1, 2);
			camp.AddPartyItem(i);
			// rifle panzerbrechend
			i = ScriptableObject.CreateInstance<BaseItem>();
			i.Setup(BaseItem.ItemType2.AmmoRifle, 3, 5);
			camp.AddPartyItem(i);
			// samen
			i = ScriptableObject.CreateInstance<BaseItem>();
			i.Setup(BaseItem.ItemType2.MiscSeeds, 1, 4);
			camp.AddPartyItem(i);
			// molotovs
			i = ScriptableObject.CreateInstance<BaseItem>();
			i.Setup(BaseItem.ItemType2.AmmoGrenadier, 1, 10);
			camp.AddPartyItem(i);
			// dietriche
			i = ScriptableObject.CreateInstance<BaseItem>();
			i.Setup(BaseItem.ItemType2.MiscKey, 1, 10);
			camp.AddPartyItem(i);
			// revival kit
			i = ScriptableObject.CreateInstance<BaseItem>();
			i.Setup(BaseItem.ItemType2.ConsumableRevival, 1, 1);
			camp.AddPartyItem(i);
		}
	}
    //summary//
    //for creating new base
    //////////

    [HideInInspector]
    public List<BountyBase> LastactiveBases;
	[HideInInspector]
    public List<int> AllBasesIDs;
    [HideInInspector]
    public bool InStevenCampBool = false;

    ///Summary
    /// The function processbase handles the instantiation of new base.
    //It is called on button as of now to spawn a new base. It is called in Load() function in BountyManager as well to automatically spawn the last active base.
    //BountyManager prefab in herriarchy holds the reference of NewBases scriptable object.
    //This function spawn the base, characters and populate the campScene variable of BountyManager according to current activeBase.
    //It also handles the situation if the current active Base is called on again to be instantiated.
    ///
	IEnumerator SpawnBaseWhileStartingGame()
    {

        Debug.Log("SpawnBaseWhileStartingGame");
        for (int i = 0; i < NonPlayerBase.AllBasesList.Count; i++)
        {
            Debug.Log(NonPlayerBase.AllBasesList[i].ScenarioID + "NonPlayerBase.LastActiveBaseScenarioID" + NonPlayerBase.LastActiveBaseScenarioID);
            if (NonPlayerBase.AllBasesList[i].ScenarioID == NonPlayerBase.LastActiveBaseScenarioID)
            {
                //StartCoroutine(LeaveNewBases());
                SpawnBase(i);
            }

            //            for (int j = 0; j < NonPlayerBase.ActiveBaseScenarioID.Count; j++)
            //{

            //	/if (NonPlayerBase.AllBasesList[i].ScenarioID == NonPlayerBase.ActiveBaseScenarioID[j])
            //	{
            //		//StartCoroutine(LeaveNewBases());
            //		SpawnBase(i);
            //		yield return new WaitForSeconds(0.1f);
            //	}
            //}
        }

        yield return null;
    }
    public void EnterHomeBase()
    {
        inCamp = true;
    }
    public void SpawnBase(int buttonIndex)
    {

        StartCoroutine(ProcessBase(buttonIndex));
    }
    private IEnumerator ProcessBase(int buttonIndex)
    {

        BaseInfo selectedBaseInfo = NonPlayerBase.AllBasesList[buttonIndex];
		SaveBaseIDs();
        if (LastactiveBases != null)
        {
            if (InCamp)
            {
                LeaveCamp();
                LeaveBase();
            }
            bool matchFound = false;

            for (int i = 0; i < LastactiveBases.Count; i++)
            {
                if (selectedBaseInfo.ScenarioID == LastactiveBases[i].ScenarioIndex)
                {
                    // If the ScenarioIndex matches the selectedBaseInfo.ScenarioID, set matchFound to true
                    matchFound = true;
                    break;
                }
            }

            // Second pass: Activate the matching element and deactivate the rest
            if (matchFound)
            {
                for (int i = 0; i < LastactiveBases.Count; i++)
                {
                    if (selectedBaseInfo.ScenarioID == LastactiveBases[i].ScenarioIndex)
                    {
                        // If the ScenarioIndex matches the selectedBaseInfo.ScenarioID, activate the GameObject
                        NonPlayerBase.LastActiveBaseScenarioID = LastactiveBases[i].ScenarioIndex;

                        LastactiveBases[i].gameObject.SetActive(true);
                        campScene = LastactiveBases[i];
						Debug.LogError(campScene.name);
                       // TempInhabitants.Clear();
                        List<BountyModel> BM;
                        BM = LastactiveBases[i].baseNav.gameObject.GetComponent<BaseNavController>().GetAllModels();
                       LastactiveBases[i].baseNav.gameObject.GetComponent<BaseNavController>().models.RemoveAll(model => model == null || model.gameObject == null);
                        yield return StartCoroutine(WaitForBaseClone(selectedBaseInfo.BaseName));
                        Save(1);
                        //                 foreach (var item in BM)
                        //                 {

                        //if (item != null)
                        //{

                        //                     TempInhabitants.Add(item.gameObject);
                        //}

                        //                 }
                    }
                    else
                    {
                        // If the ScenarioIndex doesn't match, deactivate the GameObject
                        LastactiveBases[i].gameObject.SetActive(false);
                    }
                }
                yield break;
            }
        }
        currentBaseIndex = buttonIndex;

        MainGuiController.instance.transitionPanel.FadeToBlack(selectedBaseInfo.BaseName, null, null, 2f);
        yield return new WaitForSeconds(2f);

        BountyManager.instance.scenarioManager.SpawnScenario(selectedBaseInfo.ScenarioID);

        NonPlayerBase.LastActiveBaseScenarioID = selectedBaseInfo.ScenarioID;


        yield return StartCoroutine(WaitForBaseClone(selectedBaseInfo.BaseName));


        yield return new WaitForSeconds(0.5f);
        UpdateBaseInhabitants(selectedBaseInfo, CBase);
        MainGuiController.instance.transitionPanel.FadeToTransparent(null, 2f);
        if (NonPlayerBase.activeJobs != null)
        {
            foreach (var item in NonPlayerBase.activeJobs)
            {
                BountyManager.instance.CBase.GetComponent<BountyBase>().UpdateRoomUpgradeModel(item.type, true, 1.14f);
            }
        }

        Debug.Log("Base " + (buttonIndex + 1) + " Task performed...");

        if (!InCamp)
        {
            InStevenCampBool = true;
        }



        Save(1);
    }
	public void ChangeNightLighSettingsInNewBases()
	{
		if (night)
		{

        campScene.GetComponent<Scenario>().ApplySettings(false, CurrentTime, BountyWeather.None);
		}
		if (night == false)
		{

        campScene.GetComponent<Scenario>().ApplySettings(true, CurrentTime, BountyWeather.None);
		}

    }
    public void SaveBaseIDs()
	{
        AllBasesIDs.Clear();
        for (int i = 0; i < NonPlayerBase.AllBasesList.Count; i++)
        {
            AllBasesIDs.Add(NonPlayerBase.AllBasesList[i].ScenarioID);

        }
    }
    private IEnumerator<GameObject> WaitForBaseClone(string baseName)
    {
        CBase = GameObject.Find(baseName + "(Clone)");

        while (CBase == null)
        {
            CBase = GameObject.Find(baseName + "(Clone)");
            if (CBase == null)
            {
                yield return null;
            }
        }
        Debug.Log(CBase + "CBase");

        yield return CBase;
    }

    private void UpdateBaseInhabitants(BaseInfo selectedBaseInfo, GameObject cBase)
    {
        List<BountyModel> bountyModels = new List<BountyModel>();
        List<BountyCharacter> bountyChars = new List<BountyCharacter>();
        bountyChars = camp.globalCharacters;
        foreach (var inhabitant in selectedBaseInfo.BaseInhabitants)
        {

            foreach (var nonPlayerBaseCharacter in NonPlayerBaseCharacters)
            {
                if (inhabitant.characterId == nonPlayerBaseCharacter.characterId)
                {
                    //BountyCharacter ch	= characterDatabase.LoadCharacterResource(inhabitant.characterId);
                    //Debug.Log(inhabitant.characterId + "BaseInhabitantsName");
                    inhabitant.modelPrefab = nonPlayerBaseCharacter.modelPrefab;

                    cBase.GetComponent<BountyBase>().AddInhabitant(inhabitant, false);
                   // cBase.GetComponent<BountyBase>().UpdateCharacter(inhabitant, false);
                    // cBase.GetComponent<BountyBase>().UpdateCharacter(inhabitant, true);
                }

            }

        }
        //TempInhabitants.Clear();

        //bountyModels = cBase.GetComponent<BountyBase>().baseNav.GetAllModels();

        ////foreach (var model in bountyModels)
        ////{
        ////    TempInhabitants.Add(model.transform.gameObject);
        ////}

        ////foreach (var item in TempInhabitants)
        ////{
        ////}
        //for (int i = 0; i < bountyModels.Count; i++)
        //{
        //    bountyModels[i].myCharacter.Setup(0, new CharacterCreationInfo("spawned by camp script debug setup"));
        //    selectedBaseInfo.BaseInhabitants[i] = bountyModels[i].myCharacter;

        //}
    }


    [HideInInspector]
    public int currentBaseIndex;
    //[HideInInspector]
    //public List<GameObject> TempInhabitants = new List<GameObject>();


    [HideInInspector]
    public GameObject CBase;
    /// <summary>
    /// applies the unlocked character upgrades when a new game starts
    /// </summary>
    private void ApplyUpgrades(bool tutorial)
	{
		List<BountyUpgradeInstance> list = persistentManager.Upgrades;
		int c = list.Count;
		BountyUpgradeEntry bue = null;
		BaseItem bi = null;
		ResultItemDefinition rid = null;
		for (int i = 0; i < c; i++)
		{
			bue = persistentManager.GetUpgradeRow(list[i].category);
			for (int j = 0; j < list[i].level; j++)
			{
				// apply attribute bonus
				for (int k = 0; k < bue.tiers[j].attributeBonus.Length; k++)
				{
					playerCharacter.ChangeAttributeRaw(bue.tiers[j].attributeBonus[k].attribute, bue.tiers[j].attributeBonus[k].value);
				}
				// apply item bonus
				for (int k = 0; k < bue.tiers[j].itemBonus.Length; k++)
				{
					if(bue.tiers[j].itemBonus[k].itemType == BaseItem.ItemType2.GearChest)
					{
						if ((bue.tiers[j].itemBonus[k].variant + 2) != startFaction)// apply only when faction was choosen at the start
							continue;
					}

					rid = new ResultItemDefinition();
					rid.itemType = bue.tiers[j].itemBonus[k].itemType;
					if(BaseItem.IsType(bue.tiers[j].itemBonus[k].itemType, BaseItem.ItemType2.AmmoRandomWeapon)) // ammo for reanged weapon needs to match the choosen weapon
					{
						rid.itemType = CombatGui.GetAmmoTypeFromWeapon(playerCharacter.GetEquippedItem(BaseItem.ItemType2.GearWeaponRanged).itemType);
					}

					rid.stack = bue.tiers[j].itemBonus[k].currentStack;
					rid.variant = bue.tiers[j].itemBonus[k].variant;
					rid.tier = bue.tiers[j].itemBonus[k].Tier;
					bi = rid.GenerateItem(bue.tiers[j].itemBonus[k].level, bue.tiers[j].itemBonus[k].level);

					if (BaseItem.IsType(bue.tiers[j].itemBonus[k].itemType , BaseItem.ItemType2.Gear))
					{
						playerCharacter.AddEquipmentItem(bi);
					}
					else
					{
						camp.AddPartyItem(bi);
					}
				}
				// apply item level bonus
				for (int k = 0; k < bue.tiers[j].itemLevelBonus.Length; k++)
				{
					bi = playerCharacter.GetEquippedItem(bue.tiers[j].itemLevelBonus[k].itemType);

					if(bi != null)
					{
						bi.UpgradeLevel(bue.tiers[j].itemLevelBonus[k].level, 2);
					}
					else
					{
						List<BaseItem> li = camp.GetPartyItems(n => n.IsType(bue.tiers[j].itemLevelBonus[k].itemType));
						if(li.Count > 0)
							li[0].UpgradeLevel(bue.tiers[j].itemLevelBonus[k].level, 2);
					}
				}
				if (!tutorial)
				{
					// apply resource bonus
					for (int k = 0; k < bue.tiers[j].resourceChange.Length; k++)
					{
						camp.ChangeResource(bue.tiers[j].resourceChange[k].type, bue.tiers[j].resourceChange[k].amount);
					}
					// apply reputation bonus
					for (int k = 0; k < bue.tiers[j].reputationChange.Length; k++)
					{
						if (bue.tiers[j].reputationChange.Length == 1 && bue.tiers[j].reputationChange[k].faction != (Faction)startFaction) // apply only when faction was choosen at the start
							continue;
						factionManager.ChangeRelationship(bue.tiers[j].reputationChange[k].faction, 0, bue.tiers[j].reputationChange[k].amount);
					}
				}
			}
		}
	}

	/// <summary>
	/// ends the current tutorial segment or the whole tutorial mode when no segment is left
	/// </summary>
    public void EndTutorial()
    {
        //if(currentTutEventContext != -1)
        //{
        //    eventManager.DestroyEventBlockInstance(currentTutEventContext);
        //}
        currentTutIndex += 1;
        if(tutorialEventBlocks.Length > currentTutIndex)
        {
            currentTutEventContext = eventManager.InstantiateEventBlock(tutorialEventBlocks[currentTutIndex]);
            eventManager.StartEventBlock(currentTutEventContext);
        }
        else
        {
            // no tutorials left
            currentTutIndex = -1;
			BountyCharacter oldPlayer = playerCharacter;
			playerCharacter = Instantiate<BountyCharacter>(playerPrefab);
			playerCharacter.Setup(0, new CharacterCreationInfo("spawned after tutorial by BountyManager"));

			playerCharacter.CharName = oldPlayer.CharName;
			playerCharacter.portraitData = oldPlayer.portraitData;
			playerCharacter.talents = oldPlayer.talents;
			
			camp.ClearItems();
			camp.TutEndSetup();
			GivePlayerEquip(false);
			ApplyUpgrades(false);

			// reset AI overides // added 20.3.21
			combatAi.skillOveride = 0;
			combatAi.moveOverride = 0;
			combatAi.talentOverride = 0;
			combatAi.skillIndexLimit = -1;
			combatAi.skillOveride = -1;
			combatAi.skillRawOveride = -1;
			combatManager.CritOverride = 0;
			combatManager.BlockOverride = 0;
			combatManager.ClearHitOverrides();


			if (!variables.GetVariable("Story_Smuggler_Paid").AsBool())
			{
				BaseItemDefinition bid = new BaseItemDefinition();
				bid.Setup(BaseItem.ItemType2.GearWeaponPistol, 2, 1, new List<AttributeModifier>() { new AttributeModifier(BountyCharAttribute.RangedDamage, 30), new AttributeModifier(BountyCharAttribute.Strength, 2) });
				camp.AddPartyItem(bid.GenerateItem(0, true), false, false);
			}

			playerCharacter.HealthPercent = 100;
			camp.InsertPlayer(playerCharacter);

			Map.AllowedList.Clear();

			questManager.CreateSideQuests();

			persistentManager.TutPlayed = true;
			MetaInfos.tutorialPlayed = true;
			metaInfoChanged = true;

			achievementDatabase.UnlockAchievement("FirstExperience");
		}
        
    }

	/// <summary>
	/// used by debug test functions
	/// </summary>
	public void SetupCombatTest()
	{
		combatManager = Instantiate<BountyCombatManager>(managerPrefabs.combatManager);
		combatManager.Mode = 4;
		fightActive = true;
		combatManager.StartTestCombat();

	}

    //[ContextMenu("testdebug")]
    private void Debug232()
    {
        Debug.Log(BaseItem.IsType(BaseItem.ItemType2.GearChest, BaseItem.ItemType2.GearChest));
        Debug.Log(BaseItem.IsType(BaseItem.ItemType2.GearChest, BaseItem.ItemType2.GearRandomClothing));
        Debug.Log(BaseItem.IsType(BaseItem.ItemType2.GearChest, BaseItem.ItemType2.Gear));
        Debug.Log(BaseItem.IsType(BaseItem.ItemType2.GearChest, BaseItem.ItemType2.GearRandomWeapon));
        Debug.Log(BaseItem.IsType(BaseItem.ItemType2.GearChest, BaseItem.ItemType2.Ammo));
    }

	/// <summary>
	/// returns a simple upwarts counting unique integer id. used to make items sortable by age
	/// </summary>
	/// <returns></returns>
	public int GetUniqueTimeStamp()
	{
		int result = itemCounter;
		itemCounter++;
		return result;
	}

	/// <summary>
	/// changes current session time. use negative values for time progression since we count the REMAINING time not the passed time
	/// </summary>
	/// <param name="value"></param>
	/// <param name="set"></param>
	public void ChangeTime(int value, bool set = false)
    {
        if (set)
            time = value;
        else
            time += value;

        if (time <= 0)
        {
            time = 0;
            //EnterCamp(); // auto retreat from map? show info promt?
        }
        variables.SetVariable("@Time", time);
        MainGuiController.instance.timeDisplay.UpdateTime(day, time, night);
        //Map.UpdateDaylight(time);
    }
	/// <summary>
	/// changes or sets the ingame date
	/// </summary>
	/// <param name="value"></param>
	/// <param name="set"></param>
    public void ChangeDate(int value, bool set = false)
    {
        if(set)
        {
            day = value / 2;
            session = value % 2;
        }
        else
        {
            day += value / 2;
            session += value % 2;
            if(session > 1)
            {
                day += 1;
                session = 0;
            }
                
        }
		night = session == 1;
        MainGuiController.instance.timeDisplay.UpdateTime(day, time, night);
        Map.UpdateDaylight(night ? 0 : 720);
        if(campScene.gameObject.activeInHierarchy)
            campScene.GetComponent<Scenario>().ApplySettings(night, CurrentTime, BountyWeather.None);
    }
	/// <summary>
	/// changes the date, but without couting down any quest time limits or timers. jobs and character states will not tick forward either
	/// </summary>
	/// <param name="value">halbtage zu skippen</param>
	/// <param name="set">löse dann session change aus</param>
	public void ChangeDateComplex(int value, int exceptQuestId, bool pToggleSession = false)
	{
		day += value / 2;
		session += value % 2;
		if (session > 1)
		{
			day += 1;
			session = 0;
		}
		night = session == 1;
		MainGuiController.instance.timeDisplay.UpdateTime(day, time, night);
		Map.UpdateDaylight(night ? 0 : 720);
		if (campScene.gameObject.activeInHierarchy)
			campScene.GetComponent<Scenario>().ApplySettings(night, CurrentTime, BountyWeather.None);

		questManager.SkipDateComplex(value, exceptQuestId);
		camp.SkipDateComplex(value);

		if(pToggleSession)
		{
			
			EnterCamp(false, false, false, SkipComplexHelper);
		}
	}
	// helper function used as callback in the ChangeDateComplex() function
	private void SkipComplexHelper()
	{
		LeaveBase();
	}
	/// <summary>
	/// enables or diables the player camp scene
	/// </summary>
	/// <param name="value"></param>
	public void ToggleCamp(bool value)
	{
		if (campToggleRoutine != null)
			StopCoroutine(campToggleRoutine);
		campScene.gameObject.SetActive(value);
	}
	/// <summary>
	/// enables or diables the player camp scene but with a delay, controlled by a coroutine
	/// </summary>
	/// <param name="value"></param>
	public void ToggleCampDelayed(bool value)
	{
		if (campToggleRoutine != null)
			StopCoroutine(campToggleRoutine);

		campToggleRoutine = StartCoroutine(BountyExtensions.DelayedFunction(0.5f, () => campScene.gameObject.SetActive(value)));
	}

	/// <summary>
	/// adds a pending death event to the queue
	/// </summary>
	/// <param name="bc"></param>
	public void AddPendingDead(int bc)
	{
		if(!pendingDeadEvents.Contains(bc))
		{
			pendingDeadEvents.Add(bc);
		}
	}

	/// <summary>
	/// goues through all pending death events and executes them
	/// </summary>
	private void CheckPendingDeads()
	{
		if(pendingDeadEvents == null)
		{
			pendingDeadEvents = new List<int>();
		}
		for (int i = 0; i < pendingDeadEvents.Count; i++)
		{
			eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.CharacterDied, pendingDeadEvents[i] });
		}
		pendingDeadEvents.Clear();
	}

	/// <summary>
	/// adds a controller device to the list of accepted / blocked devices
	/// </summary>
	/// <param name="id"></param>
	/// <param name="value"></param>
	public void AddControllerToList(string id, bool value)
	{
		if(!currentSettings.controllerList.Exists(n => n.Key == id))
		{
			currentSettings.controllerList.Add(new KeyValuePair<string, bool>(id, value));
			SaveSettings();
		}
	}

	/// <summary>
	/// loads the setings sdata
	/// </summary>
	/// <param name="pSettings"></param>
	/// <returns>returns true if load was successfull. if not or no file was found it returns false</returns>
	public static bool LoadSettings(ref SettingsData pSettings)
	{
		bool result = SDSaveLoad.Initialized && SDSaveLoad.FileExists("settings");
		if(result)
			result = SDSaveLoad.LoadData<SettingsData>("settings", ref pSettings);

		return result;
	}
	/// <summary>
	/// saves the settings dta
	/// </summary>
	public void SaveSettings()
	{
		SDSaveLoad.SaveData<SettingsData>("settings", ref currentSettings);
		SDSaveLoad.ScheduleWriteToDisk();
	}
	/// <summary>
	/// loads the meta information
	/// </summary>
	/// <returns>returns true if load was successfull. if not or no file was found it returns false</returns>
	public bool LoadMetaInfo()
	{
		bool result = SDSaveLoad.FileExists("metainfo");
		if (result)
			result = SDSaveLoad.LoadData<MetaInfo>("metainfo", ref metaInfos);

		return result;
	}
	/// <summary>
	/// saves the meta information
	/// </summary>
	public void SaveMetaInfo()
	{
		SDSaveLoad.SaveData<MetaInfo>("metainfo", ref metaInfos);
		SDSaveLoad.ScheduleWriteToDisk();
	}

	/// <summary>
	/// situations:
	/// -1 = dead, 0 = onSessionEnd, 1 = onNextSession after defense, 2 = onLeftScenario, 3 = on map, 4 = on resume travel, 5 = in camp, 6 = in op
	/// </summary>
	/// <param name="situation"></param>
	/// <returns></returns>
	public bool Save(int situation)
    {
		if (exceptionState > 0)
			return false;

		if(situation == 3 || situation == 5 || situation == 6)
		{
			if(Map.mapCam.Scrolling || !MainGuiController.instance.CanOpenSystemMenu())
				return false;
		}

        //Debug.Log("Saving Game");
		if(currentTutIndex >= 0)
		{
			//return false;
			if ((currentTutIndex == 0 && situation == 2) || (currentTutIndex > 1 && situation == 0))
			{
				Debug.Log("Tutorial save!");
			}
			else
			{
				return false;
			}
		}

		if(!debugDontSaveMetaInfo && metaInfoChanged)
		{
			SaveMetaInfo();
			metaInfoChanged = false;
		}

		// pack data
		MapData md;
		for (int i = 0; i < mapList.Count; i++)
		{
			if (mapData.Count <= i)
				mapData.Add(null);

			md = null;
			//md = mapData[i];
			mapList[i].Save(out md, currentMap == i);
			mapData[i] = md;
		}
		// pack manager
		BountySaveData bsd = new BountySaveData(this);
		bsd.header.situation = situation;
		string backupStr = "";
		backupIndex++;
		if (backupIndex > 1)
		{
			backupIndex = 0;
			backupStr = "b";
		}
		bool tSuccess = false;
		string backupContent = "";
		// backup last save before overwriting it
		if(playerCharacter.Health > 0 && SDSaveLoad.FileExists("Save" + SaveSlot)) // but only if the char isnt dead already 23.7.20
		{
			tSuccess = SDSaveLoad.ReadFile("Save" + SaveSlot, ref backupContent);
			//tSuccess = SDSaveLoad.BackupSave("Save" + SaveSlot, "Backup" + SaveSlot + backupStr); // added backup b 30.11.20
		}
		// save to disk
		bool result = SDSaveLoad.SaveGame("Save" + SaveSlot, bsd);
		// show info ?
		if (result && tSuccess)
		{
			// write backup

			SDSaveLoad.WriteFile("Backup" + SaveSlot + backupStr, backupContent);
		}
		SDSaveLoad.ScheduleWriteToDisk();
		return result;
    }

    /// <summary>
    /// loads complete data from file and overwrites all values in this manager. also proceeds the correct gameplay procedure depending on the situation the savegame was made in
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public IEnumerator Load()
    {
		yield return null;
		// loading game data
		loading = true;
        BountySaveData bsd = new BountySaveData();
		// pre initialize all managers from their prefab state and apply them to the save data container
		bsd.dialogueManager = dialogueManager;
		bsd.camp = Instantiate<BountyCamp>(managerPrefabs.camp);
        bsd.camp.OnLoadPrewarm();
        bsd.questManager = Instantiate<BountyQuestManager>(managerPrefabs.questManager);
        bsd.mapEventDatabase = Instantiate<MapEventDatabase>(managerPrefabs.mapEventDatabase);
        bsd.factionManager = Instantiate<FactionManager>(managerPrefabs.factionManager);
		bsd.persistentManager = Instantiate<PersistentManager>(managerPrefabs.persistentManager);
		bsd.scenarioManager = Instantiate<BountyScenarioManager>(managerPrefabs.scenarioManager);
		bsd.storyDatabase = Instantiate<StoryDatabase>(managerPrefabs.storyDatabase);

		//bsd.eventManager = eventManager;

		// initializing more managers from prefab state
		travelEventDatabase = Instantiate<TravelEventDatabase>(managerPrefabs.travelEventDatabase);
        combatManager = Instantiate<BountyCombatManager>(managerPrefabs.combatManager);
        combatAi = Instantiate<BountyCombatAI>(managerPrefabs.combatAi);
		lootLocationDatabase = Instantiate<LootLocationDatabase>(managerPrefabs.lootLocationDatabase);
		lootEventDatabase = Instantiate<LootEventDatabase>(managerPrefabs.lootEventDatabase);
		characterDatabase = Instantiate<CharacterDatabase>(managerPrefabs.characterDatabase);
		achievementDatabase = Instantiate<BountyAchievementDatabase>(managerPrefabs.achievementDatabase);

		// setup some requred data
		achievementDatabase.Setup();
		combatManager.Setup();
		lootEventDatabase.CacheEntries();
		characterDatabase.CacheEntries();

		yield return new WaitUntil(() => lootEventDatabase.PendingCaches == 0 && characterDatabase.PendingCaches == 0);

		// load the save from disk and deserialize it, that will overwrite the states of managers to become the state of the saved data
		if (!SDSaveLoad.LoadGame("Save" + SaveSlot, ref bsd))
		{
			if (!SDSaveLoad.LoadGame("Backup" + SaveSlot, ref bsd))
			{
				if (!SDSaveLoad.LoadGame("Backup" + SaveSlot + "b", ref bsd))
				{
					HandleGeneralException(new SDGeneralException("Failed to load saved data"));
					yield break;
				}
			}
		}
		currentTutIndex = bsd.tutorialIndex;
		bsd.OnLoadFinish(); // prepare some stuff in the deserialzed data

		// update references
		dialogueManager = bsd.dialogueManager;
        factionManager = bsd.factionManager;
        questManager = bsd.questManager;
        camp = bsd.camp;
		storyDatabase = bsd.storyDatabase;
		storyDatabase.Setup();
        if (bsd.NonPlayerBase != null)
        {
            NonPlayerBase = bsd.NonPlayerBase;
        }

        if (bsd.header.saveFileVersion < 3) // DA3 fix für umgestellte event manager sub list
		{
			eventManager.currentData.idCount = bsd.eventManager.idCount;
			eventManager.currentData.blockInstances = bsd.eventManager.blockInstances;
			eventManager.currentData.blockInstanceArchive = bsd.eventManager.blockInstanceArchive;
		}
		else
		{
			eventManager.currentData = bsd.eventManagerData;
		}

        mapEventDatabase = bsd.mapEventDatabase;
		persistentManager = bsd.persistentManager;
		scenarioManager = bsd.scenarioManager;
		
		variables = bsd.variables;
        variables.Merge(eventManager.GetVariableDatabase(), true);
		eventManager.Load();
		// moved event patches to lower down
		persistentManager.Load();
		factionManager.Load();
		camp.PreSetupData();

		// apply manager variables
		currentFight = bsd.currentFight;
        fightType = bsd.fightType;
        eventActive = bsd.eventActive;
        day = bsd.day;
        night = bsd.night;
        time = bsd.time;
		itemCounter = bsd.itemCounter;
        session = bsd.session;
        currentCombatScenario = bsd.currentCombatScenario;
        currentLoot = bsd.currentLoot;
        fights = bsd.fights;
        playerCharacter = bsd.playerCharacter;
        currentEventChoiceCharacter = bsd.currentEventChoiceCharacter;
		defenceVictim = bsd.currentDefenceVictim;
		pendingDefenceData = bsd.pendingDefence;
		difficulty = bsd.difficulty;
		mapData = bsd.mapData;
		currentMap = bsd.currentMap;
		pendingDeadEvents = bsd.pendingDeadEvents;
		onScenarioLeftOverride = bsd.onLeftScenarioOverride;
		startFightOverride = bsd.startFightOverride;
		lastRoomType = bsd.lastRoomType;
		backupIndex = bsd.backupIndex;
		if(lootEventTracker == null)
			lootEventTracker = new Dictionary<int, List<int>>();

		inDungeon = currentMap != 0;
		// unpack map data
		for (int i = 0; i < mapList.Count; i++)
		{
			if (mapData.Count <= i)
				mapData.Add(null);

			if(currentMap == i || i == 0)
			{
				if(mapData[i].isProcedural)
					mapList[i].SetupLoadData(mapData[i]);
				mapList[i].Setup(true);
			}
			mapList[i].Load(mapData[i], currentMap == i || i == 0, bsd.header.saveFileVersion);
			mapList[i].ApplyMapPatches(bsd.header.saveFileVersion);
			mapList[i].gameObject.SetActive(currentMap == i);
		}
		loadedSaveFileVersion = bsd.header.saveFileVersion;
		// apply patches
		camp.ApplyItemPatches(bsd.header.saveFileVersion);
		Map.TeleportTo(Map.PartyPosition);
		Map.travel.TravelStep = bsd.travelState;
		eventManager.ApplyEventPatches(bsd.header.saveFileVersion);
		questManager.OnLoaded(bsd.header.saveFileVersion);
		// prepare game stuff to start
		Map.UpdateDaylight(night ? 0 : 720);
        Map.recentLoad = true;
		if(!string.IsNullOrEmpty(bsd.randomState) && debugLoadRNG)
			SDRandom.State = bsd.randomState;
		if (bsd.notificationQueue != null)
			MainGuiController.instance.notificationPanel.Queue = new List<NotificationData>(bsd.notificationQueue);
		//Debug.Log("Game Loaded");

		MainGuiController.instance.resourceDisplay.UpdateResources(camp.GetResourceArray(), true);
		campScene.UpdateRoomModels(camp.CurrentRooms);

		// start the correct game situation
		if (bsd.header.situation == 0) // onSessionEnd
		{
			inCamp = false;
			EnterCamp();
		}
		else if(bsd.header.situation == 1) // on session after defense
		{
			startup = false;
			inCamp = true;
			loading = false;
			NextSession(true, false);
		}
		else if(bsd.header.situation == 2) // on scenario left
		{
			startup = false;
			loading = false;
			inCamp = false;

			//StartDayLife(false, false);
			campScene.defenseScenario.onEnabledHook += () => StartDayLife(false, false, false);

			LeaveCampAfterLoad(true, true);
			SimulateScenarioLeave();

		}
		else if (bsd.header.situation == 3) // on map
		{
			startup = false;
			loading = false;
			inCamp = false;

			//StartDayLife(false, false); // changed 22.10.20
			campScene.defenseScenario.onEnabledHook += () => StartDayLife(false, false, false);

			LeaveCampAfterLoad();
		}
		else if (bsd.header.situation == 4) // on Resume Travel
		{
			startup = false;
			loading = false;
			inCamp = false;

			//StartDayLife(false, false);
			campScene.defenseScenario.onEnabledHook += () => StartDayLife(false, false, false);
			
			Map.travel.TravelSuspended = true;
			LeaveCampAfterLoad();
			OnScenarioLeft(true);
			MainGuiController.instance.mapGui.UpdateButtons(false);

		}
		else if (bsd.header.situation == 5) // quit in camp
		{
			startup = false;
			inCamp = true;
			loading = false;

			EnterCamp();
			NextSession(false, true);
		}
		else if (bsd.header.situation == 6) // quit in base
		{
			startup = false;
			inCamp = false;
			inBase = true;
			loading = false;


			campScene.defenseScenario.onEnabledHook += () => StartDayLife(false, false, false);
			LeaveCampAfterLoad(false, false, true);
			// load op
			if (Map.CurrentPoint.type == WaypointType.Base && Map.CurrentPoint.BaseRef.state == 0 && Map.GetArea(Map.CurrentPoint.area).owner != Faction.Player && BountyManager.instance.Map.CurrentPoint.activeBaseRefId >= 0)
			{
				BountyManager.instance.StartFight(BountyManager.instance.factionManager.GetBaseDataTemplate(BountyManager.instance.Map.CurrentPoint.activeBaseRefId).scenario, BountyManager.instance.InBase ? 11 : 10, false);
			}

		}
        StartCoroutine(SpawnBaseWhileStartingGame());

        eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.GameLoaded, bsd.header.situation });
		
		loadGame = false;
	}
	/// <summary>
	/// used to save the faction standing high scores
	/// </summary>
	public void SaveFactionEndsData()
	{
		persistentManager.ChangeCurrentStat(BountyPersistentStat.Factions_FinalReputation_Independent, factionManager.GetRelationship(Faction.Independents, 0), true);
		persistentManager.ChangeCurrentStat(BountyPersistentStat.Factions_FinalReputation_Military, factionManager.GetRelationship(Faction.Military, 0), true);
		persistentManager.ChangeCurrentStat(BountyPersistentStat.Factions_FinalReputation_Smuggler, factionManager.GetRelationship(Faction.Smugglers, 0), true);
	}

	/// <summary>
	/// called when the game is ended either by loosing or some quest trigger
	/// </summary>
	/// <param name="showReason"></param>
	/// <param name="countDeath"></param>
	public void GameOver(bool showReason = false, bool countDeath = true, bool positive = false)
	{
		// prepare important data and save
		SaveFactionEndsData();
		if(countDeath)
		{
			persistentManager.Deaths += 1;
			persistentManager.ChangeCurrentStat(BountyPersistentStat.Game_Deaths, persistentManager.Deaths, true);
		}
		Save(-1);

		// show gui
		MainGuiController.instance.gameOverScreen.gameObject.SetActive(true);
		audioManager.PlayMusic(gameOverMusic, true);

		dialogueManager.SetGameOver();
		List<string[]> build = new List<string[]>();
		if (showReason)
		{
			//build.Add(new string[] { "CampDeath_3_" + playerCharacter.DeathReason.ToString(), null, null, null, null, null, "Jack" });
		}
		// on first death show info
		if (persistentManager.Deaths == 1)
		{
			build.Add(new string[] { "Start_Trish_death" });
			build.Add(new string[] { "Start_Trish_death_1" });
		}
		dialogueManager.StartProceduralDialogue(build, -1, -350);
	}
	/// <summary>
	/// quits the game. attempts to save when in camp or in a base
	/// </summary>
	public void QuitGame()
	{
		if (!IsEventActive() && !IsFightActive())
		{
			if (InCamp)
			{
				Save(5);
			}
			else if (InBase)
			{
				Save(6);
			}
			else
			{
				Save(3);
			}
		}

		shutDown = true;
		StopAllCoroutines();

		Application.Quit();
	}

	/// <summary>
	/// returns back to main menu. attempts to save when in camp or in a base
	/// </summary>
	/// <param name="saveGame"></param>
	public void BackToMainMenu(bool saveGame)
    {
		if(saveGame && !IsEventActive() && !IsFightActive())
		{
			if (InCamp)
			{
				Save(5);
			}
			else if (InBase)
			{
				Save(6);
			}
			else
			{
				Save(3);
			}
		}

        shutDown = true;
		if (onQuitGameSession != null)
			onQuitGameSession();

		combatManager.PrepareShutDown();
		StopAllCoroutines();
		GameObject go = new GameObject("BackToMenuRoutine");
		AudioSourceHelper ash = go.AddComponent<AudioSourceHelper>();
		MainGuiController.instance.transitionPanel.FadeToLoading(() => ash.StartCoroutine(ResetToMainState()), false);

    }

    /// <summary>
    /// Abandons the current run and returns to character creation
    /// </summary>
    public void OnBackToCharSelection()
	{
		SaveFactionEndsData();
		Save(-1);
		lastSaveSlot = SaveSlot;
		openLastSaveInMainMenu = true;
		BackToMainMenu(false);
	}
	
	/// <summary>
	/// executes a routine to get the game in the shutdown phase so all data is cleaned up before leaving the scene
	/// </summary>
	/// <returns></returns>
    private IEnumerator ResetToMainState()
    {
		if(scenarioManager.CurrentScenario != null)
			scenarioManager.CurrentScenario.gameObject.SetActive(false);

		Destroy(dialogueManager);
        Destroy(factionManager);
        Destroy(travelEventDatabase);
        Destroy(storyDatabase);
        Destroy(scenarioManager);
        Destroy(camp);
        Destroy(combatManager);
        Destroy(combatAi);
        Destroy(questManager);
        Destroy(lootLocationDatabase);
        Destroy(lootEventDatabase);
        Destroy(characterDatabase);
        Destroy(mapEventDatabase);
        Destroy(persistentManager);
        Destroy(achievementDatabase);

		//Destroy(inputManager.inControl);
		//Destroy(inputManager);

		
		MainGuiController.instance.PrepareForShutdown();
        campScene.gameObject.SetActive(false);
        Map.gameObject.SetActive(false);
		
		yield return null;
		Application.logMessageReceived -= UnHandledException;

		_isDestroyed = true;
        _instance = null;
		exceptionState = 0;

		Destroy(gameObject);
		Resources.UnloadUnusedAssets(); // added 23.7.20

		Scene oldScene = SceneManager.GetActiveScene();

		AsyncOperation operation = SceneManager.LoadSceneAsync("MainMenuStage", LoadSceneMode.Additive );
        while(!operation.isDone)
        {
            yield return null;
        }
		MainGuiController.instance.gameObject.SetActive(false);
		yield return new WaitForEndOfFrame();
        
		// leave the scene
        Scene newScene = SceneManager.GetSceneByName("MainMenuStage");
		SceneManager.UnloadSceneAsync(oldScene);
		SceneManager.SetActiveScene(newScene);

        //yield return new WaitForEndOfFrame();
        
    }
	/// <summary>
	/// triggers the interpolated change in timescale of the unity engine
	/// </summary>
	/// <param name="targetValue"></param>
	/// <param name="duration"></param>
	public void ShiftTimeScale(float targetValue, float duration)
	{
		if(!shutDown)
		{
			if (timeScaleRoutine != null)
				StopCoroutine(timeScaleRoutine);
			timeScaleRoutine = StartCoroutine(TimeScaleRoutine(targetValue, duration));
		}
	}
	/// <summary>
	/// changes the time scale value but fades it smoothliy over a certain time
	/// </summary>
	/// <param name="targetValue"></param>
	/// <param name="duration"></param>
	/// <returns></returns>
	private IEnumerator TimeScaleRoutine(float targetValue, float duration)
	{
		float delta = 0;
		float startValue = Time.timeScale;
		while(delta < 1f)
		{
			delta += Time.fixedUnscaledDeltaTime / duration;
			Time.timeScale = Mathf.Lerp(startValue, targetValue, delta);
			yield return new WaitForSecondsRealtime(Time.fixedUnscaledDeltaTime);
		}
	}

    
    private void Update()
    {
		SDSaveLoad.Update();

		if(dialogueManager != null)
			dialogueManager.CustomUpdate();

		if (combatManager != null)
			combatManager.DebugUpdate();

		// debug stuff
		if (shortcutLocked)
			return;
		if (Input.GetKeyDown(KeyCode.F12))
		{
			SaveScreenShot("$d", "Screenshots/");
		}
		if (Input.GetKeyDown(KeyCode.F11))
		{
			Settings.fullScreen = !Settings.fullScreen;
			Screen.fullScreen = Settings.fullScreen;
		}
		if (Input.GetKeyDown(KeyCode.F4) && ingame)
		{
			MainGuiController.instance.fpsCounter.SetActive(!MainGuiController.instance.fpsCounter.activeSelf);
		}


		if (debugMode)
		{
			if (Input.GetKeyDown(KeyCode.C) && Input.GetKey(KeyCode.LeftAlt))
			{
				try
				{
					TestException();
				}
				catch (SDGeneralException ex)
				{
					HandleGeneralException(ex);
				}

			}
			if (Input.GetKeyDown(KeyCode.F3))
			{
				slowMode = !slowMode;
				Time.timeScale = slowMode ? 0.1f : 1f;
			}
			//if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.F1) && debugMode)
			//{
			//          // open debug menu
			//}
			if (Input.GetKeyDown(KeyCode.F1))
			{
				if (MainGuiController.instance != null)
					MainGuiController.instance.ToggleVisible();
			}
			//if (Input.GetKeyDown(KeyCode.F5))
			//{
			//	if (scenarioManager.CurrentScenario != null)
			//		scenarioManager.CurrentScenario.guiCam.enabled = !scenarioManager.CurrentScenario.guiCam.enabled;
			//	if (combatManager.combatTransform != null)
			//		combatManager.combatTransform.SetCollider(false);
			//}
			if (Input.GetKeyDown(KeyCode.F2))
			{
				variablesDebug = !variablesDebug;
			}
			if (Input.GetKeyDown(KeyCode.F6))
			{
				combatManager.debugControlAI = !combatManager.debugControlAI;
			}
			if (Input.GetKeyDown(KeyCode.J))
			{
				List<BountyCharacter> tList = camp.GetParty(true, true);
				if (IsFightActive())
					tList = combatManager.combatants.FindAll(n => n.allied);
				foreach (var item in tList)
				{
					item.HealthPercent = 5;
				}
			}
			if (Input.GetKeyDown(KeyCode.M))
			{
				Map.ToggleDebugVisible();
			}
			//if (Input.GetKeyDown(KeyCode.N))
			//{
			//	DebugTestTroopCombat();
			//}
			if (Input.GetKeyDown(KeyCode.I))
			{
				camp.GiveDebugItems(1);
			}
			if (Input.GetKeyDown(KeyCode.U) && !IsFightActive())
			{
				playerCharacter.AddXp(playerCharacter.GetXpForNextLevel() - playerCharacter.Exp + 10);
				OrderedCheckLevelUp();
			}
			if (Input.GetKeyDown(KeyCode.F8))
			{
				persistentManager.ClearAchievements();
			}

		}

		if (!debugMode)
        {
			// check if debug keyword was entered
            if(codeWord.Length > 50)
            {
                codeWord = "";
            }
            codeWord += Input.inputString;
            if (codeWord.Length > 0 && codeWord[codeWord.Length-1] == "\b"[0])
            {
                codeWord = "";
            }
            else if(codeWord.Contains("silentpw"))
            {
                debugMode = true;
				audioManager.PlaySoundEffect("achievement");
                Debug.Log("Debug Mode enabled");
                codeWord = "";
            }
		}
        
    }

	public static void SaveScreenShot(string nameFormat, string folder = "")
	{
		nameFormat = nameFormat.Replace("$d", GetDateString());

#if UNITY_EDITOR
		string file = Application.dataPath;
		// trim the "Assets/" part but only in editor mode right?!
		file = file.Substring(0, file.Length - 6);
#else
		string file = Application.persistentDataPath+"/";
#endif

		if (!string.IsNullOrEmpty(folder))
		{
			file += folder;
		}

		if (!Directory.Exists(file))
			Directory.CreateDirectory(file);

		file += nameFormat + ".png";

		ScreenCapture.CaptureScreenshot(file);
		Debug.Log("Screenshot saved at: " + file);
	}

	private static string GetDateString()
	{
		return System.DateTime.Now.ToString("dd_MM_yyyy HH_mm_ss");
	}

	/// <summary>
	/// saves debugging information to disk
	/// </summary>
	/// <param name="file"></param>
	/// <param name="exceptionText"></param>
	/// <returns></returns>
	private bool SaveGameInformation(string file, string exceptionText)
	{
		try
		{
			// file will be: "Crash reports/" + GetDateString() + "/"

#if UNITY_EDITOR
			string path = Application.dataPath;
			// trim the "Assets/" part but only in editor mode right?!
			path = path.Substring(0, path.Length - 6);
#else
			string path = Application.persistentDataPath+"/";
#endif

			FileStream fs = File.Open(path+"/"+file+"Crash_Info.txt", FileMode.Create);
			StreamWriter sw = new StreamWriter(fs);
			sw.WriteLine("---Crash report info---");
			sw.WriteLine(exceptionText);
			sw.WriteLine("\n---system information---");
			sw.WriteLine(string.Format("Operating System {0}", SystemInfo.operatingSystem));
			sw.WriteLine(string.Format("Processor type {0}", SystemInfo.processorType));
			sw.WriteLine(string.Format("Processor frequency {0}", SystemInfo.processorFrequency));
			sw.WriteLine(string.Format("Processor count {0}", SystemInfo.processorCount));
			sw.WriteLine(string.Format("System memory size {0}",SystemInfo.systemMemorySize));
			sw.WriteLine(string.Format("Graphics device name {0}", SystemInfo.graphicsDeviceName));
			sw.WriteLine(string.Format("Graphics device type {0}", SystemInfo.graphicsDeviceType.ToString()));
			sw.WriteLine(string.Format("Graphics device vendor {0}", SystemInfo.graphicsDeviceVendor));
			sw.WriteLine(string.Format("Graphics memory size {0}", SystemInfo.graphicsMemorySize));
			sw.WriteLine(string.Format("Graphics shader level {0}", SystemInfo.graphicsShaderLevel));
			sw.WriteLine("\n---additional game information---");
			sw.WriteLine(string.Format("ExceptionState: {0}", exceptionState));
			sw.WriteLine(string.Format("Debug Mode enabled: {0}", debugMode));
			sw.WriteLine(string.Format("Starts from menu: {0}", menuStarts));
			sw.WriteLine(string.Format("Selected language: {0}", Settings.gameLanguage.ToString()));
			sw.WriteLine(string.Format("Controller mode: {0}", inputManager.ControllerMode));
			sw.WriteLine(string.Format("Controller layout: {0}", inputManager.ControllerLayout));
			sw.WriteLine("\n---Game state information---");
			// pack data
			MapData md;
			for (int i = 0; i < mapList.Count; i++)
			{
				if (mapData.Count <= i)
					mapData.Add(null);

				md = null;
				mapList[i].Save(out md, currentMap == i);
				mapData[i] = md;
			}
			BountySaveData bsd = new BountySaveData(this);
			fsData gameData;
			fsResult res = SDSaveLoad.SerializeGame(bsd, out gameData);
			if (res.Succeeded)
				sw.WriteLine(fsJsonPrinter.PrettyJson(gameData));
			else
				sw.WriteLine("Failed to serialze game data");
			sw.Close();
			fs.Close();


#if !UNITY_EDITOR
			if (File.Exists(path + "Player.log"))
				File.Copy(path + "Player.log", path + "/" + file + "Player.log");

			string savef = "Save" + saveSlot + ".json";
			if (File.Exists(path + savef))
				File.Copy(path + savef, path + "/" + file + savef);
			savef = "Backup" + saveSlot + ".json";
			if (File.Exists(path + savef))
				File.Copy(path + savef, path + "/" + file + savef);
			savef = "Backup" + saveSlot + "b.json";
			if (File.Exists(path + savef))
				File.Copy(path + savef, path + "/" + file + savef);
#endif
			return true;
		}
		catch(System.Exception)
		{
			Debug.LogError("Failed to create crash report information");
			return false;
		}
	}
	/// <summary>
	/// function for old unity debug UI on screen
	/// </summary>
	private void OnGUI()
    {
        if(variablesDebug)
        {
            List<string> keys = new List<string>(Variables.GetVariableNames());
            int amount = keys.Count;
            float lineHeight = 50f;
            variablesScrollViewVector = GUI.BeginScrollView(new Rect(100, 100, 1600, 900), variablesScrollViewVector, new Rect(0, 0, 1600, amount * lineHeight));
            GUILayout.BeginVertical();
            int defaultFont = GUI.skin.label.fontSize;
            GUI.skin.label.fontSize = 32;
            for(int i = 0; i < amount; i++)
            {
                GUI.Label(new Rect(0, i* lineHeight, 1600, lineHeight), string.Format("k = {0} v = {1}", keys[i], Variables.GetVariable(keys[i]).AsString()));
            }
            GUI.skin.label.fontSize = defaultFont;
            GUILayout.EndVertical();
            GUI.EndScrollView();
        }
			//GUI.skin.label.fontSize = 32;
			//GUI.Label(new Rect(Screen.width - 100, 100, 100, 50), Time.timeScale.ToString());
			//GUI.skin.label.fontSize = GUI.skin.label.fontSize;
	}


    [fsObject]
    [System.Serializable]
    public class BountySaveData
    {
		public BountySaveHeader header;
		public PersistentManager persistentManager;
        public BountyCamp camp;
        public NewBases NonPlayerBase;
        public FactionManager factionManager;
		public SDDialogueManager dialogueManager;
        public BountyQuestManager questManager;
        public EventManager eventManager;
        public EventManagerData eventManagerData;
		public MapEventDatabase mapEventDatabase;
		public BountyScenarioManager scenarioManager;
		public StoryDatabase storyDatabase;
		
        public VariableDatabase variables = new VariableDatabase(); // stores all runtime variables and quest states (may become a bit big during the game eg 100+ entries)
        public bool inCamp;
        public int currentFight; // index of fight in a sequence of fights eg 3
        public int fightType; // distignuish between normal 3 encounter fight, 1 encounter fight or defense fight
        public bool fightActive;
        public bool eventActive;
        public int day; // survived days
        public bool night; // night or day session
        public int time; // remaining time till end of session
		public int itemCounter;
        public int session; // is it the first or second run this day
        public BountyScenarioEntry currentCombatScenario;
        public List<BaseItem> currentLoot;
        public int fights; // number of fights in this scene
        public BountyCharacter playerCharacter; // refernce to the players character
        public BountyCharacter currentEventChoiceCharacter; // refernce to the selected victim of ean event (e.g. travel event)
		public BountyCharacter currentDefenceVictim;
		public int currentMap;
        public List<MapData> mapData;
        public Dictionary<int, int> lootEventLog;
        public List<CampDefenceData> pendingDefence;
        public GameDifficulty difficulty;
		public string randomState;
		public List<NotificationData> notificationQueue;
		public int eventCheckModifier;
		public int tutorialIndex;
		public List<int> pendingDeadEvents;
		public int onLeftScenarioOverride;
		public int startFightOverride;
		public CampRoomType lastRoomType;
		public int backupIndex = 0;
		public Dictionary<int, List<int>> lootEventTracker;
		public int travelState; // state of current waypoint travel
		/// <summary>
		/// temporarly stores the already loaded character references so we dont create double instances or split references of the same character into multiple
		/// </summary>
		[fsIgnore]
		private Dictionary<BountyCharacter, BountyCharacter> loadedList;

		public BountySaveData()
        { }
		/// <summary>
		/// creates save state from the current manager state
		/// </summary>
		/// <param name="man"></param>
        public BountySaveData(BountyManager man)
        {
			header = new BountySaveHeader()
			{
				saveDate = System.DateTime.Now,
				saveDifficulty = (int)man.Difficulty,
				saveFileVersion = Mathf.Max(man.loadedSaveFileVersion, man.saveFileVersion),
				gameVersion = string.Format("V:{0} B:{1} A:{2}", GameVersion.Version, GameVersion.MainBuild, GameVersion.AssetBuild),
				saveName = man.Player.CharName,
				ingameDate = man.DateTime,
				playerLevel = man.Player.Level,
                dayNight = man.Night,
			};
			dialogueManager = man.dialogueManager;
            factionManager = man.factionManager;
            questManager = man.questManager;
            camp = man.camp;
            eventManagerData = man.eventManager.currentData;
            mapEventDatabase = man.mapEventDatabase;
			persistentManager = man.persistentManager;
			scenarioManager = man.scenarioManager;
			storyDatabase = man.storyDatabase;
			NonPlayerBase = man.NonPlayerBase;
            variables = man.variables;
            inCamp = man.inCamp;
            currentFight = man.currentFight;
            fightType = man.fightType;
            eventActive = man.eventActive;
            day = man.day;
            night = man.night;
            time = man.time;
			itemCounter = man.itemCounter;
            session = man.session;
            currentCombatScenario = man.currentCombatScenario;
            currentLoot = man.currentLoot;
            fights = man.fights;
            playerCharacter = man.playerCharacter;
            currentEventChoiceCharacter = man.currentEventChoiceCharacter;
			currentDefenceVictim = man.defenceVictim;
			//partyPosition = man.partyPosition;
			currentMap = man.currentMap;
            mapData = man.mapData;
            //lootEventLog
            pendingDefence = man.pendingDefenceData;
            difficulty = man.difficulty;
			randomState = SDRandom.State;
			notificationQueue = new List<NotificationData>();
			if (MainGuiController.instance != null)
				notificationQueue.AddRange(MainGuiController.instance.notificationPanel.Queue);
			tutorialIndex = man.currentTutIndex;
			pendingDeadEvents = man.pendingDeadEvents;
			onLeftScenarioOverride = man.onScenarioLeftOverride;
			startFightOverride = man.startFightOverride;
			lastRoomType = man.lastRoomType;
			backupIndex = man.backupIndex;
			lootEventTracker = man.lootEventTracker;
			if (man.Map == null)
			{
				travelState = 0;
			}
			else
			{
				travelState = man.Map.travel.TravelStep;
			}
			
		}
		/// <summary>
		/// called after the first deserialization and before the second one. here we should reInstatiated all prefabs we identified in our data so we can refill any default values before finally reapplying the serialized state onto them
		/// </summary>
        public void OnLoadStep()
        {
			loadedList = new Dictionary<BountyCharacter, BountyCharacter>();
            camp.OnLoadStepExtended(loadedList);
			factionManager.OnLoadStep(loadedList);
			// remap refenreced char objects
			camp.RemapGlobalCharListAfterLoad(loadedList);

		}
		/// <summary>
		/// called after the second deserialization step
		/// </summary>
		public void OnLoadFinish()
		{
			camp.OnLoadedExtended(header.saveFileVersion, loadedList);
			factionManager.LoadExtended(header.saveFileVersion);
			
			
		}
	}

	[fsObject]
	[System.Serializable]
	public class BountySaveHeader
	{
		/// <summary>
		/// stores a counting integer of the save format version
		/// </summary>
		public int saveFileVersion;
		/// <summary>
		/// stores the game version for reference
		/// </summary>
		public string gameVersion;
		/// <summary>
		/// stores the displayed save name
		/// </summary>
		public string saveName;
		/// <summary>
		/// save date and time
		/// </summary>
		public System.DateTime saveDate;
		/// <summary>
		/// ingame date
		/// </summary>
		public int ingameDate;
		/// <summary>
		/// dificulty
		/// </summary>
		public int saveDifficulty;
		/// <summary>
		/// player level for display
		/// </summary>
		public int playerLevel;
		/// <summary>
		/// situation specifier for loading procedure
		/// </summary>
		public int situation;
        /// <summary>
        /// defines day / night
        /// </summary>
        public bool dayNight;
    }

}

// moved public enum BountyGameEvent to EventManager.GameEvents.cs 24.4.2023


public enum GameDifficulty
{
    Normal = 0,
    Hardcore = 1,
    Casual = 2,
}

[fsObject]
[System.Serializable]
public class CampDefenceData
{
	/// <summary>
	/// the faction that attacks
	/// </summary>
    [fsProperty]
    public Faction faction;
	/// <summary>
	/// the number of waves in the combat
	/// </summary>
    [fsProperty]
    public int waves;
	/// <summary>
	/// fixed waves is for not randomly but pre scripted wave templates
	/// </summary>
	[fsProperty]
	public bool fixedWaveCombat;
	/// <summary>
	/// was only used in one story instance: if equal to: 1 then a fake dummy combat will be triggered, usable for cutscenes
	/// </summary>
	[fsProperty]
	public int special; // new property
}

[System.Serializable]
public class ManagerCollection
{
    public SDDialogueManager dialogueManager;
    public FactionManager factionManager;
    public TravelEventDatabase travelEventDatabase;
    public StoryDatabase storyDatabase;
    public BountyScenarioManager scenarioManager;
    public BountyCamp camp;
    public BountyCombatManager combatManager;
    public BountyCombatAI combatAi;
    public BountyQuestManager questManager;
    public LootLocationDatabase lootLocationDatabase;
    public LootEventDatabase lootEventDatabase;
    public CharacterDatabase characterDatabase;
    public MapEventDatabase mapEventDatabase;
    public PersistentManager persistentManager;
    public BountyAchievementDatabase achievementDatabase;
	public SDAssetDatabase labyrinthDatabase;
	public SDAssetDatabase shopDatabase;
	public SDAssetDatabase baseIdleDatabse;
}

[fsObject]
[System.Serializable]
public class SettingsData
{
	[fsProperty]
	public int settingsVersion;
	[fsProperty]
    public float effectVolume;
    [fsProperty]
    public float musicVolume;
	[fsProperty]
	public float ambientVolume;
	[fsProperty]
	public float masterVolume;
	// [fsProperty]
	// public SystemLanguage language; // old deprecated
	[fsProperty]
	public BountyLanguage gameLanguage;
	[fsProperty]
    public Vector2Int resolution;
    [fsProperty]
    public int quality;
    [fsProperty]
    public bool fullScreen; // maybe deprecated?
	[fsProperty]
	public bool borderedWindow; // maybe deprecated?
	[fsProperty]
    public bool vSync;
	[fsProperty]
	public int monitor;
	[fsProperty]
	public List<KeyValuePair<string,bool>> controllerList = new List<KeyValuePair<string, bool>>();
	[fsProperty]
	public bool enableSoftParticles;
	[fsProperty]
	public bool enableSSAO; // maybe deprecated?
	[fsProperty]
	public int shadowQuality;
	[fsProperty]
	public int shadowResolution;
	[fsProperty]
	public int typeWriterSpeed; // 1 - fast, 2 - medium, 3 - slow
	[fsProperty]
	public bool swapCombatShortcuts;
	[fsProperty]
	public int superSamplingAntiAliasing;
	[fsProperty]
	public bool postProcessingBloom;
	[fsProperty]
	public bool postProcessingSSAO;
	[fsProperty]
	public bool postProcessingDepthOfField;
	[fsProperty]
	public int debugSSAAType;
	[fsProperty]
	public int debugSSAA_B;
	[fsProperty]
	public int windowMode;

	public static SettingsData GetDefaultSettings()
	{
		SettingsData result = new SettingsData()
		{
			settingsVersion = -1,
			resolution = BountyManager.GetStartResolution(),
			effectVolume = 0.7f,
			musicVolume = 0.7f,
			ambientVolume = 0.7f,
			masterVolume = 0.9f,
			gameLanguage = BountyManager.GetDefaultLanguage(),
			fullScreen = false,
			borderedWindow = true,
			vSync = false,
			quality = 1,
			controllerList = new List<KeyValuePair<string, bool>>(),
			enableSoftParticles = true,
			enableSSAO = true,
			shadowResolution = 2, // 0 - 3, sets the shadow map resolution.
			shadowQuality = 2, // 0 or 1. disable or enable real time lighting
			typeWriterSpeed = 1,
			superSamplingAntiAliasing = 2,
			debugSSAAType = 1,
			debugSSAA_B = 2,
			postProcessingBloom = true,
			postProcessingSSAO = true,
			postProcessingDepthOfField = true,
			windowMode = 1,
		};
		return result;
	}
	public SettingsData CopyClone()
	{
		SettingsData result = new SettingsData();
		result.settingsVersion = settingsVersion;
		result.effectVolume = effectVolume;
		result.musicVolume = musicVolume;
		result.ambientVolume = ambientVolume;
		result.masterVolume = masterVolume;
		result.gameLanguage = gameLanguage;
		result.resolution = resolution;
		result.quality = quality;
		result.fullScreen = fullScreen;
		result.borderedWindow = borderedWindow;
		result.vSync = vSync;
		result.monitor = monitor;

		result.controllerList = controllerList;

		result.enableSoftParticles = enableSoftParticles;
		result.enableSSAO = enableSSAO;
		result.shadowQuality = shadowQuality;
		result.shadowResolution = shadowResolution;
		result.typeWriterSpeed = typeWriterSpeed;
		result.swapCombatShortcuts = swapCombatShortcuts;
		result.superSamplingAntiAliasing = superSamplingAntiAliasing;
		result.postProcessingBloom = postProcessingBloom;
		result.postProcessingSSAO = postProcessingSSAO;
		result.postProcessingDepthOfField = postProcessingDepthOfField;
		result.debugSSAAType = debugSSAAType;
		result.debugSSAA_B = debugSSAA_B;
		result.windowMode = windowMode;


		return result;
	}


	// list of supported languages
	public static readonly List<BountyLanguage> languageList = new List<BountyLanguage>
	{
		BountyLanguage.German,
		BountyLanguage.English,
		BountyLanguage.French,
		//BountyLanguage.Spanish_Latin,
		//BountyLanguage.Spanish_Spain,
		//BountyLanguage.Portuguese_Brazil,
		//BountyLanguage.Russian,
		//BountyLanguage.Chinese_Simplified,
	};

	public static readonly Dictionary<string, BountyLanguage> languageTableRough = new Dictionary<string, BountyLanguage>()
	{
		{ "de", BountyLanguage.German },
		{ "en", BountyLanguage.English },
		{ "fr", BountyLanguage.French },
		{ "es", BountyLanguage.Spanish_Latin },
		{ "pt", BountyLanguage.Portuguese_Brazil },
		{ "ru", BountyLanguage.Russian },
		{ "zh", BountyLanguage.Chinese_Simplified },
	};
	public static readonly Dictionary<string, BountyLanguage> languageTableFine = new Dictionary<string, BountyLanguage>()
	{
		{ "es-ES", BountyLanguage.Spanish_Spain },
	};
}

public enum BountyLanguage
{
	None = 0,
	German = 10,
	English = 20,
	French = 30,
	Spanish_Latin = 40,
	Spanish_Spain = 41,
	Portuguese_Brazil = 50,
	Russian = 60,
	Chinese_Simplified = 70,

}

[fsObject]
[System.Serializable]
public class MetaInfo
{
	/// <summary>
	/// stores the activated story segments so they dont repeat randomly but are switched up round robin wise
	/// </summary>
	[fsProperty]
	public List<int> seenStorySegments;
	/// <summary>
	/// was tutorial played
	/// </summary>
	[fsProperty]
	public bool tutorialPlayed;
	/// <summary>
	/// countis the amounts of new games startet
	/// </summary>
	[fsProperty]
	public int runs;
	/// <summary>
	/// counting integer to store the read news state
	/// </summary>
	[fsProperty]
	public int newsRead;
}

public class SDGeneralException : System.Exception
{
	public string extraInfo;

	public SDGeneralException(string pInfo = "") : base()
	{
		extraInfo = pInfo;
	}
	public SDGeneralException(string message, string pInfo = "") : base(message)
	{
		extraInfo = pInfo;
	}
	public SDGeneralException(string message, System.Exception inner, string pInfo = "") : base(message, inner)
	{
		extraInfo = pInfo;
	}
}
