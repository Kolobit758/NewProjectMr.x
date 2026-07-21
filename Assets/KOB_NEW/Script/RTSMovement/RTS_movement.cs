using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class RTS_movement : MonoBehaviour
{
    public static RTS_movement Instance;

    [Header("Camera Settings")]
    public Camera cam;
    public float panSpeed = 20f;

    [Header("Selection UI Settings")]
    [SerializeField] private float dragThreshold = 10f;
    public RectTransform selectionBoxUI; // ลาก UI Image (สี่เหลี่ยมโปร่งใส) มาใส่
    public Canvas canvas;
    private Vector2 startMousePos;
    private bool isDragging = false;

    [Header("Zoom Settings")]
    public float zoomSpeed = 10f;
    public float minZoom = 5f;
    public float maxZoom = 40f;

    [Header("Layers")]
    public LayerMask groundLayer;
    public LayerMask interactableLayer;

    // ลิสต์เก็บยูนิตทั้งหมดในฉาก
    public List<UnitBase> allUnits = new List<UnitBase>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        // ทำความสะอาดลิสต์ทุกเฟรม ป้องกันการอ้างอิงถึงยูนิตที่ถูกทำลายไปแล้ว
        if (allUnits != null)
        {
            allUnits.RemoveAll(u => u == null || u.gameObject == null);
        }

        HandleCameraPan();
        HandleSelection();
        HandleZoom();

        // คำสั่งคลิกขวา (สั่งการยูนิตผ่าน Job System)
        if (Input.GetMouseButtonDown(1))
        {
            ProcessRightClickCommands();
        }
    }

    // =========================================================================
    // COMMAND SYSTEM (จัดการคำสั่งคลิกขวาผ่าน Job System และ Manager)
    // =========================================================================
    void ProcessRightClickCommands()
    {
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            List<UnitBase> selectedUnits = allUnits.FindAll(u => u != null && u.isSelected);
            if (selectedUnits.Count == 0) return;

            ITaskable clickedTask = hit.collider.GetComponentInParent<ITaskable>();

            // 1. เคสคลิกโดนแปลงผัก (CropPlots) -> มอบหมายให้ FarmManager จัดการกระจายแปลง
            if (clickedTask is CropPlots)
            {
                FarmManager.Instance.AssignUnitsToFarm(selectedUnits);
                Debug.Log("🌱 [Command]: สั่งให้ยูนิตที่เลือกเข้าทำงานในแปลงฟาร์ม");
                return;
            }

            // 2. เคสคลิกโดนแหล่งทรัพยากร (GatheringBase เช่น ต้นไม้, หิน) -> สร้าง GatherJob
            if (clickedTask is GatheringBase gatheringNode)
            {
                Transform vault = FindNearestVault(gatheringNode.transform.position);
                
                // สร้าง GatherJob ใหม่ผ่าน JobManager
                GameObject jobObj = new GameObject($"GatherJob_{gatheringNode.name}");
                GatherJob gatherJob = jobObj.AddComponent<GatherJob>();
                gatherJob.Init(gatheringNode, vault);

                foreach (var unit in selectedUnits)
                {
                    if (unit != null)
                    {
                        JobManager.Instance.CancelAllJobsForUnit(unit);
                        gatherJob.AssignUnit(unit);
                    }
                }
                Debug.Log($"🌲 [Command]: สร้าง GatherJob สำหรับ {selectedUnits.Count} ยูนิต");
                return;
            }

            // 3. เคสคลิกโดนสิ่งก่อสร้างที่กำลังสร้าง (GhostBuilding)
            if (clickedTask is GhostBuilding ghost)
            {
                foreach (var unit in selectedUnits)
                {
                    if (unit != null)
                    {
                        JobManager.Instance.CancelAllJobsForUnit(unit);
                        unit.CommandMoveTo(ghost.GetInteractionPoint());
                        // สามารถขยายเพิ่ม BuildJob ได้ในอนาคตตรงนี้
                    }
                }
                return;
            }

            // 4. เคสคลิกพื้นดินธรรมดา -> สั่งเดิน (Move) และยกเลิก Job เดิมทั้งหมด
            if ((groundLayer.value & (1 << hit.collider.gameObject.layer)) != 0 || clickedTask == null)
            {
                foreach (var unit in selectedUnits)
                {
                    if (unit != null)
                    {
                        JobManager.Instance.CancelAllJobsForUnit(unit);
                        unit.CommandMoveTo(hit.point);
                    }
                }
                Debug.Log($"🚶 [Command]: สั่งย้ายตำแหน่งไปยัง {hit.point}");
            }
        }
    }

    // =========================================================================
    // CAMERA PAN & ZOOM
    // =========================================================================
    void HandleCameraPan()
    {
        if (Input.GetMouseButton(2)) // คลิกเมาส์กลางค้างไว้เพื่อเลื่อนมุมกล้อง
        {
            float x = -Input.GetAxis("Mouse X") * panSpeed * Time.deltaTime;
            float z = -Input.GetAxis("Mouse Y") * panSpeed * Time.deltaTime;
            transform.Translate(new Vector3(x, 0, z), Space.World);
        }
    }

    void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (scroll == 0) return;

        if (cam.orthographic)
        {
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * zoomSpeed, minZoom, maxZoom);
        }
        else
        {
            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView - scroll * zoomSpeed, 10f, 90f);
        }
    }

    // =========================================================================
    // UNIT SELECTION (คลิกเลือกยูนิต และลากกล่องเลือก Box Selection)
    // =========================================================================
    void HandleSelection()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            startMousePos = Input.mousePosition;
            isDragging = true;
            if (selectionBoxUI != null)
            {
                selectionBoxUI.gameObject.SetActive(true);
                selectionBoxUI.sizeDelta = Vector2.zero;
            }
        }

        if (isDragging)
        {
            UpdateSelectionBox(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;

            if (selectionBoxUI != null)
            {
                selectionBoxUI.gameObject.SetActive(false);
            }

            bool additive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            float distance = Vector2.Distance(startMousePos, Input.mousePosition);

            if (distance < dragThreshold)
                SelectSingle(additive);
            else
                SelectBox(additive);
        }
    }

    void SelectSingle(bool additive)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        UnitBase clickedUnit = null;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            clickedUnit = hit.collider.GetComponentInParent<UnitBase>();
        }

        if (!additive)
        {
            foreach (UnitBase unit in allUnits)
            {
                if (unit != null) unit.SetSelected(false);
            }
        }

        if (clickedUnit != null)
        {
            clickedUnit.SetSelected(true);
        }
    }

    void SelectBox(bool additive)
    {
        if (cam == null || canvas == null) return;

        Rect selectionRect = GetScreenRect(startMousePos, Input.mousePosition);

        if (!additive)
        {
            foreach (UnitBase unit in allUnits)
            {
                if (unit != null) unit.SetSelected(false);
            }
        }

        foreach (UnitBase unit in allUnits)
        {
            if (unit == null) continue;

            Vector3 screenPos = cam.WorldToScreenPoint(unit.transform.position);

            if (screenPos.z < 0) continue;

            if (selectionRect.Contains(screenPos))
            {
                unit.SetSelected(true);
            }
        }
    }

    void UpdateSelectionBox(Vector2 currentMousePos)
    {
        if (selectionBoxUI == null || canvas == null) return;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 startLocal;
        Vector2 currentLocal;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, startMousePos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out startLocal);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, currentMousePos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out currentLocal);

        Vector2 size = currentLocal - startLocal;

        selectionBoxUI.anchoredPosition = startLocal + size / 2f;
        selectionBoxUI.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
        selectionBoxUI.localScale = new Vector3(Mathf.Sign(size.x), Mathf.Sign(size.y), 1);
    }

    Rect GetScreenRect(Vector2 p1, Vector2 p2)
    {
        float x = Mathf.Min(p1.x, p2.x);
        float y = Mathf.Min(p1.y, p2.y);
        float w = Mathf.Abs(p1.x - p2.x);
        float h = Mathf.Abs(p1.y - p2.y);
        return new Rect(x, y, w, h);
    }

    private Transform FindNearestVault(Vector3 pos)
    {
        GameObject[] vaults = GameObject.FindGameObjectsWithTag("Vault");
        Transform nearest = null;
        float minDist = Mathf.Infinity;
        foreach (var v in vaults)
        {
            float dist = Vector3.Distance(pos, v.transform.position);
            if (dist < minDist) { minDist = dist; nearest = v.transform; }
        }
        return nearest;
    }
}