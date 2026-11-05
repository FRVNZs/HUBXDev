using UnityEngine;
using TMPro;
using System.IO;
// ВНИМАНИЕ: НЕ добавляй сюда "using System;". Guid/DateTime квалифицированы как System.*,
// а голое Object.FindFirstObjectByType резолвится в UnityEngine.Object именно потому что
// System здесь нет. Появление "using System;" вернёт CS0104 ("Object" неоднозначен).

public class AddProjectFormController : MonoBehaviour
{
    private const string PREFERRED_MARKER = "2_0";
    public static AddProjectFormController Instance { get; private set; }

    [Header("UI Inputs")]
    [SerializeField] private TMP_InputField inputTitle;
    [SerializeField] private TMP_InputField inputVersion;
    [SerializeField] private TMP_InputField inputUrl;
    [SerializeField] private TMP_InputField inputDesc;

    [Header("Image Slots")]
    // <<< СВЕДЕНО К ОДНОМУ СЛОТУ: одна картинка 16:9, карточка режет её маской в 4:3.
    // Старый slotCover удалён из класса (значение в инспекторе просто потеряется — безвредно).
    [SerializeField] private ImageDropSlot slotDetail;

    [Header("Logic References")]
    [SerializeField] private ProjectManager projectManager;
    [SerializeField] private GridGenerator gridGenerator;

    private bool editMode;
    private ProjectData editingData;

    void Awake()
    {
        if (Instance == null) { Instance = this; Debug.Log($"[FORM] Instance = {name} (первый)"); return; }
        if (Instance == this) return;
        bool wePreferred = gameObject.name.Contains(PREFERRED_MARKER);
        bool otherPreferred = Instance.gameObject.name.Contains(PREFERRED_MARKER);
        if (wePreferred && !otherPreferred) { Instance = this; Debug.Log($"[FORM] Instance -> предпочтительный {name}"); }
        else Debug.Log($"[FORM] Instance оставлен {Instance.name} (мы={name})");
    }

    void Start()
    {
        projectManager = Object.FindFirstObjectByType<ProjectManager>();
        gridGenerator = Object.FindFirstObjectByType<GridGenerator>();
    }

    /// <summary>Режим СОЗДАНИЯ (кнопка «+» через UIManager). Чистит поля и анимирует показ.</summary>
    public void ShowForm()
    {
        editMode = false;
        editingData = null;
        ClearAllFields();
        ShowPanel(() => { if (inputTitle != null) inputTitle.Select(); });
    }

    /// <summary>Режим РЕДАКТИРОВАНИЯ (кнопка Edit на деталях). Заполняет данными и анимирует показ.</summary>
    public void ShowForEdit(ProjectData data)
    {
        if (data == null) { ShowForm(); return; }
        editMode = true;
        editingData = data;

        if (inputTitle != null) inputTitle.text = data.title ?? "";
        if (inputVersion != null) inputVersion.text = data.version ?? "";
        if (inputUrl != null) inputUrl.text = data.githubUrl ?? "";
        if (inputDesc != null) inputDesc.text = data.description ?? "";

        // <<< ОДИН СЛОТ: одна картинка = detail ?: cover (старые проекты с cover подхватятся).
        if (slotDetail != null)
        {
            slotDetail.ClearSlot();
            string img = !string.IsNullOrEmpty(data.detailImagePath) ? data.detailImagePath : data.coverImagePath;
            if (!string.IsNullOrEmpty(img)) slotDetail.LoadFromFile(img);
        }

        ShowPanel(() => { if (inputTitle != null) inputTitle.Select(); });
        Debug.Log($"[FORM] Режим правки: '{data.title}' (id={data.id})");
    }

    public void Initialize()
    {
        editMode = false; editingData = null;
        if (inputTitle != null) inputTitle.text = "";
        if (inputVersion != null) inputVersion.text = "";
        if (inputUrl != null) inputUrl.text = "";
        if (inputDesc != null) inputDesc.text = "";
        if (slotDetail != null) slotDetail.ClearSlot();
    }

    public void HideForm() => HidePanel();

