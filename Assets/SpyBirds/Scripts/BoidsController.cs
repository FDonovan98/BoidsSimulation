// Author: Harry Donovan
// Collaborators:
// License: GNU General Public License v3.0
// References:
// Description: Controller class for boid flocks. Should handle spatial partitioning.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class BoidsController : MonoBehaviour
{
    [Header("Boid List")]
    [SerializeField]
    int maxBoids = 1500;
    List<int> availableIndex = new List<int>();
    List<BoidData> boidData = new List<BoidData>();

    [Header("Partition")]
    [SerializeField]
    float partitionLength = 20.0f;
    [SerializeField]
    int partitionNumber = 70;
    [SerializeField]
    int agentUpdatesPerFrame = 100;
    int lastAgentUpdated = 0;
    PartitionData[,,] partitions;

    [SerializeField]
    Text text;

    ConcurrentQueue<UpdatePartitionQueue> updatePartQueue = new ConcurrentQueue<UpdatePartitionQueue>();
    List<UpdatePartitionQueue> updatePartitionIndex = new List<UpdatePartitionQueue>();

    // Update flock values delegate.
    public delegate void NotifyBoidsPartitionUpdate(PartitionData partitionData);
    public NotifyBoidsPartitionUpdate notifyBoidsPartitionUpdate;

    private void Awake()
    {
        partitions = new PartitionData[partitionNumber, partitionNumber, partitionNumber];

        for (int i = 0; i < maxBoids; i++)
        {
            availableIndex.Add(i);
        }
    }

    // Iterates through updatePartitionQueue and updatePartitionIndex.
    // Will need to call notifyBoidsPartitionUpdate for each updatePartitionQueue.
    private void Update()
    {
        UpdatePartitionIDLists();

        UpdatePartitionFlockData();

        text.text = updatePartQueue.Count.ToString();

        for (int i = 0; i < agentUpdatesPerFrame; i++)
        {
            lastAgentUpdated++;
            if (lastAgentUpdated >= boidData.Count)
            {
                lastAgentUpdated = 0;
            }

            Debug.Log(boidData[lastAgentUpdated]);
            Debug.Log(lastAgentUpdated);
            boidData[lastAgentUpdated].m_boidScript.RecalculateVelocity();
        }
    }

    // May create multiple Tasks which check same positions.
    private async void UpdatePartitionFlockData()
    {

        await Task.Run(() =>
        {
            int i = 0;
            do
            {
                if (updatePartQueue.Count == 0) return;

                UpdatePartitionQueue updateQueue;
                if (updatePartQueue.TryDequeue(out updateQueue))
                {
                    i++;
                    partitions[updateQueue.m_partitionID.x, updateQueue.m_partitionID.y, updateQueue.m_partitionID.z].UpdateFlockValues(boidData);

                    partitions[updateQueue.m_partitionID.x, updateQueue.m_partitionID.y, updateQueue.m_partitionID.z].CalculateAdjustedFlockValues(partitions);

                    // Delegate call and list modification needs to be done on main thread due to errors being thrown.
                    // I think.
                    notifyBoidsPartitionUpdate(partitions[updateQueue.m_partitionID.x, updateQueue.m_partitionID.y, updateQueue.m_partitionID.z]);
                }

            } while (true);
        });
    }

    private void UpdatePartitionIDLists()
    {
        // await Task.Run(() =>
        // {

        for (int i = 0; i < updatePartitionIndex.Count; i++)
        {
            partitions[updatePartitionIndex[i].m_partitionID.x, updatePartitionIndex[i].m_partitionID.y, updatePartitionIndex[i].m_partitionID.z].UpdateIDList(updatePartitionIndex[i].m_boidID, updatePartitionIndex[i].m_removeID);

            updatePartitionIndex.RemoveAt(i);
            i--;
        }

        // });
    }

    // Called by boidData to register themselves with the BoidController.
    // Returns int.MaxValue if boid list is full.
    public int RegisterBoid(Boids boidScript, out float updateDistance)
    {
        if (availableIndex.Count > 0)
        {
            int id = availableIndex[0];
            availableIndex.RemoveAt(0);

            boidData.Add(new BoidData(id, boidScript));
            UpdateBoidPos(id, true);
            updateDistance = partitionLength / 2;
            return id;
        }

        Debug.LogError("Boid count exceeded");
        updateDistance = float.MaxValue;
        return int.MaxValue;
    }

    public void UpdateBoidPos(int boidID, bool isInitialisation = false)
    {
        Vector3Int partition = CalculatePartition(boidData[boidID].m_boidScript.lastPos);

        // On initialisation boid cannot be unregistered for partitionData ID list as it has not yet been added to any.
        if (isInitialisation)
        {
            // This should be refactored into a standalone function as is duplicated code.
            boidData[boidID] = new BoidData(boidID, boidData[boidID].m_boidScript, partition);

            if (partitions[partition.x, partition.y, partition.z] == null)
            {
                partitions[partition.x, partition.y, partition.z] = new PartitionData(boidID, partition, partitionNumber);
            }
            else
            {
                QueueUpdateData(partition, boidID, false);
                return;
            }
        }
        else
        {
            UpdatePartitions(boidID, partition);
        }
    }

    // Currently partition grid is always based off of 0, 0, 0 as origin.
    // Could be improved to use contoller coords as origin.
    private Vector3Int CalculatePartition(Vector3 boidPos)
    {
        // Calculate partition number relative to this.position.
        Vector3 partitionFloat = (transform.position - boidPos) / partitionLength;
        // Recenter so this.position is at the centre of the partitions.
        partitionFloat += new Vector3(partitionNumber / 2, partitionNumber / 2, partitionNumber / 2);
        Vector3Int partition = Vector3Int.FloorToInt(partitionFloat);

        // Check partition value is within allowed range.
        if (partition.x > partitionNumber || partition.y > partitionNumber || partition.z > partitionNumber)
        {
            if (partition.x < 0 || partition.y < 0 || partition.z < 0)
            {
                Debug.LogError("Boid is outside of partition range");
                return new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            }
        }

        return partition;
    }


    // On initilisation will be run every time a boid is added.
    // Could be optimised so for initilisation it is only run once all boidData are added.

    private void UpdatePartitions(int boidID, Vector3Int newPartition)
    {
        Vector3Int partitionID = new Vector3Int(boidData[boidID].m_partitionID.x, boidData[boidID].m_partitionID.y, boidData[boidID].m_partitionID.z);

        if (boidData[boidID].m_partitionID == newPartition)
        {
            QueueUpdateData(partitionID);
            return;
        }

        // Update old partition, removing boid from id list.
        // Notify subscribed boidData partitions have updated

        QueueUpdateData(partitionID, boidID, true);

        // This should be refactored into a standalone function as is duplicated code.
        // Update new partition, adding boid to id list.
        // Notify subscribed boidData partitions have updated
        boidData[boidID] = new BoidData(boidID, boidData[boidID].m_boidScript, partitionID);
        if (partitions[newPartition.x, newPartition.y, newPartition.z] == null)
        {
            partitions[newPartition.x, newPartition.y, newPartition.z] = new PartitionData(boidID, newPartition, partitionNumber);
        }
        else
        {
            QueueUpdateData(newPartition, boidID, false);
        }
    }

    void QueueUpdateData(Vector3Int partitionID)
    {
        updatePartQueue.Enqueue(new UpdatePartitionQueue(partitionID));
    }

    void QueueUpdateData(Vector3Int partitionID, int boidID, bool idIsBeingRemoved)
    {
        updatePartitionIndex.Add(new UpdatePartitionQueue(partitionID, boidID, idIsBeingRemoved));
        QueueUpdateData(partitionID);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 0.1f);

        Vector3 startPos = transform.position - new Vector3(partitionNumber / 2 * partitionLength, partitionNumber / 2 * partitionLength, partitionNumber / 2 * partitionLength);
        for (int x = 0; x < partitionNumber; x++)
        {
            for (int y = 0; y < partitionNumber; y++)
            {
                for (int z = 0; z < partitionNumber; z++)
                {

                    Gizmos.DrawWireCube(startPos + new Vector3(x * partitionLength + partitionLength / 2, y * partitionLength + partitionLength / 2, z * partitionLength + partitionLength / 2), new Vector3(partitionLength, partitionLength, partitionLength)
                    );
                }
            }
        }
    }
}

