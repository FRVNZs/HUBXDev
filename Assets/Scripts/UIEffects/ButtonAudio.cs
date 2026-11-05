using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Звук на нативную Button: клик через onClick, ховер через pointer enter/exit.
/// Не конфликтует с UnifiedButtonTint/ButtonStickyReset — UGUI дёргает все компоненты
/// с нужным интерфейсом. Hover можно выключить на карточках/навигации, чтобы не спамить.
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private float volume = 1f;
    [Tooltip("Играть ли звук при наведении. На карточках сетки лучше OFF (спам при скролле).")]
    [SerializeField] private bool playHover = true;

    private Button btn;

    void Awake()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(() => Sfx.Play(clickClip, volume, SfxChannel.Click));
    }

    public void OnPointerEnter(PointerEventData e) { if (playHover) Sfx.Play(hoverClip, volume, SfxChannel.Hover); }
    public void OnPointerExit(PointerEventData e) { /* выход без звука */ }
}