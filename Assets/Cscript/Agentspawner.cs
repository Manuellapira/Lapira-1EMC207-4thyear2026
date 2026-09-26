using UnityEngine;
using UnityEngine.AI;

// Spawns agentCount copies of agentPrefab across the four islands.
//
// If you bake the terrain's NavMeshSurface at runtime (the Week 3 bonus
// path), bake it BEFORE this spawns anyone and let a frame pass first -
// per the cheat sheet, spawning agents the same frame a surface finishes
// baking throws errors. Easiest fix: call BuildNavMesh() from a script
// whose Start() runs earlier (Script Execution Order), or wrap this
// spawner's loop in a coroutine that does a single `yield return null`
// before spawning.
public class AgentSpawner : MonoBehaviour
{
    [SerializeField] private GameObject agentPrefab;
    [SerializeField] private Transform[] islandPoints;
    [SerializeField] private int agentCount = 150;
    [SerializeField] private float spawnScatterRadius = 2f;

    private void Start()
    {
        for (int i = 0; i < agentCount; i++)
        {
            SpawnOneAgent();
        }
    }

    private void SpawnOneAgent()
    {
        Transform spawnPoint = islandPoints[Random.Range(0, islandPoints.Length)];
        Vector3 scatter = Random.insideUnitSphere * spawnScatterRadius;
        scatter.y = 0f;

        GameObject agentInstance = Instantiate(agentPrefab, spawnPoint.position + scatter, Quaternion.identity);

        NavMeshAgent navAgent = agentInstance.GetComponent<NavMeshAgent>();
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

        // Slight priority spread (0 = highest, per the cheat sheet) stops
        // a mass of identical-priority agents from deadlocking each
        // other on the narrow bridges.
        navAgent.avoidancePriority = Random.Range(0, 99);

        RandomIslandWanderer wanderer = agentInstance.GetComponent<RandomIslandWanderer>();
        wanderer.SetIslandPoints(islandPoints);
    }
}