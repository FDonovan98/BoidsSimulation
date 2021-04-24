using System.Collections.Generic;
using UnityEngine;

public struct PartitionValues
{
    public Vector3 m_avgPos;
    public Vector3 m_avgVel;
    public Vector3[] m_posArray;

    public PartitionValues(Vector3 avgPos, Vector3 avgVel, Vector3[] posArray)
    {
        m_avgPos = avgPos;
        m_avgVel = avgVel;
        m_posArray = posArray;
    }
}