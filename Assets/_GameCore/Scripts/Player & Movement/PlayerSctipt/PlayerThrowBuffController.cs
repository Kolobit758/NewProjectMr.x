using UnityEngine;

public class PlayerThrowBuffController : MonoBehaviour
{
    [Header("Skill Settings")]
    [SerializeField] private SO_PlantProduct currentPlantProduct;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform throwPoint; 

    [Header("Preview UI Assets")]
    [SerializeField] private GameObject previewCirclePrefab; 

    private GameObject activePreviewCircle;
    private bool isAiming = false;

    void Update()
    {
        // 🔄 ระหว่างที่กำลังเปิดโหมดเล็งอยู่: ให้ขยับวงกลม Preview สีแดงตามแนวเมาส์ไปบนพื้นเรื่อยๆ
        if (isAiming && currentPlantProduct != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
            {
                if (activePreviewCircle != null)
                {
                    activePreviewCircle.SetActive(true);
                    activePreviewCircle.transform.position = hit.point;

                    float size = currentPlantProduct.aoeRadius * 2f;
                    activePreviewCircle.transform.localScale = new Vector3(size, 0.05f, size);
                }

                // 🟢 คลิกซ้ายครั้งที่สองในขณะเล็งอยู่ -> ขว้างระเบิดพืชทันที!
                if (Input.GetMouseButtonDown(0))
                {
                    ExecuteThrow(hit.point);
                }
            }
            else
            {
                // เซฟตี้ดักจับ: ถ้าเมาส์หลุดออกจากพื้น Ground เล็งว่อนลอยฟ้า แต่ดันทุรังคลิกซ้าย
                // ให้ปิดโหมดเล็งทันที ของจะได้ไม่ค้างกลางอากาศ
                if (Input.GetMouseButtonDown(0))
                {
                    EndAiming();
                }
            }
        }

        // 🖱️ กดคลิกขวาเพื่อยกเลิกการเล็งยาพืช หันกลับไปถืออาวุธตามเดิม
        if (Input.GetMouseButtonDown(1) && isAiming)
        {
            EndAiming();
        }
    }

    // 🟢 ฟังก์ชันรับลงทะเบียนจากสลอต Toolbar ข้ามมิติมา
    public void SetupPlantProduct(SO_PlantProduct plantData)
    {
        if (plantData == null) return;

        currentPlantProduct = plantData;
        isAiming = true;
        
        if (previewCirclePrefab != null)
        {
            // ถ้าไม่มีวงกลมพรีวิวเลยค่อยเสกใหม่ แต่ถ้ามีเก่าจมอยู่ให้ปลุกขึ้นมาใช้งาน
            if (activePreviewCircle == null)
            {
                activePreviewCircle = Instantiate(previewCirclePrefab);
            }
            activePreviewCircle.SetActive(true);
        }
    }

    private void ExecuteThrow(Vector3 targetPosition)
    {
        if (currentPlantProduct == null || currentPlantProduct.productPrefab == null) return;

        Debug.Log($"[Skill] 🍃 ขว้างระเบิดพืช {currentPlantProduct.productName} ออกไปแล้ว!");
        
        Vector3 spawnPos = throwPoint != null ? throwPoint.position : transform.position + Vector3.up;
        GameObject projectile = Instantiate(currentPlantProduct.productPrefab, spawnPos, Quaternion.identity);

        if (!projectile.TryGetComponent<PlantBuffProjectile>(out var curveScript))
        {
            curveScript = projectile.AddComponent<PlantBuffProjectile>();
        }

        curveScript.Initialize(currentPlantProduct, spawnPos, targetPosition);

        // 🎒 สั่งงานกระเป๋าหลัก: หักไอเทมน้ำยาพืชชิ้นนี้ออกจากช่องถือในมือ
        KobToolbarUI toolbarUI = FindAnyObjectByType<KobToolbarUI>();
        if (toolbarUI != null && ResourceInventory.Instance != null)
        {
            int slotIdx = toolbarUI.GetCurrentSelectedIndex();
            
            // ดักความปลอดภัยป้องกันดัชนีเกินขอบตาราง
            if (slotIdx >= 0 && slotIdx < ResourceInventory.Instance.slots.Length)
            {
                InventorySlotData activeSlot = ResourceInventory.Instance.slots[slotIdx];
                
                if (activeSlot != null && activeSlot.itemData == currentPlantProduct)
                {
                    activeSlot.amount--;
                    if (activeSlot.amount <= 0) activeSlot.Clear();
                    
                    // 🟢 FIX SUCCESS: สั่งคลังหลักสั่งรันลั่นกระดิ่งรีเฟรชภาพสดตรงๆ ไม่ใช้ดัชนีติดลบมั่วซั่วแล้ว!
                    ResourceInventory.Instance.NotifyChanged(); 

                    // บังคับให้ระบบ SaveGame ตัวอัปเดตข้อมูลเซฟล่าสุดทับลงดิสก์ทันทีของจะได้ไม่โกงคืนชีพ
                    if (SaveLoadManager.Instance != null)
                    {
                        SaveLoadManager.Instance.MarkInventoryDirty();
                        SaveLoadManager.Instance.SaveGame();
                    }
                }
            }
        }

        EndAiming();
    }

    public void EndAiming()
    {
        isAiming = false;
        currentPlantProduct = null;
        if (activePreviewCircle != null)
        {
            activePreviewCircle.SetActive(false);
        }
    }
}