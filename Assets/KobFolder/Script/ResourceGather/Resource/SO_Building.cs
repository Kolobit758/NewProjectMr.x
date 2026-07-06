using UnityEngine;


[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Building")]
public class SO_Building : SO_ItemData
{
    [Header("Building System Connection")]
    public GameObject buildingGhostPrefab; // โมเดลโปร่งแสงสีเขียวตอนกำลังเล็งวางของพี่

    // 🟢 เขียนทับ (Override) ฟังก์ชัน ใช้ลอจิกสร้างบ้านของพี่ชายข้ามมิติมาเลย!
    public override bool UseItem(GameObject user)
    {
        // 🏗️ ค้นหาตัวผู้จัดการระบบวางบล็อกตารางในฉาก (สมมติสคริปต์ของพี่ชื่อ GridPlacementManager)
        // GridPlacementManager placementManager = FindAnyObjectByType<GridPlacementManager>();

        // if (placementManager != null)
        // {
        //     // สั่งปลุกระบบกางบล็อกตารางของพี่ แล้วส่งโมเดลสิ่งปลูกสร้างไปให้เมาส์เล็งกางบล็อกทันที!
        //     placementManager.StartPlacementMode(buildingGhostPrefab, this);

        //     Debug.Log($"[Grid System] 🏗️ เปิดพิมพ์เขียว {itemName} กางบล็อกตารางเล็งตำแหน่งสร้างบ้านแล้ว!");
        //     return true; // บอกกระเป๋าว่าเรียกใช้สำเร็จ (กระเป๋าอาจจะปิดหน้าต่างลงชั่วคราวเพื่อให้เห็นฉากสร้างบ้าน)
        // }

        return false;
    }
}