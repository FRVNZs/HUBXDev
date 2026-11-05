using UnityEngine;

/// <summary>
/// Лёгкое «парение» карточки: вертикальный/горизонтальный bob + микро-tilt поворотом.
/// Вешать на КОРЕНЬ карточки (тот же GO, что GridLayoutGroup расставляет).
///
/// Безопасно по архитектуре:
///  - bob пишет anchoredPosition, но С TRACKING внешнего write: если позицию тронул
///    layout (пересборка при поиске/удалении), база подхватывается без прыжка/дрейфа;
///  - tilt пишет localRotation (layout/каскад rotation не трогают);
///  - localScale root'а НЕ трогаем (его контролирует каскад появления + CollectCards).
/// Фаза уникальна по instanceID -> ряд не качается синхронно.
/// </summary>
[DisallowMultipleComponent]
public class CardFloat : MonoBehaviour
{
    [Header("Bob (смещение, px)")]
    [SerializeField] private float bobY = 4f;     // вверх-вниз
    [SerializeField] private float bobX = 0f;     // вбок (0 = только вертикаль)
    [SerializeField] private float bobPeriod = 3.2f;

    [Header("Tilt (покачивание, градусы)")]
    [SerializeField] private float tilt = 1.5f;
    [SerializeField] private float tiltPeriod = 4.7f;

    private RectTransform rt;
    private Vector2 basePos;      // база layout'а (обновляется при внешнем write)
    private Vector2 applied;      // последнее что ЗАПИСАЛИ мы
    private float phase;

    void Awake()
    {
        rt = (RectTransform)transform;
        basePos = applied = rt.anchoredPosition;
        // фаза из instanceID -> соседние карточки не в такт
        phase = (Mathf.Abs(GetInstanceID()) % 997) / 997f * Mathf.PI * 2f;
    }

    void Update()
    {
        if (rt == null) return;
        float t = Time.unscaledTime;

        // 1) подхватить внешний write layout'а (без прыжка и дрейфа)
        Vector2 cur = rt.anchoredPosition;
        if ((cur - applied).sqrMagnitude > 1e-4f) basePos = cur;

        // 2) bob поверх базы
        float by = Mathf.Sin((t / bobPeriod) * Mathf.PI * 2f + phase) * bobY;
        float bx = bobX != 0f ? Mathf.Sin((t / bobPeriod) * Mathf.PI * 2f + phase * 1.3f) * bobX : 0f;
        Vector2 target = basePos + new Vector2(bx, by);
        rt.anchoredPosition = target;
        applied = target;

        // 3) tilt (rotation layout/каскад не трогают)
        rt.localRotation = Quaternion.Euler(0f, 0f,
            Mathf.Sin((t / tiltPeriod) * Mathf.PI * 2f + phase * 0.7f) * tilt);
    }

    void OnDisable()
    {
        // вернуть чистое состояние, чтобы парализация/переиспользование не оставили крен
        if (rt != null) { rt.localRotation = Quaternion.identity; rt.anchoredPosition = basePos; applied = basePos; }
    }
}