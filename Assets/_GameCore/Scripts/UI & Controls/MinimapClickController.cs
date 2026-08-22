using UnityEngine;
using UnityEngine.EventSystems;

public class MinimapClickController : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    public Transform mainCameraTransform;
    private Camera minimapCam;

    void Start()
    {
        if (mainCameraTransform == null && RTS_movement.instance != null)
        {
            mainCameraTransform = RTS_movement.instance.transform;
        }

        GameObject camObj = GameObject.Find("MinimapCamera");
        if (camObj != null)
        {
            minimapCam = camObj.GetComponent<Camera>();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector2 localPoint))
        {
            // คำนวณสัดส่วนการคลิกบนกรอบ RawImage (0 ถึง 1)
            Rect rect = rectTransform.rect;
            float normalizedX = (localPoint.x - rect.xMin) / rect.width;
            float normalizedY = (localPoint.y - rect.yMin) / rect.height;

            if (minimapCam != null)
            {
                // 🟢 ให้กล้องมินิแมปยิง Raycast ลงไปในโลก 3D ตามจุดที่คลิกบนจอ!
                // วิธีนี้จะแม่นยำ 100% ไม่ว่ากล้องจะซูมเท่าไหร่หรืออยู่ตำแหน่งไหน
                Ray ray = minimapCam.ViewportPointToRay(new Vector3(normalizedX, normalizedY, 0));
                
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    if (mainCameraTransform != null)
                    {
                        Vector3 newPos = mainCameraTransform.position;
                        newPos.x = hit.point.x;
                        newPos.z = hit.point.z;
                        mainCameraTransform.position = newPos;
                    }
                }
            }
        }
    }
}