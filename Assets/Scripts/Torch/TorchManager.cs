using UnityEngine;
using UnityEngine.VFX;

public class TorchManager : MonoBehaviour
{
    VisualEffect[] _vfxs;
    Light _light;
    float _baseLight;
    float _baseRange;


    float[] _baseFlameEmissiveIntensity;
    float[] _baseFlamePower;
    float[] _baseFlameSize;
    float[] _baseGlowIntensity;
    float[] _baseEmbersRate;
    float[] _baseSmokeDensity;

    ParticleSystem[] _smokes;
    float[] _baseSmokeEmission;

    float _lastIntensity = -1f;

    void Awake()
    {
        DontDestroyOnLoad(transform.root.gameObject);
    }

    void Start()
    {
        _vfxs   = GetComponentsInChildren<VisualEffect>(true);
        _light  = GetComponentInChildren<Light>(true);
        _smokes = GetComponentsInChildren<ParticleSystem>(true);
        if (_light != null) { _baseLight = _light.intensity; _baseRange = _light.range; }

        _baseSmokeEmission = new float[_smokes.Length];
        for (int i = 0; i < _smokes.Length; i++)
            _baseSmokeEmission[i] = _smokes[i].emission.rateOverTime.constant;


        _baseFlameEmissiveIntensity = new float[_vfxs.Length];
        _baseFlamePower             = new float[_vfxs.Length];
        _baseFlameSize              = new float[_vfxs.Length];
        _baseGlowIntensity          = new float[_vfxs.Length];
        _baseEmbersRate             = new float[_vfxs.Length];
        _baseSmokeDensity           = new float[_vfxs.Length];

        for (int i = 0; i < _vfxs.Length; i++)
        {
           
            _baseFlameEmissiveIntensity[i] = _vfxs[i].GetFloat("FlameEmissive intensity");
            _baseFlamePower[i]             = _vfxs[i].GetFloat("Flame_Power");
            _baseFlameSize[i]              = _vfxs[i].GetFloat("FlameSize");
            _baseGlowIntensity[i]          = _vfxs[i].GetFloat("GlowIntensity");
            _baseEmbersRate[i]             = _vfxs[i].GetFloat("EmbersRate");
            _baseSmokeDensity[i]           = _vfxs[i].GetFloat("Smoke_Density");
        }
    }

    void Update()
    {
        float intensity = IntensityManager.Instance.intensity;
        if (Mathf.Approximately(intensity, _lastIntensity)) return;

        float lightMult, fireMult;
        if (intensity < 0.7f)
        {
            float p = Mathf.Sin(intensity / 0.7f * Mathf.PI * 0.5f); 
            lightMult = 1f   + 80f  * p * p;
            fireMult  = 0.8f + 1.0f * p * p;
        }
        else if (intensity < 0.92f)
        {
            float s = Mathf.SmoothStep(0f, 1f, (intensity - 0.7f) / 0.22f); 
            lightMult = Mathf.Lerp(81f,  1.2f, s);
            fireMult  = Mathf.Lerp(1.8f, 1.2f, s);
        }
        else
        {
            float s = Mathf.SmoothStep(0f, 1f, (intensity - 0.92f) / 0.08f);
            lightMult = Mathf.Lerp(1.2f, 2.5f,  s);
            fireMult  = Mathf.Lerp(1.2f, 0.8f, s);
        }

        float slowSize = Mathf.Pow(intensity, 0.5f);

        for (int i = 0; i < _vfxs.Length; i++)
        {
            
            _vfxs[i].SetFloat("FlameEmissive intensity", _baseFlameEmissiveIntensity[i] * intensity * fireMult);
            _vfxs[i].SetFloat("Flame_Power",             _baseFlamePower[i]             * intensity * fireMult);
            _vfxs[i].SetFloat("FlameSize",               _baseFlameSize[i]              * slowSize  * Mathf.Clamp(fireMult, 0.7f, 1f));
            _vfxs[i].SetFloat("GlowIntensity",           _baseGlowIntensity[i]          * intensity * fireMult);
            _vfxs[i].SetFloat("EmbersRate",              _baseEmbersRate[i]             * intensity);
            _vfxs[i].SetFloat("Smoke_Density",           _baseSmokeDensity[i]           * intensity);
        }

        for (int i = 0; i < _smokes.Length; i++)
        {
            var emission = _smokes[i].emission;
            var rate = emission.rateOverTime;
            rate.constant = _baseSmokeEmission[i] * intensity;
            emission.rateOverTime = rate;
        }

        if (_light != null)
        {
            _light.intensity = _baseLight * intensity * lightMult;
            _light.range     = _baseRange * Mathf.Max(Mathf.Pow(lightMult, 0.4f), 1.4f * intensity);
        }

        _lastIntensity = intensity;
    }
}
