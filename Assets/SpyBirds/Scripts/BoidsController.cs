// // Author: Harry Donovan
// // Collaborators:
// // License: GNU General Public License v3.0
// // References:
// // Description: Controller class for boid flocks. Should handle spatial partitioning.

// using System;
// using System.Collections.Concurrent;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using UnityEngine;
// using UnityEngine.UI;

// public class BoidsController : MonoBehaviour
// {
//     [Header("Boid List")]
//     [SerializeField]
//     int maxBoids = 1500;
//     List<int> availableIndex = new List<int>();
//     BoidData[] boidData;

//     [Header("Partition")]
//     [SerializeField]
//     float partitionLength = 20.0f;
//     [SerializeField]
//     int partitionNumber = 70;
//     [SerializeField]
//     int agentUpdatesPerFrame = 100;
//     int lastAgentUpdated = 0;
//     PartitionData[,,] partitions;

//     [SerializeField]
//     Text text;

//     // ConcurrentQueue is thread safe, allowing us to use multithreading.
//     ConcurrentQueue<UpdatePartitionQueue> updatePartQueue = new ConcurrentQueue<UpdatePartitionQueue>();
//     List<UpdatePartitionQueue> updatePartitionIndex = new List<UpdatePartitionQueue>();
//     public delegate void NotifyBoidsPartitionUpdate(PartitionData partitionData);
//     public NotifyBoidsPartitionUpdate notifyBoidsPartitionUpdate;

//     private void Awake()
//     {
//         InitialiseVariables();
//     }

//     private void InitialiseVariables()
//     {
//         partitions = new PartitionData[partitionNumber, partitionNumber, partitionNumber];
//         boidData = new BoidData[maxBoids];

//         for (int i = 0; i < maxBoids; i++)
//         {
//             availableIndex.Add(i);
//         }
//     }

//     // Updates partition data if there are pending updates.
//     // Then triggers target velocity recalculations on the boids.
//     private void Update()
//     {
//         UpdatePartitionIDLists();

//         UpdatePartitionFlockData();

//         // text.text = updatePartQueue.Count.ToString();

//         for (int i = 0; i < agentUpdatesPerFrame; i++)
//         {
//             lastAgentUpdated++;
//             if (lastAgentUpdated >= boidData.Length || boidData[lastAgentUpdated].m_boidScript == null)
//             {
//                 lastAgentUpdated = 0;
//             }

//             boidData[lastAgentUpdated].m_boidScript.RecalculateVelocity();
//         }
//     }

//     // Recalculates flock values for queued partitions and triggers notifyBoidsPartitionUpdate delegate so boids are aware of updated data.
//     private async void UpdatePartitionFlockData()
//     {
//         await Task.Run(() =>
//         {
//             do
//             {
//                 if (updatePartQueue.Count == 0) return;

//                 UpdatePartitionQueue updateQueue;
//                 if (updatePartQueue.TryDequeue(out updateQueue))
//                 {
//                     partitions[updateQueue.m_partitionID.x, updateQueue.m_partitionID.y, updateQueue.m_partitionID.z].UpdateFlockValues(boidData);

//                     partitions[updateQueue.m_partitionID.x, updateQueue.m_partitionID.y, updateQueue.m_partitionID.z].CalculateAdjustedFlockValues(partitions);

//                     // Delegate call and list modification needs to be done on main thread due to errors being thrown.
//                     // I think.
//                     notifyBoidsPartitionUpdate(partitions[updateQueue.m_partitionID.x, updateQueue.m_partitionID.y, updateQueue.m_partitionID.z]);
//                 }

//             } while (true);
//         });
//     }

//     // Iterates through updatePartitionIndex to update all partitions with boid id's moving between partitions.
//     private void UpdatePartitionIDLists()
//     {
//         for (int i = 0; i < updatePartitionIndex.Count; i++)
//         {
//             partitions[updatePartitionIndex[i].m_partitionID.x, updatePartitionIndex[i].m_partitionID.y, updatePartitionIndex[i].m_partitionID.z].UpdateIDList(updatePartitionIndex[i].m_boidID, updatePartitionIndex[i].m_removeID);

//             updatePartitionIndex.RemoveAt(i);
//             i++;
//         }
//     }

//     // Called by boidData to register themselves with the BoidController.
//     // Returns int.MaxValue if boid list is full.
//     public int RegisterBoid(Boids boidScript, out float updateDistance)
//     {
//         if (availableIndex.Count > 0)
//         {
//             int id = availableIndex[0];
//             availableIndex.RemoveAt(0);

//             boidData[id] = new BoidData(id, boidScript);
//             UpdateBoidPos(id, true);
//             updateDistance = partitionLength / 2;
//             return id;
//         }

//         Debug.LogError("Boid count exceeded");
//         updateDistance = float.MaxValue;
//         return int.MaxValue;
//     }

