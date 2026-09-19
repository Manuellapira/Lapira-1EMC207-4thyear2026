using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshObstacle))]
public class AutoDoor : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float openHeight = 3f;
    [SerializeField] private float moveDuration = 1f;

    [Header("Timing")]
    [SerializeField] private float closedWaitTime = 4f;
    [SerializeField] private float openWaitTime = 4f;

    private NavMeshObstacle obstacle;
    private Vector3 closedPosition;
    private Vector3 openPosition;

    private void Start()
    {
        obstacle = GetComponent<NavMeshObstacle>();
        obstacle.carving = true;

        closedPosition = transform.position;
        openPosition = closedPosition + Vector3.up * openHeight;

        StartCoroutine(DoorCycle());
    }

    private IEnumerator DoorCycle()
    {
        while (true)
        {
            yield return new WaitForSeconds(closedWaitTime);

            obstacle.carving = false;
            yield return StartCoroutine(MoveDoor(closedPosition, openPosition));

            yield return new WaitForSeconds(openWaitTime);

            yield return StartCoroutine(MoveDoor(openPosition, closedPosition));
            obstacle.carving = true;
        }
    }

    private IEnumerator MoveDoor(Vector3 from, Vector3 to)
    {
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            transform.position = Vector3.Lerp(from, to, elapsed / moveDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = to;
    }
}