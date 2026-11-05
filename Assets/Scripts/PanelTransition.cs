using System.Collections;
using UnityEngine;

/// <summary>
/// Плавный показ/скрытие UI-панели. Навешивается ВРУЧНУЮ на корень панели
/// (HomeBoard, DetailsInfo, AddProjectModal2_0). CanvasGroup создаёт сам кодом.
///
/// SyncStateFromReality(): state выводится ИЗ РЕАЛЬНОСТИ (activeSelf + alpha) первой
/// строкой каждого публичного метода, поэтому рассинхрон state vs видимость невозможен
/// по построению - независимо от того, активна панель в редакторе или нет.
///
/// Два способа скрытия:
///  - HideWithAnimation() - плавный fade-out (для модалки, где под ней главный виден намеренно);
///  - HideInstant()       - мгновенное выключение (для УХОДЯЩЕГО экрана в стратегии
///                          hard-hide/soft-show: убирает и паузу, и белый просвет).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PanelTransition : MonoBehaviour
{
    [Header("Настройки перехода")]
    [SerializeField] private float duration = 0.28f;     // fade-in
    [SerializeField] private float hideDuration = 0.15f; // fade-out (для HideWithAnimation)
    [SerializeField] private bool useSlide = true;
    [SerializeField] private float slideOffsetY = 24f;
    [SerializeField, Range(0.5f, 1f)] private float scalePop = 0.97f;

    private RectTransform rt;
    private CanvasGroup cg;

    private Vector3 homeScale;
    private Vector2 homePos;
    private bool homeCaptured;

    private Coroutine running;
    private enum State { Hidden, Showing, Shown, Hiding }
    private State state = State.Hidden;

    // Ленивая инициализация ссылок - безопасна на неактивном GO (Awake там не вызывается).
    private void EnsureRefs()
    {
        if (rt == null) rt = (RectTransform)transform;
        if (cg == null)
        {
            cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        }
    }

    // Синхрон state с реальностью. Не трогаем, пока идёт корутина (running != null).
    private void SyncStateFromReality()
    {
        EnsureRefs();
        if (running != null) return;
        if (!gameObject.activeSelf) state = State.Hidden;
        else state = (cg.alpha > 0.5f) ? State.Shown : State.Hidden;
    }

    private void EnsureHome()
    {
        if (homeCaptured) return;
        homeScale = rt.localScale;
        homePos = rt.anchoredPosition;
        homeCaptured = true;
    }

    private Vector3 StartScale => homeScale * scalePop;
    private Vector2 StartPos => homePos + (useSlide ? new Vector2(0f, -slideOffsetY) : Vector2.zero);

    public void ShowWithAnimation(System.Action onComplete = null)
    {
        SyncStateFromReality();
        if (state == State.Shown) { onComplete?.Invoke(); return; }

        bool wasActive = gameObject.activeSelf;
        if (running != null) { StopCoroutine(running); running = null; }

        EnsureHome();
        gameObject.SetActive(true);

        if (!wasActive)
        {
            cg.alpha = 0f;
            rt.localScale = StartScale;
            rt.anchoredPosition = StartPos;
        }

        state = State.Showing;
        running = StartCoroutine(Fade(cg.alpha, 1f, rt.localScale, homeScale,
                                      rt.anchoredPosition, homePos, true, onComplete));
    }

    /// <summary>Плавное скрытие (fade-out). SetActive(false) - внутри, после анимации.</summary>
    public void HideWithAnimation(System.Action onHidden = null)
    {
        SyncStateFromReality();
        if (state == State.Hidden) { onHidden?.Invoke(); return; }
        if (!gameObject.activeSelf) { state = State.Hidden; onHidden?.Invoke(); return; }

        if (running != null) { StopCoroutine(running); running = null; }

        EnsureHome();
        state = State.Hiding;
        running = StartCoroutine(Fade(cg.alpha, 0f, rt.localScale, StartScale,
                                      rt.anchoredPosition, StartPos, false, onHidden));
    }

    /// <summary>
    /// МГНОВЕННОЕ скрытие без fade-out: выключает GO и приводит визуал в home, чтобы
    /// следующее Show стартовало из чистого состояния. Для УХОДЯЩЕГО экрана в стратегии
    /// hard-hide/soft-show - убирает паузу и белый просвет под входящим.
    /// </summary>
    public void HideInstant()
    {
        EnsureRefs();
        if (running != null) { StopCoroutine(running); running = null; }
        EnsureHome();
        cg.alpha = 1f;                 // home-визуал, чтобы при повторном Show не было скачка
        rt.localScale = homeScale;
        rt.anchoredPosition = homePos;
        state = State.Hidden;
        gameObject.SetActive(false);
    }

    private IEnumerator Fade(float fromA, float toA, Vector3 fromS, Vector3 toS,
                             Vector2 fromP, Vector2 toP, bool showing, System.Action done)
    {
        float t = 0f;
        float d = Mathf.Max(0.01f, showing ? duration : hideDuration);
        while (t < d)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / d);
            float e = 1f - (1f - k) * (1f - k); // ease-out quad

            cg.alpha = Mathf.Lerp(fromA, toA, e);
            rt.localScale = Vector3.Lerp(fromS, toS, e);
            rt.anchoredPosition = Vector2.Lerp(fromP, toP, e);
            yield return null;
        }

        cg.alpha = toA;
        rt.localScale = toS;
        rt.anchoredPosition = toP;
        running = null;

        if (showing) state = State.Shown;
        else { state = State.Hidden; gameObject.SetActive(false); }

        done?.Invoke();
    }
}