struct UpdatePartitionQueue
{
    public Vector3Int m_partitionID;
    public int m_boidID;
    public bool m_removeID;

    public UpdatePartitionQueue(Vector3Int partitionID, int boidID = int.MaxValue, bool removeID = false)
    {
        m_partitionID = partitionID;
        m_boidID = boidID;
        m_removeID = removeID;
    }
}

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

    public void UpdateFlockValues(List<BoidData> boidData)
    {
        if (boidIDs.Count < 1) return;

        Vector3 avgPos = new Vector3();
        Vector3 avgVel = new Vector3();
        List<Vector3> posArray = new List<Vector3>();

        for (int i = 0; i < boidIDs.Count; i++)
        {
            posArray.Add(boidData[boidIDs[i]].m_boidScript.lastPos);
            avgPos += boidData[boidIDs[i]].m_boidScript.lastPos;
            avgVel += boidData[boidIDs[i]].m_boidScript.velocity;
        }

        flockValues = new FlockValues(avgPos / boidIDs.Count, avgVel / boidIDs.Count, posArray.ToArray());
    }

    public void UpdateIDList(int boidID, bool idIsBeingRemoved)
    {
        if (idIsBeingRemoved) boidIDs.Remove(boidID);
        else boidIDs.Add(boidID);
    }
}

public struct BoidData
{
    public int m_id;
    public Boids m_boidScript;
    public Vector3Int m_partitionID;

    public BoidData(int id, Boids boidScript)
    {
        m_id = id;
        m_boidScript = boidScript;
        m_partitionID = Vector3Int.zero;
    }

    public BoidData(int id, Boids boidScript, Vector3Int partition)
    {
        m_id = id;
        m_boidScript = boidScript;
        m_partitionID = partition;
    }
}