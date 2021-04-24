using System.Collections.Generic;
using UnityEngine;

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

    // TODO: Add function to recalculate all relative locations stored in pointToAvoidDict variable.
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
    private Vector3Int m_partitionID;
    private Vector3Int[] neighbouringIDs;
    // Location, isLocationRelative.
    private Dictionary<Vector3, bool> pointToAvoidDict = new Dictionary<Vector3, bool>();

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