﻿using UnityEngine;

public class BoidVariables
{
    public float maxSpeed;
    public float turnRate;
    public float acceleration;
    public Target target;
    public float separationDistance;
    public float targetWeight;
    public float separationWeight;
    public float cohesionWeight;
    public float alignmentWeight;

    public BoidVariables(
        float maxSpeed,
        float turnRate,
        float acceleration,
        Target target,
        float separationDistance,
        float targetWeight,
        float separationWeight,
        float cohesionWeight,
        float alignmentWeight)
    {
        this.maxSpeed = maxSpeed;
        this.turnRate = turnRate;
        this.acceleration = acceleration;
        this.target = target;
        this.separationDistance = separationDistance;
        this.targetWeight = targetWeight;
        this.separationWeight = separationWeight;
        this.cohesionWeight = cohesionWeight;
        this.alignmentWeight = alignmentWeight;
    }

    public float MaxSpeed { get; }
    public float TurnRate { get; }
    public float Acceleration { get; }
    public Target Target { get; }
    public float SeparationDistance { get; }
    public float TargetWeight { get; }
    public float SeparationWeight { get; }
    public float CohesionWeight { get; }
    public float AlignmentWeight { get; }
}


public class Boid
{
    public BoidVariables boidVariables;

    // Identifying info.
    public int ID;
    public Vector3Int partitionID;
    private Transform transform;

    // Calculated values.
    public Vector3 lastPos;
    public Vector3 vel;
    public Vector3 targetVel;
    public FlockValues flockValues;
    private FlockValues adjustedFlockValues;

    public Boid(
        int ID,
        Transform transform,
        Vector3 startPos,
        Vector3Int partitionID,
        Vector3 startVel,
        BoidVariables boidVariables)
    {
        this.ID = ID;
        this.transform = transform;
        this.lastPos = startPos;
        this.partitionID = partitionID;
        this.vel = startVel;
        this.targetVel = startVel;
        this.boidVariables = boidVariables;

        transform.position = lastPos;
    }

    public void RecalculateVelocity(float timeModifier)
    {
        vel = Vector3.RotateTowards(vel, targetVel, boidVariables.turnRate * Mathf.Deg2Rad * timeModifier, boidVariables.acceleration * timeModifier);
    }

    internal void MoveBoid(float timeModifier)
    {
        lastPos += vel * timeModifier;
        transform.position = lastPos;
        transform.up = vel;
    }

    internal void UpdateFlockValues(FlockValues flockValues, FlockValues adjustedFlockValues)
    {
        this.flockValues = flockValues;
        this.adjustedFlockValues = adjustedFlockValues;

        CalculateTargetVelocity();
    }

    private void CalculateTargetVelocity()
    {
        targetVel = new Vector3();

        // Boid steering behaviours.
        targetVel += Target();

        targetVel += Cohesion(flockValues.m_avgPos);

        targetVel += Separation();

        targetVel += Alignment(flockValues.m_avgVel);

        targetVel = Vector3.ClampMagnitude(targetVel, boidVariables.maxSpeed);
    }

    // Move to a specific target, or continue in the current direction if there is no target.
    private Vector3 Target()
    {
        if (boidVariables.target != null)
        {
            return (boidVariables.target.lastPos - lastPos).normalized * boidVariables.maxSpeed * boidVariables.targetWeight;
        }

        return vel.normalized * boidVariables.maxSpeed * boidVariables.targetWeight;
    }

    // Align own velocity with the flocks velocity.
    private Vector3 Alignment(Vector3 avgVel)
    {
        return (avgVel - vel).normalized * boidVariables.alignmentWeight;
    }

    // Maintain distance from other boids in the flock.
    private Vector3 Separation()
    {
        Vector3 separationVector = new Vector3();

        if (flockValues.m_posArray == null) return Vector3.zero;

        for (int i = 0; i < flockValues.m_posArray.Length; i++)
        {
            separationVector += AvoidPoint(flockValues.m_posArray[i]);
        }

        return separationVector * boidVariables.separationWeight;
    }

    private Vector3 AvoidPoint(Vector3 posToAvoid)
    {
        float dist = Vector3.Distance(posToAvoid, lastPos);

        if (dist < boidVariables.separationDistance && dist != 0)
        {
            return (lastPos - posToAvoid).normalized / Mathf.Pow(dist, 2);
        }

        return Vector3.zero;
    }

    // Moves the boid to the centre of the flock.
    private Vector3 Cohesion(Vector3 avgPos)
    {
        return (avgPos - lastPos).normalized * boidVariables.cohesionWeight;
    }
}