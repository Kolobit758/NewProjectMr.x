using UnityEngine;

public class PlantMasteryManager : MonoBehaviour
{
    public static PlantMasteryManager Instance { get; private set; }

    [Header("Pepper Mastery")]
    public int pepperMasteryLevel = 0;
    public int pepperMasteryXP = 0;
    public int maxMasteryLevel = 5;

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

    public void AddPepperMasteryXPByQuality(int qualityStars)
    {
        int xpGain = GetXPFromQuality(qualityStars);
        pepperMasteryXP += xpGain;
        Debug.Log("Pepper Mastery +" + xpGain + " XP");

        CheckLevelUp();
        Debug.Log("Pepper Mastery Lv." + pepperMasteryLevel + " | XP " + pepperMasteryXP + "/" + GetXPToNextLevel());
        RefreshUI();
    }

    private int GetXPFromQuality(int qualityStars)
    {
        switch (qualityStars)
        {
            case 1:
                return 5;
            case 2:
                return 10;
            case 3:
                return 20;
            case 4:
                return 35;
            case 5:
                return 50;
            default:
                return 5;
        }
    }

    private void CheckLevelUp()
    {
        while (pepperMasteryLevel< maxMasteryLevel && pepperMasteryXP >= GetXPToNextLevel())
        {
            pepperMasteryXP -= GetXPToNextLevel();
            pepperMasteryLevel++;

            Debug.Log("Pepper Mastery Level Up! Lv." + pepperMasteryLevel);
        }
    }

    private int GetXPToNextLevel ()
    {
        if (pepperMasteryLevel >= maxMasteryLevel)
        return 0;

    return 50 + (pepperMasteryLevel * 50);
    }

    private string GetPepperHint()
    {
        if (pepperMasteryLevel >= 3)
        {
            return "Flame Pepper Unlock by Heat + Ash + Waterless";
        }

        if (pepperMasteryLevel >= 2)
        {
            return "Try Ash with Pepper";
        }

        if (pepperMasteryLevel >= 1)
        {
            return "Pepper like heat";
        }

        return "no data";
    }

    private void RefreshUI()
    {
        if (inventoryUI != null)
        {
            inventoryUI.UpdatePepperMastery(
                pepperMasteryLevel,
                pepperMasteryXP,
                GetXPToNextLevel()
            );
            inventoryUI.UpdatePepperHint(GetPepperHint());
        }
    }

    public int GetPepperMasteryLevel()
    {
        return pepperMasteryLevel;
    }
}
