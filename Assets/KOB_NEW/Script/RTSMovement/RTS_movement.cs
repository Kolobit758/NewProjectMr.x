using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class RTS_movement : MonoBehaviour
{
    public static RTS_movement instance;

    // 🟢 [ใหม่] Event นี้จะยิงทุกครั้งที่ "ผลลัพธ์การเลือก" เปลี่ยนไป
    // ไม่ว่าจะคลิกเดี่ยว, ลากคลุมกล่อง, หรือคลิกพื้นที่ว่างเพื่อยกเลิกทั้งหมด
    // UI แผงสกิลจะ subscribe ตัวนี้เพื่อโชว์/ซ่อนตัวเอง และรู้ว่าต้องตั้งค่ากับ unit ไหนบ้าง
    public static event System.Action<List<UnitBase>> OnSelectionChanged;

    // 🟢 [ใหม่] "โหมดชี้เป้า" สำหรับสกิลที่ต้องคลิกจุดในโลกก่อน (เช่น Set Auto Care ที่ต้องคลิกแปลง)
    // ตอนอยู่ในโหมดนี้ คลิกขวาจะไม่สั่ง MoveSelectedUnits ตามปกติ แต่จะยิง callback กลับไปให้ UI แทน
    private bool _isInTargetMode = false;
    private System.Action<RaycastHit> _pendingTargetCallback;

    /// <summary>เริ่มโหมดรอคลิกเป้าหมายในโลก (เรียกจาก UI ตอนกดปุ่มสกิลที่ต้องระบุจุด)</summary>
    public void BeginTargetMode(System.Action<RaycastHit> onTargetPicked)
    {
        _isInTargetMode = true;
        _pendingTargetCallback = onTargetPicked;
    }

    /// <summary>ยกเลิกโหมดชี้เป้า (เรียกเองได้ เช่น ตอนกด Esc หรือกดปุ่มสกิลซ้ำเพื่อยกเลิก)</summary>
    public void CancelTargetMode()
    {
        _isInTargetMode = false;
        _pendingTargetCallback = null;
    }

    public bool IsInTargetMode => _isInTargetMode;

    // 🟢 [ใหม่] ยิงตอนคลิกซ้าย "เดี่ยว" โดนแปลงเกษตร (ไม่ใช่ unit) — ให้ FarmPlotPanelUI ไปโชว์แผงตั้งค่า
    public static event System.Action<CropPlotsGroup> OnFarmPlotClicked;

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

        // 🟢 กด Esc เพื่อยกเลิกโหมดชี้เป้าได้ตลอดเวลา
        if (_isInTargetMode && Input.GetKeyDown(KeyCode.Escape))
        {
            CancelTargetMode();
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (_isInTargetMode)
            {
                HandleAbilityTargetClick();
            }
            else
            {
                bool isPlacing = GridPlacementManager.Instance != null && GridPlacementManager.Instance.IsPlacementModeActive;
                if (!isPlacing)
                    MoveSelectedUnits();
            }
        }
    }

    /// <summary>
    /// 🟢 ตอนอยู่ในโหมดชี้เป้า คลิกขวาครั้งถัดไปในโลกจะถูกส่งกลับไปให้ผู้ที่เรียก BeginTargetMode
    /// (เช่น UnitAbilityPanelUI ตอนกด "Set Auto Care" แล้วรอผู้เล่นคลิกแปลง)
    /// </summary>
    private void HandleAbilityTargetClick()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            var callback = _pendingTargetCallback;
            CancelTargetMode(); // เคลียร์โหมดก่อนเรียก callback กันเผื่อ callback สั่งเริ่มโหมดใหม่ซ้อน
            callback?.Invoke(hit);
        }
        else
        {
            CancelTargetMode();
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
        CropPlotsGroup clickedFarmGroup = null;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            clickedUnit = hit.collider.GetComponentInParent<UnitBase>();

            // 🟢 ไม่โดน unit ให้ลองเช็คว่าโดนแปลงเกษตร (เดี่ยว หรือ กลุ่ม) หรือเปล่า
            if (clickedUnit == null)
            {
                clickedFarmGroup = ResolveFarmGroupFromHit(hit);
            }
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

        // 🟢 แจ้งทุกคนที่ subscribe (เช่น แผง UI สกิล) ว่าผลการเลือกล่าสุดคือใครบ้าง
        BroadcastSelectionChanged();

        if (clickedFarmGroup != null)
        {
            OnFarmPlotClicked?.Invoke(clickedFarmGroup);
        }
    }

    /// <summary>
    /// 🟢 หา CropPlotsGroup จากจุดที่คลิก — รองรับทั้งกรณีคลิกโดนกลุ่มที่ตั้งค่าไว้แล้ว
    /// และกรณีคลิกโดนแปลงเดี่ยวๆ ที่ยังไม่มีกลุ่ม (จะสร้างกลุ่มขนาด 1 แปลงให้อัตโนมัติ)
    /// </summary>
    private CropPlotsGroup ResolveFarmGroupFromHit(RaycastHit hit)
    {
        CropPlotsGroup group = hit.collider.GetComponentInParent<CropPlotsGroup>();
        if (group != null) return group;

        CropPlots singlePlot = hit.collider.GetComponentInParent<CropPlots>();
        if (singlePlot == null) return null;

        group = singlePlot.GetComponent<CropPlotsGroup>();
        if (group == null)
        {
            group = singlePlot.gameObject.AddComponent<CropPlotsGroup>();
            group.plots = new List<CropPlots> { singlePlot };
        }
        return group;
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

        // 🟢 แจ้งทุกคนที่ subscribe ว่าผลการลากคลุมล่าสุดคือใครบ้าง
        BroadcastSelectionChanged();
    }

    /// <summary>
    /// 🟢 รวบรวม unit ที่ isSelected == true ทั้งหมดตอนนี้ แล้วยิง event ออกไป
    /// เรียกทุกครั้งหลังจบการเลือก (คลิกเดี่ยว / ลากกล่อง) เพื่อให้ UI sync ตามจริงเสมอ
    /// </summary>
    private void BroadcastSelectionChanged()
    {
        List<UnitBase> selected = allUnits.FindAll(u => u != null && u.isSelected);
        OnSelectionChanged?.Invoke(selected);
    }

    /// <summary>
    /// 🟢 เผื่อกรณีอื่นอยากจะดึง "รายชื่อ unit ที่ถูกเลือกอยู่ตอนนี้" แบบ on-demand
    /// (เช่น ปุ่มใน UI ที่ไม่ได้ subscribe event แต่กดแล้วอยากรู้ทันที)
    /// </summary>
    public List<UnitBase> GetCurrentlySelectedUnits()
    {
        return allUnits.FindAll(u => u != null && u.isSelected);
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