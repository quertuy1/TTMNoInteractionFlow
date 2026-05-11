using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Script de Editor que configura automaticamente la escena Foto1
/// con todos los elementos necesarios para el sistema de filtros de epoca.
/// 
/// USO: En Unity, ir a la barra de menu -> TTM -> Setup Filtros de Epoca
/// 
/// IMPORTANTE: Este script elimina el Canvas viejo, el RawImage original
/// y el boton "TomarFoto" del diseño anterior, reemplazandolos con
/// el nuevo sistema de Quad 3D + filtros.
/// </summary>
public class FilterSetupEditor : EditorWindow
{
    [MenuItem("TTM/Setup Filtros de Epoca")]
    public static void SetupFilters()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("No se puede configurar en modo Play. Deten el juego primero.");
            return;
        }

        // Confirmar antes de limpiar
        bool proceed = EditorUtility.DisplayDialog(
            "TTM - Setup Filtros de Epoca",
            "Esto va a:\n\n" +
            "1. Eliminar el Canvas viejo (RawImage, Button, TomarFoto)\n" +
            "2. Crear el sistema de Quad 3D + filtros\n" +
            "3. Crear la nueva interfaz de seleccion\n\n" +
            "Continuar?",
            "Si, configurar", "Cancelar"
        );

        if (proceed)
        {
            CleanOldObjects();
            CreateFilterSystem();
            Debug.Log("Sistema de filtros de epoca configurado exitosamente.");
        }
    }

    [MenuItem("TTM/Limpiar Todo (Reset)")]
    public static void CleanAll()
    {
        CleanOldObjects();
        CleanNewObjects();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("Escena completamente limpiada.");
    }

    /// <summary>
    /// Elimina los objetos del diseño ORIGINAL de la escena
    /// (Canvas viejo con RawImage, Button TomarFoto, etc.)
    /// </summary>
    private static void CleanOldObjects()
    {
        // Buscar y eliminar el Canvas original (el que tiene el RawImage de la webcam)
        // Lo identificamos porque tiene nombre "Canvas" (no "FilterCanvas")
        GameObject oldCanvas = GameObject.Find("Canvas");
        if (oldCanvas != null)
        {
            Object.DestroyImmediate(oldCanvas);
            Debug.Log("Canvas viejo eliminado.");
        }

        // Buscar y eliminar EventSystem duplicados
        var eventSystems = Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);
        for (int i = 1; i < eventSystems.Length; i++)
        {
            Object.DestroyImmediate(eventSystems[i].gameObject);
        }

        // Eliminar cualquier CameraManager viejo que use RawImage
        // (nuestro nuevo CameraManager usa Renderer, no RawImage)
        var oldCamManagers = Object.FindObjectsByType<CameraManager>(FindObjectsSortMode.None);
        foreach (var cm in oldCamManagers)
        {
            Object.DestroyImmediate(cm.gameObject);
        }
    }

    /// <summary>
    /// Elimina los objetos creados por nuestro setup.
    /// </summary>
    private static void CleanNewObjects()
    {
        string[] objectsToDelete = {
            "WebcamQuad", "FilterVolume", "FilterManager",
            "FilterCanvas", "CameraManager"
        };

        foreach (string name in objectsToDelete)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null) Object.DestroyImmediate(obj);
        }
    }

    private static void CreateFilterSystem()
    {
        // Limpiar objetos de setup anterior
        CleanNewObjects();

        // ============================
        // 1. CONFIGURAR LA MAIN CAMERA
        // ============================
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            // Cambiar a Perspective para que pueda ver el Quad en 3D
            mainCam.orthographic = false;
            mainCam.fieldOfView = 60;
            mainCam.transform.position = new Vector3(0, 0, -10);
            mainCam.transform.rotation = Quaternion.identity;
            mainCam.nearClipPlane = 0.1f;
            mainCam.farClipPlane = 100f;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);

            // Activar post-processing
            var camData = mainCam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (camData != null)
            {
                camData.renderPostProcessing = true;
                Debug.Log("Post-Processing activado en Main Camera.");
            }

            Debug.Log("Main Camera configurada (Perspective, FOV 60).");
        }

        // ============================
        // 2. CREAR EL QUAD 3D PARA WEBCAM
        // ============================
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "WebcamQuad";
        // Calcular posicion y escala para llenar la vista de la camara
        float distance = 10f;
        float height = 2f * distance * Mathf.Tan(mainCam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * mainCam.aspect;

        quad.transform.position = new Vector3(0, 0, distance - 10f); // z = 0 (10 unidades frente a la camara en z=-10)
        quad.transform.localScale = new Vector3(width, height, 1f);
        Object.DestroyImmediate(quad.GetComponent<Collider>());

        // Material oscuro por defecto
        Material defaultMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        defaultMat.color = new Color(0.10f, 0.10f, 0.14f, 1f);
        quad.GetComponent<Renderer>().material = defaultMat;

        Debug.Log($"WebcamQuad creado (escala: {width:F1}x{height:F1}).");

        // ============================
        // 3. CREAR EL GLOBAL VOLUME
        // ============================
        GameObject volumeObj = new GameObject("FilterVolume");
        Volume volume = volumeObj.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.weight = 1f;

        VolumeProfile defaultProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
            "Assets/Filters/VolumeProfiles/VP_Epoca_Actual.asset");
        if (defaultProfile != null)
            volume.profile = defaultProfile;

        // ============================
        // 4. CREAR EL FILTER MANAGER
        // ============================
        GameObject filterMgrObj = new GameObject("FilterManager");
        FilterManager filterMgr = filterMgrObj.AddComponent<FilterManager>();
        filterMgr.globalVolume = volume;

        filterMgr.profile50s = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
            "Assets/Filters/VolumeProfiles/VP_Epoca_50s.asset");
        filterMgr.profile70s = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
            "Assets/Filters/VolumeProfiles/VP_Epoca_70s.asset");
        filterMgr.profile90s = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
            "Assets/Filters/VolumeProfiles/VP_Epoca_90s.asset");
        filterMgr.profileActual = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
            "Assets/Filters/VolumeProfiles/VP_Epoca_Actual.asset");

        filterMgr.sound50s = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Audio/dragon-studio-tv-static-323620.mp3");
        filterMgr.sound70s = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Audio/freesound_community-old-camera-80949.mp3");
        filterMgr.sound90s = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Audio/arunangshubanerjee-cassette-recorder-stop-button-mechanical-click-sound-359987.mp3");
        filterMgr.soundActual = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Audio/photos-click-409642.mp3");

        // ============================
        // 5. CREAR CAMERA MANAGER
        // ============================
        GameObject camMgrObj = new GameObject("CameraManager");
        CameraManager camMgr = camMgrObj.AddComponent<CameraManager>();
        camMgr.cameraQuad = quad.GetComponent<Renderer>();

        // ============================
        // 6. CREAR UI
        // ============================
        CreateUI();

        // ============================
        // 7. FINALIZAR
        // ============================
        EditorUtility.SetDirty(filterMgrObj);
        EditorUtility.SetDirty(camMgrObj);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "TTM - Setup Completo",
            "Sistema de filtros configurado:\n\n" +
            "- Canvas viejo eliminado\n" +
            "- WebcamQuad 3D creado (llena toda la vista)\n" +
            "- Camara cambiada a Perspective\n" +
            "- Global Volume con filtros de epoca\n" +
            "- Nueva interfaz de seleccion\n\n" +
            "Guarda la escena (Ctrl+S) y dale Play.",
            "OK"
        );
    }

    // =========================================================================
    // UI CREATION
    // =========================================================================
    private static void CreateUI()
    {
        // ---- CANVAS ----
        GameObject canvasObj = new GameObject("FilterCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Asegurar EventSystem
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // ---- PANEL INFERIOR ----
        GameObject bottomBar = CreateUI("BottomBar", canvasObj.transform);
        RectTransform barRect = bottomBar.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0, 0);
        barRect.anchorMax = new Vector2(1, 0);
        barRect.pivot = new Vector2(0.5f, 0);
        barRect.anchoredPosition = Vector2.zero;
        barRect.sizeDelta = new Vector2(0, 200);

        Image barBg = bottomBar.AddComponent<Image>();
        barBg.color = new Color(0.06f, 0.06f, 0.10f, 0.95f);

        VerticalLayoutGroup barLayout = bottomBar.AddComponent<VerticalLayoutGroup>();
        barLayout.padding = new RectOffset(30, 30, 0, 14);
        barLayout.spacing = 6;
        barLayout.childAlignment = TextAnchor.MiddleCenter;
        barLayout.childControlWidth = true;
        barLayout.childControlHeight = true;
        barLayout.childForceExpandWidth = true;
        barLayout.childForceExpandHeight = false;

        // ---- LINEA DORADA SUPERIOR ----
        GameObject topLine = CreateUI("GoldLine", bottomBar.transform);
        LayoutElement lineLE = topLine.AddComponent<LayoutElement>();
        lineLE.preferredHeight = 3;
        lineLE.flexibleWidth = 1;
        Image lineImg = topLine.AddComponent<Image>();
        lineImg.color = new Color(0.92f, 0.72f, 0.15f, 0.9f);
        lineImg.raycastTarget = false;

        // ---- TITULO ----
        GameObject titleObj = CreateUI("Title", bottomBar.transform);
        LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
        titleLE.preferredHeight = 32;
        titleLE.flexibleWidth = 1;
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "SELECCIONA UNA EPOCA";
        titleText.fontSize = 20;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.8f, 0.8f, 0.85f, 0.6f);
        titleText.fontStyle = FontStyles.Bold;
        titleText.characterSpacing = 5f;
        titleText.raycastTarget = false;

        // ---- FILA DE BOTONES DE EPOCA ----
        GameObject btnRow = CreateUI("ButtonRow", bottomBar.transform);
        LayoutElement rowLE = btnRow.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 80;
        rowLE.flexibleWidth = 1;

        HorizontalLayoutGroup hLayout = btnRow.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 12;
        hLayout.padding = new RectOffset(8, 8, 0, 0);
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.childControlWidth = true;
        hLayout.childControlHeight = true;
        hLayout.childForceExpandWidth = true;
        hLayout.childForceExpandHeight = true;

        string[] names = { "ANOS 50", "ANOS 70", "ANOS 90", "ACTUAL" };
        string[] descs = { "Blanco y Negro", "Era Dorada", "VHS Retro", "Alta Definicion" };
        Color[] colors = {
            new Color(0.20f, 0.20f, 0.26f, 1f),
            new Color(0.50f, 0.32f, 0.16f, 1f),
            new Color(0.16f, 0.38f, 0.58f, 1f),
            new Color(0.10f, 0.52f, 0.34f, 1f)
        };
        Color[] highlights = {
            new Color(0.32f, 0.32f, 0.40f, 1f),
            new Color(0.65f, 0.44f, 0.22f, 1f),
            new Color(0.25f, 0.52f, 0.72f, 1f),
            new Color(0.16f, 0.65f, 0.45f, 1f)
        };

        for (int i = 0; i < 4; i++)
            CreateEraButton(btnRow.transform, i, names[i], descs[i], colors[i], highlights[i]);

        // ---- BOTON TOMAR FOTO ----
        GameObject photoRow = CreateUI("PhotoRow", bottomBar.transform);
        LayoutElement prLE = photoRow.AddComponent<LayoutElement>();
        prLE.preferredHeight = 48;
        prLE.flexibleWidth = 1;

        HorizontalLayoutGroup prLayout = photoRow.AddComponent<HorizontalLayoutGroup>();
        prLayout.childAlignment = TextAnchor.MiddleCenter;
        prLayout.childControlWidth = false;
        prLayout.childControlHeight = true;
        prLayout.childForceExpandWidth = false;
        prLayout.childForceExpandHeight = true;

        // Espaciador izq
        GameObject sL = CreateUI("sL", photoRow.transform);
        sL.AddComponent<LayoutElement>().flexibleWidth = 1;

        // Boton
        GameObject pBtn = CreateUI("BtnTomarFoto", photoRow.transform);
        LayoutElement pLE = pBtn.AddComponent<LayoutElement>();
        pLE.preferredWidth = 300;

        Image pBg = pBtn.AddComponent<Image>();
        pBg.color = new Color(0.85f, 0.18f, 0.22f, 1f);

        Button pButton = pBtn.AddComponent<Button>();
        ColorBlock pColors = pButton.colors;
        pColors.normalColor = new Color(0.85f, 0.18f, 0.22f, 1f);
        pColors.highlightedColor = new Color(1f, 0.28f, 0.32f, 1f);
        pColors.pressedColor = new Color(0.65f, 0.10f, 0.14f, 1f);
        pColors.selectedColor = new Color(0.85f, 0.18f, 0.22f, 1f);
        pColors.fadeDuration = 0.1f;
        pButton.colors = pColors;

        // Agregar ScreenshotManager al boton si no existe
        ScreenshotManager ssMgr = Object.FindFirstObjectByType<ScreenshotManager>();
        if (ssMgr == null)
        {
            ssMgr = pBtn.AddComponent<ScreenshotManager>();
        }

        UnityEditor.Events.UnityEventTools.AddStringPersistentListener(
            pButton.onClick,
            new UnityEngine.Events.UnityAction<string>(ssMgr.TakePhoto),
            "Foto.png"
        );

        // Texto del boton
        GameObject pText = CreateUI("Text", pBtn.transform);
        RectTransform pTR = pText.GetComponent<RectTransform>();
        pTR.anchorMin = Vector2.zero;
        pTR.anchorMax = Vector2.one;
        pTR.offsetMin = Vector2.zero;
        pTR.offsetMax = Vector2.zero;
        TextMeshProUGUI pTMP = pText.AddComponent<TextMeshProUGUI>();
        pTMP.text = "TOMAR FOTO";
        pTMP.fontSize = 24;
        pTMP.alignment = TextAlignmentOptions.Center;
        pTMP.color = Color.white;
        pTMP.fontStyle = FontStyles.Bold;
        pTMP.characterSpacing = 3f;
        pTMP.raycastTarget = false;

        // Espaciador der
        GameObject sR = CreateUI("sR", photoRow.transform);
        sR.AddComponent<LayoutElement>().flexibleWidth = 1;

        Debug.Log("UI creada.");
    }

    private static void CreateEraButton(Transform parent, int index,
        string eraName, string desc, Color bg, Color hl)
    {
        GameObject btn = CreateUI($"BtnEra_{index}", parent);

        Image img = btn.AddComponent<Image>();
        img.color = bg;

        Button button = btn.AddComponent<Button>();
        ColorBlock cb = button.colors;
        cb.normalColor = bg;
        cb.highlightedColor = hl;
        cb.pressedColor = bg * 0.6f;
        cb.selectedColor = hl;
        cb.fadeDuration = 0.12f;
        button.colors = cb;

        EraButtonHandler handler = btn.AddComponent<EraButtonHandler>();
        handler.eraIndex = index;
        handler.selectedScale = 1.06f;

        VerticalLayoutGroup vl = btn.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(6, 6, 10, 10);
        vl.spacing = 2;
        vl.childAlignment = TextAnchor.MiddleCenter;
        vl.childControlWidth = true;
        vl.childControlHeight = true;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;

        // Nombre
        GameObject nameObj = CreateUI("Name", btn.transform);
        nameObj.AddComponent<LayoutElement>().preferredHeight = 32;
        TextMeshProUGUI nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
        nameTMP.text = eraName;
        nameTMP.fontSize = 24;
        nameTMP.alignment = TextAlignmentOptions.Center;
        nameTMP.color = Color.white;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.raycastTarget = false;

        // Descripcion
        GameObject descObj = CreateUI("Desc", btn.transform);
        descObj.AddComponent<LayoutElement>().preferredHeight = 20;
        TextMeshProUGUI descTMP = descObj.AddComponent<TextMeshProUGUI>();
        descTMP.text = desc;
        descTMP.fontSize = 14;
        descTMP.alignment = TextAlignmentOptions.Center;
        descTMP.color = new Color(1, 1, 1, 0.5f);
        descTMP.fontStyle = FontStyles.Italic;
        descTMP.raycastTarget = false;

        // Indicador seleccion
        GameObject ind = CreateUI("Sel", btn.transform);
        RectTransform indRT = ind.GetComponent<RectTransform>();
        indRT.anchorMin = Vector2.zero;
        indRT.anchorMax = Vector2.one;
        indRT.offsetMin = new Vector2(-3, -3);
        indRT.offsetMax = new Vector2(3, 3);
        Image indImg = ind.AddComponent<Image>();
        indImg.color = new Color(0.92f, 0.72f, 0.15f, 0f);
        indImg.raycastTarget = false;
        Outline indOutline = ind.AddComponent<Outline>();
        indOutline.effectColor = new Color(0.92f, 0.72f, 0.15f, 1f);
        indOutline.effectDistance = new Vector2(3, 3);
        ind.SetActive(false);
        handler.selectedIndicator = ind;
    }

    private static GameObject CreateUI(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        if (obj.GetComponent<RectTransform>() == null)
            obj.AddComponent<RectTransform>();
        return obj;
    }
}
