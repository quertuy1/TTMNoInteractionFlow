using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gestiona la cámara web del dispositivo.
/// Renderiza la webcam en un Quad 3D para que los efectos de
/// post-procesado (Volume Profiles URP) se apliquen sobre la imagen.
/// </summary>
public class CameraManager : MonoBehaviour
{
    [Header("Renderizado 3D (para filtros de post-procesado)")]
    [Tooltip("MeshRenderer del Quad 3D donde se proyecta la webcam")]
    public Renderer cameraQuad;

    [Header("Configuracion de Webcam")]
    [Tooltip("Indice del dispositivo de camara (0 = primera camara detectada)")]
    public int cameraDeviceIndex = 0;

    [Tooltip("Resolucion deseada de la webcam")]
    public int requestedWidth = 1920;
    public int requestedHeight = 1080;
    public int requestedFPS = 30;

    [Header("Fallback (cuando no hay webcam)")]
    [Tooltip("Color de fondo mientras la camara no esta lista")]
    public Color fallbackColor = new Color(0.10f, 0.10f, 0.15f, 1f);

    private WebCamTexture webcamTexture;
    private Material quadMaterial;
    private bool webcamReady = false;

    void Start()
    {
        InitializeQuad();
        InitializeWebcam();
    }

    void Update()
    {
        // Verificar continuamente si la webcam ya produce frames
        if (!webcamReady && webcamTexture != null && webcamTexture.isPlaying)
        {
            // Solo mostrar la textura cuando realmente hay frames
            if (webcamTexture.didUpdateThisFrame)
            {
                quadMaterial.mainTexture = webcamTexture;
                quadMaterial.color = Color.white;
                webcamReady = true;
                Debug.Log("CameraManager: Webcam produciendo frames correctamente.");
            }
        }
    }

    /// <summary>
    /// Inicializa el Quad con color oscuro de fallback.
    /// </summary>
    private void InitializeQuad()
    {
        if (cameraQuad == null)
        {
            Debug.LogError("CameraManager: No se ha asignado el Quad 3D (cameraQuad).");
            return;
        }

        // Crear material oscuro por defecto - SIN textura
        quadMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        quadMaterial.color = fallbackColor;
        quadMaterial.mainTexture = null; // Sin textura = color solido
        cameraQuad.material = quadMaterial;
    }

    /// <summary>
    /// Inicializa la webcam sin asignarla al material todavia.
    /// La asignacion ocurre en Update() cuando hay frames reales.
    /// </summary>
    private void InitializeWebcam()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.LogWarning("CameraManager: No se detectaron camaras web.");
            return;
        }

        int deviceIdx = Mathf.Clamp(cameraDeviceIndex, 0, devices.Length - 1);
        string deviceName = devices[deviceIdx].name;

        Debug.Log($"CameraManager: Iniciando camara '{deviceName}'...");

        webcamTexture = new WebCamTexture(deviceName, requestedWidth, requestedHeight, requestedFPS);
        // NO asignar la textura al material todavia - esperar a que haya frames
        webcamTexture.Play();
    }

    /// <summary>
    /// Retorna true si la webcam esta activa y produciendo frames.
    /// </summary>
    public bool IsWebcamActive()
    {
        return webcamReady;
    }

    /// <summary>
    /// Retorna la textura de la webcam para poder analizarla (ej: Inteligencia Artificial)
    /// </summary>
    public WebCamTexture GetWebcamTexture()
    {
        return webcamTexture;
    }

    void OnDestroy()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
            webcamTexture.Stop();

        if (quadMaterial != null)
            Destroy(quadMaterial);
    }
}