using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

///<summary> A hummingbird ML Agent. This class makes decissions using neural networks </summary>
public class HummingbirdAgent : Agent
{
    [Tooltip("Force to apply when moving")]
    public float moveForce = 2f;

    [Tooltip("Speed to pitch up or down")]
    public float pitchSpeed = 100f;

    [Tooltip("Speed to rotate around the up axis")]
    public float yawSpeed = 100f;

    [Tooltip("Transform at the tip of the beak")]
    public Transform beakTip;

    [Tooltip("The agents camera")]
    public Camera agentCamera;

    [Tooltip("Whether this is training mode or gameplay mode")]
    public bool trainingMode;

    //The rigidbody of the agent
    new private Rigidbody rigidbody;

    //the flower area that the agent is in
    private FlowerArea flowerArea;

    //the nearest flower to the agent
    private Flower nearestFlower;

    //Allows for smoother pitch changes
    private float smoothPitchChange = 0f;

    //Allows for smother yaw changes
    private float smoothYawChange = 0f;

    // Maximum angle the bird can pitch up and dowen
    private const float MaxPitchAngle = 80f;

    //Maximum distance from the peak tip to accept nectar collision. Its not considered nectar 
    //collision if any other part of the bird touches the nectar but this one. 
    private const float BeakTipRadius = 0.008f;

    // Whether the agent is frozen (intentionally not flying)
    private bool frozen = false;

    /// <summary> the amount of nectar the agent has obtained this episode </summary>
    public float NectarObtained { get; private set; }

