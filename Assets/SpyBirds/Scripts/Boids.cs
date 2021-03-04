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
    float seperationDistance = 4.0f;
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float targetWeight = 1.0f;
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
    public Vector3 lastPos;
    public Vector3 velocity;
    Vector3 targetVel;

    // Start is called before the first frame update
    void Start()
    {
        InitialiseVariables();

        boidsController.notifyBoidsPartitionUpdate += FlockValuesUpdated;
        id = boidsController.RegisterBoid(this, out updateDistance);
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

            targetVel = new Vector3();

            targetVel += Target();

            // Flock behaviour.
            targetVel += Cohesion(flockValues.m_avgPos);

            targetVel += Seperation(separationVector);

            targetVel += Alignment(flockValues.m_avgVel);

            targetVel = Vector3.ClampMagnitude(targetVel, maxSpeed);
        }
    }

    // Update is called once per frame
    void Update()
    {
        velocity = Vector3.RotateTowards(velocity, targetVel, turnRate * Mathf.Deg2Rad * Time.deltaTime, acceleration * Time.deltaTime);

        // Will cause clipping as no collision checks. 
        // But is done so no RigidBody is needed.
        transform.position += velocity * Time.deltaTime;
        transform.up = velocity.normalized;

        // Update distance travelled.
        distanceTravelled += Vector3.Distance(transform.position, lastPos);
        lastPos = transform.position;

        if (distanceTravelled > updateDistance)
        {
            distanceTravelled = 0.0f;
            boidsController.UpdateBoidPos(id);
        }
    }

    private Vector3 Target()
    {
        // Vector3 target = avgPos + avgVel;
        // target -= transform.position;
        // return target * targetWeight;

        return velocity.normalized * maxSpeed * targetWeight;
    }

    private Vector3 Alignment(Vector3 avgVel)
    {
        return (avgVel - velocity).normalized * alignmentWeight;
    }

    private Vector3 Seperation(Vector3 separationVector)
    {
        return separationVector * seperationWeight;
    }

    private Vector3 AvoidPoint(Vector3 posToAvoid)
    {
        float dist = Vector3.Distance(posToAvoid, lastPos);

        if (dist < seperationDistance && dist != 0)
        {
            // Equation is gained through trial and error.
            // return (transform.position - posToAvoid) * (seperationDistance - dist);

            return (lastPos - posToAvoid).normalized / Mathf.Pow(dist, 2);
        }

        return Vector3.zero;
    }

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