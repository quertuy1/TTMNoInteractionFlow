using UnityEngine;
using System.IO;

/// <summary>
/// Gestiona la captura de pantalla (screenshot).
/// La captura incluye todos los efectos de post-procesado activos,
/// por lo que el filtro de época seleccionado se guarda en la foto.
/// 
/// El nombre del archivo incluye la época seleccionada para identificación.
/// Ejemplo: "Foto_70s_2026.png"
/// </summary>
public class ScreenshotManager : MonoBehaviour
{
    public static System.Action<Texture2D> OnPhotoTaken;

    // Última textura capturada por TakePhoto (preview editable)
    private Texture2D lastCapturedTexture;

    [Header("Photo Booth Capture")]
    public CanvasGroup captureUiGroup;
    public Camera captureCamera;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip photoCaptureClip;

    [Header("Flash")]
    public FlashEffect flashEffect;

    private Texture2D[] capturedPhotos = new Texture2D[4];

    public void TakePhoto()
    {
        // Captura la pantalla y notifica a los listeners, pero NO guarda ni cambia de escena.
        TakePhotoForEdit(null);
    }

    public void TakePhoto(string fileName)
    {
        // Mantener compatibilidad con UnityEvents que pasan un string.
        TakePhotoForEdit(null);
    }

    public void TakePhotoForEdit(System.Action<Texture2D> onCaptured)
    {
        StartCoroutine(CaptureForEdit(onCaptured));
    }

    public void TakePhotoForEditFromCameraArea(RectTransform captureArea, System.Action<Texture2D> onCaptured)
    {
        StartCoroutine(CaptureForEditFromCameraArea(captureArea, onCaptured));
    }

