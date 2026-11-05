using UnityEngine;

public class DetailsPanelController : MonoBehaviour
{
    [Header("Кнопки внутри панели")]
    [SerializeField] private ButtonEffect backButtonEffect; // Сюда перетащи компонент ButtonEffect с кнопки "Назад"

    void OnEnable()
    {
        // Когда панель включается (SetActive(true)), немедленно проверяем мышь
        if (backButtonEffect != null)
        {
            backButtonEffect.SyncWithMousePosition();
        }
    }
}