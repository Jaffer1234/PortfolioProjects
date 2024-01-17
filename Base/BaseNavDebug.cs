using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// this script is for the debug and edit function of the nav nodes in a base it is mainly used in the editor
/// </summary>
[ExecuteInEditMode]
public class BaseNavDebug : MonoBehaviour {

	public List<BaseNavNode> nodeList = new List<BaseNavNode>();

	
	// Update is called once per frame
	void Update () {
		GetComponentsInChildren<BaseNavNode>(nodeList);
	}

	private void OnRenderObject()
	{
		int c = nodeList.Count;
		for (int i = 0; i < c; i++)
		{
			nodeList[i].DebugDraw();
		}
	}

	//[ContextMenu("Restore")]
	public void Restore()
	{
		nodeList[0].GetComponent<MeshRenderer>().sharedMaterial.color = Color.white;

		/*
		int c = nodeList.Count;
		for (int i = 0; i < c; i++)
		{
			nodeList[i].DebugDraw();
		}
		*/
	}

}
