using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Цвета")]
    [Tooltip("Цвет при наведении мыши (Hover)")]
    [SerializeField] private Color hoverColor = new Color(0.8f, 0.9f, 1f);

    [Tooltip("Цвет при клике (Flash)")]
    [SerializeField] private Color clickFlashColor = new Color(0.2f, 1f, 0.4f);

    [Header("Настройки анимации")]
    [Tooltip("Скорость возврата к оригинальному цвету после Hover")]
    [SerializeField] private float returnSpeed = 5f;

    [Tooltip("Длительность вспышки при клике")]
    [SerializeField] private float flashDuration = 0.2f;

    private List<TextMeshProUGUI> texts = new List<TextMeshProUGUI>();
    private List<Image> images = new List<Image>();
    private Dictionary<Component, Color> originalColors = new Dictionary<Component, Color>();

    private Coroutine currentCoroutine;
    private bool isHovering = false;

    // Анти-спам: warning о недоступной legacy-системе печатаем один раз на объект,
    // а не при каждом событийном вызове SyncWithMousePosition из DetailsPanelController.
    private bool _legacyWarned;

    void Awake()
    {
        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in tmps) { texts.Add(t); originalColors[t] = t.color; }

        Image[] imgs = GetComponentsInChildren<Image>(true);
        foreach (var i in imgs)
        {
            if (i.gameObject == gameObject) continue;
            string lowerName = i.name.ToLower();
            if (lowerName.Contains("background") || lowerName.Contains("bg") ||
                lowerName.Contains("mask") || lowerName.Contains("panel")) continue;
            images.Add(i); originalColors[i] = i.color;
        }
    }

    void OnDisable() { ForceReset(); }
    void OnDestroy() { ForceReset(); }

    // --- HOVER ---
    public void OnPointerEnter(PointerEventData eventData) { isHovering = true; ApplyColor(hoverColor); }
    public void OnPointerExit(PointerEventData eventData) { isHovering = false; ResetToOriginal(); }

    // --- CLICK ---
    public void OnPointerClick(PointerEventData eventData) { TriggerClickFlash(); }

    private void ApplyColor(Color targetColor)
    {
        foreach (var txt in texts) txt.color = targetColor;
        foreach (var img in images) img.color = targetColor;
    }

    // force=true обходит защиту по currentCoroutine - нужен финалу вспышки,
    // иначе он сам себя блокирует (корень залипания). Обычный Exit без force
    // во время вспышки корректно НЕ трогает цвет до её конца.
    private void ResetToOriginal(bool force = false)
    {
        if (!force && currentCoroutine != null) return;
        foreach (var txt in texts) txt.color = originalColors[txt];
        foreach (var img in images) img.color = originalColors[img];
    }

    private void ForceReset()
    {
        isHovering = false;
        if (currentCoroutine != null) { StopCoroutine(currentCoroutine); currentCoroutine = null; }
        foreach (var txt in texts) if (txt != null) txt.color = originalColors[txt];
        foreach (var img in images) if (img != null) img.color = originalColors[img];
    }

    private void TriggerClickFlash()
    {
        if (!gameObject.activeInHierarchy || !enabled) return;
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        ApplyColor(clickFlashColor);
        yield return new WaitForSeconds(flashDuration);
        if (isHovering) ApplyColor(hoverColor);
        else ResetToOriginal(force: true);   // <-- фикс mutual-lock: без force залипал бы цвет
        currentCoroutine = null;
    }

    // ПУБЛИЧНЫЙ МЕТОД, КОТОРЫЙ ЗОВЁТСЯ ИЗ DetailsPanelController (строки 13/30).
    // Сигнатура и семантика совпадают с оригиналом -> потребитель компилируется без правок.
    // Вызывается событийно (show/hide панели), поэтому "сброс при исключении legacy-ввода"
    // здесь семантически верен: панель сменила состояние -> ховер кнопки сбрасываем.
    public void SyncWithMousePosition()
    {
        if (!gameObject.activeInHierarchy || !enabled) return;
        var es = EventSystem.current;
        if (es == null) return;

        bool isHoveringNow = false;
        try
        {
            Vector2 mousePos = Input.mousePosition; // legacy API; в New-only бросит -> catch ниже

            PointerEventData pointerData = new PointerEventData(es);
            pointerData.position = mousePos;

            List<RaycastResult> results = new List<RaycastResult>();
            es.RaycastAll(pointerData, results);

            for (int r = 0; r < results.Count; r++)
            {
                GameObject hit = results[r].gameObject;
                if (hit == gameObject || hit.transform.IsChildOf(transform)) { isHoveringNow = true; break; }
                if (!hit.transform.IsChildOf(transform) && hit != gameObject) { isHoveringNow = false; break; }
            }
        }
        catch (System.Exception)
        {
            // Legacy-ввод недоступен (включён только New Input System). Для событийного
            // вызова это равносильно "курсора на кнопке нет" -> принудительный сброс.
            if (!_legacyWarned) { Debug.LogWarning("Legacy-ввод недоступен, SyncWithMousePosition работает как событийный сброс ховера."); _legacyWarned = true; }
            isHoveringNow = false;
        }

        if (isHoveringNow) { ApplyColor(hoverColor); isHovering = true; }
        else { ResetToOriginal(); isHovering = false; }
    }
}