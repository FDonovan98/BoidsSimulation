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
    private float avoidTerrainWeight = 1.0f;


    // Start is called before the first frame update
    void Start()
    {
        InitialiseVariables();
    }

    private void InitialiseVariables()
    {
        rb = GetComponent<Rigidbody>();
        rb.velocity = new Vector3(Random.value, Random.value, Random.value) * Random.Range(1.0f, maxSpeed);
        Debug.Log(rb.velocity);
        rb.useGravity = false;

        sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.radius = awarenessRadius;
        sphereCollider.isTrigger = true;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 newVel = new Vector3();

        if (knownBoids.Count != 0)
        {
            Vector3 flockAvgPos = GetFlockAvgPos();
            Vector3 flockAvgVel = GetFlockAvgVel();

            newVel += Target(flockAvgPos, flockAvgVel);
            newVel += Cohesion(flockAvgPos);
            newVel += Seperation();
            newVel += Alignment(flockAvgVel);
        }

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

    private Vector3 GetFlockAvgVel()
    {
        Vector3 avgVel = new Vector3();

        if (knownBoids.Count == 0) return avgVel;

        foreach (Rigidbody item in knownBoids)
        {
            avgVel += item.velocity;
        }

        return avgVel / knownBoids.Count;
    }

    private Vector3 GetFlockAvgPos()
    {
        Vector3 avgPos = new Vector3();

        if (knownBoids.Count == 0) return avgPos;

        foreach (Rigidbody item in knownBoids)
        {
            avgPos += item.transform.position;
        }

        return avgPos / knownBoids.Count;
    }

    private Vector3 Target(Vector3 avgPos, Vector3 avgVel)
    {
        Vector3 target = avgPos + avgVel;
        target -= transform.position;
        return target * targetWeight;
    }

    private Vector3 Alignment(Vector3 avgVel)
    {
        return (avgVel - rb.velocity).normalized * alignmentWeight;
    }

    private Vector3 Seperation()
    {
        Vector3 seperationVector = new Vector3();

        if (knownBoids.Count == 0) return seperationVector;

        foreach (Rigidbody item in knownBoids)
        {
            seperationVector += AvoidPoint(item.transform.position);
        }

        return seperationVector * seperationWeight;
    }

    private Vector3 AvoidPoint(Vector3 posToAvoid)
    {
        float dist = Vector3.Distance(posToAvoid, transform.position);

        if (dist < seperationDistance)
        {
            // Equation is gained through trial and error.
            // return (transform.position - posToAvoid) * (seperationDistance - dist);

            return (transform.position - posToAvoid) / Mathf.Pow(dist, 2);
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
            knownBoids.Remove(other.GetComponent<Rigidbody>());
        }
        else
        {
            knownObstacles.Remove(other);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (debugMode)
        {
            Gizmos.DrawWireSphere(transform.position, awarenessRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(Target(GetFlockAvgPos(), GetFlockAvgVel()) + transform.position, targetWeight);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(Cohesion(GetFlockAvgPos()) + transform.position, cohesionWeight);

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(Seperation() + transform.position, seperationWeight);
        }
    }
}