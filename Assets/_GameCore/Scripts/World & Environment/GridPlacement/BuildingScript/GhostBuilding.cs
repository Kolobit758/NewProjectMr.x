using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class GhostBuilding : MonoBehaviour, ITaskable
{
    public SO_Building buildingData;
    public float totalWorkRequired = 100f;
    public float currentWorkDone = 0f;

    private HashSet<UnitBase> activeUnits = new HashSet<UnitBase>();

    [Header("Ghost Idle Bounce (ตอนกำลังสร้าง)")]
    public float ghostBounceFrequency;
    public float minAmplitude;
    public float maxAmplitude;
    private Vector3 ghostBaseScale = Vector3.one;

    [Header("UI References")]
    private GameObject sliderPrefab;            // 🟢 รับ Prefab มาจาก GridPlacementManager
    private float uiOffsetY = 1f;
    private Slider progressSlider;
    private GameObject canvasInstance;

    // 🟢 อัปเดตฟังก์ชันรับค่า Initialize ให้รองรับ Prefab ของ Slider
    public void InitializeGhost(SO_Building data, GameObject uiPrefab, float customOffsetY,float ghostBounceFrequency,float minAmplitude,float maxAmplitude)
    {
        buildingData = data;
        sliderPrefab = uiPrefab;
        uiOffsetY = customOffsetY;
        this.ghostBounceFrequency = ghostBounceFrequency;
        this.minAmplitude = minAmplitude;
        this.maxAmplitude = maxAmplitude;
    }

    void Start()
    {
        MeshRenderer[] renders = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer render in renders)
        {
            render.material.color = new Color(0, 0.5f, 1f, 0.5f);
        }

        ghostBaseScale = transform.localScale;
        CreateWorldSpaceUI();

        if (buildingData != null)
        {
            Debug.Log("Start build : " + buildingData.itemName);
        }
    }

    void Update()
    {
        float speedPerSecond = 0f;
        foreach (var unit in activeUnits)
        {
            speedPerSecond += unit.buildSpeed;
        }

        if (speedPerSecond > 0)
        {
            currentWorkDone += speedPerSecond * Time.deltaTime;
            UpdateUI();

            if (currentWorkDone >= totalWorkRequired)
            {
                FinishBuilding();
            }
        }

        UpdateBounceAnimation();
    }

    private void CreateWorldSpaceUI()
    {
        if (sliderPrefab != null)
        {
            // 🟢 สร้าง UI จาก Prefab ที่ส่งมาจาก GridPlacementManager
            canvasInstance = Instantiate(sliderPrefab, transform);

            // จัดตำแหน่งให้อยู่เหนือหัวตึกตาม Offset Y
            canvasInstance.transform.localPosition = new Vector3(0f, uiOffsetY, 0f);

            // หา Slider Component ภายใน Prefab
            progressSlider = canvasInstance.GetComponentInChildren<Slider>();
            if (progressSlider != null)
            {
                progressSlider.minValue = 0f;
                progressSlider.maxValue = totalWorkRequired;
                progressSlider.value = currentWorkDone;
            }
        }
        else
        {
            Debug.LogWarning("ไม่ได้ใส่ Progress Slider Prefab ใน GridPlacementManager!");
        }
    }

    private void UpdateUI()
    {
        if (progressSlider != null)
        {
            progressSlider.value = currentWorkDone;
        }
    }

    private void UpdateBounceAnimation()
    {
        float progressPercent = Mathf.Clamp01(currentWorkDone / totalWorkRequired);
        float currentAmplitude = Mathf.Lerp(minAmplitude, maxAmplitude, progressPercent);

        float bounce = Mathf.Sin(Time.time * ghostBounceFrequency) * currentAmplitude;
        transform.localScale = ghostBaseScale + new Vector3(bounce, bounce, bounce);
    }

    public void OnUnitInteract(UnitBase unit)
    {
        if (!activeUnits.Contains(unit))
        {
            activeUnits.Add(unit);
        }
    }

    public void OnUnitExit(UnitBase unit)
    {
        if (activeUnits.Contains(unit))
        {
            activeUnits.Remove(unit);
        }
    }

    private void FinishBuilding()
    {
        if (buildingData != null && buildingData.requiredResources != null)
        {
            foreach (ResourceCost cost in buildingData.requiredResources)
            {
                ResourceInventory.Instance.ConsumeResource(cost.item, cost.amount);
            }
        }

        foreach (UnitBase unitBase in new List<UnitBase>(activeUnits))
        {
            unitBase.ResetUnitState();
        }
        activeUnits.Clear();

        if (canvasInstance != null)
        {
            Destroy(canvasInstance);
        }

        if (buildingData != null && buildingData.Realprefab != null)
        {
            GameObject newBuilding = Instantiate(buildingData.Realprefab, transform.position, transform.rotation);
            MinimapMarker icon = newBuilding.GetComponent<MinimapMarker>();
            if (icon == null)
            {
                icon = newBuilding.AddComponent<MinimapMarker>();
                icon.teamType = TeamType.Building;
            }
        }
        Destroy(gameObject);
    }

    public Vector3 GetInteractionPoint()
    {
        return transform.position;
    }
}