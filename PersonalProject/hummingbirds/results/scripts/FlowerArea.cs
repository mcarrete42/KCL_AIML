using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a collection of flower plants and arranged flowers
/// </summary>

public class FlowerArea : MonoBehaviour
{
	// The diameter of the area where the agent and flowers can be
	// used for observing relative distance from agent to flower. 
	// we rationalise the distance so that is in a range from 0 to 1
	// so that is works ok with the neural network
	public const float AreaDiameter = 20f;

	// the lit of all flower plants in this flower area 
	//(flower plants have multiple flowers)
	private List<GameObject> flowerPlants;

	//A lookup dictiuonary ofr looking up a flower form a nectar collider
	private Dictionary<Collider, Flower> nectarFlowerDictionary;

	///List of all flowers in the flower area
	public List<Flower> Flowers { get; private set; }

	///reset flower and flower plant
	public void ResetFlowers()
	{
		//rotate each flower around the Y axis and subtly around X and Z
		foreach (GameObject flowerPlant in flowerPlants)
		{
			float xRotation = UnityEngine.Random.Range(-5f, 5f);
			float yRotation = UnityEngine.Random.Range(-180f, 180f);
			float zRotation = UnityEngine.Random.Range(-5f, 5f);
			flowerPlant.transform.localRotation = Quaternion.Euler(xRotation, yRotation, zRotation);
		}
		// Reset each flower
		foreach (Flower flower in Flowers)
		{
			flower.ResetFlower();
		}		
	}

	/// <summary> Gets the "Flower" that a nectar collider belongs to. </summary>
	/// <param name="collider"> is the nectar collider </param>
	/// <returns> The matching flower </returns>
	public Flower GetFlowerFromNectar(Collider collider)
	{
		return nectarFlowerDictionary[collider];
	}

	///<summary> called when the area wakes up
	private void Awake()
	{
		// Initialize variables
		flowerPlants = new List<GameObject>();
		nectarFlowerDictionary = new Dictionary<Collider, Flower>();
		Flowers = new List<Flower>();
	}

	private void Start()
	{
		//find all flowers that are children of this GameObject/Transform
		FindChildFlowers(transform);
	}

	///<summary> recursevely finds all flowers and flower plants 
	/// that are childrnet of a parent transform </summary>
	///<param name="parent"> The parent of teh childrent to check</param>

	private void FindChildFlowers(Transform parent)
	{
		for (int i = 0; i < parent.childCount; i++)
		{
			Transform child = parent.GetChild(i);
			if (child.CompareTag("flower_plant"))
			{
				//Found a flower plant, add it to the flowerPlants list
				flowerPlants.Add(child.gameObject);
				//Look for flowers within the flower plant
				FindChildFlowers(child);
			}
			else
			{
				//not a flower plant, look for a flower component
				Flower flower = child.GetComponent<Flower>();
				if (flower != null)
				{
					//found a flower, add it to the flowers list
					Flowers.Add(flower);
					//Add the nectar collider to the lookup dictionary
					nectarFlowerDictionary.Add(flower.nectarCollider, flower);
					// Note: there are no flwoers that are children of other flowers.
				}
				else
				{
					//Floewr component not found, so check children
					FindChildFlowers(child);
				}
			}
		}
	}
}
