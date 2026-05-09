using UnityEngine;
using System.IO;


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

        string path = Path.Combine(Application.persistentDataPath, fileName);

        File.WriteAllBytes(path, bytes);

        Destroy(texture);

        Debug.Log("Foto guardada en: " + path);
    }
}