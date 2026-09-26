using UnityEngine;
using UnityEngine.AI;

// NavMesh area "cost" only affects which route the pathfinder PICKS - it
// does not slow the agent down while walking through it. This samples
// the surface just ahead of the agent on its current path (via
// SamplePathPosition, per the cheat sheet - not a plain position check)
// and adjusts speed to match, so SlowTerrain actually behaves like
// difficult terrain.
[RequireComponent(typeof(NavMeshAgent))]
public class AreaSpeedModifier : MonoBehaviour
{
    [SerializeField] private float normalSpeed = 3.5f;
    [SerializeField] private float slowSpeedMultiplier = 0.5f;
    [SerializeField] private float sampleDistance = 0.25f;
    [SerializeField] private float checkInterval = 0.2f;

    private NavMeshAgent agent;
    private float nextCheckTime;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = normalSpeed;

        // Stagger the first check per-agent so 150 of these don't all
        // sample the navmesh on the exact same frame.
        nextCheckTime = Time.time + Random.Range(0f, checkInterval);
    }

    private void Update()
    {
        if (Time.time < nextCheckTime)
        {
            return;
        }

        nextCheckTime = Time.time + checkInterval;
        UpdateSpeedForCurrentArea();
    }

    private void UpdateSpeedForCurrentArea()
    {
        NavMeshHit navHit;

        if (agent.SamplePathPosition(NavMesh.AllAreas, sampleDistance, out navHit))
        {
            bool onSlowTerrain = (navHit.mask & TerrainMaskHandler.Instance.SlowTerrainMask) != 0;
            agent.speed = onSlowTerrain ? normalSpeed * slowSpeedMultiplier : normalSpeed;
        }
    }
}