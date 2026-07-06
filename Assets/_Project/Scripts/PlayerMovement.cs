using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    public float rotationSpeed = 12f;

    private void Update()
    {
        Move();
    }

    private void Move()
    {
        float horizontal = Input.GetAxisRaw("Horizontal"); // A D
        float vertical = Input.GetAxisRaw("Vertical"); // W S

        Vector3 inputDirection = new Vector3 (horizontal, 0f, vertical).normalized;

        if (inputDirection.magnitude <= 0f)
            return;

        bool isRunnig = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isRunnig ? runSpeed : moveSpeed;

        transform.position += inputDirection * currentSpeed * Time.deltaTime;

        Quaternion targetRotation = Quaternion.LookRotation(inputDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime  
        );
    }
}
