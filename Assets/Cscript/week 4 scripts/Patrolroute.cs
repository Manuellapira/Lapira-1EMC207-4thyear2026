using UnityEngine;
using UnityEngine.AI;

// Walks a fixed loop of waypoints, in order, forever. Used by the ground
// agents - all 100 of them share the exact same route.
[RequireComponent(typeof(NavMeshAgent))]
public class PatrolRoute : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;

    [Tooltip("When an agent gets this close to a waypoint, it heads straight to the next one. Bigger number = less crowding.")]
    [SerializeField] private float arrivalDistance = 2f;

    private NavMeshAgent agent;
    private int currentWaypointIndex;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // Don't slow down near a waypoint - just keep flowing to the next.
        agent.autoBraking = false;
    }

    private void Start()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            GoToWaypoint(currentWaypointIndex);
        }
    }

    private void Update()
    {
        if (waypoints == null || waypoints.Length == 0 || agent.pathPending)
        {
            return;
        }

        // Agents don't need to reach the exact point. Being "close
        // enough" is good - this stops 100 agents from pushing each
        // other around trying to stand on the same spot.
        if (agent.remainingDistance <= arrivalDistance)
        {
            GoToWaypoint((currentWaypointIndex + 1) % waypoints.Length);
        }
    }

    // startIndex lets the spawner spread 100 agents around the loop
    // instead of every single one starting at waypoint 0 and piling up,
    // same lesson as the island wanderer from Week 3.
    public void SetWaypoints(Transform[] points, int startIndex)
    {
        waypoints = points;
        GoToWaypoint(startIndex % waypoints.Length);
    }

    private void GoToWaypoint(int index)
    {
        currentWaypointIndex = index;
        agent.SetDestination(waypoints[index].position);
    }
}