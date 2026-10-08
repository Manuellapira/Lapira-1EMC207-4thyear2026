using UnityEngine;
using UnityEngine.AI;

// Spawns agentCount ground agents, all walking the same patrol loop,
// started at different points around it so they don't bunch up.
public class GroundAgentSpawner : MonoBehaviour
{
    [SerializeField] private GameObject groundAgentPrefab;
    [SerializeField] private Transform[] patrolWaypoints;
    [SerializeField] private int agentCount = 100;

    private void Start()
    {
        for (int i = 0; i < agentCount; i++)
        {
            SpawnOneAgent(i);
        }
    }

    private void SpawnOneAgent(int index)
    {
        Transform spawnPoint = patrolWaypoints[index % patrolWaypoints.Length];
        GameObject agentInstance = Instantiate(groundAgentPrefab, spawnPoint.position, Quaternion.identity);

        NavMeshAgent navAgent = agentInstance.GetComponent<NavMeshAgent>();
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        navAgent.avoidancePriority = Random.Range(0, 99);

        PatrolRoute patrol = agentInstance.GetComponent<PatrolRoute>();
        patrol.SetWaypoints(patrolWaypoints, index);
    }
}