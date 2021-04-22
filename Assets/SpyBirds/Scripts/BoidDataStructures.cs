using System.Collections.Generic;
using UnityEngine;

public struct FlockValues
{
    public Vector3 m_avgPos;
    public Vector3 m_avgVel;
    public Vector3[] m_posArray;

    public FlockValues(Vector3 avgPos, Vector3 avgVel, Vector3[] posArray)
    {
        m_avgPos = avgPos;
        m_avgVel = avgVel;
        m_posArray = posArray;
    }
}

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

internal class PartitionCollection
{
    private int numOfPartitions;
    private PartitionData[,,] partitionData;

    private Boid[] boids;

    private List<Vector3Int> partitionsToUpdate = new List<Vector3Int>();

    public PartitionCollection(int numOfPartitions, Boid[] boids)
    {
        this.numOfPartitions = numOfPartitions;
        this.boids = boids;

        partitionData = new PartitionData[numOfPartitions, numOfPartitions, numOfPartitions];
    }

    // TODO: Boid leaving partition collection.
    public void MoveBoid(int boidID, Vector3Int newPartition)
    {
        Vector3Int oldPartition = boids[boidID].partitionID;

        if (partitionData[oldPartition.x, oldPartition.y, oldPartition.z] != null)
        {
            partitionData[oldPartition.x, oldPartition.y, oldPartition.z].UpdateIDList(boidID, true);

            UpdatePartitionQueue(oldPartition);
        }

        if (partitionData[newPartition.x, newPartition.y, newPartition.z] == null)
        {
            partitionData[newPartition.x, newPartition.y, newPartition.z] = new PartitionData(boidID, newPartition, numOfPartitions);
        }

        boids[boidID].partitionID = newPartition;
        partitionData[newPartition.x, newPartition.y, newPartition.z].UpdateIDList(boidID, false);

        UpdatePartitionQueue(newPartition);
    }

    private void UpdatePartitionQueue(Vector3Int partition)
    {
        if (!partitionsToUpdate.Contains(partition)) partitionsToUpdate.Add(partition);
    }

    public void UpdatePartitions()
    {
        if (partitionsToUpdate.Count == 0) return;

        // Update initial flock values.
        for (int i = 0; i < partitionsToUpdate.Count; i++)
        {
            partitionData[partitionsToUpdate[i].x, partitionsToUpdate[i].y, partitionsToUpdate[i].z].UpdateFlockValues(boids);
        }

        for (int i = 0; i < partitionsToUpdate.Count; i++)
        {
            PartitionData tempData = partitionData[partitionsToUpdate[i].x, partitionsToUpdate[i].y, partitionsToUpdate[i].z];
            // Update flock values taking into account neighbouring partitions.
            tempData.CalculateAdjustedFlockValues(partitionData);

            // Update boid target velocities.
            for (int j = 0; j < tempData.boidIDs.Count; j++)
            {
                boids[tempData.boidIDs[j]].UpdateFlockValues(tempData.flockValues, tempData.adjustedFlockValues);
            }
        }

        partitionsToUpdate = new List<Vector3Int>();
    }
}

public class PartitionData
{
    public List<int> boidIDs = new List<int>();
    public FlockValues flockValues;
    public FlockValues adjustedFlockValues;
    static Vector3Int m_partitionID;
    Vector3Int[] neighbouringIDs;

    public PartitionData(int boidID, Vector3Int partitionID, int numPartitions)
    {
        boidIDs = new List<int>();
        boidIDs.Add(boidID);

        m_partitionID = partitionID;

        CalculateNeighbours(partitionID, numPartitions);
    }

    private void CalculateNeighbours(Vector3Int partitionID, int numPartitions)
    {
        List<Vector3Int> neighbours = new List<Vector3Int>();
        if (partitionID.x != 0)
        {
            neighbours.Add(new Vector3Int(partitionID.x - 1, partitionID.y, partitionID.z));
        }
        if (partitionID.x < numPartitions - 1)
        {
            neighbours.Add(new Vector3Int(partitionID.x + 1, partitionID.y, partitionID.z));
        }
        if (partitionID.y != 0)
        {
            neighbours.Add(new Vector3Int(partitionID.x, partitionID.y - 1, partitionID.z));
        }
        if (partitionID.y < numPartitions - 1)
        {
            neighbours.Add(new Vector3Int(partitionID.x, partitionID.y + 1, partitionID.z));
        }
        if (partitionID.z != 0)
        {
            neighbours.Add(new Vector3Int(partitionID.x, partitionID.y, partitionID.z - 1));
        }
        if (partitionID.z < numPartitions - 1)
        {
            neighbours.Add(new Vector3Int(partitionID.x, partitionID.y, partitionID.z + 1));
        }

        neighbouringIDs = neighbours.ToArray();
    }

    // Adjusted flock values take into account the neighbouring partitions.
    public void CalculateAdjustedFlockValues(PartitionData[,,] partitionDatas)
    {
        Vector3 avgPos = flockValues.m_avgPos * boidIDs.Count;
        Vector3 avgVel = flockValues.m_avgVel * boidIDs.Count;
        Vector3[] posArray = flockValues.m_posArray;
        int totalCount = boidIDs.Count;

        foreach (Vector3Int item in neighbouringIDs)
        {
            PartitionData itemPartitionData = partitionDatas[item.x, item.y, item.z];

            if (itemPartitionData != null)
            {
                avgPos += itemPartitionData.flockValues.m_avgPos * itemPartitionData.boidIDs.Count;

                avgVel += itemPartitionData.flockValues.m_avgVel * itemPartitionData.boidIDs.Count;

                totalCount += itemPartitionData.boidIDs.Count;
            }
        }

        adjustedFlockValues = new FlockValues(avgPos / totalCount, avgVel / totalCount, posArray);
    }

    // Update values for this partition.
    public void UpdateFlockValues(Boid[] boids)
    {
        if (boidIDs.Count < 1) return;

        Vector3 avgPos = new Vector3();
        Vector3 avgVel = new Vector3();
        List<Vector3> posArray = new List<Vector3>();

        for (int i = 0; i < boidIDs.Count; i++)
        {
            posArray.Add(boids[boidIDs[i]].lastPos);
            avgPos += boids[boidIDs[i]].lastPos;
            avgVel += boids[boidIDs[i]].vel;
        }

        flockValues = new FlockValues(avgPos / boidIDs.Count, avgVel / boidIDs.Count, posArray.ToArray());
    }

    public void UpdateIDList(int boidID, bool idIsBeingRemoved)
    {
        if (idIsBeingRemoved) boidIDs.Remove(boidID);
        else boidIDs.Add(boidID);
    }
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