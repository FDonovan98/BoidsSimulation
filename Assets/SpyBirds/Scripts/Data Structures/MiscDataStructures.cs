using System.Collections.Generic;
using UnityEngine;

public struct PartitionValues
{
    public Vector3 m_avgPos;
    public Vector3 m_avgVel;
    public Vector3[] m_pointsToAvoid;

    public PartitionValues(Vector3 avgPos, Vector3 avgVel, Vector3[] pointsToAvoid)
    {
        m_avgPos = avgPos;
        m_avgVel = avgVel;
        m_pointsToAvoid = pointsToAvoid;
    }
}