    public void CapturePhotoToSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= capturedPhotos.Length)
        {
            Debug.LogWarning("ScreenshotManager: slotIndex fuera de rango (0-3).");
            return;
        }

        StartCoroutine(CaptureToSlot(slotIndex));
    }

    public Texture2D[] GetCapturedPhotos()
    {
        return capturedPhotos;
    }

    System.Collections.IEnumerator CaptureForEdit(System.Action<Texture2D> onCaptured)
    {
        yield return new WaitForEndOfFrame();

        Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();

        // Liberar la captura anterior si existe
        if (lastCapturedTexture != null)
            Destroy(lastCapturedTexture);

        lastCapturedTexture = texture;

        // Notificar a quien quiera editar/mostrar la imagen
        onCaptured?.Invoke(texture);
        OnPhotoTaken?.Invoke(texture);

        PlayCaptureSfx();
        TriggerFlash();

        Debug.Log("ScreenshotManager: Foto tomada para edición.");
    }

    System.Collections.IEnumerator CaptureForEditFromCameraArea(RectTransform captureArea, System.Action<Texture2D> onCaptured)
    {
        yield return new WaitForEndOfFrame();

        float prevAlpha = 1f;
        bool prevInteractable = true;
        bool prevBlocksRaycasts = true;
        if (captureUiGroup != null)
        {
            prevAlpha = captureUiGroup.alpha;
            prevInteractable = captureUiGroup.interactable;
            prevBlocksRaycasts = captureUiGroup.blocksRaycasts;
            captureUiGroup.alpha = 0f;
            captureUiGroup.interactable = false;
            captureUiGroup.blocksRaycasts = false;
        }

        yield return new WaitForEndOfFrame();

        Camera cam = captureCamera != null ? captureCamera : Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("ScreenshotManager: No hay camara para capturar.");
            RestoreUiGroup(prevAlpha, prevInteractable, prevBlocksRaycasts);
            yield break;
        }

        RenderTexture prevTarget = cam.targetTexture;
        RenderTexture prevActive = RenderTexture.active;

        RenderTexture rt = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        RenderTexture.active = rt;
        cam.Render();

        Rect captureRect = GetScreenRectFromArea(captureArea);
        if (captureRect.width <= 1f || captureRect.height <= 1f)
        {
            captureRect = new Rect(0f, 0f, rt.width, rt.height);
        }

        int width = Mathf.RoundToInt(captureRect.width);
        int height = Mathf.RoundToInt(captureRect.height);
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.ReadPixels(captureRect, 0, 0);
        tex.Apply();

        cam.targetTexture = prevTarget;
        RenderTexture.active = prevActive;
        RenderTexture.ReleaseTemporary(rt);

        if (lastCapturedTexture != null)
            Destroy(lastCapturedTexture);

        lastCapturedTexture = tex;

        onCaptured?.Invoke(tex);
        OnPhotoTaken?.Invoke(tex);

        RestoreUiGroup(prevAlpha, prevInteractable, prevBlocksRaycasts);

        PlayCaptureSfx();
        TriggerFlash();

        Debug.Log("ScreenshotManager: Foto tomada para edición (camara + area).");
    }

    private Rect GetScreenRectFromArea(RectTransform captureArea)
    {
        if (captureArea == null)
            return new Rect(0f, 0f, Screen.width, Screen.height);

        Vector3[] corners = new Vector3[4];
        captureArea.GetWorldCorners(corners);

        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(null, corners[2]);

        float x = Mathf.Clamp(bottomLeft.x, 0f, Screen.width);
        float y = Mathf.Clamp(bottomLeft.y, 0f, Screen.height);
        float width = Mathf.Clamp(topRight.x - bottomLeft.x, 0f, Screen.width - x);
        float height = Mathf.Clamp(topRight.y - bottomLeft.y, 0f, Screen.height - y);

        return new Rect(x, y, width, height);
    }

    System.Collections.IEnumerator CaptureToSlot(int slotIndex)
    {
        yield return new WaitForEndOfFrame();

        float prevAlpha = 1f;
        bool prevInteractable = true;
        bool prevBlocksRaycasts = true;
        if (captureUiGroup != null)
        {
            prevAlpha = captureUiGroup.alpha;
            prevInteractable = captureUiGroup.interactable;
            prevBlocksRaycasts = captureUiGroup.blocksRaycasts;
            captureUiGroup.alpha = 0f;
            captureUiGroup.interactable = false;
            captureUiGroup.blocksRaycasts = false;
        }

        yield return new WaitForEndOfFrame();

        Camera cam = captureCamera != null ? captureCamera : Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("ScreenshotManager: No hay camara para capturar.");
            RestoreUiGroup(prevAlpha, prevInteractable, prevBlocksRaycasts);
            yield break;
        }

        RenderTexture prevTarget = cam.targetTexture;
        RenderTexture prevActive = RenderTexture.active;

        RenderTexture rt = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);

        cam.targetTexture = rt;
        RenderTexture.active = rt;
        cam.Render();

        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        cam.targetTexture = prevTarget;
        RenderTexture.active = prevActive;
        RenderTexture.ReleaseTemporary(rt);

        if (capturedPhotos[slotIndex] != null)
        {
            Destroy(capturedPhotos[slotIndex]);
        }

        capturedPhotos[slotIndex] = tex;

        RestoreUiGroup(prevAlpha, prevInteractable, prevBlocksRaycasts);

        PlayCaptureSfx();
        TriggerFlash();

        Debug.Log($"ScreenshotManager: Foto capturada en slot {slotIndex}.");
    }

    private void RestoreUiGroup(float prevAlpha, bool prevInteractable, bool prevBlocksRaycasts)
    {
        if (captureUiGroup == null) return;

        captureUiGroup.alpha = prevAlpha;
        captureUiGroup.interactable = prevInteractable;
        captureUiGroup.blocksRaycasts = prevBlocksRaycasts;
    }

    private void PlayCaptureSfx()
    {
        if (audioSource == null || photoCaptureClip == null) return;
        audioSource.PlayOneShot(photoCaptureClip);
    }

    private void TriggerFlash()
    {
        if (flashEffect == null) return;
        flashEffect.TriggerFlash();
    }

    /// <summary>
    /// Toma una captura final (incluye overlays/UI) y la guarda en disco.
    /// Si se provee "nextScene", carga esa escena al terminar.
    /// </summary>
    public void TakeFinalPhotoAndSave(string fileName, string nextScene = null)
    {
        StartCoroutine(CaptureAndSave(fileName, nextScene));
    }

    System.Collections.IEnumerator CaptureAndSave(string fileName, string nextScene)
    {
        yield return new WaitForEndOfFrame();

        Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();

        byte[] bytes = texture.EncodeToPNG();

        // Construir nombre de archivo con la época seleccionada
        string eraName = "SinFiltro";
        if (FilterManager.Instance != null)
        {
            eraName = FilterManager.Instance.GetCurrentEraName();
        }

        string baseName = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);

        if (string.IsNullOrEmpty(extension))
            extension = ".png";

        string finalFileName = $"{baseName}_{eraName}{extension}";
        string path = Path.Combine(Application.persistentDataPath, finalFileName);

        File.WriteAllBytes(path, bytes);

        Destroy(texture);

        Debug.Log("ScreenshotManager: Foto final guardada en: " + path);

        if (!string.IsNullOrEmpty(nextScene))
        {
            if (SceneFlowManager.Instance != null)
            {
                SceneFlowManager.Instance.LoadScene(nextScene);
            }
            else
            {
                Debug.LogWarning("ScreenshotManager: SceneFlowManager no disponible para cambiar escena.");
            }
        }
    }
}