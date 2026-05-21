using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DigitalLegacy.UI.Sizing;

/// <summary>
/// Componente para accesorios instanciados en pantalla (UI).
/// Soporta drag (touch/mouse), selección, eliminación con un botón X,
/// y un modo de resize controlado por toggle.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class DraggableAccessory : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private AccessoryEditorPanel panelParent;

    private GameObject deleteButton;
    private Toggle resizeToggle;
    private GameObject resizeToggleRoot;
    private uResize resizeComponent;
    private GameObject rotateHandleRoot;
    private bool isRotating = false;
    private float rotateStartAngle = 0f;
    private float rotateStartZ = 0f;

    private bool isSelected = false;
    private bool isResizeMode = false;
    private bool isDragging = false;

    [Header("Comportamiento")]
    public bool autoEnterResizeModeOnClick = true;
    public bool createResizeToggleAutomatically = true;
    public bool createRotateHandleAutomatically = true;

    public void Setup(AccessoryEditorPanel parent)
    {
        panelParent = parent;
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();

        resizeComponent = GetComponent<uResize>();
        if (resizeComponent == null)
            resizeComponent = gameObject.AddComponent<uResize>();

        // Permitir resize por todos los bordes para que sea intuitivo en touch.
        resizeComponent.AllowResizeFromLeft = true;
        resizeComponent.AllowResizeFromRight = true;
        resizeComponent.AllowResizeFromTop = true;
        resizeComponent.AllowResizeFromBottom = true;
        resizeComponent.AllowResizeFromTopLeft = true;
        resizeComponent.AllowResizeFromTopRight = true;
        resizeComponent.AllowResizeFromBottomLeft = true;
        resizeComponent.AllowResizeFromBottomRight = true;

        // Arranca con resize desactivado sin deshabilitar el componente,
        // para evitar que uResize reconstruya listeners destruidos al reactivarse.
        SetResizeListenerState(false);
    }

    private void Update()
    {
        if (!isResizeMode || rectTransform == null)
            return;

        if (ClickedOutsideThisFrame())
        {
            SetResizeMode(false);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (panelParent != null)
            panelParent.SelectInstance(this.gameObject);

        if (autoEnterResizeModeOnClick)
        {
            SetResizeMode(true);
        }
    }

    public void SetSelected(bool sel)
    {
        isSelected = sel;

        if (isSelected)
        {
            if (isResizeMode)
            {
                ShowDeleteButton();
                ShowRotateHandle();
            }
        }
        else
        {
            HideDeleteButton();
            HideResizeToggle();
            HideRotateHandle();
            SetResizeMode(false);
        }
    }

    public void SetResizeMode(bool enabled)
    {
        isResizeMode = enabled;

        SetResizeListenerState(enabled);

        if (!isSelected)
        {
            HideDeleteButton();
            HideRotateHandle();
            HideResizeToggle();
            return;
        }

        if (enabled)
        {
            ShowDeleteButton();
            ShowRotateHandle();
        }
        else
        {
            HideDeleteButton();
            HideRotateHandle();
        }

        HideResizeToggle();

        if (resizeToggle != null && resizeToggle.isOn != enabled)
            resizeToggle.isOn = enabled;
    }

    private void SetResizeListenerState(bool enabled)
    {
        if (resizeComponent == null)
            return;

        resizeComponent.AllowResizeFromLeft = enabled;
        resizeComponent.AllowResizeFromRight = enabled;
        resizeComponent.AllowResizeFromTop = enabled;
        resizeComponent.AllowResizeFromBottom = enabled;
        resizeComponent.AllowResizeFromTopLeft = enabled;
        resizeComponent.AllowResizeFromTopRight = enabled;
        resizeComponent.AllowResizeFromBottomLeft = enabled;
        resizeComponent.AllowResizeFromBottomRight = enabled;
        resizeComponent.UpdateListeners();
    }

    private bool ClickedOutsideThisFrame()
    {
        if (Input.touchCount > 0)
        {
            foreach (var touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    return !RectTransformUtility.RectangleContainsScreenPoint(rectTransform, touch.position, rootCanvas != null ? rootCanvas.worldCamera : null);
                }
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            return !RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, rootCanvas != null ? rootCanvas.worldCamera : null);
        }

        return false;
    }

    private void ShowDeleteButton()
    {
        if (deleteButton != null)
        {
            deleteButton.SetActive(true);
            return;
        }

        CreateDeleteButton();
    }

    private void HideDeleteButton()
    {
        if (deleteButton != null)
            deleteButton.SetActive(false);
    }

    private void CreateDeleteButton()
    {
        deleteButton = new GameObject("DeleteButton", typeof(RectTransform), typeof(Button), typeof(Image));
        deleteButton.transform.SetParent(transform, false);

        var rt = deleteButton.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-8f, -8f);
        rt.sizeDelta = new Vector2(32f, 32f);

        var img = deleteButton.GetComponent<Image>();
        img.color = new Color(0.9f, 0.2f, 0.2f, 0.9f);

        var btn = deleteButton.GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            if (panelParent != null) panelParent.RemoveInstance(this.gameObject);
            else Destroy(this.gameObject);
        });

        var textGO = new GameObject("X", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(deleteButton.transform, false);
        var txtRt = textGO.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        var txt = textGO.GetComponent<Text>();
        txt.text = "X";
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private void ShowResizeToggle()
    {
        if (resizeToggleRoot != null)
            resizeToggleRoot.SetActive(false);
    }

    private void HideResizeToggle()
    {
        if (resizeToggleRoot != null)
            resizeToggleRoot.SetActive(false);
    }

    private void ShowRotateHandle()
    {
        if (!createRotateHandleAutomatically)
            return;

        if (rotateHandleRoot != null)
        {
            rotateHandleRoot.SetActive(true);
            return;
        }

        CreateRotateHandle();
    }

    private void HideRotateHandle()
    {
        if (rotateHandleRoot != null)
            rotateHandleRoot.SetActive(false);
    }

    private void CreateResizeToggle()
    {
        resizeToggleRoot = new GameObject("ResizeToggle", typeof(RectTransform), typeof(Toggle), typeof(Image));
        resizeToggleRoot.transform.SetParent(transform, false);

        var rt = resizeToggleRoot.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(8f, -8f);
        rt.sizeDelta = new Vector2(36f, 36f);

        var bg = resizeToggleRoot.GetComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        resizeToggle = resizeToggleRoot.GetComponent<Toggle>();

        var checkmarkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkmarkGO.transform.SetParent(resizeToggleRoot.transform, false);
        var checkRt = checkmarkGO.GetComponent<RectTransform>();
        checkRt.anchorMin = new Vector2(0.15f, 0.15f);
        checkRt.anchorMax = new Vector2(0.85f, 0.85f);
        checkRt.offsetMin = Vector2.zero;
        checkRt.offsetMax = Vector2.zero;

        var checkImg = checkmarkGO.GetComponent<Image>();
        checkImg.color = new Color(0.2f, 0.8f, 0.2f, 0.95f);

        resizeToggle.graphic = checkImg;
        resizeToggle.targetGraphic = bg;
        resizeToggle.isOn = false;
        resizeToggle.onValueChanged.AddListener(SetResizeMode);
    }

    private void CreateRotateHandle()
    {
        rotateHandleRoot = new GameObject("RotateHandle", typeof(RectTransform), typeof(Image), typeof(EventTrigger));
        rotateHandleRoot.transform.SetParent(transform, false);

        var rt = rotateHandleRoot.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-8f, 8f);
        rt.sizeDelta = new Vector2(32f, 32f);

        var img = rotateHandleRoot.GetComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 0.9f, 0.9f);

        var trigger = rotateHandleRoot.GetComponent<EventTrigger>();
        trigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();

        AddEventTrigger(trigger, EventTriggerType.PointerDown, OnRotatePointerDown);
        AddEventTrigger(trigger, EventTriggerType.BeginDrag, OnRotateBeginDrag);
        AddEventTrigger(trigger, EventTriggerType.Drag, OnRotateDrag);
        AddEventTrigger(trigger, EventTriggerType.EndDrag, OnRotateEndDrag);
    }

    private void AddEventTrigger(EventTrigger trigger, EventTriggerType type, System.Action<PointerEventData> callback)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(ev => callback((PointerEventData)ev));
        trigger.triggers.Add(entry);
    }

    private void OnRotatePointerDown(PointerEventData eventData)
    {
        if (panelParent != null)
            panelParent.SelectInstance(this.gameObject);
    }

    private void OnRotateBeginDrag(PointerEventData eventData)
    {
        if (rectTransform == null) return;

        isRotating = true;
        isDragging = false;

        rotateStartZ = rectTransform.localEulerAngles.z;
        rotateStartAngle = GetPointerAngle(eventData);
    }

    private void OnRotateDrag(PointerEventData eventData)
    {
        if (!isRotating || rectTransform == null) return;

        float currentAngle = GetPointerAngle(eventData);
        float delta = Mathf.DeltaAngle(rotateStartAngle, currentAngle);

        rectTransform.localEulerAngles = new Vector3(0f, 0f, rotateStartZ + delta);
    }

    private void OnRotateEndDrag(PointerEventData eventData)
    {
        isRotating = false;
    }

    private float GetPointerAngle(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localPoint);
        return Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isRotating) return;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        if (isRotating)
            return;

        if (rootCanvas == null)
            return;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rootCanvas.transform, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }
}
