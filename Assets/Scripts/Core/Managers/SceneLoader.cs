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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Берем RectTransform всего канваса для точного расчета высоты
            canvasRect = GetComponent<RectTransform>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadScene(string sceneName)
    {
        // Предотвращаем двойной клик, если сцена уже грузится
        if (raycastBlocker.blocksRaycasts) return;

        StartCoroutine(LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        raycastBlocker.blocksRaycasts = true;

        // Получаем актуальную высоту канваса (с учетом масштабирования)
        float height = canvasRect.rect.height + 100f;

        // 1. Выезжаем плашкой СВЕРХУ в ЦЕНТР
        fadePanel.anchoredPosition = new Vector2(0, height);
        yield return fadePanel.DOAnchorPos(Vector2.zero, transitionDuration)
            .SetEase(moveEase)
            .SetUpdate(true) // Чтобы работало при паузе
            .WaitForCompletion();

        // 2. ЗАПОМИНАЕМ СОСТОЯНИЕ ПЕРЕД УХОДОМ
        SaveManager sm = Object.FindFirstObjectByType<SaveManager>();
        if (sm != null)
        {
            sm.data.MoneyAtLeave = sm.data.Money;
            sm.Save();
        }

        // Очистка перед новой сценой
        DOTween.KillAll();

        // 3. ЗАГРУЗКА СЦЕНЫ
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone) yield return null;

        // Небольшая задержка, чтобы сцена успела "проснуться"
        yield return new WaitForSecondsRealtime(0.2f);

        // 4. Уезжаем из ЦЕНТРА ВНИЗ
        yield return fadePanel.DOAnchorPos(new Vector2(0, -height), transitionDuration)
            .SetEase(moveEase)
            .SetUpdate(true) // ВАЖНО: добавить и сюда
            .WaitForCompletion();

        // 5. Сброс позиции ВВЕРХ (мгновенно) для следующего раза
        fadePanel.anchoredPosition = new Vector2(0, height);
        raycastBlocker.blocksRaycasts = false;
    }
}