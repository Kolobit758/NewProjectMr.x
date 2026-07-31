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

    public void StartPlacementManager(GameObject ghostPrefab, SO_Building buildingData)
    {
        // แก้ชื่อเมธอดให้ตรงกันถ้ามีการเรียกใช้จากข้างนอก หรือคงชื่อ StartPlacementMode ไว้ตามเดิม
    }

    public void StartPlacementMode(GameObject ghostPrefab, SO_Building buildingData)
    {
        if (currentGhost != null) Destroy(currentGhost);

        currentBuildingData = buildingData;
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

        // 🟢 แก้ไขจุดที่ 1: คำนวณ Offset จาก Mesh/Renderer โดยอ้างอิงจาก Pivot ของ Object โดยตรง
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

        // แปลงค่า World Bounds min.y ให้เทียบกับ Position ของ Object ในตอนนั้น
        // เพื่อให้รู้ว่าฐานล่างสุดอยู่ต่ำกว่า Pivot เท่าไหร่ในหน่วย World Space
        float bottomY = bounds.min.y;
        float pivotY = obj.transform.position.y;

        // ถ้า pivot อยู่ตรงกลางหรือด้านบน ฐานล่างสุดจะติดลบเมื่อเทียบกับ pivot เราจึงดึงระยะห่างออกมา
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

            // 🟢 แก้ไขจุดนี้: เอาความสูงพื้นผิวบวกกับ Offset ของ Pivot ที่คำนวณไว้
            // มันจะช่วยดันตัวตึกขึ้นมาให้อยู่บนพื้นพอดี ไม่จมลงไปครับ
            gridPos.y = hit.point.y + ghostPivotOffsetY;

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