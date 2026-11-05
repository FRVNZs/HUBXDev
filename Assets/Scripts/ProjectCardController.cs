using UnityEngine;
using TMPro; // Для работы с текстом
using System.IO; // Для File.Exists и ReadAllBytes
using UnityEngine.UI; // Для Button

public class ProjectCardController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private RawImage cardImage;        // Используем RawImage для внешних картинок

    // Ссылка на кнопку (если она висит на этом же объекте или родителе)
    private Button interactableButton;

    // Данные проекта
    public ProjectData CurrentData { get; private set; }

    // Ссылка на генератор сетки, чтобы сообщить о клике
    private GridGenerator gridGeneratorRef;

    void Awake()
    {
        // Ищем кнопку при старте объекта
        interactableButton = GetComponent<Button>();
        if (interactableButton == null)
        {
            // Если кнопки нет на корне, ищем среди детей (на случай, если она внутри Mask/Card)
            interactableButton = GetComponentInChildren<Button>(true);
        }
    }

    /// <summary>
    /// Вызывается из GridGenerator после создания экземпляра
    /// </summary>
    public void Setup(ProjectData data, GridGenerator generator)
    {
        CurrentData = data;
        gridGeneratorRef = generator;

        // 1. Заполняем название
        if (titleText != null)
            titleText.text = data.title.ToUpper();

        // 2. Загружаем картинку обложки (coverImagePath)
        if (cardImage != null && !string.IsNullOrEmpty(data.coverImagePath))
        {
            Texture2D tex = LoadTextureFromFile(data.coverImagePath);
            if (tex != null) cardImage.texture = tex;
        }

        // 3. Настраиваем клик по кнопке
        if (interactableButton != null)
        {
            interactableButton.onClick.RemoveAllListeners(); // Чистим старые слушатели

            // Добавляем новый: при клике дергаем метод у родителя (генератора)
            interactableButton.onClick.AddListener(() => {
                if (gridGeneratorRef != null)
                {
                    gridGeneratorRef.OpenProjectDetails(CurrentData);
                }
            });
        }
        else
        {
            Debug.LogError($"[CARD CTRL] Не найдена кнопка Button в карточке '{data.title}'!");
        }
    }

    private Texture2D LoadTextureFromFile(string fullPath)
    {
        if (!File.Exists(fullPath)) return null;

        byte[] bytes = File.ReadAllBytes(fullPath);
        Texture2D tex = new Texture2D(2, 2);

        if (tex.LoadImage(bytes))
        {
            return tex;
        }
        return null;
    }
}