using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class RTS_movement : MonoBehaviour
{
    public static RTS_movement instance;
    [Header("Camera Settings")]
    public Camera cam;
    public float panSpeed = 20f;
    [Header("Selection")]
    [SerializeField] private float dragThreshold = 10f;

    [Header("Selection Settings")]
    public RectTransform selectionBoxUI; // ลาก UI Image (สี่เหลี่ยมโปร่งใส) มาใส่ตรงนี้
    private Vector2 startMousePos;
    private bool isDragging = false;
    [Header("Zoom Settings")]
    public float zoomSpeed = 10f;
    public float minZoom = 5f;
    public float maxZoom = 40f;
    [Tooltip("อัตราส่วนที่กล้องถอยตาม Z เทียบกับความสูง Y ที่เปลี่ยน (0 = ไม่ถอยเลย)")]
    public float zoomZRatio = 0.6f;
    public Canvas canvas;

    // ลิสต์เก็บยูนิตทั้งหมด
    public List<UnitBase> allUnits = new List<UnitBase>();
    [Header("Edge Scroll Settings")]
    public bool enableEdgeScroll = true;
    public float edgeScrollSpeed = 20f;
    [Tooltip("ระยะห่างจากขอบจอ (พิกเซล) ที่จะเริ่มเลื่อนกล้อง")]
    public float edgeSize = 20f;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Update()
    {
        if (allUnits != null)
        {
            allUnits.RemoveAll(u => u == null || u.gameObject == null);
        }

        HandleCameraPan();
        HandleEdgeScroll();   // 🟢 เพิ่มบรรทัดนี้
        HandleSelection();
        HandleZoom();

        if (Input.GetMouseButtonDown(1))
        {
            bool isPlacing = GridPlacementManager.Instance != null && GridPlacementManager.Instance.IsPlacementModeActive;
            if (!isPlacing)
                MoveSelectedUnits();
        }
    }

    // แทนที่ฟังก์ชัน MoveSelectedUnits เดิมใน RTS_movement.cs
    void MoveSelectedUnits()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            ITaskable clickedTask = hit.collider.GetComponentInParent<ITaskable>();
            List<UnitBase> selectedUnits = allUnits.FindAll(u => u != null && u.isSelected);

            foreach (UnitBase unit in selectedUnits)
            {
                if (unit != null)
                {
                    if (clickedTask is CropPlots)
                    {
                        // สั่งโหมดฟาร์ม (ระบบจะจัดสรรแปลงให้เองไม่ซ้ำตัว)
                        unit.CommandFarmingPatrol();
                    }
                    else
                    {
                        // ส่งเป้าหมายพิกัดหรือ Task ปกติ (เช่น GatheringBase จะถูกส่งผ่าน MoveTo)
                        unit.MoveTo(hit.point, clickedTask);
                        if (clickedTask is GhostBuilding ghost)
                        {
                            unit.SetOrder(ghost);
                        }
                    }
                }
            }
        }
    }

    void HandleCameraPan()
    {
        if (Input.GetMouseButton(2)) // เมาส์กลาง
        {
            float x = -Input.GetAxis("Mouse X") * panSpeed * Time.deltaTime;
            float z = -Input.GetAxis("Mouse Y") * panSpeed * Time.deltaTime;
            transform.Translate(new Vector3(x, 0, z), Space.World);
        }
    }

    // 🔒 ระบบอื่น (เช่น Shop, SeedPlanting) เรียก BlockSelectionThisFrame() เพื่อป้องกัน drag-box
    private bool _externalBlockRequested = false;
    public void BlockSelectionThisFrame() => _externalBlockRequested = true;

    // 🔒 Flag: ถ้า MouseDown ตกบน UI ให้ block การ drag ตลอด gesture นี้ (จนกว่าจะปล่อยปุ่ม)
    private bool _selectionBlockedThisGesture = false;

    void HandleSelection()
    {
        // 1. โหมดวางตึก — ยกเลิก drag ทันที
        if (GridPlacementManager.Instance != null && GridPlacementManager.Instance.IsPlacementModeActive)
        {
            CancelDrag();
            _externalBlockRequested = false;
            return;
        }

        bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        // 2. จุด MouseDown — ตัดสินใจว่า gesture นี้จะ block หรือเปล่า
        if (Input.GetMouseButtonDown(0))
        {
            // ถ้า MouseDown ตกบน UI หรือมี external block → block gesture นี้ทั้งหมด
            if (isOverUI || _externalBlockRequested)
            {
                _selectionBlockedThisGesture = true;
            }
            else
            {
                _selectionBlockedThisGesture = false;
                startMousePos = Input.mousePosition;
                isDragging = true;
                if (selectionBoxUI != null)
                {
                    selectionBoxUI.gameObject.SetActive(true);
                    selectionBoxUI.sizeDelta = Vector2.zero;
                }
            }
        }

        // 3. ถ้า gesture ถูก block ให้ข้ามทุกอย่าง (รอจน MouseUp)
        if (_selectionBlockedThisGesture)
        {
            if (Input.GetMouseButtonUp(0))
            {
                _selectionBlockedThisGesture = false;
            }
            _externalBlockRequested = false;
            return;
        }

        // 4. ลาก selection box
        if (isDragging)
        {
            // ถ้าลากออกนอก UI แล้ว ยังโอเคอยู่ (gesture เริ่มบนพื้นที่โล่ง)
            UpdateSelectionBox(Input.mousePosition);
        }

        // 5. ปล่อยปุ่ม — ทำการ select
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;

            if (selectionBoxUI != null)
                selectionBoxUI.gameObject.SetActive(false);

            bool additive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            float distance = Vector2.Distance(startMousePos, Input.mousePosition);

            if (distance < dragThreshold)
                SelectSingle(additive);
            else
                SelectBox(additive);
        }

        _externalBlockRequested = false;
    }

    private void CancelDrag()
    {
        if (isDragging)
        {
            isDragging = false;
            if (selectionBoxUI != null)
                selectionBoxUI.gameObject.SetActive(false);
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

            if (screenPos.z < 0)
                continue;

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

    void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (scroll == 0) return;

        Vector3 pos = transform.position;
        float oldY = pos.y;
        float newY = Mathf.Clamp(oldY - scroll * zoomSpeed, minZoom, maxZoom);
        float deltaY = newY - oldY;

        pos.y = newY;
        pos.z -= deltaY * zoomZRatio; // ถอยกล้องตาม Z ไปพร้อมกับ Y เพื่อความรู้สึกเป็นธรรมชาติ
        transform.position = pos;
    }

    Rect GetScreenRect(Vector2 p1, Vector2 p2)
    {
        float x = Mathf.Min(p1.x, p2.x);
        float y = Mathf.Min(p1.y, p2.y);
        float w = Mathf.Abs(p1.x - p2.x);
        float h = Mathf.Abs(p1.y - p2.y);
        return new Rect(x, y, w, h);
    }

    void HandleEdgeScroll()
    {
        if (!enableEdgeScroll) return;
        if (GridPlacementManager.Instance != null && GridPlacementManager.Instance.IsPlacementModeActive) return;

        // 🟢 ถ้าเมาส์กดลากเลือกยูนิตอยู่ หรืออยู่เหนือ UI ไม่ต้องเลื่อนกล้อง (กันชนกับ selection box)
        if (isDragging) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Vector3 mousePos = Input.mousePosition;
        Vector3 moveDir = Vector3.zero;

        // เช็คว่าเมาส์อยู่นอกจอหรือเปล่า (เผื่อเคสสลับหน้าต่าง/มัลติมอนิเตอร์) ป้องกันเลื่อนเพี้ยน
        bool mouseInsideWindow = mousePos.x >= 0 && mousePos.x <= Screen.width &&
                                 mousePos.y >= 0 && mousePos.y <= Screen.height;
        if (!mouseInsideWindow) return;

        if (mousePos.x <= edgeSize)
            moveDir.x = -1f;
        else if (mousePos.x >= Screen.width - edgeSize)
            moveDir.x = 1f;

        if (mousePos.y <= edgeSize)
            moveDir.z = -1f;
        else if (mousePos.y >= Screen.height - edgeSize)
            moveDir.z = 1f;

        if (moveDir != Vector3.zero)
        {
            transform.Translate(moveDir.normalized * edgeScrollSpeed * Time.deltaTime, Space.World);
        }
    }
}