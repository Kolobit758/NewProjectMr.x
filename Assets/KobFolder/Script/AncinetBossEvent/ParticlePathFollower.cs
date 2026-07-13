using UnityEngine;

public class ParticlePathFollower : MonoBehaviour
{
    private Vector3[] pathPoints;
    private int currentPointIndex = 0;
    private Vector3 finalBossPos; // พิกัดบอสตัวจริงท้ายที่สุด
    public float speed = 15f;

    [Header("Player Tracking Settings")]
    public float playerWaitRadius = 5f; // รัศมีหยุดรอผู้เล่น
    private Transform playerTransform;
    private bool isInitialized = false;

    void Awake()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    // 🟢 ส่งทั้งอาเรย์หัวโค้ง และ พิกัดบอสตัวจริงมาพร้อมกันเลย!
    public void SetupPath(Vector3[] points, Vector3 bossPosition)
    {
        pathPoints = points;
        currentPointIndex = 0;
        finalBossPos = bossPosition + (Vector3.up * 1.2f); // ล็อกพิกัดบอสลอยฟ้าไว้
        
        // ยกลอยตัวลูกแก้วทันทีที่เกิด จะได้ไม่จมดิน
        transform.position = transform.position + (Vector3.up * 1.2f);
        
        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized || pathPoints == null) return;

        // 🛑 [FIX บั๊กหายกลางทาง]: เช็คว่าถึงตัวบอสปลาหมึกยักษ์จริงๆ หรือยัง!
        // ตราบใดที่ยังไม่ถึงตัวบอสในระยะ 1.5 เมตร มันห้ามทำลายตัวเองเด็ดขาด!
        if (Vector3.Distance(transform.position, finalBossPos) < 1.5f)
        {
            Debug.Log("🐙 ลูกแก้วนำทางพากองทัพมาถึงตัวบอสปลาหมึกเรียบร้อย!");
            Destroy(gameObject, 0.5f); // ถึงบอสแล้วค่อยยอมสลายร่างทิ้ง
            return;
        }

        // 🛑 ดักรอผู้เล่น (ถ้าทิ้งห่างผู้เล่นเกินรัศมี ให้ลอยนิ่งๆ รอ)
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            
            // ปล่อยให้โค้งแรก (Index 0) พุ่งนำไปก่อน แต่โค้งถัดไปถ้าผู้เล่นตามไม่ทันจะหยุดรอ
            if (currentPointIndex > 0 && distanceToPlayer > playerWaitRadius)
            {
                return; // เบรกหยุดรออยู่กลางอากาศ
            }
        }

        // 🏃 คำนวณหาจุดหมายหัวโค้งถัดไป
        Vector3 targetPosition;
        
        // ถ้ายังมีหัวโค้งตามทาง NavMesh ให้เลี้ยวตามโค้งไปก่อน
        if (currentPointIndex < pathPoints.Length)
        {
            targetPosition = pathPoints[currentPointIndex] + (Vector3.up * 1.2f);
        }
        else
        {
            // ถ้าหมดหัวโค้งแล้วแต่ยังไม่ถึงตัวบอส (เพราะสิ้นสุดแผ่น NavMesh) ให้พุ่งตรงดิ่งเข้าหาตัวบอสเลย!
            targetPosition = finalBossPos;
        }

        // สั่งเคลื่อนที่เลี้ยวไปตามทาง
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        // เช็คว่าเดินไปชนหัวโค้งปัจจุบันหรือยัง เพื่อเปลี่ยนไปโค้งถัดไป
        if (currentPointIndex < pathPoints.Length && Vector3.Distance(transform.position, targetPosition) < 0.6f)
        {
            currentPointIndex++; // เลี้ยวไปโค้งถัดไป
        }
    }
}