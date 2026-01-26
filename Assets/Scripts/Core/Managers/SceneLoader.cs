using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    [Header("References")]
    [SerializeField] private RectTransform fadePanel;
    [SerializeField] private CanvasGroup raycastBlocker;

    [Header("Settings")]
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private Ease moveEase = Ease.InOutQuart;

    private RectTransform canvasRect;
    private bool isTransitioning = false; // Используем отдельный флаг вместо raycastBlocker

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            canvasRect = GetComponent<RectTransform>();

            // Гарантируем, что при старте клики не заблокированы
            if (raycastBlocker != null) raycastBlocker.blocksRaycasts = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadScene(string sceneName)
    {
        // Если уже идет загрузка — игнорируем
        if (isTransitioning) return;

        StartCoroutine(LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        isTransitioning = true;
        if (raycastBlocker != null) raycastBlocker.blocksRaycasts = true;

        float height = canvasRect.rect.height + 1200f;

        // 1. Появление шторки сверху
        fadePanel.anchoredPosition = new Vector2(0, height);
        yield return fadePanel.DOAnchorPos(Vector2.zero, transitionDuration)
            .SetEase(moveEase)
            .SetUpdate(true)
            .WaitForCompletion();

        // ЗАПОМИНАЕМ СОСТОЯНИЕ
        SaveManager sm = Object.FindFirstObjectByType<SaveManager>();
        if (sm != null)
        {
            sm.data.MoneyAtLeave = sm.data.Money;
            sm.Save();
        }

        // Вместо KillAll лучше убивать всё, кроме самой шторки, 
        // но для простоты убедимся, что загрузка пойдет дальше
        DOTween.KillAll();

        // 2. Загрузка сцены
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone) yield return null;

        yield return new WaitForSecondsRealtime(0.3f);

        // 3. Уезд шторки вниз
        yield return fadePanel.DOAnchorPos(new Vector2(0, -height), transitionDuration)
            .SetEase(moveEase)
            .SetUpdate(true)
            .WaitForCompletion();

        // 4. Сброс
        fadePanel.anchoredPosition = new Vector2(0, height);
        if (raycastBlocker != null) raycastBlocker.blocksRaycasts = false;
        isTransitioning = false;
    }
}