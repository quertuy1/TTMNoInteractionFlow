using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


/// <summary>
/// Puente entre Unity y el servidor Python de Inteligencia Artificial.
/// Envia frames de la webcam en baja resolucion y recibe las coordenadas
/// del rostro para controlar los accesorios AR.
/// </summary>
public class FaceTrackerBridge : MonoBehaviour
{
    [Header("Conexion Python AI")]
    public int sendPort = 5005;
    public int receivePort = 5006;
    public float sendRate = 0.05f; // 20 fps

    [Header("Procesamiento")]
    public int downscaleWidth = 320;
    public int downscaleHeight = 180;
    [Range(10, 100)] public int jpgQuality = 40;

    private UdpClient udpSender;
    private UdpClient udpReceiver;
    private Thread receiveThread;
    private bool isRunning = true;

    // Estado de los rostros detectados
    [System.Serializable]
    public class FaceData
    {
        public float x;
        public float y;
        public float scale;
    }

    [System.Serializable]
    public class FaceList
    {
        public System.Collections.Generic.List<FaceData> faces;
    }

    private System.Collections.Generic.List<FaceData> detectedFaces = new System.Collections.Generic.List<FaceData>();
    private System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<ARItemController>> faceClones =
        new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<ARItemController>>();

    // Buffer para extraer la imagen de la webcam
    private Texture2D downscaleTex;
    private float nextSendTime = 0f;

    void Start()
    {
        // Inicializar sockets
        udpSender = new UdpClient();

        udpReceiver = new UdpClient(receivePort);
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();

        downscaleTex = new Texture2D(downscaleWidth, downscaleHeight, TextureFormat.RGB24, false);

        Debug.Log("FaceTrackerBridge: Sistema Multi-Persona (Máx 5) Iniciado.");
    }

    void Update()
    {
        if (Time.time >= nextSendTime)
        {
            SendFrameToAI();
            nextSendTime = Time.time + sendRate;
        }

        UpdateFaceFilters();
    }

    private void UpdateFaceFilters()
    {
        // 1. Obtener los items AR "maestros" que están activos en la escena
        ARItemController[] allItems = Object.FindObjectsByType<ARItemController>(FindObjectsSortMode.None);
        System.Collections.Generic.List<ARItemController> masterItems = new System.Collections.Generic.List<ARItemController>();

        foreach (var item in allItems)
        {
            // Solo consideramos "maestros" a los que no son clones creados por este script
            if (item.gameObject.activeInHierarchy && item.useFaceTracking && !item.name.Contains("(Clone)"))
            {
                masterItems.Add(item);
            }
        }

        lock (detectedFaces)
        {
            // 2. Para cada rostro detectado
            for (int i = 0; i < 5; i++)
            {
                if (i < detectedFaces.Count)
                {
                    FaceData face = detectedFaces[i];

                    if (i == 0)
                    {
                        // El primer rostro usa los items maestros
                        foreach (var master in masterItems)
                        {
                            master.UpdateFromFaceTracking(face.x, face.y, face.scale);
                        }
                    }
                    else
                    {
                        // Rostros adicionales (1 a 4) usan clones
                        UpdateClonesForFace(i, face, masterItems);
                    }
                }
                else
                {
                    // Si no hay rostro para este índice, eliminar clones si existen
                    ClearClonesForFace(i);
                }
            }
        }
    }

    private void UpdateClonesForFace(int faceIndex, FaceData face, System.Collections.Generic.List<ARItemController> masters)
    {
        if (!faceClones.ContainsKey(faceIndex))
        {
            faceClones[faceIndex] = new System.Collections.Generic.List<ARItemController>();
        }

        var clones = faceClones[faceIndex];

        // Si el número de clones no coincide con los maestros, regenerar
        if (clones.Count != masters.Count)
        {
            ClearClonesForFace(faceIndex);
            foreach (var master in masters)
            {
                ARItemController clone = Instantiate(master, master.transform.parent);
                clone.name = master.name + "(Clone)";
                clones.Add(clone);
            }
        }

        // Actualizar posición de los clones
        for (int i = 0; i < clones.Count; i++)
        {
            if (clones[i] != null)
            {
                clones[i].UpdateFromFaceTracking(face.x, face.y, face.scale);
            }
        }
    }

    private void ClearClonesForFace(int faceIndex)
    {
        if (faceClones.ContainsKey(faceIndex))
        {
            foreach (var clone in faceClones[faceIndex])
            {
                if (clone != null) Destroy(clone.gameObject);
            }
            faceClones[faceIndex].Clear();
        }
    }

    private void SendFrameToAI()
    {
        CameraManager camMgr = Object.FindFirstObjectByType<CameraManager>();
        if (camMgr == null) return;

        WebCamTexture webcam = camMgr.GetWebcamTexture();
        if (webcam == null || !webcam.isPlaying || webcam.width <= 16) return;

        // Copiar y redimensionar el frame para la IA
        Color32[] pixels = webcam.GetPixels32();

        int w = webcam.width;
        int h = webcam.height;
        Color[] downscaled = new Color[downscaleWidth * downscaleHeight];

        for (int y = 0; y < downscaleHeight; y++)
        {
            for (int x = 0; x < downscaleWidth; x++)
            {
                int srcX = Mathf.FloorToInt((float)x / downscaleWidth * w);
                int srcY = Mathf.FloorToInt((float)y / downscaleHeight * h);
                downscaled[y * downscaleWidth + x] = pixels[srcY * w + srcX];
            }
        }

        downscaleTex.SetPixels(downscaled);
        downscaleTex.Apply();

        byte[] jpgData = downscaleTex.EncodeToJPG(jpgQuality);

        try
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), sendPort);
            udpSender.Send(jpgData, jpgData.Length, endPoint);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error enviando frame a Python: {e.Message}");
        }
    }

    private void ReceiveData()
    {
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
        while (isRunning)
        {
            try
            {
                byte[] data = udpReceiver.Receive(ref anyIP);
                string json = Encoding.UTF8.GetString(data);

                FaceList list = JsonUtility.FromJson<FaceList>(json);
                if (list != null && list.faces != null)
                {
                    lock (detectedFaces)
                    {
                        detectedFaces = list.faces;
                    }
                }
            }
            catch (SocketException)
            {
                // Normal al cerrar
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Error recibiendo data de rostros: " + e.Message);
            }
        }
    }

    void OnDestroy()
    {
        isRunning = false;

        if (receiveThread != null && receiveThread.IsAlive)
            receiveThread.Abort();

        if (udpReceiver != null)
            udpReceiver.Close();

        if (udpSender != null)
            udpSender.Close();

        if (downscaleTex != null)
            Destroy(downscaleTex);
    }
}