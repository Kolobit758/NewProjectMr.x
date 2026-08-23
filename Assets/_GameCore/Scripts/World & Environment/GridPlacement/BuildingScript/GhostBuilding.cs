using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class GhostBuilding : MonoBehaviour, ITaskable
{
    public SO_Building buildingData;
    public float totalWorkRequired = 100f;
    public float currentWorkDone = 0f;

    [Header("Detection Settings (ระบบเช็คระยะ)")]
    [Tooltip("ระยะรัศมีรอบสิ่งก่อสร้างที่ยูนิตต้องอยู่เพื่อช่วยสร้าง")]
    public float buildRadius = 3.5f;
    [Tooltip("ใส่ Layer ของยูนิต (เพื่อเพิ่มประสิทธิภาพในการสแกน)")]
    public LayerMask unitLayer = ~0; 

    [Header("Ghost Idle Bounce (ตอนกำลังสร้าง)")]
    public float ghostBounceFrequency;
    public float minAmplitude;
    public float maxAmplitude;
    private Vector3 ghostBaseScale = Vector3.one;

    [Header("UI References")]
    private GameObject sliderPrefab;
    private float uiOffsetY = 1f;
    private Slider progressSlider;
    private GameObject canvasInstance;

    public void InitializeGhost(SO_Building data, GameObject uiPrefab, float customOffsetY, float ghostBounceFrequency, float minAmplitude, float maxAmplitude)
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
        // 🟢 คำนวณความเร็วการสร้างจากยูนิตที่อยู่ในรัศมีแบบเรียลไทม์
        float speedPerSecond = CalculateBuildSpeedFromNearbyUnits();

        if (speedPerSecond > 0)
        {
            currentWorkDone += speedPerSecond * Time.deltaTime;
            UpdateUI();

            if (currentWorkDone >= totalWorkRequired)
            {
                FinishBuilding();
                return;
            }
        }

        UpdateBounceAnimation();
    }

    // 🟢 สแกนหายูนิตในรัศมีรอบๆ Ghost Building
    private float CalculateBuildSpeedFromNearbyUnits()
    {
        float totalSpeed = 0f;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, buildRadius, unitLayer);

        foreach (var hitCollider in hitColliders)
        {
            UnitBase unit = hitCollider.GetComponent<UnitBase>();
            if (unit != null)
            {
                totalSpeed += unit.buildSpeed;
            }
        }

        return totalSpeed;
    }

    private void CreateWorldSpaceUI()
    {
        if (sliderPrefab != null)
        {
            canvasInstance = Instantiate(sliderPrefab, transform);
            canvasInstance.transform.localPosition = new Vector3(0f, uiOffsetY, 0f);

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

    // 🟢 คงฟังก์ชัน Interface ไว้เพื่อไม่ให้สคริปต์อื่นที่เรียกใช้เกิด Error
    public void OnUnitInteract(UnitBase unit) { }
    public void OnUnitExit(UnitBase unit) { }

    private void FinishBuilding()
    {
        if (buildingData != null && buildingData.requiredResources != null)
        {
            foreach (ResourceCost cost in buildingData.requiredResources)
            {
                ResourceInventory.Instance.ConsumeResource(cost.item, cost.amount);
            }
        }

        // 🟢 รีเซ็ตยูนิตทั้งหมดที่อยู่ในระยะก่อสร้างเมื่อสร้างเสร็จ
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, buildRadius, unitLayer);
        foreach (var hitCollider in hitColliders)
        {
            UnitBase unit = hitCollider.GetComponent<UnitBase>();
            if (unit != null)
            {
                unit.ResetUnitState();
            }
        }

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

        if (BuildingUnlockManagers.Instance != null && buildingData != null)
        {
            BuildingUnlockManagers.Instance.NotifyBuildingCompleted(buildingData);
        }

        Destroy(gameObject);
    }

    public Vector3 GetInteractionPoint()
    {
        return transform.position;
    }

    // 🟢 วาดวงกลมรัศมีในหน้า Scene ช่วยให้มองเห็นระยะสแกนได้ง่ายใน Unity Editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, buildRadius);
    }
}