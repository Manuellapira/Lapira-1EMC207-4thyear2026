using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshLinkTraverser : MonoBehaviour
{
    [Header("Straight link (e.g. island-to-island, elevator)")]
    [SerializeField] private float straightLinkSpeed = 3.5f;

    [Header("Parabolic link (e.g. stairs / jump links)")]
    [SerializeField] private float parabolaDuration = 0.8f;
    [SerializeField] private float parabolaHeight = 1.5f;

    [Header("Which links count as a 'jump'")]
    [SerializeField] private float heightDifferenceForJump = 0.5f;

    private NavMeshAgent agent;
    private bool isTraversing;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (agent.isOnOffMeshLink && !isTraversing)
        {
            StartCoroutine(TraverseLink());
        }
    }

    private IEnumerator TraverseLink()
    {
        isTraversing = true;

        OffMeshLinkData linkData = agent.currentOffMeshLinkData;
        Vector3 startPos = transform.position;
        Vector3 endPos = linkData.endPos + Vector3.up * agent.baseOffset;

        float heightDifference = Mathf.Abs(endPos.y - startPos.y);

        if (heightDifference >= heightDifferenceForJump)
        {
            // Bonus behavior: stairs-style links get a parabolic hop.
            yield return StartCoroutine(TraverseParabola(startPos, endPos));
        }
        else
        {
            // Flat links (island-to-island, elevator) are effectively instant.
            yield return StartCoroutine(TraverseStraight(startPos, endPos));
        }

        agent.CompleteOffMeshLink();
        isTraversing = false;
    }

    private IEnumerator TraverseStraight(Vector3 startPos, Vector3 endPos)
    {
        float distance = Vector3.Distance(startPos, endPos);
        float duration = Mathf.Max(distance / straightLinkSpeed, 0.01f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
    }

    private IEnumerator TraverseParabola(Vector3 startPos, Vector3 endPos)
    {
        float elapsed = 0f;

        while (elapsed < parabolaDuration)
        {
            float t = elapsed / parabolaDuration;
            Vector3 flatPosition = Vector3.Lerp(startPos, endPos, t);
            float arc = parabolaHeight * 4f * t * (1f - t);

            transform.position = new Vector3(flatPosition.x, flatPosition.y + arc, flatPosition.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
    }
}