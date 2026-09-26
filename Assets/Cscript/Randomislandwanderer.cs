using UnityEngine;
using UnityEngine.AI;

// Walks to a random island. Once it arrives, picks a different random
// island (never the one it's currently on) and walks there again,
// forever. Assign the four island waypoints via SetIslandPoints (the
// AgentSpawner below does this automatically for every spawned agent).
//
// Auto Repath is turned off deliberately. Per the cheat sheet, with
// 150+ agents potentially invalidated at once (e.g. a bridge closing
// mid-crossing), Unity's built-in auto-repath can spike the frame time
// if it fires for everyone simultaneously. Path validity is instead
// rechecked on a staggered timer, same idea as AreaSpeedModifier.
[RequireComponent(typeof(NavMeshAgent))]
public class RandomIslandWanderer : MonoBehaviour
{
    // Not a [SerializeField] on purpose - this never shows up in the
    // Inspector. AgentSpawner is the only thing that sets it, through
    // SetIslandPoints() below, right after it makes each agent.
    private Transform[] islandPoints;
    [SerializeField] private float repathCheckInterval = 0.3f;

    [Tooltip("How far around the island's center point agents can land. Bigger islands can use a bigger number.")]
    [SerializeField] private float islandScatterRadius = 4f;

    private NavMeshAgent agent;
    private int currentIslandIndex = -1;
    private Vector3 currentDestination;
    private float nextRepathCheckTime;

    private void Awake()
    {
        // Awake runs the instant Instantiate() creates this object -
        // before the spawner's next line even runs. AgentSpawner calls
        // SetIslandPoints() immediately after Instantiate(), in that
        // same frame, so "agent" has to be ready here, not in Start().
        agent = GetComponent<NavMeshAgent>();
        agent.autoRepath = false;
    }

    private void Start()
    {
        nextRepathCheckTime = Time.time + Random.Range(0f, repathCheckInterval);

        if (islandPoints != null && islandPoints.Length > 0 && currentIslandIndex == -1)
        {
            GoToNewRandomIsland();
        }
    }

    private void Update()
    {
        if (agent.pathPending)
        {
            return;
        }

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            GoToNewRandomIsland();
            return;
        }

        if (Time.time >= nextRepathCheckTime)
        {
            nextRepathCheckTime = Time.time + repathCheckInterval;

            if (agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                // Current route got cut off (e.g. a bridge closed while
                // this agent was crossing it) - retry the same spot.
                agent.SetDestination(currentDestination);
            }
        }
    }

    public void SetIslandPoints(Transform[] points)
    {
        islandPoints = points;

        if (currentIslandIndex == -1)
        {
            GoToNewRandomIsland();
        }
    }

    private void GoToNewRandomIsland()
    {
        int nextIndex = currentIslandIndex;

        while (nextIndex == currentIslandIndex)
        {
            nextIndex = Random.Range(0, islandPoints.Length);
        }

        currentIslandIndex = nextIndex;
        currentDestination = GetScatteredPoint(islandPoints[currentIslandIndex].position);
        agent.SetDestination(currentDestination);
    }

    // Picks a random spot near the island's center instead of the exact
    // same point every time - this is what stops all the agents from
    // piling up on top of each other when they arrive.
    private Vector3 GetScatteredPoint(Vector3 center)
    {
        Vector3 randomOffset = Random.insideUnitSphere * islandScatterRadius;
        randomOffset.y = 0f;
        Vector3 candidate = center + randomOffset;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(candidate, out hit, islandScatterRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return center;
    }
}