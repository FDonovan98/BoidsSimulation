// Manages the boids and partitions existing in scene, handles calculations requiring relative position info.

using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoidManager : MonoBehaviour
{
    [Header("Partition Variables")]
    [SerializeField]
    float partitionLength = 20.0f;
    [SerializeField]
    int numOfPartitions = 70;
    [SerializeField]
    int numBoidsToSpawn = 100;
    [SerializeField]
    float spawnRange = 10.0f;
    [SerializeField]
    GameObject boidPrefab = null;
    Vector3 position;


    [SerializeField]
    private BoidVariables boidVariables;

    // Internal use.
    private Boid[] boids;
    PartitionCollection partitionCollection;

    private void Start()
    {
        // Should allow manager to be moved during play without a problem.
        position = transform.position;
        BuildAllBoids();
        BuildPartitionStructure();
    }

    private void BuildAllBoids()
    {
        Transform[] boidObjects = new Transform[numBoidsToSpawn];

        for (int i = 0; i < numBoidsToSpawn; i++)
        {
            boidObjects[i] = Instantiate<GameObject>(boidPrefab, transform).transform;
        }

        boids = new Boid[numBoidsToSpawn];

        for (int i = 0; i < boids.Length; i++)
        {
            Debug.Log("Place");
            Vector3 startPos = CalculateStartPosition();

            boids[i] = new Boid(i, boidObjects[i],
                startPos,
                CalculatePartition(startPos),
                CalculateStartVel(),
                boidVariables);
        }
    }

    private Vector3 CalculateStartPosition()
    {
        return new Vector3(Random.Range(-spawnRange, spawnRange), Random.Range(-spawnRange, spawnRange), Random.Range(-spawnRange, spawnRange));
    }

    private void BuildPartitionStructure()
    {
        partitionCollection = new PartitionCollection(position, numOfPartitions, partitionLength, boids);
    }

    private Vector3 CalculateStartVel()
    {
        Vector3 startVel = new Vector3(Random.value, Random.value, Random.value);
        return startVel = startVel * Random.Range(-boidVariables.maxSpeed, boidVariables.maxSpeed);
    }

    private Vector3Int CalculatePartition(Vector3 boidPos)
    {
        // Calculate partition number relative to controller position.
        Vector3 partitionFloat = (position - boidPos) / partitionLength;
        // Recentre so controller position is at the centre of the partitions.
        partitionFloat += new Vector3(numOfPartitions / 2, numOfPartitions / 2, numOfPartitions / 2);

        Vector3Int partition = Vector3Int.RoundToInt(partitionFloat);

        // Check partition value is within allowed range.
        if (partition.x > numOfPartitions - 1 || partition.y > numOfPartitions - 1 || partition.z > numOfPartitions - 1)
        {
            Debug.LogError("Boid is outside of partition range");
            return new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        }

        if (partition.x < 0 || partition.y < 0 || partition.z < 0)
        {
            Debug.LogError("Boid is outside of partition range");
            return new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        }

        return partition;
    }

    private void Update()
    {
        // Updates position only if it ahs actually changed.
        // Value of 1.0f is arbitary.
        if (Vector3.Distance(position, transform.position) > 1.0f)
        {
            position = transform.position;
            partitionCollection.UpdateAllPointsToAvoid(position, partitionLength);
        }

        UpdateBoid();
        partitionCollection.UpdatePartitions();
    }

    private async void UpdateBoid()
    {
        float deltaTime = Time.deltaTime;
        await Task.Run(() =>
        {
            foreach (Boid item in boids)
            {
                item.RecalculateVelocity(deltaTime);
                UpdateBoidPartition(item);
            }
        });

        foreach (Boid item in boids)
        {
            item.MoveBoid(Time.deltaTime);
        }
    }

    private void UpdateBoidPartition(Boid boid)
    {
        Vector3Int partition = CalculatePartition(boid.lastPos);
        if (partition == boid.partitionID) return;

        partitionCollection.MoveBoid(boid.ID, partition);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 0.1f);

        Vector3 startPos = position - new Vector3(numOfPartitions / 2 * partitionLength, numOfPartitions / 2 * partitionLength, numOfPartitions / 2 * partitionLength);
        for (int x = 0; x < numOfPartitions; x++)
        {
            for (int y = 0; y < numOfPartitions; y++)
            {
                for (int z = 0; z < numOfPartitions; z++)
                {
                    Gizmos.DrawWireCube(startPos + new Vector3(x * partitionLength, y * partitionLength, z * partitionLength), new Vector3(partitionLength, partitionLength, partitionLength)
                    );
                }
            }
        }

        Gizmos.color = Color.blue;
        // foreach (Boid item in boids)
        // {
        //     Gizmos.DrawRay(item.lastPos, item.targetVel * 3);
        // }

        // foreach (Partition item in partitionCollection.partition)
        // {
        //     if (item != null && item.adjustedFlockValues != null)
        //     {
        //         if (item.adjustedFlockValues.m_pointsToAvoid.Length > 0)
        //         {
        //             Gizmos.color = Color.yellow;
        //             foreach (PointToAvoid element in item.adjustedFlockValues.m_pointsToAvoid)
        //             {
        //                 Gizmos.DrawSphere(element.pointPos, 1.0f);
        //             }
        //         }
        //     }
        // }

        if (boids == null) return;
        Gizmos.DrawLine(boids[0].lastPos, boids[0].lastPos + (5 * boids[0].vel));
        Gizmos.color = Color.red;
        Gizmos.DrawLine(boids[0].lastPos, boids[0].lastPos + (3 * boids[0].targetVel));

        Gizmos.color = Color.yellow;
        if (boids[0].adjustedFlockValues == null) return;
        if (boids[0].adjustedFlockValues.m_pointsToAvoid.Length == 0) return;
        foreach (PointToAvoid element in boids[0].adjustedFlockValues.m_pointsToAvoid)
        {
            Gizmos.DrawSphere(element.pointPos, 1.0f);
        }

    }
}