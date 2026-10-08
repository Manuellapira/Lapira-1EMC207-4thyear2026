using UnityEngine;
using UnityEngine.AI;

// A flying agent. It stays up in the air the whole time - it never
// comes down to ground level.
//
// It patrols two routes and alternates between them: one full lap of
// the high route (over the tall obstacle and the whole terrain), then
// one full lap of the shared route (the same loop the ground agents
// walk, but the flying agent floats ABOVE it). If you leave the second
// route empty, it just keeps looping the high route.
//
// Height is smooth, not a snap (bonus: gradual vertical change), and a
// small sine-wave bob is added on top that has nothing to do with the
// terrain (bonus: vertical position modified by something other than
// the navmesh).
[RequireComponent(typeof(NavMeshAgent))]
public class FlyingAgentController : MonoBehaviour
{
    [Header("Routes")]
    [SerializeField] private Transform[] highRoute;

    [Tooltip("Same loop the ground agents walk. The flying agent flies ABOVE it. Leave empty to only use the high route.")]
    [SerializeField] private Transform[] groundRoute;

    [Tooltip("When the agent gets this close to a waypoint, it heads straight to the next one.")]
    [SerializeField] private float arrivalDistance = 2f;

    [Header("Height")]
    [SerializeField] private float flyHeight = 8f;
    [SerializeField] private float heightSmoothSpeed = 2f;
    [SerializeField] private float bobAmplitude = 0.3f;
    [SerializeField] private float bobFrequency = 1.5f;

    private NavMeshAgent agent;
    private Transform[] activeRoute;
    private bool onHighRoute = true;
    private int waypointIndex;
    private float smoothedBaseOffset;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // Always in the air, so it never reacts to (or bumps) the
        // ground agents below it.
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

        // Keep flowing - don't slow down at each waypoint.
        agent.autoBraking = false;
    }

    private void Start()
    {
        activeRoute = highRoute;
        waypointIndex = 0;

        if (activeRoute != null && activeRoute.Length > 0)
        {
            agent.SetDestination(activeRoute[0].position);
        }
    }

    private void Update()
    {
        UpdateHeight();
        UpdatePatrol();
    }

    private void UpdatePatrol()
    {
        if (activeRoute == null || activeRoute.Length == 0 || agent.pathPending)
        {
            return;
        }

        if (agent.remainingDistance <= arrivalDistance)
        {
            waypointIndex++;

            if (waypointIndex >= activeRoute.Length)
            {
                // Finished a full lap - swap to the other route, if
                // there is one.
                if (HasSecondRoute())
                {
                    onHighRoute = !onHighRoute;
                    activeRoute = onHighRoute ? highRoute : groundRoute;
                }

                waypointIndex = 0;
            }

            agent.SetDestination(activeRoute[waypointIndex].position);
        }
    }

    private void UpdateHeight()
    {
        // Climbs up to flyHeight smoothly when the game starts, then
        // stays there.
        smoothedBaseOffset = Mathf.Lerp(smoothedBaseOffset, flyHeight, Time.deltaTime * heightSmoothSpeed);

        float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        agent.baseOffset = smoothedBaseOffset + bob;
    }

    private bool HasSecondRoute()
    {
        return groundRoute != null && groundRoute.Length > 0;
    }

    // Optional - only needed if you ever spawn the flying agent from a
    // script instead of placing it once in the scene.
    public void SetRoutes(Transform[] high, Transform[] ground)
    {
        highRoute = high;
        groundRoute = ground;
        activeRoute = onHighRoute ? highRoute : groundRoute;
        waypointIndex = 0;

        if (activeRoute != null && activeRoute.Length > 0)
        {
            agent.SetDestination(activeRoute[0].position);
        }
    }
}