using Unity.VisualScripting;
using UnityEngine;

public class FarmingManager : MonoBehaviour
{
    public static FarmingManager Instance { get; private set; }

    [Header("Selected Seed Data")]
    public SO_PlantData currentSelectedSeed;
    public LayerMask farmPlotLayer;

    [Header("Shop Selection State (อย่าไปตั้งเองใน Inspector)")]
    // 🟢 true = เมล็ดนี้เลือกมาจากร้าน ยังไม่ได้จ่ายเงิน จะหักเงินตอน "วางจริง" เท่านั้น
    [HideInInspector] public bool currentSeedRequiresPayment = false;
    [HideInInspector] public int currentSeedPrice = 0;

    [Header("Gold Settings")]
    [Tooltip("SO_ItemData ของทองคำ ใช้เช็ค/หักตอนวางพืชที่มาจากร้าน")]
    public SO_ItemData goldItemData;

    public Camera cam;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Update()
    {
        if (GridPlacementManager.Instance != null && GridPlacementManager.Instance.IsPlacementModeActive) return; // 🟢

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Try to plant");
            TryInteractWithPlot();
        }
    }

    // 🟢 เรียกจาก PlantShopUI ตอนกดเลือกเมล็ดที่ร้าน -> แค่ "ถือ" ไว้ ยังไม่หักเงิน
    public void SelectSeedFromShop(SO_PlantData seedData, int price)
    {
        currentSelectedSeed = seedData;
        currentSeedRequiresPayment = true;
        currentSeedPrice = price;
    }

    void TryInteractWithPlot()
    {
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, farmPlotLayer))
        {
            CropPlots plot = hit.collider.GetComponent<CropPlots>();
            if (plot == null) plot = hit.collider.GetComponentInChildren<CropPlots>();

            if (plot != null)
            {
                // เคสที่ 1: แปลงว่างและผู้เล่นถือเมล็ดอยู่ -> สั่งปลูก
                if (plot.currentStage == CropStage.Empty && currentSelectedSeed != null)
                {
                    // 🟢 ถ้าเมล็ดนี้มาจากร้าน (ยังไม่จ่ายเงิน) เช็คเงินตรงนี้ก่อนวางจริง
                    if (currentSeedRequiresPayment)
                    {
                        if (ResourceInventory.Instance == null || goldItemData == null)
                        {
                            Debug.LogWarning("[Farming] ระบบเงินยังไม่พร้อม (ResourceInventory / goldItemData หาย) ปลูกไม่ได้");
                            return;
                        }

                        int currentGold = ResourceInventory.Instance.GetResourceAmount(goldItemData.itemName);
                        if (currentGold < currentSeedPrice)
                        {
                            Debug.Log("[Farming] 💸 เงินไม่พอสำหรับปลูกเมล็ดนี้ (ยังถือเมล็ดไว้อยู่ ลองช่องอื่นหรือหาเงินเพิ่มได้)");
                            return; // ไม่พอ -> ไม่ปลูก ไม่หักเงิน ไม่เคลียร์เมล็ดที่ถืออยู่
                        }
                    }

                    bool success = plot.PlantSeed(currentSelectedSeed);
                    if (success)
                    {
                        Debug.Log($"🌱 [Farming]: ปลูกเมล็ด {currentSelectedSeed.itemName} สำเร็จ!");

                        if (currentSeedRequiresPayment)
                        {
                            // 🟢 คิดเงินตอนวางจริงเท่านั้น (ไม่คิดตอนกดเลือกที่ร้าน)
                            ResourceInventory.Instance.ConsumeResource(goldItemData, currentSeedPrice, false);
                        }
                        else if (ResourceInventory.Instance != null)
                        {
                            // flow เดิม: เลือกเมล็ดจากกระเป๋าปกติ (ไม่ใช่จากร้าน) -> หักเมล็ดจากกระเป๋าเหมือนเดิม
                            ResourceInventory.Instance.ConsumeResource(currentSelectedSeed, 1);
                        }

                        currentSelectedSeed = null; // ปลูกเสร็จแล้ว เคลียร์มือ
                        currentSeedRequiresPayment = false;
                        currentSeedPrice = 0;
                    }
                }
                // เคสที่ 2: แปลงพร้อมเก็บเกี่ยว -> สั่งเก็บเกี่ยวทันที
                else if (plot.currentStage == CropStage.ReadyToHarvest)
                {
                    plot.HarvestCrop();
                }
            }

        }
    }
}