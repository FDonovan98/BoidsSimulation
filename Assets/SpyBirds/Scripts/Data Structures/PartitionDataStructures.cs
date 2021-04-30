﻿using System;
using System.Collections.Generic;
using UnityEngine;

internal class PartitionCollection
{
    private int numOfPartitions;
    // TODO: Make private again.
    public Partition[,,] partition;

    private Boid[] boids;

    private List<Vector3Int> partitionsToUpdate = new List<Vector3Int>();

    public PartitionCollection(Vector3 pos, int numOfPartitions, float partitionLength, Boid[] boids)
    {
        this.numOfPartitions = numOfPartitions;
        this.boids = boids;

        partition = new Partition[numOfPartitions, numOfPartitions, numOfPartitions];

        UpdateAllPointsToAvoid(pos, partitionLength);

        UpdatePartitions();
    }

    // TODO: Boid leaving partition collection.
    public void MoveBoid(int boidID, Vector3Int newPartition)
    {
        // vector3 with all max values means partition is outside of allowed range.
        if (newPartition == new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue)) return;
        Vector3Int oldPartition = boids[boidID].partitionID;

        if (partition[oldPartition.x, oldPartition.y, oldPartition.z] != null)
        {
            partition[oldPartition.x, oldPartition.y, oldPartition.z].UpdateIDList(boidID, true);

            UpdatePartitionQueue(oldPartition);
        }


        if (partition[newPartition.x, newPartition.y, newPartition.z] == null)
        {
            partition[newPartition.x, newPartition.y, newPartition.z] = new Partition(boidID, newPartition, numOfPartitions);
        }

        boids[boidID].partitionID = newPartition;
        partition[newPartition.x, newPartition.y, newPartition.z].UpdateIDList(boidID, false);

        UpdatePartitionQueue(newPartition);
    }

    private void UpdatePartitionQueue(Vector3Int partition)
    {
        if (!partitionsToUpdate.Contains(partition)) partitionsToUpdate.Add(partition);
    }

    // TODO: Add function to recalculate all locations stored in pointToAvoidDict if the position of the partitioncollection has changed.
    public void UpdateAllPointsToAvoid(Vector3 pos, float partitionLength)
    {
        UpdateBoundingBox(pos, partitionLength, 2.0f);
        // UpdateTerrainColliders(pos);
    }

    // TODO:
    // Calculate any external faces for each partition.
    // Calculate coords in world space of the centre of the face.
    // Add this to the list of points to avoid in the partition data.
    private void UpdateBoundingBox(Vector3 position, float partitionLength, float pointDensity)
    {
        // Calculate bottom left corner of bounding cube.
        Vector3 refPos = new Vector3(
            position.x - (numOfPartitions / 2) * partitionLength,
            position.y - (numOfPartitions / 2) * partitionLength,
            position.z - (numOfPartitions / 2) * partitionLength);

        float modifiedPartitionLength = partitionLength * pointDensity;
        float partLengthToAdd = partitionLength * (1 / pointDensity);
        int modifiedNumOfPartitions = Mathf.FloorToInt(numOfPartitions * pointDensity);

        for (int x = 0; x < modifiedNumOfPartitions; x++)
        {
            for (int y = 0; y < modifiedNumOfPartitions; y++)
            {
                for (int z = 0; z < modifiedNumOfPartitions; z++)
                {
                    if (x == 0 || y == 0 || z == 0 || x == modifiedNumOfPartitions - 1 || y == modifiedNumOfPartitions - 1 || z == modifiedNumOfPartitions - 1)
                    {
                        float xLength;
                        xLength = x * partLengthToAdd + partLengthToAdd / 2;

                        float yLength;
                        yLength = y * partLengthToAdd + partLengthToAdd / 2;

                        float zLength;
                        zLength = z * partLengthToAdd + partLengthToAdd / 2;

                        Vector3 pos = new Vector3(xLength, yLength, zLength);
                        pos += refPos;

                        Vector3Int id = CalculatePartition(pos, partitionLength);
                        if (int.MaxValue - id.x < 1) break;

                        AddPointToAvoidToPartition(id, pos, true);
                    }
                }
            }
        }

    }

    // Modified version of function in BoidManager.cs.
    // That function could be modified to be static rather than repeat code.
    // Lacks offsetting by position as origin is always 0,0,0 here.
    private Vector3Int CalculatePartition(Vector3 boidPos, float partitionLength)
    {
        // Calculate partition number relative to controller position.
        Vector3 partitionFloat = -boidPos / partitionLength;
        // Recentre so controller position is at the centre of the partitions.
        partitionFloat += new Vector3(numOfPartitions / 2, numOfPartitions / 2, numOfPartitions / 2);

        Vector3Int partition = Vector3Int.CeilToInt(partitionFloat);

        // Check partition value is within allowed range.
        // +1 & -1 as points are going to be right on the edge os otherwise would always "return out of partition" error.
        if (partition.x >= numOfPartitions + 1 || partition.y >= numOfPartitions + 1 || partition.z >= numOfPartitions + 1)
        {
            Debug.LogError("Pos is outside of partition range");
            return new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        }
        else if (partition.x < 0 || partition.y < 0 || partition.z < 0)
        {
            Debug.LogError("Pos is outside of partition range");
            return new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        }

        return partition;
    }

    // Ensures partition exists before trying to write to it.
    private void AddPointToAvoidToPartition(Vector3Int partitionID, Vector3 pos, bool isPointTerrain)
    {
        if (partition[partitionID.x, partitionID.y, partitionID.z] == null)
        {
            partition[partitionID.x, partitionID.y, partitionID.z] = new Partition(partitionID, numOfPartitions);
        }

        partition[partitionID.x, partitionID.y, partitionID.z].AddPointToAvoid(new PointToAvoid(pos, isPointTerrain));

        UpdatePartitionQueue(partitionID);
    }

    // TODO:
    // Marching cube through each partition and add collider points for any colliders found in each cube.
    private void UpdateTerrainColliders(Vector3 pos)
    {
        throw new NotImplementedException();
    }

    public void UpdatePartitions()
    {
        if (partitionsToUpdate.Count == 0) return;

        // Update initial flock values.
        for (int i = 0; i < partitionsToUpdate.Count; i++)
        {
            partition[partitionsToUpdate[i].x, partitionsToUpdate[i].y, partitionsToUpdate[i].z].UpdateFlockValues(boids);
        }

        for (int i = 0; i < partitionsToUpdate.Count; i++)
        {
            Partition tempData = partition[partitionsToUpdate[i].x, partitionsToUpdate[i].y, partitionsToUpdate[i].z];
            // Update flock values taking into account neighbouring partitions.
            tempData.CalculateAdjustedFlockValues(partition);

            // Update boid target velocities.
            for (int j = 0; j < tempData.boidIDs.Count; j++)
            {
                boids[tempData.boidIDs[j]].UpdateFlockValues(tempData.partitionValues, tempData.adjustedFlockValues);
            }
        }

        partitionsToUpdate = new List<Vector3Int>();
    }
}

