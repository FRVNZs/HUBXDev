using UnityEngine;
using TMPro;
using System.Collections;

public class ClipboardCopier : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Текст самой ссылки")]
    [SerializeField] private TextMeshProUGUI urlTextField;

    [Tooltip("Объект с текстом уведомления 'Скопировано'")]
    [SerializeField] private TextMeshProUGUI feedbackLabel;

    [Header("Настройки Анимации Ссылки")]
    [Tooltip("Цвет, которым подсветится ссылка при копировании")]
    [SerializeField] private Color linkFlashColor = new Color(0.2f, 1f, 0.4f); // Ярко-зеленый

    [Tooltip("Длительность паузы перед возвратом цвета")]
    [SerializeField] private float flashHoldDuration = 0.15f;

    [Tooltip("Длительность плавного возврата к исходному цвету")]
    [SerializeField] private float flashFadeOutDuration = 0.4f;

    [Header("Тексты")]
    [SerializeField] private string successMessage = "✓ Скопировано!";

    // Хранилище данных
    private string fullUrl = "";

    // Запоминаем оригинальные цвета при старте
    private Color originalLinkColor;
    private Color originalFeedbackColor;

    void Start()
    {
        // Автоматический поиск компонентов
        if (urlTextField == null)
            urlTextField = GetComponentInChildren<TextMeshProUGUI>();

        // Сохраняем ссылку
        if (urlTextField != null && !string.IsNullOrEmpty(urlTextField.text))
        {
            fullUrl = urlTextField.text;

            // ЗАПОМИНАЕМ ЦВЕТ ССЫЛКИ ПО УМОЛЧАНИЮ
            originalLinkColor = urlTextField.color;
        }

        // ЗАПОМИНАЕМ ЦВЕТ УВЕДОМЛЕНИЯ ПО УМОЛЧАНИЮ
        if (feedbackLabel != null)
        {
            originalFeedbackColor = feedbackLabel.color;
        }
    }

    public void CopyToClipboard()
    {
        if (urlTextField != null)
        {
            fullUrl = urlTextField.text;
        }

        if (string.IsNullOrEmpty(fullUrl)) return;

        GUIUtility.systemCopyBuffer = fullUrl;

        // Останавливаем старые анимации, чтобы не было каши
        StopAllCoroutines();

        // Запускаем две независимые анимации параллельно
        StartCoroutine(AnimateLinkFlash());
        StartCoroutine(AnimateFeedbackToast());
    }

    // --- 1. Анимация моргания ССЫЛКИ ---
    private IEnumerator AnimateLinkFlash()
    {
        if (urlTextField == null) yield break;

        // Мгновенно красим в цвет вспышки
        urlTextField.color = linkFlashColor;

        // Ждем долю секунды
        yield return new WaitForSeconds(flashHoldDuration);

        // Плавно возвращаем к ЗАПОМНЕННОМУ оригинальному цвету
        float timer = 0f;
        while (timer < flashFadeOutDuration)
        {
            timer += Time.deltaTime;
            float t = timer / flashFadeOutDuration;

            // Интерполяция от цвета вспышки обратно к оригиналу
            urlTextField.color = Color.Lerp(linkFlashColor, originalLinkColor, t);

            yield return null;
        }

        // Гарантируем точное восстановление
        urlTextField.color = originalLinkColor;
    }

    // --- 2. Анимация появления УВЕДОМЛЕНИЯ ("Скопировано") ---
    private IEnumerator AnimateFeedbackToast()
    {
        if (feedbackLabel == null) yield break;

        var canvasGroup = feedbackLabel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("У Feedback Label нет Canvas Group!");
            yield break;
        }

        // Меняем только текст и прозрачность. Цвет НЕ трогаем!
        feedbackLabel.text = successMessage;

        // Убеждаемся, что цвет тот самый, что был задан в редакторе изначально
        // (на случай, если его кто-то сбросил во время игры)
        feedbackLabel.color = originalFeedbackColor;

        canvasGroup.alpha = 0f; // Прячем

        // Фаза 1: Появление (Fade In)
        float fadeInSpeed = 5f;
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += Time.deltaTime * fadeInSpeed;
            if (canvasGroup.alpha > 1f) canvasGroup.alpha = 1f;
            yield return null;
        }

        // Фаза 2: Пауза (Hold)
        yield return new WaitForSeconds(1.0f);

        // Фаза 3: Исчезновение (Fade Out)
        float fadeOutSpeed = 3f;
        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.deltaTime * fadeOutSpeed;
            if (canvasGroup.alpha < 0f) canvasGroup.alpha = 0f;
            yield return null;
        }
    }
}