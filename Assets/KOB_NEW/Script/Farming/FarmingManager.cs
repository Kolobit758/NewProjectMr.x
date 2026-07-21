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
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Try to plant");
            TryInteractWithPlot();
        }
    }

    void TryInteractWithPlot()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, farmPlotLayer))
        {
            CropPlots plot = hit.collider.GetComponent<CropPlots>();
            if (plot == null) plot = hit.collider.GetComponentInParent<CropPlots>();

            if (plot != null)
            {
                if (plot.currentStage == CropStage.Empty && currentSelectedSeed != null)
                {
                    bool success = plot.PlantSeed(currentSelectedSeed);
                    if (success)
                    {
                        Debug.Log($"🌱 [Farming]: ปลูกเมล็ด {currentSelectedSeed.itemName} สำเร็จ!");

                        if (ResourceInventory.Instance != null)
                        {
                            // 🟢 แก้บรรทัดนี้: ส่ง .itemName แทนตัว Object ดิบๆ
                            ResourceInventory.Instance.ConsumeResource(currentSelectedSeed, 1);
                        }

                        currentSelectedSeed = null; // ปลูกเสร็จแล้ว เคลียร์มือ
                    }
                }
                else if (plot.currentStage == CropStage.ReadyToHarvest)
                {
                    plot.HarvestCrop();
                }
            }
        }
    }
}