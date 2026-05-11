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
    public void TakePhoto(string fileName)
    {
        StartCoroutine(Capture(fileName));
    }

    System.Collections.IEnumerator Capture(string fileName)
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

        // Si no tiene extensión, usar .png por defecto
        if (string.IsNullOrEmpty(extension))
            extension = ".png";

        string finalFileName = $"{baseName}_{eraName}{extension}";
        string path = Path.Combine(Application.persistentDataPath, finalFileName);

        File.WriteAllBytes(path, bytes);

        Destroy(texture);

        Debug.Log("Foto guardada en: " + path);
    }
}