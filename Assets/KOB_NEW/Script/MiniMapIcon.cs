using UnityEngine;
using UnityEngine.UI;

public class MinimapIcon : MonoBehaviour
{
    public enum TeamType { PlayerUnit, Building, Enemy }
    public TeamType teamType;

    private GameObject iconObject;
    private Image iconImage;
    private static Transform minimapIconsParent;
    
    private static Camera minimapCam;
    private static RectTransform minimapRawImageRect;

    void Start()
    {
        if (minimapCam == null)
        {
            GameObject camObj = GameObject.Find("MinimapCamera");
            if (camObj != null) minimapCam = camObj.GetComponent<Camera>();
        }

        CreateMinimapMarker();
    }

    void CreateMinimapMarker()
    {
        if (minimapIconsParent == null)
        {
            GameObject container = GameObject.Find("MinimapIconsContainer");
            if (container != null)
            {
                minimapIconsParent = container.transform;
                minimapRawImageRect = container.GetComponentInParent<RectTransform>();
            }
        }

        if (minimapIconsParent == null) return;

        iconObject = new GameObject($"Icon_{gameObject.name}");
        iconObject.transform.SetParent(minimapIconsParent, false);

        iconImage = iconObject.AddComponent<Image>();
        
        switch (teamType)
        {
            case TeamType.PlayerUnit:
                iconImage.color = Color.green;
                break;
            case TeamType.Building:
                iconImage.color = Color.yellow;
                break;
            case TeamType.Enemy:
                iconImage.color = Color.red;
                break;
        }

        RectTransform rt = iconObject.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(6, 6);
    }

    void LateUpdate()
    {
        if (iconObject == null || minimapCam == null || minimapRawImageRect == null) return;

        // 🟢 คำนวณตำแหน่งเทียบกับศูนย์กลางของกล้อง Minimap โดยตรง
        Vector3 camPos = minimapCam.transform.position;
        Vector3 unitPos = transform.position;

        // ระยะห่างจากกึ่งกลางกล้อง (แกน X และ Z)
        float offsetX = unitPos.x - camPos.x;
        float offsetZ = unitPos.z - camPos.z;

        // ขนาดความกว้าง/สูงที่กล้องมองเห็น (Orthographic Size * 2)
        float orthoSize = minimapCam.orthographicSize;
        float viewHeight = orthoSize * 2f;
        float viewWidth = viewHeight * minimapCam.aspect; // ปรับตามสัดส่วนจอของกล้อง

        // ขนาดของ Raw Image บน UI จริงๆ
        Vector2 uiSize = minimapRawImageRect.rect.size;

        // แปลงระยะทางโลกให้เป็นสัดส่วนบน UI
        float uiX = (offsetX / (viewWidth / 2f)) * (uiSize.x / 2f);
        float uiY = (offsetZ / (viewHeight / 2f)) * (uiSize.y / 2f);

        // เช็คว่าอยู่ในกรอบ Minimap หรือไม่
        bool isWithinBounds = Mathf.Abs(uiX) <= (uiSize.x / 2f) && Mathf.Abs(uiY) <= (uiSize.y / 2f);
        iconObject.SetActive(isWithinBounds);

        if (isWithinBounds)
        {
            iconObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(uiX, uiY);
        }
    }

    void OnDestroy()
    {
        if (iconObject != null) Destroy(iconObject);
    }
}