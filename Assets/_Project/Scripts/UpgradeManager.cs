using TMPro;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("Upgrade State")]
    public bool hasHeatLamp = false;
    public bool hasPetHouse = false;
    public int petHouseEnergyBonus = 1;

    private bool petHouseEffectApplied = false;

    [Header("UI")]
    public TextMeshProUGUI upgradeText;
    public bool HasHeatlamp => hasHeatLamp;
    public bool HasHeatLamp => hasHeatLamp;
    public bool HasPetHouse => hasPetHouse;
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
        ApplyPetHouseEffectIfNeeded();
        RefreshUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            TryUnlockHeatLamp();
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            TryBuildPetHouse();
        }
    }

    private void TryUnlockHeatLamp()
    {
        if (hasHeatLamp)
        {
            Debug.Log("heat Lamp already unlocked");
            ShowHUDPopup("Heat Lamp already unlocked", "This upgrade is already active.");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InvnetroyManager not found");
            return;
        }

        bool success = InventoryManager.Instance.SpendForHeatLamp();

        if (!success)
            return;

        hasHeatLamp = true;

        Debug.Log("Upgrade Unlocked: Heat Lamp");

        if(InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ShowDiscoveryMessage("Heat Lamp Unlocked!");
        }

        RefreshUI();
    }

    private void TryBuildPetHouse()
    {
        if (hasPetHouse)
        {
            Debug.Log("Pet House already built");
            ShowHUDPopup("Pet House already built", "Dog already has the Pet House bonus.");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryManager not found");
            return;
        }

        bool success = InventoryManager.Instance.SpendForPetHouse();
        if (!success)
        {
            return;
        }

        hasPetHouse = true;
        ApplyPetHouseEffectIfNeeded();

        Debug.Log("Upgrade Built: Pet House");
        InventoryManager.Instance.ShowDiscoveryMessage("Pet House Built!");
        RefreshUI();
    }

    private void ApplyPetHouseEffectIfNeeded()
    {
        if (!hasPetHouse || petHouseEffectApplied)
        {
            return;
        }

        if (PetManager.Instance == null)
        {
            Debug.LogWarning("PetManager not found. Pet House energy bonus was not applied.");
            return;
        }

        PetManager.Instance.IncreaseDogMaxEnergy(petHouseEnergyBonus);
        petHouseEffectApplied = true;
    }

    private void RefreshUI()
    {
        if (upgradeText == null)
        return;

        string heatLampStatus;
        if (hasHeatLamp)
        {
            heatLampStatus = "Heat Lamp: Unlocked";
        }
        else
        {
            heatLampStatus = "Heat Lamp: Locked | Need Pepper x3 + Flame Pepper x1 | Press U";
        }

        string petHouseStatus;
        if (hasPetHouse)
        {
            petHouseStatus = "Pet House: Built | Dog Max Energy +" + petHouseEnergyBonus;
        }
        else
        {
            petHouseStatus = "Pet House: Locked | Need Pepper x5 + Flame Pepper x1 + Fog Dust x3 | Press B";
        }

        upgradeText.text = heatLampStatus + "\n" + petHouseStatus;
    }

    private void ShowHUDPopup(string title, string detail)
    {
        if (HUDController.Instance != null)
        {
            HUDController.Instance.ShowPopup(title, detail);
        }
    }
}
