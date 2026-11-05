using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Каскад окна добавления проекта. Show = волна 0->13 (OnEnable).
/// Hide = каскадный реверс 13->0 (PlayReverse) с ОТДЕЛЬНЫМИ таймингами, чтобы уход
/// читался волной даже при быстром входе. В конце onDone дёргает PanelTransition.HideInstant
/// (корень гасится ПОСЛЕ схлопывания блоков, без второго fade и без моргания).
/// Home-масштаб захватывается ОДИН РАЗ -> обрыв волны не уменьшает блоки накопительно.
/// </summary>
[DisallowMultipleComponent]
public class ModalCascadeRunner : MonoBehaviour
{
    [Header("Блоки окна (порядок = порядок появления)")]
    [Tooltip("САМ корень модалки сюда НЕ кладём.")]
    [SerializeField] private List<GameObject> blocks = new List<GameObject>();

    [Header("Тайминги появления")]
    [SerializeField] private float stagger = 0.05f;
    [SerializeField] private float blockDuration = 0.25f;
    [SerializeField, Range(0.5f, 1f)] private float scaleFrom = 0.92f;

    [Header("Тайминги ухода (отдельные, чтобы реверс был виден при быстром входе)")]
    [SerializeField] private float reverseBlockDuration = 0.08f;
    [SerializeField] private float reverseStagger = 0.02f;
    [SerializeField, Range(0.5f, 1f)] private float reverseScaleTo = 0.85f;

    private readonly List<RectTransform> rts = new List<RectTransform>();
    private readonly List<CanvasGroup> cgs = new List<CanvasGroup>();
    private readonly List<Vector3> hms = new List<Vector3>();
    private bool _homeCaptured;

    private Coroutine running;
    private bool _reversing;            // идёт обратная волна (защита от двойного HidePanel)

    void OnEnable()
    {
        _reversing = false;             // повторное открытие отменяет незавершённый реверс
        if (!_homeCaptured) CaptureHome();
        HideBlocks();
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Run());
    }

    void OnDisable()
    {
        if (running != null) { StopCoroutine(running); running = null; }
        _reversing = false;
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

    /// <summary>Обратная волна 13->0. onDone = финальное скрытие корня (HideInstant формы).</summary>
    public void PlayReverse(Action onDone)
    {
        if (_reversing) return;         // двойной клик «Отмена»: onDone уже запланирован первым
        _reversing = true;
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(RunReverse(onDone));
    }

    private IEnumerator RunReverse(Action onDone)
    {
        for (int i = rts.Count - 1; i >= 0; i--)
        {
            var rt = rts[i]; var cg = cgs[i]; var hm = hms[i];
            if (rt == null || cg == null) continue;
            float startA = cg.alpha;    // из текущего -> устойчиво к обрыву прямой волны
            Vector3 startS = rt.localScale;
            Vector3 toS = hm * reverseScaleTo;
            float t = 0f;
            while (t < reverseBlockDuration && cg != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / reverseBlockDuration);
                float e = k < 0.5f ? 2f * k * k : 1f - Mathf.Pow(-2f * k + 2f, 2f) / 2f;  // ease-in-out
                cg.alpha = Mathf.Lerp(startA, 0f, e);
                rt.localScale = Vector3.Lerp(startS, toS, e);
                yield return null;
            }
            if (cg == null) continue;
            cg.alpha = 0f; rt.localScale = toS;
            yield return new WaitForSecondsRealtime(reverseStagger);
        }
        _reversing = false; running = null;
        onDone?.Invoke();
    }
}