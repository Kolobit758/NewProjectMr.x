using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerActionController : MonoBehaviour
{
    void Update()
    {
        // 🖱️ 1. ดักจับการคลิกเมาส์ซ้าย
        if (Input.GetMouseButtonDown(0))
        {
            // 🛡️ ดักเซฟตี้ชั้นที่ 1: ถ้าผู้เล่นกำลังคลิกโดนปุ่ม UI หรือกำลังเปิดหน้าต่างกระเป๋าใหญ่จัดของอยู่ 
            // ห้ามลั่น UseItem เด็ดขาด (ป้องกันบั๊กคลิกทะลุหน้าจอ UI ไปฟันลมข้างหลัง)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // 🏗️ เรียกใช้งานไอเทมในมือตามช่อง Toolbar ปัจจุบัน
            TriggerItemUsage();
        }
    }

    private void TriggerItemUsage()
    {
        if (ResourceInventory.Instance == null) return;

        // 🔍 2. วิ่งไปถามตู้ Toolbar ว่าตอนนี้ผู้เล่นกำลังเลือกถือช่องไหนอยู่ (0-7)
        KobToolbarUI toolbarUI = FindAnyObjectByType<KobToolbarUI>();
        if (toolbarUI == null) return;

        int activeSlotIndex = toolbarUI.GetCurrentSelectedIndex();

        // 🎒 3. ดึงข้อมูลจริงใน RAM จากช่อง Toolbar นั้นออกมาแกะดู
        InventorySlotData activeSlot = ResourceInventory.Instance.slots[activeSlotIndex];

        // 💥 4. ถ้าช่องนั้นมีของอยู่... ลั่นเมาส์ซ้ายเรียก UseItem() ทันที!
        if (activeSlot != null && !activeSlot.IsEmpty && activeSlot.itemData != null)
        {
            // ส่งตัวผู้เล่น (gameObject) เข้าไปในฟังก์ชัน UseItem ตัวแม่
            // - ถ้าถือดาบคอมโบ (WeaponData) ➡️ มันจะสืบทอดไปสั่งระบบต่อสู้ฟันฮิต 1, 2, 3 ทันที!
            // - ถ้าถือปุ๋ย (Fertilizer) ➡️ มันจะรันลอจิกโปรยปุ๋ย
            bool useSuccess = activeSlot.itemData.UseItem(gameObject);

            // 🎒 ลอจิกไอเทมใช้แล้วหมดไป: ถ้าใช้สำเร็จ และไอเทมเป็นประเภทปุ๋ย/อาหาร ให้หักจำนวนออก 1 ชิ้น
            if (useSuccess && activeSlot.itemData.itemType == ItemType.Fertilizer)
            {
                activeSlot.amount--;
                
                if (activeSlot.amount <= 0)
                {
                    activeSlot.Clear(); // ล้างช่องว่าง
                }

                // สั่งให้ทุก Canvas ตื่นมารีเฟรชภาพแร่อัปเดตตัวเลขจำนวนล่าสุดพร้อมกัน
                ResourceInventory.Instance.NotifyChanged();
            }
        }
        else
        {
            // ❌ ถ้าคลิกซ้ายตอนช่อง Toolbar ว่างเปล่า (มือเปล่า) ให้รันอนิเมชั่นต่อยหมัดเปล่าตรงนี้ได้เลยเพื่อน!
            Debug.Log("[Player] 👊 ใช้หมัดลุย! ต่อยหมัดเปล่าชวนทะเลาะ");
        }
    }
}