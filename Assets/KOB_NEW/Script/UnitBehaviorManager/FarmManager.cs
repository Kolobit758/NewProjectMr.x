using UnityEngine;
using System.Collections.Generic;

public class FarmManager : MonoBehaviour
{
    public static FarmManager Instance { get; private set; }

    [Header("Farm Plots Management")]
    public List<CropPlots> allPlots = new List<CropPlots>();
    public SO_PlantData currentSelectedSeed;
    public LayerMask farmPlotLayer;
    public Camera cam;

    private FarmJob activeFarmJob;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        CropPlots[] plots = Object.FindObjectsByType<CropPlots>(FindObjectsSortMode.None);
        allPlots.AddRange(plots);
    }

    void Update()
    {
        // 🟢 คลิกซ้ายเพื่อผู้เล่นปลูกเมล็ด/เก็บเกี่ยวด้วยตัวเอง
        if (Input.GetMouseButtonDown(0))
        {
            TryInteractWithPlot();
        }
    }

    public CropPlots GetAvailablePlotForUnit(UnitBase unit, List<CropPlots> excludedPlots)
    {
        foreach (var plot in allPlots)
        {
            if (plot == null) continue;
            if (excludedPlots.Contains(plot)) continue;
            
            // แปลงว่างจะถูกเลือกก็ต่อเมื่อ Manager มีเมล็ดพันธุ์พร้อมปลูก
            if (plot.currentStage == CropStage.Empty && currentSelectedSeed == null) continue;

            if (!plot.isBeingServiced)
            {
                return plot;
            }
        }
        return null;
    }

    public void AssignUnitsToFarm(List<UnitBase> units)
    {
        if (activeFarmJob == null)
        {
            GameObject jobObj = new GameObject("ActiveFarmJob");
            activeFarmJob = jobObj.AddComponent<FarmJob>();
            activeFarmJob.Init(allPlots);
        }

        foreach (var unit in units)
        {
            JobManager.Instance.CancelAllJobsForUnit(unit);
            activeFarmJob.AssignUnit(unit);
        }
    }

    void TryInteractWithPlot()
    {
        if (cam == null) return;
        
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, farmPlotLayer))
        {
            CropPlots plot = hit.collider.GetComponent<CropPlots>() ?? hit.collider.GetComponentInParent<CropPlots>();
            if (plot != null)
            {
                // เคสที่ 1: แปลงว่าง และผู้เล่นถือเมล็ดอยู่ในมือ
                if (plot.currentStage == CropStage.Empty)
                {
                    if (currentSelectedSeed != null)
                    {
                        bool success = plot.PlantSeed(currentSelectedSeed);
                        if (success)
                        {
                            Debug.Log($"🌱 ปลูกเมล็ด {currentSelectedSeed.itemName} สำเร็จ!");
                            ResourceInventory.Instance?.ConsumeResource(currentSelectedSeed, 1);
                            currentSelectedSeed = null; // หยอดเสร็จ เคลียร์มือ
                        }
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ คุณยังไม่ได้เลือก/ถือเมล็ดพันธุ์ในมือ! (คลิกใช้เมล็ดในกระเป๋าก่อน)");
                    }
                }
                // เคสที่ 2: แปลงพร้อมเก็บเกี่ยว
                else if (plot.currentStage == CropStage.ReadyToHarvest)
                {
                    plot.HarvestCrop();
                }
            }
        }
    }
}