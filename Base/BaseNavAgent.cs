using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

/// <summary>
/// this script controlls the navigation of character models in a base scene. it can target a specific node type or cylce through available idle spots
/// </summary>
public class BaseNavAgent : MonoBehaviour {

	//public static List<BaseNavAgent> BaseNavController.activeController.Agents = new List<BaseNavAgent>();
	


	public BountyModel myModel;
	public int state = 0; // 0 = idle, 1 = searching, 2 = walking, 3 = waitung because way blocked, 4 = error, 5 = retry
	public int masterState = 0; // 0 = idle, 1 = at station, 2 = cycling idle nodes, 3 = cycling custom path targets
	public int walkMode = 1;
	public int prioMode = 0;
	public bool goToWork;
	public bool fastSkip;
	public int collisionReaction = 0;
	public bool teleportView;
	// runtime
	public System.Action<BaseNavAgent> onTargetReached;

	public NavAgentType agentType = NavAgentType.Normal;

	public BaseNavNode.NodeType targetType;
	public BaseNavNode.StationType targetStation;
	public CampRoomType preferedRoomType = CampRoomType.None;
	public BaseNavNode currentNode;
	public BaseNavNode targetNode;
	public BaseNavPathTarget[] customPathTargets;

	private List<BaseNavNode> path = new List<BaseNavNode>();
	private int pathIndex = 0;
	private BaseNavNode.StationType lastRandomStation = BaseNavNode.StationType.Any;
	private SphereCollider sphereCollider; // checks for other characters
	private SphereCollider myCollider; // my own hitbox
	private bool mainChar;
	private bool mayTeleport;
	private int collisionPriority;
	private int customPathTargetIndex; // on which entry in the list of custom path targets we are

	// die stations die sich gemerkt werden, damit er sie nicht doppelt nimmt
	public static List<BaseNavNode.StationType> choice = new List<BaseNavNode.StationType>() {
		//BaseNavNode.StationType.Grave,
		BaseNavNode.StationType.Bar,
		BaseNavNode.StationType.Bed,
		BaseNavNode.StationType.Seat,
	};

	//private static int tResult = 0;
	//private static BaseNavNode.StationType tNodeT = BaseNavNode.StationType.Any;
	private static BaseNavNode.StationType tNode2T = BaseNavNode.StationType.Any;


	public SphereCollider SphereCollider
	{
		get { return sphereCollider; }
	}
	public BaseNavNode.StationType LastRandomStation
	{
		get { return lastRandomStation; }
		set { lastRandomStation = value; }
	}

	// Use this for initialization
	void Start () {
		BaseNavController.activeController.Agents.Add(this);
		if (currentNode != null)
		{
			currentNode.occupied = true;
		}
		
		sphereCollider = gameObject.AddComponent<SphereCollider>();
		sphereCollider.isTrigger = true;
		sphereCollider.radius = 0.6f;
		sphereCollider.center = new Vector3(0f, 0f, 1.5f);
		sphereCollider.enabled = false;
		var rigi = gameObject.GetComponent<Rigidbody>();
		if(rigi == null)
			rigi = gameObject.AddComponent<Rigidbody>();
		rigi.isKinematic = true;
		rigi.useGravity = false;
		//rigi.detectCollisions = false;
		
		myCollider = gameObject.AddComponent<SphereCollider>();
		myCollider.radius = 0.7f;
		myCollider.enabled = false;
		
		if(myModel.myCharacter)
		{
			mainChar = myModel.myCharacter.mainCharacter;
		}
		collisionPriority = mainChar ? 50000 : SDRandom.Range(0, 1000);
	}

	// collisions during base walk
	//private void OnTriggerEnter(Collider col)
	//{
		
	//	if(col.GetComponent<BaseNavAgent>() && myModel.myAnimator.GetInteger("Walking") > 0)
	//	{
	//		Debug.Log("kollision zwischen: "+gameObject.name+" und "+col.gameObject.name);
	//		if (col.GetComponent<BaseNavAgent>().collisionReaction == 0)
	//		{
	//			collisionReaction = 1;
	//			myModel.myAnimator.SetInteger("Walking", 0);
	//			StartCoroutine(WaitForCollision());
	//		}
	//	}
	//}

	private void OnEnable()
	{
		if(targetNode == currentNode && path.Count == 0) // added 31.3.21
		{
			EngageNextNode();
		}
	}
	private void OnDisable()
	{
		StopAllCoroutines();
		if(path.Count > 0)
			state = 3;
		else
			state = 1;
	}
	private void OnDestroy()
	{
		if(BaseNavController.activeController != null)
			BaseNavController.activeController.Agents.Remove(this);
		if(currentNode)
			currentNode.occupied = false;
		
	}

	private void OnDrawGizmos()
	{
		if(sphereCollider != null)
			Gizmos.DrawWireSphere(transform.position + transform.TransformVector(sphereCollider.center), sphereCollider.radius);
	}


	void FixedUpdate()
	{

		ProcessNavigation();
		/*
		// collisions during base walk
		RaycastHit hit;
		if(collisionReaction == 0 && Physics.SphereCast(transform.position + transform.TransformVector(sphereCollider.center), sphereCollider.radius, transform.forward, out hit, 0.01f, LayerMask.GetMask("Combatant") ))
		{
			if (hit.transform.GetComponent<BaseNavAgent>() && myModel.myAnimator.GetInteger("Walking") > 0 && !mainChar)
			{
				BaseNavAgent otherBna = hit.transform.GetComponent<BaseNavAgent>();
				if (otherBna.collisionReaction == 0)
				{
					//if(otherBna.collisionPriority > collisionPriority)
					//{
					//	//Debug.Log("kollision zwischen: " + gameObject.name + " und " + hit.transform.gameObject.name);
					//	collisionReaction = 1;
					//	myModel.myAnimator.SetInteger("Walking", 0);
					//	StartCoroutine(WaitForCollision());
					//}
					//else
					//{
					//	otherBna.collisionReaction = 1;
					//	otherBna.myModel.myAnimator.SetInteger("Walking", 0);
					//	otherBna.StartCoroutine(hit.transform.GetComponent<BaseNavAgent>().WaitForCollision());
					//}
				}
			}
		}
		*/
	}

