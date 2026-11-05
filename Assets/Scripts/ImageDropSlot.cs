using UnityEngine;
using UnityEngine.UI;
using System.IO;
using SFB;

public class ImageDropSlot : MonoBehaviour
{
    [Header("Groups to Toggle")]
    [Tooltip("Группа объектов с рамкой и иконкой (видна по умолчанию)")]
    public GameObject frameGroup;

    [Tooltip("Группа объектов с RawImage (скрыта по умолчанию)")]
    public GameObject previewGroup;

    [Header("Material Settings")]
    [Tooltip("Материал со скруглением углов (Rounded Corners). Если пусто — картинка будет прямоугольной.")]
    public Material roundedMaterial;

    // Ссылка конкретно на компонент RawImage внутри previewGroup
    private RawImage targetRawImage;

    public string LoadedFilePath { get; private set; } = "";
    public bool IsLoaded => !string.IsNullOrEmpty(LoadedFilePath);

    void Start()
    {
        // 1. Ищем RawImage автоматически внутри группы превью
        if (previewGroup != null)
        {
            targetRawImage = previewGroup.GetComponentInChildren<RawImage>();
        }

        // 2. Гарантируем начальное состояние: Рамка ВКЛ, Картинка ВЫКЛ
        ShowPlaceholder();

        // 3. Привязываем клик к специальной зоне ClickZone
        BindClickZone();
    }

    /// <summary>
    /// Поиск и привязка обработчика клика к объекту 'ClickZone'
    /// </summary>
    private void BindClickZone()
    {
        Transform clickTrans = transform.Find("ClickZone");

        if (clickTrans != null)
        {
            Button btn = clickTrans.GetComponent<Button>();
            if (btn != null)
            {
                // Очищаем старые слушатели на случай перезапуска
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnClickSelectFile);
                Debug.Log($"✅ [{gameObject.name}] Кнопка ClickZone успешно привязана!");
            }
        }
        else
        {
            // Запасной вариант: ищем любую кнопку среди детей
            Button fallbackBtn = GetComponentInChildren<Button>();
            if (fallbackBtn != null)
            {
                fallbackBtn.onClick.RemoveAllListeners();
                fallbackBtn.onClick.AddListener(OnClickSelectFile);
                Debug.LogWarning($"⚠️ [{gameObject.name}] Не найден 'ClickZone', использована первая попавшаяся кнопка.");
            }
            else
            {
                Debug.LogError($"❌ [{gameObject.name}] НЕ НАЙДЕНО НИ ОДНОЙ КНОПКИ ДЛЯ КЛИКА! Создайте объект 'ClickZone'.");
            }
        }
    }

    private void OnClickSelectFile()
    {
        // Вызываем системный диалог выбора файла через StandaloneFileBrowser

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Выберите изображение", "", "png;jpg;jpeg", false);

        if (paths != null && paths.Length > 0)
        {
            LoadFromFile(paths[0]);
        }
#else
            Debug.LogWarning("Диалог выбора файлов доступен только в Windows билде.");
#endif
    }

    public void LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath)) return;

        try
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            Texture2D tex = new Texture2D(2, 2);

            if (tex.LoadImage(bytes))
            {
                LoadedFilePath = filePath;

                // 1. Ставим текстуру
                if (targetRawImage != null)
                {
                    targetRawImage.texture = tex;
                    targetRawImage.color = Color.white;

                    // 2. ПРИМЕНЯЕМ МАТЕРИАЛ СО СКРУГЛЕНИЕМ (если задан)
                    if (roundedMaterial != null)
                    {
                        targetRawImage.material = roundedMaterial;
                    }
                }

                // 3. Переключаем видимость групп
                ShowPreview();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка загрузки файла {filePath}: {e.Message}");
        }
    }

    public void ClearSlot()
    {
        LoadedFilePath = "";

        // Чистим текстуру
        if (targetRawImage != null)
        {
            targetRawImage.texture = null;
            // Возвращаем дефолтный материал, чтобы не осталось артефактов
            targetRawImage.material = null;
        }

        // Возвращаемся к заглушке
        ShowPlaceholder();
    }

    // --- МЕТОДЫ ПЕРЕКЛЮЧЕНИЯ СОСТОЯНИЙ ---

    private void ShowPlaceholder()
    {
        // Показываем рамку, скрываем контейнер с картинкой
        if (frameGroup != null) frameGroup.SetActive(true);
        if (previewGroup != null) previewGroup.SetActive(false);
    }

    private void ShowPreview()
    {
        // Скрываем рамку, показываем контейнер с картинкой
        if (frameGroup != null) frameGroup.SetActive(false);
        if (previewGroup != null) previewGroup.SetActive(true);
    }
}