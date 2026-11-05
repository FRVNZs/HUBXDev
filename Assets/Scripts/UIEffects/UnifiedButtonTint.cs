using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Единый ховер/пресс/дизейбл для кнопки "фон + текст одним целым".
/// selected/навигация СОЗНАТЕЛЬНО не красятся (см. историю чатов).
/// Вешать вместо ButtonEffect; у Button Transition = None.
/// </summary>
[DisallowMultipleComponent]
public class UnifiedButtonTint : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Цели (что красим синхронно)")]
    [Tooltip("true = собрать всех дочерних TMP_Text и Image (кроме фонов панелей) автоматически.")]
    [SerializeField] private bool autoCollect = true;
    [Tooltip("Ручной список целей (когда autoCollect = false). Перетащи фон и текст.")]
    [SerializeField] private List<Graphic> targets = new List<Graphic>();

    [Header("Цвета состояний (normal = исходный цвет цели, не задаётся)")]
    [SerializeField] private Color hoverColor = new Color(0.80f, 0.90f, 1.00f);
    [SerializeField] private Color pressColor = new Color(0.10f, 0.60f, 1.00f);
    [SerializeField] private Color disabledColor = new Color(0.50f, 0.50f, 0.50f, 0.50f);

    [Header("Hover lift (подпрыгивание наверх)")]
    [Tooltip("0 = выключено. Плюс = смещение вверх в юнитах anchoredPosition при наведении.")]
    [SerializeField] private float liftOnHover = 6f;

    [Header("Переход (ОДНО время для всех целей = синхронный пик)")]
    [SerializeField, Min(0.01f)] private float duration = 0.12f;
    [Tooltip("false = hold (держит pressColor пока зажато). true = резкий пик-вспышка при клике.")]
    [SerializeField] private bool pulseOnPress = false;
    [SerializeField, Min(0.01f)] private float pulseDuration = 0.12f;
    [Tooltip("Сохранять исходную альфу цели (не ломать полупрозрачные иконки/тени).")]
    [SerializeField] private bool preserveAlpha = true;

    private static readonly string[] StopWords = { "background", "bg", "mask", "panel" };

    private Button btn;
    private readonly List<Color> cur = new List<Color>();
    private readonly List<Color> orig = new List<Color>();
    private readonly List<float> origA = new List<float>();

    private bool hovered, pressed;
    private Vector2 homePos;
    private bool homeReady;                 // <<< ИЗМЕНЕНО: флаг "поза захвачена валидно"
    private float pulseTimer;

    void Awake()
    {
        btn = GetComponent<Button>();
        // <<< ИЗМЕНЕНО: УБРАЛИ строку homePos = ... из Awake.
        // Здесь layout ещё не расставил детей, поэтому homePos был бы мусором (0,0) -> угол.
        BuildTargets();
        ResetImmediate();
    }

    void OnEnable() { hovered = pressed = false; pulseTimer = 0f; ResetImmediate(); }
    void OnDisable() { hovered = pressed = false; pulseTimer = 0f; ResetImmediate(); }

    void OnApplicationFocus(bool has) { if (!has) { hovered = pressed = false; pulseTimer = 0f; } }
    void OnApplicationPause(bool p) { if (p) { hovered = pressed = false; pulseTimer = 0f; } }

    void Update()
    {
        if (targets.Count == 0) return;

        bool interactable = btn == null || btn.interactable;
        if (pulseTimer > 0f) pulseTimer -= Time.unscaledDeltaTime;

        Color state; bool isNormal;
        if (!interactable) { state = disabledColor; isNormal = false; }
        else if (pulseTimer > 0f) { state = pressColor; isNormal = false; }
        else if (pressed) { state = pressColor; isNormal = false; }
        else if (hovered) { state = hoverColor; isNormal = false; }
        else { state = default; isNormal = true; }

        float tc = Mathf.Max(0.0001f, duration / 3f);
        float f = 1f - Mathf.Exp(-Time.unscaledDeltaTime / tc);

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null) continue;
            Color goal = isNormal ? orig[i] : state;
            cur[i] = Color.Lerp(cur[i], goal, f);
            float a = preserveAlpha ? origA[i] : cur[i].a;
            targets[i].color = new Color(cur[i].r, cur[i].g, cur[i].b, a);
        }

        // <<< ИЗМЕНЕНО: весь блок lift переписан.
        // Суть: в ПОКОЕ (hovered=false и уже долетели) в anchoredPosition НЕ пишем вообще ->
        // VerticalLayoutGroup сам держит ряд, кнопки не съезжают. Пишем только при наведении
        // и только пока летим назад после выхода курсора.
        if (liftOnHover != 0f)
        {
            var rt = (RectTransform)transform;
            if (hovered)
            {
                rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, homePos + new Vector2(0f, liftOnHover), f);
            }
            else if (homeReady && Vector2.SqrMagnitude(rt.anchoredPosition - homePos) > 0.01f)
            {
                rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, homePos, f);
            }
        }
    }

    // --- события указателя (мышь/тач) ---    
    public void OnPointerEnter(PointerEventData e)
    {
        var rt = (RectTransform)transform;
        // Обновляем базу покоя ТОЛЬКО если уже долетели до неё (или ещё не захватывали).
        // Иначе при быстром повторном наведении мы бы запомнили сдвинутую позу -> залипание.
        if (!homeReady || Vector2.SqrMagnitude(rt.anchoredPosition - homePos) < 0.5f)
            homePos = rt.anchoredPosition;
        homeReady = true;
        hovered = true;
    }
    public void OnPointerExit(PointerEventData e) => hovered = false;
    public void OnPointerDown(PointerEventData e) => pressed = true;
    public void OnPointerUp(PointerEventData e) => pressed = false;

    public void OnPointerClick(PointerEventData e)
    {
        if (!pulseOnPress) return;
        for (int i = 0; i < cur.Count; i++) cur[i] = pressColor;
        pulseTimer = pulseDuration;
    }

    public void ForceReleasePointer()
    {
        hovered = pressed = false;
        pulseTimer = 0f;
        ResetImmediate();
    }

    // --- сбор целей ---
    private void BuildTargets()
    {
        if (autoCollect)
        {
            targets.Clear();
            foreach (var t in GetComponentsInChildren<TMP_Text>(true)) targets.Add(t);
            foreach (var im in GetComponentsInChildren<Image>(true))
            {
                if (im.gameObject == gameObject) continue;
                string n = im.name.ToLowerInvariant();
                bool skip = false;
                for (int s = 0; s < StopWords.Length; s++) if (n.Contains(StopWords[s])) { skip = true; break; }
                if (!skip) targets.Add(im);
            }
        }
        else targets.RemoveAll(x => x == null);

        cur.Clear(); orig.Clear(); origA.Clear();
        for (int i = 0; i < targets.Count; i++)
        {
            Color c = targets[i].color;
            orig.Add(c); cur.Add(c); origA.Add(c.a);
        }
    }

    private void ResetImmediate()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null) continue;
            cur[i] = orig[i];
            float a = preserveAlpha ? origA[i] : orig[i].a;
            targets[i].color = new Color(orig[i].r, orig[i].g, orig[i].b, a);
        }
        // <<< ИЗМЕНЕНО: добавлено && homeReady. До первого наведения поза невалидна -> не пишем.
        if (liftOnHover != 0f && homeReady) ((RectTransform)transform).anchoredPosition = homePos;
    }
}