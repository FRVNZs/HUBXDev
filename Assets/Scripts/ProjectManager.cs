using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

public class ProjectManager : MonoBehaviour
{
    public static ProjectManager Instance { get; private set; }

    // ===== Контракт для GridGenerator =====
    public static event Action OnProjectsLoaded;
    public List<ProjectData> AllProjects { get; private set; } = new List<ProjectData>();
    // ======================================

    [Header("Database Settings")]
    [SerializeField] private string databaseFolderName = "ProjectBank";
    public string DatabaseRootPath { get; private set; }

    private Dictionary<string, string> projectFolderCache = new Dictionary<string, string>();

    // ЕДИНСТВЕННАЯ точка вычисления корня базы. И Awake, и контроллер зовут только её.
    // Не зависит от GetCurrentDirectory() -> способ запуска больше не важен.
    public static string GetDatabaseRootPath()
    {
#if UNITY_EDITOR
        // Редактор: <корень проекта>/ProjectBank
        return Path.Combine(Application.dataPath, "..", "ProjectBank");
#else
        // Билд: Application.dataPath = <папка exe>/GitBank_Data  ->  ".." = папка exe
        return Path.Combine(Application.dataPath, "..", "ProjectBank");
#endif
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        DatabaseRootPath = GetDatabaseRootPath();
        Debug.Log($"📂 База данных: {DatabaseRootPath}");

        EnsureDatabaseExists();
        LoadProjectsFromDisk(notifySubscribers: false);
    }

    private void EnsureDatabaseExists()
    {
        if (!Directory.Exists(DatabaseRootPath))
            Directory.CreateDirectory(DatabaseRootPath);
    }

    private void LoadProjectsFromDisk(bool notifySubscribers = true)
    {
        AllProjects.Clear();
        projectFolderCache.Clear();

        if (Directory.Exists(DatabaseRootPath))
        {
            foreach (var folder in Directory.GetDirectories(DatabaseRootPath))
            {
                string jsonPath = Path.Combine(folder, "project.json");
                if (!File.Exists(jsonPath)) continue; // _inbox без json -> тихо пропускаем

                try
                {
                    ProjectData data = JsonUtility.FromJson<ProjectData>(File.ReadAllText(jsonPath));
                    if (data == null) continue;

                    data.coverImagePath = ResolveFullPath(folder, data.coverImageFile);
                    data.detailImagePath = ResolveFullPath(folder, data.detailImageFile);

                    if (!string.IsNullOrEmpty(data.id))
                        projectFolderCache[data.id] = folder;

                    AllProjects.Add(data);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"⚠️ Битый JSON в {folder}: {e.Message}");
                }
            }
        }

        AllProjects = AllProjects.OrderByDescending(p => p.createdAt).ToList();
        Debug.Log($"LOAD: проектов найдено = {AllProjects.Count}");

