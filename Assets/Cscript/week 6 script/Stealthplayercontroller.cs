using UnityEngine;
using UnityEngine.AI;

// Right-click to move. Nothing happens while the player is dead.
//
// Keep moveSpeed higher than the enemy's Chase Speed (3.5), so you can
// outrun a chase.
[RequireComponent(typeof(NavMeshAgent))]
public class StealthPlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("Which layers your right-click can hit. Set this to Ground ONLY, so roofs, walls and flying enemies never block your clicks.")]
    [SerializeField] private LayerMask clickMask = ~0;

    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
    }

    private void Update()
    {
        // Dead or finished? No walking.
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            return;
        }

        // Right mouse button
        if (!Input.GetMouseButtonDown(1))
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, clickMask))
        {
            agent.SetDestination(hit.point);
        }
    }
}