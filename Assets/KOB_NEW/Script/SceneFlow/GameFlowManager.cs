using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }
    public static Season SelectedSeason { get; private set; } = Season.Spring;

    [Header("Scenes")]
    public string mainMenuScene = "MainMenu";
    public string springScene = "SpringScene";
    public string summerScene = "SummerScene";
    public string autumnScene = "AutumnScene";
    public string winterScene = "WinterScene";
    public string victoryScene = "VictoryScene";
    public string defeatScene = "DefeatScene";

    [Header("Transition")]
    public float fadeDuration = 0.8f;
    public float seasonRevealDuration = 1.2f;

    private Canvas transitionCanvas;
    private Image fadeImage;
    private Text seasonText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartNewGame()
    {
        SelectedSeason = RollRandomSeason();
        string targetScene = GetSceneNameForSeason(SelectedSeason);
        StartCoroutine(ShowSeasonRevealThenLoad(targetScene));
    }
    public void StartSpringScene()
    {
        SelectedSeason = Season.Spring;
        string targetScene = GetSceneNameForSeason(SelectedSeason);
        StartCoroutine(ShowSeasonRevealThenLoad(targetScene));
    }

    public void PlaySeason(Season season)
    {
        SelectedSeason = season;
        StartCoroutine(ShowSeasonRevealThenLoad(GetSceneNameForSeason(season)));
    }

    public Season RollRandomSeason()
    {
        Array seasons = Enum.GetValues(typeof(Season));
        return (Season)seasons.GetValue(UnityEngine.Random.Range(0, seasons.Length));
    }

    private string GetSceneNameForSeason(Season season)
    {
        switch (season)
        {
            case Season.Spring: return springScene;
            case Season.Summer: return summerScene;
            case Season.Autumn: return autumnScene;
            case Season.Winter: return winterScene;
            default: return springScene;
        }
    }

    private IEnumerator ShowSeasonRevealThenLoad(string targetScene)
    {
        BuildTransitionCanvas();

        if (seasonText != null)
        {
            seasonText.text = $"ฤดูที่สุ่มได้: {SelectedSeason}";
            seasonText.enabled = true;
        }

        yield return StartCoroutine(FadeToBlack(fadeDuration));
        yield return new WaitForSeconds(seasonRevealDuration);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(FadeFromBlack(fadeDuration));
    }

    private void BuildTransitionCanvas()
    {
        if (transitionCanvas != null) return;

        GameObject canvasObject = new GameObject("SceneTransitionCanvas");
        transitionCanvas = canvasObject.AddComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = 9999;

        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject imageObject = new GameObject("FadeImage");
        imageObject.transform.SetParent(canvasObject.transform, false);
        fadeImage = imageObject.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);

        RectTransform rect = fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        GameObject textObject = new GameObject("SeasonText");
        textObject.transform.SetParent(canvasObject.transform, false);
        seasonText = textObject.AddComponent<Text>();

        try
        {
            seasonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch
        {
            seasonText.font = Font.CreateDynamicFontFromOSFont("Arial", 42);
        }

        seasonText.text = "";
        seasonText.alignment = TextAnchor.MiddleCenter;
        seasonText.fontSize = 42;
        seasonText.color = Color.white;
        seasonText.enabled = false;

        RectTransform textRect = seasonText.rectTransform;
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(700f, 120f);
        textRect.anchoredPosition = new Vector2(0f, 30f);
    }

    private IEnumerator FadeToBlack(float duration)
    {
        if (fadeImage == null) yield break;

        float elapsed = 0f;
        Color color = fadeImage.color;
        color.a = 0f;
        fadeImage.color = color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsed / duration);
            fadeImage.color = color;
            yield return null;
        }
    }

    private IEnumerator FadeFromBlack(float duration)
    {
        if (fadeImage == null) yield break;

        float elapsed = 0f;
        Color color = fadeImage.color;
        color.a = 1f;
        fadeImage.color = color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = 1f - Mathf.Clamp01(elapsed / duration);
            fadeImage.color = color;
            yield return null;
        }

        fadeImage.color = new Color(0f, 0f, 0f, 0f);
    }

    [ContextMenu("trigger victory")]
    public void TriggerVictory()
    {
        StartCoroutine(LoadSceneWithFade(victoryScene));
    }

    [ContextMenu("trigger defeat")]
    public void TriggerDefeat()
    {
        StartCoroutine(LoadSceneWithFade(defeatScene));
    }

    public void BackToMainMenu()
    {
        StartCoroutine(LoadSceneWithFade(mainMenuScene));
    }

    public void RetryGame()
    {
        SelectedSeason = RollRandomSeason();
        StartCoroutine(ShowSeasonRevealThenLoad(GetSceneNameForSeason(SelectedSeason)));
    }

    private IEnumerator LoadSceneWithFade(string targetScene)
    {
        BuildTransitionCanvas();
        yield return StartCoroutine(FadeToBlack(fadeDuration));

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(FadeFromBlack(fadeDuration));
    }
}
