using UnityEngine;

public class PlayerThrowManager : MonoBehaviour
{
    public static PlayerThrowManager Instance { get; private set; }
    public Camera cam;

    [Header("Throw & Taming Settings")]
    public float throwRangeRadius = 5f; // รัศมีรอบจุดตกที่สัตว์จะถือว่า "กินอาหารชิ้นนี้"
    public LayerMask groundLayer;     // เลือก Layer ของพื้นดิน (เพื่อให้ Raycast ตกลงบนพื้นเป๊ะๆ)

    private SO_ItemData currentItemToThrow;
    private bool isThrowMode = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        if (cam == null) cam = Camera.main;
    }

    public void StartThrowMode(SO_ItemData itemData)
    {
        currentItemToThrow = itemData;
        isThrowMode = true;
        Debug.Log($"🎯 [Throw System] เปิดโหมดขว้าง {itemData.itemName} เล็งตำแหน่งบนพื้นแล้วคลิกซ้าย!");
    }

    private void Update()
    {
        if (!isThrowMode || currentItemToThrow == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            ExecuteThrowAndCheckTame();
            isThrowMode = false;
        }
        else if (Input.GetMouseButtonDown(1))
        {
            isThrowMode = false;
            currentItemToThrow = null;
            Debug.Log("🚫 [Throw System] ยกเลิกการขว้าง");
        }
    }

    void ExecuteThrowAndCheckTame()
    {
        if (cam == null) return;

        // 1. ยิง Raycast จากหน้าจอกล้องลงไปหาจุดที่เมาส์ชี้บนพื้นผิว
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            targetPoint = hit.point; // จุดที่อาหารจะตกลงบนพื้น
        }
        else
        {
            targetPoint = ray.GetPoint(15f); // เผื่อขว้างเลยออกไปไกลๆ
        }

        // 2. ดึง Prefab หน้าตาอาหารชิ้นนั้นมาเสกโชว์แว้บหนึ่งตรงจุดตก (เพื่อความสวยงามทาง Visual)
        GameObject itemPrefabToSpawn = null;
        if (currentItemToThrow is SO_PlantProduct plantProduct)
        {
            itemPrefabToSpawn = plantProduct.productPrefab;
        }
        if (itemPrefabToSpawn == null)
        {
            itemPrefabToSpawn = currentItemToThrow.dropItemPrefab;
        }

        if (itemPrefabToSpawn != null)
        {
            // เสกโมเดลอาหารลงบนพื้นตรงจุดเป้าหมาย (ให้ลอยเหนือพื้นนิดหน่อยก็ได้)
            Vector3 spawnPos = targetPoint + Vector3.up * 0.2f; 
            GameObject droppedFood = Instantiate(itemPrefabToSpawn, spawnPos, Quaternion.identity);

            // ตั้งเวลาทำลายโมเดลอาหารทิ้งหลังตกถึงพื้น 3 วินาที (ให้ผู้เล่นทันเห็นว่าขว้างอะไรลงไป)
            Destroy(droppedFood, 3f);
        }

        // 3. 🟢 ใช้ Physics.OverlapSphere กวาดหารอบจุดตกในรัศมี throwRangeRadius ทันที!
        Collider[] hitColliders = Physics.OverlapSphere(targetPoint, throwRangeRadius);
        foreach (var col in hitColliders)
        {
            // เช็คว่าเจอสัตว์ป่า TameableAnimal แถวนั้นไหม
            TameableAnimal animal = col.GetComponent<TameableAnimal>();
            if (animal == null)
            {
                animal = col.GetComponentInParent<TameableAnimal>();
            }

            // ถ้าเจอสัตว์ป่าใกล้จุดที่ขว้างอาหารลงไป -> ส่งไอเทมไปให้มันเช็คทันที!
            if (animal != null)
            {
                Debug.Log($"🍎 [Throw System] อาหารตกใกล้ {animal.gameObject.name} กำลังให้มันตรวจสอบอาหาร...");
                animal.TryTameWithFood(currentItemToThrow);
                break; // เจอตัวนึงแล้ว จบการเช็ค (หรือถ้าอยากให้โดนหลายตัวพร้อมกันก็เอา break ออกได้ครับ)
            }
        }

        // 4. หักไอเทมออกจากกระเป๋าจริงของผู้เล่น 1 ชิ้น
        if (ResourceInventory.Instance != null)
        {
            ResourceInventory.Instance.ConsumeResourceByName(currentItemToThrow.itemName, 1);
            Debug.Log($"📦 [Inventory] ใช้ {currentItemToThrow.itemName} ไป 1 ชิ้น");
        }

        currentItemToThrow = null;
    }
}