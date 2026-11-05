using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Каскад появления блоков деталей (1..9). Home захватывается ОДИН РАЗ -> обрыв волны
/// не уменьшает блоки накопительно. УХОД = чистый alpha 1->0 всех блоков (БЕЗ сжатия,
/// по ТЗ). ВХОД = волна scaleFrom->home + alpha. Play() на неактивной панели не падает:
/// ставит _pendingPlay, волну стартует OnEnable в кадр активации (фикс coroutine-inactive).
/// </summary>
public class DetailsCascadeRunner : MonoBehaviour, IPanelCascade
{
    [Header("Блоки каскада (перетащи строго в порядке 1..9)")]
    [SerializeField] private List<GameObject> blocks = new List<GameObject>();

    [Header("Тайминги появления")]
    [SerializeField] private float stagger = 0.05f;
    [SerializeField] private float blockDuration = 0.25f;
    [SerializeField, Range(0.5f, 1f)] private float scaleFrom = 0.92f;

    [Header("Уход (чистый alpha-фейд)")]
    [SerializeField] private float fadeOutDuration = 0.3f;

    private readonly List<RectTransform> rts = new List<RectTransform>();
    private readonly List<CanvasGroup> cgs = new List<CanvasGroup>();
    private readonly List<Vector3> hms = new List<Vector3>();
    private bool _homeCaptured;
    private bool _pendingPlay;          // Play() вызван на неактивной панели
    private Coroutine running;

    void OnEnable()
    {
        if (!_homeCaptured) CaptureHome();
        HideBlocks();
        if (_pendingPlay) { _pendingPlay = false; running = StartCoroutine(Run()); }
    }
    void OnDisable()
    {
        if (running != null) { StopCoroutine(running); running = null; }
    }

    public void Play()
    {
        if (!gameObject.activeInHierarchy) { _pendingPlay = true; return; }   // фикс coroutine-inactive
        if (running != null) StopCoroutine(running);
        if (!_homeCaptured) { CaptureHome(); HideBlocks(); }
        _pendingPlay = false;
        running = StartCoroutine(Run());
    }

    private void CaptureHome()
    {
        rts.Clear(); cgs.Clear(); hms.Clear();
        for (int i = 0; i < blocks.Count; i++)
        {
            var go = blocks[i]; if (go == null) continue;
            var rt = go.transform as RectTransform; if (rt == null) continue;
            var cg = rt.GetComponent<CanvasGroup>(); if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
            rts.Add(rt); cgs.Add(cg); hms.Add(rt.localScale);
        }
        _homeCaptured = true;
    }

    private void HideBlocks()
    {
        for (int i = 0; i < rts.Count; i++)
        {
            if (rts[i] == null || cgs[i] == null) continue;
            cgs[i].alpha = 0f;
            rts[i].localScale = hms[i] * scaleFrom;
        }
    }

    private IEnumerator Run()
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
        running = null;
    }

    // --- IPanelCascade ---
    public void PlayFadeOutAll(Action onDone)
    {
        if (!_homeCaptured) { CaptureHome(); HideBlocks(); }
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(FadeOutAll(onDone));
    }

    public void ForcePlayIn()
    {
        if (running != null) StopCoroutine(running);
        if (!_homeCaptured) CaptureHome();
        HideBlocks();
        _pendingPlay = false;
        running = StartCoroutine(Run());
    }

    private IEnumerator FadeOutAll(Action onDone)
    {
        var sa = new float[rts.Count];
        for (int i = 0; i < rts.Count; i++) sa[i] = cgs[i] != null ? cgs[i].alpha : 0f;
        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeOutDuration);
            float e = k < 0.5f ? 2f * k * k : 1f - Mathf.Pow(-2f * k + 2f, 2f) / 2f;   // ease-in-out
            for (int i = 0; i < rts.Count; i++)
            {
                if (rts[i] == null || cgs[i] == null) continue;
                cgs[i].alpha = Mathf.Lerp(sa[i], 0f, e);     // ТОЛЬКО alpha, scale не трогаем
            }
            yield return null;
        }
        for (int i = 0; i < rts.Count; i++)
        { if (rts[i] == null || cgs[i] == null) continue; cgs[i].alpha = 0f; }
        running = null;
        onDone?.Invoke();
    }
}