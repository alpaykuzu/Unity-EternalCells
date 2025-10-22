using UnityEngine;

public class CameraHolder : MonoBehaviour
{
    [SerializeField]
    private Transform target;
    [SerializeField]
    private float smoothTime = 0.3f;
    [SerializeField]
    private Vector3 offset;

    private Vector3 velocity = Vector3.zero;

    void Update()
    {
        if (target != null)
        {
            Vector3 targetPosition = target.position + offset;

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
    }
}