    public void SaveProject()
    {
        if (inputTitle == null || string.IsNullOrWhiteSpace(inputTitle.text)) { Debug.LogWarning("Введите название проекта!"); return; }
        // <<< ВАЛИДАЦИЯ ОДНОГО СЛОТА (прежняя проверка cover убрана).
        if (slotDetail == null || !slotDetail.IsLoaded) { Debug.LogError("Загрузите превью (16:9)!"); return; }

        string title = inputTitle.text.Trim();
        string ver = inputVersion != null ? inputVersion.text.Trim() : "";
        string url = inputUrl != null ? inputUrl.text.Trim() : "";
        string desc = inputDesc != null ? inputDesc.text.Trim() : "";
        string detail = slotDetail.LoadedFilePath;
        string cover = "";   // <<< одна картинка: cover пустой, карточка возьмёт detail (см. CardClickHandler)

        if (editMode && editingData != null)
        {
            if (projectManager == null) projectManager = Object.FindFirstObjectByType<ProjectManager>();
            // <<< cover="" -> UpdateProject не трогает/не копирует cover.png, остаётся одна detail.jpg.
            bool ok = projectManager != null &&
                      projectManager.UpdateProject(editingData, title, ver, desc, url, "", detail);
            if (ok) HidePanel();
            else Debug.LogError("❌ Не удалось обновить проект (см. лог ProjectManager).");
            return;
        }

        string uniqueId = System.Guid.NewGuid().ToString().Substring(0, 8);
        string cleanTitle = SanitizeFileName(inputTitle.text);
        string folderName = $"{cleanTitle}_{uniqueId}";
        string bankRoot = ProjectManager.GetDatabaseRootPath();
        string targetFolder = Path.Combine(bankRoot, folderName);

        try
        {
            Directory.CreateDirectory(targetFolder);
            // <<< ЕДИНСТВЕННАЯ КАРТИНКА В ПАПКУ (прежний File.Copy(cover,...) удалён).
            File.Copy(detail, Path.Combine(targetFolder, "detail.jpg"), true);

            string now = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ProjectData newData = new ProjectData
            {
                id = uniqueId,
                title = title,
                version = ver,
                description = desc,
                createdAt = now,
                updatedAt = now,
                githubUrl = url,
                coverImageFile = "",                 // <<< новых cover нет, карточка берёт detail
                detailImageFile = "detail.jpg"
            };
            File.WriteAllText(Path.Combine(targetFolder, "project.json"), JsonUtility.ToJson(newData, true));
            Debug.Log($"✅ Проект '{newData.title}' сохранён (url='{newData.githubUrl}') в {targetFolder}");

            if (projectManager != null) projectManager.ReloadProjectsFromDisk();
            if (gridGenerator != null) gridGenerator.GenerateCards();
            HidePanel();
        }
        catch (System.Exception e) { Debug.LogError($"❌ Ошибка сохранения: {e.Message}"); }
    }

    // ---- анимирующие обёртки показа/скрытия самой модалки ----
    private void ShowPanel(System.Action onComplete = null)
    {
        var pt = GetComponent<PanelTransition>();
        if (pt != null) pt.ShowWithAnimation(onComplete);
        else { gameObject.SetActive(true); onComplete?.Invoke(); }
    }
    private void HidePanel()
    {
        var pt = GetComponent<PanelTransition>();
        var cascade = GetComponent<ModalCascadeRunner>();
        if (cascade != null)
            cascade.PlayReverse(() => { if (pt != null) pt.HideInstant(); else gameObject.SetActive(false); });
        else if (pt != null) pt.HideWithAnimation();
        else gameObject.SetActive(false);
    }

    private string SanitizeFileName(string fileName)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) fileName = fileName.Replace(c.ToString(), "");
        return fileName.Trim();
    }
    private void ClearAllFields()
    {
        if (inputTitle != null) inputTitle.text = "";
        if (inputVersion != null) inputVersion.text = "";
        if (inputUrl != null) inputUrl.text = "";
        if (inputDesc != null) inputDesc.text = "";
        if (slotDetail != null) slotDetail.ClearSlot();
    }
}