using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Ховер + клик-вспышка карточки ЧЕРЕЗ ОТДЕЛЬНЫЙ оверлей-слой (альфа-микс),
/// а не через mul-тонировку cover-Image. Вешать на корень ProjectCardPrefab
/// (тот объект, с которого сняли ButtonEffect). События указателя приходят
/// к корню от дочернего raycast-объекта (ClickZone/Card), поэтому
/// raycastTarget у самого оверлея выключен намеренно.
/// НЕ трогает CardClickHandler (открытие деталей) и НЕ трогает ButtonEffect
/// на других кнопках.
/// </summary>
[DisallowMultipleComponent]
public class CardHoverOverlay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Куда вставлять оверлей")]
    [Tooltip("Родитель оверлея. Идеал = объект с Mask (MaskCard): маска скруглит оверлей, спрайт можно оставить None. Пусто = self.")]
    [SerializeField] private Transform overlayParent;
    [Tooltip("Перед каким объектом вставить, чтобы текст имени остался ПОВЕРХ дымки. Пусто = в конец.")]
    [SerializeField] private GameObject insertBefore;

    [Header("Ховер (примешивание)")]
    [Tooltip("Спрайт оверлея. Внутри Mask хватит None (скруглит маска). Вне Mask — перетащи скруглённый сплошной.")]
    [SerializeField] private Sprite overlaySprite;
    [Tooltip("RGB подсветки ховера. Альфа этого цвета игнорится — силу задаёт maxAlpha.")]
    [SerializeField] private Color hoverColor = Color.white;
    [Range(0f, 1f)][SerializeField] private float maxAlpha = 0.12f;   // сила подмеса (0.08..0.18)
    [SerializeField] private float fadeSpeed = 8f;                     // плавность нарастания/спадания
    [Tooltip("true = оверлей режется родительской Mask (скругление без спрайта).")]
    [SerializeField] private bool useParentMask = true;

    [Tooltip("Скругление углов оверлея, если Use Parent Mask = false (иначе маска делает это сама).")]
    [SerializeField] private Sprite cornerSprite;

    [Header("Клик-вспышка (заменяет flash в ButtonEffect для карточки)")]
    [SerializeField] private Color flashColor = new Color(0.2f, 1f, 0.4f);
    [Range(0f, 1f)][SerializeField] private float flashAlpha = 0.5f;  // обычно выше maxAlpha
    [SerializeField] private float flashDuration = 0.2f;

    private Image overlay;
    private float curA;
    private bool hoverActive;
    private float flashUntil;   // Time.unscaledTime, до которого идёт вспышка

    void Awake() => BuildOverlay();

    void Update()
    {
        if (overlay == null) return;
        float dt = Time.unscaledDeltaTime;
        bool flashing = Time.unscaledTime < flashUntil;

        Color rgb = flashing ? flashColor : hoverColor;
        float targetA = flashing ? flashAlpha : (hoverActive ? maxAlpha : 0f);

        // во время вспышки держим flashAlpha; после — плавно к цели ховера/нуля
        curA = flashing ? flashAlpha : Mathf.MoveTowards(curA, targetA, fadeSpeed * dt);

        overlay.color = new Color(rgb.r, rgb.g, rgb.b, curA);
    }

    public void OnPointerEnter(PointerEventData e) => hoverActive = true;
    public void OnPointerExit(PointerEventData e) => hoverActive = false;

    public void OnPointerClick(PointerEventData e)
    {
        flashUntil = Time.unscaledTime + flashDuration;
        curA = flashAlpha;   // мгновенный прыжок вверх = вспышка, без нарастания
    }

    private void BuildOverlay()
    {
        var parent = overlayParent != null ? overlayParent : transform;
        var go = new GameObject("HoverOverlay");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;   // растянуть на родителя

        overlay = go.AddComponent<Image>();
        overlay.sprite = overlaySprite != null ? overlaySprite : cornerSprite; // None => белый квадрат
        overlay.type = Image.Type.Simple;
        overlay.raycastTarget = false;   // не перехватываем клики/ховер (события идут от ClickZone к корню)
        overlay.maskable = useParentMask;
        Color c0 = hoverColor; c0.a = 0f; overlay.color = c0;
        curA = 0f;

        if (insertBefore != null) rt.SetSiblingIndex(insertBefore.transform.GetSiblingIndex());
        else rt.SetAsLastSibling();
    }
}