using System;
using System.Collections.Generic;   // <<< ДОБАВЛЕНО (для List<ProjectData> в GetSourceList)
using UnityEngine;
using TMPro;

public class GridGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject projectCardPrefab;

    [Header("Navigation")]
    [SerializeField] private GameObject detailsPanel;
    // <<< ДОБАВЛЕНО: единый переключатель вкладок. Пусто = найдётся через Find
    // (как cascade ниже), поэтому тянуть в инспекторе НЕ обязательно.
    [Tooltip("TabSwitcher с GameObject (Project Manager). Пусто -> резолвится через Find.")]
    [SerializeField] private TabSwitcher tabSwitcher;

    // КАСКАД главного экрана. Можно оставить пустым -> возьмётся через Find.
    // Если раннер нигде не навешан -> каскада нет, карточки появятся сразу (без пустого экрана).
    [Header("Cascade")]
    [SerializeField] private ScreenCascadeRunner cascade;

    // <<< ДОБАВЛЕНО: источник карточек для этой вкладки.
    // HomeBoard = All (как было), FavoriteBoard = Favorites. History оставлен на будущее, не включаем.
    [Header("Source")]
    [SerializeField] private Source source = Source.All;
    public enum Source { All, Favorites, History }
    [Tooltip("Крестик удаления показываем только на вкладке избранного.")]
    public bool ShowsDeleteButton => source == Source.Favorites;

    [Header("Search")]
    [Tooltip("Поле «Поиск репозитория». Одно на сцену — перетащи его в ОБА генератора (HomeBoard и FavoriteBoard).")]
    [SerializeField] private TMP_InputField searchField;
    private string searchQuery = "";

    void OnEnable()
    {
        if (tabSwitcher == null) tabSwitcher = FindFirstObjectByType<TabSwitcher>();
        if (searchField != null) searchField.onValueChanged.AddListener(ApplySearch);   // <<< ДОБАВЛЕНО
        ProjectManager.OnProjectsLoaded += GenerateCards;
        GenerateCards();
    }
    void OnDisable()
    {
        if (searchField != null) searchField.onValueChanged.RemoveListener(ApplySearch); // <<< ДОБАВЛЕНО
        ProjectManager.OnProjectsLoaded -= GenerateCards;
    }

    public void GenerateCards()
    {
        if (contentParent == null) return;
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        // Определяем каскад ОДИН раз. useCascade = false => карточки видимы сразу.
        var casc = cascade != null ? cascade : FindFirstObjectByType<ScreenCascadeRunner>();
        bool useCascade = casc != null;

        if (ProjectManager.Instance == null || ProjectManager.Instance.AllProjects == null)
        {
            if (useCascade) casc.OnCardsGenerated();   // пустой список -> раннер пересоберёт статику
            return;
        }

        // <<< ИЗМЕНЕНО: БЫЛО foreach (var project in ProjectManager.Instance.AllProjects)
        foreach (var project in GetSourceList(ProjectManager.Instance.AllProjects))
        {
            GameObject cardObj = Instantiate(projectCardPrefab, contentParent);
            CardClickHandler handler = cardObj.GetComponent<CardClickHandler>();
            if (handler != null) handler.Setup(project, this);
            else Debug.LogError("Не найден CardClickHandler на префабе карточки!");

            // БЫЛО: if (doAnim) { ... anim.Configure(...); anim.Play(index * staggerStep); } index++;
            // СТАЛО: гасим до первого кадра ТОЛЬКО если есть каскад, который разбудит.
            if (useCascade)
            {
                var cg = cardObj.GetComponent<CanvasGroup>();
                if (cg == null) cg = cardObj.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
            }
        }

        if (useCascade) casc.OnCardsGenerated();   // хук в самом конце, после всех Instantiate
        ApplySearch(searchQuery);
    }

    // <<< ДОБАВЛЕНО: единственный новый метод. default = твоя прежняя логия (AllProjects).
    private List<ProjectData> GetSourceList(List<ProjectData> catalog)
    {
        switch (source)
        {
            case Source.Favorites: return UserDataStore.GetFavorites(catalog);
            case Source.History: return UserDataStore.GetHistory(catalog);   // на будущее, не включаем
            default: return catalog;                              // All = как было
        }
    }

    // <<< ДОБАВЛЕНО: крестик на карточке избранного -> убрать id из стора и пересобрать ЭТУ сетку.
    // GenerateCards уничтожит детей contentParent (включая текущую карточку) — это безопасно,
    // т.к. вызов идёт из onClick-колбэка, а Destroy отложен до конца кадра.
    public void RemoveFavorite(string id)
    {
        UserDataStore.RemoveFavorite(id);
        GenerateCards();
    }

    // Совпадаем по подстроке, регистронезависимо. Пустой запрос = показать все.
    public void ApplySearch(string query)
    {
        searchQuery = query ?? "";
        if (contentParent == null) return;
        bool empty = string.IsNullOrEmpty(searchQuery);
        foreach (Transform child in contentParent)
        {
            var h = child.GetComponent<CardClickHandler>();
            bool match = empty || (h != null && h.CurrentData != null &&
                h.CurrentData.title.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0);
            child.gameObject.SetActive(match);
        }
    }

    // ВХОД: светлый главный под входящими деталями дал бы белый -> гасим главный МГНОВЕННО
    // (это делает TabSwitcher.hideInstantly для не-целевых вкладок), детали проявляем из 0,
    // и СВЕРХУ запускаем каскад блоков 1..9 (превью = шаг 3, поэтому однократный shine-sweep
    // градиента отрабатывает в невидимости до его появления).
    public void OpenProjectDetails(ProjectData data)
    {
        Debug.Log($"Открываем детали: {data.title}");
        if (detailsPanel != null)
        {
            var dc = detailsPanel.GetComponentInChildren<DetailsController>(true);
            if (dc != null) dc.SetData(data);
            else Debug.LogError("[GRID] DetailsController не найден внутри detailsPanel.");
        }
        // <<< ЗАМЕНИЛО HardHide(mainListPanel)+SoftShow(detailsPanel): одна точка истины,
        // и она же гасит ВСЕ иконки навигации (DetailsInfo = вкладка без иконок).
        tabSwitcher.SwitchTo(detailsPanel);

        if (detailsPanel != null)
        {
            var runner = detailsPanel.GetComponentInChildren<DetailsCascadeRunner>(true);
            if (runner != null) runner.Play();
        }
    }

    public void CloseDetails()
    {
        Debug.Log("Возвращаемся к списку");
        // <<< ЗАМЕНИЛО SoftHide(detailsPanel)+SoftShow(mainListPanel): возврат строго на ту
        // вкладку, с которой вошли (lastTab внутри TabSwitcher), а не на панель этого генератора.
        // Каскад НЕ дёргаем здесь: GoBack включает вкладку входа -> OnEnable генератора ->
        // GenerateCards -> OnCardsGenerated. Один инициатор = нет двойного PlayAll.
        tabSwitcher.GoBack();
    }

    // <<< УДАЛЕНЫ HardHide / SoftShow / SoftHide: после перехода на TabSwitcher они не
    // вызываются нигде (всю видимость панелей и подсветку иконок держит TabSwitcher).
}