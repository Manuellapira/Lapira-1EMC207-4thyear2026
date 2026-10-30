using UnityEngine;

// A checkpoint. Put this on a flat Cylinder.
//
// When the player walks into it, the respawn spot is saved here.
// If the player dies and presses R, they come back to the LAST checkpoint
// they reached. The start of the level counts as the first checkpoint.
//
// It uses a simple distance check instead of a trigger, so you do not
// need a Rigidbody or any extra setup.
public class Checkpoint : MonoBehaviour
{
    [Tooltip("How close the player must be. A Cylinder with Scale 6 on X and Z is about 3.")]
    [SerializeField] private float radius = 3f;
    [SerializeField] private Color inactiveColor = new Color(0.2f, 0.4f, 1f);
    [SerializeField] private Color activeColor = Color.cyan;

    private bool isActivated;
    private Renderer zoneRenderer;

    private void Awake()
    {
        // The collider is turned off so the checkpoint never blocks the
        // enemy's view.
        Collider zoneCollider = GetComponent<Collider>();

        if (zoneCollider != null)
        {
            zoneCollider.enabled = false;
        }

        zoneRenderer = GetComponent<Renderer>();
        ApplyColor();
    }

    private void Update()
    {
        if (isActivated || GameManager.Instance == null || !GameManager.Instance.IsPlaying)
        {
            return;
        }

        Vector3 offset = GameManager.Instance.Player.position - transform.position;
        offset.y = 0f;

        if (offset.magnitude <= radius)
        {
            isActivated = true;
            GameManager.Instance.SetCheckpoint(transform.position);
            ApplyColor();
        }
    }

    private void ApplyColor()
    {
        if (zoneRenderer != null)
        {
            zoneRenderer.material.color = isActivated ? activeColor : inactiveColor;
        }
    }

    // Select the checkpoint in the Scene view to see how close you must get.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}