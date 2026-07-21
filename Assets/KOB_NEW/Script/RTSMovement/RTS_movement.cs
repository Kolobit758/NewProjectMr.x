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
    public Canvas canvas;

    // ลิสต์เก็บยูนิตทั้งหมด
    public List<UnitBase> allUnits = new List<UnitBase>();

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
        // 🟢 ทำความสะอาดลิสต์ทุกเฟรม ป้องกันการอ้างอิงถึงยูนิตหรือสัตว์ป่าที่ถูกทำลายไปแล้ว
        if (allUnits != null)
        {
            allUnits.RemoveAll(u => u == null || u.gameObject == null);
        }

        HandleCameraPan();
        HandleSelection();
        HandleZoom();

        // 🟢 เพิ่มคำสั่งเดิน (คลิกขวา)
        if (Input.GetMouseButtonDown(1))
        {
            MoveSelectedUnits();
        }
    }

    void MoveSelectedUnits()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            ITaskable clickedTask = hit.collider.GetComponentInParent<ITaskable>();

            foreach (UnitBase unit in allUnits)
            {
                if (unit != null && unit.isSelected)
                {
                    if (clickedTask != null)
                    {
                        unit.MoveTo(hit.point, clickedTask);

                        if (clickedTask is GhostBuilding ghost)
                        {
                            unit.SetTask(ghost);
                        }
                    }
                    else
                    {
                        unit.MoveTo(hit.point);
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

        if (cam.orthographic)
        {
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * zoomSpeed, minZoom, maxZoom);
        }
        else
        {
            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView - scroll * zoomSpeed, 10f, 90f);
        }
    }

    Rect GetScreenRect(Vector2 p1, Vector2 p2)
    {
        float x = Mathf.Min(p1.x, p2.x);
        float y = Mathf.Min(p1.y, p2.y);
        float w = Mathf.Abs(p1.x - p2.x);
        float h = Mathf.Abs(p1.y - p2.y);
        return new Rect(x, y, w, h);
    }
}