using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshObstacle))]
public class MovingCarveObstacle : MonoBehaviour
{
    [Tooltip("Offset from the obstacle's starting position.")]
    [SerializeField] private Vector3 localPointA = Vector3.zero;
    [SerializeField] private Vector3 localPointB = new Vector3(3f, 0f, 0f);
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float arrivalThreshold = 0.05f;

    private Vector3 pointA;
    private Vector3 pointB;
    private Vector3 targetPoint;

    private void Start()
    {
        pointA = transform.position + localPointA;
        pointB = transform.position + localPointB;
        targetPoint = pointB;
    }

    private void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPoint, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPoint) < arrivalThreshold)
        {
            targetPoint = targetPoint == pointB ? pointA : pointB;
        }
    }
}