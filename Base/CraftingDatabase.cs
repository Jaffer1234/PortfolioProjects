using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CraftingDatabase", menuName = "SDObjects/DataBase/Crafting Database")]
public class CraftingDatabase : SDContainerDatabase<CraftingRecipeTemplate>
{
    
    /// <summary>
    /// fetch a random recipe id. constrained by item type and item tier. you can privode a list of recipe ids that should be excluded
    /// </summary>
    /// <param name="type">item type to search for, can be a super type to allow random subtypes</param>
    /// <param name="tier">tier to search for, or -1 for no filter</param>
    /// <param name="exceptionList">list of integers that exclude recipe ids you dont want to get</param>
    /// <returns></returns>
    public int GetRandomRecipe(BaseItem.ItemType2 type, int tier, int variant, List<int> exceptionList = null)
	{
        List<CraftingRecipeTemplate> tList = new List<CraftingRecipeTemplate>();
        if (exceptionList == null)
            exceptionList = new List<int>();
		foreach (var item in assetList)
		{
            if ((item.recipe.result.variant == variant || variant == -1) && (item.recipe.result.tier == tier || tier == -1) && BaseItem.IsType(item.recipe.result.itemType, type) && !exceptionList.Contains(item.intId))
			{
                tList.Add(item);
			}
		}

        if (tList.Count > 0)
            return tList[SDRandom.Range(0, tList.Count)].intId;
        else
            return -1;
	}

}

[System.Serializable]
public class CraftingRecipeTemplate : SDGenericDataContainer
{

    public BountyCraftingRecipe recipe;

}