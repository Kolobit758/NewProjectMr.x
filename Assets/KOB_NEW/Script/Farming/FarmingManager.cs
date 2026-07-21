using UnityEngine;

public class FarmingManager : MonoBehaviour
{
    public static FarmingManager Instance { get; private set; }

    [Header("Selected Seed Data")]
    public SO_PlantData currentSelectedSeed; 
    public LayerMask farmPlotLayer;

    public Camera cam;          

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Update()
    {
        // คลิกซ้ายเพื่อปลูกเมล็ดหรือเก็บเกี่ยวด้วยตัวเอง (ระบบเดิมของผู้เล่น)
        if (Input.GetMouseButtonDown(0))
        {
            TryInteractWithPlot();
        }
    }

    void TryInteractWithPlot()
    {
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, farmPlotLayer))
        {
            CropPlots plot = hit.collider.GetComponent<CropPlots>();
            if (plot == null) plot = hit.collider.GetComponentInParent<CropPlots>();

            if (plot != null)
            {
                // เคสที่ 1: แปลงว่างและผู้เล่นถือเมล็ดอยู่ -> สั่งปลูก
                if (plot.currentStage == CropStage.Empty && currentSelectedSeed != null)
                {
                    bool success = plot.PlantSeed(currentSelectedSeed);
                    if (success)
                    {
                        Debug.Log($"🌱 [Farming]: ปลูกเมล็ด {currentSelectedSeed.itemName} สำเร็จ!");

                        if (ResourceInventory.Instance != null)
                        {
                            ResourceInventory.Instance.ConsumeResource(currentSelectedSeed, 1);
                        }

                        currentSelectedSeed = null; // ปลูกเสร็จแล้ว เคลียร์มือ
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