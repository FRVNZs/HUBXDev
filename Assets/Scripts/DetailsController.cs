using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Контроллер экрана деталей. Состояние «в избранном» рисуется ДВУМЯ статичными
/// братьями внутри кнопки (FavNormal / FavActive), переключаемыми SetActive — по
/// той же схеме, что и навигационные кнопки слева. Текст «В избранном» НЕ содержит
/// глифа ✓ (его нет в LiberationSans SDF -> квадрат), галочка/сердце = спрайт внутри
/// FavActive. SetData безусловно пересчитывает состояние из стора, поэтому повторный
/// вход в детали идемпотентен и ничего не накапливает.
/// </summary>
public class DetailsController : MonoBehaviour
{
    [Header("Явные поля (перетащи, если автопоиск не нашёл)")]
    [SerializeField] private TMP_Text headerTitle;
    [SerializeField] private TMP_Text versionText;
    [SerializeField] private TMP_Text updateText;
    [SerializeField] private TMP_Text sizeText;
    [SerializeField] private TMP_Text urlText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private Graphic previewImage;

    [Header("Edit")]
    [SerializeField] private Button editButton;
    [SerializeField] private AddProjectFormController formRef;

    [Header("Favorite")]
    [SerializeField] private Button favoriteButton;     // можно оставить пустым -> автопоиск
    [Tooltip("Брат «+ Добавить в избранное» (иконка-плюс + текст). Активен, когда НЕ в избранном.")]
    [SerializeField] private GameObject favNormal;
    [Tooltip("Брат «В избранном» (спрайт-галочка/сердце + текст БЕЗ глифа ✓). Активен, когда в избранном.")]
    [SerializeField] private GameObject favActive;

    private ProjectData currentData;
    private Sprite createdSprite;
    private Texture2D createdTexture;

    void Start()
    {
        if (editButton == null)
            editButton = GetComponentsInChildren<Button>(true)
                         .FirstOrDefault(b => b.gameObject != gameObject && b.name.ToLowerInvariant().Contains("edit"));
        if (editButton != null)
        {
            editButton.onClick.RemoveAllListeners();
            editButton.onClick.AddListener(OnEditClicked);
        }
        else Debug.LogWarning("[DETAILS] Кнопка Edit не найдена (перетащи Button (Edit) в editButton).");

        if (favoriteButton == null)
            favoriteButton = GetComponentsInChildren<Button>(true)
                .FirstOrDefault(b => b.gameObject != gameObject &&
                    (b.name.ToLowerInvariant().Contains("избранн") || b.name.ToLowerInvariant().Contains("favorite")));
        if (favoriteButton != null)
        {
            favoriteButton.onClick.RemoveAllListeners();
            favoriteButton.onClick.AddListener(OnFavoriteClicked);
            // братья favNormal/favActive тянутся в инспекторе; автопоиск по ним не нужен
        }
        else Debug.LogWarning("[DETAILS] Кнопка «Добавить в избранное» не найдена (перетащи Button в favoriteButton).");
    }

    void OnEnable() => ProjectManager.OnProjectsLoaded += RefreshIfOpen;
    void OnDisable() => ProjectManager.OnProjectsLoaded -= RefreshIfOpen;

    public void SetData(ProjectData data)
    {
        if (data == null) return;
        currentData = data;

        headerTitle = Resolve(headerTitle, null);
        versionText = Resolve(versionText, "version", "ue");
        updateText = Resolve(updateText, "update");
        sizeText = Resolve(sizeText, "size");
        urlText = Resolve(urlText, "url");
        descText = Resolve(descText, "описан", "description", "desc");
        previewImage = ResolveG(previewImage, "preview", "cover", "photo");

        if (headerTitle != null) headerTitle.text = data.title.ToUpperInvariant();
        if (versionText != null) versionText.text = string.IsNullOrEmpty(data.version) ? "—" : data.version;
        if (descText != null) descText.text = string.IsNullOrEmpty(data.description) ? "—" : data.description;
        if (urlText != null) urlText.text = string.IsNullOrEmpty(data.githubUrl) ? "не указан" : data.githubUrl;
        RefreshFavoriteVisual(UserDataStore.IsFavorite(data.id));   // безусловно: читает состояние из стора

        string folder = !string.IsNullOrEmpty(data.coverImagePath) ? Path.GetDirectoryName(data.coverImagePath)
                      : !string.IsNullOrEmpty(data.detailImagePath) ? Path.GetDirectoryName(data.detailImagePath) : null;

        if (updateText != null) updateText.text = FormatRelative(data.updatedAt, data.createdAt, folder);
        if (sizeText != null) sizeText.text = folder != null && Directory.Exists(folder) ? FormatSize(GetFolderSize(folder)) : "—";

        // Превью: sprite назначаем СЕЙЧАС (пока блок ещё alpha 0 в каскаде), чтобы к моменту
        // появления шага 3 картинка уже была реальной, без заглушки. Альфой превью владеет
        // DetailsCascadeRunner (блок 3), не этот класс - двух владельцев нет.
        string previewPath = !string.IsNullOrEmpty(data.detailImagePath) ? data.detailImagePath : data.coverImagePath;
        LoadPreview(previewImage, previewPath, data.title);
    }

