using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "SDObjects/DataBase/CharacterDatabase")]
public class CharacterDatabase : ScriptableObject {

	[SerializeField]
	private List<ResourceListEntry> assetList;

	[SerializeField]
	[HideInInspector]
	private int idCount;
	public int FetchNextId()
	{
		return idCount++;
	}

	private int pendingCachings = -1;
	private Dictionary<string, BountyCharacter> cachedEventList; // runtime relevant
	public System.Action onCacheCompleted; // callback, fired when event caching is completed

	public int PendingCaches
	{
		get { return pendingCachings; }
	}

	/// <summary>
	/// returns the cached entry of the prefab (not instantiated)
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	public BountyCharacter GetCachedCharacter(string id)
	{
		if (string.IsNullOrEmpty(id) || pendingCachings != 0 || !cachedEventList.ContainsKey(id))
			return null;

		return cachedEventList[id];
	}

	// lädt alle einträge einmal in den speicher
	public void CacheEntries()
	{
		cachedEventList = new Dictionary<string, BountyCharacter>();
		pendingCachings = assetList.Count;
		for (int i = pendingCachings - 1; i >= 0; i--)
		{
			Request(assetList[i]);
		}

	}
	private void Request(ResourceListEntry rle)
	{
		AsyncOperation rr = SDResources.LoadAsync<BountyCharacter>("Character/" + rle.resourceName);
		//Debug.Log(rle.resourceName + "rle.resourceName");
		rr.completed += (context) => RequestFinish(context, rle.resourceName);
	}
	private void RequestFinish(AsyncOperation op, string strId)
	{
#if USE_BUNDLES
		AssetBundleRequest request = (AssetBundleRequest)op;
		BountyCharacter le = ((BountyCharacter)request.asset);//.GetComponent<BountyCharacter>();
#else
		ResourceRequest request = (ResourceRequest)op;
		BountyCharacter le = (BountyCharacter)request.asset;
#endif
		cachedEventList.Add(strId, le);
		pendingCachings -= 1;

		if (pendingCachings == 0 && onCacheCompleted != null)
		{
			onCacheCompleted();
		}
	}


#if UNITY_EDITOR
	public void UpdateList()
	{
		string searchFolder = "Character";
		string searchPath = "/Custom Assets/Bounty Hunter Assets/Resources/"+searchFolder;
		DirectoryInfo di = new DirectoryInfo(Application.dataPath + searchPath);
		FileInfo[] raw = di.GetFiles("*.asset",SearchOption.AllDirectories);
		//di.GetFiles()

		BountyCharacter bc;
		Object o;
		string tempName;
		Debug.Log(raw.Length + " assets gefunden");
		string path;
		ResourceListEntry sde;
		List<ResourceListEntry> notFound = new List<ResourceListEntry>(assetList); // keeps track of entries that dont have prefabs anymore

		// iterate through all prefabs in the folder
		for (int i = 0; i < raw.Length; i++)
		{
			path = raw[i].FullName.Replace(@"\", "/");
			path = "Assets" + path.Replace(Application.dataPath, "");

			o = AssetDatabase.LoadAssetAtPath<Object>(path);
			bc = o as BountyCharacter;
			tempName = o.name;

			// asset is already in list
			sde = assetList.Find(n => n.assetId == AssetDatabase.AssetPathToGUID(path));
			if (sde != null)
			{
				if(raw[i].Directory.Name != searchFolder)
				{
					tempName = raw[i].Directory.Name + "/"+ tempName;
				}
				sde.resourceName = tempName; // update resource name
				notFound.Remove(sde);
			}
			else
			{
				// add new entry
				sde = new ResourceListEntry();
				sde.intId = FetchNextId();
				if(raw[i].Directory.Name != searchFolder)
				{
					tempName = raw[i].Directory.Name + "/"+ tempName;
				}
				sde.resourceName = tempName;
				sde.assetId = AssetDatabase.AssetPathToGUID(path);
				assetList.Add(sde);
			}
			if(bc.characterId != tempName)
			{
				bc.characterId = tempName;
				EditorUtility.SetDirty(bc);
			}
		}

		// remove deleted entries
		notFound.ForEach(n => assetList.Remove(n));

		assetList.Sort((n, m) => n.resourceName.CompareTo(m.resourceName));

		EditorUtility.SetDirty(this);
	}
#endif

	public string[] GetCharacterNames()
	{
		List<string> list = assetList.ConvertAll<string>(n => n.resourceName);
		list.Insert(0, "NONE");
		return list.ToArray();
	}
	public int[] GetCharacterIds()
	{
		List<int> list = assetList.ConvertAll<int>(n => n.intId);
		list.Insert(0, -1);
		return list.ToArray();
	}

	public string GetCharacterName(int id, bool trimPath = false)
	{
		ResourceListEntry result = assetList.Find(n => n.intId == id);
		if(result != null)
		{
			if (trimPath)
				return result.resourceName.Substring(result.resourceName.LastIndexOf("/")+1);
			return result.resourceName;
		}
		return null;
	}
	public int GetCharacterId(string name) // fetch by resource name
	{
		ResourceListEntry result = assetList.Find(n => n.resourceName == name);
		if(result != null)
		{
			return result.intId;
		}
		return -1;
	}

	public int GetCharacterId2(string name) // ignore folder path part
	{
		ResourceListEntry result = assetList.Find(n => n.resourceName.Substring(n.resourceName.LastIndexOf("/")+1) == name);
		if (result != null)
		{
			return result.intId;
		}
		return -1;
	}

	public BountyCharacter LoadCharacterResource(int id)
	{
		string nam = GetCharacterName(id);
		if(!string.IsNullOrEmpty(nam))
		{
			return SDResources.Load<BountyCharacter>("Character/"+nam);
		}
		else
			return null;
	}
	public BountyCharacter LoadCharacterResource(string nam)
	{
		//string nam = GetCharacterName(id);
		if(!string.IsNullOrEmpty(nam))
		{
			return SDResources.Load<BountyCharacter>("Character/"+nam);
		}
		else
			return null;
	}


	////////////////// DA3 stuff //////////////////

	private string[] nameListMale = new string[]
	{
		"Oscar",
		"Max",
		"Scott",
		"Perry",

	};
	private string[] nameListFemale = new string[]
	{
		"Amy",
		"Lisa",
		"Diana",
		"Carla",

	};

	public string GetRandomName(bool female)
	{
		if (female)
			return nameListFemale[SDRandom.Range(0, nameListFemale.Length)];
		else
			return nameListMale[SDRandom.Range(0, nameListMale.Length)];

	}
}