    /// <summary> Initializes the agent overriding the original MLAgents script </summary>
    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();
        flowerArea = GetComponentInParent<FlowerArea>();
        // If not training mdode no max step, play forever.
        if (!trainingMode)
        {
            MaxStep = 0;
        }
    }

    /// <sumary> Reset the agent when an episode begins </summary>
    public override void OnEpisodeBegin()
    {
        if (trainingMode)
        {
            //only reset flowers in training when there is one agent per area
            flowerArea.ResetFlowers();
        }
        // Reset nectar obtained
        NectarObtained = 0f;
        // Zero out velocities so that movement tops before a new episode begins
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        // Default to spayning in front of a flower
        bool inFrontofFlower = true;
        if (trainingMode)
        {
            // Spawn in front of the flower 50% of the time during training
            inFrontofFlower = UnityEngine.Random.value > 0.5f;
        }
        // Move the agent to a new random position
        MoveToSafeRandomPosition(inFrontofFlower);
        // Recalculate the nearest flower now that the agent has moved
        UpdateNearestFlower();
    }


    /// <sumary> 
    ///called when an action is received from either the player input or the neural network, 
    /// VectorAction[i] represents: 
    /// Index 0: move Vector x (+1 = right, -1 = left)
    /// Index 1: move vector y (+1 = up, -1 = down)
    /// Index 2: move vector z (+1 = forward, -1 = backward)
    /// Index 3: picht angle (+1 pitch up, -1 = pitch down)
    ///Index 4: Yaw angle (+1 = turn right, -1 = turn left) 
    /// these will be the value to set in the "space size" parameter of the behaviour in th eeditor
    ///</summary>
    /// <param name="vectorAction"> The actions to take.null Indices 0, 1, 2 are the x,y z components of the vector</param>
    public override void OnActionReceived(float[] vectorAction)
    {
        // dont take action if frozen
        if (frozen)
        {
            return;
        }
        // Calculate movement Vector
        Vector3 move = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]);
        // Add force in the direction of the move vector
        rigidbody.AddForce(move * moveForce);

        // Get the current rotation
        Vector3 rotationVector = transform.rotation.eulerAngles; //x,y,z rotation values instead of Quaternions

        // Calculate pitch and yaw rotation
        float pitchChange = vectorAction[3];
        float yawChange = vectorAction[4];

        // Calculate smooth rotation changes. Not on every frame (as it is variable) but on every fixed delta time (every 0.2 sec)
        smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
        smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);

        // Calculate new pitch and yaw based on smoothed values
        // Clam pitch to avoid flippinf upside down
        float pitch = rotationVector.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
        if (pitch > 180f)
        {
            pitch -= 360f;
        } 
        pitch = Mathf.Clamp(pitch, -MaxPitchAngle, MaxPitchAngle);

        float yaw = rotationVector.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;
        // Apply the new rotation
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }


    /// <summary>
    /// Collect vector observaitons from the environment.null 
    /// It is going to observe the rotation of the bird, the vector that point to the nearest flower,
    /// a dot product telling us if the beak is inFront of the flower or not
    /// another dot to see if the beak is POINTING at the flower
    /// and the relative distance from the beak tip to the flower to the flower (using the areadiameter), to it will
    /// tell us how far the flower is relative to the entire island area
    /// </summary>
    public override void CollectObservations(Unity.MLAgents.Sensors.VectorSensor sensor)
    {
        // If nearestFLower is null, observe an empty array and return early. 
        if (nearestFlower == null)
        {
            sensor.AddObservation(new float[10]);
            return;
        }

        // Observe the agents local rotation  (4 observations). 
        // it is best to always normalize the observations to pass it to the NN
        sensor.AddObservation(transform.localRotation.normalized);

        // Get a vector from the beak tip to the nearest flower
        Vector3 toFlower = nearestFlower.FlowerCenterPosition - beakTip.position;

        // Observe a normalised vector pointing to the nearest flower. (3 observations)
        sensor.AddObservation(toFlower.normalized);

        // Observe a dot prouduct that indicates whether the beak tip is in front of the flower. (1 observation)
        //(+1 = beak tip directly in front of the flower, -1 = directly behind)
        // for more info google dot products
        sensor.AddObservation(Vector3.Dot(toFlower.normalized, -nearestFlower.FlowerUpVector.normalized));

        //Observe a dot product that indicates whether the beak is pointing toward the flower. (1 observation)
        // +1 = beakpointing directily at the flower, -1 directly away
        sensor.AddObservation(Vector3.Dot(beakTip.forward.normalized, -nearestFlower.FlowerUpVector.normalized));

        // Observe the relative distance from the beak tip to the flower. (1 observation)
        sensor.AddObservation(toFlower.magnitude / FlowerArea.AreaDiameter);

        // 10 total observations! This number will go on the "Space size" parameter on the Behaviour parameter of the agent in the editor
    }


    /// <summary> 
    /// when behaviour type is set to heuristic only on the agent´s behaviour parameters thius function will be called.null 
    /// Its return values will be fed into <see cref="OnActionReceived(float[])"/> 
    /// instead of using the neural network.null So its going to take input from the player
    /// </summary>
    public override void Heuristic(float[] actionsOut)
    {
        // Create placeholders for all movement/turning. So by default if no input, it will stay still.
        Vector3 forward = Vector3.zero;
        Vector3 left = Vector3.zero;
        Vector3 up = Vector3.zero;
        float pitch = 0f;
        float yaw = 0f;

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

        //pitch up/down
        if (Input.GetKey(KeyCode.UpArrow)) pitch = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) pitch = -1f;
        //yaw left/right
        if (Input.GetKey(KeyCode.LeftArrow)) yaw = -1f;
        else if (Input.GetKey(KeyCode.RightArrow)) yaw = 1f;

        // Combine movement vectors and normalize
        Vector3 combined = (forward + left + up).normalized;

        // Add the three normalised movement values, pitch and yaw to the actions out array
        actionsOut[0] = combined.x;
        actionsOut[1] = combined.y;
        actionsOut[2] = combined.z;
        actionsOut[3] = pitch;
        actionsOut[4] = yaw;
    }


    ///<summary>
    /// Prevent the agent from moing and taking actions
    /// </summary>
    public void FreezAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");
        frozen = true;
        rigidbody.Sleep();
    }
    
    ///<summary>
    /// Resume agent movement and actions
    /// </summary>
    public void UnfreezAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");
        frozen = false;
        rigidbody.WakeUp();
    }


    ///<summary> Move the agent to a safe random position (i.e does not) collide with anything
    /// in in front of flower, point the beak at the flower </summary>
    ///<param name="inFrontOfFlower">Whether to choose a spot in front of a flower </param>
    private void MoveToSafeRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;
        // to prevent an infinite loop, we give the bird 100 attemts to try and find a safe position. 
        //if it doesnt find it, we assume something is not right. 
        int attemptsRemaining = 100;
        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion();

        //Loop unitl a safe position is found or we run out of attempts
        while (!safePositionFound && attemptsRemaining > 0)
        {
            attemptsRemaining--;
            if(inFrontOfFlower)
            {
                // Pick a random flower. It selects a random index from 0 to the max int on the flowers list. 
                Flower randomFlower = flowerArea.Flowers[UnityEngine.Random.Range(0, flowerArea.Flowers.Count)];
                // Position 10 to 20 cm in front of the flower - Generate aradndom value between .1 and .2 m:
                float distanceFromFlower = UnityEngine.Random.Range(.1f,.2f);
                potentialPosition = randomFlower.transform.position + randomFlower.FlowerUpVector * distanceFromFlower;
                //Point beak at flower (bird´s hear is center of transform)
                Vector3 toFlower = randomFlower.FlowerCenterPosition - potentialPosition;
                potentialRotation = Quaternion.LookRotation(toFlower, Vector3.up);

            }
            else
            {
                // Pick a random height from the ground
                float height = UnityEngine.Random.Range(1.2f, 2.5f);

                //Pick a random radius from the center of the area
                float radius = UnityEngine.Random.Range(2f, 7f);

                //Pick a random direction rotated around the y axis
                Quaternion direction = Quaternion.Euler(0f, UnityEngine.Random.Range(-180f, 180f), 0f);

                // Combine height, radius, and direction to pick a potential position
                potentialPosition = flowerArea.transform.position + Vector3.up * height + direction * Vector3.forward * radius;

                // Choose and set random starting pitch and yaw
                float pitch = UnityEngine.Random.Range(-60f, 60f);
                float yaw = UnityEngine.Random.Range(-180f, 180f);
                potentialRotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            // Check to see if the agent will collide with anything
            Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f);

            //Safe position found if no colliders are overlapped. 
            safePositionFound = colliders.Length == 0;
        }

        Debug.Assert(safePositionFound, "Could not find a safe position to spawn");
        //Set the position and roration
        transform.position = potentialPosition;
        transform.rotation = potentialRotation;
    }


    /// <summary> update the nearest flower to the agent. This function is not call at every upddate, as the bird would go crazy
    /// it is only called ocasionally </summary>
    private void UpdateNearestFlower()
    {
        foreach (Flower flower in flowerArea.Flowers)
        {
            if (nearestFlower == null && flower.HasNectar)
            {
                // no current nearest flower assigned yet and this flower has nectar, so set nearesflower to THIS flower :) 
                nearestFlower = flower;
            }
            else if (flower.HasNectar)
            {
                // Calculate distance to this flower and distance to the current nearest flower
                float distancetoFlower = Vector3.Distance(flower.transform.position, beakTip.position);
                float distanceToCurrentNearestFlower = Vector3.Distance(nearestFlower.transform.position, beakTip.position);

                // If current nearest flowert is empty OR this flower is closer, update the nearest flower
                if (!nearestFlower.HasNectar || distancetoFlower < distanceToCurrentNearestFlower)
                {
                        nearestFlower = flower;
                }
            }
        }
    }

   ///<summary>
    /// called when the agent´s collider enters a trigger collider
    /// "other" = the trigger collider
    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    ///<summary>
    /// called when the agent´s collider enters a trigger collider
    /// "other" = the trigger collider
    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }


    /// <summary>
    /// handles when the agents collider enters or stays in a trigger collider.
    /// "collider" = the trigger cllider
    private void TriggerEnterOrStay(Collider collider)
    {
        // Check if agent is colliding with nectar. 
        if (collider.CompareTag("nectar"))
        {
            Vector3 closestPointToBeakTip = collider.ClosestPoint(beakTip.position);

            // Check if the closest colision point is close to the beak tip
            // note a collision with anything bt the beak tip should not count. reward only when the beak is inside the nectar.
            if (Vector3.Distance(beakTip.position, closestPointToBeakTip) < BeakTipRadius)
            {
                // Look up the flower for this nectar collider
                Flower flower = flowerArea.GetFlowerFromNectar(collider);

                // Attempt to take .01 nectar
                // Note this is per fixed time step, meaning it happens every .02 secs, ot 50x per sec
                float nectarReceived = flower.Feed(0.1f);

                // Keep track of nectar obtained. 
                NectarObtained += nectarReceived;

                if (trainingMode)
                {
                    // calculate reward for getting nectar
                    //this first is a rewad for pointing the beak tip directly at the flower
                    float bonus = .02f * Mathf.Clamp01(Vector3.Dot(transform.forward.normalized, -nearestFlower.FlowerUpVector.normalized));
                    // its always getting a reward if its inside of the nectar, but the extra bonus if its pointing directly at it. 
                    AddReward(.01f + bonus);                    
                }
                // If flower is empty, update nearesFlower
                if (!flower.HasNectar)
                {
                    UpdateNearestFlower();
                }
            }
        }
    }

    /// <summary> 
    /// Called when the agent collides with something solid
    /// collision = collision info
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (trainingMode && collision.collider.CompareTag("boundary"))
        {
            // Collided with the area boundary, give nefative reward. 
            // Only when the collision enters, and not when stays, as we did on th epositive reward
            AddReward(-.5f);
            // Note if we give too many negative rewards, the agent will paralise as it will think if it 
            // takes any action it will have negative consequences. 
        }
    }

    /// <summary>
    /// called every frame
    /// </summary>
    private void Update()
    {
        //draw a line from the beak tip to the nearest flower. It will show only on the scene while play mode is on
        if (nearestFlower != null)
        {
            Debug.DrawLine(beakTip.position, nearestFlower.FlowerCenterPosition, Color.green);
        }
    }

    ///<summary>
    /// called every .02 secs
    /// </summary>
    private void FixedUpdate()
    {   
        //Avoids scenario where nearest flower nectar is stolen by oponent and not updated. 
        if (nearestFlower != null && !nearestFlower.HasNectar)
        {
            UpdateNearestFlower();
        }
    }
}
