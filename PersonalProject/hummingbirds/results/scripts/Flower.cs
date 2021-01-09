using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// manages a single flower with nectar
public class Flower : MonoBehaviour
{
	[Tooltip("The color when the flower is full")]
	public Color fullFlowerColor = new Color(1f, 0f, .3f);
	[Tooltip("The color when the flower is empty")]
	public Color emptyFlowerColor = new Color(.5f, 0f, 1f);

	/// <summary>
	/// The trigger collider representing the nectar
	/// </summary>
	[HideInInspector]
	public Collider nectarCollider;


    // The solid collider representing the flower petals
    private Collider flowerCollider;

	// The flower´s materil
	private Material flowerMaterial;

	/// A vector pointing straight out of the flower
	public Vector3 FlowerUpVector
	{
		get
		{
			return nectarCollider.transform.up;
		}
	}

	/// The center position of the nectar collider 
	public Vector3 FlowerCenterPosition
	{
		get
		{
			return nectarCollider.transform.position;
		}
	}

	/// The amount of nectar remaining in hte flower
	public float NectarAmount { get; private set; }

	public bool HasNectar
	{
		get
		{
			return NectarAmount > 0f;
		}
	}

	/// Attempts to remove nectar from the flower.
	/// "amount" the amount of nectar to remove
	public float Feed(float amount)
	{
		//track how much nectar was sucesfully taken (cannot take more than is available)
		float nectarTaken = Mathf.Clamp(amount, 0f, NectarAmount);

		//subtract the nectar
		NectarAmount -= amount;

		if (NectarAmount <= 0)
		{
			//no nectar remaining
			NectarAmount = 0;
			//disable the flower and nectar colliders
			flowerCollider.gameObject.SetActive(false);
			nectarCollider.gameObject.SetActive(false);
			//change flower color to indicate it is empty
			flowerMaterial.SetColor("_BaseColor", emptyFlowerColor);
		}
		//return the am9ount of nectar thatwas taken
		return nectarTaken; 

	}

	/// Resets flower
	public void ResetFlower()
	{
		//refill the nectar
		NectarAmount = 1f;
		//enable the flower and nectar colliders
		flowerCollider.gameObject.SetActive(true);
		nectarCollider.gameObject.SetActive(true);

		//change flower color to indicate it is full
		flowerMaterial.SetColor("_BaseColor", fullFlowerColor);
	}

	///calle when the flower wakes up
	private void Awake()
	{
		//find the flower´s mesh renderer and get the main material
		MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
		flowerMaterial = meshRenderer.material;
		//find flower and nectar colliders
		flowerCollider = transform.Find("FlowerCollider").GetComponent<Collider>();
		nectarCollider = transform.Find("FlowerNectarCollider").GetComponent<Collider>();
	}
}
