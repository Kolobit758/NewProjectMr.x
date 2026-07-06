using UnityEngine;

public class PlantBuffProjectile : MonoBehaviour
{
    private SO_PlantProduct data;
    private Vector3 startPos;
    private Vector3 targetPos;
    private float timeElapsed = 0f;

    // ✅ ฟังก์ชันเริ่มต้น: เรียกใช้ทันทีตอนที่สปอว์นลูกพืชนี้ออกมาจากตัวผู้เล่น
    public void Initialize(SO_PlantProduct plantData, Vector3 start, Vector3 target)
    {
        data = plantData;
        startPos = start;
        targetPos = target;
        timeElapsed = 0f;
    }

    void Update()
    {
        // กันเหนียวถ้าไม่มีข้อมูลให้รีเทิร์นกลับไป
        if (data == null) return;

        // คำนวณความคืบหน้าของเวลาการบิน (0 ไปถึง 1)
        timeElapsed += Time.deltaTime;
        float progress = timeElapsed / data.throwDuration;

        // ✅ เมื่อบินไปถึงจุดเป้าหมาย (หมดเวลาเดินทาง) ให้สั่งระเบิดสร้างวงทันที
        if (progress >= 1f)
        {
            ExplodeAndCreateZone();
            return;
        }

        // 1. คำนวณตำแหน่งแนวราบ แกน X และ Z (วิ่งเป็นเส้นตรงเข้าหาจุดหมาย)
        Vector3 currentXZ = Vector3.Lerp(startPos, targetPos, progress);

        // 2. คำนวณความสูง แกน Y ให้เด้งโค้งขึ้นฟ้าแล้วร่วงลงมา (Parabola Curve)
        float currentY = Mathf.Lerp(startPos.y, targetPos.y, progress) + 
                         (4f * data.throwArcHeight * progress * (1f - progress));

        // 3. ปรับเปลี่ยนตำแหน่งของออบเจกต์ในเฟรมนี้
        transform.position = new Vector3(currentXZ.x, currentY, currentXZ.z);

        // 4. หมุนตัวลูกพืชควงสว่านไปข้างหน้าเรื่อยๆ เพื่อความสวยงามสมจริง
        transform.Rotate(Vector3.right * 360f * Time.deltaTime);
    }

    // ✅ ฟังก์ชันระเบิด: สปอว์นวง AOE บนพื้นและแจกจ่ายบัฟให้ยูนิต
    void ExplodeAndCreateZone()
    {
        // 1. สร้างพื้นที่วงกลม AOE ณ จุดตกกระทบ (targetPos) ที่เราเล็งไว้ตั้งแต่แรก
        if (data.aoeZonePrefab != null)
        {
            GameObject zone = Instantiate(data.aoeZonePrefab, targetPos, Quaternion.identity);
            
            // 2. ส่งค่าข้อมูลและประเภทบัฟไปให้สคริปต์กางรัศมี (BuffAOEZone)
            if (zone.TryGetComponent<BuffAOEZone>(out var aoeZone))
            {
                aoeZone.Setup(data);
            }
        }

        // 3. ทำลายโมเดลลูกพืชที่ลอยมานี้ทิ้งทันที
        Destroy(gameObject);
    }
}