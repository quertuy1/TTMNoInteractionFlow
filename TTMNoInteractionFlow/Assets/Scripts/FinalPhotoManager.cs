using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Storage;
using UnityEngine;
using UnityEngine.UI;
using ZXing;
using ZXing.QrCode;
using ZXing.Rendering;

public class FinalPhotoManager : MonoBehaviour
{
    [Header("References")]
    public ScreenshotManager screenshotManager;
    public PhotoStorage photoStorage;
    public RawImage[] previewImages = new RawImage[4];
    public RawImage qrRawImage;

    [Header("QR Settings")]
    public int qrSize = 512;

    private Texture2D qrTexture;
    private bool firebaseReady = false;

    private void Awake()
    {
        if (photoStorage == null)
        {
            photoStorage = UnityEngine.Object.FindFirstObjectByType<PhotoStorage>();
        }

        InitializeFirebase();
    }

    private async void InitializeFirebase()
    {
        var status = await FirebaseApp.CheckAndFixDependenciesAsync();
        firebaseReady = status == DependencyStatus.Available;

        if (!firebaseReady)
        {
            Debug.LogError("FinalPhotoManager: Firebase dependencies not available: " + status);
        }
    }

    private void Start()
    {
        Texture2D[] photos = GetPhotosForPreview();
        AssignPreviewImages(photos);
    }

    public async void ProcessAndUploadPhotoBooth()
    {
        if (!firebaseReady)
        {
            Debug.LogError("FinalPhotoManager: Firebase no esta listo.");
            return;
        }

        if (screenshotManager == null)
        {
            Debug.LogWarning("FinalPhotoManager: ScreenshotManager no asignado.");
            return;
        }

        Texture2D[] photos = GetPhotosForPreview();
        if (photos == null || photos.Length < 4)
        {
            Debug.LogWarning("FinalPhotoManager: No hay 4 fotos capturadas.");
            return;
        }

        AssignPreviewImages(photos);

        Texture2D representative = GetRepresentativePhoto(photos);
        if (representative == null)
        {
            Debug.LogWarning("FinalPhotoManager: Fotos vacias, no se puede subir.");
            return;
        }

        byte[] jpgBytes = representative.EncodeToJPG();
        string fileName = Guid.NewGuid().ToString() + ".jpg";
        string path = "photobooth/" + fileName;

        Debug.Log("FinalPhotoManager: Subida iniciada a Firebase Storage...");

        StorageReference storageRef = FirebaseStorage.DefaultInstance.GetReference(path);

        try
        {
            await storageRef.PutBytesAsync(jpgBytes);
            Uri downloadUrl = await storageRef.GetDownloadUrlAsync();

            Debug.Log("FinalPhotoManager: Subida completa. URL: " + downloadUrl);

            GenerateQr(downloadUrl.ToString());
        }
        catch (Exception ex)
        {
            Debug.LogError("FinalPhotoManager: Error subiendo la foto. " + ex.Message);
        }
    }

    private void AssignPreviewImages(Texture2D[] photos)
    {
        if (previewImages == null || previewImages.Length == 0) return;
        if (photos == null) return;

        int count = Mathf.Min(previewImages.Length, photos.Length);
        for (int i = 0; i < count; i++)
        {
            if (previewImages[i] != null)
            {
                previewImages[i].texture = photos[i];
            }
        }
    }

    private Texture2D GetRepresentativePhoto(Texture2D[] photos)
    {
        for (int i = 0; i < photos.Length; i++)
        {
            if (photos[i] != null) return photos[i];
        }

        return null;
    }

    private Texture2D[] GetPhotosForPreview()
    {
        if (photoStorage != null && photoStorage.photos != null && photoStorage.photos.Count > 0)
        {
            return photoStorage.photos.ToArray();
        }

        if (screenshotManager == null) return null;

        return screenshotManager.GetCapturedPhotos();
    }

    private void GenerateQr(string url)
    {
        if (string.IsNullOrEmpty(url)) return;

        var options = new QrCodeEncodingOptions
        {
            Height = qrSize,
            Width = qrSize,
            Margin = 1
        };

        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = options
        };

        var pixelData = writer.Write(url);

        if (qrTexture != null)
        {
            Destroy(qrTexture);
        }

        qrTexture = new Texture2D(pixelData.Width, pixelData.Height, TextureFormat.RGBA32, false);
        qrTexture.LoadRawTextureData(pixelData.Pixels);
        qrTexture.Apply();

        if (qrRawImage != null)
        {
            qrRawImage.texture = qrTexture;
            qrRawImage.SetNativeSize();
        }

        Debug.Log("FinalPhotoManager: QR listo en pantalla.");
    }
}
