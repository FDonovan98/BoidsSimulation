// Author: Harry Donovan
// Collaborators:
// License: GNU General Public License v3.0
// References:
// Description: Controller class for boid flocks. Should handle spatial partitioning.

using System.Collections.Generic;
using UnityEngine;

public class BoidsController : MonoBehaviour
{
    [Header("Boid List")]
    [SerializeField]
    const int maxBoids = 300;
    List<int> availableIndex = new List<int>();
    BoidData[] boids = new BoidData[300];

    [Header("Partition")]
    [SerializeField]
    float partitionLength = 10.0f;
    [SerializeField]
    const int partitionNumber = 10;
    PartitionData[,,] partitions = new PartitionData[partitionNumber, partitionNumber, partitionNumber];

    private void Start()
    {
        for (int i = 0; i < maxBoids; i++)
        {
            availableIndex.Add(i);
        }
    }

    // Called by boids to register themselves with the BoidController.
    // Returns int.MaxValue if boid list is full.
    public int RegisterBoid(GameObject boid)
    {
        if (availableIndex.Count > 0)
        {
            int id = availableIndex[0];
            availableIndex.RemoveAt(0);

            boids[id] = new BoidData();
            UpdateBoidPos(id);
            return id;
        }

        Debug.LogError("Boid count exceeded");
        return int.MaxValue;
    }

    public void UpdateBoidPos(int boidID)
    {
        Vector3Int partition = CalculatePartition(boids[boidID].m_rb.position);

        if (partition == boids[boidID].m_partitionID) return;

        UpdatePartitions(boidID, partition);
    }

    public FlockValues RequestFlockValues(int boidID)
    {
        return partitions[boids[boidID].m_partitionID.x, boids[boidID].m_partitionID.y, boids[boidID].m_partitionID.z].flockValues;
    }

    // Currently partition grid is always based off of 0, 0, 0 as origin.
    // Could be improved to use contoller coords as origin.
    private Vector3Int CalculatePartition(Vector3 boidPos)
    {
        Vector3 partitionFloat = boidPos / partitionLength;
        Vector3Int partition = Vector3Int.FloorToInt(partitionFloat);

        if ((partition.x | partition.y | partition.z) > partitionNumber)
        {
            Debug.LogError("Boid is outside of partition range");
            return new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        }

        return partition;
    }


    // On initilisation will be run every time a boid is added.
    // Could be optimised so for initilisation it is only run once all boids are added.
    // Currently only updates directly effected partitions as neighbouring partitions aren't taken into account when calculating flock values.
    // This should be changed.
    private void UpdatePartitions(int boidID, Vector3Int newPartition)
    {
        if (boids[boidID].m_partitionID == newPartition) return;

        // Update old partition, removing boid from id list.
        partitions[boids[boidID].m_partitionID.x, boids[boidID].m_partitionID.y, boids[boidID].m_partitionID.z].UpdateData(boids, boidID, true);

        // Update new partition, adding boid to id list.
        partitions[newPartition.x, newPartition.y, newPartition.z].UpdateData(boids, boidID, false);
    }
}

public struct FlockValues
{
    public Vector3 m_avgPos;
    public Vector3 m_avgVel;

    public FlockValues(Vector3 avgPos, Vector3 avgVel)
    {
        m_avgPos = avgPos;
        m_avgVel = avgVel;
    }
}

struct PartitionData
{
    public List<int> boidIDs;
    public FlockValues flockValues;

    public void UpdateData(BoidData[] boidData)
    {
        if (boidIDs.Count < 1) return;

        Vector3 avgPos = new Vector3();
        Vector3 avgVel = new Vector3();

        foreach (int id in boidIDs)
        {
            avgPos += boidData[id].m_rb.position;
            avgVel += boidData[id].m_rb.velocity;
        }

        flockValues = new FlockValues(avgPos / boidIDs.Count, avgVel / boidIDs.Count);
    }

    public void UpdateData(BoidData[] boidData, int boidID, bool idIsBeingRemoved)
    {
        if (idIsBeingRemoved) boidIDs.Remove(boidID);
        else boidIDs.Add(boidID);

        UpdateData(boidData);
    }
}

public struct BoidData
{
    public int m_id;
    public Rigidbody m_rb;
    public Vector3Int m_partitionID;

    public BoidData(int id, Rigidbody rb)
    {
        m_id = id;
        m_rb = rb;
        m_partitionID = Vector3Int.zero;
    }
}