using UnityEngine;

public class FormationTestTools : MonoBehaviour
{
    [Header("ยูนิตเริ่มต้นสำหรับจำลองการจับสัตว์")]
    public UnitDataSO testUnitData;

    [Header("ตั้งค่าพิกัดสำหรับทดสอบส่งลงสล็อตจัดทีม")]
    public int targetRow;
    public int targetCol;

    [ContextMenu("🐺 STEP 1: จับสัตว์เลี้ยงเข้าคลังสำรอง")]
    public void TestCatchAnimal()
    {
        if (testUnitData == null) { Debug.LogError("กรุณาใส่ข้อมูล Test Unit Data ก่อนครับ"); return; }
        UnitInstance testUnit = new UnitInstance(testUnitData);
        AnimalInventory.Instance.AddUnitToInventory(testUnit);
    }

    [ContextMenu("🧪 STEP 2: ดึงสัตว์ตัวล่าสุดในคลังไปใส่ในพิกัดช่องว่าง")]
    public void TestAssignToSlot()
    {
        if (AnimalInventory.Instance == null || TeamFormationManager.Instance == null)
        {
            Debug.LogError("ไม่พบ AnimalInventory หรือ TeamFormationManager ในฉาก!");
            return;
        }

        var inventory = AnimalInventory.Instance.inventoryUnits;
        if (inventory.Count == 0)
        {
            Debug.LogWarning("ไม่มีสัตว์ป่าอยู่ในคลังเลย! โปรดกด STEP 1 เพื่อจับสัตว์ก่อน");
            return;
        }

        // ดึงสัตว์ตัวล่าสุดที่เพิ่งจับได้ในกระเป๋ามาทดสอบจัดทีม
        UnitInstance lastCaughtUnit = inventory[inventory.Count - 1];
        
        // 🟢 แก้ไขจุดที่ 1: เปลี่ยนมาส่งข้อมูลผ่าน uniqueId เข้าฟังก์ชันตัวใหม่ของระบบ
        bool success = TeamFormationManager.Instance.AssignUnitToSlotById(lastCaughtUnit.uniqueId, targetRow, targetCol);
        if (!success)
        {
            Debug.LogWarning($"จัดทีมไม่สำเร็จ! ช่องพิกัด Row:{targetRow}, Col:{targetCol} อาจจะโดนล็อกอยู่ (ไม่ใช่สีเขียว) หรือไม่มีสล็อตนี้");
        }
    }

    [ContextMenu("🧹 STEP 3: ถอดทหารออกจากพิกัดที่ตั้งไว้")]
    public void TestRemoveFromSlot()
    {
        if (TeamFormationManager.Instance == null) return;

        // 🟢 แก้ไขจุดที่ 2: ดึงตัวที่ประจำพิกัดนั้นขึ้นมาดูก่อนเพื่อเอา uniqueId ไปสั่งถอดออกจากทีม
        var assignedSlot = TeamFormationManager.Instance.activeFormation.Find(a => a.row == targetRow && a.col == targetCol);
        
        if (assignedSlot != null && !string.IsNullOrEmpty(assignedSlot.unitUniqueId))
        {
            TeamFormationManager.Instance.RemoveUnitFromFormationById(assignedSlot.unitUniqueId);
            Debug.Log($"ถอดทหาร {assignedSlot.unitUniqueId} ออกจากช่อง Row:{targetRow}, Col:{targetCol} เรียบร้อย");
        }
        else
        {
            Debug.LogWarning($"ไม่พบสัตว์เลี้ยงตัวใดประจำการอยู่ที่ช่อง Row:{targetRow}, Col:{targetCol} จึงไม่สามารถถอดออกได้");
        }
    }
}