public class Partition
{
    public List<int> boidIDs = new List<int>();
    public PartitionValues partitionValues;
    public PartitionValues adjustedFlockValues;
    // TODO: make privaet again.
    public Vector3Int m_partitionID;
    private Vector3Int[] neighbouringIDs;
    // Location, isLocationRelative.
    private List<PointToAvoid> pointsToAvoid = new List<PointToAvoid>();

    public Partition(int boidID, Vector3Int partitionID, int numPartitions)
    {
        boidIDs = new List<int>();
        boidIDs.Add(boidID);

        m_partitionID = partitionID;

        CalculateNeighbours(partitionID, numPartitions);
    }

    public Partition(Vector3Int partitionID, int numPartitions)
    {
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
    public void CalculateAdjustedFlockValues(Partition[,,] partitionDatas)
    {
        Vector3 avgPos = partitionValues.m_avgPos * boidIDs.Count;
        Vector3 avgVel = partitionValues.m_avgVel * boidIDs.Count;
        List<PointToAvoid> avoidPoints = new List<PointToAvoid>();

        if (partitionValues.m_pointsToAvoid.Length > 0) avoidPoints.AddRange(partitionValues.m_pointsToAvoid);

        int totalCount = boidIDs.Count;

        foreach (Vector3Int item in neighbouringIDs)
        {
            Partition itemPartitionData = partitionDatas[item.x, item.y, item.z];

            if (itemPartitionData != null)
            {
                avgPos += itemPartitionData.partitionValues.m_avgPos * itemPartitionData.boidIDs.Count;

                avgVel += itemPartitionData.partitionValues.m_avgVel * itemPartitionData.boidIDs.Count;

                totalCount += itemPartitionData.boidIDs.Count;

                avoidPoints.AddRange(itemPartitionData.pointsToAvoid);
            }
        }

        adjustedFlockValues = new PartitionValues(avgPos / totalCount, avgVel / totalCount, avoidPoints.ToArray());
    }

    // Update values for this partition.
    public void UpdateFlockValues(Boid[] boids)
    {
        Vector3 avgPos = new Vector3();
        Vector3 avgVel = new Vector3();
        List<PointToAvoid> avoidPoints = new List<PointToAvoid>();

        // Uses stored boidID's for index positions in boids array. 
        for (int i = 0; i < boidIDs.Count; i++)
        {
            avoidPoints.Add(new PointToAvoid(boids[boidIDs[i]].lastPos, false));
            avgPos += boids[boidIDs[i]].lastPos;
            avgVel += boids[boidIDs[i]].vel;
        }

        avoidPoints.AddRange(pointsToAvoid);

        partitionValues = new PartitionValues(avgPos / boidIDs.Count, avgVel / boidIDs.Count, avoidPoints.ToArray());
    }

    public void UpdateIDList(int boidID, bool idIsBeingRemoved)
    {
        if (idIsBeingRemoved) boidIDs.Remove(boidID);
        else boidIDs.Add(boidID);
    }

    // If point doesn't already exist, add it.
    internal void AddPointToAvoid(PointToAvoid pointToAvoid)
    {
        for (int i = 0; i < pointsToAvoid.Count; i++)
        {
            if (Vector3.Distance(pointsToAvoid[i].pointPos, pointToAvoid.pointPos) < 1)
            {
                // isPointTerrain needs to be checked if point does exist due to terrain being reweighted by boids.
                if (pointToAvoid.isPointTerrain && !pointsToAvoid[i].isPointTerrain)
                {
                    pointsToAvoid[i] = pointToAvoid;
                }
                return;
            }

        }

        pointsToAvoid.Add(pointToAvoid);
    }
}