	private float TargetDot(BaseNavAgent a, BaseNavAgent b)
	{
		Vector3 vA = a.myModel.WalkTarget - a.transform.position;
		vA = Vector3.ProjectOnPlane(vA, Vector3.up);
		Vector3 vB = b.myModel.WalkTarget - b.transform.position;
		vB = Vector3.ProjectOnPlane(vB, Vector3.up);

		return Vector3.Dot(vA, vB);
	}

	/// <summary>
	/// called in fixed update, or when needed manually to kickstart the navigation calculations
	/// </summary>
	public void ProcessNavigation()
	{
		if (state == 1)
		{
			if (masterState == 1)
			{
				int tPrio = prioMode;
				if (targetStation == BaseNavNode.StationType.Bar && BountyManager.instance.InCamp)
				{
					tPrio = 1;
				}
				int tResult = Search(targetType, targetStation, currentNode == null, true/*goToWork*/, BaseNavNode.StationType.None, false, tPrio);
				if (tResult == 1)
				{
					if (goToWork && myModel.myCharacter != null)
					{
						List<BaseNavNode> list = BaseNavController.activeController.Nodes.FindAll(n => n.nodeType == targetType && (targetStation == BaseNavNode.StationType.Any || n.stationType == targetStation) && n.IsUnlocked());
						myModel.myCharacter.startNodeIndex = list.IndexOf(targetNode);
					}
					state = 3; // ready to go
					if (teleportView)
					{
						mayTeleport = true;
						TryTeleportView();
					}
					myCollider.enabled = true;
				}
				else if (tResult == 2)
				{
					state = 0; // already there
					 // enter the node
					//OnWalkFinished2();
					//EngageNextNode();
					OnNodeReached();
				}
				else
				{
					state = 4; // error
					targetNode = currentNode; // added 4.12.19

					if(goToWork)
					{
						masterState = 2;
						state = 1;
					}
				}
			}
			else if (masterState == 2) // cycle idles spots
			{
				tNode2T = BaseNavNode.StationType.None;
				if (choice.Contains(lastRandomStation))
				{
					tNode2T = lastRandomStation;
				}
				BaseNavNode.StationType tStation = BaseNavNode.StationType.Any;
				if(BountyManager.instance.InBase && BountyManager.instance.Night)
				{
					tStation = BaseNavNode.StationType.Bed;
					tNode2T = BaseNavNode.StationType.None;
				}
				if (preferedRoomType != CampRoomType.None)
					tNode2T = BaseNavNode.StationType.None;

				int check = Search(BaseNavNode.NodeType.Idle, tStation, true, true, tNode2T, true, 0, preferedRoomType);
				if (check == 1)
				{
					state = 3; // ready to go
					myCollider.enabled = true;
					lastRandomStation = targetNode.stationType;
				}
				else
				{
					check = Search(BaseNavNode.NodeType.Idle, BaseNavNode.StationType.Any, true, true, BaseNavNode.StationType.None, true);
					if (check == 1)
					{
						state = 3; // ready to go
						myCollider.enabled = true;
						lastRandomStation = targetNode.stationType;
					}
					else
					{
						Debug.LogFormat(gameObject, "{0} couldnt find a matching node when excluding type: {1} and is now waiting",gameObject.name, tNode2T);
						targetNode = currentNode;
						state = 4; // error
						StartCoroutine(NextNavDelay()); // retry
						lastRandomStation = BaseNavNode.StationType.None;
					}
				}

			}
			else if (masterState == 3) // cycle path spots
			{
				
				int check = CheckPath(customPathTargets[customPathTargetIndex].node);
				if (check == 1)
				{
					walkMode = customPathTargets[customPathTargetIndex].walkMode;
					state = 3; // ready to go
					myCollider.enabled = true;
					lastRandomStation = BaseNavNode.StationType.None;
				}
				else
				{
					targetNode = currentNode;
					state = 4; // error
					StartCoroutine(NextNavDelay()); // retry
					lastRandomStation = BaseNavNode.StationType.None;
				}
				// count up index
				customPathTargetIndex++;
				if (customPathTargetIndex >= customPathTargets.Length)
					customPathTargetIndex = 0;
			}
		}
		else if (state == 3/* && path.Count > 0*/)
		{
			//myModel.SetPathFindingTarget(targetNode.transform.position, 0.5f, OnArrivingNode, walkMode);
			OnLeavingNode();
			myModel.TogglePathFinding(true);
			targetNode.occupied = true;
			myModel.PathfindingBaseWalk(targetNode, OnArrivingNode, walkMode);
			//Debug.LogFormat("{0} goes to {1}", name, targetNode.name);
			state = 2;
			//EngageNextNode();
		}
		else if(state == 2 && mayTeleport)
		{
			TryTeleportView();
		}
	}

