using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Подсветка активной кнопки навигации через дубликат-иконку.
/// Вешается НА САМУЮ кнопку (рядом с её Toggle). Никаких списков и общего
/// контроллера: слушаем ТОЛЬКО собственный Toggle кнопки, поэтому подсветка
/// всегда синхронна с показом панели (панель ведь тоже от этого Toggle'а).
/// Эксклюзивность ("светится одна") даёт ToggleGroup на родителе.
/// </summary>
[DisallowMultipleComponent]
public class NavIconHighlight : MonoBehaviour
{
    [Tooltip("Обычная (серая) иконка — видна, когда кнопка НЕ выбрана.")]
    [SerializeField] private GameObject normalIcon;
    [Tooltip("Светящаяся иконка-дубликат — видна, когда кнопка выбрана.")]
    [SerializeField] private GameObject activeIcon;

    private Toggle toggle;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        if (toggle == null)
            Debug.LogError($"[{name}] NavIconHighlight: на объекте нет Toggle — подсветка работать не будет.", this);
    }

    void OnEnable()
    {
        if (toggle == null) return;
        toggle.onValueChanged.AddListener(ApplyState);
        ApplyState(toggle.isOn); // сразу синхронизируем под стартовое выбранное состояние
    }

    void OnDisable()
    {
        if (toggle != null) toggle.onValueChanged.RemoveListener(ApplyState);
    }

    private void ApplyState(bool isOn)
    {
        if (activeIcon) activeIcon.SetActive(isOn);   // выбранная -> светящаяся
        if (normalIcon) normalIcon.SetActive(!isOn);  // и наоборот
    }
}