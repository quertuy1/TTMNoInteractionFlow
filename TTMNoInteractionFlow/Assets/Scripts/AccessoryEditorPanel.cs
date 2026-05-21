using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Panel superpuesto que permite seleccionar accesorios por escena,
/// agregar instancias draggables sobre la foto y finalizar con "Listo".
/// </summary>
public class AccessoryEditorPanel : MonoBehaviour
{
    public static AccessoryEditorPanel Instance;

    public System.Action OnDoneRequested;
    public System.Action OnBackRequested;

    [Header("UI References")]
    public GameObject panelRoot; // panel que se muestra/oculta
    public RawImage photoBackground; // muestra la foto tomada
    public Image accessoryPreviewImage; // preview del accesorio seleccionado
    public Button leftButton;
    public Button rightButton;
    public Button addButton;
    public Button doneButton;
    public Button backButton;

    [Header("Instanciado")]
    public RectTransform instancesParent; // donde se instancian los accesorios (debe ser parte del Canvas)
    public int maxInstances = 6;

    [Header("Prefabs")]
    public GameObject accessoryInstancePrefab; // prefab base para instanciar accesorios

    [Header("Sprites")]
    public AccessorySpriteLibrary spriteLibrary;

    private Sprite[] currentCatalog = null;
    private int catalogIndex = 0;
    private List<GameObject> instances = new List<GameObject>();
    private GameObject selectedInstance = null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        if (leftButton != null) leftButton.onClick.AddListener(OnLeft);
        if (rightButton != null) rightButton.onClick.AddListener(OnRight);
        if (addButton != null) addButton.onClick.AddListener(OnAdd);
        if (doneButton != null) doneButton.onClick.AddListener(OnDone);
        if (backButton != null) backButton.onClick.AddListener(OnBack);

        if (panelRoot != null) panelRoot.SetActive(false);

        LoadCatalogFromLibrary();
    }

    private void OnDisable()
    {
        if (leftButton != null) leftButton.onClick.RemoveListener(OnLeft);
        if (rightButton != null) rightButton.onClick.RemoveListener(OnRight);
        if (addButton != null) addButton.onClick.RemoveListener(OnAdd);
        if (doneButton != null) doneButton.onClick.RemoveListener(OnDone);
        if (backButton != null) backButton.onClick.RemoveListener(OnBack);
    }

    public void SetBaseTexture(Texture2D tex)
    {
        if (photoBackground != null)
        {
            if (!photoBackground.gameObject.activeSelf)
                photoBackground.gameObject.SetActive(true);

            Color c = photoBackground.color;
            c.a = 1f;
            photoBackground.color = c;

            // mostrar la foto como textura de fondo
            photoBackground.texture = tex;
        }
    }

    public void SetEditingActive(bool active)
    {
        if (panelRoot != null) panelRoot.SetActive(active);
    }

    public void SetSpriteLibrary(AccessorySpriteLibrary library)
    {
        spriteLibrary = library;
        LoadCatalogFromLibrary();
        ClearAllInstances();
    }

    private void LoadCatalogFromLibrary()
    {
        currentCatalog = spriteLibrary != null ? spriteLibrary.GetSprites() : null;
        catalogIndex = 0;
        UpdateAccessoryPreview();
    }

    private void UpdateAccessoryPreview()
    {
        if (accessoryPreviewImage == null) return;
        if (currentCatalog == null || currentCatalog.Length == 0)
        {
            accessoryPreviewImage.sprite = null;
            accessoryPreviewImage.enabled = false;
            return;
        }

        accessoryPreviewImage.enabled = true;
        accessoryPreviewImage.sprite = currentCatalog[catalogIndex];
    }

    private void OnLeft()
    {
        if (currentCatalog == null || currentCatalog.Length == 0) return;
        catalogIndex = (catalogIndex - 1 + currentCatalog.Length) % currentCatalog.Length;
        UpdateAccessoryPreview();
    }

    private void OnRight()
    {
        if (currentCatalog == null || currentCatalog.Length == 0) return;
        catalogIndex = (catalogIndex + 1) % currentCatalog.Length;
        UpdateAccessoryPreview();
    }

    private void OnAdd()
    {
        if (currentCatalog == null || currentCatalog.Length == 0) return;
        if (instances.Count >= maxInstances) return;

        Sprite s = currentCatalog[catalogIndex];
        CreateInstanceForSprite(s);
    }

    private void CreateInstanceForSprite(Sprite s)
    {
        if (accessoryInstancePrefab == null)
        {
            Debug.LogError("AccessoryEditorPanel: accessoryInstancePrefab no asignado.");
            return;
        }

        GameObject go = Instantiate(accessoryInstancePrefab, instancesParent, false);
        go.name = "AccessoryInstance";

        var img = go.GetComponent<UnityEngine.UI.Image>();
        if (img != null)
        {
            img.sprite = s;
            img.SetNativeSize();
        }

        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(s.rect.width, s.rect.height);
        }

        var drag = go.GetComponent<DraggableAccessory>();
        if (drag != null) drag.Setup(this);

        instances.Add(go);
        SelectInstance(go);
    }

    public void SelectInstance(GameObject go)
    {
        if (selectedInstance == go) return;
        if (selectedInstance != null)
        {
            var prev = selectedInstance.GetComponent<DraggableAccessory>();
            if (prev != null) prev.SetSelected(false);
        }

        selectedInstance = go;

        if (selectedInstance != null)
        {
            var cur = selectedInstance.GetComponent<DraggableAccessory>();
            if (cur != null) cur.SetSelected(true);
        }
    }

    public void DeselectCurrent()
    {
        SelectInstance(null);
    }

    public void ClearAllInstances()
    {
        foreach (var inst in instances)
        {
            if (inst != null) Destroy(inst);
        }
        instances.Clear();
        selectedInstance = null;
    }

    public void ResetEditor()
    {
        ClearAllInstances();
        if (photoBackground != null)
        {
            photoBackground.texture = null;
            Color c = photoBackground.color;
            c.a = 0f;
            photoBackground.color = c;
            if (photoBackground.gameObject.activeSelf)
                photoBackground.gameObject.SetActive(false);
        }
        SetEditingActive(false);
    }

    private void OnDone()
    {
        OnDoneRequested?.Invoke();
    }

    private void OnBack()
    {
        OnBackRequested?.Invoke();
    }

    public void RemoveInstance(GameObject go)
    {
        if (instances.Contains(go)) instances.Remove(go);
        if (selectedInstance == go) selectedInstance = null;
        if (go != null) Destroy(go);
    }
}
