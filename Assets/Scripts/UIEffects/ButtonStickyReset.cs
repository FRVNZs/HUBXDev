using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Обходка залипания нативной Button/Selectable при переключении SetActive под курсором.
/// Вешать на КАЖДУЮ из двух кнопок («Добавить в избранное» и «В избранном»).
/// Сбрасывает hover/press/select в LateUpdate (гарантированно после onClick, который
/// переключил кнопки и не дал EventSystem дослать Exit/Up) и в OnEnable (новая кнопка
/// не стартует залипшей).
/// </summary>
[DisallowMultipleComponent]
public class ButtonStickyReset : MonoBehaviour,
    IPointerExitHandler, IPointerUpHandler, IPointerClickHandler
{
    private Selectable sel;
    private bool needReset;

    void Awake() => sel = GetComponent<Selectable>();

    public void OnPointerExit(PointerEventData e) => needReset = true;
    public void OnPointerUp(PointerEventData e) => needReset = true;
    public void OnPointerClick(PointerEventData e) => needReset = true;

    void LateUpdate()
    {
        if (!needReset) return;
        needReset = false;
        ForceReset();
    }

    void OnEnable() => ForceReset(clearHover: false);

    private void ForceReset(bool clearHover = true)
    {
        if (sel == null) return;
        var es = EventSystem.current;
        if (es == null) return;
        var ped = new PointerEventData(es);

        // Имена паттернов разные (ex/up/ds) -> нет CS0128.
        if (clearHover && sel is IPointerExitHandler ex) ex.OnPointerExit(ped);
        if (sel is IPointerUpHandler up) up.OnPointerUp(ped);
        if (sel is IDeselectHandler ds) ds.OnDeselect(ped);   // снятие выделения = IDeselectHandler
    }
}