//     // Called by boids once they have moved their updateDistance.
//     // Recalculates the boids partition and queues a partition update if it has changed partitions.
//     public void UpdateBoidPos(int boidID, bool isInitialisation = false)
//     {
//         Vector3Int partition = CalculatePartition(boidData[boidID].m_boidScript.lastPos);

//         // On initialisation boid cannot be unregistered for partitionData ID list as it has not yet been added to any.
//         if (isInitialisation)
//         {
//             // This should be refactored into a standalone function as is duplicated code.
//             boidData[boidID].m_partitionID = partition;

//             if (partitions[partition.x, partition.y, partition.z] == null)
//             {
//                 partitions[partition.x, partition.y, partition.z] = new PartitionData(boidID, partition, partitionNumber);
//             }
//             else
//             {
//                 QueueUpdateData(partition, boidID, false);
//                 return;
//             }
//         }
//         else
//         {
//             UpdatePartitions(boidID, partition);
//         }
//     }

//     // Currently partition grid is always based off of 0, 0, 0 as origin.
//     // Could be improved to use contoller coords as origin.
//     private Vector3Int CalculatePartition(Vector3 boidPos)
//     {
//         // Calculate partition number relative to controller position.
//         Vector3 partitionFloat = (transform.position - boidPos) / partitionLength;
//         // Recentre so controller position is at the centre of the partitions.
//         partitionFloat += new Vector3(partitionNumber / 2, partitionNumber / 2, partitionNumber / 2);
//         Vector3Int partition = Vector3Int.FloorToInt(partitionFloat);

//         // Check partition value is within allowed range.
//         if (partition.x > partitionNumber || partition.y > partitionNumber || partition.z > partitionNumber)
//         {
//             if (partition.x < 0 || partition.y < 0 || partition.z < 0)
//             {
//                 Debug.LogError("Boid is outside of partition range");
//                 return new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
//             }
//         }

//         return partition;
//     }

//     // Queue's a new partition update.
//     // On initialisation will be run every time a boid is added.
//     // Could be optimised so for initialisation it is only run once all boidData are added.
//     private void UpdatePartitions(int boidID, Vector3Int newPartition)
//     {
//         // When the partition hasn't changed we just need to update it's current partition.
//         if (boidData[boidID].m_partitionID == newPartition)
//         {
//             QueueUpdateData(boidData[boidID].m_partitionID);
//             return;
//         }

//         // Queue an update to remove boid from id list for old partition.
//         QueueUpdateData(boidData[boidID].m_partitionID, boidID, true);

//         // Queue an update to add boid to id list for new partition.
//         // Creates the partition if it doesn't yet exist.
//         if (partitions[newPartition.x, newPartition.y, newPartition.z] == null)
//         {
//             partitions[newPartition.x, newPartition.y, newPartition.z] = new PartitionData(boidID, newPartition, partitionNumber);
//         }
//         else
//         {
//             QueueUpdateData(newPartition, boidID, false);
//         }
//     }

//     // Queue's an update for a specific partition.
//     void QueueUpdateData(Vector3Int partitionID)
//     {
//         updatePartQueue.Enqueue(new UpdatePartitionQueue(partitionID));
//     }

//     // Queue's an ID list update for the partition, then queue's an update for the partition data.
//     void QueueUpdateData(Vector3Int partitionID, int boidID, bool idIsBeingRemoved)
//     {
//         updatePartitionIndex.Add(new UpdatePartitionQueue(partitionID, boidID, idIsBeingRemoved));
//         QueueUpdateData(partitionID);
//     }

//     private void OnDrawGizmosSelected()
//     {
//         Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 0.1f);

//         Vector3 startPos = transform.position - new Vector3(partitionNumber / 2 * partitionLength, partitionNumber / 2 * partitionLength, partitionNumber / 2 * partitionLength);
//         for (int x = 0; x < partitionNumber; x++)
//         {
//             for (int y = 0; y < partitionNumber; y++)
//             {
//                 for (int z = 0; z < partitionNumber; z++)
//                 {

//                     Gizmos.DrawWireCube(startPos + new Vector3(x * partitionLength + partitionLength / 2, y * partitionLength + partitionLength / 2, z * partitionLength + partitionLength / 2), new Vector3(partitionLength, partitionLength, partitionLength)
//                     );
//                 }
//             }
//         }
//     }
// }

// struct UpdatePartitionQueue
// {
//     public Vector3Int m_partitionID;
//     public int m_boidID;
//     public bool m_removeID;

//     public UpdatePartitionQueue(Vector3Int partitionID, int boidID = int.MaxValue, bool removeID = false)
//     {
//         m_partitionID = partitionID;
//         m_boidID = boidID;
//         m_removeID = removeID;
//     }
// }