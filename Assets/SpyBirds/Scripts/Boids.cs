// Author: Harry Donovan
// Collaborators:
// License: GNU General Public License v3.0
// References: https://zklinger.com/unity-boids/
// Description:

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]

public class Boids : MonoBehaviour
{
    [SerializeField]
    private bool debugMode;
    [Header("Detection")]
    Rigidbody rb;
    SphereCollider sphereCollider;
    [SerializeField]
    private float awarenessRadius = 10.0f;
    private List<Collider> knownObstacles = new List<Collider>();

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
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float avoidTerrainWeight = 1.0f;

    // Calculated stats.
    BoidsController boidsController;
    int id;
    FlockValues flockValues;
    Vector3 separationVector;
    // The distance the boid should cover before reporting it's updated position to the BoidController.
    float updateDistance;
    float distanceTravelled = 0.0f;
    Vector3 lastPos;

    // Start is called before the first frame update
    void Start()
    {
        InitialiseVariables();

        boidsController.notifyBoidsPartitionUpdate += FlockValuesUpdated;
        id = boidsController.RegisterBoid(rb, out updateDistance);
    }

    private void InitialiseVariables()
    {
        boidsController = transform.parent.GetComponent<BoidsController>();
        lastPos = transform.position;

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        Vector3 startingVel = new Vector3(Random.value, Random.value, Random.value) * 2;
        startingVel.x -= 1.0f;
        startingVel.y -= 1.0f;
        startingVel.z -= 1.0f;
        rb.velocity = startingVel * Random.Range(1.0f, maxSpeed);

        sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.radius = awarenessRadius;
        sphereCollider.isTrigger = true;
    }

    // Check if boid id is in list of applicable ids.
    // If it is update flock values.
    void FlockValuesUpdated(PartitionData partitionData)
    {
        foreach (int relevantID in partitionData.boidIDs)
        {
            if (id == relevantID)
            {
                // Update stored flock values.
                flockValues = partitionData.flockValues;

                // Recalculate separationVector now that the flockValues have changed.
                separationVector = new Vector3();
                foreach (Vector3 otherBoidPos in partitionData.flockValues.m_posArray)
                {
                    Debug.Log("ran");
                    separationVector += AvoidPoint(otherBoidPos);
                }
                Debug.Log(separationVector);
                return;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(this.name);
        Debug.Log(flockValues.m_avgPos);
        Debug.Log(flockValues.m_avgVel);
        Vector3 newVel = new Vector3();

        newVel += Target();
        Debug.Log(newVel);
        // Flock behaviour.
        newVel += Cohesion(flockValues.m_avgPos);
        Debug.Log(newVel);
        Debug.Log(separationVector);
        newVel += Seperation(separationVector);
        Debug.Log(newVel);
        newVel += Alignment(flockValues.m_avgVel);
        Debug.Log(newVel);

        // Obstacle avoidance.
        if (knownObstacles.Count != 0)
        {
            newVel += AvoidTerrain();
        }
        Debug.Log(newVel);
        // Sets velocity to limited newVel
        rb.velocity = ApplyVelLimits(newVel);

        transform.up = rb.velocity.normalized;

        // Update distance travelled.
        distanceTravelled += Vector3.Distance(transform.position, lastPos);
        lastPos = transform.position;
        if (distanceTravelled > updateDistance)
        {
            distanceTravelled = 0.0f;
            boidsController.UpdateBoidPos(id);
        }
    }

    private Vector3 AvoidTerrain()
    {
        Vector3 avoidanceVector = new Vector3();
        foreach (Collider item in knownObstacles)
        {
            avoidanceVector += AvoidPoint(item.ClosestPoint(transform.position));
        }

        return avoidanceVector.normalized * avoidTerrainWeight;
    }

    private Vector3 ApplyVelLimits(Vector3 newVel)
    {
        newVel = newVel.normalized * Mathf.Clamp(newVel.magnitude, -maxSpeed, maxSpeed);
        newVel = Vector3.RotateTowards(rb.velocity, newVel, turnRate * Mathf.Deg2Rad * Time.deltaTime, acceleration * Time.deltaTime);

        return newVel;
    }

    private Vector3 Target()
    {
        // Vector3 target = avgPos + avgVel;
        // target -= transform.position;
        // return target * targetWeight;

        return rb.velocity.normalized * maxSpeed * targetWeight;
    }

    private Vector3 Alignment(Vector3 avgVel)
    {
        return (avgVel - rb.velocity).normalized * alignmentWeight;
    }

    private Vector3 Seperation(Vector3 separationVector)
    {
        return separationVector * seperationWeight;
    }

    private Vector3 AvoidPoint(Vector3 posToAvoid)
    {
        Vector3 pos = transform.position;
        float dist = Vector3.Distance(posToAvoid, pos);

        if (dist < seperationDistance && dist != 0)
        {
            // Equation is gained through trial and error.
            // return (transform.position - posToAvoid) * (seperationDistance - dist);

            return (pos - posToAvoid) / Mathf.Pow(dist, 2);
        }

        return Vector3.zero;
    }

    private Vector3 Cohesion(Vector3 avgPos)
    {
        return (avgPos - transform.position) * cohesionWeight;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Boid")) knownObstacles.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Boid")) knownObstacles.Remove(other);
    }

    private void OnDrawGizmosSelected()
    {
        // if (debugMode)
        // {
        //     Gizmos.DrawWireSphere(transform.position, awarenessRadius);

        //     Gizmos.color = Color.red;
        //     Gizmos.DrawSphere(Target() + transform.position, targetWeight);

        //     Gizmos.color = Color.green;
        //     Gizmos.DrawSphere(Cohesion(GetFlockAvgPos()) + transform.position, cohesionWeight);

        //     Gizmos.color = Color.yellow;
        //     Gizmos.DrawSphere(Seperation() + transform.position, seperationWeight);
        // }
    }
}