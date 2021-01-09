using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainingArea : MonoBehaviour
{
	public List<GameObject> Targets { get; private set; }
    // Start is called before the first frame update
    void Start()
    {
        
    }
	private void Awake()
	{
		Targets = new List<GameObject>();
		FindChildTargets(transform);
		//Debug.Log("tamaño de lista: " + Targets.Count);
	}
    // Update is called once per frame
    void Update()
    {
        
    }

    private void FindChildTargets(Transform parent)
	{
        //Debug.Log(parent.name);
		for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.CompareTag("target") || child.CompareTag("finalTarget"))
            {
                // Found a target, add it to the targets list
                Targets.Add(child.gameObject);
            }
        }
	}
}
