using UnityEngine;

public class FootprintNode : MonoBehaviour
{
    [Header("Trail Connection")]
    public FootprintNode nextNode; // ➡️ รอยเท้าจุดถัดไป (ลากใส่ใน Inspector เรียงตามลำดับ)
    public bool isFinalObjective = false; // 🎯 จุดนี้คือจุดสิ้นสุด (เจอหีบสมบัติ/มอนสเตอร์เป้าหมาย)

    [Header("Visual Components")]
    public GameObject visualGraphic; // ตัวโมเดลรอยเท้า หรือ Particle Effect

    private void Awake()
    {
        // 🔒 ตอนเริ่มเกม ให้ซ่อนรอยเท้าไว้ก่อน (มองไม่เห็นด้วยตาเปล่า)
        if (visualGraphic != null) visualGraphic.SetActive(false);
    }

    // 🟢 สั่งเปิดการแสดงผลเบาะแสเมื่อโดนดมกลิ่นเจอ
    public void RevealNode()
    {
        if (visualGraphic != null) visualGraphic.SetActive(true);
    }
}