	/// <summary>
	/// tries to teleport the model to a nav point close the players viewport
	/// </summary>
	/// <returns></returns>
	private bool TryTeleportView()
	{
		BountyBase bb = !BountyManager.instance.InBase ? BountyManager.instance.campScene : BountyManager.instance.scenarioManager.CurrentScenario.baseData;
		
		if(bb.IsInsideView(myModel.transform.position))
		{
			mayTeleport = true;
			return false;
		}
		else
		{
			//if (mayTeleport && myModel.pathfinding.remainingDistance < 5f) // almost there no need to teleport
			//{
			//	mayTeleport = false;
			//	teleportView = false;
			//	return true;
			//}
			//else
			if (mayTeleport && bb.TeleportModelToViewBorder(myModel, targetNode.transform.position))
			{
				//BaseNavNode tempTargetNode = targetNode;
				//targetNode = null;

				//if (currentNode)
				//	currentNode.occupied = false;
				//currentNode = GetNearestNode();

				//Search(targetType, targetStation, currentNode == null, goToWork);

				//if (targetNode != null) // added 29.1.21
				//{
				//	Vector3 look = targetNode.transform.position - transform.position;
				//	if(path.Count > 1)
				//		look = path[1].transform.position - transform.position;
				//	transform.rotation = Quaternion.LookRotation(look, Vector3.up);
				//	myModel.myAnimator.rootRotation = transform.rotation;
				//}
				mayTeleport = false;
				teleportView = false;
				return true;
			}
			else
			{
				return false;
			}
		}
		
	}

	public void PlaceOnNode(bool useOffset = false)
	{
		Vector3 tPos = currentNode.transform.position;
		Quaternion tRot = currentNode.transform.rotation;
		if(useOffset)
		{
			tPos += transform.rotation * currentNode.skipPositionOffset;
			tRot *= Quaternion.Euler(currentNode.skipRotationOffset);
		}
		if (currentNode != null)
		{
			transform.position = tPos;
			myModel.myAnimator.rootPosition = transform.position;

			transform.rotation = tRot;
			myModel.myAnimator.rootRotation = transform.rotation;
			//Debug.LogFormat(gameObject, "model {0} gtes placed on its current node {1}", gameObject.name, currentNode.name);
		}
	}

	public BaseNavNode GetNearestNode()
	{
		BaseNavNode best = null;
		float min = float.MaxValue;
		float temp = 0f;
		List<BaseNavNode> nodes = new List<BaseNavNode>(BaseNavController.activeController.Nodes);
		if (agentType == NavAgentType.Normal)
			nodes.RemoveAll(n => n.stationType == BaseNavNode.StationType.CampDog); // this helps hunting stand user to restart at the correct point addeed 1.4.21
		int c = nodes.Count;
		for (int i = 0; i < c; i++)
		{
			temp = Vector3.Distance(transform.position, nodes[i].transform.position);
			if (temp < min)
			{
				min = temp;
				best = nodes[i];
			}

		}
		return best;
	}

	public void EngageNextNode()
	{
		if (pathIndex >= path.Count - 1) // goal reached
		{
			if (targetNode == null)
				return;
			//Debug.LogFormat(gameObject, "{0} reached goal nav node: {1}", gameObject.name, path[pathIndex].name);
			transform.position = targetNode.transform.position;
			//transform.rotation = targetNode.transform.rotation;
			// enter the node
			state = 0; // goal reached
			teleportView = false;
			bool idleData = true;
			if (masterState >= 2 && BountyManager.instance.InCamp && myModel.myCharacter != null && myModel.myCharacter.Faction == Faction.Player)
			{
				myModel.myCharacter.ShowState();
				idleData = !myModel.myCharacter.HasBedriddenState();
			}

			if (idleData)
				idleData = ApplyNodeSwitches(targetNode);
			if (masterState >= 2 && !idleData)
			{
				StartCoroutine(NextNavDelay());
			}
			
			if(myCollider != null)
				myCollider.enabled = false;
			
		}
		else
		{ // leave the node and walk to next

			if (path[pathIndex + 1].occupied)
			{
				//Debug.LogFormat(gameObject, "{0} has a blocked path trying to find new one", gameObject.name);
				if (currentNode == null)
					currentNode = GetNearestNode();
				if (targetNode == null)
					targetNode = path[path.Count - 1];

				List<BaseNavNode> checkPath = FindPath(currentNode, targetNode, true);
				if (checkPath.Count == 0)
				{
					checkPath = FindPath(currentNode, targetNode, false);
					if (checkPath.Count == 0)
					{
						//Debug.LogFormat(gameObject, "{0} couldnt find a new path waiting...", gameObject.name);
						myModel.myAnimator.SetInteger("Walking", 0);
						state = 4; // wait
						StartCoroutine(WaitForPath());
					}
				}
				else
				{
					//path = checkPath;
					//pathIndex = 0;
					//state = 3;
				}
			}
			//Debug.LogFormat(gameObject, "{0} leaves nav node: {1}", gameObject.name, path[pathIndex].name);
			if (path[pathIndex].idleData != null && path[pathIndex].idleData.Length > 0)
			{
				//myModel.CurrentIdleData = myModel.DefaultIdleData;
				myModel.CurrentIdleData = new BaseIdleData[0];
			}
			if (!string.IsNullOrEmpty(path[pathIndex].animationSwitch1))
			{
				myModel.myAnimator.SetBool(path[pathIndex].animationSwitch1, false);
			}
			if (!string.IsNullOrEmpty(path[pathIndex].animationSwitch2))
			{
				myModel.myAnimator.SetBool(path[pathIndex].animationSwitch2, false);
			}
			path[pathIndex].occupied = false;
			path[pathIndex + 1].occupied = true;

			state = 2; // walk
			
			if (pathIndex+1 >= path.Count - 1)
				myModel.LinearBaseWalk(path[pathIndex + 1], path[pathIndex + 1], OnWalkFinished, 0f, walkMode, true, OnWalkFinished2); // last node
			else
				myModel.LinearBaseWalk(path[pathIndex + 1], path[pathIndex + 2], OnWalkFinished, 0f, walkMode, false); // not last node

			pathIndex += 1;
			
			currentNode = path[pathIndex];
		}
	}

