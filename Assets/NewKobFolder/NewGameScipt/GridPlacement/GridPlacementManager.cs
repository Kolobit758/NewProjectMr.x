using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GridPlacementManager : MonoBehaviour
{
    public static GridPlacementManager Instance { get; private set; }

    [Header("Grid Settings")]
    public float cellSize = 1f;
    public LayerMask placementLayer;

    private GameObject currentGhost;
    private SO_Building currentBuildingData;
    public Material ghostMaterial;
    private bool isPlacementMode = false;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Update()
    {
        if (!isPlacementMode) return;

        UpdateGhostPosition();

        // 🟢 แก้ไขจุดที่ 1: เอา canPlaceThisFrame ที่เอ๋อค้างออกไปเลยมึง จิ้มเมื่อไหร่วางเมื่อนั้น ดักด้วยความชัวร์ของตัววัตถุแทน!
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

    public void StartPlacementMode(GameObject ghostPrefab, SO_Building buildingData)
    {
        if (currentGhost != null) Destroy(currentGhost);

        currentBuildingData = buildingData;
        currentGhost = InstantiatingGhost(ghostPrefab);
        isPlacementMode = true; // 🟢 เปิดโหมดตรงๆ ไม่ง้อ Coroutine หน่วงเวลาให้ค้างมึง
    }

    private GameObject InstantiatingGhost(GameObject prefab)
    {
        GameObject ghost = Instantiate(prefab);

        Collider[] ghostColliders = ghost.GetComponentsInChildren<Collider>();
        foreach (Collider c in ghostColliders)
        {
            c.enabled = false;
        }

        // บังคับเปลี่ยน Layer ไป Ignore Raycast ป้องกันเลเซอร์ชนตัวเอง
        ghost.layer = LayerMask.NameToLayer("Ignore Raycast");
        foreach (Transform child in ghost.transform)
        {
            child.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }

        MeshRenderer[] renderers = ghost.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer render in renderers)
        {
            if (ghostMaterial != null)
            {
                render.material = ghostMaterial;
            }
        }
        return ghost;
    }

    private void UpdateGhostPosition()
    {
        if (currentGhost == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // ยิงเลเซอร์ตรวจจับหน้าผิวเกาะ
        if (Physics.Raycast(ray, out hit, 500f, placementLayer))
        {
            Vector3 gridPos = SnapToGrid(hit.point);

            // 🟢 [แก้ไขจุดตาย]: ล็อกความสูงจากจุดที่เลเซอร์เมาส์ชนผิวบนสุดจริง ๆ ไม่เด้งลงไปกึ่งกลางโมเดลแล้วมึง!
            gridPos.y = hit.point.y;

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

        Vector3 spawnPos = currentGhost.transform.position;

        if (currentBuildingData != null && currentBuildingData.Realprefab != null)
        {
            // สร้างสิ่งปลูกสร้างจริงตรงพิกัดที่ Ghost ล็อกผิวไว้เรียบร้อย
            GameObject realBuilding = Instantiate(currentBuildingData.Realprefab, spawnPos, Quaternion.identity);

            // ยิงเลเซอร์ดักลงทะเบียนเข้าเกาะ
            Ray castDown = new Ray(spawnPos + Vector3.up * 5f, Vector3.down);
            RaycastHit hit;

            if (Physics.Raycast(castDown, out hit, 15f, placementLayer))
            {
                IslandController island = hit.collider.GetComponent<IslandController>();
                if (island == null) island = hit.collider.GetComponentInParent<IslandController>();

                if (island != null)
                {
                    if (island.placedStructures == null)
                    {
                        island.placedStructures = new List<GameObject>();
                    }

                    island.placedStructures.Add(realBuilding);

                    if (realBuilding.TryGetComponent<MachineStructure>(out MachineStructure machine))
                    {
                        machine.myIsland = island;
                    }
                }
            }

            if (ResourceInventory.Instance != null)
            {
                ResourceInventory.Instance.ConsumeResource(currentBuildingData, 1);
            }
        }

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