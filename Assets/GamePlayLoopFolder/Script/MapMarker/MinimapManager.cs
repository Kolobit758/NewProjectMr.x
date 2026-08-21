// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;

// public class MinimapManager : MonoBehaviour
// {
//     [System.Serializable]
//     public class MarkerStyle
//     {
//         public MinimapMarkerType type;

//         [Header("UI")]
//         public Sprite sprite;
//         public Color color = Color.white;

//         [Header("Size")]
//         public Vector2 size = new Vector2(10f, 10f);
//     }

//     [Header("References")]
//     [SerializeField] private Camera minimapCamera;
//     [SerializeField] private RectTransform minimapRect;
//     [SerializeField] private RectTransform markerParent;

//     [Header("Marker Prefab")]
//     [SerializeField] private GameObject markerPrefab;

//     [Header("Marker Styles")]
//     [SerializeField] private MarkerStyle[] markerStyles;

//     [Header("Update")]
//     [SerializeField] private float updateInterval = 0.05f;

//     private readonly Dictionary<MinimapMarker, Image> activeMarkers = new();
//     private readonly List<MinimapMarker> markers = new();

//     private float updateTimer;

//     private void Start()
//     {
//         RefreshMarkers();
//     }

//     private void Update()
//     {
//         updateTimer += Time.deltaTime;

//         if (updateTimer >= updateInterval)
//         {
//             updateTimer = 0f;

//             RefreshMarkers();
//             UpdateMarkerPositions();
//         }
//     }

//     // ============================================================
//     // FIND MARKERS
//     // ============================================================

//     private void RefreshMarkers()
//     {
//         MinimapMarker[] foundMarkers =
//             FindObjectsByType<MinimapMarker>(FindObjectsSortMode.None);

//         HashSet<MinimapMarker> currentMarkers =
//             new HashSet<MinimapMarker>(foundMarkers);

//         // Add new markers
//         foreach (MinimapMarker marker in foundMarkers)
//         {
//             if (marker == null)
//                 continue;

//             if (!activeMarkers.ContainsKey(marker))
//             {
//                 CreateMarker(marker);
//             }
//         }

//         // Remove destroyed markers
//         List<MinimapMarker> toRemove = new();

//         foreach (var pair in activeMarkers)
//         {
//             if (!currentMarkers.Contains(pair.Key) || pair.Key == null)
//             {
//                 if (pair.Value != null)
//                     Destroy(pair.Value.gameObject);

//                 toRemove.Add(pair.Key);
//             }
//         }

//         foreach (MinimapMarker marker in toRemove)
//         {
//             activeMarkers.Remove(marker);
//         }
//     }

//     // ============================================================
//     // CREATE UI MARKER
//     // ============================================================

//     private void CreateMarker(MinimapMarker marker)
//     {
//         if (markerPrefab == null)
//         {
//             Debug.LogError("Minimap Marker Prefab is missing!");
//             return;
//         }

//         GameObject markerObject =
//             Instantiate(markerPrefab, markerParent);

//         Image image = markerObject.GetComponent<Image>();

//         if (image == null)
//         {
//             Debug.LogError(
//                 "Minimap Marker Prefab must have an Image component!"
//             );

//             Destroy(markerObject);
//             return;
//         }

//         activeMarkers.Add(marker, image);

//         ApplyStyle(marker, image);

//         RectTransform rect =
//             markerObject.GetComponent<RectTransform>();

//         rect.localScale = Vector3.one;
//     }

//     // ============================================================
//     // APPLY COLOR / SPRITE
//     // ============================================================

//     private void ApplyStyle(
//         MinimapMarker marker,
//         Image image)
//     {
//         MarkerStyle style = GetStyle(marker.markerType);

//         if (style == null)
//             return;

//         image.sprite = style.sprite;
//         image.color = style.color;

//         RectTransform rect =
//             image.GetComponent<RectTransform>();

//         rect.sizeDelta = style.size;
//     }

//     private MarkerStyle GetStyle(MinimapMarkerType type)
//     {
//         if (markerStyles == null)
//             return null;

//         foreach (MarkerStyle style in markerStyles)
//         {
//             if (style.type == type)
//                 return style;
//         }

//         return null;
//     }

//     // ============================================================
//     // UPDATE POSITION
//     // ============================================================

//     private void UpdateMarkerPositions()
//     {
//         if (minimapCamera == null)
//             return;

//         if (minimapRect == null)
//             return;

//         foreach (var pair in activeMarkers)
//         {
//             MinimapMarker marker = pair.Key;
//             Image image = pair.Value;

//             if (marker == null || image == null)
//                 continue;

//             Transform target = marker.target;

//             if (target == null)
//                 target = marker.transform;

//             Vector3 viewportPosition =
//                 minimapCamera.WorldToViewportPoint(
//                     target.position
//                 );

//             // ถ้าอยู่นอกกล้อง minimap
//             bool inside =
//                 viewportPosition.z > 0f &&
//                 viewportPosition.x >= 0f &&
//                 viewportPosition.x <= 1f &&
//                 viewportPosition.y >= 0f &&
//                 viewportPosition.y <= 1f;

//             image.gameObject.SetActive(inside);

//             if (!inside)
//                 continue;

//             RectTransform markerRect =
//                 image.rectTransform;

//             float x =
//                 (viewportPosition.x - 0.5f) *
//                 minimapRect.rect.width;

//             float y =
//                 (viewportPosition.y - 0.5f) *
//                 minimapRect.rect.height;

//             markerRect.anchoredPosition =
//                 new Vector2(x, y);
//         }
//     }
// }