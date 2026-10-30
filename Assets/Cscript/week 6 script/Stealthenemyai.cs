using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    None,
    Patrol,
    Alert,
    Investigate,
    Chase
}

// Metal Gear style enemy. It only reacts to what it SEES.
// One script for BOTH ground enemies and flying enemies. For a flying
// enemy, just tick "Is Flying" in the Inspector.
//
// Patrol      -> Alert       : it sees the player.
// Alert       -> Chase       : it keeps seeing the player for alertTime
//                              seconds. It stands still and stares.
// Alert       -> Investigate : the player slips out of sight.
// Chase       -> Investigate : it loses the player.
// Investigate -> Chase       : it sees the player again (right away).
// Investigate -> Patrol      : it searched the last known spot for
//                              searchTime seconds and found nothing.
//
// DEATH: if the enemy is CHASING and gets within catchDistance of the
// player, the player dies.
[RequireComponent(typeof(NavMeshAgent))]
public class StealthEnemyAI : MonoBehaviour
{
    public EnemyState CurrentState { get; private set; }

    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Patrol")]
    [Tooltip("Drag the object that has the PatrolPath script on it.")]
    [SerializeField] private PatrolPath patrolPath;
    [SerializeField] private float patrolSpeed = 2f;
    [Tooltip("Close enough to a point to count as arrived.")]
    [SerializeField] private float arrivalDistance = 1f;

    [Header("Vision")]
    [SerializeField] private float viewDistance = 10f;
    [Tooltip("Flying enemies: use 360 so they see all around and below them.")]
    [Range(10f, 360f)]
    [SerializeField] private float viewAngle = 90f;
    [Tooltip("Eye height above this object's pivot. Capsule: 0.5. Model with the pivot at its feet: about 1.6.")]
    [SerializeField] private float eyeHeight = 0.5f;
    [Tooltip("Height of the spot on the player the enemy looks at. Capsule: 0.5. Model with the pivot at its feet: about 1.")]
    [SerializeField] private float targetHeight = 0.5f;

    [Header("Alert (staring)")]
    [Tooltip("How long it stares at the player before it starts chasing.")]
    [SerializeField] private float alertTime = 1f;
    [SerializeField] private float turnSpeed = 360f;

    [Header("Investigate")]
    [SerializeField] private float investigateSpeed = 3f;
    [Tooltip("How long it searches at the last known spot before it gives up and goes back to patrol.")]
    [SerializeField] private float searchTime = 4f;
    [Tooltip("How fast it turns on the spot while searching.")]
    [SerializeField] private float searchTurnSpeed = 60f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 3.5f;

    [Header("Catching")]
    [Tooltip("If the enemy is CHASING and gets this close to the player, the player dies.")]
    [SerializeField] private float catchDistance = 1.5f;

    [Header("Flying (tick Is Flying for flying enemies)")]
    [SerializeField] private bool isFlying = false;
    [SerializeField] private float flyHeight = 8f;
    [SerializeField] private float heightSmoothSpeed = 2f;
    [SerializeField] private float bobAmplitude = 0.3f;
    [SerializeField] private float bobFrequency = 1.5f;

    [Header("Optional")]
    [Tooltip("Drag the enemy here to see its state as a colour: green = patrol, yellow = alert, orange = investigate, red = chase.")]
    [SerializeField] private Renderer bodyRenderer;

