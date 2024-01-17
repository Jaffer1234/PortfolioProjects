using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// a navigation node in a base or a scene. mostly used in bases, may could be used in combat scenes, but that's not fully tested out yet
/// before the 3rd party navmesh and pathfinding tool was introduces this was used as a network of nodes where BaseNavAgents would move along the connections
/// a node is identified by a node type and a station type. the node also stores information when it is reachable (room upgrades) and what animations should be played when a character arrives at the node
/// </summary>
[SelectionBase]
public class BaseNavNode : MonoBehaviour {

	//public static List<BaseNavNode> BaseNavController.activeController.Nodes = new List<BaseNavNode>();

	//public BaseNavNode[] connections; // not used anymore
	public BaseNavObjectInteraction objectAnchor;
	public NodeType nodeType;
	public StationType stationType;
	public CampRoomType roomType;
	public string animationSwitch1; // switches on Only on node and off afterwards
	public string animationSwitch2;
	public string animationTrigger; // triggers once
	public string animationSwitchToggle; // keeps its value
	public bool animationToggleValue;
	public string animationSwitchInteger; // keeps its value
	public int animationIntegerValue;
	public int priority;
	public int roomMinLvl = 1;
	public bool dontPreRotate; // used on target points that are to close to walls to pre turn into the point looking direction
	public bool dontTeleportTo;
	public BaseIdleData[] idleData;
	public bool updateModelBuild; // used for update naked model in recreation room
	public bool nodeEnabled = true;
	public bool tutorialOnly;
	public string conditionVariableCheck; // checks global variable to be true otherwise ignore the node
	public bool occupied;
	public bool reAssertIdleAnis;

	public bool dontSpawnModelsRandomly;

	public Vector3 skipPositionOffset;
	public Vector3 skipRotationOffset;

	private Vector3 offset = new Vector3(0f, 0.1f, 0f);
	[SerializeField]
	private Material[] myMaterials;

	private static Material[] myMatLoading;

	[HideInInspector]
	public float myScore;

	private void Start()
	{

		if(GetComponent<MeshRenderer>())
			GetComponent<MeshRenderer>().enabled = false;
		/*
		for(int i = 0; i < connections.Length; i++)
		{
			if(connections[i] == null)
			{
				Debug.LogErrorFormat(gameObject, "Connection {0} of node {1} is left empty", i, gameObject.name);
			}
			if(connections[i] == this)
			{
				Debug.LogErrorFormat(gameObject, "Connection {0} of node {1} is linked to itself", i, gameObject.name);
			}
		}
		*/
	}

	private void OnEnable()
	{

	}
	private void OnDisable()
	{

	}

	// triggered by ctrl + middle mouse button in sceneview, was used before new navmesh tool
#if UNITY_EDITOR
	public void ToggleLink(BaseNavObjectInteraction node)
	{
		objectAnchor = node;
		if (node != null && node.navNodePosition != null)
		{
			UpdateAnchorPos();
		}
		UnityEditor.EditorUtility.SetDirty(this);
	}

	public void UpdateAnchorPos()
	{
		if(objectAnchor != null && objectAnchor.navNodePosition != null)
		{
			transform.position = objectAnchor.navNodePosition.position;
			transform.rotation = objectAnchor.navNodePosition.rotation;
		}
	}
#endif

	public bool IsUnlocked()
	{
		//if(BountyManager.instance.InBase)
		//{
		//	return true;
		//}
		if (!string.IsNullOrEmpty(conditionVariableCheck))
		{
			return BountyManager.instance.Variables.GetVariable(conditionVariableCheck).AsBool();
		}
		if (roomMinLvl > 0)
		{
			return BountyManager.instance.camp.GetRoomLevel(roomType) >= roomMinLvl;
		}
		
		return true;
	}

