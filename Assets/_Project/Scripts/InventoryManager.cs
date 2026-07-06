using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Inventory")]
    public int pepperCount = 0;
    public int flamePepperCount = 0;
    public int fogDustCount = 0;
    public int woodCount = 0;

    [Header("UI")]
    public InventoryUI inventoryUI;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        RefreshUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            AddWood(1);
            ShowDiscoveryMessage("Gathered Wood x1");
        }
    }

    public void AddPepper(int amount)
    {
        pepperCount += amount;
        Debug.Log("Inventory Pepper x" + pepperCount);
        RefreshUI();
    }

    public void AddFlamePepper(int amount)
    {
        flamePepperCount += amount;
        Debug.Log("Inventory: Flame Pepper x" + flamePepperCount);
        RefreshUI();
    }

    public void AddFogDust(int amount)
    {
        fogDustCount += amount;
        Debug.Log("Inventory: Fog Dust x" + fogDustCount);
        RefreshUI();
    }

    public void AddWood(int amount)
    {
        woodCount += amount;
        Debug.Log("Inventory: Wood x" + woodCount);
        RefreshUI();
    }

    public bool CanSpendPepper(int amount)
    {
        return pepperCount >= amount;
    }

    public bool SpendPepper(int amount)
    {
        if (!CanSpendPepper(amount))
        {
            Debug.Log("Not enough Pepper. Need Pepper x" + amount);
            return false;
        }

        pepperCount -= amount;
        Debug.Log("Spent Pepper x" + amount);
        RefreshUI();
        return true;
    }

    public void ShowDiscoveryMessage(string message)
    {
        if (inventoryUI != null)
        {
            inventoryUI.ShowDiscoveryMessage(message);
        }
    }

    public bool CanSpendForHeatLamp()
    {
        return pepperCount >= 3 && flamePepperCount >= 1;
    }

    public bool SpendForHeatLamp()
    {
        if (!CanSpendForHeatLamp())
        {
            Debug.Log("Not enough resources. need Pepper x3 and Flame pepper x1");
            return false;
        }

        pepperCount -= 3;
        flamePepperCount -=1;
        Debug.Log("Spend Pepper x3 and flame Pepper x1");
        RefreshUI();
        return true;


    }

    public bool CanSpendForPetHouse()
    {
        return pepperCount >= 5 && flamePepperCount >= 1 && fogDustCount >= 3;
    }

    public bool SpendForPetHouse()
    {
        if (!CanSpendForPetHouse())
        {
            Debug.Log("Not enough resources. Need Pepper x5, Flame Pepper x1, and Fog Dust x3");
            return false;
        }

        pepperCount -= 5;
        flamePepperCount -= 1;
        fogDustCount -= 3;
        Debug.Log("Spent Pepper x5, Flame Pepper x1, and Fog Dust x3");
        RefreshUI();
        return true;
    }

    public bool CanSpendForHouseLevel2()
    {
        return woodCount >= 5 && fogDustCount >= 3 && flamePepperCount >= 1;
    }

    public bool SpendForHouseLevel2()
    {
        if (!CanSpendForHouseLevel2())
        {
            Debug.Log("Not enough resources. Need Wood x5, Fog Dust x3, and Flame Pepper x1");
            return false;
        }

        woodCount -= 5;
        fogDustCount -= 3;
        flamePepperCount -= 1;
        Debug.Log("Spent Wood x5, Fog Dust x3, and Flame Pepper x1");
        RefreshUI();
        return true;
    }

    private void RefreshUI()
    {
        if (inventoryUI != null)
        {
            inventoryUI.UpdatePepperCount(pepperCount);
            inventoryUI.UpdateFlamePepperCount(flamePepperCount);
            inventoryUI.UpdateFogDustCount(fogDustCount);
            inventoryUI.UpdateWoodCount(woodCount);
        }
    }
}
