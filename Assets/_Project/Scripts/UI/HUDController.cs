using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [System.Serializable]
    public class ResourceRow
    {
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI countText;
        public Image icon;

        public void Set(string resourceName, int count)
        {
            if (nameText != null)
            {
                nameText.text = resourceName;
            }

            if (countText != null)
            {
                countText.text = count.ToString();
            }
        }
    }

    [System.Serializable]
    public class StatusRow
    {
        public TextMeshProUGUI statusNameText;
        public TextMeshProUGUI statusValueText;
        public Image icon;

        public void Set(string statusName, string statusValue)
        {
            if (statusNameText != null)
            {
                statusNameText.text = statusName;
            }

            if (statusValueText != null)
            {
                statusValueText.text = statusValue;
            }
        }
    }

    [System.Serializable]
    public class UpgradeRow
    {
        public TextMeshProUGUI upgradeNameText;
        public TextMeshProUGUI upgradeStatusText;
        public Image icon;

        public void Set(string upgradeName, string upgradeStatus)
        {
            if (upgradeNameText != null)
            {
                upgradeNameText.text = upgradeName;
            }

            if (upgradeStatusText != null)
            {
                upgradeStatusText.text = upgradeStatus;
            }
        }
    }

    [System.Serializable]
    public class RequirementRow
    {
        public GameObject root;
        public TextMeshProUGUI requirementObject;
        public TextMeshProUGUI requirementAmount;
        public Image requirementImage;
        public Color completeColor = new Color(0.45f, 1f, 0.45f);
        public Color incompleteColor = new Color(1f, 0.85f, 0.45f);

        public void Set(string objectText, string amountText, bool isComplete)
        {
            if (root != null)
            {
                root.SetActive(true);
            }

            Color color = isComplete ? completeColor : incompleteColor;

            if (requirementObject != null)
            {
                requirementObject.text = objectText;
                requirementObject.color = color;
            }

            if (requirementAmount != null)
            {
                requirementAmount.text = amountText;
                requirementAmount.color = color;
            }

            if (requirementImage != null)
            {
                requirementImage.color = color;
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
            else
            {
                if (requirementObject != null)
                {
                    requirementObject.text = "";
                }

                if (requirementAmount != null)
                {
                    requirementAmount.text = "";
                }
            }
        }
    }

    [System.Serializable]
    public class ActionSlot
    {
        public GameObject root;
        public Image icon;
        public Image keyBox;
        public TextMeshProUGUI smallHint;

        public void Set(string hintText)
        {
            if (smallHint != null)
            {
                smallHint.text = hintText;
            }
        }

        public void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.SetActive(visible);
            }
        }
    }

    [Header("Resource Panel")]
    [SerializeField] private ResourceRow pepperRow;
    [SerializeField] private ResourceRow flamePepperRow;
    [SerializeField] private ResourceRow fogDustRow;
    [SerializeField] private ResourceRow woodRow;

    [Header("Status Panel")]
    [SerializeField] private StatusRow weatherStatusRow;
    [SerializeField] private StatusRow fertilizerStatusRow;
    [SerializeField] private StatusRow pepperMasteryStatusRow;

    [Header("Upgrade Panel")]
    [SerializeField] private UpgradeRow heatLampUpgradeRow;
    [SerializeField] private UpgradeRow petHouseUpgradeRow;
    [SerializeField] private UpgradeRow houseLevelUpgradeRow;

    [Header("Dog Panel")]
    [SerializeField] private TextMeshProUGUI dogTitleText;
    [SerializeField] private TextMeshProUGUI dogStatusText;
    [SerializeField] private TextMeshProUGUI dogEnergyValueText;
    [SerializeField] private Image dogEnergyBarFill;
    [SerializeField] private GameObject dogTimeLeftRoot;
    [SerializeField] private TextMeshProUGUI dogTimeLeftValueText;
    [SerializeField] private Image exploreKeyImage;
    [SerializeField] private TextMeshProUGUI exploreHintText;
    [SerializeField] private Image feedKeyImage;
    [SerializeField] private TextMeshProUGUI feedHintText;

    [Header("Objective Panel")]
    [SerializeField] private TextMeshProUGUI objectiveTitleText;
    [SerializeField] private TextMeshProUGUI objectiveGoalText;
    [SerializeField] private RequirementRow requirement1;
    [SerializeField] private RequirementRow requirement2;
    [SerializeField] private RequirementRow requirement3;
    [SerializeField] private GameObject nextActionRoot;
    [SerializeField] private TextMeshProUGUI nextActionText;
    [SerializeField] private Image nextActionImage;

    [Header("Action Bar")]
    [SerializeField] private ActionSlot plantSlot;
    [SerializeField] private ActionSlot waterSlot;
    [SerializeField] private ActionSlot harvestSlot;
    [SerializeField] private ActionSlot fertilizeSlot;
    [SerializeField] private ActionSlot unlockSlot;
    [SerializeField] private bool showUnlockSlot = true;

    [Header("Popup Panel")]
    [SerializeField] private GameObject popupPanelRoot;
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [SerializeField] private TextMeshProUGUI popupTitleText;
    [SerializeField] private TextMeshProUGUI popupDetailText;
    [SerializeField] private float popupFadeTime = 0.2f;
    [SerializeField] private float popupHoldTime = 2f;

    [Header("Complete Panel")]
    [SerializeField] private GameObject completePanelRoot;
    [SerializeField] private TextMeshProUGUI completeTitleText;
    [SerializeField] private TextMeshProUGUI completeBodyText;
    [SerializeField] private TextMeshProUGUI completeContinueText;

    private Coroutine popupRoutine;
    private bool completeShown = false;

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
        SetupStaticText();
        HidePopupImmediate();

        if (completePanelRoot != null)
        {
            completePanelRoot.SetActive(false);
        }

        RefreshHUD();
    }

    private void Update()
    {
        RefreshHUD();
    }

    public void ShowPopup(string message)
    {
        ShowPopup(message, "");
    }

    public void ShowPopup(string title, string detail)
    {
        if (popupTitleText != null)
        {
            popupTitleText.text = title;
        }

        if (popupDetailText != null)
        {
            popupDetailText.text = detail;
        }

        if (popupRoutine != null)
        {
            StopCoroutine(popupRoutine);
        }

        if (popupCanvasGroup != null)
        {
            popupRoutine = StartCoroutine(ShowPopupRoutine());
            return;
        }

        if (popupPanelRoot != null)
        {
            popupPanelRoot.SetActive(true);
            popupRoutine = StartCoroutine(HidePopupAfterDelay());
        }
    }

    public void ShowPrototypeComplete()
    {
        completeShown = true;

        if (completePanelRoot != null)
        {
            completePanelRoot.SetActive(true);
        }

        if (completeTitleText != null)
        {
            completeTitleText.text = "PROTOTYPE COMPLETE";
        }

        if (completeBodyText != null)
        {
            completeBodyText.text = "House upgraded to Level 2.\nYou completed the core loop:\n\nFarming -> Pet -> Expedition -> Resource -> Upgrade";
        }

        if (completeContinueText != null)
        {
            completeContinueText.text = "Thank you for playing this prototype.";
        }
    }

    private void RefreshHUD()
    {
        UpdateResources();
        UpdateStatusPanel();
        UpdateUpgradePanel();
        UpdateDogPanel();
        UpdateObjectivePanel();

        if (!completeShown && HouseUpgradeManager.Instance != null && HouseUpgradeManager.Instance.HouseLevel >= 2)
        {
            ShowPrototypeComplete();
        }
    }

    private void SetupStaticText()
    {
        if (objectiveTitleText != null)
        {
            objectiveTitleText.text = "Current Goal";
        }

        SetActionSlot(plantSlot, "Plant Pepper", true);
        SetActionSlot(waterSlot, "Water", true);
        SetActionSlot(harvestSlot, "Harvest", true);
        SetActionSlot(fertilizeSlot, "Apply Fertilizer", true);
        SetActionSlot(unlockSlot, "Unlock Heat Lamp", showUnlockSlot);

        if (dogTitleText != null)
        {
            dogTitleText.text = "Dog";
        }

        if (exploreHintText != null)
        {
            exploreHintText.text = "Explore";
        }

        if (feedHintText != null)
        {
            feedHintText.text = "Feed";
        }
    }

    private void UpdateResources()
    {
        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null)
        {
            return;
        }

        SetResourceRow(pepperRow, "Pepper", inventory.PepperCount);
        SetResourceRow(flamePepperRow, "Flame Pepper", inventory.FlamePepperCount);
        SetResourceRow(fogDustRow, "Fog Dust", inventory.FogDustCount);
        SetResourceRow(woodRow, "Wood", inventory.WoodCount);
    }

    private void UpdateStatusPanel()
    {
        string weather = WeatherManager.Instance != null ? WeatherManager.Instance.CurrentWeather.ToString() : "Unknown";
        string fertilizer = FertilizerManager.Instance != null ? FertilizerManager.Instance.CurrentFertilizer.ToString() : "Unknown";
        int mastery = PlantMasteryManager.Instance != null ? PlantMasteryManager.Instance.GetPepperMasteryLevel() : 0;

        SetStatusRow(weatherStatusRow, "Weather", weather);
        SetStatusRow(fertilizerStatusRow, "Fertilizer", fertilizer);
        SetStatusRow(pepperMasteryStatusRow, "Pepper Mastery", "Lv. " + mastery);
    }

    private void UpdateUpgradePanel()
    {
        bool heatLampUnlocked = UpgradeManager.Instance != null && UpgradeManager.Instance.HasHeatLamp;
        bool petHouseBuilt = UpgradeManager.Instance != null && UpgradeManager.Instance.HasPetHouse;
        int houseLevel = HouseUpgradeManager.Instance != null ? HouseUpgradeManager.Instance.HouseLevel : 1;

        SetUpgradeRow(heatLampUpgradeRow, "Heat Lamp", heatLampUnlocked ? "Unlocked" : "Locked");
        SetUpgradeRow(petHouseUpgradeRow, "Pet House", petHouseBuilt ? "Built" : "Locked");
        SetUpgradeRow(houseLevelUpgradeRow, "House", "Level " + houseLevel);
    }

    private void UpdateDogPanel()
    {
        PetManager pet = PetManager.Instance;
        if (pet == null)
        {
            return;
        }

        if (dogStatusText != null)
        {
            dogStatusText.text = pet.IsExploring ? "Exploring" : "Idle";
        }

        if (dogEnergyValueText != null)
        {
            dogEnergyValueText.text = pet.DogEnergy + " / " + pet.DogMaxEnergy;
        }

        if (dogEnergyBarFill != null)
        {
            float fillAmount = pet.DogMaxEnergy <= 0 ? 0f : (float)pet.DogEnergy / pet.DogMaxEnergy;
            dogEnergyBarFill.fillAmount = Mathf.Clamp01(fillAmount);
        }

        if (dogTimeLeftRoot != null)
        {
            dogTimeLeftRoot.SetActive(pet.IsExploring);
        }

        if (dogTimeLeftValueText != null)
        {
            dogTimeLeftValueText.text = pet.IsExploring ? Mathf.CeilToInt(pet.DogExploreTimeRemaining) + "s" : "--";
        }
    }

    private void UpdateObjectivePanel()
    {
        InventoryManager inventory = InventoryManager.Instance;
        UpgradeManager upgrades = UpgradeManager.Instance;
        PetManager pet = PetManager.Instance;
        HouseUpgradeManager house = HouseUpgradeManager.Instance;

        if (inventory == null)
        {
            SetObjective("Prepare the Farm", "Inventory", "Loading", false, "Managers", "Waiting", false, "", "", false, "Wait for prototype managers to load.");
            return;
        }

        bool heatLampUnlocked = upgrades != null && upgrades.HasHeatLamp;
        bool petHouseBuilt = upgrades != null && upgrades.HasPetHouse;
        bool dogExploring = pet != null && pet.IsExploring;
        int houseLevel = house != null ? house.HouseLevel : 1;

        if (houseLevel >= 2)
        {
            SetObjective("Prototype Complete", "House", "Level 2", true, "Core Loop", "Complete", true, "", "", false, "The farm is ready to grow.");
            return;
        }

        if (!inventory.HasHarvestedPepper)
        {
            SetObjective("Plant and Harvest Pepper", "Pepper", FormatAmount(inventory.PepperCount, 1), inventory.PepperCount >= 1, "", "", false, "", "", false, "Select a plot and press 1 to plant Pepper.");
            return;
        }

        if (!inventory.HasDiscoveredFlamePepper)
        {
            int masteryLevel = PlantMasteryManager.Instance != null ? PlantMasteryManager.Instance.GetPepperMasteryLevel() : 0;
            bool isHeat = WeatherManager.Instance != null && WeatherManager.Instance.IsHeat();
            bool isAsh = FertilizerManager.Instance != null && FertilizerManager.Instance.CurrentFertilizer == FertilizerType.Ash;

            SetObjective(
                "Discover Flame Pepper",
                "Pepper Mastery", "Lv. " + Mathf.Min(masteryLevel, 3) + "/3", masteryLevel >= 3,
                "Weather", isHeat ? "Heat" : "Need Heat", isHeat,
                "Fertilizer", isAsh ? "Ash" : "Need Ash", isAsh,
                "Use Heat + Ash, then harvest an unwatered Pepper."
            );
            return;
        }

        if (!heatLampUnlocked)
        {
            SetObjective(
                "Unlock Heat Lamp",
                "Pepper", FormatAmount(inventory.PepperCount, 3), inventory.PepperCount >= 3,
                "Flame Pepper", FormatAmount(inventory.FlamePepperCount, 1), inventory.FlamePepperCount >= 1,
                "", "", false,
                "Press U to unlock Heat Lamp."
            );
            return;
        }

        if (!inventory.HasCollectedFogDust)
        {
            string dogEnergy = pet != null ? pet.DogEnergy + "/" + pet.DogMaxEnergy : "0/0";
            SetObjective(
                dogExploring ? "Collect Fog Dust" : "Send Dog Exploring",
                "Dog", dogExploring ? "Exploring" : "Idle", dogExploring,
                "Energy", dogEnergy, pet != null && pet.DogEnergy > 0,
                "Fog Dust", FormatAmount(inventory.FogDustCount, 1), inventory.FogDustCount >= 1,
                dogExploring ? "Wait for Dog to return." : "Press P to send Dog exploring."
            );
            return;
        }

        if (!petHouseBuilt)
        {
            SetObjective(
                "Build Pet House",
                "Pepper", FormatAmount(inventory.PepperCount, 5), inventory.PepperCount >= 5,
                "Flame Pepper", FormatAmount(inventory.FlamePepperCount, 1), inventory.FlamePepperCount >= 1,
                "Fog Dust", FormatAmount(inventory.FogDustCount, 3), inventory.FogDustCount >= 3,
                "Gather the cost, then press B to build Pet House."
            );
            return;
        }

        if (inventory.WoodCount < 5)
        {
            SetObjective("Gather Wood", "Wood", FormatAmount(inventory.WoodCount, 5), inventory.WoodCount >= 5, "", "", false, "", "", false, "Press J to gather Wood for now.");
            return;
        }

        SetObjective(
            "Upgrade House to Level 2",
            "Wood", FormatAmount(inventory.WoodCount, 5), inventory.WoodCount >= 5,
            "Fog Dust", FormatAmount(inventory.FogDustCount, 3), inventory.FogDustCount >= 3,
            "Flame Pepper", FormatAmount(inventory.FlamePepperCount, 1), inventory.FlamePepperCount >= 1,
            "Press L to upgrade House."
        );
    }

    private void SetObjective(
        string goal,
        string req1Name, string req1Amount, bool req1Complete,
        string req2Name, string req2Amount, bool req2Complete,
        string req3Name, string req3Amount, bool req3Complete,
        string nextAction)
    {
        if (objectiveGoalText != null)
        {
            objectiveGoalText.text = goal;
        }

        SetRequirementRow(requirement1, req1Name, req1Amount, req1Complete);
        SetRequirementRow(requirement2, req2Name, req2Amount, req2Complete);
        SetRequirementRow(requirement3, req3Name, req3Amount, req3Complete);

        if (nextActionRoot != null)
        {
            nextActionRoot.SetActive(!string.IsNullOrEmpty(nextAction));
        }

        if (nextActionText != null)
        {
            nextActionText.text = nextAction;
        }
    }

    private string FormatAmount(int current, int required)
    {
        return Mathf.Min(current, required) + "/" + required;
    }

    private void SetResourceRow(ResourceRow row, string resourceName, int count)
    {
        if (row != null)
        {
            row.Set(resourceName, count);
        }
    }

    private void SetStatusRow(StatusRow row, string statusName, string statusValue)
    {
        if (row != null)
        {
            row.Set(statusName, statusValue);
        }
    }

    private void SetUpgradeRow(UpgradeRow row, string upgradeName, string upgradeStatus)
    {
        if (row != null)
        {
            row.Set(upgradeName, upgradeStatus);
        }
    }

    private void SetRequirementRow(RequirementRow row, string objectText, string amountText, bool isComplete)
    {
        if (row == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(objectText))
        {
            row.Hide();
            return;
        }

        row.Set(objectText, amountText, isComplete);
    }

    private void SetActionSlot(ActionSlot slot, string hintText, bool visible)
    {
        if (slot != null)
        {
            slot.SetVisible(visible);
            slot.Set(hintText);
        }
    }

    private IEnumerator ShowPopupRoutine()
    {
        if (popupPanelRoot != null)
        {
            popupPanelRoot.SetActive(true);
        }

        yield return FadePopup(0f, 1f, popupFadeTime);
        yield return new WaitForSeconds(popupHoldTime);
        yield return FadePopup(1f, 0f, popupFadeTime);

        if (popupPanelRoot != null)
        {
            popupPanelRoot.SetActive(false);
        }

        popupRoutine = null;
    }

    private IEnumerator HidePopupAfterDelay()
    {
        yield return new WaitForSeconds(popupHoldTime);

        if (popupPanelRoot != null)
        {
            popupPanelRoot.SetActive(false);
        }

        popupRoutine = null;
    }

    private IEnumerator FadePopup(float from, float to, float duration)
    {
        if (popupCanvasGroup == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            popupCanvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            popupCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        popupCanvasGroup.alpha = to;
    }

    private void HidePopupImmediate()
    {
        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.alpha = 0f;
        }

        if (popupPanelRoot != null)
        {
            popupPanelRoot.SetActive(false);
        }
    }
}