	public void OnLeavingNode()
	{
		if (currentNode.idleData != null && currentNode.idleData.Length > 0)
		{
			myModel.CurrentIdleData = new BaseIdleData[0];
		}
		if (!string.IsNullOrEmpty(currentNode.animationSwitch1))
		{
            myModel.myAnimator.SetBool(currentNode.animationSwitch1, false);

			// Adding delay to animation before when changing clothes
			if(currentNode.updateModelBuild)
			{
                //Change Clothes back after standing up
                StartCoroutine(DelayChangeClothes(4.5f));
            }
        }
		if (!string.IsNullOrEmpty(currentNode.animationSwitch2))
		{
			myModel.myAnimator.SetBool(currentNode.animationSwitch2, false);

            // Adding delay to animation before when changing clothes
            if (currentNode.updateModelBuild)
            {
                //Change Clothes back after standing up
                StartCoroutine(DelayChangeClothes(4.5f));
            }
        }
		currentNode.occupied = false;
	}

	public void OnArrivingNode()
	{
		// match roation and placement
		StartCoroutine(ArrivalRoutine());
	}

	private IEnumerator ArrivalRoutine()
	{
		float delta = 0f;
		Quaternion startRot = transform.rotation;
		Quaternion targetRot = targetNode.transform.rotation;
		Vector3 startPos = transform.position;
		Vector3 targetPos = targetNode.transform.position;
		while(delta < 1f)
		{
			delta += Time.fixedDeltaTime;
			transform.position = Vector3.Lerp(startPos, targetPos, delta);
			transform.rotation = Quaternion.Lerp(startRot, targetRot, delta);

			yield return new WaitForFixedUpdate();
		}
		OnNodeReached();
	}

	public void OnNodeReached()
	{
		if (targetNode == null)
			return;

		currentNode = targetNode;
		//Debug.LogFormat(gameObject, "{0} reached goal nav node: {1}", gameObject.name, path[pathIndex].name);
		transform.position = targetNode.transform.position;
		myModel.TogglePathFinding(false);
		//transform.rotation = targetNode.transform.rotation;
		// enter the node
		state = 0; // goal reached
		teleportView = false;
		bool idleData = true;
		if (masterState >= 2 && BountyManager.instance.InCamp && myModel.myCharacter != null && myModel.myCharacter.Faction == Faction.Player)
		{
			myModel.myCharacter.ShowState();
			idleData = !myModel.myCharacter.HasBedriddenState();
		}

		if (idleData)
			idleData = ApplyNodeSwitches(targetNode);
		if (masterState >= 2 && !idleData)
		{
			StartCoroutine(NextNavDelay());
		}

		OnWalkFinished2();
	}

	private void OnWalkFinished()
	{
		if (teleportView)
		{
			mayTeleport = true;
			TryTeleportView();
		}

		//state = 3;
		EngageNextNode(); // next node
		
	}
	// important for dialogue triggering in a base just before the walk animation is finished
	private void OnWalkFinished2()
	{
		if (onTargetReached != null)
			onTargetReached(this);

		if (masterState == 1 && myModel.myCharacter != null)
		{

			BountyManager.instance.eventManager.CheckTrigger(SDTriggerType.GameEvent, new object[] { (int)BountyGameEvent.CharacterBaseNavArrived, myModel.myCharacter.uniqueId });
		}

	}

