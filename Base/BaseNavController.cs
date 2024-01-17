using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// stores information about the navigation nodes in base
/// </summary>
public class BaseNavController : MonoBehaviour {

	[SerializeField]
	public List<BountyModel> models;
	[SerializeField]
	private BaseNavRoomEntry[] rooms;
	[SerializeField]
	private BaseNavNode[] barRoomSlots;

	private List<BaseNavNode> nodes;
	private List<BaseNavAgent> agents = new List<BaseNavAgent>();

	public List<BaseNavNode> Nodes
	{
		get { return nodes; }
	}
	public List<BaseNavAgent> Agents
	{
		get { return agents; }
	}

	public static BaseNavController activeController;

	private void Start()
	{
		// nodes = new List<BaseNavNode>(GetComponentsInChildren<BaseNavNode>(false));
		// nodes.RemoveAll(n => !n.nodeEnabled);
		

        UpdateNodes();
	}
	private void OnEnable()
	{
		activeController = this;
		
		
	//	Invoke("DebugCharacters", 1f);
        UpdateNodes();
	}
    private void FixedUpdate()
    {
		
		if (BountyManager.instance.DateTime <= 2)
		{
			if (FoundModel)
			{

				for (int i = 0; i < agents.Count; i++)
				{
					if (agents[i].name == "Trish(Clone)")
					{

						modelTrish = agents[i].transform.GetComponent<BaseNavAgent>();
						modelTrish.walkMode = 0;
						ingameDate = true;
						FoundModel = false;

                       

					}
				}
			}

            if (ingameDate)
			{
				modelTrish.targetNode = modelTrish.currentNode;
			}
		}
	}
   
    private BaseNavAgent modelTrish;
	bool ingameDate = false;
	bool FoundModel = true;
    void DebugCharacters()
	{
       
		for (int i = 0; i < models.Count; i++)
		{
			if (models[i].name == "Trish(Clone)")
			{
               
					modelTrish = models[i].transform.GetComponent<BaseNavAgent>();
				modelTrish.walkMode = 0;
				ingameDate = true;
		Debug.LogError(ingameDate + "Ingame");
		
            }
		}
    }
	//private void OnDisable()
	//{
 //       if (BountyManager.instance.DateTime <= 2)
 //       {
	//		ingameDate = true;
 //       }
 //   }
	public void UpdateNodes()
	{
		Scenario scen = GetComponentInParent<Scenario>(true);
		nodes = new List<BaseNavNode>(scen.GetComponentsInChildren<BaseNavNode>(false));
		nodes.RemoveAll(n => !n.nodeEnabled);
		if(BountyManager.instance.CurrentTutorialIndex <= 0)
		{
			nodes.RemoveAll(n => n.tutorialOnly);
		}
		//}
	}

	/// <summary>
	/// takes all room states of the base and updates the nodes in the scene accordingly
	/// </summary>
	/// <param name="input"></param>
	public void UpdateNodes(List<CampRoomEntry> input)
	{

	}

	public void AddModel(BountyModel m)
	{
		if(!models.Contains(m))
		{
			models.Add(m);
		}
        models.RemoveAll(model => model == null || model.gameObject == null);
    }
	public void RemoveModel(BountyModel m)
	{
		if (models.Contains(m))
		{
			models.Remove(m);
		}
	}
	public BountyModel GetModelByCharacter(BountyCharacter c)
	{
		return models.Find(n => n.myCharacter == c);
	}
	public List<BountyModel> GetAllModels()
	{
		return new List<BountyModel>(models);
	}
	public void KeepModels(List<BountyModel> m)
	{

	}

	public BountyModel GetModelOnNode(BaseNavNode node, bool includeTargetNode = true)
	{
		foreach (var agent in agents)
		{
			if(agent.currentNode == node || (includeTargetNode && agent.targetNode == node))
			{
				return agent.myModel;
			}
		}
		return null;
	}
	public List<BountyCharacter> GetBarSlotChars()
	{
		List<BountyCharacter> result = new List<BountyCharacter>();
		BountyModel bm;
		foreach (var slot in barRoomSlots)
		{
			bm = GetModelOnNode(slot);
			if (bm != null && bm.myCharacter != null)
				result.Add(bm.myCharacter);
			else
				result.Add(null);
		}

		return result;
	}
}

[System.Serializable]
public class BaseNavRoomEntry
{
	public CampRoomType roomType;
	public BaseNavNode[] nodes;

}
