using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Контракт анимированного ухода/оживления панели (Details/Screen каскады).</summary>
public interface IPanelCascade
{
    void PlayFadeOutAll(Action onDone);   // чистый alpha 1->0 всех блоков -> onDone (HardHide)
    void ForcePlayIn();                   // переиграть волну ВХОДА для тающейся панели (спам-клик)
}

/// <summary>
/// Переключатель вкладок/сабвью. БЕСШОВНЫЙ CROSSFADE: уходящая растворяется (alpha 1->0),
/// а новая проявляется СРАЗУ в тот же кадр (PanelTransition fade корня + волна блоков внутри).
/// Паузы-дырки нет: сумма альф видима всегда. Длительность ухода раннера должна ~=
/// PanelTransition.Duration входящей (симметрия кроссфейда). Возврат на уже-видимую
/// (перекрытую сабвью) панель не переигрывает волну — она обнажается сама.
/// </summary>
public class TabSwitcher : MonoBehaviour
{
    [Header("Вкладки (порядок любой)")]
    [SerializeField] private List<TabVisual> tabs = new List<TabVisual>();

    [Header("Сабвью поверх вкладок (DetailsInfo и т.п.)")]
    [SerializeField] private List<GameObject> subviews = new List<GameObject>();

    [Header("Уход")]
    [SerializeField] private bool hideAnimated = true;     // false = прежний мгновенный уход

    [System.Serializable]
    private class TabVisual
    {
        public GameObject panel;
        public GameObject normalIcon;
        public GameObject activeIcon;
    }

    private GameObject lastTab;
    private readonly List<GameObject> lastLeaving = new List<GameObject>();   // достройка при спам-клике
    private readonly HashSet<GameObject> _fadingOut = new HashSet<GameObject>();

    void Start()
    {
        GameObject initial = null;
        foreach (var t in tabs) if (t.panel != null && t.panel.activeSelf) { initial = t.panel; break; }
        RememberTab(initial);
        ApplyVisual(initial);
    }

    public void SwitchTo(GameObject target)
    {
        if (target == null) return;

        // спам-клик во время fade: мгновенно достроить прошлый уход (убрать полупрозрачный призрак)
        foreach (var x in lastLeaving) HardHide(x);
        lastLeaving.Clear();
        _fadingOut.Clear();

        // 1) уход всех активных не-целей (параллельно с появлением новой)
        foreach (var p in CollectActiveExcept(target))
        {
            if (hideAnimated && p.TryGetComponent<IPanelCascade>(out var fc))
            {
                fc.PlayFadeOutAll(() => HardHide(p));
                _fadingOut.Add(p);
                lastLeaving.Add(p);
            }
            else HardHide(p);
        }

        // 2) новая показывается СРАЗУ (crossfade, без паузы)
        RevealTarget(target);
    }

    public void GoBack() => SwitchTo(lastTab);

    private void RevealTarget(GameObject target)
    {
        var ptDbg = target.GetComponent<PanelTransition>();
        Debug.Log($"[SWITCH] target={target.name} activeSelf={target.activeSelf} fadingOut={_fadingOut.Contains(target)} pt={(ptDbg != null ? "ok" : "НЕТ")}");  // <<< ВРЕМЕННО
        if (target.activeSelf)
        {
            // тающаяся (спам-клик) -> переиграть волну; иначе уже видима (была перекрыта) -> не трогаем
            if (_fadingOut.Contains(target) && target.TryGetComponent<IPanelCascade>(out var fc))
                fc.ForcePlayIn();
        }
        else Show(target);
        ApplyVisual(target);
        RememberTab(target);
    }

    private List<GameObject> CollectActiveExcept(GameObject target)
    {
        var res = new List<GameObject>();
        foreach (var t in tabs) if (t.panel != null && t.panel != target && t.panel.activeSelf) res.Add(t.panel);
        foreach (var s in subviews) if (s != null && s != target && s.activeSelf) res.Add(s);
        return res;
    }

    private void Show(GameObject go)
    {
        var pt = go.GetComponent<PanelTransition>();
        if (pt != null) pt.ShowWithAnimation(); else go.SetActive(true);
    }
    private void HardHide(GameObject go)
    {
        if (go == null) return;
        var pt = go.GetComponent<PanelTransition>();
        if (pt != null) pt.HideInstant(); else go.SetActive(false);
    }

    private void RememberTab(GameObject target)
    {
        if (target == null) return;
        foreach (var t in tabs) if (t.panel == target && t.activeIcon != null) { lastTab = target; return; }
    }

    private void ApplyVisual(GameObject target)
    {
        foreach (var v in tabs)
        {
            bool on = (v.panel != null && v.panel == target);
            if (v.activeIcon) v.activeIcon.SetActive(on);
            if (v.normalIcon) v.normalIcon.SetActive(!on);
        }
    }
}