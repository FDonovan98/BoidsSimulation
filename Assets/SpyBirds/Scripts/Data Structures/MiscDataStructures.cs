using System.Collections.Generic;
using UnityEngine;

public class PartitionValues
{
    public Vector3 m_avgPos;
    public Vector3 m_avgVel;
    public PointToAvoid[] m_pointsToAvoid;

    public PartitionValues(Vector3 avgPos, Vector3 avgVel, PointToAvoid[] pointsToAvoid)
    {
        m_avgPos = avgPos;
        m_avgVel = avgVel;
        m_pointsToAvoid = pointsToAvoid;
    }
}

public struct PointToAvoid
{
    public Vector3 pointPos;
    public bool isPointTerrain;

    public PointToAvoid(Vector3 pos, bool isTerrain)
    {
        pointPos = pos;
        isPointTerrain = isTerrain;
    }
}