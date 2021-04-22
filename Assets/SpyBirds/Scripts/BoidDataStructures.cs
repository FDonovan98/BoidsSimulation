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

// public class PartitionData
// {
//     public List<int> boidIDs = new List<int>();
//     public FlockValues flockValues;
//     public FlockValues adjustedFlockValues;
//     static Vector3Int m_partitionID;
//     Vector3Int[] neighbouringIDs;

//     public PartitionData(int boidID, Vector3Int partitionID, int numPartitions)
//     {
//         boidIDs = new List<int>();
//         boidIDs.Add(boidID);

//         m_partitionID = partitionID;

//         CalculateNeighbours(partitionID, numPartitions);
//     }

//     private void CalculateNeighbours(Vector3Int partitionID, int numPartitions)
//     {
//         List<Vector3Int> neighbours = new List<Vector3Int>();
//         if (partitionID.x != 0)
//         {
//             neighbours.Add(new Vector3Int(partitionID.x - 1, partitionID.y, partitionID.z));
//         }
//         if (partitionID.x < numPartitions - 1)
//         {
//             neighbours.Add(new Vector3Int(partitionID.x + 1, partitionID.y, partitionID.z));
//         }
//         if (partitionID.y != 0)
//         {
//             neighbours.Add(new Vector3Int(partitionID.x, partitionID.y - 1, partitionID.z));
//         }
//         if (partitionID.y < numPartitions - 1)
//         {
//             neighbours.Add(new Vector3Int(partitionID.x, partitionID.y + 1, partitionID.z));
//         }
//         if (partitionID.z != 0)
//         {
//             neighbours.Add(new Vector3Int(partitionID.x, partitionID.y, partitionID.z - 1));
//         }
//         if (partitionID.z < numPartitions - 1)
//         {
//             neighbours.Add(new Vector3Int(partitionID.x, partitionID.y, partitionID.z + 1));
//         }

//         neighbouringIDs = neighbours.ToArray();
//     }

//     // Adjusted flock values take into account the neighbouring partitions.
//     public void CalculateAdjustedFlockValues(PartitionData[,,] partitionDatas)
//     {
//         Vector3 avgPos = flockValues.m_avgPos * boidIDs.Count;
//         Vector3 avgVel = flockValues.m_avgVel * boidIDs.Count;
//         Vector3[] posArray = flockValues.m_posArray;
//         int totalCount = boidIDs.Count;

//         foreach (Vector3Int item in neighbouringIDs)
//         {
//             PartitionData itemPartitionData = partitionDatas[item.x, item.y, item.z];

//             if (itemPartitionData != null)
//             {
//                 avgPos += itemPartitionData.flockValues.m_avgPos * itemPartitionData.boidIDs.Count;

//                 avgVel += itemPartitionData.flockValues.m_avgVel * itemPartitionData.boidIDs.Count;

//                 totalCount += itemPartitionData.boidIDs.Count;
//             }
//         }

//         adjustedFlockValues = new FlockValues(avgPos / totalCount, avgVel / totalCount, posArray);
//     }

//     // Update values for this partition.
//     public void UpdateFlockValues(BoidData[] boidData)
//     {
//         if (boidIDs.Count < 1) return;

//         Vector3 avgPos = new Vector3();
//         Vector3 avgVel = new Vector3();
//         List<Vector3> posArray = new List<Vector3>();

//         for (int i = 0; i < boidIDs.Count; i++)
//         {
//             posArray.Add(boidData[boidIDs[i]].m_boidScript.lastPos);
//             avgPos += boidData[boidIDs[i]].m_boidScript.lastPos;
//             avgVel += boidData[boidIDs[i]].m_boidScript.velocity;
//         }

//         flockValues = new FlockValues(avgPos / boidIDs.Count, avgVel / boidIDs.Count, posArray.ToArray());
//     }

//     public void UpdateIDList(int boidID, bool idIsBeingRemoved)
//     {
//         if (idIsBeingRemoved) boidIDs.Remove(boidID);
//         else boidIDs.Add(boidID);
//     }
// }

// public struct BoidData
// {
//     public int m_id;
//     public Boids m_boidScript;
//     public Vector3Int m_partitionID;

//     public BoidData(int id, Boids boidScript)
//     {
//         m_id = id;
//         m_boidScript = boidScript;
//         m_partitionID = Vector3Int.zero;
//     }

//     public BoidData(int id, Boids boidScript, Vector3Int partition)
//     {
//         m_id = id;
//         m_boidScript = boidScript;
//         m_partitionID = partition;
//     }
// }
