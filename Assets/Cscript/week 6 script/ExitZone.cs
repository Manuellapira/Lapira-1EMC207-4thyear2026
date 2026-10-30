using UnityEngine;

// The finishing area. Put this on a flat Cylinder.
//
// When the player stands inside it WITH the item, the level is won.
// Without the item, a hint shows up instead.
//
// It uses a simple distance check instead of a trigger, so you do not
// need a Rigidbody or any extra setup.
public class ExitZone : MonoBehaviour
{
    [Tooltip("How close the player must be. A Cylinder with Scale 4 on X and Z is about 2.")]
    [SerializeField] private float exitRadius = 2f;
    [SerializeField] private Color zoneColor = Color.green;

    private void Awake()
    {
        // The collider is turned off so the zone never blocks the
        // enemy's view or your right-click.
        Collider zoneCollider = GetComponent<Collider>();

        if (zoneCollider != null)
        {
            zoneCollider.enabled = false;
        }

        Renderer zoneRenderer = GetComponent<Renderer>();

        if (zoneRenderer != null)
        {
            zoneRenderer.material.color = zoneColor;
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
        {
            return;
        }

        Vector3 offset = GameManager.Instance.Player.position - transform.position;
        offset.y = 0f;

        if (offset.magnitude <= exitRadius)
        {
            GameManager.Instance.TryFinish();
        }
    }

    // Select the zone in the Scene view to see the exit area.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, exitRadius);
    }
}