	public void DebugDraw()
	{
		//if (myMaterial == null)
		//{
		//	//if(Application.isPlaying)
		//		myMaterial = GetComponent<MeshRenderer>().material;
		//	// else
		//	// 	myMaterial = GetComponent<MeshRenderer>().sharedMaterial;
		//}

		if(myMaterials == null || myMaterials.Length == 0)
		{
			if(myMatLoading == null || myMatLoading.Length == 0)
			{
				myMatLoading = new Material[4];
				myMatLoading[0] = Resources.Load<Material>("Debug/DebugWhite");
				myMatLoading[1] = Resources.Load<Material>("Debug/DebugBlue");
				myMatLoading[2] = Resources.Load<Material>("Debug/DebugYellow");
				myMatLoading[3] = Resources.Load<Material>("Debug/DebugGreen");
			}
			myMaterials = new Material[4];
			myMaterials[0] = myMatLoading[0];
			myMaterials[1] = myMatLoading[1];
			myMaterials[2] = myMatLoading[2];
			myMaterials[3] = myMatLoading[3];
		}

		/*
		if (connections == null)
		{
			connections = new BaseNavNode[0];
		}
		for (int i = 0; i < connections.Length; i++)
		{
			if(connections[i] != null)
			{
				//Gizmos.color = connections[i].occupied ? Color.red : Color.green;
				//Gizmos.DrawLine(transform.position, connections[i].transform.position + offset);
				
				Debug.DrawLine(transform.position, connections[i].transform.position + offset, connections[i].occupied ? Color.red : Color.green);

				//if (Application.isPlaying)
				//{
				//	Color restoreColor = GUI.color;
				//	UnityEditor.Handles.BeginGUI();
				//	GUI.color = Color.white;
				//	UnityEditor.Handles.Label(transform.position, myScore.ToString());
				//	GUI.color = restoreColor;
				//	UnityEditor.Handles.EndGUI();
				//}
			}
				
		}
		*/
		Color c = Color.white;
		if (nodeType == NodeType.Station || nodeType == NodeType.Target)
		{
			c = Color.blue;
			GetComponent<MeshRenderer>().sharedMaterial = myMaterials[1];
		}
		else if(nodeType == NodeType.Idle)
		{
			c = Color.yellow;
			GetComponent<MeshRenderer>().sharedMaterial = myMaterials[2];
		}
		else if (nodeType == NodeType.Construction)
		{
			c = Color.green;
			GetComponent<MeshRenderer>().sharedMaterial = myMaterials[3];
		}
		else
		{
			//myMaterial.color = Color.white;
			GetComponent<MeshRenderer>().sharedMaterial = myMaterials[0];
		}

	}


	public static StationType GetStationFromRoom(CampRoomType input)
	{
		switch(input)
		{
			case CampRoomType.ArmorSmith:
				return StationType.Armor;
			case CampRoomType.Bar:
				return StationType.Bar;
			case CampRoomType.BedRoom:
				return StationType.Bed;
			case CampRoomType.Defense:
				return StationType.Guard;
			case CampRoomType.Farm:
				return StationType.Farming;
			case CampRoomType.Graveyard:
				return StationType.Grave;
			case CampRoomType.Hunter:
				return StationType.Hunting;
			case CampRoomType.Leader:
				return StationType.Leader;
			case CampRoomType.Medical:
				return StationType.Medical;
			case CampRoomType.Merchant:
				return StationType.Merchant;
			case CampRoomType.Arena:
				return StationType.Arena;
			case CampRoomType.RadioStation:
				return StationType.Radio;
			case CampRoomType.Smith:
				return StationType.Upgrades;
			case CampRoomType.WeaponSmith:
				return StationType.Weapons;
			case CampRoomType.Prison:
				return StationType.Prison;

			case CampRoomType.Generator:
				return StationType.Generator;
			case CampRoomType.WaterTreatment:
				return StationType.WaterTreatment;
			case CampRoomType.OreMining:
				return StationType.OreMining;
			case CampRoomType.Recreation:
				return StationType.Recreation;
			case CampRoomType.WorkerTraining:
				return StationType.WorkerTraining;
			case CampRoomType.TroopTraining:
				return StationType.TroopTraining;
			case CampRoomType.Kennel:
				return StationType.Kennel;
			case CampRoomType.Kitchen:
				return StationType.Kitchen;
			case CampRoomType.MerchantAnimals:
				return StationType.MerchantAnimals;
			case CampRoomType.MerchantSurvivors:
				return StationType.MerchantSurivors;

			default:
				return StationType.Any;
		}
	}

	public enum NodeType
	{
		Path = 0,
		Doorway = 1,
		Station = 2,
		Idle = 3,
		Construction = 4,
		Target = 5,
		BaseEntry = 6,
		PlayerSpot = 7,
	}
	public enum StationType
	{
		Farming = 0,
		Guard = 1,
		Hunting = 2,
		Medical = 3,
		Upgrades = 4,
		Armor = 5,
		Weapons = 6,
		Bar = 7,
		Bed = 8, // logic relevant
		Grave = 9, // logic relevant 
		Standing = 10, // logic relevant 
		Any = 11,
		Merchant = 12,
		Leader = 13,
		Radio = 14,
		Arena = 15,
		None = 16,
		Seat = 17, // logic relevant 
		Prison = 18,
		CampDog = 19,
		Generator = 20,
		WaterTreatment = 21,
		Recreation = 22,
		WorkerTraining = 23,
		TroopTraining = 24,
		Kennel = 25,
		Kitchen = 26,
		LeaderHelper = 27,
		Prisoner = 28,
		PlayerSpot = 29,
		Extra_1 = 30,
		MerchantAnimals = 31,
		MerchantSurivors = 32,
		SoldierBedroom = 33,
		MedicalBed = 34,
		OreMining = 35,

		DoorConferenceRoom = 100,
		DoorStaircase = 101,
	}
}

[System.Serializable]
public class BaseNavLink
{

	public BaseNavNode node;
	public int penalty;


}