	/// <summary>
	/// fires possible triggers and animation switches when arriving at a new node
	/// </summary>
	/// <param name="node"></param>
	/// <returns>returns true if the node has idle data array</returns>
	private bool ApplyNodeSwitches(BaseNavNode node)
	{
		if(node == null)
		{
			return false;
		}

		// spezialfall angriffskampf am eingang
		if (BountyManager.instance.InBase && BountyManager.instance.scenarioManager.CurrentScenario.baseData.EntranceFight)
		{
			myModel.RotateToCombat = BountyManager.instance.scenarioManager.CurrentScenario.combatPositions[0];
			myModel.CurrentIdleData = BountyManager.instance.scenarioManager.CurrentScenario.baseData.combatViewerIdleData;
			
			return true;
		}

        walkMode = 1;
		
		bool result = false;
		myModel.DefaultRotation = node.transform.rotation.eulerAngles; // added because it may help when a model returns to normal after combat no matter where it arrives while combat is running

		if (node.idleData != null && node.idleData.Length > 0)
		{
			myModel.FastSkip = fastSkip;
			myModel.CurrentIdleData = node.idleData;
			result = true;
		}

        // If the Clothes need to update too, execute the animation with a delay
        if (node.updateModelBuild)
        {
            myModel.UpdateClothes();

            if ((node.nodeType == BaseNavNode.NodeType.Station /*&& (goToWork || BountyManager.instance.InBase || BountyManager.instance.CurrentTutorialIndex > 0) */) || node.nodeType == BaseNavNode.NodeType.Idle || node.nodeType == BaseNavNode.NodeType.Construction)
            {
                if (!fastSkip)
                    myModel.Rotate(node.transform.rotation, 0.5f);
                if (!string.IsNullOrEmpty(node.animationSwitch1))
                {
                    StartCoroutine(DelayAnimation(node.animationSwitch1, true, 1f));
                }
                if (!string.IsNullOrEmpty(node.animationSwitch2))
                {
                    StartCoroutine(DelayAnimation(node.animationSwitch2, true, 1f));
                }
                if (!string.IsNullOrEmpty(node.animationTrigger))
                {
                    StartCoroutine(DelayAnimation(node.animationTrigger, 1f));
                }
                if (!string.IsNullOrEmpty(node.animationSwitchToggle))
                {
                    StartCoroutine(DelayAnimation(node.animationSwitchToggle, node.animationToggleValue, 1f));
                }
                if (!string.IsNullOrEmpty(node.animationSwitchInteger))
                {
                    StartCoroutine(DelayAnimation(node.animationSwitchInteger, node.animationIntegerValue, 1f));
                }
            }
        }
		else
		{
            if ((node.nodeType == BaseNavNode.NodeType.Station /*&& (goToWork || BountyManager.instance.InBase || BountyManager.instance.CurrentTutorialIndex > 0) */) || node.nodeType == BaseNavNode.NodeType.Idle || node.nodeType == BaseNavNode.NodeType.Construction||(node.stationType==BaseNavNode.StationType.Bar&&node.nodeType==BaseNavNode.NodeType.Target))
            {
                if (!fastSkip)
                    myModel.Rotate(node.transform.rotation, 0.5f);
                if (!string.IsNullOrEmpty(node.animationSwitch1))
                {
                    myModel.myAnimator.SetBool(node.animationSwitch1, true);
                }
                if (!string.IsNullOrEmpty(node.animationSwitch2))
                {
                    myModel.myAnimator.SetBool(node.animationSwitch2, true);
                }
                if (!string.IsNullOrEmpty(node.animationTrigger))
                {
                    myModel.myAnimator.SetTrigger(node.animationTrigger);
                }
                if (!string.IsNullOrEmpty(node.animationSwitchToggle))
                {
                    myModel.myAnimator.SetBool(node.animationSwitchToggle, node.animationToggleValue);
                }
                if (!string.IsNullOrEmpty(node.animationSwitchInteger))
                {
                    myModel.myAnimator.SetInteger(node.animationSwitchInteger, node.animationIntegerValue);
                }
            }
        }       
		
		goToWork = false;
		if (fastSkip)
		{
			myModel.myAnimator.applyRootMotion = false;
			myModel.transform.position = node.transform.position + node.transform.rotation * node.skipPositionOffset;
			myModel.transform.rotation = node.transform.rotation * Quaternion.Euler(node.skipRotationOffset);
			myModel.myAnimator.rootPosition = myModel.transform.position;
			myModel.myAnimator.rootRotation = myModel.transform.rotation;
			//myModel.myAnimator.SetTrigger("Reset");
			myModel.myAnimator.SetTrigger("SkipFast");
			StartCoroutine(DelayedRootMotionRoutine());

			fastSkip = false;
		}

		return result;
	}

	// Delays Animation in seconds (SetBool)
	private IEnumerator DelayAnimation(string node, bool check,float amount)
	{
		yield return new WaitForSeconds(amount);
		myModel.myAnimator.SetBool(node, check);
    }
    // Delays Animation in seconds (SetInteger)
    private IEnumerator DelayAnimation(string node, int num, float amount)
    {
        yield return new WaitForSeconds(amount);
        myModel.myAnimator.SetInteger(node, num);
    }
    // Delays Animation in seconds (SetTrigger)
    private IEnumerator DelayAnimation(string node, float amount)
    {
        yield return new WaitForSeconds(amount);
        myModel.myAnimator.SetTrigger(node);
    }


	// Delay the UpdateClothes Animation by 'amount'
    private IEnumerator DelayChangeClothes(float amount)
	{
		yield return new WaitForSeconds(amount);
        myModel.UpdateClothes();
    }


	private IEnumerator DelayedRootMotionRoutine()
	{
		yield return new WaitForSeconds(0.2f);
		myModel.myAnimator.applyRootMotion = true;
	}


