using Unity.VisualScripting;
using UnityEngine;

public class FertilizerManager2 : MonoBehaviour
{
    public static FertilizerManager2 Instance { get; private set; }

    [Header("Selected Fertilizer Data")]
    public SO_FertilizerData currentSelectedFertilizer;
    public LayerMask farmPlotLayer;
    public Camera cam;
    private bool isFertilizeMode = false;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        if (cam == null) cam = Camera.main;
    }

    public void StartFertilizeMode(SO_FertilizerData fertilizerData)
    {
        currentSelectedFertilizer = fertilizerData;
        isFertilizeMode = true;
        Debug.Log($"🧪 [Fertilizer System] เปิดโหมดใส่ปุ๋ย {fertilizerData.itemName} แล้ว! คลิกซ้ายที่แปลงผัก");
    }

    void Update()
    {


        if (!isFertilizeMode || currentSelectedFertilizer == null) return;
        if (GridPlacementManager.Instance != null && GridPlacementManager.Instance.IsPlacementModeActive) return; // 🟢

        if (Input.GetMouseButtonDown(0))
        {
            ApplyFertilizerToClosestPlot();
            isFertilizeMode = false;
        }
        else if (Input.GetMouseButtonDown(1))
        {
            isFertilizeMode = false;
            currentSelectedFertilizer = null;
            Debug.Log("🚫 ยกเลิกโหมดใส่ปุ๋ย");
        }
    }

    void ApplyFertilizerToClosestPlot()
    {
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, farmPlotLayer))
        {
            CropPlots plot = hit.collider.GetComponent<CropPlots>();
            if (plot == null) plot = hit.collider.GetComponentInChildren<CropPlots>();

            if (plot != null)
            {

                // สมมติว่าในสคริปต์ CropPlots ของคุณมีฟังก์ชันใส่ปุ๋ย (ถ้ายังไม่มี ให้ไปเพิ่มฟังก์ชันรับปุ๋ยใน CropPlots ต่อได้เลยครับ)
                plot.ApplyFertilizer(currentSelectedFertilizer);

                // หักปุ๋ยออกจากกระเป๋า 1 ชิ้น
                if (ResourceInventory.Instance != null)
                {
                    ResourceInventory.Instance.ConsumeResource(currentSelectedFertilizer, 1);
                }

                currentSelectedFertilizer = null;
            }
        }
    }
}