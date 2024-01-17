using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities.UniversalDelegates;
using UnityEngine;


[CreateAssetMenu(fileName = "XBase", menuName = "SDObjects/DataBase/NewBase")]
public class NewBases : ScriptableObject
{
    /// <summary>
    /// -This scriptable object is Data Container for any new Bases we would like to implement in the game. It hold important variables like, 
    /// Base prefab, Base Inhabitants, Current Rooms and Active Jobs. 
    /// </summary>
    [System.Serializable]
    public class BaseInfo
    {
        public int ScenarioID;
        public string BaseName;
        public List<JobDefinition> jobDefinitions;
        public List<CampRoomEntry> currentRooms;
        public GameObject BasePrefab;
        public BountyCharacter[] BaseInhabitants;
    }

    public List<BaseInfo> AllBasesList;
    [HideInInspector]
    public List<JobInstance> activeJobs;
    [HideInInspector]
    public int LastActiveBaseScenarioID;
    private List<BountyCharacter> ActiveBaseBountyCharacter;

    // Start is called before the first frame update
    public List<BountyCharacter> GetAllCurrentBasePeople()
    {
        List<BountyModel> AvailableModelsInBase = new List<BountyModel>();
        List<BountyCharacter> result = new List<BountyCharacter>();
        AvailableModelsInBase = BountyManager.instance.campScene.baseNav.gameObject.GetComponent<BaseNavController>().GetAllModels();
        foreach (BountyModel item in AvailableModelsInBase)
        {

            result.Add(item.myCharacter);
        }

        return result;
    }

    
    public List<JobInstance> GetActiveJobs()
    {
        return new List<JobInstance>(activeJobs);
    }
    /// <summary>
    /// -This function is almost same as StartDayLife which is already implemented in BountyManager.Session for HQ_Homebase. Rest of the bases
    /// will complete their day via this function.
    /// </summary>
    public void StartDayLifeForNewBases(bool updateAll, bool midday, bool ommitWorkAnis, BaseInfo baseInfo)
    {
        if (activeJobs.Count > 0)
        {
            BountyCharacter bc;
            // update graphical camp people
            //if (!(day == 1 && session == 0) && (CurrentTutorialIndex < 0 || CurrentTutorialIndex > 3))
            {
                //  bc = this.GetAllStevenCampPeople(n => n == playerCharacter);
                // bc = list[0];
                if (activeJobs != null)
                {


                    for (int k = 0; k < activeJobs.Count; k++)
                    {
                        bc = activeJobs[k].character;
                        Debug.Log(bc.name);

                        if (bc && bc.Job == CampRoomType.None)
                        {
                            bc.startNodeIndex = 0;
                            if (midday)
                            {
                                bc.startNodeStation = BaseNavNode.StationType.Any;
                                bc.startNodeType = BaseNavNode.NodeType.Idle;
                            }
                            else
                            {
                                bc.startNodeStation = BaseNavNode.StationType.Bed;
                                bc.startNodeType = BaseNavNode.NodeType.Station;
                            }
                            bc.startNavMode = 1;
                            bc.skipAniFast = true;
                        }
                        else if (bc)
                        {
                            bc.startNodeStation = BaseNavNode.GetStationFromRoom(bc.Job);
                            bc.startNodeType = BaseNavNode.NodeType.Station;
                            bc.startNavMode = 1;
                            bc.skipAniFast = true;
                        }



                        List<JobInstance> jobs = GetActiveJobs();
                        if (ommitWorkAnis)
                        {
                            foreach (JobInstance ji in jobs)
                            {
                                ji.character.startNavMode = 2;
                                ji.character.goToWork = false;
                                ji.character.startNodeType = BaseNavNode.NodeType.Idle;
                                ji.character.startNodeStation = BaseNavNode.StationType.Any;
                                BountyManager.instance.CBase.GetComponent<BountyBase>().UpdateRoomUpgradeModel(ji.type, false, 1f);
                                activeJobs.Remove(ji);
                            }
                        }
                        else
                        {
                            foreach (JobInstance ji in jobs)
                            {
                                ji.UpdateCharacterAnis();
                                BountyManager.instance.CBase.GetComponent<BountyBase>().UpdateRoomUpgradeModel(ji.type, false, 1f);
                                activeJobs.Remove(ji);
                            }
                        }
                    }
                }

            }


            List<BountyCharacter> list2 = new List<BountyCharacter>();

            list2 = GetAllCurrentBasePeople();
            foreach (var item in list2)
            {
                if (item.Model && item.Model.myNavAgent && item.Model.myNavAgent.currentNode)
                    item.Model.myNavAgent.currentNode.occupied = false;
                item.DestroyModel();
            }

            BountyManager.instance.CBase.GetComponent<BountyBase>().UpdatePeople(list2);
            BountyManager.instance.CBase.GetComponent<BountyBase>().UpdateRoomModels(baseInfo.currentRooms);
            Debug.Log(BountyManager.instance.CBase.name);



            // prevent people from going to bed on first random choosen node
            list2.ForEach(n => n.Model.myNavAgent.LastRandomStation = BaseNavNode.StationType.Bed);
            BountyManager.instance.ChangeNightLighSettingsInNewBases();

        }
        else
        {
            Debug.LogError("AtiveJobsIsNull");
        }
       
    }
    


}






