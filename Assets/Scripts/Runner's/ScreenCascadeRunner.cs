using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Каскад главного/избранного: статика + карточки. Инициация ТОЛЬКО OnCardsGenerated
/// (OnEnable нет намеренно). Статика home захватывается ОДИН РАЗ; карточки свежие каждый раз.
/// УХОД = чистый alpha 1->0 статики и карточек (БЕЗ сжатия, по ТЗ). ВХОД = волна scaleFrom->home.
/// </summary>
[DisallowMultipleComponent]
public class ScreenCascadeRunner : MonoBehaviour, IPanelCascade
{
    [Header("Статичные блоки экрана (порядок = порядок появления)")]
    [Tooltip("САМ корень сюда НЕ кладём. Content/Scroll View/Viewport НЕ кладём — вложенная альфа.")]
    [SerializeField] private List<GameObject> staticBlocks = new List<GameObject>();

    [Header("Контейнер динамических карточек (его дети = хвост каскада)")]
    [SerializeField] private RectTransform cardsContainer;

    [Header("Тайминги появления")]
    [SerializeField] private float stagger = 0.05f;
    [SerializeField] private float blockDuration = 0.25f;
    [SerializeField, Range(0.5f, 1f)] private float scaleFrom = 0.92f;

    [Header("Уход (чистый alpha-фейд)")]
    [SerializeField] private float fadeOutDuration = 0.3f;

    private readonly List<RectTransform> sRt = new List<RectTransform>();
    private readonly List<CanvasGroup> sCg = new List<CanvasGroup>();
    private readonly List<Vector3> sHm = new List<Vector3>();
    private bool _staticHomeCaptured;

    private readonly List<RectTransform> cRt = new List<RectTransform>();
    private readonly List<CanvasGroup> cCg = new List<CanvasGroup>();
    private readonly List<Vector3> cHm = new List<Vector3>();

    private bool _staticPlayed;
    private Coroutine running;

    void OnDisable()
    {
        if (running != null) { StopCoroutine(running); running = null; }
        _staticPlayed = false;
    }

    public void OnCardsGenerated()
    {
        if (!_staticHomeCaptured) CaptureStaticHome();
        CollectCards();
        if (!_staticPlayed) { _staticPlayed = true; HideStatics(); PlayAll(); }
        else PlayCards();
    }

    private void CaptureStaticHome()
    {
        sRt.Clear(); sCg.Clear(); sHm.Clear();
        for (int i = 0; i < staticBlocks.Count; i++)
        {
            var go = staticBlocks[i]; if (go == null) continue;
            var rt = go.transform as RectTransform; if (rt == null) continue;
            var cg = rt.GetComponent<CanvasGroup>(); if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
            sRt.Add(rt); sCg.Add(cg); sHm.Add(rt.localScale);
        }
        _staticHomeCaptured = true;
    }

    private void HideStatics()
    {
        for (int i = 0; i < sRt.Count; i++)
        {
            if (sRt[i] == null || sCg[i] == null) continue;
            sCg[i].alpha = 0f;
            sRt[i].localScale = sHm[i] * scaleFrom;
        }
    }

    private void CollectCards()
    {
        cRt.Clear(); cCg.Clear(); cHm.Clear();
        if (cardsContainer == null) return;
        for (int i = 0; i < cardsContainer.childCount; i++)
        {
            var rt = cardsContainer.GetChild(i) as RectTransform; if (rt == null) continue;
            var cg = rt.GetComponent<CanvasGroup>(); if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
            Vector3 hm = rt.localScale;
            cRt.Add(rt); cCg.Add(cg); cHm.Add(hm);
            cg.alpha = 0f; rt.localScale = hm * scaleFrom;
        }
    }

    private void PlayAll() { if (running != null) StopCoroutine(running); running = StartCoroutine(PlayAllRoutine()); }
    private void PlayCards() { if (running != null) StopCoroutine(running); running = StartCoroutine(PlayCardsRoutine()); }
    private IEnumerator PlayAllRoutine() { yield return RunList(sRt, sCg, sHm); yield return RunList(cRt, cCg, cHm); running = null; }
    private IEnumerator PlayCardsRoutine() { yield return RunList(cRt, cCg, cHm); running = null; }

    private IEnumerator RunList(List<RectTransform> rts, List<CanvasGroup> cgs, List<Vector3> hms)
    {
        for (int i = 0; i < rts.Count; i++)
        {
            var rt = rts[i]; var cg = cgs[i]; var hm = hms[i];
            if (rt == null || cg == null) continue;
            Vector3 from = hm * scaleFrom;
            float t = 0f;
            while (t < blockDuration && cg != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / blockDuration);
                float e = 1f - (1f - k) * (1f - k);
                cg.alpha = e;
                rt.localScale = Vector3.Lerp(from, hm, e);
                yield return null;
            }
            if (cg == null) continue;
            cg.alpha = 1f; rt.localScale = hm;
            yield return new WaitForSecondsRealtime(stagger);
        }
    }

    // --- IPanelCascade ---
    public void PlayFadeOutAll(Action onDone)
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(FadeOutAll(onDone));
    }

    public void ForcePlayIn()
    {
        if (running != null) StopCoroutine(running);
        HideStatics();
        for (int i = 0; i < cRt.Count; i++)
        { if (cRt[i] == null || cCg[i] == null) continue; cCg[i].alpha = 0f; cRt[i].localScale = cHm[i] * scaleFrom; }
        running = StartCoroutine(PlayAllRoutine());
    }

    private IEnumerator FadeOutAll(Action onDone)
    {
        var saS = Snap(sCg);
        var saC = Snap(cCg);
        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeOutDuration);
            float e = k < 0.5f ? 2f * k * k : 1f - Mathf.Pow(-2f * k + 2f, 2f) / 2f;   // ease-in-out
            Step(sRt, sCg, saS, e);
            Step(cRt, cCg, saC, e);
            yield return null;
        }
        Finish(sRt, sCg); Finish(cRt, cCg);
        running = null;
        onDone?.Invoke();
    }

    private float[] Snap(List<CanvasGroup> cgs)
    { var a = new float[cgs.Count]; for (int i = 0; i < cgs.Count; i++) a[i] = cgs[i] != null ? cgs[i].alpha : 0f; return a; }
    private void Step(List<RectTransform> rts, List<CanvasGroup> cgs, float[] sa, float e)
    { for (int i = 0; i < rts.Count; i++) { if (rts[i] == null || cgs[i] == null) continue; cgs[i].alpha = Mathf.Lerp(sa[i], 0f, e); } }   // ТОЛЬКО alpha
    private void Finish(List<RectTransform> rts, List<CanvasGroup> cgs)
    { for (int i = 0; i < rts.Count; i++) { if (rts[i] == null || cgs[i] == null) continue; cgs[i].alpha = 0f; } }
}