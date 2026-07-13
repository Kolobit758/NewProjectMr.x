using UnityEngine;

public class HiddenChest : MonoBehaviour
{
    [Header("Visual Settings")]
    public GameObject chestModel;         // ลากโมเดลกล่องสมบัติมาใส่
    public GameObject spawnParticleEffect; // Prefab เอฟเฟกต์แสง/ฝุ่นระเบิดตอนกล่องโผล่ (ถ้ามี)

    [Header("Animation Setup")]
    public float riseHeight = 1.5f;       // ระยะความสูงที่กล่องจะพุ่งขึ้นมาเหนือพื้น
    public float riseSpeed = 3f;          // ความเร็วในการเลื่อนขึ้น

    private Vector3 targetVisiblePosition;
    private bool isRising = false;

    void Start()
    {
        // บันทึกตำแหน่งเปิดเผยที่ควรจะเป็นบนพื้นโลก
        targetVisiblePosition = transform.position;

        // 🔒 ตอนเริ่มเกม: สั่งโยกตำแหน่งตัวกล่องสมบัติมุดลงไปซ่อนใต้ดินล่วงหน้า และปิดกราฟิกไว้ก่อน
        transform.position = targetVisiblePosition + (Vector3.down * riseHeight);
        if (chestModel != null) chestModel.SetActive(false);
    }

    void Update()
    {
        // ลูปค่อยๆ ย้ายพิกัดเลื่อนกล่องสมบัติขึ้นมาจากใต้ดินอย่างนุ่มนวล
        if (isRising)
        {
            transform.position = Vector3.Lerp(transform.position, targetVisiblePosition, Time.deltaTime * riseSpeed);
            
            // ถ้าเลื่อนขึ้นมาจนใกล้พิกัดเป้าหมายแล้ว ให้ล็อกสิทธิ์หยุดเลื่อน
            if (Vector3.Distance(transform.position, targetVisiblePosition) < 0.01f)
            {
                transform.position = targetVisiblePosition;
                isRising = false;
                Debug.Log("[Chest] 🎯 กล่องสมบัติขึ้นมาสแตนบายบนดินร้อยเปอร์เซ็นต์!");
            }
        }
    }

    /// <summary>
    /// ฟังก์ชันที่จะถูกสัตว์เลี้ยงเรียกใช้หลังจากขุดดินตรงจุดสุดท้ายสำเร็จ
    /// </summary>
    public void SpawnChest()
    {
        if (chestModel != null) chestModel.SetActive(true); // เปิดการแสดงผลกราฟิกกล่อง
        isRising = true; // เปิดสวิตช์รันลูปใน Update ให้กล่องเลื่อนขึ้นบนพื้น

        // เสกเอฟเฟกต์ฝุ่นระเบิดกระจายรอบกล่องเพิ่มความอลังการ
        if (spawnParticleEffect != null)
        {
            Instantiate(spawnParticleEffect, targetVisiblePosition, Quaternion.identity);
        }

        Debug.Log("[Chest] 💥 ปลดล็อกกล่องความลับโผล่ขึ้นมาจากใต้ดินเรียบร้อย!");
    }
}