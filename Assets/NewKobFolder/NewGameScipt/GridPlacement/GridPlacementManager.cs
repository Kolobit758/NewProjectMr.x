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

    [Header("Height Adjustment")]
    public float manualYOffset = 0f;           // 🟢 ปรับ global ใน Inspector ได้ (ปรับตายตัวถ้าตึกส่วนใหญ่ลอย/จมเท่ากัน)
    public float fineTuneStep = 0.05f;         // 🟢 ระยะขยับต่อการกดปุ่ม/scroll 1 ครั้ง
    private float currentFineTuneOffset = 0f;  // 🟢 offset ที่ปรับสดตอน placement (reset ทุกครั้งที่เริ่มวางใหม่)

    private GameObject currentGhost;
    private SO_Building currentBuildingData;
    public Material ghostMaterial;
    private bool isPlacementMode = false;
    public bool IsPlacementModeActive => isPlacementMode;
    private float ghostPivotOffsetY = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Update()
    {
        if (!isPlacementMode) return;

        HandleFineTuneInput(); // 🟢 เช็ค input ปรับความสูงก่อน แล้วค่อย update ตำแหน่ง
        UpdateGhostPosition();

        if (Input.GetMouseButtonDown(0))
        {
            if (currentGhost != null)
            {
                PlaceStructure();
            }
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            EndPlacementMode();
        }
    }

    // 🟢 ปรับความสูงสดๆ ระหว่าง placement ด้วย PageUp/PageDown หรือ scroll wheel
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

    public void StartPlacementManager(GameObject ghostPrefab, SO_Building buildingData)
    {
        // แก้ชื่อเมธอดให้ตรงกันถ้ามีการเรียกใช้จากข้างนอก หรือคงชื่อ StartPlacementMode ไว้ตามเดิม
    }

    public void StartPlacementMode(GameObject ghostPrefab, SO_Building buildingData)
    {
        if (currentGhost != null) Destroy(currentGhost);

        currentBuildingData = buildingData;
        currentFineTuneOffset = 0f; // 🟢 reset ทุกครั้งที่เริ่มวางตึกใหม่
        currentGhost = InstantiatingGhost(ghostPrefab);
        isPlacementMode = true;
    }

    private GameObject InstantiatingGhost(GameObject prefab)
    {
        GameObject ghost = Instantiate(prefab);

        GhostBuilding gb = ghost.GetComponent<GhostBuilding>();
        if (gb == null) gb = ghost.AddComponent<GhostBuilding>();
        gb.buildingData = currentBuildingData;

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
            Vector3 gridPos = SnapToGrid(hit.point);

            // 🟢 รวม offset ทั้งหมด: พื้นผิว + offset ที่คำนวณจาก mesh + manual global + fine-tune สด
            gridPos.y = hit.point.y + ghostPivotOffsetY + manualYOffset + currentFineTuneOffset;

            currentGhost.transform.position = gridPos;
            currentGhost.SetActive(true);
        }
        else
        {
            currentGhost.SetActive(false);
        }
    }

    private void PlaceStructure()
    {
        if (currentGhost == null || !currentGhost.activeSelf) return;

        GameObject placedGhost = currentGhost;

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
    }

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
}