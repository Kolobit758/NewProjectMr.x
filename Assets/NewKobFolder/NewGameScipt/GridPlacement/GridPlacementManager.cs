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

        // 🟢 แปะสคริปต์ GhostBuilding ลงไปที่ตัวมันเลย
        GhostBuilding gb = ghost.AddComponent<GhostBuilding>();
        gb.buildingData = currentBuildingData;

        // ล้าง Collider ออกเพื่อให้เดินชนได้ (ผ่าน ITaskable)
        Collider[] ghostColliders = ghost.GetComponentsInChildren<Collider>();
        foreach (Collider c in ghostColliders) c.enabled = false;

        ghost.layer = LayerMask.NameToLayer("Ignore Raycast");
        return ghost;
    }

    private void UpdateGhostPosition()
    {
        if (currentGhost == null) return;

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
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

        // 1. เก็บ Ghost ตัวปัจจุบันไว้
        GameObject placedGhost = currentGhost;

        // 2. ปิด Material สีฟ้า/ปรับ Material ให้เป็นตึกจริง (ถ้ามี)
        // หรือถ้ามึงมีโมเดลตึกจริงที่ซ่อนอยู่ ให้เปิดมันขึ้นมาแทนที่ Ghost ตัวนี้
        placedGhost.GetComponent<GhostBuilding>().enabled = true; // มั่นใจว่า Script นี้ทำงาน

        // 3. ห้ามทำลาย Ghost (ห้ามเรียก EndPlacementMode แบบปกติ) 
        // แต่ให้เรา "วาง" มันไว้แล้วสร้าง Ghost ตัวใหม่ขึ้นมาให้ผู้เล่นวางต่อ (ถ้าต้องการ)
        // หรือถ้ามึงอยากให้วางแล้วจบ ก็แค่ตัดการ Destroy(currentGhost) ออก



        // 4. หักของใน Inventory
        if (ResourceInventory.Instance != null)
        {
            ResourceInventory.Instance.ConsumeResource(currentBuildingData, 1);
        }
        // 🟢 เพิ่มตรงนี้: หลังจากวาง Ghost ทิ้งไว้บนฉากแล้ว ให้เปิด Collider มันซะ!
        Collider[] colliders = currentGhost.GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders) c.enabled = true;

        // แล้วค่อยเคลียร์ currentGhost เพื่อสร้างตัวใหม่
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