    private NavMeshAgent agent;
    private int patrolIndex;
    private int patrolDirection = 1;
    private Vector3 lastKnownPosition;
    private bool hasArrivedAtSearchSpot;
    private float alertTimer;
    private float searchTimer;
    private float currentFlyOffset;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (isFlying)
        {
            // It is in the air, so it never bumps into things on the ground.
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        }
    }

    private void Start()
    {
        currentFlyOffset = agent.baseOffset;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterEnemy(this);
        }

        ChangeState(EnemyState.Patrol);
    }

    private void Update()
    {
        UpdateFlyingHeight();
        CheckCatchPlayer();

        bool canSeePlayer = CanSeePlayer();

        if (canSeePlayer)
        {
            lastKnownPosition = player.position;
        }

        switch (CurrentState)
        {
            case EnemyState.Patrol:
                UpdatePatrol(canSeePlayer);
                break;
            case EnemyState.Alert:
                UpdateAlert(canSeePlayer);
                break;
            case EnemyState.Investigate:
                UpdateInvestigate(canSeePlayer);
                break;
            case EnemyState.Chase:
                UpdateChase(canSeePlayer);
                break;
            default:
                // It shouldn't reach this, but just in case.
                break;
        }
    }

    // ---------- Called by the GameManager ----------

    // After the player respawns, every enemy calms down and walks its
    // patrol again.
    public void ResetToPatrol()
    {
        ChangeState(EnemyState.Patrol);
    }

    // ---------- States ----------

    private void UpdatePatrol(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            ChangeState(EnemyState.Alert);
            return;
        }

        if (patrolPath == null || patrolPath.Count == 0 || agent.pathPending)
        {
            return;
        }

        if (agent.remainingDistance <= arrivalDistance)
        {
            patrolIndex = patrolPath.GetNextIndex(patrolIndex, ref patrolDirection);
            GoToPatrolPoint(patrolIndex);
        }
    }

    private void UpdateAlert(bool canSeePlayer)
    {
        if (!canSeePlayer)
        {
            ChangeState(EnemyState.Investigate);
            return;
        }

        // Stand still, look at the player, and count up.
        FacePosition(player.position);
        alertTimer += Time.deltaTime;

        if (alertTimer >= alertTime)
        {
            ChangeState(EnemyState.Chase);
        }
    }

    private void UpdateInvestigate(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        if (!hasArrivedAtSearchSpot)
        {
            // Still walking to the last place it saw the player.
            if (agent.pathPending || agent.remainingDistance > arrivalDistance)
            {
                return;
            }

            hasArrivedAtSearchSpot = true;
            agent.isStopped = true;
            searchTimer = 0f;
        }

        // At the spot: look around, and give up after searchTime.
        transform.Rotate(0f, searchTurnSpeed * Time.deltaTime, 0f);
        searchTimer += Time.deltaTime;

        if (searchTimer >= searchTime)
        {
            ChangeState(EnemyState.Patrol);
        }
    }

    private void UpdateChase(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            agent.SetDestination(player.position);
            return;
        }

        ChangeState(EnemyState.Investigate);
    }

    // ---------- Changing state ----------

    private void ChangeState(EnemyState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case EnemyState.Patrol:
                agent.isStopped = false;
                agent.speed = patrolSpeed;
                GoToPatrolPoint(patrolIndex);
                break;

            case EnemyState.Alert:
                agent.isStopped = true;
                alertTimer = 0f;
                break;

            case EnemyState.Investigate:
                hasArrivedAtSearchSpot = false;
                searchTimer = 0f;
                agent.isStopped = false;
                agent.speed = investigateSpeed;
                agent.SetDestination(lastKnownPosition);
                break;

            case EnemyState.Chase:
                agent.isStopped = false;
                agent.speed = chaseSpeed;
                break;

            default:
                break;
        }

        ApplyStateColor();
    }

    // ---------- Helpers ----------

    // Flying enemies slowly rise to flyHeight, then float with a small bob.
    // Ground enemies skip this completely.
    private void UpdateFlyingHeight()
    {
        if (!isFlying)
        {
            return;
        }

        currentFlyOffset = Mathf.Lerp(currentFlyOffset, flyHeight, Time.deltaTime * heightSmoothSpeed);
        float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        agent.baseOffset = currentFlyOffset + bob;
    }

    private void GoToPatrolPoint(int index)
    {
        if (patrolPath == null || patrolPath.Count == 0)
        {
            return;
        }

        agent.SetDestination(patrolPath.GetPoint(index));
    }

    // The player only dies if this enemy is CHASING and reaches them.
    // A flying enemy that just flies over a roof does NOT kill a player
    // hiding underneath, because it is not chasing.
    private void CheckCatchPlayer()
    {
        if (CurrentState != EnemyState.Chase)
        {
            return;
        }

        if (player == null || GameManager.Instance == null)
        {
            return;
        }

        Vector3 offset = player.position - transform.position;
        offset.y = 0f;

        if (offset.magnitude <= catchDistance)
        {
            GameManager.Instance.PlayerDied();
        }
    }

    // True only if the player is close enough, inside the vision cone,
    // AND nothing is blocking the line between them.
    // That last part is what makes hiding work: a wall blocks a ground
    // enemy, and a ROOF blocks a flying enemy looking down.
    private bool CanSeePlayer()
    {
        if (player == null)
        {
            return false;
        }

        // A dead player cannot be seen.
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            return false;
        }

        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Vector3 targetPosition = player.position + Vector3.up * targetHeight;
        Vector3 toTarget = targetPosition - eyePosition;

        if (toTarget.magnitude > viewDistance)
        {
            return false;
        }

        Vector3 flatDirection = new Vector3(toTarget.x, 0f, toTarget.z);
        float angleToPlayer = Vector3.Angle(transform.forward, flatDirection);

        if (angleToPlayer > viewAngle * 0.5f)
        {
            return false;
        }

        RaycastHit hit;

        if (Physics.Raycast(eyePosition, toTarget.normalized, out hit, viewDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.transform == player || hit.transform.IsChildOf(player);
        }

        return false;
    }

    private void FacePosition(Vector3 worldPosition)
    {
        Vector3 direction = worldPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void ApplyStateColor()
    {
        if (bodyRenderer == null)
        {
            return;
        }

        switch (CurrentState)
        {
            case EnemyState.Patrol:
                bodyRenderer.material.color = Color.green;
                break;
            case EnemyState.Alert:
                bodyRenderer.material.color = Color.yellow;
                break;
            case EnemyState.Investigate:
                bodyRenderer.material.color = new Color(1f, 0.5f, 0f);
                break;
            case EnemyState.Chase:
                bodyRenderer.material.color = Color.red;
                break;
            default:
                break;
        }
    }

    // Select the enemy in the Scene view to see what it can see.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        if (viewAngle >= 359f)
        {
            Gizmos.DrawWireSphere(transform.position, viewDistance);
            return;
        }

        Vector3 leftEdge = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward * viewDistance;
        Vector3 rightEdge = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward * viewDistance;

        Gizmos.DrawRay(transform.position, leftEdge);
        Gizmos.DrawRay(transform.position, rightEdge);
    }
}