using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Offset")]
    public Vector3 offset = new Vector3(0f, 8f, -8f);

    [Header("Smooth Follow")]
    public float followSpeed = 8f;

    [Header("Look Target")]
    public float lookHeight = 0.5f;

    private void LateUpdate()
    {
        if(target == null)
        return;

        Vector3 desiredPosition = target.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        Vector3 lookPoint = target.position + new Vector3(0f, lookHeight, 0f);
        transform.LookAt(lookPoint);
    }
}