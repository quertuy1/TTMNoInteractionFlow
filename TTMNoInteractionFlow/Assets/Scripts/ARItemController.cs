using UnityEngine;

/// <summary>
/// Controlador de accesorios AR (Filtros, gafas, etc.).
/// Usa el servidor de Inteligencia Artificial para posicionar el objeto
/// en el rostro del usuario.
/// </summary>
public class ARItemController : MonoBehaviour
{
    [Header("Configuración del Accesorio")]
    [Tooltip("Distancia desde la cámara (debe estar entre la cámara y el Quad)")]
    public float distanceFromCamera = 5f;

    [Tooltip("Suavizado del movimiento")]
    public float smoothSpeed = 10f;

    [Header("Rotacion Base")]
    public Vector3 baseRotation = new Vector3(-10f, 0f, 0f); // Rotacion base del accesorio
    
    [Header("Inteligencia Artificial")]
    public bool useFaceTracking = false;
    public float faceScaleMultiplier = 5f; // Ajustado para imágenes 2D (PNGs) en vez de modelos 3D
    
    [Tooltip("Ajusta donde se pone el accesorio respecto a la cara (X: lados, Y: arriba/abajo, Z: profundidad)")]
    public Vector3 trackingOffset = new Vector3(0f, 0.5f, 0f);

    private Camera mainCam;
    private Vector3 targetPosition;

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        if (mainCam == null || useFaceTracking) return; // Si usamos IA, la actualizacion viene de FaceTrackerBridge

        // Leer posicion del mouse en la pantalla (0 a 1)
        Vector2 screenPos = Input.mousePosition;
        
        // Convertir la posicion 2D de la pantalla a un punto 3D en el espacio
        // La distancia Z es fundamental para que se vea delante de la webcam pero detras de la UI
        Vector3 worldPoint = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, distanceFromCamera));

        targetPosition = worldPoint;

        // Mover suavemente
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

        // Opcional: Pequeña rotacion basada en el movimiento para darle vida
        float tiltX = (screenPos.y / Screen.height - 0.5f) * -20f;
        float tiltY = (screenPos.x / Screen.width - 0.5f) * -30f;
        
        Quaternion targetRot = Quaternion.Euler(baseRotation.x + tiltX, baseRotation.y + tiltY, baseRotation.z);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * smoothSpeed);
    }

    /// <summary>
    /// Llamado por FaceTrackerBridge cuando llega nueva data de Python
    /// </summary>
    public void UpdateFromFaceTracking(float faceX, float faceY, float faceScale)
    {
        if (mainCam == null) return;

        // Las coordenadas de Python vienen de 0 a 1
        float pixelX = faceX * Screen.width;
        float pixelY = faceY * Screen.height;

        // Convertir el pixel central a espacio 3D
        Vector3 worldPoint = mainCam.ScreenToWorldPoint(new Vector3(pixelX, pixelY, distanceFromCamera));
        
        // Escalar basado en el tamaño de la cara
        float targetSize = faceScale * faceScaleMultiplier;
        Vector3 targetScaleVec = new Vector3(targetSize, targetSize, targetSize);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScaleVec, Time.deltaTime * smoothSpeed);

        // Aplicar el offset (multiplicado por el tamaño para que se mantenga proporcional si te alejas/acercas)
        // Como 'trackingOffset.y' sera positivo para accesorios superiores, esto lo subira.
        Vector3 finalOffset = new Vector3(
            trackingOffset.x * targetSize,
            trackingOffset.y * targetSize,
            trackingOffset.z * targetSize
        );
        
        targetPosition = worldPoint + finalOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

        // Rotacion basica
        Quaternion targetRot = Quaternion.Euler(baseRotation);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * smoothSpeed);
    }
}
