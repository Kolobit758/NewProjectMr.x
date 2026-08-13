using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GridPlacementManager : MonoBehaviour
{
    public static GridPlacementManager Instance { get; private set; }
    public Camera mainCam;

    [Header("Grid Settings")]
    public float cellSize = 1f;
    public LayerMask placementLayer;

    [Header("Collision & Overlap Check")]
    [Tooltip("Layer ของสิ่งปลูกสร้างอื่นๆ ที่ห้ามวางทับ")]
    public LayerMask obstacleLayer; 
    [Tooltip("ขนาดของ Box ที่ใช้เช็คการชนตอนวางตึก (ควรปรับให้เท่ากับขนาดฐานตึก)")]
    public Vector3 checkCubeSize = new Vector3(0.9f, 1f, 0.9f);

    [Header("Height Adjustment")]
    public float manualYOffset = 0f;             
    public float fineTuneStep = 0.05f;         
    private float currentFineTuneOffset = 0f;  

    private GameObject currentGhost;
    private SO_Building currentBuildingData;
    public Material ghostMaterial;
    private bool isPlacementMode = false;
    public bool IsPlacementModeActive => isPlacementMode;
    private float ghostPivotOffsetY = 0f;
    public static event System.Action<SO_Building> OnBuildingPlaced; 

    [Header("UI Settings")]
    public GameObject progressSliderPrefab; 
    public float ghostBounceFrequency = 5f;
    public float minAmplitude = 1f;
    public float maxAmplitude = 5f;
    public float uiYoffset = 0.5f;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Update()
    {
        if (!isPlacementMode) return;

        HandleFineTuneInput(); 
        UpdateGhostPosition();

        if (Input.GetMouseButtonDown(0))
        {
            if (currentGhost != null)
            {
                // 🟢 เช็คก่อนว่าตำแหน่งนี้วางได้ไหม (ไม่ชนกับตึกอื่น) ค่อยสร้าง
                if (IsValidPlacement())
                {
                    PlaceStructure();
                }
                else
                {
                    Debug.Log("⚠️ [Grid Placement]: ตรงนี้มีสิ่งปลูกสร้างอื่นอยู่แล้ว วางทับไม่ได้!");
                }
            }
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            EndPlacementMode();
        }
    }

    private void HandleFineTuneInput()
    {
        if (Input.GetKeyDown(KeyCode.PageUp))
        {
            currentFineTuneOffset += fineTuneStep;
        }
        else if (Input.GetKeyDown(KeyCode.PageDown))
        {
            currentFineTuneOffset -= fineTuneStep;
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentFineTuneOffset += scroll * fineTuneStep;
        }
    }

    public void StartPlacementMode(GameObject ghostPrefab, SO_Building buildingData)
    {
        if (currentGhost != null) Destroy(currentGhost);

        currentBuildingData = buildingData;
        currentFineTuneOffset = 0f; 
        currentGhost = InstantiatingGhost(ghostPrefab);
        isPlacementMode = true;
    }

    private GameObject InstantiatingGhost(GameObject prefab)
    {
        GameObject ghost = Instantiate(prefab);

        GhostBuilding gb = ghost.GetComponent<GhostBuilding>();
        if (gb == null) gb = ghost.AddComponent<GhostBuilding>();

        gb.InitializeGhost(currentBuildingData, progressSliderPrefab, uiYoffset, ghostBounceFrequency, minAmplitude, maxAmplitude);

        Collider[] ghostColliders = ghost.GetComponentsInChildren<Collider>();
        foreach (Collider c in ghostColliders) c.enabled = false;

        ghost.layer = LayerMask.NameToLayer("Ignore Raycast");

        ghostPivotOffsetY = CalculatePivotToBottomOffset(ghost);

        return ghost;
    }

    private float CalculatePivotToBottomOffset(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return 0f;

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }

        float bottomY = bounds.min.y;
        float pivotY = obj.transform.position.y;

        return pivotY - bottomY;
    }

    private void UpdateGhostPosition()
    {
        if (currentGhost == null) return;

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 500f, placementLayer))
        {
            // 🟢 ล็อกตำแหน่ง X และ Z ให้ลงกริดเป๊ะๆ เสมอกัน
            Vector3 gridPos = SnapToGrid(hit.point);

            // 🟢 ล็อกความสูง Y ของพื้นผิวให้เรียบเสมอกันตามกริด ไม่กระยุกกระยักตามความชันพื้น
            gridPos.y = hit.point.y + ghostPivotOffsetY + manualYOffset + currentFineTuneOffset;

            currentGhost.transform.position = gridPos;
            currentGhost.SetActive(true);
        }
        else
        {
            currentGhost.SetActive(false);
        }
    }

    // 🟢 ฟังก์ชันเช็คการชน: ป้องกันการวางซ้อนทับตึกอื่น
    private bool IsValidPlacement()
    {
        if (currentGhost == null) return false;

        // เช็คพื้นที่รอบๆ ตัวโกสต์ว่ามี Collider ตัวอื่นใน obstacleLayer ขวางอยู่ไหม
        Vector3 center = currentGhost.transform.position + Vector3.up * (checkCubeSize.y / 2f);
        Collider[] hits = Physics.OverlapBox(center, checkCubeSize / 2f, Quaternion.identity, obstacleLayer);

        // ถ้าเจอ Collider ขวางอยู่ แปลว่าวางไม่ได้ (คืนค่า false)
        return hits.Length == 0;
    }

    private void PlaceStructure()
    {
        if (currentGhost == null || !currentGhost.activeSelf) return;

        GameObject placedGhost = currentGhost;
        SO_Building placedBuildingData = currentBuildingData; 

        GhostBuilding gb = placedGhost.GetComponent<GhostBuilding>();
        if (gb != null) gb.enabled = true;

        if (ResourceInventory.Instance != null)
        {
            ResourceInventory.Instance.ConsumeResource(currentBuildingData, 1);
        }

        Collider[] colliders = placedGhost.GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders) c.enabled = true;

        currentGhost = null;
        EndPlacementMode();

        OnBuildingPlaced?.Invoke(placedBuildingData); 
    }

    // 🟢 ล็อกพิกัด X และ Z ให้เข้าช่องตาราง Grid แบบล็อกเป็นสเต็ปชัดเจน
    private Vector3 SnapToGrid(Vector3 position)
    {
        float x = Mathf.Floor(position.x / cellSize) * cellSize + (cellSize / 2f);
        float z = Mathf.Floor(position.z / cellSize) * cellSize + (cellSize / 2f);
        return new Vector3(x, position.y, z);
    }

    public void EndPlacementMode()
    {
        if (currentGhost != null) Destroy(currentGhost);
        currentBuildingData = null;
        isPlacementMode = false;

        if (ResourceInventory.Instance != null)
        {
            ResourceInventory.Instance.NotifyChanged();
        }
    }

    // 🟢 วาดเส้นขอบเขตเช็คการชน (Gizmos) ให้เห็นในหน้า Scene View ของ Unity
    void OnDrawGizmos()
    {
        if (currentGhost != null && isPlacementMode)
        {
            Gizmos.color = IsValidPlacement() ? Color.green : Color.red;
            Vector3 center = currentGhost.transform.position + Vector3.up * (checkCubeSize.y / 2f);
            Gizmos.DrawWireCube(center, checkCubeSize);
        }
    }
}