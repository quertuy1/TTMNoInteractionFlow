using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Handler para los botones de selección de época.
/// Asignar a cada botón de la UI con el índice correspondiente:
/// 0 = Años 50s, 1 = Años 70s, 2 = Años 90s, 3 = Actual
/// 
/// Incluye feedback visual con animación suave de escala
/// e indicador de selección.
/// </summary>
[RequireComponent(typeof(Button))]
public class EraButtonHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Índice de la época: 0 = 50s, 1 = 70s, 2 = 90s, 3 = Actual")]
    public int eraIndex;

    [Header("Feedback Visual")]
    [Tooltip("Objeto indicador que se activa al seleccionar esta época")]
    public GameObject selectedIndicator;

    [Tooltip("Escala al estar seleccionado")]
    public float selectedScale = 1.05f;

    [Tooltip("Escala al hacer hover")]
    public float hoverScale = 1.03f;

    [Tooltip("Velocidad de la animación de escala")]
    public float scaleSpeed = 8f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private Button button;
    private bool isSelected = false;
    private bool isHovered = false;

    // Registro estático para coordinar el estado visual entre botones
    private static EraButtonHandler[] allButtons;
    private static int selectedEraIndex = -1;

    private void Awake()
    {
        button = GetComponent<Button>();
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void Start()
    {
        button.onClick.AddListener(OnClick);
        // Refrescar lista de botones
        allButtons = FindObjectsByType<EraButtonHandler>(FindObjectsSortMode.None);

        if (selectedIndicator != null)
            selectedIndicator.SetActive(false);
    }

    private void Update()
    {
        // Animación suave de escala
        if (transform.localScale != targetScale)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                targetScale,
                Time.deltaTime * scaleSpeed
            );
        }
    }

    /// <summary>
    /// Llamado cuando el usuario presiona este botón de época.
    /// </summary>
    public void OnClick()
    {
        if (FilterManager.Instance == null)
        {
            Debug.LogError("EraButtonHandler: FilterManager no encontrado.");
            return;
        }

        FilterManager.Instance.SetEra(eraIndex);
        selectedEraIndex = eraIndex;
        UpdateAllButtonVisuals();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (!isSelected)
        {
            targetScale = originalScale * hoverScale;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (!isSelected)
        {
            targetScale = originalScale;
        }
    }

    /// <summary>
    /// Actualiza el estado visual de todos los botones.
    /// </summary>
    private void UpdateAllButtonVisuals()
    {
        if (allButtons == null)
            allButtons = FindObjectsByType<EraButtonHandler>(FindObjectsSortMode.None);

        foreach (var btn in allButtons)
        {
            if (btn == null) continue;

            btn.isSelected = (btn.eraIndex == selectedEraIndex);

            if (btn.selectedIndicator != null)
                btn.selectedIndicator.SetActive(btn.isSelected);

            if (btn.isSelected)
            {
                btn.targetScale = btn.originalScale * btn.selectedScale;
            }
            else
            {
                btn.targetScale = btn.isHovered
                    ? btn.originalScale * btn.hoverScale
                    : btn.originalScale;
            }
        }
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }
}
