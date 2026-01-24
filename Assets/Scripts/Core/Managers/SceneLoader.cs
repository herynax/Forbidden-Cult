using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    [Header("References")]
    [SerializeField] private RectTransform fadePanel; // Картинка на весь экран
    [SerializeField] private CanvasGroup raycastBlocker;

    [Header("Settings")]
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private Ease moveEase = Ease.InOutQuart;
    [SerializeField] private float screenOfset = 300f;

    private float screenHeight;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            screenHeight = Screen.height + screenOfset; // Берем высоту с запасом
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        raycastBlocker.blocksRaycasts = true;

        // 1. Позиционируем над экраном и выезжаем в центр
        fadePanel.anchoredPosition = new Vector2(0, screenHeight);
        yield return fadePanel.DOAnchorPos(Vector2.zero, transitionDuration).SetEase(moveEase).WaitForCompletion();

        // 2. Загружаем сцену
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone) yield return null;

        yield return new WaitForSeconds(0.2f); // Небольшая пауза для стабильности

        // 3. Уезжаем вниз
        yield return fadePanel.DOAnchorPos(new Vector2(0, -screenHeight), transitionDuration).SetEase(moveEase).WaitForCompletion();

        // 4. Сброс позиции вверх (мгновенно) для следующего раза
        fadePanel.anchoredPosition = new Vector2(0, screenHeight);
        raycastBlocker.blocksRaycasts = false;
    }
}