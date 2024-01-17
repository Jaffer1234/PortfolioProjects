using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// this is planned as a helper script to make nav nodes part of 3d prefabs that can be placed in the scene by artists. the nav node would already correctly positioned.
/// the problem is that a chair prefab with a sitting animation, doesnt know which room/station it is in, which makes the identification of the node by the system very hard.
/// therefor the node and its properties must exist by themselves and only it's position in 3D space is controlled by this script inside the model prefab in question eg. a chair
/// </summary>
public class BaseNavObjectInteraction : MonoBehaviour
{

    public Transform navNodePosition;

#if UNITY_EDITOR

	private void OnDrawGizmos()
	{
        if(navNodePosition != null)
           Handles.PositionHandle(navNodePosition.position, navNodePosition.rotation);
	}

#endif

}
