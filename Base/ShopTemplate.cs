using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// a collection of shop pools that can be assigned as a shop inventory provider
/// </summary>
[CreateAssetMenu(fileName = "ShopTemplate", menuName = "SDObjects/Object/Shop Template")]
public class ShopTemplate : ScriptableObject
{

    [NonReorderable]
    public ShopPool[] shopPool;

	public ShopPool GetShopPool(ShopPool.PoolType type)
	{
		foreach (var item in shopPool)
		{
			if (item.type == type)
				return item;
		}
		return null;
	}

}
