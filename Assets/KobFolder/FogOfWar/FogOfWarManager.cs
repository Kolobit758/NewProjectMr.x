using UnityEngine;
using System.Collections.Generic;

public class FogOfWarManager : MonoBehaviour
{
    public static FogOfWarManager Instance;

    [Header("Target Field")]
    [Tooltip("ลากแผ่นพื้นสีเขียว (Plane) ใน Hierarchy มาใส่ตรงนี้")]
    public Transform groundPlane;

    [Header("Fog Settings")]
    [Tooltip("ความละเอียดของหมอก แนะนำ 256 เพื่อความลื่นไหล")]
    public int textureResolution = 256; 
    [Tooltip("ความสูงของหมอกที่ต้องการให้ลอยเหนือพื้นดิน")]
    public float fogHeight = 1.5f;

    [Header("Performance")]
    [Tooltip("ให้หมอกอัปเดตทุก ๆ 0.1 วินาที (10 ครั้งต่อวินาที) ช่วยให้เกมไม่กระตุก")]
    public float updateInterval = 0.1f;
    private float updateTimer = 0f;

    [Header("Rendering")]
    public Material fogMaterial;
    
    // ปรับค่าความเข้มของหมอก (0 = ใสสะอาด, 1 = ดำสนิท)
    [HideInInspector] private float unexploredValue = 1f; 
    [HideInInspector] private float exploredValue = 0.5f;   

    private Texture2D fogTexture;
    private Color[] pixels; // เปลี่ยนมาใช้ Color ปกติเพื่อความแม่นยำในการคำนวณของ URP
    private byte[,] fogData; 
    private MeshRenderer fogRenderer;
    private List<FogVisionAgent> visionSources = new List<FogVisionAgent>();
    
    private Vector3 minBound;
    private Vector3 maxBound;
    private float worldWidth;
    private float worldLength;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (groundPlane == null)
        {
            Debug.LogError("อย่าลืมลากพื้น (Ground Plane) มาใส่ในช่องของ FogManager ด้วยนะคร้าบ!");
            return;
        }

        // 1. คำนวณขนาดและขอบเขตพื้นที่จากแผ่นพื้นจริงของคุณ
        MeshRenderer groundRenderer = groundPlane.GetComponent<MeshRenderer>();
        minBound = groundRenderer.bounds.min;
        maxBound = groundRenderer.bounds.max;
        worldWidth = groundRenderer.bounds.size.x;
        worldLength = groundRenderer.bounds.size.z;

        // 2. สร้าง Texture หมอกและจองพื้นที่หน่วยความจำ
        fogTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.R8, false); // ใช้ตารางสีช่องเดี่ยวเพื่อรีด Performance
        fogTexture.filterMode = FilterMode.Bilinear;
        fogTexture.wrapMode = TextureWrapMode.Clamp;

        fogData = new byte[textureResolution, textureResolution];
        pixels = new Color[textureResolution * textureResolution];

        // เติมหมอกสีดำ (ค่าสูงสุดคือ 1) ให้เต็มพื้นที่ตั้งแต่เริ่มเกม
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color(unexploredValue, 0, 0, 0); 

        fogTexture.SetPixels(pixels);
        fogTexture.Apply();

        // 3. สร้างแผ่นสี่เหลี่ยมสำหรับวาดหมอกให้ขนาดพอดีเป๊ะกับพื้น
        GameObject fogPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fogPlane.name = "FogPlane_Generated";
        
        // วางกึ่งกลางฉาก ขยับความสูงขึ้นมา และหันหน้าเข้าหากล้อง Top-down
        fogPlane.transform.position = new Vector3(groundPlane.position.x, fogHeight, groundPlane.position.z);
        fogPlane.transform.localScale = new Vector3(worldWidth, worldLength, 1f);
        fogPlane.transform.rotation = Quaternion.Euler(90f, 0f, 0f); 
        
        if (fogPlane.TryGetComponent<Collider>(out Collider col)) Destroy(col);

        fogRenderer = fogPlane.GetComponent<MeshRenderer>();
        fogRenderer.material = fogMaterial;
        fogRenderer.material.mainTexture = fogTexture;

        UpdateFogTexture();
    }

    void LateUpdate()
    {
        if (groundPlane == null) return;

        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            UpdateFogTexture();
            updateTimer = 0f;
        }
    }

    void UpdateFogTexture()
    {
        // สลับพื้นที่ที่กำลังเห็น ให้กลายเป็นพื้นที่เคยเดินผ่านแล้ว
        for (int y = 0; y < textureResolution; y++)
        {
            for (int x = 0; x < textureResolution; x++)
            {
                if (fogData[x, y] == 2)
                    fogData[x, y] = 1;
            }
        }

        // วาดวงกลมการมองเห็นของยูนิตทั้งหมด
        foreach (FogVisionAgent agent in visionSources)
        {
            if (agent != null)
            {
                RevealCircle(WorldToTex(agent.transform.position), agent.visionRadius);
            }
        }

        // แปลงข้อมูลข้อมูลหมอกออกมาเป็นระดับความมืด (เก็บไว้ในช่องสีแดง R เพื่อส่งให้ Shader)
        for (int y = 0; y < textureResolution; y++)
        {
            for (int x = 0; x < textureResolution; x++)
            {
                int index = y * textureResolution + x;
                byte state = fogData[x, y];

                if (state == 0)
                    pixels[index] = new Color(unexploredValue, 0, 0, 0); // ทึบสนิท
                else if (state == 1)
                    pixels[index] = new Color(exploredValue, 0, 0, 0);   // หมอกจาง
                else
                    pixels[index] = new Color(0f, 0, 0, 0);              // ใสสะอาด มองเห็นชัดเจน
            }
        }

        fogTexture.SetPixels(pixels);
        fogTexture.Apply();
    }

    void RevealCircle(Vector2Int center, float radiusWorld)
    {
        float averageWorldSize = (worldWidth + worldLength) / 2f;
        int radiusTex = Mathf.RoundToInt((radiusWorld / averageWorldSize) * textureResolution);
        
        int cx = center.x;
        int cy = center.y;

        for (int y = -radiusTex; y <= radiusTex; y++)
        {
            for (int x = -radiusTex; x <= radiusTex; x++)
            {
                int fx = cx + x;
                int fy = cy + y;

                if (fx < 0 || fx >= textureResolution || fy < 0 || fy >= textureResolution) continue;

                if (x * x + y * y <= radiusTex * radiusTex)
                {
                    fogData[fx, fy] = 2; // กำหนดสถานะเป็นกำลังมองเห็น
                }
            }
        }
    }

    Vector2Int WorldToTex(Vector3 worldPos)
    {
        float normX = Mathf.InverseLerp(minBound.x, maxBound.x, worldPos.x);
        float normZ = Mathf.InverseLerp(minBound.z, maxBound.z, worldPos.z);
        
        int x = Mathf.FloorToInt(normX * textureResolution);
        int y = Mathf.FloorToInt(normZ * textureResolution);
        return new Vector2Int(x, y);
    }

    public void RegisterVisionSource(FogVisionAgent agent)
    {
        if (!visionSources.Contains(agent)) visionSources.Add(agent);
    }

    public void UnregisterVisionSource(FogVisionAgent agent)
    {
        visionSources.Remove(agent);
    }
}