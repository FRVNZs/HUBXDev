using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject addProjectPanel;

    [Header("Buttons")]
    [Tooltip("Перетащи сюда объект AddIcon из Hierarchy")]
    [SerializeField] private Button addButton;

    void Start()
    {
        if (addButton != null)
        {
            addButton.onClick.RemoveAllListeners();
            addButton.onClick.AddListener(ShowAddProjectForm);
            Debug.Log("✅ Кнопка AddIcon успешно связана через ссылку!");
        }
        else Debug.LogError("Не назначена ссылка на кнопку AddIcon в инспекторе UIManager!");

        if (addProjectPanel != null)
        {
            Transform cancelBtnTrans = addProjectPanel.transform.Find("ButtonsRow/Button (Отмена)");
            if (cancelBtnTrans != null)
            {
                Button btn = cancelBtnTrans.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(HideAddProjectForm);
                    Debug.Log("✅ Кнопка Отмены успешно привязана к HideAddProjectForm");
                }
                else Debug.LogError("На объекте 'Button (Отмена)' нет компонента Button!");
            }
            else Debug.LogWarning("Не найден путь к кнопке Отмены. Проверь структуру Hierarchy внутри AddProjectModal.");
        }
    }

    public void ShowAddProjectForm()
    {
        if (addProjectPanel == null) return;
        // Единый путь открытия - через контроллер: он сам чистит поля и сам анимирует показ
        // (PanelTransition.ShowWithAnimation). Инициализация внутри ShowForm.
        var controller = addProjectPanel.GetComponent<AddProjectFormController>();
        if (controller != null) controller.ShowForm();
        else addProjectPanel.SetActive(true); // фолбэк, если контроллер вдруг потерян
    }

    public void HideAddProjectForm()
    {
        if (addProjectPanel == null) return;
        var controller = addProjectPanel.GetComponent<AddProjectFormController>();
        if (controller != null) controller.HideForm(); // анимирует скрытие
        else addProjectPanel.SetActive(false);
    }
}