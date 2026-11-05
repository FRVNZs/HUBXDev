using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Локальный пользовательский слой поверх каталога репозиториев: избранное + история заходов.
/// НЕ трогает ProjectData/базу (ProjectManager) — хранит только id и таймстампы в PlayerPrefs.
/// Статик-класс: грузится лениво при первом обращении, вешать на сцену не нужно,
/// гонка порядка Awake между стором и панелями исключена по построению.
/// </summary>
public static class UserDataStore
{
    [Serializable] private struct VisitEntry { public string id; public long ticks; }
    [Serializable]
    private struct Payload
    {
        public List<string> favorites;
        public List<VisitEntry> history;
    }

    private const string KEY = "GitBank.UserData.v1";
    private const int HISTORY_CAP = 200;

    private static readonly HashSet<string> favorites = new HashSet<string>();
    private static readonly List<VisitEntry> history = new List<VisitEntry>();
    private static bool loaded;

    private static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        var json = PlayerPrefs.GetString(KEY, "");
        if (string.IsNullOrEmpty(json)) return;
        try
        {
            var p = JsonUtility.FromJson<Payload>(json);
            if (p.favorites != null) foreach (var id in p.favorites) if (!string.IsNullOrEmpty(id)) favorites.Add(id);   // было p?.favorites
            if (p.history != null) history.AddRange(p.history);                                                          // было p?.history
        }
        catch (Exception e) { Debug.LogWarning($"[UserDataStore] не удалось прочитать стор: {e.Message}"); }
    }

    // --- ИЗБРАННОЕ ---
    public static bool IsFavorite(string id) { EnsureLoaded(); return !string.IsNullOrEmpty(id) && favorites.Contains(id); }

    public static bool ToggleFavorite(string id)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(id)) return false;
        bool now;
        if (favorites.Contains(id)) { favorites.Remove(id); now = false; }
        else { favorites.Add(id); now = true; }
        Save();
        return now;
    }

    // <<< ДОБАВЛЕНО: явное удаление из избранного (для крестика на карточке).
    // Не зависит от текущего состояния, в отличие от ToggleFavorite.
    public static void RemoveFavorite(string id)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(id)) return;
        if (favorites.Remove(id)) Save();   // Save только если реально было что убирать
    }

    // --- ИСТОРИЯ (уникальные проекты, последние сверху; повторный заход поднимает наверх) ---
    public static void RecordVisit(string id)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(id)) return;
        long t = DateTime.UtcNow.Ticks;
        for (int i = 0; i < history.Count; i++)
        {
            if (history[i].id == id) { history[i] = new VisitEntry { id = id, ticks = t }; MoveToFront(i); Save(); return; }
        }
        history.Insert(0, new VisitEntry { id = id, ticks = t });
        if (history.Count > HISTORY_CAP) history.RemoveAt(history.Count - 1);
        Save();
    }

    private static void MoveToFront(int i) { var e = history[i]; history.RemoveAt(i); history.Insert(0, e); }

    // --- СРЕЗЫ ДЛЯ ГЕНЕРАТОРОВ (ProjectData в нужном порядке) ---
    public static List<ProjectData> GetFavorites(List<ProjectData> catalog)
    {
        EnsureLoaded();
        var byId = IndexById(catalog);
        var res = new List<ProjectData>(favorites.Count);
        foreach (var id in favorites) if (byId.TryGetValue(id, out var p)) res.Add(p);
        return res;
    }

    public static List<ProjectData> GetHistory(List<ProjectData> catalog)
    {
        EnsureLoaded();
        var byId = IndexById(catalog);
        var res = new List<ProjectData>(history.Count);
        for (int i = 0; i < history.Count; i++) if (byId.TryGetValue(history[i].id, out var p)) res.Add(p);
        return res;
    }

    private static Dictionary<string, ProjectData> IndexById(List<ProjectData> catalog)
    {
        var d = new Dictionary<string, ProjectData>(catalog?.Count ?? 0);
        if (catalog != null) for (int i = 0; i < catalog.Count; i++) { var p = catalog[i]; if (p != null && !string.IsNullOrEmpty(p.id) && !d.ContainsKey(p.id)) d[p.id] = p; }
        return d;
    }

    private static void Save()
    {
        var p = new Payload { favorites = new List<string>(favorites), history = new List<VisitEntry>(history) };
        PlayerPrefs.SetString(KEY, JsonUtility.ToJson(p));
        PlayerPrefs.Save();
    }
}