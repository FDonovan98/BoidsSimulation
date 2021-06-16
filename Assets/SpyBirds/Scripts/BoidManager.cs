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
    GameObject boidPrefab = null;
    Vector3 position;

    [Header("Boid Variables")]
    // Movement variables.
    [SerializeField]
    private float maxSpeed = 10.0f;
    [SerializeField]
    private float turnRate = 1.0f;
    [SerializeField]
    private float acceleration = 1.0f;

    // Steering variables.
    [SerializeField]
    private Target target = null;
    [SerializeField]
    private float separationDistance = 4.0f;

    // Steering weights.
    [SerializeField]
    private float targetWeight = 1.0f;
    [SerializeField]
    private float separationWeight = 1.0f;
    [SerializeField]
    private float cohesionWeight = 1.0f;
    [SerializeField]
    private float alignmentWeight = 1.0f;

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
            BoidVariables boidVariables = new BoidVariables(maxSpeed,
                turnRate,
                acceleration,
                target,
                separationDistance,
                targetWeight,
                separationWeight,
                cohesionWeight,
                alignmentWeight);

            boids[i] = new Boid(i, boidObjects[i],
                startPos,
                CalculatePartition(startPos),
                CalculateStartVel(),
                boidVariables);
        }
    }

    private Vector3 CalculateStartPosition()
    {
        float startOffset = 10.0f;
        return new Vector3(Random.Range(-startOffset, startOffset), Random.Range(-startOffset, startOffset), Random.Range(-startOffset, startOffset));
    }

    private void BuildPartitionStructure()
    {
        partitionCollection = new PartitionCollection(numOfPartitions, boids);

        // AddBoundingPoints();
    }

    // Calculate points that need to be avoided to keep boids within partition collection.
    // Adds these points to the partitions.
    private void AddBoundingPoints()
    {
        throw new System.NotImplementedException();
    }

    private Vector3 CalculateStartVel()
    {
        Vector3 startVel = new Vector3(Random.value, Random.value, Random.value);
        return startVel = startVel * Random.Range(-maxSpeed, maxSpeed);
    }

    private Vector3Int CalculatePartition(Vector3 boidPos)
    {
        // Calculate partition number relative to controller position.
        Vector3 partitionFloat = (position - boidPos) / partitionLength;
        // Recentre so controller position is at the centre of the partitions.
        partitionFloat += new Vector3(numOfPartitions / 2, numOfPartitions / 2, numOfPartitions / 2);
        Vector3Int partition = Vector3Int.FloorToInt(partitionFloat);

        // Check partition value is within allowed range.
        if (partition.x > numOfPartitions || partition.y > numOfPartitions || partition.z > numOfPartitions)
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
        position = transform.position;
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
                    Gizmos.DrawWireCube(startPos + new Vector3(x * partitionLength + partitionLength / 2, y * partitionLength + partitionLength / 2, z * partitionLength + partitionLength / 2), new Vector3(partitionLength, partitionLength, partitionLength)
                    );
                }
            }
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(boids[0].lastPos, boids[0].lastPos + (5 * boids[0].vel));
        Gizmos.color = Color.red;
        Gizmos.DrawLine(boids[0].lastPos, boids[0].lastPos + (3 * boids[0].targetVel));

        Gizmos.DrawSphere(boids[0].flockValues.m_avgPos, 1.0f);
    }
}