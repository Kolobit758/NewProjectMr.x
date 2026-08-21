using UnityEngine;
using UnityEngine.UI;

public enum TeamType { PlayerUnit, Building, Enemy }

public class MinimapMarker : MonoBehaviour
{
    
    public TeamType teamType;

    [Header("Layer Settings")]
    public string minimapIconLayerName = "miniMapLayer";   // layer สำหรับไอคอนบน minimap

    [Header("Minimap Icon Customization")]
    public Sprite customIconSprite;                      // รูปไอคอนบน Minimap (ถ้าไม่ใส่จะใช้รูปสี่เหลี่ยมขาวพื้นฐาน)
    public Color customColor = Color.clear;               // สีไอคอน (ถ้าตั้งเป็น clear จะใช้สีตาม TeamType)
    public Vector2 iconSize = new Vector2(8f, 8f);        // ขนาดไอคอนบน Minimap ( default 8x8 )

    [Header("Minimap Icon")]
    private GameObject iconObject;
    private Image iconImage;
    private static Transform minimapIconsParent;
    private static Camera minimapCam;
    private static RectTransform minimapRawImageRect;

    [Header("Head Marker (World Space Canvas)")]
    public bool enableHeadMarker = true;
    public Vector3 headMarkerOffset = new Vector3(0f, 2.2f, 0f);
    public Vector2 headMarkerSize = new Vector2(40f, 40f);
    public float headMarkerScale = 0.01f;

    [Header("Optional Sprites")]
    public Sprite playerMarkerSprite;
    public Sprite buildingMarkerSprite;
    public Sprite enemyMarkerSprite;

    private GameObject headMarkerObject;
    private Image headMarkerImage;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;

        if (minimapCam == null)
        {
            GameObject camObj = GameObject.Find("MinimapCamera");
            if (camObj != null) minimapCam = camObj.GetComponent<Camera>();
        }

        CreateMinimapMarker();
        Debug.Log("Minimap start mark");

        if (enableHeadMarker)
            InitHeadMarker();
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
        SetLayerRecursive(iconObject, minimapIconLayerName);

        iconImage = iconObject.AddComponent<Image>();
        if (customIconSprite != null)
        {
            iconImage.sprite = customIconSprite;
        }
        else
        {
            iconImage.sprite = GetDefaultCircleSprite();
        }

        if (customColor != Color.clear && customColor.a > 0f)
        {
            iconImage.color = customColor;
        }
        else
        {
            SetColorByTeam(iconImage);
        }

        RectTransform rt = iconObject.GetComponent<RectTransform>();
        rt.sizeDelta = iconSize;
    }

    private static Sprite defaultCircleSprite;

    public static Sprite GetDefaultCircleSprite()
    {
        if (defaultCircleSprite != null) return defaultCircleSprite;

        int res = 64;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        float center = res / 2f;
        float radius = center - 2f;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                {
                    float alpha = Mathf.Clamp01(radius + 1.5f - dist);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        defaultCircleSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
        return defaultCircleSprite;
    }

    void InitHeadMarker()
    {
        headMarkerObject = new GameObject($"HeadMarker_{gameObject.name}");
        headMarkerObject.transform.SetParent(transform, false);
        headMarkerObject.transform.localPosition = headMarkerOffset;
        headMarkerObject.transform.localScale = Vector3.one * headMarkerScale;

        Canvas canvas = headMarkerObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = mainCam;

        headMarkerObject.AddComponent<CanvasScaler>();
        headMarkerObject.AddComponent<GraphicRaycaster>();

        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(headMarkerObject.transform, false);

        headMarkerImage = iconGO.AddComponent<Image>();
        SetColorByTeam(headMarkerImage);

        switch (teamType)
        {
            case TeamType.PlayerUnit:
                if (playerMarkerSprite != null) headMarkerImage.sprite = playerMarkerSprite;
                break;
            case TeamType.Building:
                if (buildingMarkerSprite != null) headMarkerImage.sprite = buildingMarkerSprite;
                break;
            case TeamType.Enemy:
                if (enemyMarkerSprite != null) headMarkerImage.sprite = enemyMarkerSprite;
                break;
        }

        RectTransform rt = iconGO.GetComponent<RectTransform>();
        rt.sizeDelta = headMarkerSize;
        rt.anchoredPosition = Vector2.zero;
    }

    void SetColorByTeam(Image img)
    {
        switch (teamType)
        {
            case TeamType.PlayerUnit:
                img.color = Color.green;
                break;
            case TeamType.Building:
                img.color = Color.yellow;
                break;
            case TeamType.Enemy:
                img.color = Color.red;
                break;
        }
    }

    // ตั้ง Layer ให้ตัวเองและลูกทุกตัว (สำคัญเพราะ CanvasScaler/GraphicRaycaster ไม่ auto สืบทอด layer)
    void SetLayerRecursive(GameObject obj, string layerName)
    {
        int layerIndex = LayerMask.NameToLayer(layerName);
        if (layerIndex == -1)
        {
            Debug.LogWarning($"Layer '{layerName}' ไม่มีอยู่ใน Project Settings > Tags and Layers");
            return;
        }

        obj.layer = layerIndex;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layerName);
        }
    }

    void LateUpdate()
    {
        if (headMarkerObject != null && mainCam != null)
        {
            headMarkerObject.transform.rotation = mainCam.transform.rotation;
        }

        if (iconObject == null || minimapCam == null || minimapRawImageRect == null) return;

        Vector3 camPos = minimapCam.transform.position;
        Vector3 unitPos = transform.position;

        float offsetX = unitPos.x - camPos.x;
        float offsetZ = unitPos.z - camPos.z;

        float orthoSize = minimapCam.orthographicSize;
        float viewHeight = orthoSize * 2f;
        float viewWidth = viewHeight * minimapCam.aspect;

        Vector2 uiSize = minimapRawImageRect.rect.size;

        float uiX = (offsetX / (viewWidth / 2f)) * (uiSize.x / 2f);
        float uiY = (offsetZ / (viewHeight / 2f)) * (uiSize.y / 2f);

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
        if (headMarkerObject != null) Destroy(headMarkerObject);
    }
}