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
    private List<Rigidbody> knownBoids = new List<Rigidbody>();
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
    bool flockChanged;
    Vector3 flockAvgPos;
    Vector3 flockAvgVel;
    Vector3 separationVector;

    // Start is called before the first frame update
    void Start()
    {
        InitialiseVariables();
    }

    private void InitialiseVariables()
    {
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

    // Update is called once per frame
    void Update()
    {
        Vector3 newVel = new Vector3();

        newVel += Target();

        // Flock behaviour.
        if (knownBoids.Count != 0)
        {
            if (flockChanged)
            {
                GetFlockStats(out flockAvgPos, out flockAvgVel, out separationVector);
                flockChanged = false;
            }

            newVel += Cohesion(flockAvgPos);
            newVel += Seperation(separationVector);
            newVel += Alignment(flockAvgVel);
        }

        // Obstacle avoidance.
        if (knownObstacles.Count != 0)
        {
            newVel += AvoidTerrain();
        }

        // Sets velocity to limited newVel
        rb.velocity = ApplyVelLimits(newVel);

        transform.up = rb.velocity.normalized;
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

    // Calculates flock average position and velocity.
    private void GetFlockStats(out Vector3 avgPos, out Vector3 avgVel, out Vector3 separationVector)
    {
        avgPos = new Vector3();
        avgVel = new Vector3();
        separationVector = new Vector3();

        if (knownBoids.Count == 0) return;

        foreach (Rigidbody item in knownBoids)
        {
            Vector3 itemPos = item.transform.position;
            avgVel += item.velocity;
            avgPos += itemPos;
            separationVector += AvoidPoint(itemPos);
        }

        avgPos /= knownBoids.Count;
        avgVel /= knownBoids.Count;
        // separationVector /= knownBoids.Count;
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

        if (dist < seperationDistance)
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
        if (other.CompareTag("Boid"))
        {
            knownBoids.Add(other.GetComponent<Rigidbody>());
            flockChanged = true;
        }
        else
        {
            knownObstacles.Add(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Boid"))
        {
            flockChanged = true;
            knownBoids.Remove(other.GetComponent<Rigidbody>());
        }
        else
        {
            knownObstacles.Remove(other);
        }
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