using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

/// <summary>
/// Controlador principal del sistema de filtros de epoca.
/// Singleton persistente que gestiona el cambio de Volume Profiles
/// y audio tematico segun la epoca seleccionada.
/// </summary>
public class FilterManager : MonoBehaviour
{
    public static FilterManager Instance;

    [Header("Configuracion de Volumen")]
    [Tooltip("Referencia al Global Volume de la escena de captura")]
    public Volume globalVolume;

    [Header("Volume Profiles por Epoca")]
    public VolumeProfile profile50s;
    public VolumeProfile profile70s;
    public VolumeProfile profile90s;
    public VolumeProfile profileActual;

    [Header("Audio por Epoca")]
    public AudioClip sound50s;
    public AudioClip sound70s;
    public AudioClip sound90s;
    public AudioClip soundActual;

    [Header("Props (Accesorios) por Epoca")]
    [Tooltip("Asigna los GameObjects (imágenes) que pertenecen a los años 50")]
    public GameObject[] props50s;
    [Tooltip("Asigna los GameObjects (imágenes) que pertenecen a los años 70")]
    public GameObject[] props70s;
    [Tooltip("Asigna los GameObjects (imágenes) que pertenecen a los años 90")]
    public GameObject[] props90s;
    [Tooltip("Asigna los GameObjects (imágenes) que pertenecen a la época actual")]
    public GameObject[] propsActual;

    [Header("Configuracion de Audio")]
    [Tooltip("Volumen de los efectos de sonido (0 a 1)")]
    [Range(0f, 1f)]
    public float soundVolume = 0.35f;

    [Tooltip("Duracion maxima del sonido en segundos")]
    public float soundDuration = 0.8f;

    [Tooltip("Tiempo de fade out del sonido en segundos")]
    public float soundFadeOut = 0.3f;

    private AudioSource audioSource;
    private int currentEraIndex = -1;
    private readonly string[] eraNames = { "50s", "70s", "90s", "Actual" };
    private VolumeProfile[] eraProfiles;
    private Coroutine currentSoundCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void Start()
    {
        eraProfiles = new VolumeProfile[]
        {
            profile50s,
            profile70s,
            profile90s,
            profileActual
        };

        // Auto-asignar props por nombre si el usuario no lo ha hecho en el Inspector
        AutoAssignProps();

        // Ocultar todos los props al iniciar
        DisableAllProps();
    }

    private void AutoAssignProps()
    {
        if (props50s == null || props50s.Length == 0)
        {
            var p1 = GameObject.Find("50s sombrero");
            var p2 = GameObject.Find("Corbatín");
            var p3 = GameObject.Find("Moustache");
            
            System.Collections.Generic.List<GameObject> list50 = new System.Collections.Generic.List<GameObject>();
            if (p1) list50.Add(p1);
            if (p2) list50.Add(p2);
            if (p3) list50.Add(p3);
            props50s = list50.ToArray();
        }

        // El usuario dijo "80s", lo asignaremos a la época 70s o 90s. 
        // Vamos a asignarlo a props70s temporalmente ya que las épocas son 50, 70, 90.
        if (props70s == null || props70s.Length == 0)
        {
            var p4 = GameObject.Find("Aretes");
            var p5 = GameObject.Find("Gafa");
            
            System.Collections.Generic.List<GameObject> list70 = new System.Collections.Generic.List<GameObject>();
            if (p4) list70.Add(p4);
            if (p5) list70.Add(p5);
            props70s = list70.ToArray();
        }
    }

    /// <summary>
    /// Cambia el filtro de epoca activo.
    /// 0 = Anos 50s, 1 = Anos 70s, 2 = Anos 90s, 3 = Actual
    /// </summary>
    public void SetEra(int index)
    {
        if (index < 0 || index >= eraProfiles.Length)
        {
            Debug.LogWarning($"FilterManager: Indice de epoca invalido: {index}");
            return;
        }

        if (eraProfiles[index] == null)
        {
            Debug.LogWarning($"FilterManager: El perfil para '{eraNames[index]}' no esta asignado.");
            return;
        }

        currentEraIndex = index;

        // Cambiar el Volume Profile activo
        if (globalVolume != null)
        {
            globalVolume.profile = eraProfiles[index];
        }
        else
        {
            Debug.LogError("FilterManager: No se ha asignado el Global Volume.");
            return;
        }

        // Mostrar los props correctos
        UpdatePropsVisibility(index);

        // Reproducir sonido tematico de la epoca (breve y a volumen bajo)
        PlayEraSound(index);

        Debug.Log($"Filtro cambiado a: {eraNames[index]}");
    }

    private void DisableAllProps()
    {
        SetPropsActive(props50s, false);
        SetPropsActive(props70s, false);
        SetPropsActive(props90s, false);
        SetPropsActive(propsActual, false);
    }

    private void UpdatePropsVisibility(int index)
    {
        DisableAllProps();

        switch (index)
        {
            case 0: SetPropsActive(props50s, true); break;
            case 1: SetPropsActive(props70s, true); break;
            case 2: SetPropsActive(props90s, true); break;
            case 3: SetPropsActive(propsActual, true); break;
        }
    }

    private void SetPropsActive(GameObject[] props, bool isActive)
    {
        if (props == null) return;
        foreach (var prop in props)
        {
            if (prop != null) prop.SetActive(isActive);
        }
    }

    /// <summary>
    /// Reproduce un sonido breve asociado a la epoca seleccionada.
    /// El sonido se reproduce a volumen controlado y se detiene
    /// despues de la duracion configurada con un fade out suave.
    /// </summary>
    private void PlayEraSound(int index)
    {
        AudioClip selectedSound = index switch
        {
            0 => sound50s,
            1 => sound70s,
            2 => sound90s,
            3 => soundActual,
            _ => null
        };

        if (selectedSound == null || audioSource == null)
            return;

        // Detener sonido anterior si hay uno reproduciendose
        if (currentSoundCoroutine != null)
        {
            StopCoroutine(currentSoundCoroutine);
            audioSource.Stop();
        }

        // Reproducir a volumen controlado
        audioSource.volume = soundVolume;
        audioSource.clip = selectedSound;
        audioSource.Play();

        // Iniciar coroutine para detener y fade out
        currentSoundCoroutine = StartCoroutine(StopSoundAfterDuration());
    }

    /// <summary>
    /// Coroutine que detiene el sonido despues de la duracion configurada,
    /// aplicando un fade out suave.
    /// </summary>
    private IEnumerator StopSoundAfterDuration()
    {
        // Esperar la duracion del sonido menos el tiempo de fade
        float waitTime = Mathf.Max(0, soundDuration - soundFadeOut);
        yield return new WaitForSeconds(waitTime);

        // Fade out suave
        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < soundFadeOut)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / soundFadeOut;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = soundVolume; // Restaurar volumen para el proximo sonido
        currentSoundCoroutine = null;
    }

    /// <summary>
    /// Retorna el nombre legible de la epoca actual.
    /// Se usa para nombrar los archivos de captura.
    /// </summary>
    public string GetCurrentEraName()
    {
        if (currentEraIndex >= 0 && currentEraIndex < eraNames.Length)
            return eraNames[currentEraIndex];
        return "SinFiltro";
    }

    /// <summary>
    /// Retorna el indice de la epoca actualmente seleccionada.
    /// -1 si no se ha seleccionado ninguna.
    /// </summary>
    public int GetCurrentEraIndex()
    {
        return currentEraIndex;
    }
}