	/// <summary>
	/// searches a given node
	/// </summary>
	/// <param name="nt"></param>
	/// <param name="st"></param>
	/// <param name="excludeCurrentNode"></param>
	/// <param name="usePriority">0=not, 1=high prio, -1=low prio</param>
	/// <returns>0=failure, 1=success, 2=already at target</returns>
	public int Search(BaseNavNode.NodeType nt, BaseNavNode.StationType st, bool excludeCurrentNode = false, bool excludeLockedNodes = true, BaseNavNode.StationType exclSta = BaseNavNode.StationType.None, bool chooseThree = false, int usePriority = 0, CampRoomType pPreferedRoomType = CampRoomType.None)
	{
		//Debug.Log(gameObject.name+" searches target node: "+nt.ToString()+ " - "+st.ToString());
		//myModel.SpecialDebugLog(string.Format("{0} searches target node: {1} / {2}", gameObject.name, nt.ToString(), st.ToString()));
		
		List<BaseNavNode> ocuppied = new List<BaseNavNode>();
		// moved the agent occuping section below the special cases 17.8.2020,  15.11.22 -> mabye move it up again? we reserve quest giver nodes and then remove nodes targeted by quest givers, but the first reservation is compeltely random and my only partly intersect the actual requred reservation

		if (exclSta != BaseNavNode.StationType.None)
		{
			ocuppied.AddRange(BaseNavController.activeController.Nodes.FindAll(n => n.stationType == exclSta && !ocuppied.Contains(n)));
		}
		if(agentType == NavAgentType.CampDog)
		{
			ocuppied.AddRange(BaseNavController.activeController.Nodes.FindAll(n => n.nodeType == BaseNavNode.NodeType.Idle && n.stationType != BaseNavNode.StationType.CampDog && !ocuppied.Contains(n)));
		}
		else
		{
			ocuppied.AddRange(BaseNavController.activeController.Nodes.FindAll(n => n.nodeType == BaseNavNode.NodeType.Idle && n.stationType == BaseNavNode.StationType.CampDog && !ocuppied.Contains(n)));
		}
		if (pPreferedRoomType != CampRoomType.None)
		{
			ocuppied.AddRange(BaseNavController.activeController.Nodes.FindAll(n => n.roomType != pPreferedRoomType && !ocuppied.Contains(n)));
		}

		
		// in base: bed nodes need to be reserved for infected people and also for the player
		if (BountyManager.instance.InCamp && myModel.myCharacter != null)
		{
			if(!myModel.myCharacter.HasBedriddenState())
			{
				List<BaseNavNode> test = BaseNavController.activeController.Nodes.FindAll(n => (n.stationType == BaseNavNode.StationType.Bed || n.stationType == BaseNavNode.StationType.MedicalBed) && n.nodeType == BaseNavNode.NodeType.Station && !ocuppied.Contains(n));
				test.Sort((a, b) => a.priority.CompareTo(b.priority)); // sort by prority asc
				int tMax = Mathf.Min(BountyManager.instance.camp.GetAllBedriddenPeople().Count, test.Count);
				for(int i = 0; i < tMax; i++)
				{
					ocuppied.Add(test[i]);
				}
				if(!myModel.myCharacter.mainCharacter && test.Count > 0)
				{
					ocuppied.Add(test[test.Count-1]);
				}
			}
			else
			{
				// if not main char dont go to the player reserved bed
				List<BaseNavNode> test = BaseNavController.activeController.Nodes.FindAll(n => n.stationType == BaseNavNode.StationType.Bed && n.nodeType == BaseNavNode.NodeType.Station && !ocuppied.Contains(n));
				test.Sort((a, b) => a.priority.CompareTo(b.priority)); // sort by prority asc
				if (!myModel.myCharacter.mainCharacter && test.Count > 0)
				{
					ocuppied.Add(test[test.Count - 1]);
				}
			}
		}

		// in base: barhocker nodes need to be reserved for quest people
		if (BountyManager.instance.InCamp && myModel.myCharacter != null)
		{
			if (!myModel.myCharacter.IsQuestGiver)
			{
				List<BaseNavNode> test = BaseNavController.activeController.Nodes.FindAll(n => n.stationType == BaseNavNode.StationType.Bar && n.nodeType == BaseNavNode.NodeType.Idle && !ocuppied.Contains(n));
				List<BountyCharacter> test2 = BountyManager.instance.camp.GetAllCampPeople(false).FindAll(n => n.IsQuestGiver && n.Model && !test.Contains(n.Model.myNavAgent.targetNode));
				int tMax = Mathf.Min(test2.Count + 2, test.Count);
				for (int i = 0; i < tMax; i++)
				{
					ocuppied.Add(test[i]);
				}
			}
		}


		// now exclude all allready taken nodes
		foreach (var aa in BaseNavController.activeController.Agents)
		{
			if (aa != this && aa.targetNode != null && !ocuppied.Contains(aa.targetNode))
				ocuppied.Add(aa.targetNode);
		}

		List<BaseNavNode> goals;
		// first try
		goals = BaseNavController.activeController.Nodes.FindAll(n => n.nodeType == nt && (st == BaseNavNode.StationType.Any || n.stationType == st) && !(excludeLockedNodes && !n.IsUnlocked()) && !ocuppied.Contains(n) && !(excludeCurrentNode && (n == currentNode || (targetNode != null && n.roomType == targetNode.roomType))));
		if (goals.Count == 0)
		{
			// second try
			goals = BaseNavController.activeController.Nodes.FindAll(n => n.nodeType == nt && (st == BaseNavNode.StationType.Any || n.stationType == st) && !(excludeLockedNodes && !n.IsUnlocked()) && !ocuppied.Contains(n));
		}

		//myModel.SpecialDebugLog("goals:"+goals.Count);
		if (goals.Count == 0)
			return 0;

		BaseNavNode best = null;

		//goals.Sort((a, b) => HeuristicDistance(currentNode, a).CompareTo(HeuristicDistance(currentNode, b)));
		List<BaseNavNode> temp = new List<BaseNavNode>();
		int c = goals.Count;
		int j;
		for (int i = 0; i < c; i++) // insertion sort into new list
		{
			j = i;
			while (j > 0 && CompareNodes(goals[i], goals[j - 1], usePriority)) // sort by distance asc and priority desc
			{
				j--;
			}
			if (j == i)
				temp.Add(goals[i]);
			else
				temp.Insert(j, goals[i]);
		}

		if (!chooseThree)
		{
			// take nearest node 
			//float min = float.MaxValue;
			//float temp = 0f;
			//for (int i = 0; i < goals.Count; i++)
			//{
			//	temp = HeuristicDistance(currentNode, goals[i]);
			//	if (temp < min)
			//	{
			//		min = temp;
			//		best = goals[i];
			//	}
			//}

			best = temp[0];
		}
		else
		{
			// take 3 nearsest node 
			
			best = temp[myModel.AgentRng.GetRange(0, Mathf.Min(goals.Count, 3))];
		}
		// already there
		if (currentNode == best)
		{
			targetNode = best;
			pathIndex = 0;
			path.Clear();
			return 2;
		}

		if(best != null)
		{
			targetNode = best; ;
			return 1;
		}

		/* pathfinding is now handled externally
		
		path = FindPath(currentNode, best, false);
		
		if(path.Count > 0)
		{
			//Debug.LogFormat(gameObject, "{0} has new target node: {1} ", gameObject.name, best.name);
			targetNode = best;
			pathIndex = 0;
			return 1;
		}
		*/

		//myModel.SpecialDebugLog(string.Format("{0} has no path found to: {1} ", gameObject.name, best.name));
		//Debug.Break();

		return 0;
		
	}