    private void RefreshIfOpen()
    {
        if (!gameObject.activeInHierarchy || currentData == null) return;
        if (ProjectManager.Instance == null) return;
        var fresh = ProjectManager.Instance.AllProjects.FirstOrDefault(p => p.id == currentData.id);
        if (fresh != null) SetData(fresh);
    }

    private void OnEditClicked()
    {
        if (currentData == null) { Debug.LogWarning("[DETAILS] Edit: нет данных проекта"); return; }
        var form = formRef != null ? formRef : AddProjectFormController.Instance;
        if (form == null) form = UnityEngine.Object.FindFirstObjectByType<AddProjectFormController>();
        if (form != null) form.ShowForEdit(currentData);
        else Debug.LogError("[DETAILS] Edit: форма не найдена. Перетащи AddProjectModal2_0 в Form Ref.");
    }

    private TMP_Text Resolve(TMP_Text f, params string[] h) => f != null ? f : FindTmp(h);
    private Graphic ResolveG(Graphic f, params string[] h) => f != null ? f : FindGph(h);
    private TMP_Text FindTmp(params string[] hints)
    {
        if (hints == null || hints.Length == 0) return null;
        foreach (var t in GetComponentsInChildren<TMP_Text>(true))
        { if (t.gameObject == gameObject) continue; string n = t.name.ToLowerInvariant(); foreach (var h in hints) if (n.Contains(h)) return t; }
        return null;
    }
    private Graphic FindGph(params string[] hints)
    {
        if (hints == null || hints.Length == 0) return null;
        foreach (var g in GetComponentsInChildren<Graphic>(true))
        { if (g.gameObject == gameObject) continue; string n = g.name.ToLowerInvariant(); foreach (var h in hints) if (n.Contains(h)) return g; }
        return null;
    }

    private void LoadPreview(Graphic cover, string fullPath, string title)
    {
        if (cover == null) { Debug.LogError($"[DETAILS] '{title}': превью не найдено."); return; }
        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath)) { Debug.LogError($"[DETAILS] '{title}': файла нет: '{fullPath}'"); cover.color = Color.red; return; }
        byte[] bytes = File.ReadAllBytes(fullPath);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes)) { Destroy(tex); cover.color = Color.red; Debug.LogError($"[DETAILS] '{title}': не декодируется {fullPath}"); return; }
        tex.name = fullPath; FreeCreated();
        if (cover is RawImage ri) { createdTexture = tex; ri.texture = tex; ri.color = Color.white; }
        else if (cover is Image im) { createdSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f); createdTexture = tex; im.sprite = createdSprite; im.color = Color.white; }
        else { Destroy(tex); cover.color = Color.red; Debug.LogError($"[DETAILS] '{title}': на превью ни Image ни RawImage ({cover.GetType().Name})"); }
    }

    private long GetFolderSize(string folder) { try { return new DirectoryInfo(folder).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length); } catch { return 0; } }
    private string FormatSize(long bytes)
    {
        if (bytes <= 0) return "0 Б";
        string[] u = { "Б", "КБ", "МБ", "ГБ", "ТБ" }; double v = bytes; int i = 0;
        while (v >= 1024 && i < u.Length - 1) { v /= 1024; i++; }
        return $"{v.ToString(i == 0 ? "0" : "0.#")} {u[i]}";
    }
    private string FormatRelative(string updatedAt, string createdAt, string folder)
    {
        DateTime dt;
        if (!TryParse(updatedAt, out dt) && !TryParse(createdAt, out dt))
            dt = folder != null && Directory.Exists(folder) ? Directory.GetLastWriteTime(folder) : DateTime.MinValue;
        if (dt == DateTime.MinValue) return "—";
        var span = DateTime.Now - dt;
        if (span.TotalMinutes < 1) return "только что";
        if (span.TotalHours < 1) return $"{(int)span.TotalMinutes} мин назад";
        if (span.TotalDays < 1) return $"{(int)span.TotalHours} ч назад";
        if (span.TotalDays < 30) return $"{(int)span.TotalDays} дн назад";
        return dt.ToString("dd.MM.yyyy");
    }
    private static bool TryParse(string s, out DateTime dt)
        => DateTime.TryParseExact(s, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dt);

    private void FreeCreated()
    {
        if (createdSprite != null) { Destroy(createdSprite); createdSprite = null; }
        if (createdTexture != null) { Destroy(createdTexture); createdTexture = null; }
    }

    private void OnFavoriteClicked()
    {
        if (currentData == null) { Debug.LogWarning("[DETAILS] Favorite: нет данных проекта"); return; }
        bool now = UserDataStore.ToggleFavorite(currentData.id);
        RefreshFavoriteVisual(now);
        if (favoriteButton != null)
            foreach (var t in favoriteButton.GetComponentsInChildren<UnifiedButtonTint>(true))
                t.ForceReleasePointer();
    }

    private void RefreshFavoriteVisual(bool isFav)
    {
        if (favActive) favActive.SetActive(isFav);    // «В избранном» + спрайт-галочка (без глифа ✓)
        if (favNormal) favNormal.SetActive(!isFav);   // «Добавить в избранное» + плюс
    }

    void OnDestroy() => FreeCreated();
}