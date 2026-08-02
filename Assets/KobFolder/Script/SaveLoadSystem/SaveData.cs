using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    // === ของเดิมก๊อปวางครบเซ็ตไม่มีหาย ===
    public List<string> playerInventoryKeys = new List<string>();
    public List<int> playerInventoryValues = new List<int>();

    public List<string> outpostVaultKeys = new List<string>();
    public List<int> outpostVaultValues = new List<int>();
    
    public List<string> capturedOutpostIDs = new List<string>();
    
    public List<string> outpostStateIds = new List<string>();
    public List<bool> outpostStateOccupied = new List<bool>();

    public List<UnitInstance> tamedAnimals = new List<UnitInstance>();

    public float playerPosX, playerPosY, playerPosZ;
    public string currentMapName;

    public List<string> savedAnimalTemplates = new List<string>();
    public List<string> savedAnimalUniqueIds = new List<string>();
    public List<string> savedAnimalCustomNames = new List<string>();
    public List<int> savedAnimalHPs = new List<int>();

    // 🟢 NEW: ตัวแปรสำหรับการเซฟ/โหลดสถานะความหิว และ ยูนิตทหารที่ประจำค่ายย่อยแบบราบรื่น
    public List<float> outpostHungerLevels = new List<float>(); 
    public List<string> garrisonOutpostIDs = new List<string>(); 
    public List<string> garrisonTemplateIDs = new List<string>();
    public List<string> garrisonUniqueIDs = new List<string>();  
    public List<string> garrisonCustomNames = new List<string>();
    public List<int> garrisonHPs = new List<int>();             
}