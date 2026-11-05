using System.Collections;
using UnityEngine;

/// <summary>
/// Stagger-анимация появления одной карточки. Навешивается кодом на инстанс
/// из GridGenerator — настраивать префаб НЕ нужно.
/// Анимует только alpha (CanvasGroup) и localScale, поэтому безопасно
/// соседствует с любым Layout Group на родителе.
/// ВАЖНО: anchoredPosition — это Vector2, позиция хранится в Vector2,
/// scale — в Vector3. Не смешивать (иначе CS0034 на операторе +).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CardEntranceAnimator : MonoBehaviour
{
    private RectTransform rt;
    private CanvasGroup cg;
    private Vector3 baseScale;     // исходный scale префаба (к нему лерпим)
    private Vector3 startScale;    // baseScale * entranceScale (с чего начинаем)

    private float duration = 0.35f;
    private float entranceScale = 0.85f;
    private bool slideEnabled = false;
    private float slideOffsetY = 18f;

    public void Configure(float dur, float scaleMul, bool slide, float slideY)
    {
        duration = Mathf.Max(0.01f, dur);
        entranceScale = Mathf.Clamp(scaleMul, 0.1f, 1f);
        slideEnabled = slide;
        slideOffsetY = slideY;

        rt = (RectTransform)transform;
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        baseScale = rt.localScale;
        startScale = baseScale * entranceScale;
    }

    public void Play(float delay)
    {
        cg.alpha = 0f;
        rt.localScale = startScale;

        Vector2 startPos = rt.anchoredPosition;          // <-- Vector2, не Vector3
        if (slideEnabled)
            rt.anchoredPosition = startPos + new Vector2(0f, -slideOffsetY);

        StartCoroutine(Run(delay, startPos));
    }

    private IEnumerator Run(float delay, Vector2 homePos) // <-- Vector2, не Vector3
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float eased = 1f - (1f - k) * (1f - k);       // ease-out quad

            cg.alpha = eased;
            rt.localScale = Vector3.Lerp(startScale, baseScale, eased);
            if (slideEnabled)
                rt.anchoredPosition = Vector2.Lerp(homePos + new Vector2(0f, -slideOffsetY), homePos, eased);

            yield return null;
        }

        cg.alpha = 1f;
        rt.localScale = baseScale;
        if (slideEnabled) rt.anchoredPosition = homePos;
    }
}