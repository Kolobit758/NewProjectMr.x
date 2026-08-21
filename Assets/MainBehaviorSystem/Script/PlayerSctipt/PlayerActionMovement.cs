using System.Collections;
using UnityEngine;

public class PlayerActionMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 720f;

    [Header("Dash Settings")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.25f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private int dashStaminaCost = 25; 
    [SerializeField] private AnimationCurve dashCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("References")]
    [SerializeField] private Transform playerModel;

    private PlayerManager playerManager; 
    private Rigidbody rb;
    private Vector3 moveDirection;
    private Vector3 dashDirection;
    private float x;
    private float z;

    private bool isDashing = false;
    private bool canDash = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // 🛠️ ล็อกการหมุนทางฟิสิกส์ทุกแกนในโค้ด เพื่อกันฟิสิกส์ภายนอกมาทำตัวละครหมุนติ้ว

        playerManager = GetComponent<PlayerManager>();
    }

    void Update()
    {
        if (isDashing) return;

        x = Input.GetAxisRaw("Horizontal");
        z = Input.GetAxisRaw("Vertical");
        moveDirection = new Vector3(x, 0f, z).normalized;

        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && HasEnoughStamina())
        {
            StartCoroutine(DashRoutine());
        }
    }

    private bool HasEnoughStamina()
    {
        if (playerManager == null || playerManager.Stats == null) return true;
        return playerManager.Stats.currentStamina >= dashStaminaCost;
    }

    void FixedUpdate()
    {
        if (isDashing) return;
        MovePlayer();
        RotatePlayerModel();
    }

    void MovePlayer()
    {
        // 🛠️ เปลี่ยนมาเคลื่อนที่ด้วยการยัดความเร็ว (Velocity) ให้ Rigidbody ตัวแม่ตรงๆ 
        // วิธีนี้จะทำให้ชนกำแพงแล้วหยุดสนิท ไม่ทะลุ 100%
        Vector3 targetVelocity = moveDirection * moveSpeed;
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
    }

    void RotatePlayerModel()
    {
        // 🛠️ ไฮไลต์เด็ด: เนื่องจากเรา Freeze Rotation Y บน Rigidbody ไว้ไม่ให้มอนสเตอร์มาหมุนเรา
        // เราจะแอบสั่งหมุนตัวละคร "ลูก" (playerModel) แทนตัวแม่ เพื่อให้หันหน้าตาม WASD ได้อิสระ!
        if (moveDirection != Vector3.zero && playerModel != null)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    private IEnumerator DashRoutine()
    {
        canDash = false;
        isDashing = true;

        if (playerManager != null && playerManager.Stats != null)
        {
            playerManager.Stats.UseStamina(dashStaminaCost);
        }

        // ทิศทางแดช: อิงตามทิศที่ playerModel (ตัวลูก) หันหน้าไปล่าสุด
        dashDirection = moveDirection != Vector3.zero ? moveDirection : playerModel.forward;
        
        Vector3 startPosition = rb.position;
        Vector3 targetPosition = startPosition + dashDirection * dashDistance;
        float elapsedTime = 0f;

        // 🛠️ ระบบแดชแบบฟิสิกส์: ขยับผ่าน rb.MovePosition จะทำให้ตอนแดชถ้าชนกำแพง มันจะติดกำแพง ไม่ทะลุฉาก!
        while (elapsedTime < dashDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / dashDuration;
            float curveValue = dashCurve.Evaluate(normalizedTime);

            Vector3 currentTargetPos = Vector3.Lerp(startPosition, targetPosition, curveValue);
            
            // ใช้ MovePosition ตอนปิด Kinematic จะเป็นการสไลด์แบบฟิสิกส์ ชนคือติด!
            rb.MovePosition(currentTargetPos);

            yield return null;
        }

        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}