using UnityEngine;
using UnityEngine.AI;

// Simple right-click-to-move player. No stealth button and no sound
// anymore - the enemy only reacts to what it can SEE, so hiding behind
// walls is how you stay safe.
//
// Keep moveSpeed higher than the enemy's Chase Speed (3.5), so you can
// outrun it.
[RequireComponent(typeof(NavMeshAgent))]
public class StealthPlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
    }

    private void Update()
    {
        // Right mouse button
        if (!Input.GetMouseButtonDown(1))
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            agent.SetDestination(hit.point);
        }
    }
}