	private IEnumerator NextNavDelay()
	{
		//myModel.myAnimator.SetInteger("IdleIndex", AgentRng.Range(1, 6));
		float waiting = myModel.AgentRng.GetRange(3f, 7f);
		yield return BountyExtensions.WaitForFixedFrameSeconds(waiting);
		yield return new WaitForSeconds(waiting);
		//myModel.myAnimator.SetInteger("IdleIndex", 0);

		state = 1;
	}

	private IEnumerator WaitForPath()
	{
		List<BaseNavNode> nextPath = new List<BaseNavNode>();
		float waiting = myModel.AgentRng.GetRange(10f, 20f) / 10f;
		//Debug.LogFormat(gameObject, "Random Wait: {0} sec for: {1}", waiting, gameObject.name);
		while (nextPath.Count == 0)
		{
			//yield return new WaitForSeconds(waiting);
			yield return BountyExtensions.WaitForFixedFrameSeconds(waiting);
			nextPath = FindPath(currentNode, targetNode, true);
			//Debug.LogFormat(gameObject, "{0} has now path: {1}", gameObject.name, path);
		}
		//Debug.LogFormat(gameObject, "{0} stopped waiting", gameObject.name);
		path = nextPath;
		state = 3;
	}
	private IEnumerator WaitForCollision()
	{
		float waiting = myModel.AgentRng.GetRange(15f, 35f) / 10f;
		//Debug.LogFormat(gameObject, "Random Wait: {0} sec for: {1}", waiting, gameObject.name);
		//yield return new WaitForSeconds(waiting);
		yield return BountyExtensions.WaitForFixedFrameSeconds(waiting);
		if (path.Count > 0 && state != 0 && SphereCollider.enabled)
		{
			//Debug.LogFormat(gameObject, "collision wait for {0} is over now walking again", gameObject.name);
			myModel.myAnimator.SetInteger("Walking", 1);
		}

		collisionReaction = 0;
	}

	#region pathfinding
	public int CheckPath(BaseNavNode target)
	{
		path = FindPath(currentNode, target, false);

		if (path.Count > 0)
		{
			//Debug.LogFormat(gameObject, "{0} has new target node: {1} ", gameObject.name, best.name);
			targetNode = target;
			pathIndex = 0;
			return 1;
		}
		//myModel.SpecialDebugLog(string.Format("{0} has no path found to: {1} ", gameObject.name, best.name));
		//Debug.Break();
		return 0;
	}

	// compares 2 nodes for the insertion sort and returns true if node a should descend compared to b
	private bool CompareNodes(BaseNavNode a, BaseNavNode b, int usePriority)
	{
		bool result = false;

		if(usePriority != 0 && a.priority != b.priority)
		{
			if (usePriority > 0)
				result = a.priority > b.priority;
			else if (usePriority < 0)
				result = a.priority < b.priority;
		}
		else
		{
			result = HeuristicDistance(currentNode, a) < HeuristicDistance(currentNode, b);
		}
		return result;
	}

	public List<BaseNavNode> FindPath(BaseNavNode start, BaseNavNode goal, bool occupiedBlocksPath)
	{
		/*
		List<BaseNavNode> open = new List<BaseNavNode>();
		List<BaseNavNode> closed = new List<BaseNavNode>();
		Dictionary<BaseNavNode, float> gScores = new Dictionary<BaseNavNode, float>();
		Dictionary<BaseNavNode, float> fScores = new Dictionary<BaseNavNode, float>();
		Dictionary<BaseNavNode, BaseNavNode> cameFrom = new Dictionary<BaseNavNode, BaseNavNode>();
		BaseNavNode current;
		float tent_gScore = 0f;

		// add first node
		open.Add(start);
		gScores.Add(start, 0);
		fScores.Add(start, HeuristicDistance(start, goal));

		// go through all unchecked points
		while (open.Count > 0)
		{
			current = GetMin(open, fScores);
			if(current == goal) // found target
			{
				return ReconstructPath(cameFrom, current, true);
			}

			open.Remove(current);
			closed.Add(current);

			for(int i = 0; i < current.connections.Length; i++)
			{
				if (closed.Contains(current.connections[i])) // already evaluated
					continue;

				if (occupiedBlocksPath && current.connections[i].occupied) // blocked
				{
					closed.Add(current.connections[i]);
					continue;
				}

				tent_gScore = gScores[current] + HeuristicDistance(current.connections[i], current); // add gScore aka distance aka hop count?

				if(!open.Contains(current.connections[i])) // discovered new node
				{
					open.Add(current.connections[i]);
				}
				else if(tent_gScore >= gScores[current.connections[i]]) // not a better path
				{
					continue;
				}

				// this path is the best at the moment, save it
				if (cameFrom.ContainsKey(current.connections[i]))
				{
					cameFrom[current.connections[i]] = current;
					gScores[current.connections[i]] = tent_gScore;
					fScores[current.connections[i]] = tent_gScore + HeuristicDistance(current.connections[i], goal);

					//if (UnityEditor.Selection.activeObject == gameObject)
					//	current.connections[i].myScore = fScores[current.connections[i]];
				}
				else
				{
					cameFrom.Add(current.connections[i], current);
					gScores.Add(current.connections[i], tent_gScore);
					fScores.Add(current.connections[i], tent_gScore + HeuristicDistance(current.connections[i], goal));

					//if (UnityEditor.Selection.activeObject == gameObject)
					//	current.connections[i].myScore = fScores[current.connections[i]];
				}
			}
		}
		*/
		return new List<BaseNavNode>();
	}

