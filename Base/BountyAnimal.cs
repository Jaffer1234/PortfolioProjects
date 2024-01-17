using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FullSerializer;

/// <summary>
/// simple data structure that could be used to store the instance information of an animal in a shop or a base
/// </summary>
[CreateAssetMenu(fileName = "Animal", menuName = "SDObjects/Object/Animal")]
[fsObject]
public class BountyAnimal : ScriptableObject
{

	public string animalId;
	public AnimalType type;
	public int price;

    public enum AnimalType
	{
		None = 0,
		Chicken = 1,
		Sheep = 2,
		Pig = 3,
		Cow = 4,
		Goat = 5,
	}
}
