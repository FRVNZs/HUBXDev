using UnityEngine;
using UnityEngine.UI;   // Graphic, Image, RawImage, Button
using TMPro;
using System.IO;        // File.Exists / ReadAllBytes

/// <summary>
/// Двуслойная обложка карточки:
///  - Card (coverNew): новые проекты, одна картинка 16:9 -> CardCoverFit делает cover,
///    маска режет бока. Якоря center-middle, sizeDelta владеет компонент.
///  - CardOLD (coverOld): старые проекты, настоящая 4:3 обложка -> stretch-all, без
///    компонента, вписывается в окно ровно (без кропа и без растяжения).
/// Выбор слоя: coverImagePath непустой -> CardOLD; иначе -> Card (detail).
/// </summary>
public class CardClickHandler : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
    [Tooltip("Card — слой новых проектов (16:9 -> cover-кроп). На нём висит CardCoverFit.")]
    [SerializeField] private Graphic coverNew;
    [Tooltip("CardOLD — слой старых проектов (4:3 native, stretch-all, без CardCoverFit).")]
    [SerializeField] private Graphic coverOld;

    [Header("Interaction")]
    [SerializeField] private Button interactableButton;   // ClickZone — открытие деталей
    [Tooltip("Крестик удаления. Включается кодом ТОЛЬКО на избранном.")]
    [SerializeField] private Button deleteButton;

    public ProjectData CurrentData { get; private set; }
    private GridGenerator gridGeneratorRef;

    private Sprite createdSprite;
    private Texture2D createdTexture;

    public void Setup(ProjectData data, GridGenerator generator)
    {
        CurrentData = data;
        gridGeneratorRef = generator;

        if (titleText != null) titleText.text = data.title.ToUpper();

        // <<< ВЫБОР СЛОЯ: старый проект (есть настоящий 4:3 cover) -> CardOLD; новый -> Card (detail).
        bool hasNativeCover = !string.IsNullOrEmpty(data.coverImagePath);
        Graphic target = hasNativeCover ? coverOld : coverNew;
        string path = hasNativeCover ? data.coverImagePath : data.detailImagePath;

        // Видимость слоёв переключаем ДО загрузки (карточка ещё alpha=0 в каскаде -> мигания нет).
        if (coverNew != null) coverNew.gameObject.SetActive(!hasNativeCover);
        if (coverOld != null) coverOld.gameObject.SetActive(hasNativeCover);

        LoadCoverInto(target, path);

        // Клик по карточке = открыть детали.
        if (interactableButton != null)
        {
            interactableButton.onClick.RemoveAllListeners();
            interactableButton.onClick.AddListener(() =>
            {
                Debug.Log($"[CARD-CLICK] onClick вызван: '{CurrentData?.title}' gen={(gridGeneratorRef != null)}");
                if (gridGeneratorRef != null) gridGeneratorRef.OpenProjectDetails(CurrentData);
                else Debug.LogError("GridGenerator ссылка пустая!");
            });
        }
        else Debug.LogError($"В карточке '{data.title}' НЕ НАЙДЕН компонент Button!");
        string ibName = interactableButton != null ? interactableButton.name : "NULL";   // <<< ВРЕМЕННО
        Debug.Log($"[CARD-SETUP] interactableButton={ibName}");

        // Крестик: видимость + listener в одном if (None на All-префабе -> блок пропускается).
        if (deleteButton != null)
        {
            bool showDelete = gridGeneratorRef != null && gridGeneratorRef.ShowsDeleteButton;
            deleteButton.gameObject.SetActive(showDelete);
            deleteButton.onClick.RemoveAllListeners();
            if (showDelete)
                deleteButton.onClick.AddListener(() =>
                {
                    if (gridGeneratorRef != null && CurrentData != null)
                        gridGeneratorRef.RemoveFavorite(CurrentData.id);
                });
        }
    }

    private void LoadCoverInto(Graphic cover, string fullPath)
    {
        if (cover == null) { Debug.LogWarning($"[CARD] '{CurrentData?.title}': слой обложки не назначен (coverNew/coverOld)."); return; }
        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath)) { Debug.LogWarning($"[CARD] '{CurrentData?.title}': нет файла '{fullPath}'"); return; }

        byte[] bytes = File.ReadAllBytes(fullPath);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes)) { Destroy(tex); Debug.LogError($"[CARD] '{CurrentData?.title}': не декодируется '{fullPath}'"); return; }
        tex.name = fullPath;
        FreeCreated();

        if (cover is RawImage ri) { createdTexture = tex; ri.texture = tex; }
        else if (cover is Image im)
        {
            createdSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            createdTexture = tex; im.sprite = createdSprite;
        }
        else { Destroy(tex); Debug.LogError($"[CARD] '{CurrentData?.title}': на слое ни Image ни RawImage"); }

        // <<< ИСПРАВЛЕНО: пересчёт cover/crop на ЦЕЛЕВОМ слое (раньше GetComponent был на корне -> null).
        // На CardOLD компонента нет -> fit==null -> не трогаем (stretch-all остаётся ровным).
        var fit = cover.GetComponent<CardCoverFit>();
        if (fit != null) fit.ForceApply();
    }

    private void FreeCreated()
    {
        if (createdSprite != null) { Destroy(createdSprite); createdSprite = null; }
        if (createdTexture != null) { Destroy(createdTexture); createdTexture = null; }
    }

    void OnDestroy() => FreeCreated();
}