        if (notifySubscribers)
            OnProjectsLoaded?.Invoke();
    }

    public List<ProjectData> GetAllProjects()
    {
        LoadProjectsFromDisk(notifySubscribers: false);
        return AllProjects;
    }

    public void ReloadProjectsFromDisk()
    {
        LoadProjectsFromDisk(notifySubscribers: true);
    }

    public bool SaveNewProject(string title, string version, string description,
                               string coverTempPath, string detailTempPath, string githubUrl = "")
    {
        try
        {
            string safeFolderName = GenerateSafeFolderName(title, version);
            string projectFolderPath = Path.Combine(DatabaseRootPath, safeFolderName);
            if (Directory.Exists(projectFolderPath)) return false;
            Directory.CreateDirectory(projectFolderPath);

            string coverFile = "", detailFile = "";
            if (!string.IsNullOrEmpty(coverTempPath) && File.Exists(coverTempPath))
            {
                coverFile = "cover" + Path.GetExtension(coverTempPath);
                File.Copy(coverTempPath, Path.Combine(projectFolderPath, coverFile), true);
            }
            if (!string.IsNullOrEmpty(detailTempPath) && File.Exists(detailTempPath))
            {
                detailFile = "detail" + Path.GetExtension(detailTempPath);
                File.Copy(detailTempPath, Path.Combine(projectFolderPath, detailFile), true);
            }

            ProjectData data = new ProjectData
            {
                id = Guid.NewGuid().ToString(),
                title = title,
                version = version,
                description = description,
                createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                githubUrl = githubUrl,
                coverImageFile = coverFile,
                detailImageFile = detailFile
            };
            File.WriteAllText(Path.Combine(projectFolderPath, "project.json"),
                              JsonUtility.ToJson(data, true));

            LoadProjectsFromDisk(notifySubscribers: true);
            return true;
        }
        catch (Exception e) { Debug.LogError($"💥 SaveNewProject: {e.Message}"); return false; }
    }
       
    public bool UpdateProject(ProjectData existing, string newTitle, string newVersion,
                              string newDescription, string newGithubUrl,
                              string coverTempPath, string detailTempPath)
    {
        if (existing == null || string.IsNullOrEmpty(existing.id)) return false;
        try
        {
            // 1. Находим папку проекта по id (кэш -> перебор, как в DeleteProject)
            string folder = null;
            if (projectFolderCache.TryGetValue(existing.id, out string cached) && Directory.Exists(cached))
                folder = cached;
            else
                foreach (var f in Directory.GetDirectories(DatabaseRootPath))
                {
                    string jp = Path.Combine(f, "project.json");
                    if (!File.Exists(jp)) continue;
                    var d = JsonUtility.FromJson<ProjectData>(File.ReadAllText(jp));
                    if (d != null && d.id == existing.id) { folder = f; break; }
                }
            if (folder == null) { Debug.LogError($"[UPDATE] папка проекта {existing.id} не найдена"); return false; }

            // 2. Картинки: копируем только если источник != целевой файл в папке
            string coverFile = existing.coverImageFile;   // по умолчанию оставляем старое имя
            string detailFile = existing.detailImageFile;

            if (!string.IsNullOrEmpty(coverTempPath) && File.Exists(coverTempPath))
            {
                string dst = Path.Combine(folder, string.IsNullOrEmpty(coverFile) ? "cover.png" : coverFile);
                if (!SamePath(coverTempPath, dst))
                {
                    if (string.IsNullOrEmpty(coverFile)) coverFile = "cover.png";
                    File.Copy(coverTempPath, Path.Combine(folder, coverFile), true);
                }
            }
            if (!string.IsNullOrEmpty(detailTempPath) && File.Exists(detailTempPath))
            {
                string dst = Path.Combine(folder, string.IsNullOrEmpty(detailFile) ? "detail.jpg" : detailFile);
                if (!SamePath(detailTempPath, dst))
                {
                    if (string.IsNullOrEmpty(detailFile)) detailFile = "detail.jpg";
                    File.Copy(detailTempPath, Path.Combine(folder, detailFile), true);
                }
            }

            // 3. Перезаписываем JSON: id/createdAt НЕ трогаем, updatedAt = сейчас
            existing.title = newTitle;
            existing.version = newVersion;
            existing.description = newDescription;
            existing.githubUrl = newGithubUrl;
            existing.updatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            existing.coverImageFile = coverFile;
            existing.detailImageFile = detailFile;

            File.WriteAllText(Path.Combine(folder, "project.json"), JsonUtility.ToJson(existing, true));
            Debug.Log($"✅ Обновлён проект '{existing.title}' (id={existing.id}) в {folder}");

            LoadProjectsFromDisk(notifySubscribers: true); // -> OnProjectsLoaded -> сетка и детали обновятся
            return true;
        }
        catch (Exception e) { Debug.LogError($"💥 UpdateProject: {e.Message}"); return false; }
    }

    private static bool SamePath(string a, string b)
        => !string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b) &&
           string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);

    public bool DeleteProject(string projectId)
    {
        if (string.IsNullOrEmpty(projectId)) return false;
        try
        {
            string folderPath = null;
            if (projectFolderCache.TryGetValue(projectId, out string cached) && Directory.Exists(cached))
                folderPath = cached;
            else
                foreach (var folder in Directory.GetDirectories(DatabaseRootPath))
                {
                    string jp = Path.Combine(folder, "project.json");
                    if (!File.Exists(jp)) continue;
                    var d = JsonUtility.FromJson<ProjectData>(File.ReadAllText(jp));
                    if (d != null && d.id == projectId) { folderPath = folder; break; }
                }
            if (folderPath == null) return false;

            Directory.Delete(folderPath, true);
            LoadProjectsFromDisk(notifySubscribers: true);
            return true;
        }
        catch (Exception e) { Debug.LogError($"DeleteProject: {e.Message}"); return false; }
    }

    private string GenerateSafeFolderName(string title, string version)
    {
        string raw = $"{title}_{version}";
        char[] invalid = Path.GetInvalidFileNameChars();
        string safe = new string(raw.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return safe.Replace(' ', '_').ToLowerInvariant();
    }

    private string ResolveFullPath(string folder, string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return "";
        string full = Path.Combine(folder, fileName);
        return File.Exists(full) ? full : "";
    }
}