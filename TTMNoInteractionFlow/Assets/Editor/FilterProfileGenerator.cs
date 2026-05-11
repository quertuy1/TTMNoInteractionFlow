using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;

public class FilterProfileGenerator
{
    [MenuItem("TTM/Generar Perfiles de Filtros (Fijar Post-Processing)")]
    public static void GenerateProfiles()
    {
        string dir = "Assets/Filters/VolumeProfiles";
        if (!AssetDatabase.IsValidFolder("Assets/Filters"))
            AssetDatabase.CreateFolder("Assets", "Filters");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Filters", "VolumeProfiles");

        // 50s
        VolumeProfile p50 = CreateProfile($"{dir}/VP_Epoca_50s.asset");
        var ca50 = p50.Add<ColorAdjustments>();
        ca50.active = true;
        ca50.saturation.Override(-100f);
        ca50.contrast.Override(30f);
        
        var fg50 = p50.Add<FilmGrain>();
        fg50.active = true;
        fg50.type.Override(FilmGrainLookup.Thin1);
        fg50.intensity.Override(0.8f);

        var vig50 = p50.Add<Vignette>();
        vig50.active = true;
        vig50.intensity.Override(0.45f);
        vig50.smoothness.Override(0.3f);

        // 70s
        VolumeProfile p70 = CreateProfile($"{dir}/VP_Epoca_70s.asset");
        var ca70 = p70.Add<ColorAdjustments>();
        ca70.active = true;
        ca70.saturation.Override(-50f);
        ca70.contrast.Override(-15f);
        ca70.colorFilter.Override(new Color(1f, 0.95f, 0.8f));
        ca70.postExposure.Override(0.3f);

        var wb70 = p70.Add<WhiteBalance>();
        wb70.active = true;
        wb70.temperature.Override(40f);
        wb70.tint.Override(10f);

        var fg70 = p70.Add<FilmGrain>();
        fg70.active = true;
        fg70.type.Override(FilmGrainLookup.Medium3);
        fg70.intensity.Override(0.5f);

        var vig70 = p70.Add<Vignette>();
        vig70.active = true;
        vig70.intensity.Override(0.35f);

        var blm70 = p70.Add<Bloom>();
        blm70.active = true;
        blm70.intensity.Override(0.5f);
        blm70.threshold.Override(0.9f);

        // 90s
        VolumeProfile p90 = CreateProfile($"{dir}/VP_Epoca_90s.asset");
        var ca90 = p90.Add<ColorAdjustments>();
        ca90.active = true;
        ca90.saturation.Override(25f);
        ca90.contrast.Override(15f);

        var chr90 = p90.Add<ChromaticAberration>();
        chr90.active = true;
        chr90.intensity.Override(0.3f);

        var fg90 = p90.Add<FilmGrain>();
        fg90.active = true;
        fg90.type.Override(FilmGrainLookup.Thin2);
        fg90.intensity.Override(0.4f);

        var blm90 = p90.Add<Bloom>();
        blm90.active = true;
        blm90.intensity.Override(0.8f);
        blm90.threshold.Override(1.1f);

        var vig90 = p90.Add<Vignette>();
        vig90.active = true;
        vig90.intensity.Override(0.2f);

        // Actual
        VolumeProfile pAct = CreateProfile($"{dir}/VP_Epoca_Actual.asset");
        var caAct = pAct.Add<ColorAdjustments>();
        caAct.active = true;
        caAct.saturation.Override(5f);
        caAct.contrast.Override(5f);

        var tmAct = pAct.Add<Tonemapping>();
        tmAct.active = true;
        tmAct.mode.Override(TonemappingMode.ACES);

        var blmAct = pAct.Add<Bloom>();
        blmAct.active = true;
        blmAct.intensity.Override(0.15f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Asignar al FilterManager
        FilterManager fm = Object.FindFirstObjectByType<FilterManager>();
        if (fm != null)
        {
            fm.profile50s = p50;
            fm.profile70s = p70;
            fm.profile90s = p90;
            fm.profileActual = pAct;
            EditorUtility.SetDirty(fm);
        }

        // Asegurar que la camara principal tiene PostProcessing
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            var camData = mainCam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null) camData = mainCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            EditorUtility.SetDirty(mainCam.gameObject);
        }

        Debug.Log("✅ Perfiles de filtro generados nativamente y asignados.");
    }

    private static VolumeProfile CreateProfile(string path)
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile != null)
        {
            // Limpiar componentes existentes
            profile.components.Clear();
        }
        else
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }
        return profile;
    }
}
