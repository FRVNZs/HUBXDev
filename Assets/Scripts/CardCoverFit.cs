using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Режим COVER: картинка заполняет окно маски по большей оси, переполнение по меньшей
/// режет маска родителя. Ставится НА тот же объект, что и Image/RawImage; родитель и есть
/// маска (окно 4:3 карточки). preserveAspect принудительно off.
///
/// ВНИМАНИЕ про редактор: cover физически требует sizeDelta БОЛЬШЕ окна по одной оси
/// (иначе маске нечего резать и Image растянется). Поэтому в edit-mode компонент НЕ
/// работает автоматически — иначе он перетирал бы ручное управление размером.
/// Предпросмотр кропа в редакторе: правый клик по компоненту -> "Apply Cover Fit".
/// В рантайме пересчёт делает CardClickHandler.ForceApply() после назначения sprite.
/// </summary>
[RequireComponent(typeof(Graphic))]
public class CardCoverFit : MonoBehaviour
{
    private Graphic gfx;
    private RectTransform self, parent;

    private void EnsureRefs()
    {
        if (gfx == null) gfx = GetComponent<Graphic>();
        if (self == null) self = (RectTransform)transform;
        if (parent == null && transform.parent != null) parent = transform.parent as RectTransform;
        if (gfx is Image im) im.preserveAspect = false;   // contain нам не нужен
    }

    /// <summary>Рантайм: дёргает CardClickHandler ПОСЛЕ назначения sprite/texture
    /// (смена картинки не вызывает пересчёт RectTransform).</summary>
    public void ForceApply() => Apply();

    /// <summary>Редактор: ручной предпросмотр кропа, не мешающий крутить размер окна.</summary>
    [ContextMenu("Apply Cover Fit")]
    private void ApplyFromMenu() { EnsureRefs(); Apply(); }

    private void Apply()
    {
        EnsureRefs();
        if (self == null || parent == null) return;
        float tw, th;
        if (!GetTexSize(out tw, out th)) return;          // нет текстуры -> не трогаем
        float pw = parent.rect.width, ph = parent.rect.height;
        if (pw <= 0f || ph <= 0f || tw <= 0f || th <= 0f) return;

        // cover: фиксируем ту ось, где картинка "более вытянута" relative окна.
        if (tw / th > pw / ph) self.sizeDelta = new Vector2(ph * (tw / th), ph);  // шире окна -> по высоте
        else self.sizeDelta = new Vector2(pw, pw * (th / tw));  // выше окна -> по ширине
        self.anchoredPosition = Vector2.zero;             // центр кропа
    }

    private bool GetTexSize(out float w, out float h)
    {
        if (gfx is Image im && im.sprite != null && im.sprite.texture != null)
        { w = im.sprite.texture.width; h = im.sprite.texture.height; return true; }
        if (gfx is RawImage ri && ri.texture != null)
        { w = ri.texture.width; h = ri.texture.height; return true; }
        w = h = 0f; return false;
    }
}