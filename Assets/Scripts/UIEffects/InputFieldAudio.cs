using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Звуки для поля ввода (симметричный компаньон ButtonAudio, но для TMP_InputField).
///  - focus: поле получило фокус (клик/Tab) -> аналог «клика кнопки».
///  - type : набран/удалён символ -> тихий тик, ИГРАЕТ ТОЛЬКО когда поле в фокусе,
///           чтобы программные очистки (text="") не давали ложных звуков.
///  - hover/blur: по умолчанию выключены (поля не озвучивают на наведение, чтобы не спамить).
/// Не конфликтует с внутренней логикой TMP: UGUI дёргает ВСЕ компоненты с нужным интерфейсом.
/// </summary>
[RequireComponent(typeof(TMP_InputField))]
public class InputFieldAudio : MonoBehaviour,
    ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Clips")]
    [Tooltip("Получил фокус (клик/Tab). Можно тянуть button_click.")]
    [SerializeField] private AudioClip focusClip;
    [Tooltip("Набор/удаление символа. Тяни тихий тик (или тот же button_click, но тише).")]
    [SerializeField] private AudioClip typeClip;
    [Tooltip("Наведение мыши (опционально).")]
    [SerializeField] private AudioClip hoverClip;
    [Tooltip("Потеря фокуса (обычно пусто = без звука).")]
    [SerializeField] private AudioClip blurClip;

    [Header("Levels / Flags")]
    [SerializeField] private float volume = 1f;
    [Tooltip("Тик набора играем тише, чтобы длинный поиск не утомлял.")]
    [SerializeField] private float typeVolume = 0.5f;
    [SerializeField] private bool playHover = false;
    [SerializeField] private bool playBlur = false;

    private TMP_InputField field;
    private UnityAction<string> onVal;   // держим ссылку, чтобы RemoveListener сработал

    void OnEnable()
    {
        field = GetComponent<TMP_InputField>();
        onVal = OnValueChanged;
        field.onValueChanged.AddListener(onVal);
    }
    void OnDisable()
    {
        if (field != null && onVal != null) field.onValueChanged.RemoveListener(onVal);
    }

    private void OnValueChanged(string _)
    {
        // onlyWhenFocused: отсекаем программное text="" (ClearAllFields в форме) — поле не в фокусе.
        if (field != null && field.isFocused) Sfx.Play(typeClip, typeVolume, SfxChannel.Hover);
    }

    public void OnSelect(BaseEventData e) => Sfx.Play(focusClip, volume, SfxChannel.Click);
    public void OnDeselect(BaseEventData e) { if (playBlur) Sfx.Play(blurClip, volume, SfxChannel.Hover); }
    public void OnPointerEnter(PointerEventData e) { if (playHover) Sfx.Play(hoverClip, volume, SfxChannel.Hover); }
    public void OnPointerExit(PointerEventData e) { /* без звука */ }
}