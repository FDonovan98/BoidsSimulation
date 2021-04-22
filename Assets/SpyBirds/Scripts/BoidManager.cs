using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoidManager : MonoBehaviour
{
    [Header("Partition Variables")]
    [SerializeField]
    float partitionLength = 20.0f;
    [SerializeField]
    int numOfPartitions = 70;

    [Header("Boid Variables")]
    // Movement variables.
    [SerializeField]
    private float maxSpeed = 10.0f;
    [SerializeField]
    private float turnRate = 1.0f;
    [SerializeField]
    private float acceleration = 1.0f;

    // Steering variables.
    [SerializeField]
    private Target target = null;
    [SerializeField]
    private float separationDistance = 4.0f;

    // Steering weights.
    [SerializeField]
    private float targetWeight = 1.0f;
    [SerializeField]
    private float separationWeight = 1.0f;
    [SerializeField]
    private float cohesionWeight = 1.0f;
    [SerializeField]
    private float alignmentWeight = 1.0f;

    // Internal use.
    private Boid[] boids;
    PartitionCollection partitionCollection;

    private void Start()
    {
        BuildAllBoids();
        BuildPartitionStructure();
    }

    private void BuildAllBoids()
    {
        // TODO: (Option to) Spawn in boids instead.
        Transform[] boidObjects = this.GetComponentsInChildren<Transform>();
        boids = new Boid[boidObjects.Length];

        for (int i = 0; i < boids.Length; i++)
        {
            // TODO: Set start position.
            Vector3 startPos = Vector3.zero;

            boids[i] = new Boid(boidObjects[i],
                startPos,
                CalculatePartition(startPos),
                CalculateStartVel(),
                maxSpeed,
                turnRate,
                acceleration,
                target,
                separationDistance,
                targetWeight,
                separationWeight,
                cohesionWeight,
                alignmentWeight);
        }
    }

    private void BuildPartitionStructure()
    {
        partitionCollection = new PartitionCollection(numOfPartitions, boids);
    }

    private Vector3 CalculateStartVel()
    {
        Vector3 startVel = new Vector3(Random.value, Random.value, Random.value);
        return startVel = startVel * Random.Range(-maxSpeed, maxSpeed);
    }

    private Vector3Int CalculatePartition(Vector3 boidPos)
    {
        // Calculate partition number relative to controller position.
        Vector3 partitionFloat = (transform.position - boidPos) / partitionLength;
        // Recentre so controller position is at the centre of the partitions.
        partitionFloat += new Vector3(numOfPartitions / 2, numOfPartitions / 2, numOfPartitions / 2);
        Vector3Int partition = Vector3Int.FloorToInt(partitionFloat);

        // Check partition value is within allowed range.
        if (partition.x > numOfPartitions || partition.y > numOfPartitions || partition.z > numOfPartitions)
        {
            if (partition.x < 0 || partition.y < 0 || partition.z < 0)
            {
                Debug.LogError("Boid is outside of partition range");
                return new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            }
        }

        return partition;
    }

    private void Update()
    {
        UpdateBoid();
        partitionCollection.UpdatePartitions();
    }

    private void UpdateBoid()
    {
        foreach (Boid item in boids)
        {
            item.RecalculateVelocity();
            item.MoveBoid(Time.deltaTime);
            UpdateBoidPartition(item);
        }
    }

    private void UpdateBoidPartition(Boid boid)
    {
        Vector3Int partition = CalculatePartition(boid.lastPos);
        if (partition == boid.partitionID) return;

        partitionCollection.MoveBoid(boid.ID, partition);
    }
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

        for (int i = 0; i < partitionsToUpdate.Count; i++)
        {
            partitionData[partitionsToUpdate[i].x, partitionsToUpdate[i].y, partitionsToUpdate[i].z].UpdateFlockValues(boids);
        }

        for (int i = 0; i < partitionsToUpdate.Count; i++)
        {
            partitionData[partitionsToUpdate[i].x, partitionsToUpdate[i].y, partitionsToUpdate[i].z].CalculateAdjustedFlockValues(partitionData);
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
    // Movement variables.
    private float maxSpeed;
    private float turnRate;
    private float acceleration;

    // Steering variables.
    private Target target;
    private float separationDistance;

    // Steering weights.
    private float targetWeight;
    private float separationWeight;
    private float cohesionWeight;
    private float alignmentWeight;

    // Identifying info.
    public int ID;
    public Vector3Int partitionID;
    private Transform transform;

    // Calculated values.
    public Vector3 lastPos;
    public Vector3 vel;
    private Vector3 targetVel;

    public Boid(
        Transform transform,
        Vector3 startPos,
        Vector3Int partitionID,
        Vector3 startVel,
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
        this.transform = transform;
        this.lastPos = startPos;
        this.partitionID = partitionID;
        this.vel = startVel;
        this.targetVel = startVel;
        this.maxSpeed = maxSpeed;
        this.turnRate = turnRate;
        this.acceleration = acceleration;
        this.target = target;
        this.separationDistance = separationDistance;
        this.targetWeight = targetWeight;
        this.separationWeight = separationWeight;
        this.cohesionWeight = cohesionWeight;
        this.alignmentWeight = alignmentWeight;

        transform.position = lastPos;
    }

    public void RecalculateVelocity()
    {
        vel = Vector3.RotateTowards(vel, targetVel, turnRate * Mathf.Deg2Rad, acceleration);
    }

    internal void MoveBoid(float timeModifier)
    {
        lastPos += vel * timeModifier;
        transform.position = lastPos;
        transform.up = vel;
    }
}