using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using System.Collections;
using System.Collections.Generic;

public class mc2Agent : Agent
{
	public Transform target;
	public float speed = 10;

	private Rigidbody player;
	private Vector3 moveDir;

	private GameObject nearestTarget;
	private TrainingArea trainingArea;

	private void Start()
	{
		player = GetComponent<Rigidbody>();
		trainingArea = GetComponentInParent<TrainingArea>();
	}

	//public override void AgentAction(float[] vectorAction, string textAction)

	public override void CollectObservations(VectorSensor sensor)
	{
		//the observations to collect are the current position, the position of the target and the velocity x and z of the agent.
		Vector3 toTarget = target.localPosition - player.position;
		sensor.AddObservation(toTarget.normalized); // this sensor collect 3 observations (one per x,y z of the vector3)
		sensor.AddObservation(player.position.normalized); // this sensor collect 3 observations (one per x,y z of the vector3)

		// Agent velocity:
		sensor.AddObservation(player.velocity.x); // this sensor collects one observation
		sensor.AddObservation(player.velocity.z); // this sensor collects one observation

		//relative distance to target
		sensor.AddObservation(toTarget.magnitude / 100);

		//total of 9 observations to be collected. Change paremeter in editor
	}

	public override void OnActionReceived(float[] vectorAction) //used to be AgentAction()
	{
		
		Vector3 controlSignal = Vector3.zero;
		moveDir.x = vectorAction[0] * speed;
		//moveDir.y = Mathf.Clamp(0, 0);
		moveDir.z = vectorAction[1] * speed;
		transform.LookAt(transform.localPosition + new Vector3(moveDir.x, 0, moveDir.z));
		player.velocity = moveDir;

		
		//if the agent fell off the platform:
		if (this.transform.localPosition.y < 0f || this.transform.localPosition.y > 2.5f)
		{
            AddReward(-.05f);
			EndEpisode();
		}
		/*
		Vector3 move = new Vector3(vectorAction[0], 0, vectorAction[1]);
		player.AddForce(move * speed);
		*/
	}

	public override void OnEpisodeBegin()  //Used to be AgentReset()
	{
		Debug.Log("EpisodeBegins...");
		// Zero velocity and respawn in the middle
		player.velocity = Vector3.zero;
		transform.localPosition = new Vector3(10,1,-5.5f);
		foreach (GameObject extratarget in trainingArea.Targets)
		{
			if(extratarget.CompareTag("targetDone"))
			{
				extratarget.transform.tag = "target";
			}
		}
		UpdateNearestTarget();
		/*
		if (this.transform.localPosition.y < 0)
		{
			player.velocity = Vector3.zero;
			transform.localPosition = new Vector3(10,1,-7);
		}
		*/
	}

	    public override void Heuristic(float[] actionsOut)
    {
        // Create placeholders for all movement/turning. So by default if no input, it will stay still.
        Vector3 forward = Vector3.zero;
        Vector3 left = Vector3.zero;
        Vector3 up = Vector3.zero;

        // Convert keyboard inputs to movement and turning
        // All values should be between -1 and +1
        // forwad / backward
        if (Input.GetKey(KeyCode.W)) forward = transform.forward;
        else if (Input.GetKey(KeyCode.S)) forward = -transform.forward;
        // left / right
        if (Input.GetKey(KeyCode.A)) left = -transform.right;
        else if (Input.GetKey(KeyCode.D)) left = transform.right;
        // up / down
        if (Input.GetKey(KeyCode.E)) up = transform.up;
        else if (Input.GetKey(KeyCode.C)) up = -transform.up;

        // Combine movement vectors and normalize
        Vector3 combined = (forward + left + up).normalized;
		/*
        // Add the three normalised movement values, pitch and yaw to the actions out array
        actionsOut[0] = combined.x;
        actionsOut[1] = combined.y;
        actionsOut[2] = combined.z;*/
    }

	private void OnTriggerEnter(Collider other)
	{
		//UpdateNearestTarget();
		if (other.gameObject == target.gameObject)
		{
			SetReward(5f);
			EndEpisode();
		}
		else if (other.CompareTag("target"))
        {
            // Collided with the area boundary, give a negative reward
            AddReward(2f);
			other.transform.tag = "targetDone";
			UpdateNearestTarget();
			//EndEpisode();
        }
	}

	private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("sideWall"))
        {
            // Collided with the area boundary, give a negative reward
            AddReward(-.05f);
			Debug.Log("HA DADO EN PARED LATERAL");
			//EndEpisode();
        }
		/*
        else if (collision.collider.CompareTag("wall"))
        {
            // Collided with the area boundary, give a negative reward
            AddReward(-.001f);
        }
		*/
    }

	private void UpdateNearestTarget()
	{
		foreach (GameObject extratarget in trainingArea.Targets)
		{
			if (nearestTarget == null)
			{
				nearestTarget = extratarget;
			}
			float distanceTotarget = Vector3.Distance(extratarget.transform.position, player.position);
			float distanceToCurrentNearestTarget = Vector3.Distance(nearestTarget.transform.position, player.position);
			if (extratarget.CompareTag("target") && (distanceTotarget < distanceToCurrentNearestTarget))
			{
				nearestTarget = extratarget;
				Debug.Log("Target actualizado");
			}
		}
	}
	private void Update()
    {
		if (nearestTarget != null)
		{
        	Debug.DrawLine(player.position, nearestTarget.transform.position, Color.green);
		}
    }
}