	// start at goal and find the closest way to the start
	public List<BaseNavNode> FindPath2(BaseNavNode start, BaseNavNode goal, bool occupiedBlocksPath)
	{
		/*
		List<BaseNavNode> open = new List<BaseNavNode>();
		List<BaseNavNode> closed = new List<BaseNavNode>();
		Dictionary<BaseNavNode, float> gScores = new Dictionary<BaseNavNode, float>();
		Dictionary<BaseNavNode, float> fScores = new Dictionary<BaseNavNode, float>();
		Dictionary<BaseNavNode, int> hopScores = new Dictionary<BaseNavNode, int>();
		Dictionary<BaseNavNode, BaseNavNode> cameFrom = new Dictionary<BaseNavNode, BaseNavNode>();
		BaseNavNode current;
		float tent_gScore = 0f;
		float basisDistance = HeuristicDistance(start, goal);
		int tent_hopScore = 0;

		// pre calc hop scores
		open.Add(goal);
		hopScores.Add(goal, 0);
		while (open.Count > 0)
		{
			current = GetMin(open, hopScores);

			open.Remove(current);
			closed.Add(current);

			for (int i = 0; i < current.connections.Length; i++)
			{
				if (closed.Contains(current.connections[i])) // already evaluated
					continue;


				tent_hopScore = hopScores[current] + 1; // add gScore hop count

				if (!open.Contains(current.connections[i])) // discovered new node
				{
					open.Add(current.connections[i]);
				}

				// this path is the best at the moment, save it
				if (cameFrom.ContainsKey(current.connections[i]))
				{
					cameFrom[current.connections[i]] = current;
					hopScores[current.connections[i]] = tent_hopScore;
				}
				else
				{
					cameFrom.Add(current.connections[i], current);
					hopScores.Add(current.connections[i], tent_hopScore);
				}
			}
		}
		open.Clear();
		closed.Clear();
		cameFrom.Clear();

		// add first node
		open.Add(start);
		gScores.Add(start, 0);
		fScores.Add(start, HeuristicDistance2(start, goal, basisDistance));

		// go through all unchecked points
		while (open.Count > 0)
		{
			current = GetMin(open, fScores);
			if (current == goal) // found target
			{
				return ReconstructPath(cameFrom, current, true);
			}

			open.Remove(current);
			closed.Add(current);

			for (int i = 0; i < current.connections.Length; i++)
			{
				if (closed.Contains(current.connections[i])) // already evaluated
					continue;

				if (occupiedBlocksPath && current.connections[i].occupied) // blocked
				{
					closed.Add(current.connections[i]);
					continue;
				}

				tent_gScore = hopScores[current]; // add gScore aka distance aka hop count?

				if (!open.Contains(current.connections[i])) // discovered new node
				{
					open.Add(current.connections[i]);
				}
				else if (tent_gScore >= gScores[current.connections[i]]) // not a better path
				{
					continue;
				}

				// this path is the best at the moment, save it
				if (cameFrom.ContainsKey(current.connections[i]))
				{
					cameFrom[current.connections[i]] = current;
					gScores[current.connections[i]] = tent_gScore;
					fScores[current.connections[i]] = tent_gScore;// + HeuristicDistance2(current.connections[i], goal, basisDistance);
					//if(UnityEditor.Selection.activeObject == gameObject)
					//	current.connections[i].myScore = fScores[current.connections[i]];
				}
				else
				{
					cameFrom.Add(current.connections[i], current);
					gScores.Add(current.connections[i], tent_gScore);
					fScores.Add(current.connections[i], tent_gScore);// + HeuristicDistance2(current.connections[i], goal, basisDistance));
					//if (UnityEditor.Selection.activeObject == gameObject)
					//	current.connections[i].myScore = fScores[current.connections[i]];
				}
			}
		}
		*/
		return new List<BaseNavNode>();
	}

	private List<BaseNavNode> ReconstructPath(Dictionary<BaseNavNode, BaseNavNode> cameFrom, BaseNavNode current, bool reverse)
	{
		List<BaseNavNode> result = new List<BaseNavNode>();
		//float sum = 0f;
		result.Add(current);

		while(cameFrom.ContainsKey(current))
		{
			current = cameFrom[current];
			result.Add(current);
		}
		if(reverse)
			result.Reverse();
		return result;

		/*
		return new BaseNavPath()
		{
			cost = sum,
			nodes = result
		};
		*/
	}

	private float HeuristicDistance(BaseNavNode a, BaseNavNode b)
	{
		// simple direct line metric but it creates some uneccessary cuves in a path that could be more straight
		return (b.transform.position - a.transform.position).magnitude;
		//return Vector3.Distance(a.transform.position, b.transform.position);

	}
	private float HeuristicDistance2(BaseNavNode a, BaseNavNode b, float basis)
	{
		// linear distance ratio to the start-goal distance
		return (b.transform.position - a.transform.position).magnitude / basis;
		

	}

	private BaseNavNode GetMin(List<BaseNavNode> set, Dictionary<BaseNavNode, float> values)
	{
		int count = set.Count;
		float min = float.MaxValue;
		BaseNavNode best = null;
		for (int i = 0; i < count; i++)
		{
			if(values.ContainsKey(set[i]) && values[set[i]] < min)
			{
				min = values[set[i]];
				best = set[i];
			}
		}
		return best;
	}
	private BaseNavNode GetMin(List<BaseNavNode> set, Dictionary<BaseNavNode, int> values)
	{
		int count = set.Count;
		int min = int.MaxValue;
		BaseNavNode best = null;
		for (int i = 0; i < count; i++)
		{
			if (values.ContainsKey(set[i]) && values[set[i]] < min)
			{
				min = values[set[i]];
				best = set[i];
			}
		}
		return best;
	}
#endregion

}

public enum NavAgentType
{
	Normal = (1 << 0),
	CampDog = (1 << 1),

}

[System.Serializable]
public class BaseNavPathTarget
{
	public int walkMode;
	public BaseNavNode node;
}