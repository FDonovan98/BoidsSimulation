// Author: Harry Donovan
// Collaborators:
// License: GNU General Public License v3.0
// References: https://zklinger.com/unity-boids/
// Description:

using System.Collections.Generic;
using UnityEngine;

public class Boids : MonoBehaviour
{
    [SerializeField]
    private bool debugMode;

    [Header("Movement")]
    [SerializeField]
    float maxSpeed = 10.0f;
    [SerializeField]
    [Tooltip("The turn rate of the boid in degrees per second")]
    float turnRate = 1.0f;
    [SerializeField]
    float acceleration = 1.0f;

    [Header("Flock Behaviour")]
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float targetWeight = 1.0f;
    [SerializeField]
    bool targetObject = false;
    [SerializeField]
    Target target;
    [SerializeField]
    float seperationDistance = 4.0f;
    [SerializeField]
    [Range(0.0f, 1.0f)]
    float cohesionWeight = 1.0f;
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float seperationWeight = 1.0f;
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float alignmentWeight = 1.0f;

    // Calculated stats.
    BoidsController boidsController;
    int id;
    FlockValues flockValues;
    Vector3 separationVector;
    // The distance the boid should cover before reporting it's updated position to the BoidController.
    float updateDistance;
    float distanceTravelled = 0.0f;
    [HideInInspector]
    public Vector3 lastPos;
    [HideInInspector]
    public Vector3 velocity;

    Vector3 targetVel;
    Vector3 modifiedVel;

    // Start is called before the first frame update
    void Start()
    {
        InitialiseVariables();

        // Registers this boid with the parent controller. 
        boidsController.notifyBoidsPartitionUpdate += FlockValuesUpdated;
        id = boidsController.RegisterBoid(this, out updateDistance);

        // Randomisation on update distance to help space out partition update calls from the same flock.
        updateDistance += Random.Range(0, updateDistance) - updateDistance / 2;
    }

    private void InitialiseVariables()
    {
        boidsController = transform.parent.GetComponent<BoidsController>();
        lastPos = transform.position;

        Vector3 startingVel = new Vector3(Random.value, Random.value, Random.value);
        velocity = startingVel * Random.Range(-maxSpeed, maxSpeed);
    }

    // Check if boid id is in list of applicable ids.
    // If it is update flock values.
    // Bound to boidsController.notifyBoidsPartitionUpdate delegate, is called when a partitions values have been updated.
    void FlockValuesUpdated(PartitionData partitionData)
    {
        if (partitionData.boidIDs.Contains(id))
        {
            // Update stored flock values.
            flockValues = partitionData.adjustedFlockValues;

            // Recalculate separationVector now that the flockValues have changed.
            separationVector = new Vector3();
            foreach (Vector3 otherBoidPos in partitionData.flockValues.m_posArray)
            {
                separationVector += AvoidPoint(otherBoidPos);
            }

            CalculateTargetVel();
        }
    }

    void CalculateTargetVel()
    {
        targetVel = new Vector3();

        // Boid steering behaviours.
        targetVel += Target();

        targetVel += Cohesion(flockValues.m_avgPos);

        targetVel += Seperation(separationVector);

        targetVel += Alignment(flockValues.m_avgVel);

        targetVel = Vector3.ClampMagnitude(targetVel, maxSpeed);
    }

    // Update is called once per frame
    void Update()
    {
        // Will cause clipping as no collision checks. 
        // But is done so no RigidBody is needed.
        Vector3 newPos = lastPos + modifiedVel;
        transform.position = newPos;
        lastPos = newPos;

        transform.up = velocity;

        // Update distance travelled.
        distanceTravelled += modifiedVel.magnitude;

        if (distanceTravelled > updateDistance)
        {
            boidsController.UpdateBoidPos(id);
            distanceTravelled = 0.0f;
        }
    }

    // Called by the boid controller
    public void RecalculateVelocity()
    {
        velocity = Vector3.RotateTowards(velocity, targetVel, turnRate * Mathf.Deg2Rad, acceleration);
        modifiedVel = velocity * Time.deltaTime;
    }

    // Move to a specific target, or continue in the current direction if there is no target.
    private Vector3 Target()
    {
        if (targetObject && target != null)
        {
            return (target.lastPos - lastPos).normalized * maxSpeed * targetWeight;
        }

        return velocity.normalized * maxSpeed * targetWeight;
    }

    // Align own velocity with the flocks velocity.
    private Vector3 Alignment(Vector3 avgVel)
    {
        return (avgVel - velocity).normalized * alignmentWeight;
    }

    // Maintain distance from other boids in the flock.
    private Vector3 Seperation(Vector3 separationVector)
    {
        return separationVector * seperationWeight;
    }

    private Vector3 AvoidPoint(Vector3 posToAvoid)
    {
        float dist = Vector3.Distance(posToAvoid, lastPos);

        if (dist < seperationDistance && dist != 0)
        {
            return (lastPos - posToAvoid).normalized / Mathf.Pow(dist, 2);
        }

        return Vector3.zero;
    }

    // Moves the boid to the centre of the flock.
    private Vector3 Cohesion(Vector3 avgPos)
    {
        return (avgPos - lastPos).normalized * cohesionWeight;
    }

    private void OnDrawGizmos()
    {
        if (debugMode)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(flockValues.m_avgPos, 1f);
        }
    }
}