using UnityEngine;

// Put this on the Main Camera. It follows the player from above, so the
// big map works. It never turns the camera. Keep the camera's Rotation
// at 60, 0, 0 like before.
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [Tooltip("Where the camera sits compared to the player. 0, 30, -17 looks down at about 60 degrees.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 30f, -17f);
    [SerializeField] private float smoothSpeed = 8f;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }
}