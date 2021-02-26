// Author: Harry Donovan
// Collaborators:
// License: GNU General Public License v3.0
// References:
// Description: Controller class for boid flocks. Should handle spatial partitioning.

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BoidsController : MonoBehaviour
{
    [Header("Boid List")]
    [SerializeField]
    const int maxBoids = 1500;
    List<int> availableIndex = new List<int>();
    BoidData[] boidData = new BoidData[maxBoids];

    [Header("Partition")]
    [SerializeField]
    float partitionLength = 20.0f;
    [SerializeField]
    const int partitionNumber = 70;
    [SerializeField]
    int partitionUpdatesPerFrame = 10;
    PartitionData[,,] partitions = new PartitionData[partitionNumber, partitionNumber, partitionNumber];

    List<UpdatePartitionQueue> updatePartitionQueue = new List<UpdatePartitionQueue>();
    List<UpdatePartitionQueue> updatePartitionIndex = new List<UpdatePartitionQueue>();

    // Update flock values delegate.
    public delegate void NotifyBoidsPartitionUpdate(PartitionData partitionData);
    public NotifyBoidsPartitionUpdate notifyBoidsPartitionUpdate;

    private void Awake()
    {
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

        // foreach (UpdatePartitionQueue item in updatePartitionQueue)
        // {
        //     partitions[item.m_partitionID.x, item.m_partitionID.y, item.m_partitionID.z].UpdateFlockValues(boidData);

        //     notifyBoidsPartitionUpdate(partitions[item.m_partitionID.x, item.m_partitionID.y, item.m_partitionID.z]);
        // }

        for (int i = 0; i < Mathf.Min(partitionUpdatesPerFrame, updatePartitionQueue.Count); i++)
        {
            partitions[updatePartitionQueue[0].m_partitionID.x, updatePartitionQueue[0].m_partitionID.y, updatePartitionQueue[0].m_partitionID.z].UpdateFlockValues(boidData);

            updatePartitionQueue.RemoveAt(0);
        }
        Debug.Log(updatePartitionQueue.Count);
    }

    private void UpdatePartitionIDLists()
    {
        foreach (UpdatePartitionQueue item in updatePartitionIndex)
        {
            partitions[item.m_partitionID.x, item.m_partitionID.y, item.m_partitionID.z].UpdateIDList(item.m_boidID, item.m_removeID);
        }
        updatePartitionIndex = new List<UpdatePartitionQueue>();
    }

    // Called by boidData to register themselves with the BoidController.
    // Returns int.MaxValue if boid list is full.
    public int RegisterBoid(Rigidbody rb, out float updateDistance)
    {
        if (availableIndex.Count > 0)
        {
            int id = availableIndex[0];
            availableIndex.RemoveAt(0);

            boidData[id] = new BoidData(id, rb);
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
        Vector3Int partition = CalculatePartition(boidData[boidID].m_rb.position);

        // On initialisation boid cannot be unregistered for partitionData ID list as it has not yet been added to any.
        if (isInitialisation)
        {
            // This should be refactored into a standalone function as is duplicated code.
            boidData[boidID].m_partitionID = partition;

            if (partitions[partition.x, partition.y, partition.z] == null)
            {
                partitions[partition.x, partition.y, partition.z] = new PartitionData(boidData, boidID);
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
    // Currently only updates directly effected partitions as neighbouring partitions aren't taken into account when calculating flock values.
    // This should be changed.
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

        // This should be refactored into a standalone function as is duplicated code.
        QueueUpdateData(partitionID, boidID, true);

        // This should be refactored into a standalone function as is duplicated code.
        // Update new partition, adding boid to id list.
        // Notify subscribed boidData partitions have updated
        boidData[boidID].m_partitionID = partitionID;
        if (partitions[newPartition.x, newPartition.y, newPartition.z] == null)
        {
            partitions[newPartition.x, newPartition.y, newPartition.z] = new PartitionData(boidData, boidID);
        }
        else
        {
            QueueUpdateData(newPartition, boidID, false);
        }
    }

    void QueueUpdateData(Vector3Int partitionID)
    {
        UpdatePartitionQueue queueItem = new UpdatePartitionQueue(partitionID);

        // Uses foreach rather than .contains as we only care about matching partitionID, not full match
        foreach (UpdatePartitionQueue item in updatePartitionQueue)
        {
            if (item.m_partitionID == partitionID)
            {
                return;
            }
        }

        updatePartitionQueue.Add(queueItem);
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

                    Gizmos.DrawCube(startPos + new Vector3(x * partitionLength + partitionLength / 2, y * partitionLength + partitionLength / 2, z * partitionLength + partitionLength / 2), new Vector3(partitionLength, partitionLength, partitionLength)
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

    public PartitionData(BoidData[] boidData, int boidID)
    {
        boidIDs = new List<int>();
        boidIDs.Add(boidID);
    }

    public void UpdateFlockValues(BoidData[] boidData)
    {
        if (boidIDs.Count < 1) return;

        Vector3 avgPos = new Vector3();
        Vector3 avgVel = new Vector3();
        Vector3[] posArray = new Vector3[boidIDs.Count];

        int i = 0;
        foreach (int id in boidIDs)
        {
            posArray[i] = boidData[id].m_rb.position;
            avgPos += posArray[i];
            avgVel += boidData[id].m_rb.velocity;

            i++;
        }

        flockValues = new FlockValues(avgPos / boidIDs.Count, avgVel / boidIDs.Count, posArray);
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
    public Rigidbody m_rb;
    public Vector3Int m_partitionID;

    public BoidData(int id, Rigidbody rb)
    {
        m_id = id;
        m_rb = rb;
        m_partitionID = Vector3Int.zero;
    }
}