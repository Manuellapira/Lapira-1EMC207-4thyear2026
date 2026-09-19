using UnityEngine;
using UnityEngine.AI;

public class PlayerClickToMove : MonoBehaviour
{
    private NavMeshAgent agent;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        // Right mouse button
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                MoveTo(hit.point);
            }
        }
    }

    
    private void MoveTo(Vector3 destination)
    {
        NavMeshPath samplePath = new NavMeshPath();
        bool pathFound = agent.CalculatePath(destination, samplePath);

        if (pathFound && samplePath.status == NavMeshPathStatus.PathComplete)
        {
            agent.SetPath(samplePath);
        }
        else if (samplePath.corners.Length > 0)
        {
            Vector3 closestReachablePoint = samplePath.corners[samplePath.corners.Length - 1];
            agent.SetDestination(closestReachablePoint);
        }
    }
}