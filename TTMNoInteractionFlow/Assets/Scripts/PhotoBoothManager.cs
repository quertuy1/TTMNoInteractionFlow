using UnityEngine;

public class PhotoBoothManager : MonoBehaviour
{
    [Header("References")]
    public ScreenshotManager screenshotManager;
    public AccessoryEditorPanel accessoryPanel;
    public CameraManager cameraManager;
    public SceneFlowManager sceneFlowManager;
    public PhotoStorage photoStorage;

    [Header("Capture Area (Square)")]
    public RectTransform captureArea;

    [Header("Scene Flow")]
    public string nextSceneName;

    [Header("Settings")]
    public bool hideLiveCameraOnEdit = true;

    [Header("Final Capture UI")]
    public CanvasGroup accessoryUiGroup;

    private bool isEditing = false;

    private void Awake()
    {
        EnsureReferences();
    }

    private void OnEnable()
    {
        if (accessoryPanel != null)
        {
            accessoryPanel.OnDoneRequested += HandleDone;
            accessoryPanel.OnBackRequested += HandleBack;
        }
    }

    private void OnDisable()
    {
        if (accessoryPanel != null)
        {
            accessoryPanel.OnDoneRequested -= HandleDone;
            accessoryPanel.OnBackRequested -= HandleBack;
        }
    }

    public void TakePhotoPressed()
    {
        if (isEditing) return;

        EnsureReferences();

        if (screenshotManager == null || accessoryPanel == null)
        {
            Debug.LogWarning("PhotoBoothManager: faltan referencias para tomar foto.");
            return;
        }

        screenshotManager.TakePhotoForEditFromCameraArea(captureArea, HandleBaseCaptured);
    }

    private void HandleBaseCaptured(Texture2D baseTexture)
    {
        if (baseTexture == null) return;

        isEditing = true;

        if (hideLiveCameraOnEdit) SetLiveCameraVisible(false);

        accessoryPanel.SetBaseTexture(baseTexture);
        accessoryPanel.SetEditingActive(true);
    }

    private void HandleBack()
    {
        if (!isEditing) return;

        accessoryPanel.ResetEditor();
        isEditing = false;

        if (hideLiveCameraOnEdit) SetLiveCameraVisible(true);
    }

    private void HandleDone()
    {
        if (!isEditing) return;

        StartCoroutine(CaptureEditedAndSave());
    }

    private System.Collections.IEnumerator CaptureEditedAndSave()
    {
        float prevAlpha = 1f;
        bool prevInteractable = true;
        bool prevBlocksRaycasts = true;
        if (accessoryUiGroup != null)
        {
            prevAlpha = accessoryUiGroup.alpha;
            prevInteractable = accessoryUiGroup.interactable;
            prevBlocksRaycasts = accessoryUiGroup.blocksRaycasts;
            accessoryUiGroup.alpha = 0f;
            accessoryUiGroup.interactable = false;
            accessoryUiGroup.blocksRaycasts = false;
        }

        yield return new WaitForEndOfFrame();

        Texture2D edited = CaptureSquareFromScreen();
        if (edited == null)
        {
            edited = ScreenCapture.CaptureScreenshotAsTexture();
        }

        if (accessoryUiGroup != null)
        {
            accessoryUiGroup.alpha = prevAlpha;
            accessoryUiGroup.interactable = prevInteractable;
            accessoryUiGroup.blocksRaycasts = prevBlocksRaycasts;
        }

        EnsureReferences();

        if (photoStorage != null)
        {
            int index = photoStorage.GetNextIndex();
            if (photoStorage.maxPhotos <= 0 || index < photoStorage.maxPhotos)
            {
                photoStorage.SetPhoto(index, edited);
            }
            else
            {
                Debug.LogWarning("PhotoBoothManager: maxPhotos alcanzado, se sobrescribe el ultimo.");
                photoStorage.SetPhoto(photoStorage.maxPhotos - 1, edited);
            }
        }
        else
        {
            Debug.LogWarning("PhotoBoothManager: PhotoStorage no encontrado, la foto no se guardo.");
        }

        accessoryPanel.ResetEditor();
        isEditing = false;

        if (hideLiveCameraOnEdit) SetLiveCameraVisible(true);

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (sceneFlowManager == null)
            {
                sceneFlowManager = Object.FindFirstObjectByType<SceneFlowManager>();
            }

            if (sceneFlowManager != null)
            {
                sceneFlowManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.LogWarning("PhotoBoothManager: SceneFlowManager no disponible para cambiar escena.");
            }
        }
    }

    private Texture2D CaptureSquareFromScreen()
    {
        if (captureArea == null) return null;

        Vector3[] corners = new Vector3[4];
        captureArea.GetWorldCorners(corners);

        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(null, corners[2]);

        float width = topRight.x - bottomLeft.x;
        float height = topRight.y - bottomLeft.y;
        float size = Mathf.Min(width, height);

        if (size <= 1f) return null;

        Vector2 center = (bottomLeft + topRight) * 0.5f;
        float x = center.x - (size * 0.5f);
        float y = center.y - (size * 0.5f);

        x = Mathf.Clamp(x, 0f, Screen.width - size);
        y = Mathf.Clamp(y, 0f, Screen.height - size);

        int sizeInt = Mathf.RoundToInt(size);
        if (sizeInt <= 0) return null;

        Texture2D tex = new Texture2D(sizeInt, sizeInt, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(x, y, sizeInt, sizeInt), 0, 0);
        tex.Apply();

        return tex;
    }

    private void SetLiveCameraVisible(bool visible)
    {
        if (cameraManager == null || cameraManager.cameraQuad == null) return;
        cameraManager.cameraQuad.enabled = visible;
    }

    private void EnsureReferences()
    {
        if (screenshotManager == null)
            screenshotManager = Object.FindFirstObjectByType<ScreenshotManager>();

        if (accessoryPanel == null)
            accessoryPanel = Object.FindFirstObjectByType<AccessoryEditorPanel>();

        if (cameraManager == null)
            cameraManager = Object.FindFirstObjectByType<CameraManager>();

        if (sceneFlowManager == null)
            sceneFlowManager = Object.FindFirstObjectByType<SceneFlowManager>();

        if (photoStorage == null)
            photoStorage = Object.FindFirstObjectByType<PhotoStorage>();
    }
}
