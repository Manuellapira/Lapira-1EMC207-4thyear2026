using UnityEngine;

// The thing the player has to steal. Put this on a Cube.
//
// It spins and turns yellow so it is easy to spot. When the player
// walks within pickupRadius, it is collected and disappears.
//
// It uses a simple distance check instead of a trigger, so you do not
// need a Rigidbody or any extra setup.
public class ObjectiveItem : MonoBehaviour
{
    [SerializeField] private float pickupRadius = 1.5f;
    [SerializeField] private float spinSpeed = 90f;
    [SerializeField] private Color itemColor = Color.yellow;

    private void Awake()
    {
        // The collider is turned off so the item never blocks the
        // enemy's view or your right-click.
        Collider itemCollider = GetComponent<Collider>();

        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }

        Renderer itemRenderer = GetComponent<Renderer>();

        if (itemRenderer != null)
        {
            itemRenderer.material.color = itemColor;
        }
    }

    private void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
        {
            return;
        }

        Vector3 offset = GameManager.Instance.Player.position - transform.position;
        offset.y = 0f;

        if (offset.magnitude <= pickupRadius)
        {
            GameManager.Instance.CollectObjective();
            gameObject.SetActive(false);
        }
    }

    // Select the item in the Scene view to see how close you must get.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}